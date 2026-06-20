using System.Collections;
using UnityEngine;

/// <summary>
/// 씬 간 유지되는 BGM/SFX 단일 진입점. 첫 씬(OutGame)에 GameObject로 두면 DontDestroyOnLoad 로 살아남는다.
/// 두 번째 인스턴스는 중복 방지를 위해 즉시 파괴 — 씬마다 prefab 을 두고 싶을 때도 안전.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Crossfade")]
    [SerializeField] private float crossfadeDuration = 0.5f;

    private AudioSource bgmA;
    private AudioSource bgmB;
    private AudioSource sfxSource;
    private AudioSource activeBgm;
    private Coroutine crossfadeRoutine;

    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmA = CreateSource("BGM_A", loop: true);
        bgmB = CreateSource("BGM_B", loop: true);
        sfxSource = CreateSource("SFX", loop: false);
        sfxSource.volume = 1f; // BGM 은 크로스페이드가 볼륨을 올리지만 SFX 는 PlayOneShot 의 volumeScale 만 곱하므로 1 로 둬야 들림
        activeBgm = bgmA;
    }

    private AudioSource CreateSource(string name, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        src.volume = 0f;
        return src;
    }

    /// <summary>같은 클립이면 무시. 다르면 크로스페이드. clip == null 이면 페이드아웃 후 정지.</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (activeBgm != null && activeBgm.clip == clip && activeBgm.isPlaying) return;

        if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(CrossfadeRoutine(clip));
    }

    private IEnumerator CrossfadeRoutine(AudioClip next)
    {
        var fromSrc = activeBgm;
        var toSrc = (activeBgm == bgmA) ? bgmB : bgmA;

        if (next != null)
        {
            toSrc.clip = next;
            toSrc.volume = 0f;
            toSrc.Play();
        }

        float t = 0f;
        float startFromVol = fromSrc != null ? fromSrc.volume : 0f;
        while (t < crossfadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / crossfadeDuration);
            if (fromSrc != null) fromSrc.volume = Mathf.Lerp(startFromVol, 0f, k);
            if (next != null) toSrc.volume = Mathf.Lerp(0f, bgmVolume, k);
            yield return null;
        }

        if (fromSrc != null)
        {
            fromSrc.Stop();
            fromSrc.clip = null;
            fromSrc.volume = 0f;
        }
        if (next != null)
        {
            toSrc.volume = bgmVolume;
            activeBgm = toSrc;
        }
        crossfadeRoutine = null;
    }

    public void StopBGM() => PlayBGM(null);

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void SetBgmVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        if (activeBgm != null && crossfadeRoutine == null) activeBgm.volume = bgmVolume;
    }

    public void SetSfxVolume(float v) => sfxVolume = Mathf.Clamp01(v);
}
