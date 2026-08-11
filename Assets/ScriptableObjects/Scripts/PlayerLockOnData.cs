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
    [field: SerializeField][field: Range(0f, 3f)] public float LockPointHeightOffset { get; private set; } = 1f;
}
