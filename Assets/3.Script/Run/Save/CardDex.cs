using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 도감 — 한 번이라도 조우한 카드 ID 를 SaveData.discoveredCards 에 누적.
/// 등록 시점: BattleController.Init 의 양 진영, RecruitEvent 의 후보 슬롯.
/// MVP 단계(카드 4종) 에서는 모든 카드가 사실상 즉시 해금되지만, 카드 풀이 늘어나면 이 API 가 도감 UI 의 해금 토글로 직접 쓰인다.
/// </summary>
public static class CardDex
{
    /// <summary>cardId 를 도감에 추가하고 즉시 Flush. 이미 등록돼 있으면 no-op.</summary>
    public static void Register(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return;
        var data = SaveSystem.Current;
        if (data == null) return;
        if (data.discoveredCards == null) data.discoveredCards = new List<string>();
        if (data.discoveredCards.Contains(cardId)) return;
        data.discoveredCards.Add(cardId);
        SaveSystem.Flush();
    }

    /// <summary>CardDataSO 편의 오버로드. null 카드는 무시.</summary>
    public static void Register(CardDataSO card)
    {
        if (card == null) return;
        Register(GameAssetsSO.CardId(card));
    }

    public static bool IsDiscovered(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        var data = SaveSystem.Current;
        if (data == null || data.discoveredCards == null) return false;
        return data.discoveredCards.Contains(cardId);
    }

    /// <summary>GameAssetsSO.allCards 전체. 도감 UI 가 그리드 채울 때 소스. null/missing 자산은 건너뜀.</summary>
    public static IEnumerable<CardDataSO> AllCards
    {
        get
        {
            var inst = GameAssetsSO.Instance;
            if (inst == null || inst.allCards == null) yield break;
            for (int i = 0; i < inst.allCards.Length; i++)
                if (inst.allCards[i] != null) yield return inst.allCards[i];
        }
    }
}
