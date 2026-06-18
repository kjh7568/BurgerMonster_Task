using System;

public static class SkillFactory
{
    /// <summary>
    /// CardType에 매핑된 ICardSkill 구현체를 새로 생성한다. CardInstance가 CardDataSO.type을 받아 자신의 스킬을 부여받을 때 사용.
    /// </summary>
    /// <param name="type">생성할 스킬에 대응하는 카드 타입. Normal/Ranged/Mighty/Healer 중 하나.</param>
    /// <returns>type에 대응하는 ICardSkill 인스턴스. 상태를 가지지 않으므로 카드마다 새로 만들어도 무방.</returns>
    /// <exception cref="ArgumentException">정의되지 않은 CardType이 들어온 경우. 신규 카드 추가 시 이 switch에 분기 추가 필수.</exception>
    public static ICardSkill Create(CardType type) => type switch
    {
        CardType.Normal => new NormalSkill(),
        CardType.Ranged => new RangedSkill(),
        CardType.Mighty => new MightySkill(),
        CardType.Healer => new HealerSkill(),
        _ => throw new ArgumentException($"Unknown CardType: {type}")
    };
}
