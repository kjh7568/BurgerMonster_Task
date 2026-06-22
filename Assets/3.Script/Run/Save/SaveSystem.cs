using System.IO;
using UnityEngine;

/// <summary>
/// 단일 슬롯 JSON 세이브의 디스크 I/O. 위치는 <c>Application.persistentDataPath/save.json</c>.
/// 모든 시스템(설정/Run/전투)이 한 파일을 공유하기 때문에 항상 전체 SaveData 를 읽고 쓴다.
/// 캐시된 인스턴스는 <see cref="Current"/> 로 노출 — 디스크 I/O 없이 메모리 갱신 후 <see cref="Flush"/> 로 한 번에 기록.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";
    private const string Version = SaveData.CurrentVersion;

    private static SaveData current;
    private static bool loaded;

    public static SaveData Current
    {
        get
        {
            EnsureLoaded();
            return current;
        }
    }

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>세이브 파일이 존재하고 Run 또는 Battle 진행 데이터가 들어있는가. 설정만 있는 상태는 "이어하기" 대상이 아님.</summary>
    public static bool HasResumableRun()
    {
        EnsureLoaded();
        return current != null && current.run != null;
    }

    public static bool FileExists() => File.Exists(FilePath);

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        current = ReadFromDisk() ?? new SaveData();
        // 버전 불일치 시 안전 처리: 설정만 보존, run/battle 폐기.
        if (current.version != Version)
        {
            Debug.LogWarning($"[SaveSystem] 버전 불일치 ({current.version} → {Version}) — run/battle 폐기, 설정만 유지.");
            current.version = Version;
            current.run = null;
            current.battle = null;
            current.activeScene = null;
        }
    }

    private static SaveData ReadFromDisk()
    {
        var path = FilePath;
        try
        {
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 읽기 실패: {e.Message}");
            return null;
        }
    }

    /// <summary>Current 의 현재 메모리 상태를 디스크에 기록. 원자성을 위해 임시 파일에 쓰고 Replace.</summary>
    public static void Flush()
    {
        EnsureLoaded();
        try
        {
            var path = FilePath;
            var tmp = path + ".tmp";
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonUtility.ToJson(current, prettyPrint: true);
            File.WriteAllText(tmp, json);
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
            Debug.Log($"[SaveSystem] Flushed → {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] 쓰기 실패: {e.Message}");
        }
    }

    /// <summary>설정은 보존하고 run/battle 만 비운다. 패배 시 호출.</summary>
    public static void ClearRun()
    {
        EnsureLoaded();
        current.run = null;
        current.battle = null;
        current.activeScene = null;
        Flush();
    }

    /// <summary>전투 스냅샷만 비운다. 전투 종료 시 호출 — Run 진행은 유지.</summary>
    public static void ClearBattle()
    {
        EnsureLoaded();
        if (current.battle != null)
        {
            current.battle = null;
            // activeScene 도 정리해 다음 부팅이 Map 으로 가게.
            current.activeScene = null;
        }
    }

    /// <summary>세이브 파일 자체 삭제. 디버그용. 게임 진행에서는 ClearRun 사용 권장.</summary>
    public static void DeleteFile()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); }
        catch (System.Exception e) { Debug.LogError($"[SaveSystem] 삭제 실패: {e.Message}"); }
        current = new SaveData();
        loaded = true;
    }
}
