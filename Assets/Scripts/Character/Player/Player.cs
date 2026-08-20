using UnityEngine;

public class Player : MonoBehaviour
{
    [field: Header("References")]
    [field: SerializeField] public PlayerSO Data { get; private set; }

    [field: Header("Animations")]
    [field: SerializeField] public PlayerAnimationData AnimationData { get; private set; }

    [field: Header("Camera")]
    [Tooltip("이동 방향 계산에 쓰는 카메라 브리지 참조(Deoccluder 영향 없는 오빗 각도 조회용). 브리지가 이미 Player를 인스펙터로 참조하는 것과 대칭으로, 여기서도 명시적으로 배선한다.")]
    [field: SerializeField] public CinemachineCameraBridge CameraBridge { get; private set; }

    public Rigidbody Rigidbody { get; private set; }
    public Animator Animator { get; private set; }
    public PlayerInput Input { get; private set; }
    public CharacterController Controller { get; private set; }
    public ForceReceiver ForceReceiver { get; private set; }
    public PlayerLockOn LockOn { get; private set; }
    public PlayerCameraFollowTarget CameraFollowTarget { get; private set; }
    public PlayerHitbox Hitbox { get; private set; }

    private PlayerStateMachine stateMachine;
    public PlayerStateMachine StateMachine => stateMachine;


    private void Awake()
    {
        AnimationData.Initialize();

        Rigidbody = GetComponent<Rigidbody>();
        Animator = GetComponentInChildren<Animator>();
        Input = GetComponent<PlayerInput>();
        Controller = GetComponent<CharacterController>();
        ForceReceiver = GetComponent<ForceReceiver>();
        LockOn = GetComponent<PlayerLockOn>();
        CameraFollowTarget = GetComponent<PlayerCameraFollowTarget>();
        Hitbox = GetComponentInChildren<PlayerHitbox>();

        stateMachine = new PlayerStateMachine(this);
        
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        stateMachine.ChangeState(stateMachine.IdleState);
    }

    private void Update()
    {
        stateMachine.HandleInput();
        stateMachine.Update();
    }

    private void FixedUpdate()
    {
        stateMachine.PhysicsUpdate();
    }

}
