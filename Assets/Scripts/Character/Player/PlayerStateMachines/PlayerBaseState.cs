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
        input.PlayerActions.Run.started += OnRunStarted;
    }

    protected virtual void RemoveInputActionsCallback()
    {
        PlayerInput input = stateMachine.Player.Input;
        input.PlayerActions.Move.canceled -= OnMoveCanceled;
        input.PlayerActions.Run.started -= OnRunStarted;
    }

    protected virtual void OnMoveCanceled(InputAction.CallbackContext context)
    {

    }

    protected virtual void OnRunStarted(InputAction.CallbackContext context)
    {
        
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
        stateMachine.Player.Controller.Move(moveDir * moveSpeed * Time.deltaTime);
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
}
