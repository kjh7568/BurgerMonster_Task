using UnityEngine;

[CreateAssetMenu(menuName = "BurgerMonster/CardData")]
public class CardDataSO : ScriptableObject
{
    public string cardName;          // 표시명 (기사, 궁수, 광전사, 사제)
    public CardType type;
    public int baseHP;               // 초기 HP, 회복 상한
    public Sprite illustration;      // 일러스트 (placeholder OK)
    [TextArea] public string description;
}
