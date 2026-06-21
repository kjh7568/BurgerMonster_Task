using UnityEngine;

/// <summary>
/// 단일 Run 동안의 진행 상태. 씬 전환에서 살아남도록 static 으로 보관.
/// MapScene 진입 시 <see cref="EnsureInitialized"/> 가 한 번 실행되며,
/// 전투 결과에 따라 <see cref="AdvanceNode"/>·<see cref="ResetRun"/> 가 호출된다.
/// </summary>
public static class RunState
{
    private static bool initialized;

    public static int CurrentNodeIndex { get; private set; }
    public static bool RunCompleted { get; private set; }

    public static void EnsureInitialized()
    {
        if (initialized) return;
        ResetRun();
    }

    public static void ResetRun()
    {
        CurrentNodeIndex = 0;
        RunCompleted = false;
        initialized = true;
        Debug.Log("[RunState] Reset");
    }

    public static void AdvanceNode(int totalNodes)
    {
        if (totalNodes <= 0) return;
        CurrentNodeIndex = Mathf.Min(CurrentNodeIndex + 1, totalNodes);
        if (CurrentNodeIndex >= totalNodes) RunCompleted = true;
        Debug.Log($"[RunState] Advance → {CurrentNodeIndex}/{totalNodes} (completed={RunCompleted})");
    }
}
