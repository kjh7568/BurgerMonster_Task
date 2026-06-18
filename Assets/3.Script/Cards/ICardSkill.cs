using System.Collections.Generic;

public interface ICardSkill
{
    /// <summary>
    /// 이 스킬로 효과를 적용할 수 있는 슬롯 인덱스를 열거한다.
    /// </summary>
    /// <param name="ctx">현재 전투 컨텍스트. 공격 측/방어 측/위치 정보가 들어 있다.</param>
    /// <returns>효과 대상 진영(보통 ctx.defenderSide, 자가 효과 스킬이라면 ctx.attackerSide)의 field 배열 인덱스. 지연 평가 가능한 시퀀스.</returns>
    /// <remarks>
    /// 기본 구현은 보통 "살아있는 적 슬롯 전부"(Side.AliveIndices())를 그대로 흘려보낸다.
    /// 스킬 고유 제약(예: 무쌍의 "앞줄만", 힐러의 "자신 제외 아군")은 이 메서드 안에서 추가 필터링한다.
    /// 즉시 리스트로 만들 필요 없음 — 호출자가 하나만 골라 쓰는 경우가 많아 IEnumerable로 둔다.
    /// </remarks>
    IEnumerable<int> GetValidTargets(BattleContext ctx);

    /// <summary>
    /// 선택된 타겟에 스킬 효과를 실제로 적용한다.
    /// </summary>
    /// <param name="ctx">현재 전투 컨텍스트.</param>
    /// <param name="targetIndex">GetValidTargets에서 받은 슬롯 인덱스. 효과 대상 진영의 field 배열에서 사용된다.</param>
    /// <remarks>
    /// 피해/회복 적용은 직접 CardInstance를 만지지 말고 ctx.resolver를 통해 수행한다.
    /// 사망 처리, 슬롯 비우기, 대기 카드 자동 배치는 DamageResolver 책임 — 스킬 구현은 신경 쓸 필요 없음.
    /// HP 스냅샷 주의(반격이 있는 스킬): 첫 피해 적용 전에 공격자/방어자의 CurrentHP를 지역 변수로 미리 저장. 그렇지 않으면 방어자가 죽은 후 0이 된 HP가 반격에 적용된다.
    /// </remarks>
    void Execute(BattleContext ctx, int targetIndex);
}

public class BattleContext
{
    public Side attackerSide;
    public int attackerIndex;
    public Side defenderSide;
    public DamageResolver resolver;
}
