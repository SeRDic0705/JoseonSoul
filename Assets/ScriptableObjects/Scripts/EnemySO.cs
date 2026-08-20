using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Characters/Enemy")]
public class EnemySO : ScriptableObject
{
    [field: SerializeField] public string DisplayName { get; private set; }
    [field: SerializeField] public int MaxHealth { get; private set; }
}
