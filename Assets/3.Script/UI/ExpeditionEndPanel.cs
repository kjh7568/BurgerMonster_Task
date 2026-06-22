using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마지막 노드(=원정 최종 전투) 승리 후 표시되는 패널. 누적 점수를 보여주고,
/// 첫 클리어이면 [엔딩 보기] 로 EndingScene 진입, 그 이후엔 [타이틀로] 로 곧장 타이틀 복귀.
/// 어느 경로든 RunState/세이브의 Run 진행은 초기화한다.
///
/// 호출 흐름:
///   ResultUI.HandleGameEnded — 마지막 노드 승리를 감지하면 0.5초 대기 후 Show 호출.
/// </summary>
public class ExpeditionEndPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmLabel;

    [Header("Text")]
    [SerializeField] private string titleLabel = "원정 종료!";
    [SerializeField] private string scoreFormat = "POINT  {0}";
    [SerializeField] private string toEndingLabel = "엔딩 보기";
    [SerializeField] private string toTitleLabel = "타이틀로";

    private bool isFirstClear;

    private void Awake()
    {
        // root 가 자기 자신이면 Awake 가 곧 Show 직후에 호출되는 케이스(=인스펙터에서 비활성 시작 + 외부에서 SetActive(true) 한 직후) 에서
        // 패널을 다시 꺼버리는 자기충돌이 생긴다. 외부 자식만 root 로 분리한 경우에만 강제 비활성화.
        if (root != null && root != gameObject) root.SetActive(false);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        else Debug.LogError("[ExpeditionEndPanel] confirmButton 참조 누락 — 버튼이 동작하지 않음.");
    }

    private void OnDestroy()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirm);
    }

    /// <summary>패널을 띄우고 점수/버튼 라벨을 세팅. firstClear 여부에 따라 버튼 라벨이 갈린다.</summary>
    public void Show(int finalScore, bool firstClear)
    {
        isFirstClear = firstClear;
        if (root != null) root.SetActive(true);
        if (titleText != null) titleText.text = titleLabel;
        if (scoreText != null) scoreText.text = string.Format(scoreFormat, finalScore);
        if (confirmLabel != null) confirmLabel.text = firstClear ? toEndingLabel : toTitleLabel;
        Debug.Log($"[ExpeditionEndPanel] Show — score={finalScore}, firstClear={firstClear}");
    }

    private void OnConfirm()
    {
        // 어느 경로든 Run 은 종료. 세이브의 run/battle 슬롯도 비운다 (hasSeenIntro/hasClearedRun 등 메타는 보존).
        RunState.ResetRun();
        SaveSystem.ClearRun();

        if (isFirstClear)
        {
            // 엔딩 컷씬 → 타이틀. hasClearedRun 은 EndingController.FinishAndGoToTitle 에서 저장.
            SceneLoader.LoadAsync(SceneNames.Ending);
        }
        else
        {
            SceneLoader.LoadAsync(SceneNames.Title);
        }
    }
}
