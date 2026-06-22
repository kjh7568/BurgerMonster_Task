using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 한 글자씩 타자기 효과로 텍스트를 출력하는 다이얼로그 박스. 오프닝 컷씬·튜토리얼에서 공용으로 사용.
///
/// 사용 패턴:
///   1) Play(lines, onAllEnd) 호출 → 첫 라인 타자기 출력 시작.
///   2) 진행 중 박스 클릭 → 현재 라인 즉시 완성 (Skip).
///   3) 라인 완성 상태에서 다시 클릭 → 다음 라인. 마지막 라인이면 onAllEnd 콜백.
///
/// 박스 자체는 화면 전면을 덮는 풀스크린 클릭 영역(advanceCatcher)을 깔아 어디를 눌러도 진행되게 한다.
/// 외부에서 SkipAll() 을 호출하면 시퀀스를 즉시 종료하고 onAllEnd 만 발화 (오프닝 스킵 버튼 등에서 사용).
/// </summary>
public class DialogueBox : MonoBehaviour
{
    [Header("UI Refs")]
    [Tooltip("박스 루트. Play 호출 시 SetActive(true), 시퀀스 종료 시 false.")]
    [SerializeField] private GameObject root;
    [Tooltip("본문 텍스트. maxVisibleCharacters 를 증가시키는 방식으로 타자기 효과를 낸다 (GC alloc 없음).")]
    [SerializeField] private TMP_Text bodyText;
    [Tooltip("라인 완성 후 깜빡일 ▼ 인디케이터. 비워두면 비활성.")]
    [SerializeField] private GameObject nextIndicator;
    [Tooltip("화면 어디든 눌러도 진행되게 할 풀스크린 투명 Button. 비워두면 root 의 Button 컴포넌트를 찾아 사용.")]
    [SerializeField] private Button advanceCatcher;

    [Header("Typewriter")]
    [Tooltip("초당 출력 글자 수. 30~40 권장.")]
    [SerializeField] private float charsPerSecond = 35f;
    [Tooltip("문장 부호(.,!?) 뒤에 추가로 쉴 시간(초).")]
    [SerializeField] private float punctuationPauseSeconds = 0.12f;

    [Header("SFX (선택)")]
    [Tooltip("타자기 효과음. 비워두면 무음.")]
    [SerializeField] private AudioClip typeSfx;
    [Tooltip("몇 글자마다 한 번씩 효과음을 낼지. 1 이면 매 글자, 3 이면 3글자에 1회.")]
    [SerializeField] private int sfxEveryNChars = 3;

    /// <summary>설정 패널에서 텍스트 속도 옵션을 바꿀 때 곱하는 전역 배율. 1=보통, &lt;1=느림, &gt;1=빠름.</summary>
    public static float GlobalSpeedMultiplier = 1f;

    /// <summary>한 라인이 완전히 노출된 직후 발화.</summary>
    public event Action OnLineEnd;
    /// <summary>마지막 라인까지 모두 끝났거나 SkipAll 로 강제 종료된 직후 발화.</summary>
    public event Action OnAllEnd;

    private IList<string> queue;
    private int lineIndex;
    private Coroutine typingRoutine;
    private bool lineComplete;       // 현재 라인이 모두 노출된 상태인가
    private bool sequenceActive;     // Play 가 살아있는 상태인가 (중복 입력/이중 종료 방지)
    private Action onAllEndOnce;     // Play 호출 1회용 콜백

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (nextIndicator != null) nextIndicator.SetActive(false);

        if (advanceCatcher == null && root != null)
            advanceCatcher = root.GetComponent<Button>();
        if (advanceCatcher != null)
            advanceCatcher.onClick.AddListener(Advance);
    }

    private void OnDestroy()
    {
        if (advanceCatcher != null)
            advanceCatcher.onClick.RemoveListener(Advance);
    }

    /// <summary>주어진 라인들을 순서대로 재생. 이전 시퀀스가 살아있으면 즉시 종료하고 새로 시작.</summary>
    public void Play(IList<string> lines, Action onAllEnd = null)
    {
        if (lines == null || lines.Count == 0)
        {
            onAllEnd?.Invoke();
            return;
        }

        // 이전 시퀀스가 남아있으면 콜백 발화 없이 청소.
        if (sequenceActive) HardReset();

        queue = lines;
        lineIndex = 0;
        onAllEndOnce = onAllEnd;
        sequenceActive = true;
        if (root != null) root.SetActive(true);
        StartLine(lineIndex);
    }

    /// <summary>현재 라인을 즉시 전부 노출. 이미 완성 상태면 다음 라인으로 진행.</summary>
    public void Skip()
    {
        if (!sequenceActive) return;
        if (!lineComplete) CompleteCurrentLine();
        else AdvanceToNext();
    }

    /// <summary>전체 시퀀스를 즉시 종료하고 OnAllEnd 발화. 오프닝의 [Skip ▶] 버튼에서 사용.</summary>
    public void SkipAll()
    {
        if (!sequenceActive) return;
        HardReset();
        FireAllEnd();
    }

    /// <summary>박스/풀스크린 캐처 클릭 시 호출되는 진행 함수. Skip 과 동일 동작이지만 외부 노출용 이름을 분리.</summary>
    private void Advance() => Skip();

    private void StartLine(int idx)
    {
        if (queue == null || idx < 0 || idx >= queue.Count) return;
        if (typingRoutine != null) StopCoroutine(typingRoutine);

        var line = queue[idx] ?? string.Empty;
        if (bodyText != null)
        {
            bodyText.text = line;
            bodyText.maxVisibleCharacters = 0;
        }
        lineComplete = false;
        if (nextIndicator != null) nextIndicator.SetActive(false);

        typingRoutine = StartCoroutine(TypeRoutine(line));
    }

    private IEnumerator TypeRoutine(string line)
    {
        if (bodyText == null) yield break;

        // TMP 가 줄바꿈/태그 처리를 마치도록 한 프레임 강제 갱신.
        bodyText.ForceMeshUpdate();
        int totalChars = bodyText.textInfo.characterCount;

        float baseCps = Mathf.Max(1f, charsPerSecond) * Mathf.Max(0.1f, GlobalSpeedMultiplier);
        float perCharDelay = 1f / baseCps;
        int sfxCounter = 0;
        var audio = AudioManager.Instance;

        for (int i = 1; i <= totalChars; i++)
        {
            bodyText.maxVisibleCharacters = i;

            if (typeSfx != null && audio != null && sfxEveryNChars > 0)
            {
                sfxCounter++;
                if (sfxCounter >= sfxEveryNChars)
                {
                    audio.PlaySFX(typeSfx);
                    sfxCounter = 0;
                }
            }

            // 문장 부호 뒤에 살짝 멈춤. 마지막 글자엔 의미 없음.
            float delay = perCharDelay;
            if (i < totalChars && i - 1 < line.Length && IsPausePunctuation(line[i - 1]))
                delay += punctuationPauseSeconds;

            yield return new WaitForSeconds(delay);
        }

        CompleteCurrentLine();
    }

    private static bool IsPausePunctuation(char c)
    {
        return c == '.' || c == '!' || c == '?' || c == ',' || c == '…';
    }

    private void CompleteCurrentLine()
    {
        if (typingRoutine != null) { StopCoroutine(typingRoutine); typingRoutine = null; }
        if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
        lineComplete = true;
        if (nextIndicator != null) nextIndicator.SetActive(true);
        OnLineEnd?.Invoke();
    }

    private void AdvanceToNext()
    {
        lineIndex++;
        if (lineIndex >= queue.Count)
        {
            HardReset();
            FireAllEnd();
            return;
        }
        StartLine(lineIndex);
    }

    private void HardReset()
    {
        if (typingRoutine != null) { StopCoroutine(typingRoutine); typingRoutine = null; }
        sequenceActive = false;
        lineComplete = false;
        queue = null;
        lineIndex = 0;
        if (nextIndicator != null) nextIndicator.SetActive(false);
        if (root != null) root.SetActive(false);
    }

    private void FireAllEnd()
    {
        var cb = onAllEndOnce;
        onAllEndOnce = null;
        cb?.Invoke();
        OnAllEnd?.Invoke();
    }
}
