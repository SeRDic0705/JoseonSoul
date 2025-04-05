using UnityEngine;

public class Player : MonoBehaviour
{
    [field: Header("Animations")]
    [field: SerializeField] public PlayerAnimationData animationData { get; private set; }


    public Rigidbody Rigidbody { get; private set; }
    public Animator Animator { get; private set; }
    public PlayerInput Input { get; private set; }
    public CharacterController Controller { get; private set; }


    private void Awake()
    {
        animationData.Initialize();

        Rigidbody = GetComponent<Rigidbody>();
        Animator = GetComponent<Animator>();
        Input = GetComponent<PlayerInput>();
        Controller = GetComponent<CharacterController>();
        
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }


}
