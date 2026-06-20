using System;

public class DamageResolver
{
    public event Action<Side, int, int> OnDamageDealt;                  // side, slotIdx, amount
    public event Action<Side, int, int> OnHealApplied;                  // side, slotIdx, amount
    public event Action<Side, int, CardInstance> OnCardDied;            // side, slotIdx, dyingCard
    public event Action<Side, int, CardInstance, int> OnCardSpawned;    // side, slotIdx, newCard, fromStandbyIdx

    /// <summary>
    /// 지정한 진영의 전장 슬롯 카드에 피해를 적용하고, 사망 시 standby 슬롯 중 무작위 1장을 같은 field 슬롯에 자동 배치한다.
    /// OnCardSpawned 이벤트의 fromStandbyIdx로 UI가 어느 대기 슬롯이 비워졌는지 식별한다.
    /// </summary>
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

        var (next, fromStandbyIdx) = side.PopRandomStandby();
        if (next != null)
        {
            side.field[slotIdx] = next;
            OnCardSpawned?.Invoke(side, slotIdx, next, fromStandbyIdx);
        }
    }

    /// <summary>
    /// 회복 적용. 카드가 비어있거나 죽었으면 무시. baseHP 클램프는 CardInstance.Heal 이 처리.
    /// 회복이 실제 일어났을 때 OnHealApplied 발화 — UI 가 HealFX/Refresh 트리거.
    /// </summary>
    public void Heal(Side side, int slotIdx, int amount)
    {
        if (side == null || slotIdx < 0 || slotIdx >= side.field.Length) return;
        var card = side.field[slotIdx];
        if (card == null || card.IsDead) return;
        if (amount <= 0) return;

        int before = card.CurrentHP;
        card.Heal(amount);
        if (card.CurrentHP > before)
            OnHealApplied?.Invoke(side, slotIdx, card.CurrentHP - before);
    }
}
