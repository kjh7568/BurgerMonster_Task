using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감에서 카드 한 장 클릭 시 뜨는 풀스크린 상세. CardView prefab 을 쓰지 않고 자체 Image/TMP_Text 슬롯에 데이터를 주입한다.
/// 백드롭(rootBlocker) 어디든 탭하면 닫힌다 — IPointerClickHandler 는 호환성 위해 별도 자식 컴포넌트(CardCodexBackdropTap) 가 처리.
/// </summary>
public class CardCodexDetail : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("상세 오버레이 루트. Show/HideImmediate 시 활성/비활성.")]
    [SerializeField] private GameObject root;
    [Tooltip("화면 전체를 덮는 풀스크린 Button. onClick 으로 닫기 연결. 비워두면 자동으로 root 의 BackdropTap 컴포넌트를 찾아 연결.")]
    [SerializeField] private Button backdropButton;

    [Header("Card Visual")]
    [SerializeField] private Image illustration;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text classText;
    [SerializeField] private TMP_Text hpText;
    [Tooltip("스킬/플레이버 설명. CardDataSO.description 그대로 표시.")]
    [SerializeField] private TMP_Text skillText;

    [Header("Class Display")]
    [Tooltip("CardType ↔ 한글 표시. 비워두면 enum 이름 그대로.")]
    [SerializeField] private ClassLabelPair[] classLabels;

    [System.Serializable]
    public struct ClassLabelPair
    {
        public CardType type;
        public string label;
    }

    private void Awake()
    {
        if (backdropButton != null) backdropButton.onClick.AddListener(HideImmediate);
    }

    public void Show(CardDataSO card)
    {
        if (root != null) root.SetActive(true);
        if (card == null) return;

        if (illustration != null)
        {
            illustration.sprite = card.illustration;
            illustration.enabled = card.illustration != null;
        }
        if (nameText != null) nameText.text = card.cardName;
        if (classText != null) classText.text = LookupClassLabel(card.type);
        if (hpText != null) hpText.text = $"HP {card.baseHP}";
        if (skillText != null) skillText.text = BuildSkillText(card);
    }

    /// <summary>CardDataSO 의 skillName/skillDescription 을 조합해 표시. 도감은 강화 정보 없음 — 베이스 설명만.</summary>
    private static string BuildSkillText(CardDataSO card)
    {
        if (string.IsNullOrEmpty(card.skillName) && string.IsNullOrEmpty(card.skillDescription)) return string.Empty;
        return $"<b>[{card.skillName}]</b>\n{card.skillDescription}";
    }

    public void HideImmediate()
    {
        if (root != null) root.SetActive(false);
    }

    private string LookupClassLabel(CardType type)
    {
        if (classLabels != null)
            foreach (var p in classLabels)
                if (p.type == type && !string.IsNullOrEmpty(p.label)) return p.label;
        return type.ToString();
    }
}
