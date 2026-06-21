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

    /// <summary>강화 이벤트 옵션1 — 모든 카드에 가산되는 글로벌 HP 보너스. BattleController.BuildPlayerCards 에서 hpBonus 로 주입.</summary>
    public static int GlobalHpBonus { get; private set; }

    /// <summary>강화 이벤트 옵션2 — PlayerDeck 인덱스별 HP 보너스. 길이는 PlayerDeck.Count 와 동기화.</summary>
    private static readonly List<int> perCardHpBonus = new List<int>();

    /// <summary>강화 이벤트 옵션3 — PlayerDeck 인덱스별 스킬 수치 보너스(HealSkill 회복량, VolleySkill 데미지). 길이는 PlayerDeck.Count 와 동기화.</summary>
    private static readonly List<int> perCardSkillBonus = new List<int>();

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
        GlobalHpBonus = 0;
        perCardHpBonus.Clear();
        perCardSkillBonus.Clear();
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
        SyncBonusLists();
        Debug.Log($"[RunState] PlayerDeck set ({PlayerDeck.Count} cards)");
    }

    /// <summary>덱 크기 변화에 맞춰 보너스 리스트 길이를 늘리거나(0으로 채움) 줄인다. 기존 값은 보존.</summary>
    private static void SyncBonusLists()
    {
        while (perCardHpBonus.Count < PlayerDeck.Count) perCardHpBonus.Add(0);
        while (perCardHpBonus.Count > PlayerDeck.Count) perCardHpBonus.RemoveAt(perCardHpBonus.Count - 1);
        while (perCardSkillBonus.Count < PlayerDeck.Count) perCardSkillBonus.Add(0);
        while (perCardSkillBonus.Count > PlayerDeck.Count) perCardSkillBonus.RemoveAt(perCardSkillBonus.Count - 1);
    }

    public static int GetPerCardHpBonus(int deckIndex)
    {
        return (deckIndex >= 0 && deckIndex < perCardHpBonus.Count) ? perCardHpBonus[deckIndex] : 0;
    }

    public static int GetPerCardSkillBonus(int deckIndex)
    {
        return (deckIndex >= 0 && deckIndex < perCardSkillBonus.Count) ? perCardSkillBonus[deckIndex] : 0;
    }

    /// <summary>강화 옵션1 — 모든 카드 최대 HP +amount.</summary>
    public static void ApplyGlobalHpUpgrade(int amount)
    {
        GlobalHpBonus += amount;
        Debug.Log($"[RunState] GlobalHpBonus +{amount} → {GlobalHpBonus}");
    }

    /// <summary>강화 옵션2 — PlayerDeck[deckIndex] 의 HP +amount.</summary>
    public static void ApplyPerCardHpUpgrade(int deckIndex, int amount)
    {
        SyncBonusLists();
        if (deckIndex < 0 || deckIndex >= perCardHpBonus.Count) return;
        perCardHpBonus[deckIndex] += amount;
        Debug.Log($"[RunState] perCardHpBonus[{deckIndex}] +{amount} → {perCardHpBonus[deckIndex]} ({PlayerDeck[deckIndex].cardName})");
    }

    /// <summary>강화 옵션3 — PlayerDeck[deckIndex] 의 스킬 수치 +amount.</summary>
    public static void ApplyPerCardSkillUpgrade(int deckIndex, int amount)
    {
        SyncBonusLists();
        if (deckIndex < 0 || deckIndex >= perCardSkillBonus.Count) return;
        perCardSkillBonus[deckIndex] += amount;
        Debug.Log($"[RunState] perCardSkillBonus[{deckIndex}] +{amount} → {perCardSkillBonus[deckIndex]} ({PlayerDeck[deckIndex].cardName})");
    }

    public static void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        Debug.Log($"[RunState] Gold +{amount} → {Gold}");
    }

    /// <summary>디버그/치트용 골드 강제 세팅. 음수 무시.</summary>
    public static void SetGold(int amount)
    {
        if (amount < 0) return;
        Gold = amount;
        Debug.Log($"[RunState] Gold set → {Gold}");
    }

    /// <summary>amount 만큼 골드 차감 시도. 잔액 부족이면 false 반환하고 차감 안 함.</summary>
    public static bool SpendGold(int amount)
    {
        if (amount < 0) return false;
        if (Gold < amount) return false;
        Gold -= amount;
        Debug.Log($"[RunState] Gold -{amount} → {Gold}");
        return true;
    }

    /// <summary>용병 영입 — PlayerDeck[deckIndex] 의 카드를 newCard 로 교체. 해당 슬롯의 HP/스킬 보너스는 0으로 초기화(이전 카드 강화 흔적 제거).</summary>
    public static void ReplaceCardAt(int deckIndex, CardDataSO newCard)
    {
        if (newCard == null) return;
        if (deckIndex < 0 || deckIndex >= PlayerDeck.Count) return;
        var old = PlayerDeck[deckIndex];
        PlayerDeck[deckIndex] = newCard;
        SyncBonusLists();
        if (deckIndex < perCardHpBonus.Count) perCardHpBonus[deckIndex] = 0;
        if (deckIndex < perCardSkillBonus.Count) perCardSkillBonus[deckIndex] = 0;
        Debug.Log($"[RunState] PlayerDeck[{deckIndex}] {old?.cardName} → {newCard.cardName} (bonuses reset)");
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
