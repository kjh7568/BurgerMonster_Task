using System;

public static class SkillFactory
{
    public static ICardSkill Create(CardType type) => type switch
    {
        CardType.Normal => new NormalSkill(),
        CardType.Ranged => new RangedSkill(),
        CardType.Mighty => null, // TODO: MightySkill 작업에서 채움
        CardType.Healer => null, // TODO: HealerSkill 작업에서 채움
        _ => throw new ArgumentException($"Unknown CardType: {type}")
    };
}
