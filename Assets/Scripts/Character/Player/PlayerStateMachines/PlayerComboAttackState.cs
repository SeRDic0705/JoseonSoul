using UnityEngine;

public class PlayerComboAttackState : PlayerAttackState, IComboWindowEventReceiver
{
    private const float ComboBufferDuration = 0.2f;    // 창 열리기 직전 입력을 인정하는 버퍼 유효시간(PlayerAvoidState.AttackQueueDuration과 동일 폭)

    private bool alreadyApplyCombo;

    // 이벤트/폴백 배타 실행 가드 — 셋 다 Enter()에서만 초기화(Exit()에서 리셋 안 함: 크로스페이드 블렌드 중 늦게 도착하는
    // 이전 클립의 이벤트가 다음 상태 진입 이후 들어와도 안전하도록)
    private bool openHandled;
    private bool closeHandled;

    private bool comboWindowOpen;   // OnOpenComboWindow~OnCloseComboWindow 구간 여부
    private bool comboRequested;    // 이번 구간에 다음 콤보 입력이 확정됐는지

    public PlayerComboAttackState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();
        StartAnim(stateMachine.Player.AnimationData.ComboAttackParameterHash);

        alreadyApplyCombo = false;
        openHandled = false;
        closeHandled = false;
        comboWindowOpen = false;
        comboRequested = false;

        int comboIndex = stateMachine.ComboIndex;
        attackInfo = stateMachine.Player.Data.AttackData.GetAttackInfo(comboIndex);
        stateMachine.Player.Animator.SetInteger("Combo", comboIndex);
    }

    public override void Exit()
    {
        base.Exit();
        StopAnim(stateMachine.Player.AnimationData.ComboAttackParameterHash);

        if (!alreadyApplyCombo)
        {
            stateMachine.ComboIndex = 0;
        }
    }

    private bool IsBufferFresh()
    {
        return stateMachine.AttackQueued && Time.time - stateMachine.AttackQueuedTime <= ComboBufferDuration;
    }

    public void OnOpenComboWindow()
    {
        if (openHandled || closeHandled) return;   // Close 이후 늦게 온 Open이 창을 되살리는 것 방지
        openHandled = true;
        comboWindowOpen = true;

        if (IsBufferFresh())
        {
            comboRequested = true;
            stateMachine.AttackQueued = false;     // 래치에 쓰인 즉시 소비
        }
    }

    public void OnCloseComboWindow()
    {
        if (closeHandled) return;
        closeHandled = true;
        comboWindowOpen = false;

        if (comboRequested && attackInfo.ComboStateIndex != -1)
        {
            alreadyApplyCombo = true;
        }
    }

    public override void Update()
    {
        base.Update();

        ForceMove();

        float normalizedTime = GetNormalizedTime(stateMachine.Player.Animator, "Attack");
        if (normalizedTime < 1f)
        {
            if (!forceHandled && normalizedTime >= attackInfo.ForceTransitionTime)
                OnApplyForce();

            if (comboWindowOpen && !comboRequested)
            {
                if (IsBufferFresh())
                {
                    comboRequested = true;
                    stateMachine.AttackQueued = false;     // 래치에 쓰인 즉시 소비 — 다음 상태로 유출 방지
                }
                else if (stateMachine.IsAttacking)
                {
                    comboRequested = true;
                }
            }

            if (!openHandled && normalizedTime >= attackInfo.ComboTransitionTime)
                OnOpenComboWindow();
        }
        else
        {
            if (!closeHandled)
                OnCloseComboWindow();

            if (alreadyApplyCombo)
            {
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
