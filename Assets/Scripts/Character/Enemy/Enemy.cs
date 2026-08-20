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
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        gameObject.SetActive(false);
    }
}
