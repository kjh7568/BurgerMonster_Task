using UnityEngine;

/// <summary>
/// MapScene 안에 두는 BGM 컨트롤러. BattleSceneAudio 와 대칭 — 각 씬이 자기 BGM 을 책임진다.
/// 씬 진입 시 Start 가 호출돼 아웃게임 BGM 으로 크로스페이드, 미설정이면 전투 BGM 만 페이드아웃.
/// </summary>
public class OutGameSceneAudio : MonoBehaviour
{
    [SerializeField] private AudioClip outGameBGM;
    [SerializeField] private AudioClip openMapSfx;

    [SerializeField] private bool playOpenMapOnStart = true;

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            // 클립이 있으면 그걸로 크로스페이드, 없으면 진행 중인(전투) BGM 페이드아웃.
            if (outGameBGM != null) AudioManager.Instance.PlayBGM(outGameBGM);
            else AudioManager.Instance.StopBGM();
        }
        if (playOpenMapOnStart) PlayOpenMap();
    }

    public void PlayOpenMap()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(openMapSfx);
    }
}
