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
        stateMachine.UpdateGroundedStability();

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
    protected virtual bool FacesLockedTarget => true;    // 락온 중 이 상태가 타겟 쪽을 계속 바라보는지 (Run은 이동 방향 기준 자유 회전을 위해 false로 오버라이드)

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

    // Camera.main.transform(=MainCameraTransform)이 아니라 CameraBridge.GetPlanarMoveBasis()를 쓴다 —
    // 벽 근처에서 CinemachineDeoccluder가 카메라 위치를 보정할 때 Camera.main.transform.forward가 함께 흔들려서
    // 이동 방향이 진동하는 피드백 루프가 있었다(2026-08-12 Discord 진단). MainCameraTransform은 락온 시야각
    // 판정(PlayerLockOn) 등 "화면에 실제로 보이는지"가 기준인 곳엔 계속 쓴다.
    private Vector3 GetMoveDir()
    {
        (Vector3 forward, Vector3 right) = stateMachine.CameraBridge.GetPlanarMoveBasis();

        return forward * stateMachine.MoveInput.y + right * stateMachine.MoveInput.x;
    }

    private void Move(Vector3 moveDir)
    {
        float moveSpeed = GetMoveSpeed();
        Vector3 inputDelta = moveDir * moveSpeed * Time.deltaTime;

        if (IsBlockedByWall(inputDelta))
        {
            inputDelta = Vector3.zero;
        }

        // ForceReceiver(중력/넉백)는 벽 검사에 섞지 않고 항상 그대로 전달 — CharacterController 기본 충돌 처리를 그대로 받는다.
        stateMachine.Player.Controller.Move(inputDelta + stateMachine.Player.ForceReceiver.Movement * Time.deltaTime);
    }

    // 입력 이동 델타 방향으로 벽을 미리 검사해, 거의 정면으로 부딪히는 경우 CharacterController의 기본 collide&slide
    // 대신 이동 자체를 완전히 정지시킨다(정면 충돌에서도 미세한 접선 성분이 누적돼 옆으로 밀리는 문제 방지).
    // 비스듬한 충돌은 여기서 손대지 않고 기존 슬라이드 그대로 둔다(벽 타는 느낌 유지).
    private bool IsBlockedByWall(Vector3 inputDelta)
    {
        if (inputDelta.sqrMagnitude < 0.0001f) return false;

        CharacterController controller = stateMachine.Player.Controller;
        Transform t = controller.transform;

        Vector3 direction = inputDelta.normalized;
        float skinWidth = controller.skinWidth;

        Vector3 scale = t.lossyScale;
        float radius = controller.radius * Mathf.Max(scale.x, scale.z);
        float halfHeight = Mathf.Max(controller.height * scale.y * 0.5f - radius, 0f);
        Vector3 center = t.TransformPoint(controller.center);
        Vector3 top = center + Vector3.up * halfHeight;
        Vector3 bottom = center - Vector3.up * halfHeight;

        // 캐스트 시작점을 살짝 뒤로 당겨서 이미 벽 스킨 두께 안에 박혀있어 캐스트가 충돌을 못 잡는 경우를 완화한다(완전한 보장은 아님).
        Vector3 pullBack = -direction * skinWidth;
        float castDistance = inputDelta.magnitude + skinWidth;

        bool blocked = false;
        if (Physics.CapsuleCast(top + pullBack, bottom + pullBack, radius, direction, out RaycastHit hit,
                castDistance, groundData.WallLayerMask, QueryTriggerInteraction.Ignore))
        {
            float dot = Vector3.Dot(direction, -hit.normal);
            blocked = dot >= groundData.WallFrontalDotThreshold;
        }

        return blocked;
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
        Vector3 lookDir;

        if (stateMachine.LockedTarget != null && FacesLockedTarget)
        {
            // 락온 중엔 이동 여부와 무관하게 타겟 쪽을 계속 바라봄(스트레이프 이동) — FacesLockedTarget이
            // false인 상태(Run)에서는 이 분기를 건너뛰고 아래 이동 방향 기준 자유 회전으로 폴백
            lookDir = stateMachine.LockedTarget.position - stateMachine.Player.transform.position;
            lookDir.y = 0f;
        }
        else
        {
            lookDir = moveDir;
        }

        if (lookDir.sqrMagnitude > 0.0001f)    // 제로 벡터로 LookRotation 호출 방지
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
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
