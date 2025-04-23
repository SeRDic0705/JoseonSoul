using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAvoidState : PlayerGroundState
{
    private float avoidStartTime;

    public PlayerAvoidState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.MoveSpeedModifier = groundData.avoidSpeed;
        base.Enter();

        avoidStartTime = Time.time;

        StartAnim(stateMachine.Player.AnimationData.AvoidParameterHash);

        stateMachine.Player.StartCoroutine(WaitForAvoidEnd());

    }

    public override void Exit()
    {
        base.Exit();

        StopAnim(stateMachine.Player.AnimationData.AvoidParameterHash);
    }

    private IEnumerator WaitForAvoidEnd()
    {
        yield return new WaitForSeconds(0.3f); // 회피 애니메이션 시간

        bool isHoldingAvoid = stateMachine.Player.Input.PlayerActions.AvoidRun.ReadValue<float>() > 0;
        bool hasMoveInput = stateMachine.MoveInput != Vector2.zero;

        if (isHoldingAvoid && hasMoveInput)
        {
            stateMachine.ChangeState(stateMachine.RunState);
        }
        else if (hasMoveInput)
        {
            stateMachine.ChangeState(stateMachine.WalkState);
        }
        else
        {
            stateMachine.ChangeState(stateMachine.IdleState);
        }
    }

}
