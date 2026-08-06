using UnityEngine;

// 콤보 창 개념이 없는 단발 공격 상태라 IComboWindowEventReceiver는 구현하지 않음 — Force만 이벤트/폴백 배타 실행(PlayerAttackState)
public class PlayerDodgeAttackState : PlayerAttackState
{
    public PlayerDodgeAttackState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        StartAnim(stateMachine.Player.AnimationData.DodgeAttackParameterHash);

        attackInfo = stateMachine.Player.Data.AttackData.DodgeAttackInfo;
    }

    public override void Exit()
    {
        base.Exit();
        StopAnim(stateMachine.Player.AnimationData.DodgeAttackParameterHash);
    }

    public override void Update()
    {
        base.Update();

        ForceMove();

        // ComboAttackState와 동일하게 "Attack" 태그로 정규화 시간 추적 (Animator State의 Tag를 Attack으로 설정)
        float normalizedTime = GetNormalizedTime(stateMachine.Player.Animator, "Attack");
        if (normalizedTime < 1f)
        {
            if (!forceHandled && normalizedTime >= attackInfo.ForceTransitionTime)
                OnApplyForce();
        }
        else
        {
            if (stateMachine.IsAttacking)
            {
                // 회피공격 종료 시점까지 공격키를 누르고 있으면 일반 콤보로 이어서 체인
                stateMachine.ComboIndex = attackInfo.ComboStateIndex;
                stateMachine.ChangeState(stateMachine.ComboAttackState);
            }
            else
            {
                stateMachine.ChangeState(stateMachine.IdleState);
            }
        }
    }
}
