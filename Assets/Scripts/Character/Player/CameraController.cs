using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [field: SerializeField] public CameraSO Data { get; private set; }

    [Header("Target Settings")]
    [SerializeField] private Transform target; // 카메라가 바라볼 대상 (Player 머리 위 CameraTarget)

    private Vector3 currentTargetPosition;


    private InputActions inputActions; // 카메라용 입력 액션 인스턴스
    private InputAction lookAction; // 마우스/패드 Look 입력

    private float yaw;   // 좌우 회전량
    private float pitch; // 상하 회전량

    private float currentDistance;  // 현재 카메라 거리

    private void Awake()
    {
        // target이 연결 안 되어있으면 경고 출력하고 비활성화
        if (target == null)
        {
            Debug.LogError("[CameraController] Target이 없습니다!");
            enabled = false;
            return;
        }

        inputActions = new InputActions(); // 입력 액션 생성
        lookAction = inputActions.Player.Look; // Look 액션 가져오기

        currentTargetPosition = target.position;
    }

    private void OnEnable()
    {
        inputActions.Enable(); // 입력 활성화
    }

    private void OnDisable()
    {
        inputActions.Disable(); // 입력 비활성화
    }

    private void Update()
    {
        HandleInput(); // 입력 처리 (회전값 계산)
    }

    private void LateUpdate()
    {
        HandleTargetFollowing(); // 타겟 따라가기 먼저
        HandleCamera(); // 카메라 위치 및 방향 업데이트
    }

    // 입력값을 받아서 yaw, pitch 업데이트
    private void HandleInput()
    {
        Vector2 lookDelta = lookAction.ReadValue<Vector2>(); // 마우스 이동량 읽기

        yaw += lookDelta.x * Data.RotationSpeed * Data.xSensitivity;   // 좌우(yaw) 회전
        pitch -= lookDelta.y * Data.RotationSpeed * Data.ySensitivity; // 상하(pitch) 회전 (마우스 y축 반전)

        // pitch(상하) 회전 각도를 제한
        pitch = Mathf.Clamp(pitch, Data.pitchLimits.x, Data.pitchLimits.y);
    }

    private void HandleTargetFollowing()
    {
        // 화면에서 타겟이 어디 있는지 구하기
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(target.position);

        // (0.5, 0.5) = 화면 정중앙
        Vector2 offsetFromCenter = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);

        // 중심에서 얼마나 벗어났는지 거리
        float distanceFromCenter = offsetFromCenter.magnitude;

        if (distanceFromCenter > Data.deadZoneRadius)
        {
            // 데드존을 벗어났으면 타겟 위치를 부드럽게 따라간다
            currentTargetPosition = Vector3.Lerp(currentTargetPosition, target.position, Time.deltaTime * Data.followSpeed);
        }
        // else: 벗어나지 않으면 currentTargetPosition 그대로 유지
    }


    /*
    // 카메라 위치와 회전 적용
    private void HandleCamera()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f); // pitch, yaw 기반 회전 생성
        Vector3 targetPosition = currentTargetPosition + rotation * Data.cameraOffset; // 회전된 offset 위치 계산

        transform.position = targetPosition; // 카메라 위치 이동
        transform.LookAt(currentTargetPosition);   // 카메라가 target 바라보게 설정
    }
    */
    private void HandleCamera()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredCameraPos = currentTargetPosition + rotation * Data.cameraOffset;

        // 충돌이 있으면 충돌 지점으로 당긴다
        Vector3 finalCameraPos = AdjustCameraCollision(currentTargetPosition, desiredCameraPos);

        transform.position = finalCameraPos;
        transform.LookAt(currentTargetPosition);
    }

    // 충돌 감지 및 카메라 위치 조정
    private Vector3 AdjustCameraCollision(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;
        float maxDistance = Vector3.Distance(from, to); // 원래 떨어져야 할 거리

        // SphereCast를 사용해 충돌 감지
        if (Physics.SphereCast(from, Data.cameraRadius, direction, out RaycastHit hit, maxDistance, Data.collisionMask))
        {
            // 충돌 지점 바로 앞까지 카메라를 당긴다
            return from + direction * (hit.distance - Data.collisionOffset);
        }


        // 충돌 없으면 원래 위치 반환
        return to;
    }
}