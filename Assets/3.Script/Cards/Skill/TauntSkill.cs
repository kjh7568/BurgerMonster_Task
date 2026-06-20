using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 기사 액티브 — 자기 시전. 발동 시 자신의 IsTaunting=true.
/// 도발 효과는 Side.GetAttackPriorityIndices()에서 살아있는 도발 카드만 valid로 반환하는 것으로 양 진영 자동 적용.
/// 자신이 죽으면 IsTaunting 도 함께 사라짐(인스턴스 자체가 standby로 교체됨).
/// </summary>
public class TauntSkill : ICardSkill
{
    public bool IsActive => true;
    public SkillTargetMode TargetMode => SkillTargetMode.None;

    public IEnumerable<int> GetValidTargets(BattleContext ctx) => Enumerable.Empty<int>();

    public void Execute(BattleContext ctx, int targetIndex)
    {
        var self = ctx.attackerSide.field[ctx.attackerIndex];
        if (self != null) self.IsTaunting = true;
    }
}
