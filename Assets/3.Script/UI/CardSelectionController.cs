using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 카드 슬롯들의 클릭을 받아 액션 오버레이를 토글한다.
/// 한 번에 한 카드의 패널만 켜져 있고, 같은 카드 재클릭은 닫기, 다른 카드 클릭은 전환.
/// 공격/스킬 버튼 클릭은 현재 단계에서는 로그만 출력 — 대상 선택/실행은 후속 작업에서 이벤트 구독.
/// CardView는 자기 표시/클릭만 알고, 누구의 어떤 카드가 어떤 조건에서 선택될 수 있는지 판정은 여기서 한다(SRP).
/// </summary>
public class CardSelectionController : MonoBehaviour
{
    [SerializeField] private BattleController battle;
    [SerializeField] private List<CardView> playerSlots = new();

    private CardView opened;

    private void Awake()
    {
        foreach (var v in playerSlots)
        {
            if (v == null) continue;
            v.OnClicked += HandleCardClicked;
            v.OnAttackPressed += HandleAttackPressed;
            v.OnSkillPressed += HandleSkillPressed;
        }
    }

    private void OnDestroy()
    {
        foreach (var v in playerSlots)
        {
            if (v == null) continue;
            v.OnClicked -= HandleCardClicked;
            v.OnAttackPressed -= HandleAttackPressed;
            v.OnSkillPressed -= HandleSkillPressed;
        }
    }

    private void HandleCardClicked(CardView v)
    {
        if (!IsSelectable(v)) return;

        if (opened == v)
        {
            CloseOpened();
            return;
        }

        if (opened != null) opened.HideActionPanel();
        v.ShowActionPanel();
        opened = v;
    }

    /// <summary>
    /// 클릭한 카드가 선택 가능한지: 카드가 묶여 있고, 살아 있고, 플레이어 진영 소속이며,
    /// (BattleController가 연결돼 있다면) AwaitCardSelect 단계여야 한다.
    /// </summary>
    private bool IsSelectable(CardView v)
    {
        if (v == null || v.Bound == null || v.Bound.IsDead) return false;
        if (v.OwningSide == null || !v.OwningSide.isPlayer) return false;
        if (battle != null && battle.State != BattleState.AwaitCardSelect) return false;
        return true;
    }

    private void CloseOpened()
    {
        if (opened != null) opened.HideActionPanel();
        opened = null;
    }

    private void HandleAttackPressed(CardView v)
    {
        Debug.Log($"[CardSelection] Attack pressed on slot {v.SlotIndex}");
    }

    private void HandleSkillPressed(CardView v)
    {
        Debug.Log($"[CardSelection] Skill pressed on slot {v.SlotIndex}");
    }
}
