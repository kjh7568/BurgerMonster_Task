using UnityEngine;

public static class TurnStartEffects
{
    /// <summary>
    /// 해당 진영의 모든 살아있는 힐러가 같은 진영 다른 아군 카드를 +1 회복한다.
    /// 힐러 자신은 회복 대상에서 제외. 회복 상한은 baseHP(CardInstance.Heal 이 클램프).
    /// 힐러가 여러 명이면 각각 +1씩 누적된다.
    /// resolver 를 통해 Heal 호출 → OnHealApplied 이벤트 발화 → UI 가 HealFX/갱신 처리.
    /// </summary>
    public static void Apply(Side side, DamageResolver resolver)
    {
        if (side == null || resolver == null) return;
        foreach (int i in side.AliveIndices())
        {
            var card = side.field[i];
            if (card.data.type != CardType.Healer) continue;

            foreach (int j in side.AliveIndices())
            {
                if (j == i) continue;
                var target = side.field[j];
                int before = target.CurrentHP;
                resolver.Heal(side, j, 1);
                Debug.Log($"[TurnStart] Healer slot={i} healed slot={j} {before}->{target.CurrentHP}");
            }
        }
    }
}
