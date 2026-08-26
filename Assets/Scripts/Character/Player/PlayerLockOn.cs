using System.Collections.Generic;
using UnityEngine;

// 락온(시점 고정) — "Enemy" 태그 대상을 탐색·유지·해제한다. 카메라 쪽 배선(Cinemachine)은
// CinemachineCameraBridge가 이 컴포넌트의 CurrentTarget/LockPoint를 읽어서 처리한다(Design/LockOn_Design.md §3).
public class PlayerLockOn : MonoBehaviour
{
    [SerializeField] private Player player;    // Awake 순서 의존 없이 즉시 참조 가능하도록 인스펙터 자기참조

    public Transform CurrentTarget { get; private set; }
    public Transform LockPoint { get; private set; }    // 타겟의 실제 콜라이더 높이 비례 지점을 매 프레임 따라가는 프록시

    private Transform mainCameraTransform;
    private Collider lockedBodyCollider;    // TryLock에서 실제로 탐지된 Collider를 그대로 캐싱(루트에 별도로 없어도 됨)

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

        RecalculateLockPoint();

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
        Collider bestCollider = null;
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
            bestCollider = hit;    // 이 루트를 실제로 탐지시킨 Collider — 조준점 높이 계산의 대표 Collider로 그대로 재사용
            bestAngle = angle;
            bestDistance = distance;
        }

        if (best == null) return;

        CurrentTarget = best;
        lockedBodyCollider = bestCollider;
        LockPoint.gameObject.SetActive(true);
        RecalculateLockPoint();
    }

    // 조준점 Y를 대표 Collider의 실제 월드 바운즈(min~max) 비례 지점으로 계산한다(Design 미문서화,
    // 2026-08-13 Discord 합의 — 적마다 키가 달라도 고정 오프셋 대신 실측 높이를 따라가게 하기 위함).
    // 대표 Collider는 TryLock에서 실제로 탐지된 그 Collider를 그대로 쓰므로(루트에 별도로 Collider가
    // 있어야 한다는 전제 없음) "Enemy 루트에는 몸통을 대표하는 Collider 하나만 붙인다"는 프로젝트
    // 규약에 의존한다 — 다중 파츠(래그돌 등) 적이 추가되면 대표 Collider 선택 기준을 다시 설계할 것.
    // Collider.bounds는 월드축 AABB라 대표 Collider가 기울면 높이가 왜곡되므로 직립 Capsule류 전제.
    private void RecalculateLockPoint()
    {
        if (lockedBodyCollider == null || !lockedBodyCollider.enabled || !lockedBodyCollider.gameObject.activeInHierarchy)
        {
            LockPoint.position = CurrentTarget.position;    // 대표 Collider를 못 쓰면 Y 보정 없이 루트 위치로 폴백
            return;
        }

        Bounds bounds = lockedBodyCollider.bounds;
        float ratio = Mathf.Clamp01(player.Data.LockOnData.LockPointHeightRatio);
        float y = bounds.min.y + bounds.size.y * ratio;
        LockPoint.position = new Vector3(CurrentTarget.position.x, y, CurrentTarget.position.z);
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
        lockedBodyCollider = null;
        if (LockPoint != null) LockPoint.gameObject.SetActive(false);
    }
}
