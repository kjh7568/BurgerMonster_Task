using System.Collections.Generic;

/// <summary>
/// 사제 액티브 — 아군 1명에 +2 회복. 자기 자신도 valid 후보에 포함.
/// 회복 상한은 CardInstance.Heal 의 baseHP 클램프가 보장.
/// </summary>
public class HealSkill : ICardSkill
{
    public bool IsActive => true;
    public SkillTargetMode TargetMode => SkillTargetMode.Ally;

    public IEnumerable<int> GetValidTargets(BattleContext ctx) => ctx.attackerSide.AliveIndices();

    public void Execute(BattleContext ctx, int targetIndex)
    {
        // Resolver 경유로 OnHealApplied 이벤트 발화 → UI 가 HealFX 재생.
        var caster = ctx.attackerSide.field[ctx.attackerIndex];
        int amount = 2 + (caster != null ? caster.SkillBonus : 0);
        ctx.resolver.Heal(ctx.attackerSide, targetIndex, amount);
    }
}
