using UnityEngine;

// 점프/낙하 중에도 카메라 위치(Follow)가 플레이어의 수직 이동을 그대로 따라가지 않도록 분리하는 프록시.
// CinemachineCameraBridge가 FollowPoint를 읽어 vcam.Follow로 사용한다(LookAt은 안 건드림).
public class PlayerCameraFollowTarget : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Transform head;    // Head_M — 초기 카메라 높이 오프셋 실측용
    [SerializeField] private float landingSmoothTime = 0.15f;

    public Transform FollowPoint { get; private set; }

    private float heightOffset;    // head.position.y - player.position.y (Awake 시점 1회 실측, 하드코딩 안 함)
    private float ySmoothVelocity;

    private void Awake()
    {
        FollowPoint = new GameObject("CameraFollowPoint").transform;
        FollowPoint.position = head.position;    // 첫 프레임 순간이동 방지 — 기존 카메라 높이 그대로 복제
        heightOffset = head.position.y - player.transform.position.y;
    }

    private void Update()
    {
        Vector3 pos = FollowPoint.position;
        pos.x = player.transform.position.x;
        pos.z = player.transform.position.z;

        if (player.StateMachine.IsGrounded)
        {
            float groundedY = player.transform.position.y + heightOffset;
            pos.y = Mathf.SmoothDamp(pos.y, groundedY, ref ySmoothVelocity, landingSmoothTime);
        }
        else
        {
            ySmoothVelocity = 0f;    // 공중 진입 시 즉시 동결 + 다음 착지 때 깨끗하게 재보간 시작
        }

        FollowPoint.position = pos;
    }

    private void OnDestroy()
    {
        if (FollowPoint != null) Destroy(FollowPoint.gameObject);
    }
}
