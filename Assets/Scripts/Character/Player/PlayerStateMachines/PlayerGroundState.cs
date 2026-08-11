using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGroundState : PlayerBaseState
{
    protected float lastMoveInputTime;
    protected float moveInputGracePeriod = 0.2f; // 입력 유예 시간

    public PlayerGroundState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        StartAnim(stateMachine.Player.AnimationData.GroundParameterHash);
    }

    public override void Exit()
    {
        base.Exit();
        StopAnim(stateMachine.Player.AnimationData.GroundParameterHash);
    }

    public override void Update()
    {
        // 벼랑 이탈(점프 없이 낙하 시작) — 착지/벼랑 이탈이 공격·점프 입력보다 우선이라 base.Update() 전에 체크
        if (!stateMachine.Player.Controller.isGrounded && stateMachine.Player.ForceReceiver.Movement.y <= 0f)
        {
            stateMachine.ChangeState(stateMachine.FallState);
            return;
        }

        base.Update();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    protected override void OnMoveCanceled(InputAction.CallbackContext context)
    {
        base.OnMoveCanceled(context);
        lastMoveInputTime = Time.time;

        // 일정 시간 안에 다시 입력이 들어오면 Idle로 전이하지 않음
        stateMachine.Player.StartCoroutine(DelayedIdleCheck());

    }

    protected virtual void OnMove()
    {
        stateMachine.ChangeState(stateMachine.WalkState);
    }

    protected override void OnAttack()
    {
        stateMachine.ComboIndex = 0;    // 체인 재진입이 아닌 새 진입 — 이전 상태 Exit() 정리에만 의존하지 않음(Design/AirState_Design.md §8-4)
        stateMachine.ChangeState(stateMachine.ComboAttackState);
    }

    protected override void OnJump()
    {
        stateMachine.ChangeState(stateMachine.JumpState);
    }

    private IEnumerator DelayedIdleCheck()
    {
        yield return new WaitForSeconds(moveInputGracePeriod);

        if (stateMachine.MoveInput == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.IdleState);
        }
    }

}
