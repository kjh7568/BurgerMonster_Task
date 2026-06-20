using System.Collections.Generic;

/// <summary>
/// 모든 카드가 가지는 일반 공격. valid target과 실행 로직만 제공.
/// BattleContext는 본 파일에 함께 두어 ICardSkill도 같은 컨텍스트를 공유.
/// </summary>
public interface ICardAttack
{
    /// <summary>이 공격이 칠 수 있는 슬롯 인덱스(= ctx.defenderSide.field 인덱스).</summary>
    IEnumerable<int> GetValidTargets(BattleContext ctx);

    /// <summary>실제 데미지/효과 적용. targetIndex는 GetValidTargets에서 반환된 인덱스.</summary>
    void Execute(BattleContext ctx, int targetIndex);
}

/// <summary>
/// 공격·스킬 모두에 사용되는 한 번의 행동 컨텍스트.
/// </summary>
public class BattleContext
{
    public Side attackerSide;
    public int attackerIndex;
    public Side defenderSide;
    public DamageResolver resolver;
}
