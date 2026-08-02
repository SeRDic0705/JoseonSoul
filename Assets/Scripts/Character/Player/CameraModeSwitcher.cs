using UnityEngine;
using Unity.Cinemachine;

public class CameraModeSwitcher : MonoBehaviour
{
    [SerializeField] private CameraController legacyController;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private GameObject cmRig;
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private CinemachineCameraBridge bridge;

    [SerializeField] private bool useCinemachine;

    private void Start()
    {
        Apply(useCinemachine);
    }

    [ContextMenu("Switch To Cinemachine")]
    public void SwitchToCinemachine()
    {
        SyncAxesFromLegacy();
        Apply(true);
    }

    [ContextMenu("Switch To Legacy")]
    public void SwitchToLegacy()
    {
        Apply(false);
    }

    private void SyncAxesFromLegacy()
    {
        if (legacyController == null || orbitalFollow == null) return;

        var horizontalAxis = orbitalFollow.HorizontalAxis;
        horizontalAxis.Value = legacyController.Yaw;
        orbitalFollow.HorizontalAxis = horizontalAxis;

        var verticalAxis = orbitalFollow.VerticalAxis;
        verticalAxis.Value = legacyController.Pitch;
        orbitalFollow.VerticalAxis = verticalAxis;
    }

    private void Apply(bool cinemachineOn)
    {
        if (bridge != null && cinemachineOn)
        {
            bridge.Configure();
        }

        if (legacyController != null) legacyController.enabled = !cinemachineOn;
        if (brain != null) brain.enabled = cinemachineOn;
        if (cmRig != null) cmRig.SetActive(cinemachineOn);

        useCinemachine = cinemachineOn;
    }
}
