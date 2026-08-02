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
    [Range(0.5f, 10f)] public float radius = 2.83f;

    [Header("회전 감도 (Look Orbit X/Y Gain, 부호 포함 — Y는 보통 음수)")]
    [Range(-10f, 10f)] public float rotationGainX = 1f;
    [Range(-10f, 10f)] public float rotationGainY = -1f;

    [Header("화면 구도 데드존 / 감쇠")]
    [Range(0f, 1f)] public float deadZoneSize = 0.02f;
    [Range(0f, 5f)] public Vector2 rotationDamping = new Vector2(0.5f, 0.5f);

    [Header("충돌 회피 (Deoccluder)")]
    [Range(0.05f, 2f)] public float cameraRadius = 0.3f;
    [Range(0f, 2f)] public float minimumDistanceFromTarget = 0.3f;
    [Range(0f, 2f)] public float obstacleDampingIn = 0.4f;
    [Range(0f, 2f)] public float obstacleDampingOut = 0.2f;

    [ContextMenu("Pull From Components")]
    public void PullFromComponents()
    {
        if (orbitalFollow != null)
        {
            radius = orbitalFollow.Radius;
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
            deoccluder.MinimumDistanceFromTarget = minimumDistanceFromTarget;
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
