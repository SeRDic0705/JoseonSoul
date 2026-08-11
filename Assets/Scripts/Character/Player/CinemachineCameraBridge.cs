using UnityEngine;
using Unity.Cinemachine;

public class CinemachineCameraBridge : MonoBehaviour
{
    [field: SerializeField] public CameraSO Data { get; private set; }

    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private CinemachineRotationComposer rotationComposer;
    [SerializeField] private CinemachineDeoccluder deoccluder;

    [Header("Lock-On")]
    [SerializeField] private CinemachineInputAxisController orbitInputAxis;    // 락온 중엔 배경에서 오빗 축이 안 흐르도록 정지
    [SerializeField] private Player player;
    [SerializeField] private float horizontalDampTime = 0.15f;    // 오빗 수평 각도가 타겟 쪽으로 도는 속도(SmoothDampAngle)

    private CinemachineCamera vcam;
    private Transform defaultLookAt;    // 락온 해제 시 복귀할 원래 LookAt(Head_M)
    private bool wasLocked;
    private bool orbitInputAxisWasEnabled;    // 락온 진입 직전 enabled 상태(무조건 true로 복원하지 않기 위함)
    private float horizontalAxisVelocity;

    private void Awake()
    {
        if (orbitalFollow != null) vcam = orbitalFollow.GetComponent<CinemachineCamera>();
        if (vcam != null) defaultLookAt = vcam.LookAt;
    }

    private void OnEnable()
    {
        Configure();
    }

    private void Update()
    {
        UpdateLockOn();
    }

    // 타겟-플레이어-카메라가 일직선이 되도록 오빗 수평 각도를 매 프레임 보간(Design/LockOn_Design.md §4-1).
    // 신규 vcam 없이 기존 CM_ThirdPersonCamera 하나만 사용 — LookAt만 락온 중엔 LockPoint로 직접 전환.
    private void UpdateLockOn()
    {
        if (player == null || player.LockOn == null || vcam == null || orbitalFollow == null) return;

        Transform target = player.LockOn.CurrentTarget;
        bool locked = target != null;

        if (locked && !wasLocked)
        {
            orbitInputAxisWasEnabled = orbitInputAxis != null && orbitInputAxis.enabled;
            if (orbitInputAxis != null) orbitInputAxis.enabled = false;
            vcam.LookAt = player.LockOn.LockPoint;
        }
        else if (!locked && wasLocked)
        {
            if (orbitInputAxis != null) orbitInputAxis.enabled = orbitInputAxisWasEnabled;
            vcam.LookAt = defaultLookAt;
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

    public void Configure()
    {
        if (Data == null) return;

        if (orbitalFollow != null)
        {
            orbitalFollow.Radius = Data.cameraOffset.magnitude;

            var verticalAxis = orbitalFollow.VerticalAxis;
            verticalAxis.Range = Data.pitchLimits;
            verticalAxis.Center = (Data.pitchLimits.x + Data.pitchLimits.y) * 0.5f;
            orbitalFollow.VerticalAxis = verticalAxis;
        }

        if (rotationComposer != null)
        {
            var composition = rotationComposer.Composition;
            composition.DeadZone.Enabled = true;
            composition.DeadZone.Size = new Vector2(Data.deadZoneRadius * 2f, Data.deadZoneRadius * 2f);
            rotationComposer.Composition = composition;
        }

        if (deoccluder != null)
        {
            var avoid = deoccluder.AvoidObstacles;
            avoid.CameraRadius = Data.cameraRadius;
            deoccluder.AvoidObstacles = avoid;
            deoccluder.CollideAgainst = Data.collisionMask;
            deoccluder.MinimumDistanceFromTarget = Mathf.Max(Data.collisionOffset, 0.01f);
        }
    }
}
