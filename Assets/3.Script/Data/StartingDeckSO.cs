using UnityEngine;

/// <summary>
/// 플레이어 시작 카드 후보 풀. Run 시작 시 이 풀에서 6장을 랜덤 픽해 <see cref="RunState.PlayerDeck"/> 에 복사한다.
/// 풀 사이즈가 6 이상이면 셔플 후 앞 6장, 부족하면 중복 허용으로 채움.
/// </summary>
[CreateAssetMenu(menuName = "BurgerMonster/StartingDeck")]
public class StartingDeckSO : ScriptableObject
{
    [Tooltip("플레이어 시작 후보 카드. 같은 SO 를 여러 번 넣거나 variance 가 다른 변형 SO 를 섞어 다양성 확보.")]
    public CardDataSO[] cards;
}
