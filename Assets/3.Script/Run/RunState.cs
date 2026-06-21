using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 단일 Run 동안의 진행 상태. 씬 전환에서 살아남도록 static 으로 보관.
/// MapScene 진입 시 <see cref="EnsureInitialized"/> 가 한 번 실행되며,
/// 전투 결과·이벤트 노드 완료에 따라 <see cref="AdvanceNode"/>·<see cref="ResetRun"/> 가 호출된다.
/// </summary>
public static class RunState
{
    private static bool initialized;

    public static int CurrentNodeIndex { get; private set; }
    public static bool RunCompleted { get; private set; }
    /// <summary>이번 Run 에서 선택된 지도. MapPanelController 가 Run 시작 시 풀에서 한 번 뽑아 세팅한다.</summary>
    public static MapDataSO CurrentMap { get; private set; }

    /// <summary>현재 난이도 단계. Upgrade/Recruit 노드 완료 시마다 +1. DifficultyTableSO 가 stage→EnemyPool 매핑에 사용.</summary>
    public static int Stage { get; private set; }

    /// <summary>Run 동안 누적 골드. 전투 승리 시 EnemyPool.goldReward 가 더해지고, 용병 영입 노드에서 소모.</summary>
    public static int Gold { get; private set; }

    /// <summary>플레이어 덱(시작 6장 + 영입/강화 결과). MapScene 진입 시 비어있으면 StartingDeck 에서 픽해 초기화.</summary>
    public static List<CardDataSO> PlayerDeck { get; private set; } = new List<CardDataSO>();

    public static void EnsureInitialized()
    {
        if (initialized) return;
        ResetRun();
    }

    public static void ResetRun()
    {
        CurrentNodeIndex = 0;
        RunCompleted = false;
        CurrentMap = null;
        Stage = 0;
        Gold = 0;
        PlayerDeck.Clear();
        initialized = true;
        Debug.Log("[RunState] Reset");
    }

    public static void SelectMap(MapDataSO map)
    {
        CurrentMap = map;
        Debug.Log($"[RunState] Map selected: {(map != null ? map.name : "null")}");
    }

    public static void SetPlayerDeck(IEnumerable<CardDataSO> cards)
    {
        PlayerDeck.Clear();
        if (cards != null) PlayerDeck.AddRange(cards);
        Debug.Log($"[RunState] PlayerDeck set ({PlayerDeck.Count} cards)");
    }

    public static void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        Debug.Log($"[RunState] Gold +{amount} → {Gold}");
    }

    /// <summary>현재 노드 완료 처리. 인덱스를 1 증가시키고, 완료한 노드가 이벤트(Upgrade/Recruit)면 Stage 도 +1.</summary>
    public static void AdvanceNode(int totalNodes, NodeType completedType)
    {
        if (totalNodes <= 0) return;
        CurrentNodeIndex = Mathf.Min(CurrentNodeIndex + 1, totalNodes);
        if (CurrentNodeIndex >= totalNodes) RunCompleted = true;

        if (completedType == NodeType.Upgrade || completedType == NodeType.Recruit)
        {
            Stage++;
            Debug.Log($"[RunState] Stage → {Stage} (after {completedType})");
        }
        Debug.Log($"[RunState] Advance → {CurrentNodeIndex}/{totalNodes} (completed={RunCompleted})");
    }
}
