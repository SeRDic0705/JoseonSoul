using UnityEngine;

public class Enemy : MonoBehaviour
{
    [field: SerializeField] public EnemySO Data { get; private set; }

    public int CurrentHealth { get; private set; }

    private void Awake()
    {
        CurrentHealth = Data.MaxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth -= damage;
        Debug.Log($"[Enemy] {Data.DisplayName} 피격: -{damage} (남은 체력 {Mathf.Max(CurrentHealth, 0)}/{Data.MaxHealth})");

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"[Enemy] {Data.DisplayName} 사망");
        gameObject.SetActive(false);
    }
}
