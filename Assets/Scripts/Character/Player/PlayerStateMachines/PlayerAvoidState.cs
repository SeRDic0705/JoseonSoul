using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAvoidState : PlayerGroundState
{
    private const float AttackQueueDuration = 0.2f;    // 회피 종료 시점 기준 공격 버퍼 유효시간(moveInputGracePeriod와 동일 폭)

    private float avoidStartTime;

    protected override bool CanBeInterruptedByAttack => false;    // 회피 재생 중엔 공격으로 즉시 끊기지 않음 — 종료 후 버퍼로 소비

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
        stateMachine.AttackQueued = false;     // 못 쓰인 버퍼가 다음 상태로 유출되지 않도록 초기화
    }

    private IEnumerator WaitForAvoidEnd()
    {
        yield return new WaitForSeconds(0.3f); // 회피 애니메이션 시간

        if (stateMachine.CurrentState != this) yield break;   // 대기 중 다른 경로로 이미 전이됨 — 늦은 재개 방지

        if (stateMachine.AttackQueued && Time.time - stateMachine.AttackQueuedTime <= AttackQueueDuration)
        {
            stateMachine.AttackQueued = false;
            stateMachine.ChangeState(stateMachine.DodgeAttackState);
            yield break;
        }

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
