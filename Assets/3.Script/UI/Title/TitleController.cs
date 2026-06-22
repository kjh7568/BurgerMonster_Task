using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 타이틀 씬 진입점. 세이브 유무에 따라 [이어하기] 버튼 활성/비활성을 조절하고, [새 게임] 은 기존 세이브가 있으면 덮어쓰기 확인 모달을 띄운다.
/// </summary>
public class TitleController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Continue Button Label Colors")]
    [Tooltip("이어하기 버튼 안 TMP_Text. 비워두면 자식에서 자동 탐색.")]
    [SerializeField] private TMP_Text continueLabel;
    [SerializeField] private Color continueEnabledColor = Color.white;
    [SerializeField] private Color continueDisabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Overwrite Confirm Modal")]
    [Tooltip("세이브 존재 상태에서 새 게임 누르면 뜨는 모달 루트.")]
    [SerializeField] private GameObject overwriteModal;
    [SerializeField] private Button overwriteConfirm;
    [SerializeField] private Button overwriteCancel;

    [Header("Settings")]
    [SerializeField] private SettingsPanel settingsPanel;

    [Header("Audio")]
    [Tooltip("타이틀 BGM. 비워두면 이전 씬의 BGM 을 정지만 시킴. 전투/지도에서 돌아왔을 때 BGM 이 계속 남는 문제 방지용.")]
    [SerializeField] private AudioClip titleBGM;

    private void Start()
    {
        if (overwriteModal != null) overwriteModal.SetActive(false);

        // 이전 씬(특히 전투)의 BGM 갈아치우기. 클립 있으면 크로스페이드, 없으면 페이드아웃 정지.
        if (AudioManager.Instance != null)
        {
            if (titleBGM != null) AudioManager.Instance.PlayBGM(titleBGM);
            else AudioManager.Instance.StopBGM();
        }

        bool resumable = SaveSystem.HasResumableRun();
        if (continueButton != null)
        {
            // 세이브 없을 때도 버튼은 보이되 클릭 불가능 + 라벨 회색.
            continueButton.interactable = resumable;
            continueButton.onClick.AddListener(OnContinue);

            if (continueLabel == null)
                continueLabel = continueButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (continueLabel != null)
                continueLabel.color = resumable ? continueEnabledColor : continueDisabledColor;
        }
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        if (overwriteConfirm != null) overwriteConfirm.onClick.AddListener(OnOverwriteConfirmed);
        if (overwriteCancel != null) overwriteCancel.onClick.AddListener(OnOverwriteCancelled);

        // 첫 부팅에서 AudioManager 가 아직 없으면 설정값은 SaveSystem 에 이미 로드돼 있고, 다음 씬에서 적용된다.
    }

    private void OnContinue()
    {
        SaveBridge.ResumeRun();
    }

    private void OnNewGameClicked()
    {
        if (SaveSystem.HasResumableRun() && overwriteModal != null)
        {
            overwriteModal.SetActive(true);
        }
        else
        {
            StartNewGame();
        }
    }

    private void OnOverwriteConfirmed()
    {
        if (overwriteModal != null) overwriteModal.SetActive(false);
        StartNewGame();
    }

    private void OnOverwriteCancelled()
    {
        if (overwriteModal != null) overwriteModal.SetActive(false);
    }

    private void StartNewGame()
    {
        SaveBridge.StartNewRun();
        // 오프닝 컷씬을 한 번도 본 적 없으면 먼저 재생, 그렇지 않으면 곧장 지도.
        // (StartNewRun 은 run/battle 만 비울 뿐 hasSeenIntro 는 보존하므로 두 번째 새 게임부터는 자동 스킵.)
        bool seen = SaveSystem.Current != null && SaveSystem.Current.hasSeenIntro;
        SceneLoader.LoadAsync(seen ? SceneNames.Map : SceneNames.Opening);
    }

    private void OnSettings()
    {
        if (settingsPanel != null) settingsPanel.Show();
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
