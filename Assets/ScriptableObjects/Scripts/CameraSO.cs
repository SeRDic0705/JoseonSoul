using UnityEngine;

[CreateAssetMenu(fileName = "Camera", menuName = "Camera")]
public class CameraSO : ScriptableObject
{
    [Header("Camera Control")]
    [SerializeField] public Vector3 cameraOffset = new Vector3(0f, 2f, -2f); // 대상 기준 카메라 위치 오프셋
    [field: SerializeField][field: Range(0f, 3f)] public float RotationSpeed { get; private set;} = 0.01f; // 카메라 회전 속도
    [field: SerializeField][field: Range(0f, 100f)] public float xSensitivity { get; private set;} = 50f; // x축 감도
    [field: SerializeField][field: Range(0f, 100f)] public float ySensitivity { get; private set;} = 50f; // x축 감도
    [SerializeField] public Vector2 pitchLimits = new Vector2(-80f, 30f); // 상하(pitch) 회전 제한


    [Header("Deadzone Settings")]
    [SerializeField][field: Range(0f, 1f)] public float deadZoneRadius = 0.2f; // 데드존 크기 (0~1 사이, 화면비율)
    [SerializeField][field: Range(0f, 10f)] public float followSpeed = 5f;       // 소프트리밋 부드럽게 따라오는 속도


    [Header("Collision Settings")]
    [SerializeField] public LayerMask collisionMask; // 충돌 체크할 레이어
    [SerializeField][Range(0.01f, 1f)] public float cameraRadius = 0.3f; // 스피어캐스트 반지름
    [SerializeField][Range(0f, 1f)] public float collisionOffset = 0.2f; // 벽에 너무 딱 붙지 않게 최소 거리 유지
    [SerializeField][Range(0f, 20f)] public float cameraAdjustSpeed = 10f; // 카메라 이동 보간 속도
}
