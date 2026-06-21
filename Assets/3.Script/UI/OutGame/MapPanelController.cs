using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 지도 패널 본체. MapDataSO 를 받아 노드 버튼을 생성·배치하고,
/// 현재 노드 클릭 시 타입에 맞춰 BattleScene 로드 또는 스텁 패널을 띄운다.
/// </summary>
public class MapPanelController : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Run 시작 시 이 풀에서 하나를 랜덤으로 뽑아 RunState.CurrentMap 에 세팅한다.")]
    [SerializeField] private MapDataSO[] mapPool;
    [Tooltip("풀 대신 강제로 사용할 지도. 디버그/단일 테스트용. 비워두면 mapPool 에서 뽑음.")]
    [SerializeField] private MapDataSO forcedMap;

    [Header("Layout")]
    [SerializeField] private RectTransform nodeContainer;
    [SerializeField] private MapNodeButton nodeButtonPrefab;
    [Tooltip("0번 노드의 시작 anchoredPosition (x, y). x 는 지터의 중심축.")]
    [SerializeField] private Vector2 startPosition = new Vector2(0f, -650f);
    [Tooltip("노드 간 간격. 세로 위로 쌓으려면 (0, 250).")]
    [SerializeField] private Vector2 stepOffset = new Vector2(0f, 250f);

    [Header("X Jitter")]
    [Tooltip("x 지터 최소값(startPosition.x 기준).")]
    [SerializeField] private float xJitterMin = -50f;
    [Tooltip("x 지터 최대값(startPosition.x 기준).")]
    [SerializeField] private float xJitterMax = 50f;
    [Tooltip("인접 노드 x 변화 절대값 상한. 50 이면 직전 노드 ±50 이내.")]
    [SerializeField] private float maxStepX = 50f;
    [Tooltip("x 지터 스냅 단위(픽셀). 10 이면 좌표가 …, -20, -10, 0, 10, … 처럼 10 단위로만 잡힘.")]
    [SerializeField] private int xJitterStep = 10;
    [Tooltip("같은 Run 동안 좌표가 흔들리지 않도록 시드 고정. 값 변경하면 위치 재배치.")]
    [SerializeField] private int randomSeed = 12345;

    [Header("Icons")]
    [SerializeField] private Sprite battleIcon;
    [SerializeField] private Sprite upgradeIcon;
    [SerializeField] private Sprite recruitIcon;

    [Header("Sub Panels")]
    [SerializeField] private StubEventPanel upgradeStubPanel;
    [SerializeField] private StubEventPanel recruitStubPanel;
    [SerializeField] private EndingPanel endingPanel;

    private readonly List<MapNodeButton> spawned = new List<MapNodeButton>();

    private void OnEnable()
    {
        RunState.EnsureInitialized();
        EnsureMapSelected();
        Rebuild();
        Refresh();
        CheckEnding();
    }

    /// <summary>Run 시작 시(또는 ResetRun 직후) CurrentMap 이 비어 있으면 풀에서 한 번 뽑는다.</summary>
    private void EnsureMapSelected()
    {
        if (RunState.CurrentMap != null) return;
        if (forcedMap != null) { RunState.SelectMap(forcedMap); return; }
        if (mapPool == null || mapPool.Length == 0)
        {
            Debug.LogError("[MapPanelController] mapPool 비어있음 — Inspector 에서 MapDataSO 자산을 채우거나 forcedMap 을 지정해야 함.");
            return;
        }
        var pick = mapPool[UnityEngine.Random.Range(0, mapPool.Length)];
        RunState.SelectMap(pick);
    }

    private void Rebuild()
    {
        foreach (var b in spawned) if (b != null) Destroy(b.gameObject);
        spawned.Clear();
        var mapData = RunState.CurrentMap;
        if (mapData == null || nodeButtonPrefab == null || nodeContainer == null) return;

        var rng = new System.Random(randomSeed);
        int step = Mathf.Max(1, xJitterStep);
        int lastIdx = mapData.nodes.Count - 1;
        int prevJitter = 0;
        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            var entry = mapData.nodes[i];
            var btn = Instantiate(nodeButtonPrefab, nodeContainer);
            btn.gameObject.SetActive(true);

            int jitter;
            if (i == 0 || i == lastIdx)
            {
                jitter = 0;
            }
            else
            {
                float loF = Mathf.Max(xJitterMin, prevJitter - maxStepX);
                float hiF = Mathf.Min(xJitterMax, prevJitter + maxStepX);
                int loUnits = Mathf.CeilToInt(loF / step);
                int hiUnits = Mathf.FloorToInt(hiF / step);
                jitter = (hiUnits >= loUnits) ? rng.Next(loUnits, hiUnits + 1) * step : Mathf.RoundToInt(loF / step) * step;
            }
            prevJitter = jitter;

            var rt = (RectTransform)btn.transform;
            rt.anchoredPosition = startPosition + stepOffset * i + new Vector2(jitter, 0f);

            var label = string.IsNullOrEmpty(entry.id) ? (i + 1).ToString() : entry.id;
            btn.Bind(i, entry.type, IconFor(entry.type), label, OnNodeClicked);
            spawned.Add(btn);
        }
    }

    /// <summary>RunState 진척에 맞춰 각 노드의 시각/상호작용 상태를 갱신한다.</summary>
    public void Refresh()
    {
        int cur = RunState.CurrentNodeIndex;
        for (int i = 0; i < spawned.Count; i++)
        {
            MapNodeButton.NodeVisualState state;
            if (i < cur) state = MapNodeButton.NodeVisualState.Cleared;
            else if (i == cur) state = MapNodeButton.NodeVisualState.Current;
            else state = MapNodeButton.NodeVisualState.Locked;
            spawned[i].SetVisualState(state);
        }
    }

    private void CheckEnding()
    {
        var mapData = RunState.CurrentMap;
        if (mapData == null) return;
        if (RunState.RunCompleted || RunState.CurrentNodeIndex >= mapData.nodes.Count)
        {
            if (endingPanel != null) endingPanel.Show();
        }
    }

    private Sprite IconFor(NodeType type) => type switch
    {
        NodeType.Battle => battleIcon,
        NodeType.Upgrade => upgradeIcon,
        NodeType.Recruit => recruitIcon,
        _ => battleIcon,
    };

    private void OnNodeClicked(int index)
    {
        if (index != RunState.CurrentNodeIndex) return;
        var mapData = RunState.CurrentMap;
        if (mapData == null || index < 0 || index >= mapData.nodes.Count) return;

        var type = mapData.nodes[index].type;
        switch (type)
        {
            case NodeType.Battle:
                SceneManager.LoadScene(SceneNames.Battle);
                break;
            case NodeType.Upgrade:
                if (upgradeStubPanel != null) upgradeStubPanel.Show(OnStubResolved);
                else OnStubResolved();
                break;
            case NodeType.Recruit:
                if (recruitStubPanel != null) recruitStubPanel.Show(OnStubResolved);
                else OnStubResolved();
                break;
        }
    }

    /// <summary>스텁 노드 완료 콜백 — 인덱스만 진행하고 지도에 머문다.</summary>
    private void OnStubResolved()
    {
        var mapData = RunState.CurrentMap;
        if (mapData == null) return;
        RunState.AdvanceNode(mapData.nodes.Count);
        Refresh();
        CheckEnding();
    }
}
