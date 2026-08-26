using UnityEngine;

public class PlayerJumpState : PlayerAirState
{
    public PlayerJumpState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        stateMachine.Player.ForceReceiver.Jump(stateMachine.Player.Data.AirData.JumpForce);
        StartAnim(stateMachine.Player.AnimationData.JumpParameterHash);
    }

    public override void Exit()
    {
        base.Exit();
        StopAnim(stateMachine.Player.AnimationData.JumpParameterHash);
    }

    public override void Update()
    {
        base.Update();    // 착지 체크 먼저 — 점프 직후엔 verticalVelocity>0이라 여기서 안 걸림
        if (stateMachine.CurrentState != this) return;

        if (stateMachine.Player.ForceReceiver.Movement.y <= 0f)    // 정점 통과
        {
            stateMachine.ChangeState(stateMachine.FallState);
        }
    }
}
