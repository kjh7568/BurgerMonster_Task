using UnityEngine;

/// <summary>
/// BattleController 의 상태 전이를 듣고 안정 체크포인트(AwaitCardSelect)와 종료 시점에 SaveBridge 를 호출.
/// BattleScene 루트에 BattleController 와 같이 두면 된다.
/// </summary>
[RequireComponent(typeof(BattleController))]
public class BattleSaveTrigger : MonoBehaviour
{
    [SerializeField] private BattleController battle;
    private bool subscribed;

    private void Awake()
    {
        if (battle == null) battle = GetComponent<BattleController>();
    }

    private void OnEnable()
    {
        if (battle == null) return;
        battle.OnStateChanged += HandleStateChanged;
        battle.OnGameEnded += HandleGameEnded;
        subscribed = true;
    }

    private void OnDisable()
    {
        if (!subscribed || battle == null) return;
        battle.OnStateChanged -= HandleStateChanged;
        battle.OnGameEnded -= HandleGameEnded;
        subscribed = false;
    }

    private void HandleStateChanged(BattleState s)
    {
        if (s == BattleState.AwaitCardSelect)
            SaveBridge.SaveBattleCheckpoint(battle);
    }

    private void HandleGameEnded(Side winner)
    {
        if (winner == null) return;
        // 승리 시 저장은 ResultUI 가 AdvanceNode 직후에 호출 — 여기선 패배 케이스만 처리.
        if (!winner.isPlayer) SaveBridge.ClearOnDefeat();
    }
}
