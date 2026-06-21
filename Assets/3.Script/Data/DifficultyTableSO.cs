using System;
using UnityEngine;

/// <summary>
/// Stage 값을 EnemyPool 로 매핑하는 테이블. BattleController 가 전투 시작 시
/// 현재 <see cref="RunState.Stage"/> 에 해당하는 풀을 골라 적을 뽑는다.
/// 매핑은 minStage 오름차순으로 정렬돼 있어야 하며, 일치/그 이하 중 가장 큰 minStage 의 엔트리를 사용.
/// </summary>
[CreateAssetMenu(menuName = "BurgerMonster/DifficultyTable")]
public class DifficultyTableSO : ScriptableObject
{
    [Serializable]
    public struct StageEntry
    {
        [Min(0)] public int minStage;
        public EnemyPoolSO pool;
    }

    [Tooltip("minStage 오름차순. 예: [(0, Easy), (1, Normal), (2, Hard)].")]
    public StageEntry[] stages;

    /// <summary>주어진 stage 에 대응하는 EnemyPool 을 반환. 일치하는 엔트리가 없으면 null.</summary>
    public EnemyPoolSO Resolve(int stage)
    {
        if (stages == null || stages.Length == 0) return null;
        EnemyPoolSO best = null;
        int bestMin = -1;
        for (int i = 0; i < stages.Length; i++)
        {
            var e = stages[i];
            if (e.minStage <= stage && e.minStage > bestMin)
            {
                best = e.pool;
                bestMin = e.minStage;
            }
        }
        return best;
    }
}
