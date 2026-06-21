using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// BattleController.OnGameEnded 를 구독해 결과 패널을 띄우고, 승리 시 RunState 진행 + MapScene 로드,
/// 패배 시 같은 노드를 재도전(BattleScene 재로드)한다. RunState.CurrentMap 이 비어 있으면(단독 실행) MapScene 으로 가서 Run 을 시작.
/// </summary>
public class ResultUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmLabel;
    [SerializeField] private BattleController battle;

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
        if (panel != null) panel.SetActive(true);

        playerWon = winner != null && winner.isPlayer;
        Debug.Log($"[ResultUI] HandleGameEnded — playerWon={playerWon}");
        if (resultText != null)
        {
            resultText.text = playerWon ? victoryLabel : defeatLabel;
            resultText.color = playerWon ? victoryColor : defeatColor;
        }
        if (confirmLabel != null) confirmLabel.text = playerWon ? toMapLabel : toTitleLabel;
    }

    private void OnConfirm()
    {
        var map = RunState.CurrentMap;
        Debug.Log($"[ResultUI] OnConfirm — playerWon={playerWon}, map={(map != null ? map.name : "null")}");

        if (map == null)
        {
            // 지도 없이 BattleScene 단독 실행 — MapScene 으로 가서 Run 진입 흐름을 다시 탄다.
            SceneManager.LoadScene(SceneNames.Map);
            return;
        }

        if (playerWon)
        {
            RunState.AdvanceNode(map.nodes.Count, NodeType.Battle);
            SceneManager.LoadScene(SceneNames.Map);
        }
        else
        {
            // 패배 — Run 초기화 후 타이틀로. 아직 타이틀 씬이 없으면 MapScene 으로 fallback.
            RunState.ResetRun();
            SceneManager.LoadScene(string.IsNullOrEmpty(SceneNames.Title) ? SceneNames.Map : SceneNames.Title);
        }
    }
}
