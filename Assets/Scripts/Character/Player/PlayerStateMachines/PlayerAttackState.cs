using UnityEngine;

public class PlayerAttackState : PlayerBaseState, IForceEventReceiver
{
    protected AttackInfo attackInfo;
    protected bool forceHandled;   // 전진력 이벤트/폴백 배타 실행 가드

    public PlayerAttackState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {

    }

    public override void Enter()
    {
        stateMachine.MoveSpeedModifier = 0;
        base.Enter();

        forceHandled = false;

        StartAnim(stateMachine.Player.AnimationData.AttackParameterHash);
    }

    public override void Exit()
    {
        base.Exit();

        StopAnim(stateMachine.Player.AnimationData.AttackParameterHash);
    }

    // 애니메이션 이벤트/폴백 폴링 양쪽에서 호출 — forceHandled 가드로 중복 실행 방지
    public void OnApplyForce()
    {
        if (forceHandled) return;
        forceHandled = true;

        stateMachine.Player.ForceReceiver.Reset();
        stateMachine.Player.ForceReceiver.AddForce(stateMachine.Player.transform.forward * attackInfo.Force);
    }
}
