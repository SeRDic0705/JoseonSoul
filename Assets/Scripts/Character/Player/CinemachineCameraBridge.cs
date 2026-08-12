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

    private CinemachineCamera vcam;
    private bool wasLocked;
    private bool orbitInputAxisWasEnabled;    // 락온 진입 직전 enabled 상태(무조건 true로 복원하지 않기 위함)
    private float horizontalAxisVelocity;

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
        }
        else if (!locked && wasLocked)
        {
            if (orbitInputAxis != null) orbitInputAxis.enabled = orbitInputAxisWasEnabled;
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
}
