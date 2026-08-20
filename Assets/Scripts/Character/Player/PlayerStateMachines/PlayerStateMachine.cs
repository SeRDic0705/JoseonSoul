using UnityEngine;

public class PlayerStateMachine : StateMachine
{
    public Player Player { get; }

    // States
    public PlayerIdleState IdleState { get; }
    public PlayerWalkState WalkState { get; }
    public PlayerAvoidState AvoidState { get; }
    public PlayerRunState RunState { get; }
    public PlayerComboAttackState ComboAttackState { get; }
    public PlayerDodgeAttackState DodgeAttackState { get; }
    public PlayerJumpState JumpState { get; }
    public PlayerFallState FallState { get; }
    public PlayerAirComboAttackState AirComboAttackState { get; }

    public Vector2 MoveInput { get; set; }
    public float MoveSpeed { get; private set; }
    public float RotationDamping { get; private set; }
    public float MoveSpeedModifier { get; set;} = 1f;

    public bool IsAttacking { get; set; }
    public int ComboIndex { get; set; }

    public bool AttackQueued { get; set; }     // 회피 등 인터럽트 불가 상태 중 눌린 공격 입력 버퍼(원샷)
    public float AttackQueuedTime { get; set; }

    public Transform MainCameraTransform { get; set; }
    public CinemachineCameraBridge CameraBridge { get; }

    public Transform LockedTarget => Player.LockOn.CurrentTarget;

    // 지상->공중 경계를 넘는 순간의 FacesLockedTarget 값을 스냅샷해 공중 상태(PlayerAirState/
    // PlayerAirComboAttackState)가 그대로 물려받게 한다 — Run(false)에서 점프하면 공중에서도 계속
    // 자유 회전, Idle/Walk/공격(true)에서 점프하면 공중에서도 계속 타겟 응시(2026-08-13 Discord 합의).
    // 공중 내부 전이(Jump<->Fall, Air<->AirCombo)는 건드리지 않음 — 어차피 이 값을 그대로 읽으므로 유지됨.
    public bool AirborneFacesLockedTarget { get; set; } = true;

    public bool IsGrounded => !(CurrentState is IAirborneState);

    // Controller.isGrounded 원시값 대신 쓰는 디바운스된 접지 판정 — 콤보 목적지(지상/공중) 판단처럼
    // 단발성 오판이 애니메이터 상태 불일치로 이어지는 곳에 사용한다(더미처럼 둥근 콜라이더 위에 서면
    // isGrounded가 프레임 단위로 깜빡여서, 이 값을 그대로 쓰면 콤보 전환 중 엉뚱한 패밀리로 전이해버림 —
    // 2026-08-20 발견). PlayerGroundState의 notGroundedGracePeriod와 같은 디바운스 폭을 공유 지점에 재사용.
    private const float NotGroundedDebounce = 0.15f;
    private float notGroundedTimer;
    public bool IsGroundedStable { get; private set; } = true;

    public void UpdateGroundedStability()
    {
        if (Player.Controller.isGrounded)
        {
            notGroundedTimer = 0f;
            IsGroundedStable = true;
        }
        else
        {
            notGroundedTimer += Time.deltaTime;
            if (notGroundedTimer >= NotGroundedDebounce)
            {
                IsGroundedStable = false;
            }
        }
    }

    public PlayerStateMachine(Player player)
    {
        this.Player = player;

        IdleState = new PlayerIdleState(this);
        WalkState = new PlayerWalkState(this);
        AvoidState = new PlayerAvoidState(this);
        RunState = new PlayerRunState(this);
        ComboAttackState = new PlayerComboAttackState(this);
        DodgeAttackState = new PlayerDodgeAttackState(this);
        JumpState = new PlayerJumpState(this);
        FallState = new PlayerFallState(this);
        AirComboAttackState = new PlayerAirComboAttackState(this);

        MainCameraTransform = Camera.main.transform;
        CameraBridge = player.CameraBridge;

        MoveSpeed = player.Data.GroundData.BaseSpeed;
        RotationDamping = player.Data.GroundData.BaseRotationDamping;
    }
}
