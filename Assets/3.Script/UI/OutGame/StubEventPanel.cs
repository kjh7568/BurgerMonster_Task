using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Upgrade/Recruit 노드의 임시 패널. 본 구현 전 진행 흐름만 확인할 수 있도록 'Coming Soon' 안내 후
/// 확인 버튼으로 노드를 강제 클리어한다.
/// </summary>
public class StubEventPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button confirmButton;

    [SerializeField] private string title = "Coming Soon";
    [SerializeField] private string body = "이 노드는 아직 준비 중입니다.\n넘어가시겠습니까?";

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

    public void Show(Action onConfirmed)
    {
        onConfirm = onConfirmed;
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        if (root != null) root.SetActive(true);
    }

    private void HandleConfirm()
    {
        if (root != null) root.SetActive(false);
        var cb = onConfirm;
        onConfirm = null;
        cb?.Invoke();
    }
}
