using UnityEngine;

public class CardInstance
{
    public readonly CardDataSO data;

    /// <summary>일반 공격 — 매 턴 무한 사용 가능.</summary>
    public ICardAttack Attack { get; }

    /// <summary>카드 고유 스킬 — 생존 중 단 1회. 패시브는 IsActive=false 로 표시되며 게임 이벤트로 자동 트리거.</summary>
    public ICardSkill Skill { get; }

    /// <summary>이 인스턴스의 HP 상한. baseHP + variance 흔들림 + 외부 hpBonus(EnemyPool 등). 회복 상한·HP바 fillAmount 의 분모로 사용. 상호 HP 데미지 모델에선 이 값이 곧 공격력이기도 함.</summary>
    public int MaxHP { get; }

    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    /// <summary>액티브 스킬이 한 번 소모됐는지. 패시브는 별도 플래그 사용.</summary>
    public bool SkillUsed { get; set; }

    /// <summary>기사 도발 상태. true면 공격 우선순위에서 강제 1순위.</summary>
    public bool IsTaunting { get; set; }

    /// <summary>광전사 위기 생존 패시브 1회 사용 여부.</summary>
    public bool LastStandUsed { get; private set; }

    /// <summary>강화 옵션3 — 스킬 수치(회복량/데미지)에 가산되는 보너스. HealSkill/VolleySkill 에서 시전 시 읽음.</summary>
    public int SkillBonus { get; }

    /// <summary>
    /// CardDataSO 와 외부 HP/스킬 보정으로 카드 인스턴스를 만든다.
    /// MaxHP = max(1, baseHP + variance 랜덤 ± + hpBonus). CurrentHP 은 MaxHP 로 시작.
    /// </summary>
    /// <param name="hpBonus">EnemyPool · RunState 강화 등 외부 HP 보정. 0 이면 효과 없음. 상호 HP 데미지 모델이라 이 값이 공격력 증가도 겸함.</param>
    /// <param name="skillBonus">강화 이벤트의 스킬 수치 보너스. HealSkill 회복량/VolleySkill 데미지에 가산.</param>
    public CardInstance(CardDataSO data, int hpBonus = 0, int skillBonus = 0)
    {
        this.data = data;
        int variance = data.hpVariance > 0 ? Random.Range(-data.hpVariance, data.hpVariance + 1) : 0;
        MaxHP = Mathf.Max(1, data.baseHP + variance + hpBonus);
        CurrentHP = MaxHP;
        SkillBonus = skillBonus;
        Attack = SkillFactory.CreateAttack(data.type);
        Skill = SkillFactory.CreateSkill(data.type);
    }

    /// <summary>세이브 로드용 — variance 를 다시 굴리지 않고 보존된 MaxHP/CurrentHP/상태 플래그를 그대로 복원.</summary>
    private CardInstance(CardDataSO data, int maxHP, int currentHP, int skillBonus, bool skillUsed, bool isTaunting, bool lastStandUsed)
    {
        this.data = data;
        MaxHP = Mathf.Max(1, maxHP);
        CurrentHP = Mathf.Clamp(currentHP, 0, MaxHP);
        SkillBonus = skillBonus;
        SkillUsed = skillUsed;
        IsTaunting = isTaunting;
        LastStandUsed = lastStandUsed;
        Attack = SkillFactory.CreateAttack(data.type);
        Skill = SkillFactory.CreateSkill(data.type);
    }

    public static CardInstance CreateRestored(CardDataSO data, int maxHP, int currentHP, int skillBonus, bool skillUsed, bool isTaunting, bool lastStandUsed)
    {
        if (data == null) return null;
        return new CardInstance(data, maxHP, currentHP, skillBonus, skillUsed, isTaunting, lastStandUsed);
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

    /// <summary>HP를 amount만큼 회복. MaxHP 상한 클램프.</summary>
    public void Heal(int amount) => CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
}
