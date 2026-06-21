using System;
using UnityEngine;

/// <summary>
/// 유료 영입(Recruit_Paid) 노드용 풀. (카드, 가격) 엔트리 리스트.
/// 노드 진입 시 PaidRecruitEvent 가 이 풀에서 중복 없이 후보 3장을 랜덤으로 뽑는다.
/// </summary>
[CreateAssetMenu(menuName = "BurgerMonster/MercenaryPool")]
public class MercenaryPoolSO : ScriptableObject
{
    [Serializable]
    public struct PaidEntry
    {
        public CardDataSO card;
        [Min(0)] public int price;
    }

    [Tooltip("후보로 뽑힐 (카드, 가격) 엔트리. 같은 카드를 다른 가격으로 여러 번 넣어 가중치 효과를 줄 수도 있다.")]
    public PaidEntry[] entries;
}
