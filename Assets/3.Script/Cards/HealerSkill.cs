using System.Collections.Generic;

public class HealerSkill : ICardSkill
{
    readonly NormalSkill normal = new NormalSkill();

    public IEnumerable<int> GetValidTargets(BattleContext c) => normal.GetValidTargets(c);

    public void Execute(BattleContext c, int targetIdx) => normal.Execute(c, targetIdx);
}
