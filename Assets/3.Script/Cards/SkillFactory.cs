using System;

/// <summary>
/// CardType ↔ ICardAttack / ICardSkill 구현체 매핑. CardInstance 생성 시 각 1회 호출.
/// 무상태(stateless) 구현 권장 — 인스턴스마다 새로 만들어도 메모리 부담 없음.
/// </summary>
public static class SkillFactory
{
    public static ICardAttack CreateAttack(CardType type) => type switch
    {
        CardType.Normal => new NormalAttack(),
        CardType.Ranged => new RangedAttack(),
        CardType.Mighty => new MightyAttack(),
        CardType.Healer => new HealerAttack(),
        _ => throw new ArgumentException($"Unknown CardType: {type}")
    };

    public static ICardSkill CreateSkill(CardType type) => type switch
    {
        CardType.Normal => new TauntSkill(),
        CardType.Ranged => new VolleySkill(),
        CardType.Mighty => new LastStandSkill(),
        CardType.Healer => new HealSkill(),
        _ => throw new ArgumentException($"Unknown CardType: {type}")
    };
}
