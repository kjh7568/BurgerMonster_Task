using UnityEngine;

[CreateAssetMenu(menuName = "BurgerMonster/BattleConfig")]
public class BattleConfigSO : ScriptableObject
{
    public CardDataSO[] playerStartingCards = new CardDataSO[6];
    public CardDataSO[] opponentStartingCards = new CardDataSO[6];
    public int fieldSlotCount = 3;
    public float opponentActionDelay = 0.8f;  // AI 행동 사이 시각 딜레이
}
