using System.Collections.Generic;

public interface ICardSkill
{
    IEnumerable<int> GetValidTargets(BattleContext ctx);
    void Execute(BattleContext ctx, int targetIndex);
}

public class BattleContext
{
    public Side attackerSide;
    public int attackerIndex;
    public Side defenderSide;
    public DamageResolver resolver;
}
