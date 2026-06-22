using System;
using UnityEngine;

/// <summary>
/// 한 stage 대역에서 적이 뽑히는 풀. weighted random pick + 풀 전체 스탯 보정 + 승리 골드 보상.
/// </summary>
[CreateAssetMenu(menuName = "BurgerMonster/EnemyPool")]
public class EnemyPoolSO : ScriptableObject
{
    [Serializable]
    public struct EnemyEntry
    {
        public CardDataSO card;
        [Min(1)] public int weight;
    }

    [Tooltip("픽 대상 카드 + 가중치. weight 가 클수록 자주 뽑힘.")]
    public EnemyEntry[] enemies;

    [Tooltip("풀 전체에 더해질 HP 보정 (베이스 + variance + 이 값). 상호 HP 데미지 모델이라 이 값이 곧 공격력 증가도 겸함.")]
    public int statBonusHP;

    [Tooltip("이 풀의 적을 처치(전투 승리)했을 때 획득할 골드.")]
    public int goldReward;

    [Tooltip("이 풀의 적 1장 처치 시 가산될 점수의 난이도(1~3). 점수 = difficulty × 100. 후반 풀일수록 큰 값.")]
    [Range(1, 3)] public int difficulty = 1;
}
