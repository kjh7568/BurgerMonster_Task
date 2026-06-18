using System.Collections.Generic;
using UnityEngine;

public class MightySkill : ICardSkill
{
    public IEnumerable<int> GetValidTargets(BattleContext c) => c.defenderSide.AliveIndices();

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
