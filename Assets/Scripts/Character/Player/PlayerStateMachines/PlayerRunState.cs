using UnityEngine;

public class PlayerRunState : PlayerGroundState
{
    public PlayerRunState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.MoveSpeedModifier = groundData.RunSpeed;
        base.Enter();
        Debug.Log("Enter Run");
        StartAnim(stateMachine.Player.AnimationData.RunParameterHash);
    }

    public override void Exit()
    {
        base.Exit();
        Debug.Log("Exit Run");
        StopAnim(stateMachine.Player.AnimationData.RunParameterHash);
    }

    public override void Update()
    {
        base.Update();
        if (stateMachine.CurrentState != this) return;    // base.Update()에서 이미 다른 상태로 전이됨 — 여기서 멈춤

        // 회피 키를 놓았을 때 Walk 상태로 전이
        if (stateMachine.Player.Input.PlayerActions.AvoidRun.ReadValue<float>() <= 0f)
        {
            if (stateMachine.MoveInput != Vector2.zero)
            {
                stateMachine.ChangeState(stateMachine.WalkState);
            }
            else
            {
                stateMachine.ChangeState(stateMachine.IdleState);
            }
        }
    }

}
