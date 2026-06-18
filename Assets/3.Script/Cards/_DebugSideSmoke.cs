using UnityEngine;

public class _DebugSideSmoke : MonoBehaviour
{
    public BattleConfigSO config;

    void Awake()
    {
        if (config == null)
        {
            Debug.LogError("[SideSmoke] config is null. Inspector에서 BattleConfigSO 더미를 연결하세요.");
            return;
        }

        var side = new Side(true, config.playerStartingCards, config.fieldSlotCount);
        Debug.Log($"[SideSmoke] field.Length={side.field.Length}, standby={side.standby.Count}");

        side.field[1] = null;
        var refilled = side.RefillField();
        Debug.Log($"[SideSmoke] refilled=[{string.Join(",", refilled)}], standby={side.standby.Count}");

        Debug.Log($"[SideSmoke] alive=[{string.Join(",", side.AliveIndices())}], defeated={side.IsDefeated}");
    }
}
