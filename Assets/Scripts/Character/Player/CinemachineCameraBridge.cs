using UnityEngine;
using Unity.Cinemachine;

public class CinemachineCameraBridge : MonoBehaviour
{
    [field: SerializeField] public CameraSO Data { get; private set; }

    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private CinemachineRotationComposer rotationComposer;
    [SerializeField] private CinemachineDeoccluder deoccluder;

    private void OnEnable()
    {
        Configure();
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
