using UnityEngine;

public class PlayerDodgeAttackState : PlayerAttackState
{
    private bool alreadyAppliedForce;

    private AttackInfo attackInfo;

    public PlayerDodgeAttackState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        StartAnim(stateMachine.Player.AnimationData.DodgeAttackParameterHash);

        alreadyAppliedForce = false;
        attackInfo = stateMachine.Player.Data.AttackData.DodgeAttackInfo;
    }

    public override void Exit()
    {
        base.Exit();
        StopAnim(stateMachine.Player.AnimationData.DodgeAttackParameterHash);
    }

    private void TryApplyForce()
    {
        if (alreadyAppliedForce) return;
        alreadyAppliedForce = true;

        stateMachine.Player.ForceReceiver.Reset();
        stateMachine.Player.ForceReceiver.AddForce(stateMachine.Player.transform.forward * attackInfo.Force);
    }

    public override void Update()
    {
        base.Update();

        ForceMove();

        // ComboAttackState와 동일하게 "Attack" 태그로 정규화 시간 추적 (Animator State의 Tag를 Attack으로 설정)
        float normalizedTime = GetNormalizedTime(stateMachine.Player.Animator, "Attack");
        if (normalizedTime < 1f)
        {
            if (normalizedTime >= attackInfo.ForceTransitionTime)
                TryApplyForce();
        }
        else
        {
            if (stateMachine.IsAttacking)
            {
                // 회피공격 종료 시점까지 공격키를 누르고 있으면 일반 콤보로 이어서 체인
                stateMachine.ComboIndex = attackInfo.ComboStateIndex;
                stateMachine.ChangeState(stateMachine.ComboAttackState);
            }
            else
            {
                stateMachine.ChangeState(stateMachine.IdleState);
            }
        }
    }
}
