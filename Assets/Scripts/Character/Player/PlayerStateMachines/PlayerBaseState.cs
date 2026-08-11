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

        if (stateMachine.IsAttacking && CanBeInterruptedByAttack)
        {
            OnAttack();
            return;
        }

        if (CanJump && stateMachine.Player.Input.PlayerActions.Jump.WasPerformedThisFrame())
        {
            OnJump();
        }
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

    protected virtual bool CanBeInterruptedByAttack => true;    // 이 상태가 공격 입력으로 즉시 전이될 수 있는지 (회피/공격류 등은 false로 오버라이드)
    protected virtual bool CanJump => true;    // 이 상태에서 점프 입력을 받는지 (회피/공격류/공중류 등은 false로 오버라이드)

    protected virtual void OnAttackPerformed(InputAction.CallbackContext obj)
    {
        stateMachine.IsAttacking = true;
        stateMachine.AttackQueued = true;
        stateMachine.AttackQueuedTime = Time.time;
    }

    protected virtual void OnAttackCanceled(InputAction.CallbackContext obj)
    {
        stateMachine.IsAttacking = false;
    }

    // 공격 입력으로 전이할 목적지 — Ground/Air가 각자의 콤보 상태로 오버라이드. 공격/점프 우선순위는 Update()의 체크 순서(공격 먼저)로 보장.
    protected virtual void OnAttack()
    {

    }

    // 점프 입력으로 전이할 목적지 — Ground가 JumpState로 오버라이드.
    protected virtual void OnJump()
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

    // 현재 이동입력/AvoidRun 홀드 상태를 보고 Idle/Walk/Run 중 하나로 복귀 — 회피 종료, 착지 등 여러 진입점에서 공용
    protected void ChangeToLocomotionState()
    {
        bool isHoldingAvoidRun = stateMachine.Player.Input.PlayerActions.AvoidRun.ReadValue<float>() > 0f;
        bool hasMoveInput = stateMachine.MoveInput != Vector2.zero;

        if (isHoldingAvoidRun && hasMoveInput)
        {
            stateMachine.ChangeState(stateMachine.RunState);
        }
        else if (hasMoveInput)
        {
            stateMachine.ChangeState(stateMachine.WalkState);
        }
        else
        {
            stateMachine.ChangeState(stateMachine.IdleState);
        }
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
