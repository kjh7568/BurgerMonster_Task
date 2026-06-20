using System.Collections.Generic;

/// <summary>
/// 사제의 일반 공격은 기사와 동일(상호 데미지). 위임으로 코드 중복 회피.
/// </summary>
public class HealerAttack : ICardAttack
{
    private readonly NormalAttack normal = new NormalAttack();
    public IEnumerable<int> GetValidTargets(BattleContext c) => normal.GetValidTargets(c);
    public void Execute(BattleContext c, int targetIdx) => normal.Execute(c, targetIdx);
}
