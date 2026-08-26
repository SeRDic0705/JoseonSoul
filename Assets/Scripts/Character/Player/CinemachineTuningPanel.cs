using UnityEngine;
using Unity.Cinemachine;

// 비교/튜닝용으로 여러 Cinemachine 컴포넌트에 흩어진 값을 한 군데(이 컴포넌트)에서 조정.
// 인스펙터에서 값을 바꾸면 OnValidate가 즉시 실제 컴포넌트에 반영한다(플레이모드 중에도 동작).
public class CinemachineTuningPanel : MonoBehaviour
{
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private CinemachineRotationComposer rotationComposer;
    [SerializeField] private CinemachineDeoccluder deoccluder;
    [SerializeField] private CinemachineInputAxisController inputAxisController;

    [Header("오빗 거리")]
    [Tooltip("캐릭터-카메라 거리. 늘리면 줌아웃(멀어짐), 줄이면 줌인(가까워짐).")]
    [Range(0.5f, 10f)] public float radius = 2.83f;

    [Header("회전 감도 (Look Orbit X/Y Gain, 부호 포함 — Y는 보통 음수)")]
    [Tooltip("좌우 회전 감도. 절대값이 클수록 같은 입력에 더 빨리 돈다. 부호가 바뀌면 회전 방향이 반전된다.")]
    [Range(-10f, 10f)] public float rotationGainX = 1f;
    [Tooltip("상하 회전 감도. 절대값이 클수록 더 빨리 돈다. 보통 음수(입력 반전)로 둔다.")]
    [Range(-10f, 10f)] public float rotationGainY = -1f;

    [Header("화면 구도 데드존 / 감쇠")]
    [Tooltip("화면 중앙 기준 데드존 크기. 타겟이 이 범위 안에 있으면 카메라가 반응하지 않는다. 값이 클수록 안 움직이는 범위가 넓어진다.")]
    [Range(0f, 1f)] public float deadZoneSize = 0.02f;
    [Tooltip("타겟이 데드존을 벗어났을 때 카메라가 따라잡는 보간 속도(x=수평, y=수직). 값이 클수록 천천히/부드럽게 따라가고, 0에 가까울수록 즉각 반응한다.")]
    public Vector2 rotationDamping = new Vector2(0.5f, 0.5f);

    [Header("상하 회전 제한 (Vertical Axis)")]
    [Tooltip("카메라 상하 회전 각도 제한(도 단위, x=최소/y=최대). 범위를 좁히면 위아래로 덜 움직인다.")]
    public Vector2 pitchLimits = new Vector2(-100f, 30f);

    [Header("충돌 회피 (Deoccluder)")]
    [Tooltip("충돌 감지용 스피어캐스트 반지름. 값이 클수록 더 두꺼운 장애물도 미리 감지해서 회피한다.")]
    [Range(0.05f, 2f)] public float cameraRadius = 0.3f;
    [Tooltip("장애물 회피 시 타겟과 카메라 사이 최소 거리. 값이 작을수록 카메라가 벽/장애물에 더 바짝 붙을 수 있다.")]
    [Min(0.01f)] public float minimumDistanceFromTarget = 0.1f;
    [Tooltip("장애물이 감지돼 회피에 들어갈 때의 보간 속도. 값이 클수록 회피 반응이 느려지고 부드러워진다.")]
    [Range(0f, 2f)] public float obstacleDampingIn = 0.4f;
    [Tooltip("장애물에서 벗어나 원래 위치로 복귀할 때의 보간 속도. 값이 클수록 천천히 복귀한다.")]
    [Range(0f, 2f)] public float obstacleDampingOut = 0.2f;
    [Tooltip("충돌 감지 대상 레이어. 체크된 레이어만 카메라 충돌 회피 계산에 포함된다.")]
    public LayerMask collisionMask = 64;

    [ContextMenu("Pull From Components")]
    public void PullFromComponents()
    {
        if (orbitalFollow != null)
        {
            radius = orbitalFollow.Radius;
            var verticalAxis = orbitalFollow.VerticalAxis;
            pitchLimits = verticalAxis.Range;
        }

        if (rotationComposer != null)
        {
            deadZoneSize = rotationComposer.Composition.DeadZone.Size.x;
            rotationDamping = rotationComposer.Damping;
        }

        if (deoccluder != null)
        {
            cameraRadius = deoccluder.AvoidObstacles.CameraRadius;
            minimumDistanceFromTarget = deoccluder.MinimumDistanceFromTarget;
            obstacleDampingIn = deoccluder.AvoidObstacles.Damping;
            obstacleDampingOut = deoccluder.AvoidObstacles.DampingWhenOccluded;
            collisionMask = deoccluder.CollideAgainst;
        }

        if (inputAxisController != null)
        {
            for (int i = 0; i < inputAxisController.Controllers.Count; i++)
            {
                var c = inputAxisController.Controllers[i];
                if (c.Name == "Look Orbit X") rotationGainX = c.Input.Gain;
                else if (c.Name == "Look Orbit Y") rotationGainY = c.Input.Gain;
            }
        }
    }

    private void OnValidate()
    {
        Apply();
    }

    [ContextMenu("Push To Components")]
    public void Apply()
    {
        if (orbitalFollow != null)
        {
            orbitalFollow.Radius = radius;
            var verticalAxis = orbitalFollow.VerticalAxis;
            verticalAxis.Range = pitchLimits;
            verticalAxis.Center = (pitchLimits.x + pitchLimits.y) * 0.5f;
            orbitalFollow.VerticalAxis = verticalAxis;
        }

        if (rotationComposer != null)
        {
            var composition = rotationComposer.Composition;
            composition.DeadZone.Enabled = true;
            composition.DeadZone.Size = new Vector2(deadZoneSize, deadZoneSize);
            rotationComposer.Composition = composition;
            rotationComposer.Damping = rotationDamping;
        }

        if (deoccluder != null)
        {
            var avoid = deoccluder.AvoidObstacles;
            avoid.CameraRadius = cameraRadius;
            avoid.Damping = obstacleDampingIn;
            avoid.DampingWhenOccluded = obstacleDampingOut;
            deoccluder.AvoidObstacles = avoid;
            deoccluder.MinimumDistanceFromTarget = Mathf.Max(minimumDistanceFromTarget, 0.01f);
            deoccluder.CollideAgainst = collisionMask;
        }

        if (inputAxisController != null)
        {
            for (int i = 0; i < inputAxisController.Controllers.Count; i++)
            {
                var c = inputAxisController.Controllers[i];
                if (c.Name == "Look Orbit X")
                {
                    c.Input.Gain = rotationGainX;
                    inputAxisController.Controllers[i] = c;
                }
                else if (c.Name == "Look Orbit Y")
                {
                    c.Input.Gain = rotationGainY;
                    inputAxisController.Controllers[i] = c;
                }
            }
        }
    }
}
