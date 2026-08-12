using UnityEngine;
using Unity.Cinemachine;

public class CinemachineCameraBridge : MonoBehaviour
{
    [Tooltip("오빗 위치(Body) 계산에 쓰는 CinemachineOrbitalFollow. 락온 중 수평 각도를 이 컴포넌트의 HorizontalAxis에 직접 써서 정렬한다.")]
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;

    [Header("Lock-On")]
    [Tooltip("자유 시점 입력(마우스/스틱). 락온 중엔 비활성화해서 배경에서 오빗 축이 안 흐르도록 막고, 해제 시 원래 상태로 복원한다.")]
    [SerializeField] private CinemachineInputAxisController orbitInputAxis;
    [Tooltip("락온 대상/카메라 Follow 지점 조회에 쓰는 플레이어 참조.")]
    [SerializeField] private Player player;
    [Tooltip("락온 중 오빗 수평 각도가 목표각(타겟-플레이어-카메라 정렬)을 따라잡는 속도(SmoothDampAngle의 smoothTime, 초 단위). 값이 작을수록 빠르게 스냅, 클수록 천천히 부드럽게 돈다.")]
    [SerializeField] private float horizontalDampTime = 0.15f;
    [Tooltip("락온 중 카메라가 '이상적인 위치'(오빗 각도 정렬 결과)를 따라잡는 위치 감쇠(TrackerSettings.PositionDamping 대체값). 0에 가까울수록 즉시 스냅, 클수록 천천히 부드럽게 따라간다.")]
    [SerializeField] private Vector3 lockedPositionDamping = new Vector3(0.1f, 0.1f, 0.1f);
    [Tooltip("락온 중 카메라 조준(Aim)이 따라잡는 회전 감쇠(TrackerSettings.RotationDamping 대체값). 0에 가까울수록 즉시 스냅, 클수록 천천히 부드럽게 따라간다.")]
    [SerializeField] private Vector3 lockedRotationDamping = new Vector3(0.1f, 0.1f, 0.1f);

    private CinemachineCamera vcam;
    private bool wasLocked;
    private bool orbitInputAxisWasEnabled;    // 락온 진입 직전 enabled 상태(무조건 true로 복원하지 않기 위함)
    private float horizontalAxisVelocity;
    private Vector3 savedPositionDamping;    // 락온 해제 시 복원할 원래 TrackerSettings 감쇠값
    private Vector3 savedRotationDamping;

    private void Awake()
    {
        if (orbitalFollow != null) vcam = orbitalFollow.GetComponent<CinemachineCamera>();
    }

    private void Start()
    {
        // player.CameraFollowTarget.FollowPoint는 그쪽 Awake()에서 생성되므로, 모든 Awake()가
        // 끝난 뒤 실행이 보장되는 Start()에서 배선(실행 순서 의존 없이 안전).
        if (vcam != null && player != null && player.CameraFollowTarget != null)
            vcam.Follow = player.CameraFollowTarget.FollowPoint;
    }

    private void Update()
    {
        UpdateLockOn();
    }

    // 타겟-플레이어-카메라가 일직선이 되도록 오빗 수평 각도를 매 프레임 보간(Design/LockOn_Design.md §4-1).
    // 신규 vcam 없이 기존 CM_ThirdPersonCamera 하나만 사용 — LookAt은 락온 여부와 무관하게 원래 대상(플레이어) 유지.
    private void UpdateLockOn()
    {
        if (player == null || player.LockOn == null || vcam == null || orbitalFollow == null) return;

        Transform target = player.LockOn.CurrentTarget;
        bool locked = target != null;

        if (locked && !wasLocked)
        {
            orbitInputAxisWasEnabled = orbitInputAxis != null && orbitInputAxis.enabled;
            if (orbitInputAxis != null) orbitInputAxis.enabled = false;

            // TrackerSettings의 Position/RotationDamping이 우리가 맞춰둔 오빗 각도 위에 한 번 더 지연을
            // 걸어서, 플레이어가 움직이는 동안엔 정렬이 계속 뒤처지는 원인이었다 — 락온 중엔 튜닝 가능한 값으로 대체.
            var tracker = orbitalFollow.TrackerSettings;
            savedPositionDamping = tracker.PositionDamping;
            savedRotationDamping = tracker.RotationDamping;
        }
        else if (!locked && wasLocked)
        {
            if (orbitInputAxis != null) orbitInputAxis.enabled = orbitInputAxisWasEnabled;

            var tracker = orbitalFollow.TrackerSettings;
            tracker.PositionDamping = savedPositionDamping;
            tracker.RotationDamping = savedRotationDamping;
            orbitalFollow.TrackerSettings = tracker;
        }

        if (locked)
        {
            // 플레이 중 lockedPositionDamping/lockedRotationDamping을 바꿔도 즉시 반영되도록 매 프레임 동기화.
            var tracker = orbitalFollow.TrackerSettings;
            tracker.PositionDamping = lockedPositionDamping;
            tracker.RotationDamping = lockedRotationDamping;
            orbitalFollow.TrackerSettings = tracker;
        }

        wasLocked = locked;

        if (!locked) return;

        Vector3 toTarget = target.position - player.transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;    // 플레이어-타겟 XZ 거리 0 근접 — 이번 프레임 각도 갱신 스킵(이전 각도 유지)

        // BindingMode=WorldSpace(§4-1 실측)이라 플레이어 회전과 무관하게 성립하는 월드 yaw 공식
        float targetAngle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

        orbitalFollow.HorizontalAxis.Value = Mathf.SmoothDampAngle(
            orbitalFollow.HorizontalAxis.Value, targetAngle, ref horizontalAxisVelocity, horizontalDampTime);
        // Vertical은 이번 범위에서 고정 유지(건드리지 않음)
    }

    // 플레이어 이동/회전용 평면 방향 basis. Camera.main.transform.forward는 CinemachineDeoccluder가 벽 회피로
    // 카메라 위치를 보정할 때 함께 흔들려서(Design 문서 미작성, 2026-08-12 Discord 진단 참조 — 벽 근처에서
    // 이동 방향이 최대 반바퀴 가까이 진동하는 피드백 루프의 원인이었음) 이동 방향 계산에 부적합하다.
    // 대신 Deoccluder의 영향을 받지 않는 HorizontalAxis.Value(오빗 각도)에서 직접 유도한다.
    // BindingMode=WorldSpace(§4-1 실측, UpdateLockOn()의 Atan2 공식과 정확히 역연산 관계) 전제 — 다른
    // BindingMode로 바뀌면 이 공식도 함께 갱신해야 한다.
    public (Vector3 forward, Vector3 right) GetPlanarMoveBasis()
    {
        if (orbitalFollow == null) return (Vector3.forward, Vector3.right);

        Quaternion yawRot = Quaternion.Euler(0f, orbitalFollow.HorizontalAxis.Value, 0f);
        return (yawRot * Vector3.forward, yawRot * Vector3.right);
    }
}
