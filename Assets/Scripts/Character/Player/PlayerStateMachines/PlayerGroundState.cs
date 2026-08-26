using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGroundState : PlayerBaseState
{
    protected float lastMoveInputTime;
    protected float moveInputGracePeriod = 0.2f; // 입력 유예 시간

    // CharacterController.isGrounded가 평지에서도 프레임 단위로 흔들릴 수 있어(실사용 중 확인됨),
    // 순간적으로 !isGrounded가 잡혀도 곧바로 Fall로 보내지 않고 이 시간만큼 지속돼야 진짜 낙하로 인정한다.
    private const float notGroundedGracePeriod = 0.15f;
    private float notGroundedTimer;

    public PlayerGroundState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        notGroundedTimer = 0f;
        StartAnim(stateMachine.Player.AnimationData.GroundParameterHash);
    }

    public override void Exit()
    {
        base.Exit();
        StopAnim(stateMachine.Player.AnimationData.GroundParameterHash);
    }

    public override void Update()
    {
        // 벼랑 이탈(점프 없이 낙하 시작) — 착지/벼랑 이탈이 공격·점프 입력보다 우선이라 base.Update() 전에 체크
        if (!stateMachine.Player.Controller.isGrounded && stateMachine.Player.ForceReceiver.Movement.y <= 0f)
        {
            notGroundedTimer += Time.deltaTime;
            if (notGroundedTimer >= notGroundedGracePeriod)
            {
                stateMachine.AirborneFacesLockedTarget = FacesLockedTarget;    // 지상->공중 경계 스냅샷
                stateMachine.ChangeState(stateMachine.FallState);
                return;
            }
        }
        else
        {
            notGroundedTimer = 0f;
        }

        UpdateLockedOnAnim();

        base.Update();
    }

    // 락온 이동 애니메이션(WalkLockedOn 블렌드트리)용 파라미터 갱신 — Idle에서도 최신값이어야
    // Idle→WalkLockedOn 직결 트랜지션 조건이 정확히 평가된다(PlayerWalkState 안에만 두면 stale해짐).
    private void UpdateLockedOnAnim()
    {
        var animator = stateMachine.Player.Animator;
        var animData = stateMachine.Player.AnimationData;

        bool isLockedOn = stateMachine.LockedTarget != null;
        animator.SetBool(animData.LockedOnParameterHash, isLockedOn);

        Vector3 localMoveDir = stateMachine.Player.transform.InverseTransformDirection(GetMoveDir());
        animator.SetFloat(animData.MoveXParameterHash, localMoveDir.x);
        animator.SetFloat(animData.MoveZParameterHash, localMoveDir.z);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    protected override void OnMoveCanceled(InputAction.CallbackContext context)
    {
        base.OnMoveCanceled(context);
        lastMoveInputTime = Time.time;

        // 일정 시간 안에 다시 입력이 들어오면 Idle로 전이하지 않음
        stateMachine.Player.StartCoroutine(DelayedIdleCheck());

    }

    protected virtual void OnMove()
    {
        stateMachine.ChangeState(stateMachine.WalkState);
    }

    protected override void OnAttack()
    {
        stateMachine.ComboIndex = 0;    // 체인 재진입이 아닌 새 진입 — 이전 상태 Exit() 정리에만 의존하지 않음(Design/AirState_Design.md §8-4)
        stateMachine.ChangeState(stateMachine.ComboAttackState);
    }

    protected override void OnJump()
    {
        stateMachine.AirborneFacesLockedTarget = FacesLockedTarget;    // 지상->공중 경계 스냅샷
        stateMachine.ChangeState(stateMachine.JumpState);
    }

    private IEnumerator DelayedIdleCheck()
    {
        yield return new WaitForSeconds(moveInputGracePeriod);

        // 대기하는 동안 다른 상태로 이미 전이됐으면(점프/공격 등) 강제로 Idle을 덮어쓰지 않는다.
        if (stateMachine.CurrentState != this) yield break;

        if (stateMachine.MoveInput == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.IdleState);
        }
    }

}
