using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 궁수 액티브 — 적 field 의 살아있는 모든 카드에 1뎀. 대상 선택 없음.
/// 적용 도중 사망/standby 교체가 일어날 수 있으므로 시작 시점의 alive 스냅샷을 기준으로 순회.
/// </summary>
public class VolleySkill : ICardSkill
{
    public bool IsActive => true;
    public SkillTargetMode TargetMode => SkillTargetMode.None;

    public IEnumerable<int> GetValidTargets(BattleContext ctx) => Enumerable.Empty<int>();

    public void Execute(BattleContext ctx, int targetIndex)
    {
        var caster = ctx.attackerSide.field[ctx.attackerIndex];
        int amount = 1 + (caster != null ? caster.SkillBonus : 0);
        var snapshot = ctx.defenderSide.AliveIndices().ToList();
        foreach (int i in snapshot)
        {
            ctx.resolver.DealDamage(ctx.defenderSide, i, amount);
        }
    }
}
