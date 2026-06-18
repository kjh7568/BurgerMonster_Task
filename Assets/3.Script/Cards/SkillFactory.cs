using System;

public static class SkillFactory
{
    public static ICardSkill Create(CardType type) => type switch
    {
        CardType.Normal => new NormalSkill(),
        CardType.Ranged => new RangedSkill(),
        CardType.Mighty => new MightySkill(),
        CardType.Healer => new HealerSkill(),
        _ => throw new ArgumentException($"Unknown CardType: {type}")
    };
}
