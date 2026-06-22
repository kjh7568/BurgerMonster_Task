using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 페이크 로딩 패널로 씬 전환의 한 박자 멈춤을 가린다.
/// LoadSceneAsync 의 실제 진행률(0~0.9) 을 따라 게이지가 부드럽게 차오르고, 최소 노출 시간을 보장한다.
/// 씬 활성화 직후 새 씬에서 90→100% 를 짧게 채워 활성화 1프레임 끊김을 게이지 진행 중에 묻는다.
/// Resources/LoadingPanel.prefab 을 자동으로 띄워 DontDestroyOnLoad 로 유지한다.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    private const string PrefabResourcePath = "LoadingPanel";

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image fillImage;
    [Tooltip("프로세스 바의 전체 트랙. 채워진 끝 X 위치 계산에 사용.")]
    [SerializeField] private RectTransform barRect;
    [Tooltip("프로세스 바 끝(오른쪽) 위에 따라다니는 캐릭터 로고. barRect 자식, 앵커 Left-Center 권장.")]
    [SerializeField] private RectTransform logoCharacter;
    [SerializeField] private TMP_Text percentText;

    [Header("Tuning")]
    [Tooltip("로딩 패널 최소 노출 시간. 빠른 폰에서 깜빡 사라지지 않게 함.")]
    [SerializeField] private float minDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 0.12f;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [Tooltip("씬 활성화 후 90→100% 채우는 시간.")]
    [SerializeField] private float postActivationFillDuration = 0.25f;
    [Tooltip("게이지가 실제 진행률을 따라가는 부드러움. 클수록 천천히 따라감.")]
    [SerializeField] private float smoothTime = 0.15f;

    private static SceneLoader instance;
    private float displayedProgress;
    private float progressVelocity;

    public static bool IsLoading { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var prefab = Resources.Load<SceneLoader>(PrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"[SceneLoader] Resources/{PrefabResourcePath}.prefab 을 찾지 못했습니다. 프리팹 루트에 SceneLoader 컴포넌트가 붙어 있어야 합니다.");
            return;
        }
        var go = Instantiate(prefab);
        go.name = prefab.name;
        DontDestroyOnLoad(go.gameObject);
        instance = go;
        instance.HideImmediate();
    }

    /// <summary>씬을 페이크 로딩 패널로 가리며 비동기 로드한다. 기존 SceneManager.LoadScene 호출의 드롭인 대체용.</summary>
    public static void LoadAsync(string sceneName)
    {
        if (instance == null) Bootstrap();
        if (instance == null)
        {
            // 폴백: 부트스트랩 실패 시 그냥 동기 로드.
            SceneManager.LoadScene(sceneName);
            return;
        }
        if (IsLoading) return;
        instance.StartCoroutine(instance.LoadRoutine(sceneName));
    }

    private void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        displayedProgress = 0f;
        progressVelocity = 0f;
        ApplyProgressVisual(0f);
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        IsLoading = true;
        displayedProgress = 0f;
        progressVelocity = 0f;
        ApplyProgressVisual(0f);

        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        yield return FadeCanvas(0f, 1f, fadeInDuration);

        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            float target = Mathf.Clamp01(op.progress / 0.9f) * 0.9f;
            displayedProgress = Mathf.SmoothDamp(displayedProgress, target, ref progressVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            ApplyProgressVisual(displayedProgress);

            bool sceneReady = op.progress >= 0.9f;
            bool barCaughtUp = displayedProgress >= 0.88f;
            bool minTimeMet = elapsed >= minDuration;
            if (sceneReady && barCaughtUp && minTimeMet) break;

            yield return null;
        }

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        float t = 0f;
        float start = displayedProgress;
        float dur = Mathf.Max(0.0001f, postActivationFillDuration);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            displayedProgress = Mathf.Lerp(start, 1f, t / dur);
            ApplyProgressVisual(displayedProgress);
            yield return null;
        }
        ApplyProgressVisual(1f);

        yield return FadeCanvas(1f, 0f, fadeOutDuration);
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        IsLoading = false;
    }

    private void ApplyProgressVisual(float p)
    {
        p = Mathf.Clamp01(p);
        if (fillImage != null) fillImage.fillAmount = p;
        if (percentText != null) percentText.text = $"{Mathf.RoundToInt(p * 100f)}%";

        if (logoCharacter != null && barRect != null)
        {
            float barWidth = barRect.rect.width;
            Vector2 pos = logoCharacter.anchoredPosition;
            pos.x = p * barWidth;
            logoCharacter.anchoredPosition = pos;
        }
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        if (canvasGroup == null || duration <= 0f)
        {
            if (canvasGroup != null) canvasGroup.alpha = to;
            yield break;
        }
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
