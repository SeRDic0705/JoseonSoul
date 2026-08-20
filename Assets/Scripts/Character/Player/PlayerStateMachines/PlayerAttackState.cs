using UnityEngine;

public class PlayerAttackState : PlayerBaseState, IForceEventReceiver, IHitWindowEventReceiver
{
    protected AttackInfo attackInfo;
    protected bool forceHandled;   // 전진력 이벤트/폴백 배타 실행 가드

    // 공격 중엔 (공유 Update() 체크가) 같은 공격으로 재차 전이시키지 않도록, 그리고 점프도 금지
    protected override bool CanBeInterruptedByAttack => false;
    protected override bool CanJump => false;

    public PlayerAttackState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {

    }

    // 공격류가 켜는 애니메이터 family bool — 지상은 @Attack(기본값), 공중공격은 오버라이드해서 @AirAttack 사용
    protected virtual int AttackFamilyParameterHash => stateMachine.Player.AnimationData.AttackParameterHash;

    public override void Enter()
    {
        stateMachine.MoveSpeedModifier = 0;
        base.Enter();

        forceHandled = false;

        StartAnim(AttackFamilyParameterHash);
    }

    public override void Exit()
    {
        base.Exit();

        StopAnim(AttackFamilyParameterHash);
        stateMachine.Player.Hitbox.Deactivate();
    }

    // 애니메이션 이벤트/폴백 폴링 양쪽에서 호출 — forceHandled 가드로 중복 실행 방지
    public void OnApplyForce()
    {
        if (forceHandled) return;
        forceHandled = true;

        stateMachine.Player.ForceReceiver.Reset();
        stateMachine.Player.ForceReceiver.AddForce(stateMachine.Player.transform.forward * attackInfo.Force);
    }

    public void OnOpenHitWindow()
    {
        stateMachine.Player.Hitbox.Activate(attackInfo.Damage);
    }

    public void OnCloseHitWindow()
    {
        stateMachine.Player.Hitbox.Deactivate();
    }

    // 콤보 미확정/닷지 종료 등, 공격류 상태가 끝날 때 목적지를 isGrounded로 분기(Design/AirState_Design.md §8-1/8-2)
    protected void ChangeToGroundedOrFall()
    {
        if (stateMachine.IsGroundedStable)
        {
            ChangeToLocomotionState();
        }
        else
        {
            stateMachine.AirborneFacesLockedTarget = FacesLockedTarget;    // 지상->공중 경계 스냅샷
            stateMachine.ChangeState(stateMachine.FallState);
        }
    }
}
