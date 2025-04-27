using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [field: SerializeField] public CameraSO Data { get; private set; }

    [Header("Target Settings")]
    [SerializeField] private Transform target; // 카메라가 바라볼 대상 (Player 머리 위 CameraTarget)


    private InputActions inputActions; // 카메라용 입력 액션 인스턴스
    private InputAction lookAction; // 마우스/패드 Look 입력

    private float yaw;   // 좌우 회전량
    private float pitch; // 상하 회전량

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

    // 카메라 위치와 회전 적용
    private void HandleCamera()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f); // pitch, yaw 기반 회전 생성
        Vector3 targetPosition = target.position + rotation * Data.cameraOffset; // 회전된 offset 위치 계산

        transform.position = targetPosition; // 카메라 위치 이동
        transform.LookAt(target.position);   // 카메라가 target 바라보게 설정
    }
}