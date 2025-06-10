using UnityEngine;

public class PlayerStateMachine : StateMachine
{
    public Player Player { get; }

    // States
    public PlayerIdleState IdleState { get; }
    public PlayerWalkState WalkState { get; }
    public PlayerAvoidState AvoidState { get; }
    public PlayerRunState RunState { get; }
    public PlayerComboState AttackState { get; }

    public Vector2 MoveInput { get; set; }
    public float MoveSpeed { get; private set; }
    public float RotationDamping { get; private set; }
    public float MoveSpeedModifier { get; set;} = 1f;

    public float JumpForce { get; set; }

    public bool IsAttacking { get; set; }
    public int ComboIndex { get; set; }

    public Transform MainCameraTransform { get; set; }

    public PlayerStateMachine(Player player)
    {
        this.Player = player;

        IdleState = new PlayerIdleState(this);
        WalkState = new PlayerWalkState(this);
        AvoidState = new PlayerAvoidState(this);
        RunState = new PlayerRunState(this);
        AttackState = new PlayerComboState(this);

        MainCameraTransform = Camera.main.transform;

        MoveSpeed = player.Data.GroundData.BaseSpeed;
        RotationDamping = player.Data.GroundData.BaseRotationDamping;
    }
}
