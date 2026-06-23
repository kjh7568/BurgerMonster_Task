using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DeckLayoutPanel 상단의 카드 상세 영역. 선택된 카드 한 장의 일러스트/이름/클래스/HP/스킬/강화 표시.
/// 카드 미선택 상태에서는 Clear() 로 모든 텍스트/이미지를 비움.
/// </summary>
public class DeckDetailView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image illustration;
    [SerializeField] private TMP_Text classLabel;   // CardType 텍스트 (기사/궁수/광전사/사제)
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;       // "HP 20 (+2)" 형태
    [SerializeField] private TMP_Text skillText;   // 스킬 설명 + (수치 보너스)
    [SerializeField] private TMP_Text descriptionText; // CardDataSO.description (옵션)

    [Header("Class Display")]
    [Tooltip("CardType ↔ 한글 표시명 매핑. 비워두면 enum 이름 그대로 표시.")]
    [SerializeField] private ClassLabelPair[] classLabels;

    [System.Serializable]
    public struct ClassLabelPair
    {
        public CardType type;
        public string label;
    }

    public void Clear()
    {
        if (illustration != null) { illustration.sprite = null; illustration.enabled = false; }
        if (classLabel != null) classLabel.text = string.Empty;
        if (nameText != null) nameText.text = string.Empty;
        if (hpText != null) hpText.text = string.Empty;
        if (skillText != null) skillText.text = string.Empty;
        if (descriptionText != null) descriptionText.text = string.Empty;
    }

    public void Bind(CardDataSO data, int deckIndex)
    {
        if (data == null) { Clear(); return; }

        if (illustration != null)
        {
            illustration.sprite = data.illustration;
            illustration.enabled = data.illustration != null;
        }
        if (classLabel != null) classLabel.text = LookupClassLabel(data.type);
        if (nameText != null) nameText.text = data.cardName;

        int hpBonus = RunState.GlobalHpBonus + RunState.GetPerCardHpBonus(deckIndex);
        int maxHp = Mathf.Max(1, data.baseHP + hpBonus);
        if (hpText != null)
            hpText.text = hpBonus > 0 ? $"HP {maxHp} (+{hpBonus})" : $"HP {maxHp}";

        int skillBonus = RunState.GetPerCardSkillBonus(deckIndex);
        if (skillText != null)
            skillText.text = skillBonus > 0 ? $"스킬 +{skillBonus}" : string.Empty;

        if (descriptionText != null) descriptionText.text = data.description ?? string.Empty;
    }

    private string LookupClassLabel(CardType type)
    {
        if (classLabels != null)
            foreach (var p in classLabels)
                if (p.type == type && !string.IsNullOrEmpty(p.label)) return p.label;
        return type.ToString();
    }
}
