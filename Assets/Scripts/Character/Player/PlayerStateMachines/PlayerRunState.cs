using UnityEngine;

public class PlayerRunState : PlayerGroundState
{
    public PlayerRunState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    protected override bool FacesLockedTarget => false;    // Run 중엔 락온 상태여도 이동 방향 기준 자유 회전 — Run 종료 시 다음 상태의 Rotate()가 다시 타겟을 향함

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
