using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// BattleController.OnGameEnded 를 구독해 결과 패널을 띄우고, 승리 시 RunState 진행 + MapScene 로드,
/// 패배 시 같은 노드를 재도전(BattleScene 재로드)한다. mapData 가 비어 있으면(단독 실행) 그냥 씬 재로드.
/// </summary>
public class ResultUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmLabel;
    [SerializeField] private BattleController battle;
    [SerializeField] private MapDataSO mapData;

    [Header("Result Colors")]
    [SerializeField] private Color victoryColor = new Color(0.4f, 0.95f, 0.4f);
    [SerializeField] private Color defeatColor = new Color(0.95f, 0.4f, 0.4f);

    [Header("Result Text")]
    [SerializeField] private string victoryLabel = "VICTORY";
    [SerializeField] private string defeatLabel = "DEFEAT";
    [SerializeField] private string toMapLabel = "지도로";
    [SerializeField] private string retryLabel = "다시 도전";

    private bool playerWon;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (battle != null) battle.OnGameEnded += HandleGameEnded;
        else Debug.LogWarning("[ResultUI] battle 참조 누락. 인스펙터에서 연결 필요.");
    }

    private void OnDestroy()
    {
        if (battle != null) battle.OnGameEnded -= HandleGameEnded;
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirm);
    }

    private void HandleGameEnded(Side winner)
    {
        if (panel != null) panel.SetActive(true);

        playerWon = winner != null && winner.isPlayer;
        if (resultText != null)
        {
            resultText.text = playerWon ? victoryLabel : defeatLabel;
            resultText.color = playerWon ? victoryColor : defeatColor;
        }
        if (confirmLabel != null) confirmLabel.text = playerWon ? toMapLabel : retryLabel;
    }

    private void OnConfirm()
    {
        if (mapData == null)
        {
            // 지도 시스템 미사용 — 기존 동작 유지(씬 재로드).
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (playerWon)
        {
            RunState.AdvanceNode(mapData.nodes.Count);
            SceneManager.LoadScene(SceneNames.Map);
        }
        else
        {
            // 같은 노드 재도전 — 인덱스 유지하고 전투 씬 재로드.
            SceneManager.LoadScene(SceneNames.Battle);
        }
    }
}
