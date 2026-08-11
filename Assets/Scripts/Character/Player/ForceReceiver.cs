using UnityEngine;

public class ForceReceiver : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private float drag = 0.3f;

    private Vector3 dampingVelocity;
    private Vector3 impact;
    private float verticalVelocity;
    private bool gravitySuspended;    // 공중공격 중 호버 — 소유자는 PlayerAirAttackState 하나뿐(Design/AirState_Design.md §8-3)

    public Vector3 Movement => impact + Vector3.up * verticalVelocity;

    void Update()
    {
        if (!gravitySuspended)
        {
            if (verticalVelocity < 0f && controller.isGrounded)
            {
                verticalVelocity = Physics.gravity.y * Time.deltaTime;
            }
            else
            {
                verticalVelocity += Physics.gravity.y * Time.deltaTime;
            }
        }

        impact = Vector3.SmoothDamp(impact, Vector3.zero, ref dampingVelocity, drag);
    }

    // 공중공격 모션 재생 중 수직속도를 0으로 고정해 제자리에 띄워둔다. 중첩 호출 없음이 불변조건 — 위반 시 조기 탐지.
    public void SuspendGravity()
    {
        Debug.Assert(!gravitySuspended, "ForceReceiver.SuspendGravity() 중복 호출 — 중력 정지 소유권 위반 의심");
        gravitySuspended = true;
        verticalVelocity = 0f;
    }

    // 멱등 — 이미 재개 상태에서 또 불러도 무해
    public void ResumeGravity()
    {
        gravitySuspended = false;
    }

    public void Reset()
    {
        impact = Vector3.zero;
        verticalVelocity = 0f;
    }

    public void AddForce(Vector3 force)
    {
        impact += force;
    }

    public void Jump(float jumpForce)
    {
        verticalVelocity += jumpForce;
    }
}