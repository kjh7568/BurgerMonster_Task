using System;

public class DamageResolver
{
    public event Action<Side, int, int> OnDamageDealt;          // side, slotIdx, amount
    public event Action<Side, int, CardInstance> OnCardDied;    // side, slotIdx, dyingCard
    public event Action<Side, int, CardInstance> OnCardSpawned; // side, slotIdx, newCard

    /// <summary>
    /// 지정한 진영의 전장 슬롯 카드에 피해를 적용하고, 사망 시 같은 슬롯에 standby의 다음 카드를 자동 배치한다.
    /// </summary>
    /// <param name="side">피해를 입을 카드가 위치한 진영. 이 진영의 field/standby가 변경될 수 있다.</param>
    /// <param name="slotIdx">피해 대상 카드의 field 슬롯 인덱스(0~2).</param>
    /// <param name="amount">적용할 피해량. 0 이하면 아무 동작도 하지 않는다.</param>
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
