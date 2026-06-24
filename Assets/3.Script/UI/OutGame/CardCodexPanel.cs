using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 도감 패널. GameAssetsSO.allCards 전체를 그리드에 작은 CardView 로 펼치고, 카드 탭 시 풀스크린 상세 오버레이를 띄운다.
/// 상세 오버레이는 CardView prefab 을 재활용하지 않고 패널 안에 미리 배치한 자체 Image/TMP_Text 슬롯에 데이터를 그린다 — 라벨/일러스트 비율 자유롭게 조정 가능.
/// 오버레이는 전체 영역을 덮는 IPointerClickHandler 백드롭 탭으로 닫힌다.
///
/// MVP(카드 4종) 단계에서는 해금 상태와 무관하게 전부 보인다. CardDex.IsDiscovered 분기 / 실루엣 처리는 카드 풀이 늘어날 때 활성화.
/// </summary>
public class CardCodexPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [Tooltip("우상단 등 헤더 텍스트. \"카드 도감   N / M 해금\" 형식. 비워두면 표시 생략.")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private string headerFormat = "카드 도감   {0} / {1} 해금";
    [SerializeField] private Button closeButton;

    [Header("Grid")]
    [Tooltip("CardView 인스턴스가 자식으로 채워질 컨테이너. GridLayoutGroup 권장.")]
    [SerializeField] private RectTransform gridContainer;
    [Tooltip("도감 셀로 쓸 CardView prefab. 인게임에서 쓰는 것과 동일.")]
    [SerializeField] private CardView cardViewPrefab;

    [Header("Detail Overlay")]
    [Tooltip("선택 카드의 큰 상세를 표시하는 자체 패널. CardView 가 아니다 — 아래 image/text 슬롯에 직접 주입.")]
    [SerializeField] private CardCodexDetail detail;

    private readonly List<CardView> spawnedCells = new List<CardView>();
    private bool built;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (detail != null) detail.HideImmediate();
    }

    public void Show()
    {
        if (root != null) root.SetActive(true);
        EnsureBuilt();
        RefreshHeader();
        if (detail != null) detail.HideImmediate();
    }

    public void Hide()
    {
        if (detail != null) detail.HideImmediate();
        if (root != null) root.SetActive(false);
    }

    /// <summary>그리드 인스턴스 생성. 첫 Show 에 1회만 실행 — 카드 목록은 런타임에 바뀌지 않으므로 재생성 불필요.</summary>
    private void EnsureBuilt()
    {
        if (built) return;
        built = true;
        if (gridContainer == null || cardViewPrefab == null) return;
        foreach (var c in CardDex.AllCards)
        {
            // 셀이 노출됐다 = 해금됐다. MVP 단계(카드 4종 전부 노출) 정책의 일관성. 실루엣 분기를 도입할 때 이 줄은 IsDiscovered 분기 안으로 들어간다.
            CardDex.Register(c);
            var view = Instantiate(cardViewPrefab, gridContainer);
            view.gameObject.SetActive(true);
            view.name = $"CodexCell_{c.cardName}";
            // 도감 셀은 강화/HP 보너스 무관 — 0 으로 고정해서 baseHP 그대로 표시.
            view.BindPreview(c, 0, 0, interactable: true);
            var captured = c;
            view.OnClicked += _ => OpenDetail(captured);
            spawnedCells.Add(view);
        }
    }

    private void RefreshHeader()
    {
        if (headerText == null) return;
        int total = 0;
        int discovered = 0;
        foreach (var c in CardDex.AllCards)
        {
            total++;
            if (CardDex.IsDiscovered(GameAssetsSO.CardId(c))) discovered++;
        }
        headerText.text = string.Format(headerFormat, discovered, total);
    }

    private void OpenDetail(CardDataSO card)
    {
        if (detail == null || card == null) return;
        detail.Show(card);
    }
}
