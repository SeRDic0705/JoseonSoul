using System.Collections.Generic;
using UnityEngine;

// 락온(시점 고정) — "Enemy" 태그 대상을 탐색·유지·해제한다. 카메라 쪽 배선(Cinemachine)은
// CinemachineCameraBridge가 이 컴포넌트의 CurrentTarget/LockPoint를 읽어서 처리한다(Design/LockOn_Design.md §3).
public class PlayerLockOn : MonoBehaviour
{
    [SerializeField] private Player player;    // Awake 순서 의존 없이 즉시 참조 가능하도록 인스펙터 자기참조

    public Transform CurrentTarget { get; private set; }
    public Transform LockPoint { get; private set; }    // CurrentTarget.position + up*offset를 매 프레임 따라가는 프록시

    private Transform mainCameraTransform;

    private void Awake()
    {
        mainCameraTransform = Camera.main.transform;

        LockPoint = new GameObject("LockPointProxy").transform;
        LockPoint.gameObject.SetActive(false);
    }

    private void Update()
    {
        PlayerLockOnData data = player.Data.LockOnData;

        if (player.Input.PlayerActions.Lock.WasPerformedThisFrame())
        {
            if (CurrentTarget == null)
                TryLock(data);
            else
                Unlock();
        }

        if (CurrentTarget == null) return;

        if (!CurrentTarget.gameObject.activeInHierarchy)
        {
            Unlock();
            return;
        }

        LockPoint.position = CurrentTarget.position + Vector3.up * data.LockPointHeightOffset;

        float distance = Vector3.Distance(transform.position, CurrentTarget.position);
        if (distance > data.ReleaseRange)
        {
            Unlock();
        }
    }

    private void OnDisable() => Unlock();
    private void OnDestroy() => Unlock();

    private void TryLock(PlayerLockOnData data)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, data.LockRange, data.LockableLayer, QueryTriggerInteraction.Ignore);

        HashSet<Transform> candidates = new HashSet<Transform>();
        Transform best = null;
        float bestAngle = float.MaxValue;
        float bestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            // 콜라이더에서 부모로 올라가며 "Enemy" 태그를 가진 가장 가까운 조상을 논리적 루트로 판정
            // (attachedRigidbody는 래그돌/다중 Rigidbody에서 깨지고, transform.root는 "Enemies" 같은
            // 공통 부모 아래 여러 캐릭터가 있으면 전부 같은 루트로 뭉개짐 — 태그 자체를 경계로 삼아 회피)
            Transform root = FindEnemyRoot(hit.transform);
            if (root == null || !candidates.Add(root)) continue;    // 태그 조상 없음 또는 같은 대상의 중복 콜라이더

            float angle = Vector3.Angle(mainCameraTransform.forward, root.position - mainCameraTransform.position);
            if (angle > data.LockAngle) continue;

            float distance = Vector3.Distance(transform.position, root.position);
            bool better = angle < bestAngle - 0.01f || (Mathf.Abs(angle - bestAngle) <= 0.01f && distance < bestDistance);
            if (!better) continue;

            best = root;
            bestAngle = angle;
            bestDistance = distance;
        }

        if (best == null) return;

        CurrentTarget = best;
        LockPoint.gameObject.SetActive(true);
        LockPoint.position = CurrentTarget.position + Vector3.up * data.LockPointHeightOffset;
    }

    private static Transform FindEnemyRoot(Transform from)
    {
        for (Transform current = from; current != null; current = current.parent)
        {
            if (current.CompareTag("Enemy")) return current;
        }
        return null;
    }

    private void Unlock()
    {
        CurrentTarget = null;
        if (LockPoint != null) LockPoint.gameObject.SetActive(false);
    }
}
