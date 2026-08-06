using UnityEngine;

public class PlayerIdleState : PlayerGroundState
{
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.MoveSpeedModifier = 0f;
        base.Enter();
        StartAnim(stateMachine.Player.AnimationData.IdleParameterHash);
    }

    public override void Exit()
    {
        base.Exit();
        StopAnim(stateMachine.Player.AnimationData.IdleParameterHash);
    }

    public override void Update()
    {
        base.Update();
        if (stateMachine.CurrentState != this) return;    // base.Update()에서 이미 다른 상태로 전이됨 — 여기서 멈춤

        if (stateMachine.MoveInput != Vector2.zero)
        {
            // 스페이스바(회피키) 계속 누르고 있다면 Run
            if (stateMachine.Player.Input.PlayerActions.AvoidRun.ReadValue<float>() > 0)
            {
                stateMachine.ChangeState(stateMachine.RunState);
            }
            else
            {
                OnMove();
            }
        }
    }
}
