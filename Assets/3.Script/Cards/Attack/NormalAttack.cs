using System.Collections.Generic;

/// <summary>
/// 상호 데미지: 공격자/방어자가 서로의 현재 HP만큼 동시 피해.
/// 도발 카드가 방어 진영에 있으면 그쪽으로 강제(GetAttackPriorityIndices).
/// </summary>
public class NormalAttack : ICardAttack
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
        c.resolver.DealDamage(c.attackerSide, c.attackerIndex, defHP);
    }
}
