using TMPro;
using UnityEngine;

/// <summary>
/// 현재 턴 번호와 누구 차례인지 한 줄로 표시. BattleController.OnTurnStarted를 구독해 자동 갱신.
/// 색상은 진영별로 다르게 줘서 한눈에 구분 가능.
/// </summary>
public class TurnIndicator : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private BattleController battle;
    [SerializeField] private Color playerColor = new Color(0.4f, 0.85f, 1f);
    [SerializeField] private Color opponentColor = new Color(1f, 0.45f, 0.45f);

    private void Start()
    {
        if (battle == null)
        {
            Debug.LogWarning("[TurnIndicator] battle 참조가 비어있음. 인스펙터에서 연결 필요.");
            return;
        }

        battle.OnTurnStarted += HandleTurnStarted;
        // BattleController.Start가 DefaultExecutionOrder(-100)이라 첫 PlayerTurnStart 이벤트는 본 Start 전에 이미 발화됨.
        // 따라서 현재 CurrentSide 기반으로 1회 수동 갱신.
        HandleTurnStarted(battle.CurrentSide);
    }

    private void OnDestroy()
    {
        if (battle != null) battle.OnTurnStarted -= HandleTurnStarted;
    }

    private void HandleTurnStarted(Side side)
    {
        if (label == null || side == null) return;
        label.text = $"TURN {battle.TurnNumber}";
        label.color = side.isPlayer ? playerColor : opponentColor;
    }
}
