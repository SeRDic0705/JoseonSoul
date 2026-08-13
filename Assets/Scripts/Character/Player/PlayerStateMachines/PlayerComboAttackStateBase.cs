using UnityEngine;

// 지상/공중 콤보 공용 베이스 — 콤보창(Open/Close)·입력버퍼·Animation Event/폴백 배타 실행 로직을 여기서 관리.
// 하위 클래스는 공격 데이터 소스·재진입 상태·애니메이터 파라미터만 제공한다(Design/AirState_Design.md §8-7).
public abstract class PlayerComboAttackStateBase : PlayerAttackState, IComboWindowEventReceiver
{
    private const float ComboBufferDuration = 0.2f;    // 창 열리기 직전 입력을 인정하는 버퍼 유효시간(PlayerAvoidState.AttackQueueDuration과 동일 폭)

    private bool alreadyApplyCombo;

    // 이벤트/폴백 배타 실행 가드 — 전부 Enter()에서만 초기화(Exit()에서 리셋 안 함: 크로스페이드 블렌드 중 늦게 도착하는
    // 이전 클립의 이벤트가 다음 상태 진입 이후 들어와도 안전하도록)
    private bool openHandled;
    private bool closeHandled;

    private bool comboWindowOpen;   // OnOpenComboWindow~OnCloseComboWindow 구간 여부
    private bool comboRequested;    // 이번 구간에 다음 콤보 입력이 확정됐는지

    protected PlayerComboAttackStateBase(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    protected abstract AttackInfo GetAttackInfo(int comboIndex);
    protected abstract bool IsAirCombo { get; }    // 지상=false, 공중=true — 콤보 확정 시 목적지 패밀리가 바뀌었는지 판단용
    protected abstract int ComboAnimatorParameterHash { get; }

    public override void Enter()
    {
        base.Enter();
        StartAnim(ComboAnimatorParameterHash);

        alreadyApplyCombo = false;
        openHandled = false;
        closeHandled = false;
        comboWindowOpen = false;
        comboRequested = false;

        attackInfo = GetAttackInfo(stateMachine.ComboIndex);
        stateMachine.Player.Animator.SetInteger("Combo", stateMachine.ComboIndex);
    }

    public override void Exit()
    {
        base.Exit();
        StopAnim(ComboAnimatorParameterHash);

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
                // 착지/이륙으로 패밀리(지상↔공중)가 바뀌었으면 콤보를 리셋해 새 패밀리 1타부터,
                // 같은 패밀리 안에서 이어지면 기존처럼 스윙 번호 유지(Design/AirState_Design.md §8-3 B안)
                bool destinationIsAir = !stateMachine.Player.Controller.isGrounded;
                bool familyChanged = destinationIsAir != IsAirCombo;

                stateMachine.ComboIndex = familyChanged ? 0 : attackInfo.ComboStateIndex;
                if (familyChanged)
                {
                    stateMachine.AttackQueued = false;    // 패밀리 전환에 쓰인 입력이 새 1타의 콤보창에서 다시 소비되는 것 방지
                }
                if (destinationIsAir)
                {
                    stateMachine.AirborneFacesLockedTarget = FacesLockedTarget;    // 지상->공중 경계 스냅샷(공격으로 공중에 뜬 경우)
                }
                stateMachine.ChangeState(destinationIsAir ? stateMachine.AirComboAttackState : stateMachine.ComboAttackState);
            }
            else
            {
                ChangeToGroundedOrFall();
            }
        }

    }

}
