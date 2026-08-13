using System;
using UnityEngine;

[Serializable]
public class PlayerGroundData
{
    [field: SerializeField][field: Range(0f, 25f)] public float BaseSpeed { get; private set;} = 5f;
    [field: SerializeField][field: Range(0f, 25f)] public float BaseRotationDamping { get; private set;} = 5f;

    [field: Header("IdleData")]

    [field: Header("WalkData")]
    [field: SerializeField][field: Range(0f, 2f)] public float WalkSpeed { get; private set; } = 1f;

    [field: Header("AvoidData")]

    [field: SerializeField][field: Range(0f, 3f)] public float avoidSpeed { get; private set; } = 2.5f;
    [field: SerializeField][field: Range(0f, 1f)] public float avoid2runTransitionTime { get; private set; } = 0.5f;

    [field: Header("RunData")]
    [field: SerializeField][field: Range(0f, 2f)] public float RunSpeed { get; private set; } = 2f;

    [field: Header("WallCollisionData")]
    [field: SerializeField] public LayerMask WallLayerMask { get; private set; } = 64; // Wall 레이어(비트 6) — CinemachineTuningPanel.collisionMask와 동일 레이어 재사용
    [field: SerializeField][field: Range(0.5f, 1f)] public float WallFrontalDotThreshold { get; private set; } = 0.95f; // 이 값 이상이면 "정면 충돌"로 보고 이동을 완전히 정지(투영 대신) — 값이 클수록 더 정면에 가까워야 정지

    // TODO: avoid

}
