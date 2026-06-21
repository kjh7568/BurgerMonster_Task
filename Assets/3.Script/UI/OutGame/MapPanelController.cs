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
    [SerializeField] private MapDataSO mapData;

    [Header("Layout")]
    [SerializeField] private RectTransform nodeContainer;
    [SerializeField] private MapNodeButton nodeButtonPrefab;

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
        Rebuild();
        Refresh();
        CheckEnding();
    }

    private void Rebuild()
    {
        foreach (var b in spawned) if (b != null) Destroy(b.gameObject);
        spawned.Clear();
        if (mapData == null || nodeButtonPrefab == null || nodeContainer == null) return;

        for (int i = 0; i < mapData.nodes.Count; i++)
        {
            var entry = mapData.nodes[i];
            var btn = Instantiate(nodeButtonPrefab, nodeContainer);
            btn.gameObject.SetActive(true);
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
        RunState.AdvanceNode(mapData.nodes.Count);
        Refresh();
        CheckEnding();
    }
}
