using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 양 진영 field 3슬롯(앞면) + standby 3슬롯(뒷면) 총 12장의 CardView를 인스턴스화하고 Bind한다.
/// 추가로 플레이어 field 카드의 클릭/공격/스킬 입력을 받아 액션 오버레이 토글을 관장한다.
/// 한 번에 한 카드의 오버레이만 켜져 있고, 같은 카드 재클릭은 닫기, 다른 카드 클릭은 전환.
/// 공격/스킬 버튼은 오버레이만 닫고 이벤트 발화는 CardView가 직접 — 후속 작업(대상 선택)이 그 이벤트를 구독해서 흐름을 이어받는다.
/// standby→field 이동, 대상 선택, ExecutePlayerAction 호출은 모두 후속 작업의 책임.
/// </summary>
public class BattleSceneUI : MonoBehaviour
{
    [SerializeField] private BattleController battle;
    [SerializeField] private GameObject cardViewPrefab;

    [Header("Player Slots")]
    [SerializeField] private RectTransform[] playerFieldSlots;
    [SerializeField] private RectTransform[] playerStandbySlots;

    [Header("Opponent Slots")]
    [SerializeField] private RectTransform[] opponentFieldSlots;
    [SerializeField] private RectTransform[] opponentStandbySlots;

    private CardView[] playerFieldViews;
    private CardView[] playerStandbyViews;
    private CardView[] opponentFieldViews;
    private CardView[] opponentStandbyViews;

    private CardView openedCard;

    public IReadOnlyList<CardView> PlayerFieldViews => playerFieldViews;
    public IReadOnlyList<CardView> PlayerStandbyViews => playerStandbyViews;
    public IReadOnlyList<CardView> OpponentFieldViews => opponentFieldViews;
    public IReadOnlyList<CardView> OpponentStandbyViews => opponentStandbyViews;

    private void Start()
    {
        if (battle == null)
        {
            Debug.LogWarning("[BattleSceneUI] battle 참조가 비어있음. 인스펙터에서 연결 필요.");
            return;
        }

        playerFieldViews = SpawnRow(battle.Player, battle.Player.field, playerFieldSlots, faceUp: true);
        playerStandbyViews = SpawnRow(battle.Player, battle.Player.standby, playerStandbySlots, faceUp: false);
        opponentFieldViews = SpawnRow(battle.Opponent, battle.Opponent.field, opponentFieldSlots, faceUp: true);
        opponentStandbyViews = SpawnRow(battle.Opponent, battle.Opponent.standby, opponentStandbySlots, faceUp: false);

        foreach (var v in playerFieldViews)
        {
            if (v == null) continue;
            v.OnClicked += HandlePlayerCardClicked;
            v.OnAttackPressed += HandleActionPressed;
            v.OnSkillPressed += HandleActionPressed;
        }

        battle.OnTurnStarting += HandleTurnStarting;
        battle.Resolver.OnDamageDealt += HandleDamageDealt;
        battle.Resolver.OnCardDied += HandleCardDied;
        battle.Resolver.OnCardSpawned += HandleCardSpawned;
    }

    private void OnDestroy()
    {
        if (battle != null)
        {
            battle.OnTurnStarting -= HandleTurnStarting;
            if (battle.Resolver != null)
            {
                battle.Resolver.OnDamageDealt -= HandleDamageDealt;
                battle.Resolver.OnCardDied -= HandleCardDied;
                battle.Resolver.OnCardSpawned -= HandleCardSpawned;
            }
        }

        if (playerFieldViews == null) return;
        foreach (var v in playerFieldViews)
        {
            if (v == null) continue;
            v.OnClicked -= HandlePlayerCardClicked;
            v.OnAttackPressed -= HandleActionPressed;
            v.OnSkillPressed -= HandleActionPressed;
        }
    }

    /// <summary>
    /// 플레이어 field 카드 클릭 시 액션 오버레이 토글.
    /// 선택 불가 카드(죽음/비-플레이어/뒷면)거나 AwaitCardSelect 상태가 아니면 무시.
    /// 같은 카드 재클릭은 닫기, 다른 카드 클릭은 이전 닫고 새로 열기.
    /// </summary>
    private void HandlePlayerCardClicked(CardView v)
    {
        if (!IsSelectable(v)) return;

        if (openedCard == v)
        {
            CloseOpened();
            return;
        }

        if (openedCard != null) openedCard.HideActionPanel();
        v.ShowActionPanel();
        openedCard = v;
    }

    private bool IsSelectable(CardView v)
    {
        if (v == null || v.Bound == null || v.Bound.IsDead) return false;
        if (v.OwningSide == null || !v.OwningSide.isPlayer) return false;
        if (!v.IsFaceUp) return false;
        if (battle != null && battle.State != BattleState.AwaitCardSelect) return false;
        return true;
    }

    private void CloseOpened()
    {
        if (openedCard != null) openedCard.HideActionPanel();
        openedCard = null;
    }

    /// <summary>
    /// 공격/스킬 버튼 클릭. 현재 작업 범위에선 오버레이만 닫고 끝낸다.
    /// 후속 작업이 CardView.OnAttackPressed/OnSkillPressed를 직접 구독해 대상 선택 단계로 진입시킨다.
    /// </summary>
    private void HandleActionPressed(CardView v)
    {
        CloseOpened();
    }

    /// <summary>
    /// 턴 시작 직전 호출. 힐러 회복 등 TurnStartEffects가 곧 HP를 변경하므로 양 진영 모든 field 카드를 일괄 Refresh.
    /// </summary>
    private void HandleTurnStarting(Side side)
    {
        RefreshAllField();
    }

    private void HandleDamageDealt(Side side, int slotIdx, int amount)
    {
        var view = GetFieldView(side, slotIdx);
        if (view != null) view.Refresh();
    }

    private void HandleCardDied(Side side, int slotIdx, CardInstance dyingCard)
    {
        // OnCardDied는 field[slotIdx] = null 직전에 발화 — 죽은 카드 상태로 한 번 보여줘서 deadOverlay 노출.
        var view = GetFieldView(side, slotIdx);
        if (view != null) view.Refresh();
    }

    /// <summary>
    /// standby의 한 장이 field 빈 슬롯으로 자동 배치됐을 때 호출.
    /// 1) field 슬롯 CardView를 새 인스턴스로 Rebind(앞면).
    /// 2) 비워진 standby 슬롯 CardView를 null Bind 해 사라지게 함.
    /// </summary>
    private void HandleCardSpawned(Side side, int fieldSlot, CardInstance newCard, int fromStandbyIdx)
    {
        var fieldView = GetFieldView(side, fieldSlot);
        if (fieldView != null) fieldView.Bind(side, fieldSlot, newCard, faceUp: true);

        var standbyView = GetStandbyView(side, fromStandbyIdx);
        if (standbyView != null) standbyView.Bind(side, fromStandbyIdx, null, faceUp: false);
    }

    private void RefreshAllField()
    {
        if (playerFieldViews != null) foreach (var v in playerFieldViews) v?.Refresh();
        if (opponentFieldViews != null) foreach (var v in opponentFieldViews) v?.Refresh();
    }

    private CardView GetFieldView(Side side, int slotIdx)
    {
        var arr = side != null && side.isPlayer ? playerFieldViews : opponentFieldViews;
        if (arr == null || slotIdx < 0 || slotIdx >= arr.Length) return null;
        return arr[slotIdx];
    }

    private CardView GetStandbyView(Side side, int slotIdx)
    {
        var arr = side != null && side.isPlayer ? playerStandbyViews : opponentStandbyViews;
        if (arr == null || slotIdx < 0 || slotIdx >= arr.Length) return null;
        return arr[slotIdx];
    }

    /// <summary>
    /// slots 각각에 cardViewPrefab을 자식으로 인스턴스화하고 슬롯 전체를 채우도록 RectTransform을 stretch 처리.
    /// cards 범위를 넘는 인덱스는 null로 Bind해 CardView가 스스로 비활성화하도록 둔다.
    /// </summary>
    private CardView[] SpawnRow(Side side, CardInstance[] cards, RectTransform[] slots, bool faceUp)
    {
        var views = new CardView[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            var go = Instantiate(cardViewPrefab, slots[i]);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            var view = go.GetComponent<CardView>();
            var card = i < cards.Length ? cards[i] : null;
            view.Bind(side, i, card, faceUp);
            views[i] = view;
        }
        return views;
    }
}
