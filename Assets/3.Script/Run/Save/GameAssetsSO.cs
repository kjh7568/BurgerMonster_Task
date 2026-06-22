using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 세이브에 등장하는 모든 ScriptableObject(CardDataSO, MapDataSO, EnemyPoolSO) 의 ID↔SO 룩업 테이블.
/// Resources 폴더에 단일 자산(이름: "GameAssets")으로 두면 첫 접근 시 자동 로드된다.
/// ID 는 자산 이름(`.name`) — Unity 에서 자산 이름 변경하면 기존 세이브가 깨질 수 있다는 트레이드오프.
/// </summary>
[CreateAssetMenu(menuName = "BurgerMonster/GameAssets")]
public class GameAssetsSO : ScriptableObject
{
    [Tooltip("세이브에 등장할 수 있는 모든 카드 SO. 시작덱·적풀·영입풀 카드를 모두 포함해야 한다.")]
    public CardDataSO[] allCards;

    [Tooltip("Run 에서 뽑힐 수 있는 모든 MapDataSO.")]
    public MapDataSO[] allMaps;

    [Tooltip("승리 시 골드 보상 재현용으로 보관해야 할 EnemyPoolSO. mid-battle 저장에는 안 쓰여도 됨.")]
    public EnemyPoolSO[] allEnemyPools;

    private const string ResourcePath = "GameAssets";

    private static GameAssetsSO cached;
    private static Dictionary<string, CardDataSO> cardById;
    private static Dictionary<string, MapDataSO> mapById;
    private static Dictionary<string, EnemyPoolSO> poolById;

    public static GameAssetsSO Instance
    {
        get
        {
            if (cached != null) return cached;
            cached = Resources.Load<GameAssetsSO>(ResourcePath);
            if (cached == null)
                Debug.LogError($"[GameAssetsSO] Resources/{ResourcePath}.asset 를 못 찾음. 세이브/로드가 동작하지 않을 수 있음.");
            return cached;
        }
    }

    private static void EnsureMaps()
    {
        if (cardById != null) return;
        cardById = new Dictionary<string, CardDataSO>();
        mapById = new Dictionary<string, MapDataSO>();
        poolById = new Dictionary<string, EnemyPoolSO>();
        var inst = Instance;
        if (inst == null) return;
        if (inst.allCards != null)
            foreach (var c in inst.allCards) if (c != null) cardById[c.name] = c;
        if (inst.allMaps != null)
            foreach (var m in inst.allMaps) if (m != null) mapById[m.name] = m;
        if (inst.allEnemyPools != null)
            foreach (var p in inst.allEnemyPools) if (p != null) poolById[p.name] = p;
    }

    public static string CardId(CardDataSO c) => c != null ? c.name : null;
    public static CardDataSO ResolveCard(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EnsureMaps();
        return cardById != null && cardById.TryGetValue(id, out var v) ? v : null;
    }

    public static string MapId(MapDataSO m) => m != null ? m.name : null;
    public static MapDataSO ResolveMap(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EnsureMaps();
        return mapById != null && mapById.TryGetValue(id, out var v) ? v : null;
    }

    public static string EnemyPoolId(EnemyPoolSO p) => p != null ? p.name : null;
    public static EnemyPoolSO ResolveEnemyPool(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EnsureMaps();
        return poolById != null && poolById.TryGetValue(id, out var v) ? v : null;
    }
}
