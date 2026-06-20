using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 광전사 패시브 — 죽을 데미지를 받을 때 단 1회 HP=1로 생존.
/// 실제 트리거는 CardInstance.TakeDamage 내부의 type/LastStandUsed 분기로 처리.
/// 본 클래스는 UI/팩토리 일관성을 위해 존재하며 Execute는 호출되지 않는다.
/// </summary>
public class LastStandSkill : ICardSkill
{
    public bool IsActive => false;
    public SkillTargetMode TargetMode => SkillTargetMode.None;

    public IEnumerable<int> GetValidTargets(BattleContext ctx) => Enumerable.Empty<int>();

    public void Execute(BattleContext ctx, int targetIndex) { /* no-op */ }
}
