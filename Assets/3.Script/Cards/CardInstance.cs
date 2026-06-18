using UnityEngine;

public class CardInstance
{
    public readonly CardDataSO data;
    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    public CardInstance(CardDataSO data)
    {
        this.data = data;
        CurrentHP = data.baseHP;
    }

    public void TakeDamage(int amount) => CurrentHP = Mathf.Max(0, CurrentHP - amount);
    public void Heal(int amount) => CurrentHP = Mathf.Min(data.baseHP, CurrentHP + amount);
}
