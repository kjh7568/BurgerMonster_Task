using UnityEngine;

/// <summary>
/// 앱 일시정지/종료 시점에 최신 상태를 디스크로 강제 Flush. 각 씬의 BattleController/MapPanelController 가
/// 평소엔 체크포인트마다 SaveBridge 호출로 충분하지만, 강제 종료에 대비해 한 번 더 보장.
/// 단일 인스턴스 — 첫 씬에 두면 DontDestroyOnLoad 로 유지.
/// </summary>
public class AutoSaveHook : MonoBehaviour
{
    private static AutoSaveHook instance;

    [Tooltip("BattleScene 안이면 BattleController 를 찾아 Battle 스냅샷도 같이 저장.")]
    [SerializeField] private bool captureBattleOnPause = true;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) FlushNow();
    }

    private void OnApplicationQuit() => FlushNow();

    private void FlushNow()
    {
        if (captureBattleOnPause)
        {
            var battle = FindObjectOfType<BattleController>();
            if (battle != null) SaveBridge.SaveBattleCheckpoint(battle);
            else SaveSystem.Flush();
        }
        else
        {
            SaveSystem.Flush();
        }
    }
}
