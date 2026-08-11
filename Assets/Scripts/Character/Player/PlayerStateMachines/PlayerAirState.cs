using UnityEngine;

public class PlayerAirState : PlayerBaseState, IAirborneState
{
    protected override bool CanJump => false;    // 이중 점프 없음

    public PlayerAirState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        StartAnim(stateMachine.Player.AnimationData.AirParameterHash);
    }

    public override void Exit()
    {
        base.Exit();
        StopAnim(stateMachine.Player.AnimationData.AirParameterHash);
    }

    protected override void OnAttack()
    {
        stateMachine.ComboIndex = 0;    // 체인 재진입이 아닌 새 진입 — 이전 상태 Exit() 정리에만 의존하지 않음(Design/AirState_Design.md §8-4)
        stateMachine.ChangeState(stateMachine.AirComboAttackState);
    }

    public override void Update()
    {
        base.Update();
        if (stateMachine.CurrentState != this) return;    // base.Update()에서 이미 다른 상태로 전이됨 — 여기서 멈춤

        // 착지: 땅에 닿았고 + 더는 위로 올라가지 않을 때(점프 시작 직후 isGrounded 잔류값으로 인한 오탐 방지)
        if (stateMachine.Player.Controller.isGrounded && stateMachine.Player.ForceReceiver.Movement.y <= 0f)
        {
            ChangeToLocomotionState();
        }
    }
}
