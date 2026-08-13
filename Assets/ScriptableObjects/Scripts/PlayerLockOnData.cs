using System;
using UnityEngine;

[Serializable]
public class PlayerLockOnData
{
    [field: Header("Search")]
    [field: SerializeField][field: Range(0f, 30f)] public float LockRange { get; private set; } = 15f;
    [field: SerializeField][field: Range(0f, 180f)] public float LockAngle { get; private set; } = 70f;    // 카메라 정면 기준 허용 각도(180 미만 — 후방 후보 자동 배제)
    [field: SerializeField] public LayerMask LockableLayer { get; private set; } = ~0;    // 전용 Enemy 레이어 생기면 좁힐 것

    [field: Header("Release")]
    [field: SerializeField][field: Range(0f, 40f)] public float ReleaseRange { get; private set; } = 18f;    // LockRange보다 커야 경계에서 반복 해제 방지

    [field: Header("Aim Point")]
    // 대표 Collider(직립 Capsule 등, 월드축 AABB 기준) 높이에 곱해 조준점 Y를 정한다 — bounds.min.y가
    // 기준(0)이고 bounds.max.y가 1. 인스펙터 [Range]는 입력값만 제한하므로 계산부에서 Clamp01도 적용한다.
    [field: SerializeField][field: Range(0f, 1f)] public float LockPointHeightRatio { get; private set; } = 0.667f;
}
