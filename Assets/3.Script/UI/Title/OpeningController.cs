using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 오프닝 컷씬 진행. 컷 이미지를 페이드 인/아웃으로 전환하면서 컷별 라인 묶음을 DialogueBox 로 출력한다.
/// 마지막 컷이 끝나거나 [Skip ▶] 을 누르면 hasSeenIntro = true 저장 후 지도로 진입.
///
/// 호출 흐름:
///   TitleController.StartNewGame
///     → hasSeenIntro 가 false 면 SceneManager.LoadScene(SceneNames.Opening)
///   OpeningScene 의 OpeningController.Start
///     → 컷 0 부터 순서대로 재생
///     → 마지막 컷의 OnAllEnd → FinishAndGoToMap()
/// </summary>
public class OpeningController : MonoBehaviour
{
    [System.Serializable]
    public class Cut
    {
        [Tooltip("컷 일러스트. 비워두면 검정 화면에 텍스트만.")]
        public Sprite illustration;
        [TextArea(2, 6), Tooltip("이 컷에서 출력할 내레이션 라인들. 한 줄 = 한 라인. 빈 줄은 무시.")]
        public string[] lines;
    }

    [Header("UI")]
    [Tooltip("컷 일러스트가 들어가는 Image. 풀스크린 권장.")]
    [SerializeField] private Image illustrationImage;
    [Tooltip("페이드 전환용 단색 검정 패널 (alpha 0~1). 비워두면 페이드 생략.")]
    [SerializeField] private CanvasGroup fadePanel;
    [Tooltip("공용 다이얼로그 박스. 컷별 lines 출력.")]
    [SerializeField] private DialogueBox dialogueBox;
    [Tooltip("우상단 [Skip ▶] 버튼.")]
    [SerializeField] private Button skipButton;

    [Header("Cuts")]
    [SerializeField] private Cut[] cuts;

    [Header("Timing")]
    [Tooltip("컷 전환 페이드 시간(초).")]
    [SerializeField] private float fadeDuration = 0.6f;
    [Tooltip("새 컷이 페이드 인된 직후 다이얼로그 시작 전 대기(초). 일러스트를 잠깐 보여주는 효과.")]
    [SerializeField] private float postFadeHold = 0.4f;

    [Header("Audio")]
    [Tooltip("오프닝 BGM. 비워두면 AudioManager 의 현재 BGM 유지.")]
    [SerializeField] private AudioClip openingBGM;

    private int cutIndex;
    private bool finishing; // 종료 처리가 한 번만 실행되도록.

    private void Start()
    {
        if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);

        if (AudioManager.Instance != null && openingBGM != null)
            AudioManager.Instance.PlayBGM(openingBGM);

        if (cuts == null || cuts.Length == 0)
        {
            Debug.LogWarning("[OpeningController] 컷이 비어있음 — 즉시 지도로 이동.");
            FinishAndGoToMap();
            return;
        }

        cutIndex = 0;
        StartCoroutine(PlayCutRoutine(cutIndex));
    }

    private void OnDestroy()
    {
        if (skipButton != null) skipButton.onClick.RemoveListener(OnSkipClicked);
        if (dialogueBox != null) dialogueBox.OnAllEnd -= OnCutLinesEnded;
    }

    private System.Collections.IEnumerator PlayCutRoutine(int idx)
    {
        var cut = cuts[idx];

        // 새 컷 이미지로 교체. 첫 컷이면 fade 0→1 in, 이후 컷은 out → swap → in.
        if (fadePanel != null && idx > 0)
            yield return Fade(0f, 1f);

        if (illustrationImage != null)
        {
            illustrationImage.sprite = cut.illustration;
            illustrationImage.enabled = cut.illustration != null;
        }

        if (fadePanel != null)
            yield return Fade(1f, 0f);

        if (postFadeHold > 0f)
            yield return new WaitForSecondsRealtime(postFadeHold);

        // 컷 라인 출력. 빈 라인은 거른다.
        var lines = FilterLines(cut.lines);
        if (lines.Count == 0)
        {
            // 텍스트 없는 컷은 짧게 보여주고 다음으로.
            yield return new WaitForSecondsRealtime(1.2f);
            AdvanceCut();
            yield break;
        }

        if (dialogueBox != null)
        {
            dialogueBox.OnAllEnd -= OnCutLinesEnded;
            dialogueBox.OnAllEnd += OnCutLinesEnded;
            dialogueBox.Play(lines);
        }
        else
        {
            // 다이얼로그 박스가 없으면 그냥 다음 컷으로 (에디터 셋업 누락 방지용 폴백).
            yield return new WaitForSecondsRealtime(1.2f);
            AdvanceCut();
        }
    }

    private void OnCutLinesEnded()
    {
        if (dialogueBox != null) dialogueBox.OnAllEnd -= OnCutLinesEnded;
        AdvanceCut();
    }

    private void AdvanceCut()
    {
        cutIndex++;
        if (cutIndex >= cuts.Length)
        {
            FinishAndGoToMap();
            return;
        }
        StartCoroutine(PlayCutRoutine(cutIndex));
    }

    private void OnSkipClicked()
    {
        if (dialogueBox != null) dialogueBox.SkipAll();
        FinishAndGoToMap();
    }

    private void FinishAndGoToMap()
    {
        if (finishing) return;
        finishing = true;

        var data = SaveSystem.Current;
        if (data != null && !data.hasSeenIntro)
        {
            data.hasSeenIntro = true;
            SaveSystem.Flush();
        }
        SceneManager.LoadScene(SceneNames.Map);
    }

    private System.Collections.IEnumerator Fade(float from, float to)
    {
        if (fadePanel == null) yield break;
        float t = 0f;
        fadePanel.alpha = from;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        fadePanel.alpha = to;
    }

    private static List<string> FilterLines(string[] src)
    {
        var list = new List<string>();
        if (src == null) return list;
        foreach (var s in src)
        {
            if (!string.IsNullOrWhiteSpace(s)) list.Add(s);
        }
        return list;
    }
}
