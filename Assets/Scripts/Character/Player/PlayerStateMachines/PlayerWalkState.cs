using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWalkState : PlayerGroundState
{
    public PlayerWalkState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.MoveSpeedModifier = groundData.WalkSpeed;
        base.Enter();
        StartAnim(stateMachine.Player.AnimationData.WalkParameterHash);
    }

    public override void Exit()
    {
        base.Exit();
        StopAnim(stateMachine.Player.AnimationData.WalkParameterHash);
    }

    protected override void OnAvoidStarted(InputAction.CallbackContext context)
    {
        base.OnAvoidStarted(context);
        stateMachine.ChangeState(stateMachine.AvoidState);
    }
}
