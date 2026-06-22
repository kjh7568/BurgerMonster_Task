using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 시스템과 SaveSystem 사이의 고수준 접착부. 호출자가 "노드 진입 직전이다 / 전투 체크포인트다 / 전투 끝났다" 같은
/// 의미적 시점을 알려주면 RunState/BattleController 에서 적절한 스냅샷을 떠 SaveSystem.Current 에 넣고 Flush 한다.
/// </summary>
public static class SaveBridge
{
    /// <summary>지도/이벤트 노드에서 진행이 갱신된 시점 — Run 만 저장. battle 슬롯은 항상 비워둠.
    /// 노드 클릭 직후 종료해도 이어하기는 지도로 복귀해야 하므로 activeScene 같은 부가 힌트는 쓰지 않는다.</summary>
    public static void SaveBeforeNodeEntry(string _ = null)
    {
        var data = SaveSystem.Current;
        data.run = RunState.CaptureSnapshot();
        data.battle = null;
        SaveSystem.Flush();
    }

    /// <summary>전투 중 안정 시점(플레이어 입력 대기 등) — Run + Battle 스냅샷 갱신. 이 호출이 한 번이라도 일어나야 이어하기가 전투로 복귀.</summary>
    public static void SaveBattleCheckpoint(BattleController battle)
    {
        if (battle == null) return;
        var snap = battle.CaptureSnapshot();
        if (snap == null) return;
        var data = SaveSystem.Current;
        data.run = RunState.CaptureSnapshot();
        data.battle = snap;
        SaveSystem.Flush();
    }

    /// <summary>전투 승리 후 — Run 갱신, battle 슬롯 비움.</summary>
    public static void SaveAfterBattleWin()
    {
        var data = SaveSystem.Current;
        data.run = RunState.CaptureSnapshot();
        data.battle = null;
        SaveSystem.Flush();
    }

    /// <summary>패배 — Run/Battle 모두 삭제하고 설정만 남김.</summary>
    public static void ClearOnDefeat() => SaveSystem.ClearRun();

    /// <summary>설정 변경 직후 호출. run/battle 은 건드리지 않음.</summary>
    public static void SaveSettings()
    {
        var data = SaveSystem.Current;
        if (data.settings == null) data.settings = new SettingsState();
        if (AudioManager.Instance != null)
        {
            data.settings.bgmVolume = AudioManager.Instance.BgmVolume;
            data.settings.sfxVolume = AudioManager.Instance.SfxVolume;
        }
        SaveSystem.Flush();
    }

    /// <summary>새 게임 — 세이브 비우고 RunState 도 리셋.</summary>
    public static void StartNewRun()
    {
        SaveSystem.ClearRun();
        RunState.ResetRun();
    }

    /// <summary>이어하기 — SaveSystem.Current.run 으로 RunState 복원 + 재진입할 씬 로드.</summary>
    public static void ResumeRun()
    {
        var data = SaveSystem.Current;
        if (data.run == null)
        {
            Debug.LogWarning("[SaveBridge] Resume 시도 — 저장된 Run 없음. 새 게임으로 폴백.");
            StartNewRun();
            SceneLoader.LoadAsync(SceneNames.Map);
            return;
        }

        RunState.RestoreFromSnapshot(data.run);
        if (data.battle != null)
        {
            BattleController.PendingRestoreSnapshot = data.battle;
            SceneLoader.LoadAsync(SceneNames.Battle);
        }
        else
        {
            SceneLoader.LoadAsync(SceneNames.Map);
        }
    }
}
