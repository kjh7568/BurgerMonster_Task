using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 지도의 개별 노드 칸. 아이콘 + 인덱스 라벨 + Button.
/// 상태(현재/진행 가능/이미 완료/잠금)에 따라 색을 변경하고 상호작용 잠금.
/// </summary>
[RequireComponent(typeof(Button))]
public class MapNodeButton : MonoBehaviour
{
    public enum NodeVisualState { Locked, Available, Current, Cleared }

    [SerializeField] private Image iconImage;
    [SerializeField] private Image highlightRing;

    [Header("Tint")]
    [SerializeField] private Color lockedTint = new Color(0.4f, 0.4f, 0.4f, 0.6f);
    [SerializeField] private Color availableTint = Color.white;
    [SerializeField] private Color currentTint = new Color(1f, 0.95f, 0.4f);
    [SerializeField] private Color clearedTint = new Color(0.6f, 0.6f, 0.6f, 0.85f);

    public int NodeIndex { get; private set; }
    public NodeType NodeType { get; private set; }

    private Button button;
    private Action<int> clickCallback;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);
    }

    public void Bind(int index, NodeType type, Sprite icon, string label, Action<int> onClick)
    {
        NodeIndex = index;
        NodeType = type;
        clickCallback = onClick;
        if (iconImage != null) iconImage.sprite = icon;
    }

    /// <summary>외부에서 강제로 버튼 활성/비활성. 덱 패널 열려있는 동안 노드 진입을 막는 용도. 다음 SetVisualState 호출 시 정상 상태로 복귀.</summary>
    public void SetInteractable(bool on)
    {
        if (button != null) button.interactable = on;
    }

    public void SetVisualState(NodeVisualState state)
    {
        var tint = state switch
        {
            NodeVisualState.Locked => lockedTint,
            NodeVisualState.Available => availableTint,
            NodeVisualState.Current => currentTint,
            NodeVisualState.Cleared => clearedTint,
            _ => availableTint,
        };
        if (iconImage != null) iconImage.color = tint;
        if (highlightRing != null) highlightRing.gameObject.SetActive(state == NodeVisualState.Current);
        if (button != null) button.interactable = state == NodeVisualState.Current;
    }

    private void OnClick() => clickCallback?.Invoke(NodeIndex);
}
