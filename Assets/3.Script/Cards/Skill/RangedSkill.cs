using System.Collections.Generic;

public class RangedSkill : ICardSkill
{
    public IEnumerable<int> GetValidTargets(BattleContext c) => c.defenderSide.AliveIndices();

    public void Execute(BattleContext c, int targetIdx)
    {
        var atk = c.attackerSide.field[c.attackerIndex];
        c.resolver.DealDamage(c.defenderSide, targetIdx, atk.CurrentHP);
    }
}
