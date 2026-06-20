using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// BattleController.OnGameEnded 를 구독해 결과 패널을 띄우고 RESTART 입력으로 씬을 재로드한다.
/// 패널은 평소 비활성, 게임 종료 시점에만 활성. 본 스크립트는 항상 활성인 별도 GameObject에 부착해 이벤트 구독이 항상 살아있도록 한다.
/// </summary>
public class ResultUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button restartButton;
    [SerializeField] private BattleController battle;

    [Header("Result Colors")]
    [SerializeField] private Color victoryColor = new Color(0.4f, 0.95f, 0.4f);
    [SerializeField] private Color defeatColor = new Color(0.95f, 0.4f, 0.4f);

    [Header("Result Text")]
    [SerializeField] private string victoryLabel = "VICTORY";
    [SerializeField] private string defeatLabel = "DEFEAT";

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (battle != null) battle.OnGameEnded += HandleGameEnded;
        else Debug.LogWarning("[ResultUI] battle 참조 누락. 인스펙터에서 연결 필요.");
    }

    private void OnDestroy()
    {
        if (battle != null) battle.OnGameEnded -= HandleGameEnded;
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
    }

    private void HandleGameEnded(Side winner)
    {
        if (panel != null) panel.SetActive(true);

        bool playerWon = winner != null && winner.isPlayer;
        if (resultText != null)
        {
            resultText.text = playerWon ? victoryLabel : defeatLabel;
            resultText.color = playerWon ? victoryColor : defeatColor;
        }
    }

    private void Restart()
    {
        var idx = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(idx);
    }
}
