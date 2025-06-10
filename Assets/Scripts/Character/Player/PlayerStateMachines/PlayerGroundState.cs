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
        base.Update();

        if (stateMachine.IsAttacking)
        {
            OnAttack();
            return;
        }
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

    protected virtual void OnAttack()
    {
        stateMachine.ChangeState(stateMachine.ComboAttackState);
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
