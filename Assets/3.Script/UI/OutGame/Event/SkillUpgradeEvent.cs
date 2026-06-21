using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 강화 이벤트 — "카드 한 장의 스킬 수치 +N". 수치형 스킬(HealSkill, VolleySkill) 보유 카드만 클릭 가능.
/// 덱에 수치형 스킬 카드가 한 장도 없으면 CanRun=false 로 후보에서 자동 제외.
/// 그리드 각 칸은 전투용 CardView prefab 을 그대로 재사용 (BindPreview 모드).
/// </summary>
public class SkillUpgradeEvent : UpgradeEventBase
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [Tooltip("CardView prefab 인스턴스를 PlayerDeck 크기만큼 미리 배치. 남는 슬롯은 자동 비활성.")]
    [SerializeField] private CardView[] cardSlots;

    [SerializeField] private string title = "지혜의 비전";
    [SerializeField] private string body = "한 명의 카드를 골라 스킬 수치를 +{0} 강화합니다.";
    [SerializeField] private int amount = 1;

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

    public override bool CanRun()
    {
        var deck = RunState.PlayerDeck;
        if (deck == null) return false;
        for (int i = 0; i < deck.Count; i++)
            if (deck[i] != null && HasNumericSkill(deck[i].type)) return true;
        return false;
    }

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
            bool interactable = HasNumericSkill(deck[i].type);
            view.BindPreview(deck[i], hpBonus, skillBonus, interactable);
            slotToDeckIndex[view] = i;
        }
    }

    private void HandleSlotClicked(CardView view)
    {
        if (!slotToDeckIndex.TryGetValue(view, out int deckIndex)) return;
        RunState.ApplyPerCardSkillUpgrade(deckIndex, amount);
        if (root != null) root.SetActive(false);
        var cb = onConfirm;
        onConfirm = null;
        cb?.Invoke();
    }

    /// <summary>수치(데미지/회복량)를 가진 스킬 보유 카드인지. Taunt/LastStand 는 수치가 없어 강화 불가.</summary>
    private static bool HasNumericSkill(CardType type)
    {
        return type == CardType.Healer || type == CardType.Ranged;
    }
}
