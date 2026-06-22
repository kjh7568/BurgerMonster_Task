using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// BGM/SFX 슬라이더 모달. 슬라이더 변경 즉시 AudioManager 에 반영, 패널 닫을 때 SaveBridge.SaveSettings 로 디스크 기록.
/// 타이틀·지도·전투 세 씬 어디서든 같은 컴포넌트를 쓰면 됨. 톱니 버튼 OnClick 에서 Show() 연결.
/// "타이틀로 돌아가기" 버튼은 Map/Battle 씬에서만 동작. 전투 중이면 현재 진행 상황을 체크포인트로 저장한 뒤 이동.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button closeButton;
    [Tooltip("타이틀로 돌아가기 버튼. TitleScene 에서는 자동 비활성화. 비워두면 기능 자체가 빠진 패널로 동작.")]
    [SerializeField] private Button returnToTitleButton;

    [Header("Value Labels (0~100 정수 표시)")]
    [Tooltip("BGM 슬라이더 옆 숫자 텍스트. 비워두면 표시 안 함.")]
    [SerializeField] private TMP_Text bgmValueText;
    [Tooltip("SFX 슬라이더 옆 숫자 텍스트. 비워두면 표시 안 함.")]
    [SerializeField] private TMP_Text sfxValueText;

    private bool wired;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }
        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.AddListener(OnReturnToTitle);
            // TitleScene 안에서는 이 버튼이 의미 없으므로 숨김.
            bool isTitle = SceneManager.GetActiveScene().name == SceneNames.Title;
            returnToTitleButton.gameObject.SetActive(!isTitle);
        }
        wired = true;
    }

    private void OnDestroy()
    {
        if (!wired) return;
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        if (returnToTitleButton != null) returnToTitleButton.onClick.RemoveListener(OnReturnToTitle);
    }

    public void Show()
    {
        var settings = SaveSystem.Current?.settings;
        float bgm = settings != null ? settings.bgmVolume : (AudioManager.Instance != null ? AudioManager.Instance.BgmVolume : 0.5f);
        float sfx = settings != null ? settings.sfxVolume : (AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(bgm);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);
        UpdateValueText(bgmValueText, bgm);
        UpdateValueText(sfxValueText, sfx);
        if (root != null) root.SetActive(true);
    }

    /// <summary>0~1 값을 0~100 정수로 변환해 텍스트에 표시.</summary>
    private static void UpdateValueText(TMP_Text label, float v01)
    {
        if (label == null) return;
        label.text = Mathf.RoundToInt(Mathf.Clamp01(v01) * 100f).ToString();
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        SaveBridge.SaveSettings();
    }

    private void OnBgmChanged(float v)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetBgmVolume(v);
        UpdateValueText(bgmValueText, v);
    }

    private void OnSfxChanged(float v)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(v);
        UpdateValueText(sfxValueText, v);
    }

    /// <summary>
    /// 현재 진행을 디스크에 보존한 뒤 TitleScene 으로 이동. 전투 중이면 BattleController 를 찾아 체크포인트를 떠
    /// 이어하기로 정확히 복원되도록 한다. 지도/타이틀에서는 SaveSystem.Flush 만으로 충분.
    /// </summary>
    private void OnReturnToTitle()
    {
        // 설정 변경분 먼저 반영.
        SaveBridge.SaveSettings();

        var battle = FindObjectOfType<BattleController>();
        if (battle != null) SaveBridge.SaveBattleCheckpoint(battle);
        else SaveSystem.Flush();

        if (root != null) root.SetActive(false);
        SceneManager.LoadScene(SceneNames.Title);
    }
}
