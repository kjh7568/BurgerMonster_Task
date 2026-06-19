using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 한 장의 UI 표현. BattleSceneUI가 진영/슬롯/CardInstance를 Bind해 사용한다.
/// 일러스트·이름·HP·타입 아이콘을 표시하고, 탭 입력을 OnClicked 이벤트로 라우팅한다.
/// HP 변화·죽음 등 상태가 바뀌면 호출자가 Refresh를 다시 부른다.
/// </summary>
public class CardView : MonoBehaviour
{
    [SerializeField] private Image illustration;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Image hpBar;
    [SerializeField] private Image typeIcon;
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject deadOverlay;
    [SerializeField] private Button button;

    public Side OwningSide { get; private set; }
    public int SlotIndex { get; private set; }
    public CardInstance Bound { get; private set; }

    public event Action<CardView> OnClicked;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(() => OnClicked?.Invoke(this));
        }
    }

    /// <summary>
    /// 이 슬롯이 어떤 진영/위치의 어떤 카드를 표시할지 지정하고 즉시 표시를 갱신한다.
    /// BattleSceneUI가 슬롯 초기화·교체 시 호출.
    /// </summary>
    /// <param name="side">소속 진영(플레이어/적). 클릭 라우팅용.</param>
    /// <param name="slot">전장 슬롯 인덱스(0..fieldSize-1).</param>
    /// <param name="card">표시할 카드 인스턴스. null이면 빈 슬롯으로 비활성.</param>
    public void Bind(Side side, int slot, CardInstance card)
    {
        OwningSide = side;
        SlotIndex = slot;
        Bound = card;
        Refresh();
    }

    /// <summary>
    /// 현재 Bound 상태를 화면에 반영. HP 변화·사망 등 이벤트 발생 시 호출.
    /// Bound가 null이면 GameObject를 비활성화해 빈 슬롯을 표현한다.
    /// </summary>
    public void Refresh()
    {
        if (Bound == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (illustration != null) illustration.sprite = Bound.data.illustration;
        if (nameText != null) nameText.text = Bound.data.cardName;
        if (hpText != null) hpText.text = $"{Bound.CurrentHP}/{Bound.data.baseHP}";
        if (hpBar != null)
        {
            hpBar.fillAmount = Bound.data.baseHP > 0
                ? (float)Bound.CurrentHP / Bound.data.baseHP
                : 0f;
        }
        if (deadOverlay != null) deadOverlay.SetActive(Bound.IsDead);
    }

    public void SetHighlight(bool on)
    {
        if (highlight != null) highlight.SetActive(on);
    }

    public void SetInteractable(bool on)
    {
        if (button != null) button.interactable = on;
    }
}
