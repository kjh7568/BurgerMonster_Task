using System;

public class DamageResolver
{
    public event Action<Side, int, int> OnDamageDealt;          // side, slotIdx, amount
    public event Action<Side, int, CardInstance> OnCardDied;    // side, slotIdx, dyingCard
    public event Action<Side, int, CardInstance> OnCardSpawned; // side, slotIdx, newCard

    public void DealDamage(Side side, int slotIdx, int amount)
    {
        var card = side.field[slotIdx];
        if (card == null || card.IsDead) return;
        if (amount <= 0) return;

        card.TakeDamage(amount);
        OnDamageDealt?.Invoke(side, slotIdx, amount);

        if (!card.IsDead) return;

        OnCardDied?.Invoke(side, slotIdx, card);
        side.field[slotIdx] = null;

        if (side.standby.Count > 0)
        {
            var next = side.standby.Dequeue();
            side.field[slotIdx] = next;
            OnCardSpawned?.Invoke(side, slotIdx, next);
        }
    }
}
