using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 본 타겟 상호 데미지 + 인접 살아있는 카드 1명에 atkHP의 0.5배 스플래시.
/// 도발 우선 적용(메인 타겟만). 스플래시는 인접만 보고 도발 무시.
/// </summary>
public class MightyAttack : ICardAttack
{
    public IEnumerable<int> GetValidTargets(BattleContext c) =>
        c.defenderSide.GetAttackPriorityIndices();

    public void Execute(BattleContext c, int targetIdx)
    {
        var atk = c.attackerSide.field[c.attackerIndex];
        var def = c.defenderSide.field[targetIdx];

        int atkHP = atk.CurrentHP;
        int defHP = def.CurrentHP;

        c.resolver.DealDamage(c.defenderSide, targetIdx, atkHP);

        var adjacents = GetAdjacentAliveIndices(c.defenderSide, targetIdx);
        if (adjacents.Count > 0)
        {
            int splashIdx = adjacents[Random.Range(0, adjacents.Count)];
            int splashDmg = Mathf.RoundToInt(atkHP * 0.5f);
            c.resolver.DealDamage(c.defenderSide, splashIdx, splashDmg);
        }

        c.resolver.DealDamage(c.attackerSide, c.attackerIndex, defHP);
    }

    public static List<int> GetAdjacentAliveIndices(Side side, int idx)
    {
        var result = new List<int>();
        foreach (int delta in new[] { -1, 1 })
        {
            int i = idx + delta;
            if (i < 0 || i >= side.field.Length) continue;
            if (side.field[i] == null || side.field[i].IsDead) continue;
            result.Add(i);
        }
        return result;
    }
}
