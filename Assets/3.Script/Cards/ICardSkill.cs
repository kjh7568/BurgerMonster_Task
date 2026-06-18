using System.Collections.Generic;

public interface ICardSkill
{
    /// <summary>
    /// 이 스킬로 효과를 적용할 수 있는 슬롯 인덱스를 열거한다.
    /// </summary>
    /// <param name="ctx">현재 전투 컨텍스트. 공격 측/방어 측/위치 정보가 들어 있다.</param>
    /// <returns>효과 대상 진영(보통 ctx.defenderSide, 자가 효과 스킬이라면 ctx.attackerSide)의 field 배열 인덱스. 지연 평가 가능한 시퀀스.</returns>
    IEnumerable<int> GetValidTargets(BattleContext ctx);

    /// <summary>
    /// 선택된 타겟에 스킬 효과를 실제로 적용한다.
    /// </summary>
    /// <param name="ctx">현재 전투 컨텍스트.</param>
    /// <param name="targetIndex">GetValidTargets에서 받은 슬롯 인덱스. 효과 대상 진영의 field 배열에서 사용된다.</param>
    void Execute(BattleContext ctx, int targetIndex);
}

public class BattleContext
{
    public Side attackerSide;
    public int attackerIndex;
    public Side defenderSide;
    public DamageResolver resolver;
}
