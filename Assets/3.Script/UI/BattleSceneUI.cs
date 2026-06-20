using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 양 진영 field 3슬롯(앞면) + standby 3슬롯(뒷면) 총 12장의 CardView를 인스턴스화하고 Bind한다.
/// 추가로 플레이어 입력 라우터 역할:
///   - 아군 카드 탭 → 액션 오버레이 토글
///   - 공격 버튼 → 적 field 대상 선택
///   - 스킬 버튼 → Skill.TargetMode 에 따라 즉시 발동(None) / 적 대상 선택(Enemy) / 아군 대상 선택(Ally)
///   - 적/아군 카드 탭 → 대상 선택 단계에서 ExecutePlayerAction 라우팅
/// 그 외 게임 상태 이벤트(턴 시작·데미지·사망·자동 배치)를 받아 CardView 갱신.
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
            v.OnAttackPressed += HandleAttackPressed;
            v.OnSkillPressed += HandleSkillPressed;
        }

        foreach (var v in opponentFieldViews)
        {
            if (v == null) continue;
            v.OnClicked += HandleEnemyFieldClicked;
            v.SetInteractable(false); // 평소엔 클릭 안 받음. AwaitTargetSelect 진입 시 valid만 켬.
        }

        battle.OnTurnStarting += HandleTurnStarting;
        battle.OnStateChanged += HandleStateChanged;
        battle.Resolver.OnDamageDealt += HandleDamageDealt;
        battle.Resolver.OnCardDied += HandleCardDied;
        battle.Resolver.OnCardSpawned += HandleCardSpawned;
    }

    private void OnDestroy()
    {
        if (battle != null)
        {
            battle.OnTurnStarting -= HandleTurnStarting;
            battle.OnStateChanged -= HandleStateChanged;
            if (battle.Resolver != null)
            {
                battle.Resolver.OnDamageDealt -= HandleDamageDealt;
                battle.Resolver.OnCardDied -= HandleCardDied;
                battle.Resolver.OnCardSpawned -= HandleCardSpawned;
            }
        }

        if (playerFieldViews != null)
        {
            foreach (var v in playerFieldViews)
            {
                if (v == null) continue;
                v.OnClicked -= HandlePlayerCardClicked;
                v.OnAttackPressed -= HandleAttackPressed;
                v.OnSkillPressed -= HandleSkillPressed;
            }
        }

        if (opponentFieldViews != null)
        {
            foreach (var v in opponentFieldViews)
            {
                if (v == null) continue;
                v.OnClicked -= HandleEnemyFieldClicked;
            }
        }
    }

    /// <summary>
    /// 플레이어 field 카드 클릭. 상황별 분기:
    ///   - AwaitTargetSelect + 아군 타깃 스킬 진행 중: 클릭한 아군을 대상으로 ExecutePlayerAction.
    ///   - AwaitTargetSelect + 그 외: 취소 후 새 선택 흐름.
    ///   - AwaitCardSelect: 액션 오버레이 토글(같은 카드 닫기 / 다른 카드 전환).
    /// </summary>
    private void HandlePlayerCardClicked(CardView v)
    {
        if (battle == null) return;

        if (battle.State == BattleState.AwaitTargetSelect)
        {
            if (IsAllyTargetSkillInProgress() && v != null && v.Bound != null && !v.Bound.IsDead)
            {
                battle.ExecutePlayerAction(v.SlotIndex);
                return;
            }
            battle.CancelTargetSelect();
        }

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

    private bool IsAllyTargetSkillInProgress()
    {
        if (battle == null) return false;
        if (battle.PendingActionKind != PendingActionKind.Skill) return false;
        int idx = battle.PendingAttackerIdx;
        if (idx < 0 || idx >= battle.Player.field.Length) return false;
        var attacker = battle.Player.field[idx];
        if (attacker == null || attacker.Skill == null) return false;
        return attacker.Skill.TargetMode == SkillTargetMode.Ally;
    }

    private void CloseOpened()
    {
        if (openedCard != null) openedCard.HideActionPanel();
        openedCard = null;
    }

    /// <summary>공격 버튼 → 적 대상 선택 단계 진입.</summary>
    private void HandleAttackPressed(CardView v)
    {
        if (v == null || v.Bound == null || v.Bound.IsDead) return;
        if (battle == null) return;
        if (battle.State != BattleState.AwaitCardSelect && battle.State != BattleState.AwaitActionSelect) return;

        CloseOpened();
        battle.EnterTargetSelect(v.SlotIndex, PendingActionKind.Attack);
    }

    /// <summary>스킬 버튼 → Skill.TargetMode 따라 즉시 발동 또는 대상 선택 진입.</summary>
    private void HandleSkillPressed(CardView v)
    {
        if (v == null || v.Bound == null || v.Bound.IsDead) return;
        if (battle == null) return;
        if (battle.State != BattleState.AwaitCardSelect && battle.State != BattleState.AwaitActionSelect) return;

        var skill = v.Bound.Skill;
        if (skill == null || !skill.IsActive || v.Bound.SkillUsed) return;

        CloseOpened();
        if (skill.TargetMode == SkillTargetMode.None)
            battle.ExecutePlayerSkillImmediate(v.SlotIndex);
        else
            battle.EnterTargetSelect(v.SlotIndex, PendingActionKind.Skill);
    }

    /// <summary>적 field 슬롯 클릭 → AwaitTargetSelect 일 때만 라우팅.</summary>
    private void HandleEnemyFieldClicked(CardView v)
    {
        if (battle == null) return;
        if (battle.State != BattleState.AwaitTargetSelect) return;
        if (v == null || v.Bound == null || v.Bound.IsDead) return;

        battle.ExecutePlayerAction(v.SlotIndex);
    }

    /// <summary>
    /// 상태 전이 훅. AwaitTargetSelect 진입 시 PendingActionKind 에 따라 적 또는 아군 진영을 하이라이트.
    /// 그 외 상태 진입 시 모두 클리어.
    /// </summary>
    private void HandleStateChanged(BattleState s)
    {
        if (s == BattleState.AwaitTargetSelect)
            HighlightValidTargets(battle.PendingAttackerIdx);
        else
            ClearHighlights();
    }

    /// <summary>
    /// 공격이면 적 field, 스킬이면 Skill.TargetMode 에 따라 적/아군 field 의 valid 인덱스만 하이라이트.
    /// </summary>
    private void HighlightValidTargets(int attackerIdx)
    {
        if (battle == null) { ClearHighlights(); return; }
        if (attackerIdx < 0 || attackerIdx >= battle.Player.field.Length) { ClearHighlights(); return; }

        var attacker = battle.Player.field[attackerIdx];
        if (attacker == null || attacker.IsDead) { ClearHighlights(); return; }

        var ctx = battle.BuildContext(battle.Player, attackerIdx);

        HashSet<int> valid;
        CardView[] targetViews;
        bool targetIsEnemy;

        if (battle.PendingActionKind == PendingActionKind.Attack)
        {
            valid = new HashSet<int>(attacker.Attack.GetValidTargets(ctx));
            targetViews = opponentFieldViews;
            targetIsEnemy = true;
        }
        else
        {
            var skill = attacker.Skill;
            valid = new HashSet<int>(skill.GetValidTargets(ctx));
            targetViews = skill.TargetMode == SkillTargetMode.Ally ? playerFieldViews : opponentFieldViews;
            targetIsEnemy = skill.TargetMode != SkillTargetMode.Ally;
        }

        // 양쪽 모두 일단 클리어한 뒤 대상 진영에만 적용.
        ClearHighlightsInternal();

        if (targetViews == null) return;
        for (int i = 0; i < targetViews.Length; i++)
        {
            var view = targetViews[i];
            if (view == null) continue;
            bool on = valid.Contains(i);
            view.SetHighlight(on);
            if (targetIsEnemy) view.SetInteractable(on);
            // 아군 타깃: 플레이어 카드 interactable 은 그대로 유지(이미 true) — HandlePlayerCardClicked가 분기.
        }
    }

    private void ClearHighlights() => ClearHighlightsInternal();

    private void ClearHighlightsInternal()
    {
        if (opponentFieldViews != null)
        {
            foreach (var view in opponentFieldViews)
            {
                if (view == null) continue;
                view.SetHighlight(false);
                view.SetInteractable(false);
            }
        }
        if (playerFieldViews != null)
        {
            foreach (var view in playerFieldViews)
            {
                if (view == null) continue;
                view.SetHighlight(false);
                // 플레이어 interactable 은 건드리지 않음(평소 항상 활성).
            }
        }
    }

    private void HandleTurnStarting(Side side) => RefreshAllField();

    private void HandleDamageDealt(Side side, int slotIdx, int amount)
    {
        var view = GetFieldView(side, slotIdx);
        if (view != null) view.Refresh();
    }

    private void HandleCardDied(Side side, int slotIdx, CardInstance dyingCard)
    {
        var view = GetFieldView(side, slotIdx);
        if (view != null) view.Refresh();
    }

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

    /// <summary>외부(AIController 등)에서 임의 진영·슬롯의 field CardView 하이라이트를 토글.</summary>
    public void SetFieldHighlight(Side side, int slotIdx, bool on)
    {
        var view = GetFieldView(side, slotIdx);
        if (view != null) view.SetHighlight(on);
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
