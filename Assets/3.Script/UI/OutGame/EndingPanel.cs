using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 모든 노드 클리어 시 나타나는 엔딩 패널. 새 Run 시작 버튼만 제공.
/// </summary>
public class EndingPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button newRunButton;

    [SerializeField] private string title = "RUN CLEAR!";

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (newRunButton != null) newRunButton.onClick.AddListener(StartNewRun);
    }

    private void OnDestroy()
    {
        if (newRunButton != null) newRunButton.onClick.RemoveListener(StartNewRun);
    }

    public void Show()
    {
        if (titleText != null) titleText.text = title;
        if (root != null) root.SetActive(true);
    }

    private void StartNewRun()
    {
        RunState.ResetRun();
        SceneLoader.LoadAsync(SceneNames.Map);
    }
}
