using UnityEngine;

/// <summary>
/// OutGame 씬 로딩 시 아웃게임 BGM 재생. 지도 열기 버튼 OnClick 에 PlayOpenMap 연결.
/// </summary>
public class OutGameSceneAudio : MonoBehaviour
{
    [SerializeField] private AudioClip outGameBGM;
    [SerializeField] private AudioClip openMapSfx;

    private void Start()
    {
        if (AudioManager.Instance != null && outGameBGM != null)
            AudioManager.Instance.PlayBGM(outGameBGM);
    }

    public void PlayOpenMap()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(openMapSfx);
    }
}
