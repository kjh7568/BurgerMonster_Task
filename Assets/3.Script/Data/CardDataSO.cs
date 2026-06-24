using UnityEngine;

[CreateAssetMenu(menuName = "BurgerMonster/CardData")]
public class CardDataSO : ScriptableObject
{
    public string cardName;          // 표시명 (기사, 궁수, 광전사, 사제)
    public CardType type;
    public int baseHP;               // 초기 HP. CardInstance.MaxHP = baseHP + variance 적용분 + 외부 hpBonus.
    [Range(0, 5)] public int hpVariance = 0;  // 0이면 고정, N이면 인스턴스 생성 시 baseHP ± N 균등 랜덤.
    public Sprite illustration;      // 일러스트 (placeholder OK)
    [TextArea] public string description;
    [Header("Skill (UI 표시용 메타)")]
    [Tooltip("스킬명. 예: \"도발\", \"일제사격\". 도감/덱뷰가 동일하게 사용.")]
    public string skillName;
    [Tooltip("스킬 설명문. 강화 보너스 수치는 UI 가 따로 표기 — 여기엔 base 수치/패시브 여부만 적는다.")]
    [TextArea(2, 5)] public string skillDescription;
}
