using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// BattleController.OnGameEnded 를 구독해 결과 패널을 띄우고, 승리 시 RunState 진행 + MapScene 로드,
/// 패배 시 같은 노드를 재도전(BattleScene 재로드)한다. RunState.CurrentMap 이 비어 있으면(단독 실행) MapScene 으로 가서 Run 을 시작.
///
/// 단, 승리 노드가 지도의 마지막 노드라면 일반 VICTORY 대신 0.5초 뒤 ExpeditionEndPanel 을 띄운다(원정 종료).
/// </summary>
public class ResultUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmLabel;
    [SerializeField] private BattleController battle;
    [Tooltip("원정 최종 노드 승리 시 일반 결과 패널 대신 띄울 패널. 비워두면 이 분기는 무시되고 기존 흐름으로 폴백.")]
    [SerializeField] private ExpeditionEndPanel expeditionEndPanel;
    [Tooltip("마지막 몬스터 처치 후 ExpeditionEndPanel 이 뜨기까지 대기 시간(초).")]
    [SerializeField] private float expeditionEndDelay = 0.5f;

    [Header("Result Colors")]
    [SerializeField] private Color victoryColor = new Color(0.4f, 0.95f, 0.4f);
    [SerializeField] private Color defeatColor = new Color(0.95f, 0.4f, 0.4f);

    [Header("Result Text")]
    [SerializeField] private string victoryLabel = "VICTORY";
    [SerializeField] private string defeatLabel = "DEFEAT";
    [SerializeField] private string toMapLabel = "맵으로 돌아가기";
    [SerializeField] private string toTitleLabel = "타이틀로 돌아가기";

    private bool playerWon;
    private bool subscribed;

    private void Awake()
    {
        // 패널 자식 활성화 타이밍과 무관하게 리스너를 미리 걸어둔다.
        if (panel != null) panel.SetActive(false);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        else Debug.LogError("[ResultUI] confirmButton 참조 누락 — 결과 패널 버튼이 동작하지 않음.");

        if (battle == null) battle = FindObjectOfType<BattleController>();
        if (battle != null)
        {
            battle.OnGameEnded += HandleGameEnded;
            subscribed = true;
        }
        else Debug.LogError("[ResultUI] BattleController 참조 누락 — 결과 패널이 뜨지 않음.");
    }

    private void OnDestroy()
    {
        if (subscribed && battle != null) battle.OnGameEnded -= HandleGameEnded;
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirm);
    }

    private void HandleGameEnded(Side winner)
    {
        playerWon = winner != null && winner.isPlayer;
        Debug.Log($"[ResultUI] HandleGameEnded — playerWon={playerWon}");

        // 마지막 노드 승리 = 원정 종료. 일반 결과 패널 대신 0.5초 뒤 ExpeditionEndPanel.
        if (playerWon && IsLastNode() && expeditionEndPanel != null)
        {
            StartCoroutine(ShowExpeditionEndAfterDelay());
            return;
        }

        if (panel != null) panel.SetActive(true);
        if (resultText != null)
        {
            resultText.text = playerWon ? victoryLabel : defeatLabel;
            resultText.color = playerWon ? victoryColor : defeatColor;
        }
        if (confirmLabel != null) confirmLabel.text = playerWon ? toMapLabel : toTitleLabel;
    }

    /// <summary>현재 RunState.CurrentNodeIndex 가 지도의 마지막 노드인지. CurrentMap 미설정이면 false.</summary>
    private static bool IsLastNode()
    {
        var map = RunState.CurrentMap;
        if (map == null || map.nodes == null || map.nodes.Count == 0) return false;
        return RunState.CurrentNodeIndex == map.nodes.Count - 1;
    }

    private IEnumerator ShowExpeditionEndAfterDelay()
    {
        yield return new WaitForSeconds(expeditionEndDelay);
        // hasClearedRun 이 false 면 이번이 첫 클리어. 패널이 자체적으로 EndingScene 으로 분기한다.
        bool firstClear = SaveSystem.Current == null || !SaveSystem.Current.hasClearedRun;
        expeditionEndPanel.Show(RunState.Score, firstClear);
    }

    private void OnConfirm()
    {
        var map = RunState.CurrentMap;
        Debug.Log($"[ResultUI] OnConfirm — playerWon={playerWon}, map={(map != null ? map.name : "null")}");

        if (map == null)
        {
            // 지도 없이 BattleScene 단독 실행 — MapScene 으로 가서 Run 진입 흐름을 다시 탄다.
            SceneLoader.LoadAsync(SceneNames.Map);
            return;
        }

        if (playerWon)
        {
            RunState.AdvanceNode(map.nodes.Count, NodeType.Battle);
            SaveBridge.SaveAfterBattleWin();
            SceneLoader.LoadAsync(SceneNames.Map);
        }
        else
        {
            // 패배 — 세이브는 BattleSaveTrigger.HandleGameEnded 에서 이미 삭제됨.
            RunState.ResetRun();
            SceneLoader.LoadAsync(SceneNames.Title);
        }
    }
}
