using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBaseState : IState
{
    protected PlayerStateMachine stateMachine;
    protected readonly PlayerGroundData groundData;

    public PlayerBaseState(PlayerStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        groundData = this.stateMachine.Player.Data.GroundData;
    }

    public virtual void Enter()
    {
        AddInputActionsCallback();
    }

    public virtual void Exit()
    {
        RemoveInputActionsCallback();
    }

    public virtual void HandleInput()
    {
        ReadMoveInput();
    }

    public virtual void PhysicsUpdate()
    {

    }

    public virtual void Update()
    {
        Move();
    }


    protected virtual void AddInputActionsCallback()
    {
        PlayerInput input = stateMachine.Player.Input;
        input.PlayerActions.Move.canceled += OnMoveCanceled;
        input.PlayerActions.AvoidRun.started += OnAvoidStarted;


        stateMachine.Player.Input.PlayerActions.Attack.performed += OnAttackPerformed;
        stateMachine.Player.Input.PlayerActions.Attack.canceled += OnAttackCanceled;
    }

    protected virtual void RemoveInputActionsCallback()
    {
        PlayerInput input = stateMachine.Player.Input;
        input.PlayerActions.Move.canceled -= OnMoveCanceled;
        input.PlayerActions.AvoidRun.started -= OnAvoidStarted;


        stateMachine.Player.Input.PlayerActions.Attack.performed -= OnAttackPerformed;
        stateMachine.Player.Input.PlayerActions.Attack.canceled -= OnAttackCanceled;
    }

    protected virtual void OnMoveCanceled(InputAction.CallbackContext context)
    {

    }

    protected virtual void OnAvoidStarted(InputAction.CallbackContext context)
    {
        
    }

    protected virtual void OnAttackPerformed(InputAction.CallbackContext obj)
    {
        stateMachine.IsAttacking = true;
    }

    protected virtual void OnAttackCanceled(InputAction.CallbackContext obj)
    {
        stateMachine.IsAttacking = false;
    }

    private void ReadMoveInput()
    {
        stateMachine.MoveInput = stateMachine.Player.Input.PlayerActions.Move.ReadValue<Vector2>();
    }

    private void Move()
    {
        Vector3 moveDir = GetMoveDir();

        Rotate(moveDir);

        Move(moveDir);
    }

    private Vector3 GetMoveDir()
    {
        Vector3 forward = stateMachine.MainCameraTransform.forward;
        Vector3 right = stateMachine.MainCameraTransform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        return forward * stateMachine.MoveInput.y + right * stateMachine.MoveInput.x;
    }

    private void Move(Vector3 moveDir)
    {
        float moveSpeed = GetMoveSpeed();
        stateMachine.Player.Controller.Move(
        ((moveDir * moveSpeed)
        + stateMachine.Player.ForceReceiver.Movement)
        * Time.deltaTime
        );
    }

    protected void ForceMove()
    {
        stateMachine.Player.Controller.Move(stateMachine.Player.ForceReceiver.Movement * Time.deltaTime);
    }

    private void Rotate(Vector3 moveDir)
    {
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            stateMachine.Player.transform.rotation = Quaternion.Slerp(stateMachine.Player.transform.rotation, targetRot, stateMachine.RotationDamping * Time.deltaTime);
        }
    }

    private float GetMoveSpeed()
    {
        float moveSpeed = stateMachine.MoveSpeed * stateMachine.MoveSpeedModifier;
        return moveSpeed;
    }

    protected void StartAnim(int animHash)
    {
        stateMachine.Player.Animator.SetBool(animHash, true);
    }

    protected void StopAnim(int animHash)
    {
        stateMachine.Player.Animator.SetBool(animHash, false);
    }

    protected float GetNormalizedTime(Animator animator, string tag)
    {
        AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(0);    // 현재 애니메이션 정보
        AnimatorStateInfo nextInfo = animator.GetNextAnimatorStateInfo(0);  // 다음에 올 애니메이션 정보

        if (animator.IsInTransition(0) && nextInfo.IsTag(tag))      // 트랜지션 중에 있고, 다음 애니메이션의 태그가 일치한다면(다음 애니메이션이 이미 블렌딩 되고 있다면)
        {
            return nextInfo.normalizedTime;     // 현재 애니메이션이 몇% 재생됐는지
        }
        else if (!animator.IsInTransition(0) && currentInfo.IsTag(tag))     // 트랜지션 중이 아니고, 다음 애니메이션의 태그가 일치한다면
        {
            return currentInfo.normalizedTime;  // 다음 애니메이션이 몇% 재생됐는지
        }
        else
        {
            return 0f;
        }
    }
}
