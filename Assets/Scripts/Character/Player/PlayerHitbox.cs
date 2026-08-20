using System.Collections.Generic;
using UnityEngine;

// 무기(Hwando) 루트에 부착하는 트리거 콜라이더. Activate/Deactivate 구간에서만 Collider.enabled를 켜서
// 판정 구간 시작 시점부터 겹쳐있는 대상도 다음 물리 스텝에 정상적으로 OnTriggerEnter를 받는다.
public class PlayerHitbox : MonoBehaviour
{
    [SerializeField] private Collider hitCollider;

    private int pendingDamage;
    private readonly HashSet<Enemy> hitThisWindow = new();

    private void Awake()
    {
        if (hitCollider == null) hitCollider = GetComponent<Collider>();
        hitCollider.enabled = false;
    }

    public void Activate(int damage)
    {
        pendingDamage = damage;
        hitThisWindow.Clear();
        hitCollider.enabled = true;
    }

    public void Deactivate()
    {
        hitCollider.enabled = false;
    }

    private void OnDisable()
    {
        hitCollider.enabled = false;
        hitThisWindow.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;
        if (!hitThisWindow.Add(enemy)) return;

        enemy.TakeDamage(pendingDamage);
    }
}
