using UnityEngine;

public class CardInstance
{
    public readonly CardDataSO data;

    /// <summary>일반 공격 — 매 턴 무한 사용 가능.</summary>
    public ICardAttack Attack { get; }

    /// <summary>카드 고유 스킬 — 생존 중 단 1회. 패시브는 IsActive=false 로 표시되며 게임 이벤트로 자동 트리거.</summary>
    public ICardSkill Skill { get; }

    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    /// <summary>액티브 스킬이 한 번 소모됐는지. 패시브는 별도 플래그 사용.</summary>
    public bool SkillUsed { get; set; }

    /// <summary>기사 도발 상태. true면 공격 우선순위에서 강제 1순위.</summary>
    public bool IsTaunting { get; set; }

    /// <summary>광전사 위기 생존 패시브 1회 사용 여부.</summary>
    public bool LastStandUsed { get; private set; }

    /// <summary>
    /// CardDataSO를 기반으로 새 카드 인스턴스를 만든다. CurrentHP=baseHP, Attack/Skill은 타입에 맞춰 자동 생성.
    /// </summary>
    public CardInstance(CardDataSO data)
    {
        this.data = data;
        CurrentHP = data.baseHP;
        Attack = SkillFactory.CreateAttack(data.type);
        Skill = SkillFactory.CreateSkill(data.type);
    }

    /// <summary>
    /// 피해 적용. 광전사이면서 LastStand 미사용 상태에서 죽을 데미지를 받으면 HP=1로 클램프하고 LastStandUsed=true.
    /// </summary>
    public void TakeDamage(int amount)
    {
        int next = Mathf.Max(0, CurrentHP - amount);
        if (next == 0 && data.type == CardType.Mighty && !LastStandUsed)
        {
            next = 1;
            LastStandUsed = true;
            Debug.Log($"[LastStand] {data.cardName} 위기 생존 발동 (HP=1)");
        }
        CurrentHP = next;
    }

    /// <summary>HP를 amount만큼 회복. baseHP 상한 클램프.</summary>
    public void Heal(int amount) => CurrentHP = Mathf.Min(data.baseHP, CurrentHP + amount);
}
