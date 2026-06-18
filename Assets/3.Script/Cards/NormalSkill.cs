using System.Collections.Generic;

public class NormalSkill : ICardSkill
{
    public IEnumerable<int> GetValidTargets(BattleContext c) => c.defenderSide.AliveIndices();

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
