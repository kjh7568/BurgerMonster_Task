using System.Collections.Generic;
using UnityEngine;

public class BattleSceneUI : MonoBehaviour
{
    [SerializeField] private BattleController battle;
    [SerializeField] private CardView[] opponentFieldSlots;
    [SerializeField] private GameObject cardViewPrefab;
    [SerializeField] private RectTransform[] playerSlots;
    [SerializeField] private RectTransform[] opponentSlots;

    private CardView[] playerViews;
    private CardView[] opponentViews;

    public IReadOnlyList<CardView> PlayerViews => playerViews;
    public IReadOnlyList<CardView> OpponentViews => opponentViews;

    private void Start()
    {
        playerViews = SpawnSide(battle.Player, playerSlots);
        opponentViews = SpawnSide(battle.Opponent, opponentSlots);
        
        if (battle == null)
        {
            Debug.LogWarning("[BattleSceneUI] battle 참조가 비어있음. 인스펙터에서 연결 필요.");
            return;
        }

        battle.OnStateChanged += HandleStateChanged;

        if (opponentFieldSlots != null)
        {
            foreach (var v in opponentFieldSlots)
            {
                if (v == null) continue;
                v.OnClicked += HandleOpponentClicked;
            }
        }

        ClearHighlights();
    }

    private void OnDestroy()
    {
        if (battle != null) battle.OnStateChanged -= HandleStateChanged;

        if (opponentFieldSlots != null)
        {
            foreach (var v in opponentFieldSlots)
            {
                if (v == null) continue;
                v.OnClicked -= HandleOpponentClicked;
            }
        }
    }
    
    private CardView[] SpawnSide(Side side, RectTransform[] slots)
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
            var card = i < side.field.Length ? side.field[i] : null;
            view.Bind(side, i, card);
            views[i] = view;
        }
        return views;
    }
    
    private void HandleStateChanged(BattleState s)
    {
        if (s == BattleState.AwaitTargetSelect)
        {
            HighlightValidTargets(battle.PendingAttackerIdx);
        }
        else
        {
            ClearHighlights();
        }
    }

    /// <summary>
    /// 공격자 슬롯의 스킬을 기준으로 유효 대상만 외곽선+상호작용 활성. 빈 슬롯/이상 인덱스가 들어오면 전체 해제.
    /// </summary>
    private void HighlightValidTargets(int attackerIdx)
    {
        if (opponentFieldSlots == null) return;
        if (battle == null) { ClearHighlights(); return; }
        if (attackerIdx < 0 || attackerIdx >= battle.Player.field.Length) { ClearHighlights(); return; }

        var attacker = battle.Player.field[attackerIdx];
        if (attacker == null || attacker.IsDead) { ClearHighlights(); return; }

        var ctx = battle.BuildContext(battle.Player, attackerIdx);
        var skill = SkillFactory.Create(attacker.data.type);
        var valid = new HashSet<int>(skill.GetValidTargets(ctx));

        for (int i = 0; i < opponentFieldSlots.Length; i++)
        {
            var slot = opponentFieldSlots[i];
            if (slot == null) continue;
            bool on = valid.Contains(i);
            slot.SetHighlight(on);
            slot.SetInteractable(on);
        }
    }

    private void ClearHighlights()
    {
        if (opponentFieldSlots == null) return;
        foreach (var v in opponentFieldSlots)
        {
            if (v == null) continue;
            v.SetHighlight(false);
            v.SetInteractable(false);
        }
    }

    private void HandleOpponentClicked(CardView v)
    {
        if (battle == null || v == null) return;
        if (battle.State != BattleState.AwaitTargetSelect) return;
        battle.ExecutePlayerAction(v.SlotIndex);
    }
}
