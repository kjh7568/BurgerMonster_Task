using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 한 장의 UI 표현. BattleSceneUI가 진영/슬롯/CardInstance/면 상태를 Bind해 사용한다.
/// faceUp=true: 일러스트·이름·HP·타입 아이콘을 표시하고 탭을 OnClicked로 라우팅.
/// faceUp=false: 뒷면(FaceDownRoot)만 표시하고 클릭 비활성.
/// HP 변화·죽음 등 상태가 바뀌면 호출자가 Refresh를 다시 부른다.
/// </summary>
public class CardView : MonoBehaviour
{
    [Header("Face Roots")]
    [SerializeField] private GameObject faceUpRoot;
    [SerializeField] private GameObject faceDownRoot;

    [Header("Face Up Children")]
    [SerializeField] private Image illustration;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Image hpBar;
    [SerializeField] private Image typeIcon;
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject deadOverlay;
    [SerializeField] private Button button;

    [Header("Action Overlay")]
    [SerializeField] private GameObject actionOverlay;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button skillButton;

    [Header("Type Icon Mapping")]
    [Tooltip("CardType ↔ Sprite 매핑. 인스펙터에서 4종(Normal/Ranged/Mighty/Healer) 쌍을 채워둠.")]
    [SerializeField] private TypeIconPair[] typeIcons;

    [Serializable]
    public struct TypeIconPair
    {
        public CardType type;
        public Sprite icon;
    }

    [Header("FX")]
    [SerializeField] private GameObject bloodHitFX;
    [SerializeField] private GameObject healFX;

    public Side OwningSide { get; private set; }
    public int SlotIndex { get; private set; }
    public CardInstance Bound { get; private set; }
    public bool IsFaceUp { get; private set; }
    public bool IsActionPanelOpen => actionOverlay != null && actionOverlay.activeSelf;

    public event Action<CardView> OnClicked;
    public event Action<CardView> OnAttackPressed;
    public event Action<CardView> OnSkillPressed;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(() => OnClicked?.Invoke(this));
        if (attackButton != null)
            attackButton.onClick.AddListener(() => OnAttackPressed?.Invoke(this));
        if (skillButton != null)
            skillButton.onClick.AddListener(() => OnSkillPressed?.Invoke(this));
        HideActionPanel();
    }

    /// <summary>
    /// 어떤 진영/슬롯의 어떤 카드를 어떤 면으로 표시할지 지정하고 즉시 갱신한다.
    /// BattleSceneUI가 슬롯 초기화·교체 시 호출.
    /// </summary>
    /// <param name="side">소속 진영. 클릭 라우팅용.</param>
    /// <param name="slot">슬롯 인덱스. field/standby 각각의 내부 인덱스(0..2).</param>
    /// <param name="card">표시할 카드 인스턴스. null이면 빈 슬롯으로 비활성.</param>
    /// <param name="faceUp">앞면(true)/뒷면(false). 뒷면은 클릭 비활성.</param>
    public void Bind(Side side, int slot, CardInstance card, bool faceUp)
    {
        OwningSide = side;
        SlotIndex = slot;
        Bound = card;
        IsFaceUp = faceUp;
        HideActionPanel();
        if (button != null) button.interactable = faceUp;
        Refresh();
    }

    /// <summary>
    /// 현재 Bound/IsFaceUp 상태를 화면에 반영. HP·사망 등 변화 시 호출.
    /// Bound가 null이면 GameObject 자체를 비활성화. 뒷면이면 내용 갱신 생략.
    /// </summary>
    public void Refresh()
    {
        if (Bound == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        if (faceUpRoot != null) faceUpRoot.SetActive(IsFaceUp);
        if (faceDownRoot != null) faceDownRoot.SetActive(!IsFaceUp);
        // button.interactable은 외부(SetInteractable / Bind / SetFaceUp)가 소유. Refresh는 표시만 갱신.

        if (!IsFaceUp) return;

        if (illustration != null) illustration.sprite = Bound.data.illustration;
        if (nameText != null) nameText.text = Bound.data.cardName;
        if (typeIcon != null) typeIcon.sprite = LookupTypeIcon(Bound.data.type);
        if (hpText != null) hpText.text = $"{Bound.CurrentHP}/{Bound.MaxHP}";

        bool hasActiveSkill = Bound.Skill != null && Bound.Skill.IsActive;
        if (skillButton != null)
        {
            skillButton.gameObject.SetActive(hasActiveSkill);
            skillButton.interactable = hasActiveSkill && !Bound.SkillUsed;
        }
        if (hpBar != null)
            hpBar.fillAmount = Bound.MaxHP > 0
                ? (float)Bound.CurrentHP / Bound.MaxHP
                : 0f;
        if (deadOverlay != null) deadOverlay.SetActive(Bound.IsDead);
        if (Bound.IsDead) HideActionPanel();
    }

    /// <summary>
    /// 전투 외 미리보기 모드(강화 이벤트 그리드 등). CardInstance 없이 CardDataSO + 보너스로만 표시.
    /// MaxHP = baseHP + hpBonus 로 고정(variance 미적용). 액션 오버레이/스킬 버튼은 강제 숨김.
    /// 클릭 활성/비활성은 button.interactable 로 처리 — 비활성 시각화는 Button 의 Disabled Color 사용.
    /// </summary>
    public void BindPreview(CardDataSO data, int hpBonus, int skillBonus, bool interactable)
    {
        OwningSide = null;
        SlotIndex = -1;
        Bound = null;
        IsFaceUp = true;
        HideActionPanel();

        gameObject.SetActive(true);
        if (faceUpRoot != null) faceUpRoot.SetActive(true);
        if (faceDownRoot != null) faceDownRoot.SetActive(false);

        if (data != null)
        {
            if (illustration != null) illustration.sprite = data.illustration;
            if (nameText != null) nameText.text = data.cardName;
            if (typeIcon != null) typeIcon.sprite = LookupTypeIcon(data.type);
            int maxHp = Mathf.Max(1, data.baseHP + hpBonus);
            string hpSuffix = (hpBonus > 0 || skillBonus > 0) ? BuildBonusSuffix(hpBonus, skillBonus) : string.Empty;
            if (hpText != null) hpText.text = $"{maxHp}/{maxHp}{hpSuffix}";
            if (hpBar != null) hpBar.fillAmount = 1f;
        }
        if (deadOverlay != null) deadOverlay.SetActive(false);
        if (skillButton != null) skillButton.gameObject.SetActive(false);
        if (button != null) button.interactable = interactable;
    }

    private static string BuildBonusSuffix(int hpBonus, int skillBonus)
    {
        if (hpBonus > 0 && skillBonus > 0) return $" (HP+{hpBonus}, SK+{skillBonus})";
        if (hpBonus > 0) return $" (+{hpBonus})";
        if (skillBonus > 0) return $" (SK+{skillBonus})";
        return string.Empty;
    }

    /// <summary>면 상태만 바꾸고 즉시 반영. Bound 카드는 그대로 유지. 뒷면이면 액션 패널 강제 닫음.</summary>
    public void SetFaceUp(bool up)
    {
        IsFaceUp = up;
        if (!up) HideActionPanel();
        if (button != null) button.interactable = up;
        Refresh();
    }

    public void ShowActionPanel()
    {
        if (actionOverlay != null) actionOverlay.SetActive(true);
    }

    public void HideActionPanel()
    {
        if (actionOverlay != null) actionOverlay.SetActive(false);
    }

    public void SetHighlight(bool on)
    {
        if (highlight != null) highlight.SetActive(on);
    }

    /// <summary>외부에서 클릭 가능 여부 조정. 뒷면 상태에선 절대 활성화되지 않는다.</summary>
    public void SetInteractable(bool on)
    {
        if (button != null) button.interactable = on && IsFaceUp;
    }

    private Sprite LookupTypeIcon(CardType t)
    {
        if (typeIcons == null) return null;
        foreach (var p in typeIcons)
            if (p.type == t) return p.icon;
        return null;
    }

    public void PlayHitFX() => PlayFX(bloodHitFX);
    public void PlayHealFX() => PlayFX(healFX);

    /// <summary>ParticleSystem 이면 Stop+Play 로 재시작, 아니면 GameObject 토글로 Animator 재트리거.</summary>
    private void PlayFX(GameObject fx)
    {
        if (fx == null) return;
        var ps = fx.GetComponent<UnityEngine.ParticleSystem>();
        if (ps != null)
        {
            fx.SetActive(true);
            ps.Stop(true, UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }
        else
        {
            fx.SetActive(false);
            fx.SetActive(true);
        }
    }
}
