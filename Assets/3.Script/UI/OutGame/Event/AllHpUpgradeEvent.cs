using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 강화 이벤트 — "모든 카드 최대 HP +N". 카드 선택 없이 안내 + 확인만.
/// </summary>
public class AllHpUpgradeEvent : UpgradeEventBase
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button confirmButton;

    [SerializeField] private string title = "축복의 빛";
    [SerializeField] private string body = "모든 카드의 최대 체력이 +{0} 증가합니다.";
    [SerializeField] private int amount = 1;

    private Action onConfirm;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirm);
    }

    private void OnDestroy()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveListener(HandleConfirm);
    }

    public override bool CanRun() => RunState.PlayerDeck != null && RunState.PlayerDeck.Count > 0;

    public override void Show(Action onConfirmed)
    {
        onConfirm = onConfirmed;
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = string.Format(body, amount);
        if (root != null) root.SetActive(true);
    }

    private void HandleConfirm()
    {
        RunState.ApplyGlobalHpUpgrade(amount);
        if (root != null) root.SetActive(false);
        var cb = onConfirm;
        onConfirm = null;
        cb?.Invoke();
    }
}
