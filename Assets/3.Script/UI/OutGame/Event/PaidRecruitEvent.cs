using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유료 영입 이벤트 — 풀에서 3장 후보 + 카드 밑 가격 표시, 덱 6장은 하단에 표시.
/// 흐름: 덱 1장 클릭(버릴 카드 하이라이트) → 후보 클릭 = 즉시 영입(SpendGold + ReplaceCardAt).
/// 골드/슬롯 허용 한도 내 여러 장 구매 가능. "나가기" 누르면 노드 진행.
/// </summary>
public class PaidRecruitEvent : RecruitEventBase
{
    [Header("Pool")]
    [SerializeField] private MercenaryPoolSO pool;
    [Tooltip("후보 슬롯 수. 기본 3.")]
    [SerializeField] private int candidateCount = 3;

    [Header("Root / Texts")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private string title = "용병 영입";
    [SerializeField] private string body = "자리는 6개뿐. 데려오려면 한 장을 버려야 한다.";
    [SerializeField] private string bodyAfterCandidatePicked = "해고할 용병을 내 카드에서 선택하세요";
    [SerializeField] private string goldFormat = "소지 골드: {0}G";

    [Header("Candidate Slots (3)")]
    [Tooltip("CardView prefab 인스턴스 3개. 가격은 candidatePriceTexts 와 평행하게 매칭.")]
    [SerializeField] private CardView[] candidateSlots;
    [Tooltip("각 후보 카드 밑에 붙는 가격 TMP_Text. candidateSlots 와 같은 개수/순서. 총 3개.")]
    [SerializeField] private TMP_Text[] candidatePriceTexts;
    [SerializeField] private string priceFormat = "{0}G";

    [Header("Deck Slots (6)")]
    [Tooltip("PlayerDeck 표시용 CardView 인스턴스. 보통 6칸. 덱 길이보다 많으면 남는 칸 자동 비활성.")]
    [SerializeField] private CardView[] deckSlots;

    [Header("Buttons")]
    [SerializeField] private Button exitButton;

    [Header("Debug")]
    [Tooltip("0 이상이면 Show() 호출 시 RunState.Gold 를 이 값으로 덮어씀(테스트용). 음수면 비활성.")]
    [SerializeField] private int debugForceGold = -1;

    private Action onConfirm;
    private readonly List<MercenaryPoolSO.PaidEntry> picked = new List<MercenaryPoolSO.PaidEntry>();
    private bool[] bought;
    private int selectedCandidateIdx = -1;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (candidateSlots != null)
            for (int i = 0; i < candidateSlots.Length; i++)
                if (candidateSlots[i] != null) candidateSlots[i].OnClicked += HandleCandidateClicked;
        if (deckSlots != null)
            for (int i = 0; i < deckSlots.Length; i++)
                if (deckSlots[i] != null) deckSlots[i].OnClicked += HandleDeckClicked;
        if (exitButton != null) exitButton.onClick.AddListener(HandleExit);
    }

    private void OnDestroy()
    {
        if (candidateSlots != null)
            for (int i = 0; i < candidateSlots.Length; i++)
                if (candidateSlots[i] != null) candidateSlots[i].OnClicked -= HandleCandidateClicked;
        if (deckSlots != null)
            for (int i = 0; i < deckSlots.Length; i++)
                if (deckSlots[i] != null) deckSlots[i].OnClicked -= HandleDeckClicked;
        if (exitButton != null) exitButton.onClick.RemoveListener(HandleExit);
    }

    public override bool CanRun()
    {
        if (pool == null || pool.entries == null || pool.entries.Length == 0) return false;
        if (RunState.PlayerDeck == null || RunState.PlayerDeck.Count == 0) return false;
        return true;
    }

    public override void Show(Action onConfirmed)
    {
        onConfirm = onConfirmed;
        if (debugForceGold >= 0) RunState.SetGold(debugForceGold);
        PickCandidates();
        bought = new bool[picked.Count];
        selectedCandidateIdx = -1;
        if (titleText != null) titleText.text = title;
        RefreshAll();
        if (root != null) root.SetActive(true);
    }

    /// <summary>풀에서 중복 없이 candidateCount(보통 3)장 픽. 풀이 부족하면 가능한 만큼만.</summary>
    private void PickCandidates()
    {
        picked.Clear();
        if (pool == null || pool.entries == null) return;
        int n = Mathf.Min(candidateCount, pool.entries.Length);
        var indices = new List<int>(pool.entries.Length);
        for (int i = 0; i < pool.entries.Length; i++) indices.Add(i);
        for (int i = 0; i < n; i++)
        {
            int j = UnityEngine.Random.Range(i, indices.Count);
            (indices[i], indices[j]) = (indices[j], indices[i]);
            picked.Add(pool.entries[indices[i]]);
        }
    }

    private void RefreshAll()
    {
        RefreshBodyText();
        RefreshGoldText();
        RefreshCandidates();
        RefreshDeck();
    }

    private void RefreshBodyText()
    {
        if (bodyText == null) return;
        bodyText.text = (selectedCandidateIdx >= 0) ? bodyAfterCandidatePicked : body;
    }

    private void RefreshGoldText()
    {
        if (goldText != null) goldText.text = string.Format(goldFormat, RunState.Gold);
    }

    private void RefreshCandidates()
    {
        if (candidateSlots == null) return;
        for (int i = 0; i < candidateSlots.Length; i++)
        {
            var view = candidateSlots[i];
            if (view == null) continue;
            bool slotHasEntry = i < picked.Count;
            bool isBought = bought != null && i < bought.Length && bought[i];
            if (!slotHasEntry || isBought)
            {
                view.gameObject.SetActive(false);
                if (candidatePriceTexts != null && i < candidatePriceTexts.Length && candidatePriceTexts[i] != null)
                    candidatePriceTexts[i].gameObject.SetActive(false);
                continue;
            }
            var entry = picked[i];
            bool canAfford = RunState.Gold >= entry.price;
            view.BindPreview(entry.card, 0, 0, canAfford);
            view.SetHighlight(selectedCandidateIdx == i);

            if (candidatePriceTexts != null && i < candidatePriceTexts.Length && candidatePriceTexts[i] != null)
            {
                candidatePriceTexts[i].gameObject.SetActive(true);
                candidatePriceTexts[i].text = string.Format(priceFormat, entry.price);
            }
        }
    }

    private void RefreshDeck()
    {
        if (deckSlots == null) return;
        var deck = RunState.PlayerDeck;
        int deckCount = deck != null ? deck.Count : 0;
        bool candidatePicked = selectedCandidateIdx >= 0 && selectedCandidateIdx < picked.Count;
        for (int i = 0; i < deckSlots.Length; i++)
        {
            var view = deckSlots[i];
            if (view == null) continue;
            if (i >= deckCount)
            {
                view.gameObject.SetActive(false);
                continue;
            }
            int hpBonus = RunState.GlobalHpBonus + RunState.GetPerCardHpBonus(i);
            int skillBonus = RunState.GetPerCardSkillBonus(i);
            view.BindPreview(deck[i], hpBonus, skillBonus, candidatePicked);
            view.SetHighlight(false);
        }
    }

    private void HandleCandidateClicked(CardView view)
    {
        int idx = IndexOf(candidateSlots, view);
        if (idx < 0 || idx >= picked.Count) return;
        if (bought != null && idx < bought.Length && bought[idx]) return;
        if (RunState.Gold < picked[idx].price) return;
        selectedCandidateIdx = (selectedCandidateIdx == idx) ? -1 : idx;
        RefreshAll();
    }

    private void HandleDeckClicked(CardView view)
    {
        int idx = IndexOf(deckSlots, view);
        if (idx < 0 || idx >= RunState.PlayerDeck.Count) return;
        if (selectedCandidateIdx < 0 || selectedCandidateIdx >= picked.Count) return;
        var entry = picked[selectedCandidateIdx];
        if (RunState.Gold < entry.price) return;
        if (!RunState.SpendGold(entry.price)) return;
        RunState.ReplaceCardAt(idx, entry.card);
        bought[selectedCandidateIdx] = true;
        selectedCandidateIdx = -1;
        RefreshAll();
    }

    private void HandleExit()
    {
        if (root != null) root.SetActive(false);
        var cb = onConfirm;
        onConfirm = null;
        cb?.Invoke();
    }

    private static int IndexOf(CardView[] arr, CardView v)
    {
        if (arr == null) return -1;
        for (int i = 0; i < arr.Length; i++) if (arr[i] == v) return i;
        return -1;
    }
}
