using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대상 선택 단계의 UI 책임. AwaitTargetSelect 진입 시 적 field 슬롯 중 현재 PendingAttacker의 스킬이 valid로 본 슬롯에만 노란 외곽선+상호작용 활성, 나머지는 비활성. 상태가 바뀌면 즉시 해제.
/// 적 standby 슬롯은 뒷면이라 대상에서 자체적으로 제외(필드 호출 자체 안 함).
/// </summary>
public class BattleSceneUI : MonoBehaviour
{
    [SerializeField] private BattleController battle;
    [SerializeField] private CardView[] opponentFieldSlots;

    private void Start()
    {
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
