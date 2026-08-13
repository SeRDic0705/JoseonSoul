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
