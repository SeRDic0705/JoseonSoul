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

    public Vector2 MoveInput { get; set; }
    public float MoveSpeed { get; private set; }
    public float RotationDamping { get; private set; }
    public float MoveSpeedModifier { get; set;} = 1f;

    public float JumpForce { get; set; }

    public bool IsAttacking { get; set; }
    public int ComboIndex { get; set; }

    public bool AttackQueued { get; set; }     // 회피 등 인터럽트 불가 상태 중 눌린 공격 입력 버퍼(원샷)
    public float AttackQueuedTime { get; set; }

    public Transform MainCameraTransform { get; set; }

    public PlayerStateMachine(Player player)
    {
        this.Player = player;

        IdleState = new PlayerIdleState(this);
        WalkState = new PlayerWalkState(this);
        AvoidState = new PlayerAvoidState(this);
        RunState = new PlayerRunState(this);
        ComboAttackState = new PlayerComboAttackState(this);
        DodgeAttackState = new PlayerDodgeAttackState(this);

        MainCameraTransform = Camera.main.transform;

        MoveSpeed = player.Data.GroundData.BaseSpeed;
        RotationDamping = player.Data.GroundData.BaseRotationDamping;
    }
}
