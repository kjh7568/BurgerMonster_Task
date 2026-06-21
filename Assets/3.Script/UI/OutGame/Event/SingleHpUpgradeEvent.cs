using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 강화 이벤트 — "카드 한 장 최대 HP +N". 보유 카드 그리드에서 1장 선택.
/// 그리드 각 칸은 전투용 CardView prefab 을 그대로 재사용 (BindPreview 모드).
/// </summary>
public class SingleHpUpgradeEvent : UpgradeEventBase
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [Tooltip("CardView prefab 인스턴스를 PlayerDeck 크기만큼 미리 배치. 남는 슬롯은 자동 비활성.")]
    [SerializeField] private CardView[] cardSlots;

    [SerializeField] private string title = "단련의 시련";
    [SerializeField] private string body = "한 명의 카드를 골라 최대 체력을 +{0} 강화합니다.";
    [SerializeField] private int amount = 3;

    private Action onConfirm;
    private readonly Dictionary<CardView, int> slotToDeckIndex = new Dictionary<CardView, int>();

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (cardSlots != null)
        {
            for (int i = 0; i < cardSlots.Length; i++)
                if (cardSlots[i] != null) cardSlots[i].OnClicked += HandleSlotClicked;
        }
    }

    private void OnDestroy()
    {
        if (cardSlots != null)
        {
            for (int i = 0; i < cardSlots.Length; i++)
                if (cardSlots[i] != null) cardSlots[i].OnClicked -= HandleSlotClicked;
        }
    }

    public override bool CanRun() => RunState.PlayerDeck != null && RunState.PlayerDeck.Count > 0;

    public override void Show(Action onConfirmed)
    {
        onConfirm = onConfirmed;
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = string.Format(body, amount);
        BuildGrid();
        if (root != null) root.SetActive(true);
    }

    private void BuildGrid()
    {
        slotToDeckIndex.Clear();
        if (cardSlots == null) return;
        var deck = RunState.PlayerDeck;
        int deckCount = deck != null ? deck.Count : 0;
        for (int i = 0; i < cardSlots.Length; i++)
        {
            var view = cardSlots[i];
            if (view == null) continue;
            if (i >= deckCount)
            {
                view.gameObject.SetActive(false);
                continue;
            }
            int hpBonus = RunState.GlobalHpBonus + RunState.GetPerCardHpBonus(i);
            int skillBonus = RunState.GetPerCardSkillBonus(i);
            view.BindPreview(deck[i], hpBonus, skillBonus, true);
            slotToDeckIndex[view] = i;
        }
    }

    private void HandleSlotClicked(CardView view)
    {
        if (!slotToDeckIndex.TryGetValue(view, out int deckIndex)) return;
        RunState.ApplyPerCardHpUpgrade(deckIndex, amount);
        if (root != null) root.SetActive(false);
        var cb = onConfirm;
        onConfirm = null;
        cb?.Invoke();
    }
}
