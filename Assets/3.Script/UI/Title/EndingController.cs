using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 엔딩 컷씬 진행. OpeningController 와 동일한 페이드+다이얼로그 패턴이지만,
/// 종료 시 SaveSystem.Current.hasClearedRun = true 를 저장하고 타이틀 씬으로 복귀한다.
///
/// 호출 흐름:
///   ExpeditionEndPanel 의 [엔딩 보기] 버튼
///     → SceneLoader.LoadAsync(SceneNames.Ending)
///   EndingScene 의 EndingController.Start
///     → 컷 0 부터 순서대로 재생
///     → 마지막 컷의 OnAllEnd → FinishAndGoToTitle()
/// </summary>
public class EndingController : MonoBehaviour
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
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float postFadeHold = 0.4f;

    [Header("Audio")]
    [Tooltip("엔딩 BGM. 비워두면 AudioManager 의 현재 BGM 유지.")]
    [SerializeField] private AudioClip endingBGM;

    private int cutIndex;
    private bool finishing;

    private void Start()
    {
        if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);

        if (AudioManager.Instance != null && endingBGM != null)
            AudioManager.Instance.PlayBGM(endingBGM);

        if (cuts == null || cuts.Length == 0)
        {
            Debug.LogWarning("[EndingController] 컷이 비어있음 — 즉시 타이틀로 이동.");
            FinishAndGoToTitle();
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

        var lines = FilterLines(cut.lines);
        if (lines.Count == 0)
        {
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
            FinishAndGoToTitle();
            return;
        }
        StartCoroutine(PlayCutRoutine(cutIndex));
    }

    private void OnSkipClicked()
    {
        if (dialogueBox != null) dialogueBox.SkipAll();
        FinishAndGoToTitle();
    }

    private void FinishAndGoToTitle()
    {
        if (finishing) return;
        finishing = true;

        var data = SaveSystem.Current;
        if (data != null && !data.hasClearedRun)
        {
            data.hasClearedRun = true;
            SaveSystem.Flush();
        }
        // Run 자체는 ExpeditionEndPanel 에서 이미 정리됨. 여기선 씬 전환만.
        SceneLoader.LoadAsync(SceneNames.Title);
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
