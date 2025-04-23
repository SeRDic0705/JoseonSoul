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

    // TODO: avoid

}
