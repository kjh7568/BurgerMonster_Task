using System.Collections.Generic;

/// <summary>
/// 일방향 데미지: 공격자 HP 만큼 적에게만 피해. 반격 없음(원거리 특성).
/// 도발 우선 적용.
/// </summary>
public class RangedAttack : ICardAttack
{
    public IEnumerable<int> GetValidTargets(BattleContext c) =>
        c.defenderSide.GetAttackPriorityIndices();

    public void Execute(BattleContext c, int targetIdx)
    {
        var atk = c.attackerSide.field[c.attackerIndex];
        c.resolver.DealDamage(c.defenderSide, targetIdx, atk.CurrentHP);
    }
}
