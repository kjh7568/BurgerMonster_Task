using UnityEngine;

public class CardInstance
{
    public readonly CardDataSO data;
    public ICardSkill Skill { get; }
    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    /// <summary>
    /// CardDataSO를 기반으로 새 카드 인스턴스를 만든다. 전투 시작 시 또는 대기 카드 생성 시 호출. CurrentHP는 baseHP로, Skill은 타입에 맞춰 자동 생성된다.
    /// </summary>
    /// <param name="data">이 카드의 정적 데이터(이름·타입·만피 등) SO.</param>
    public CardInstance(CardDataSO data)
    {
        this.data = data;
        CurrentHP = data.baseHP;
        Skill = SkillFactory.Create(data.type);
    }

    /// <summary>
    /// 피해를 받아 CurrentHP를 amount만큼 깎는다. 0 미만으로는 내려가지 않는다. DamageResolver에서 호출.
    /// </summary>
    /// <param name="amount">깎을 HP. 음수 검증은 호출자 책임.</param>
    public void TakeDamage(int amount) => CurrentHP = Mathf.Max(0, CurrentHP - amount);

    /// <summary>
    /// HP를 amount만큼 회복한다. baseHP(최대 체력)를 넘지 않는다. 힐러 스킬에서 사용.
    /// </summary>
    /// <param name="amount">회복할 HP. 음수 검증은 호출자 책임.</param>
    public void Heal(int amount) => CurrentHP = Mathf.Min(data.baseHP, CurrentHP + amount);
}
