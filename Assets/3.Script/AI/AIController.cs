using System.Collections;
using UnityEngine;

/// <summary>
/// 상대 턴 자동 진행 + 시각 딜레이 오케스트레이터.
/// OnStateChanged(OpponentTurnAction)을 받으면 IAIStrategy로 (attacker, target) 결정 후 코루틴 실행:
///   딜레이 → attacker 하이라이트 → 짧은 딜레이 → target 하이라이트 → 짧은 딜레이
///   → 하이라이트 해제 → ExecuteOpponentAction(스킬 실행 + 턴 종료).
/// AI 결정이 null이면 행동 없이 EndCurrentTurn으로 턴을 넘긴다.
/// </summary>
public class AIController : MonoBehaviour
{
    [SerializeField] private BattleController battle;
    [SerializeField] private BattleSceneUI sceneUI;
    [SerializeField] private bool useHeuristic = true;
    [SerializeField] private float highlightStepDelay = 0.4f;

    private IAIStrategy strategy;
    private bool turnInProgress;

    private void Start()
    {
        if (battle == null)
        {
            Debug.LogWarning("[AIController] battle 참조 누락. 인스펙터에서 연결 필요.");
            return;
        }

        strategy = useHeuristic ? new HeuristicAIStrategy() : (IAIStrategy)new RandomAIStrategy();
        battle.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (battle != null) battle.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(BattleState s)
    {
        if (s != BattleState.OpponentTurnAction) return;
        if (turnInProgress) return;
        StartCoroutine(RunTurn());
    }

    private IEnumerator RunTurn()
    {
        turnInProgress = true;
        float lead = battle.Config != null ? battle.Config.opponentActionDelay : 0.8f;
        yield return new WaitForSeconds(lead);

        var decision = strategy.Decide(battle);
        if (decision.HasValue)
        {
            var (atk, tgt) = decision.Value;
            Debug.Log($"[AI] decide attacker={atk} target={tgt} (strategy={(useHeuristic ? "Heuristic" : "Random")})");

            if (sceneUI != null) sceneUI.SetFieldHighlight(battle.Opponent, atk, true);
            yield return new WaitForSeconds(highlightStepDelay);

            if (sceneUI != null) sceneUI.SetFieldHighlight(battle.Player, tgt, true);
            yield return new WaitForSeconds(highlightStepDelay);

            if (sceneUI != null)
            {
                sceneUI.SetFieldHighlight(battle.Opponent, atk, false);
                sceneUI.SetFieldHighlight(battle.Player, tgt, false);
            }

            battle.ExecuteOpponentAction(atk, tgt);
        }
        else
        {
            Debug.Log("[AI] no valid action — passing turn.");
            battle.EndCurrentTurn();
        }

        turnInProgress = false;
    }
}
