using UnityEngine;

/// <summary>
/// 전투 룰 설정. 카드 풀(플레이어/적)은 더 이상 여기 두지 않는다.
/// 플레이어 시작 풀은 <see cref="StartingDeckSO"/>, 적 풀은 <see cref="DifficultyTableSO"/> 참조.
/// </summary>
[CreateAssetMenu(menuName = "BurgerMonster/BattleConfig")]
public class BattleConfigSO : ScriptableObject
{
    public int fieldSlotCount = 3;
    public float opponentActionDelay = 0.8f;  // AI 행동 사이 시각 딜레이
}
