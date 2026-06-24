using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 양 진영 field 3슬롯(앞면) + standby 3슬롯(뒷면) 총 12장의 CardView를 인스턴스화하고 Bind한다.
/// 추가로 플레이어 입력 라우터 역할:
///   - 아군 카드 탭 → 액션 오버레이 토글
///   - 공격 버튼 → 적 field 대상 선택
///   - 스킬 버튼 → Skill.TargetMode 에 따라 즉시 발동(None) / 적 대상 선택(Enemy) / 아군 대상 선택(Ally)
///   - 적/아군 카드 탭 → 대상 선택 단계에서 ExecutePlayerAction 라우팅
/// 그 외 게임 상태 이벤트(턴 시작·데미지·사망·자동 배치)를 받아 CardView 갱신.
/// </summary>
public class BattleSceneUI : MonoBehaviour
{
    [SerializeField] private BattleController battle;
    [SerializeField] private GameObject cardViewPrefab;

    [Header("Player Slots")]
    [SerializeField] private RectTransform[] playerFieldSlots;
    [SerializeField] private RectTransform[] playerStandbySlots;

    [Header("Opponent Slots")]
    [SerializeField] private RectTransform[] opponentFieldSlots;
    [SerializeField] private RectTransform[] opponentStandbySlots;

    [Header("Animation")]
    [Tooltip("공격 lunge 동안 카드를 잠시 옮길 Canvas 상단 빈 RectTransform. 비어있으면 Canvas 루트에 자동 폴백.")]
    [SerializeField] private RectTransform animLayer;
    [SerializeField] private float lungeLiftDuration = 0.12f;
    [SerializeField] private float lungeSlamDuration = 0.18f;
    [SerializeField] private float lungeReturnDuration = 0.22f;
    [SerializeField] private float hitShakeDuration = 0.3f;
    [SerializeField] private float hitShakeStrength = 18f;
    [SerializeField] private float flipHalfDuration = 0.3f;

    [Header("Ranged Attack")]
    [Tooltip("UI Image 기반 화살 프리팹. PlayRangedAttack 에서 spawn → 대상까지 직선 트윈 → Destroy. 화살 크기는 이 프리팹의 RectTransform sizeDelta 로 조정.")]
    [SerializeField] private GameObject arrowProjectilePrefab;
    [Tooltip("화살이 대상에 도달하기까지의 시간(s).")]
    [SerializeField] private float arrowFlyDuration = 0.22f;
    [Tooltip("Arrow 스프라이트가 기본적으로 향하는 각도(도). 오른쪽=0, 위=90. 프리팹 일러스트 방향에 맞춰 조정.")]
    [SerializeField] private float arrowSpriteAngleOffset = 0f;

    private CardView[] playerFieldViews;
    private CardView[] playerStandbyViews;
    private CardView[] opponentFieldViews;
    private CardView[] opponentStandbyViews;

    private CardView openedCard;

    private struct PendingDeath { public Side side; public int fieldSlot; }
    private struct PendingSpawn { public Side side; public int fieldSlot; public CardInstance newCard; public int fromStandbyIdx; }
    private readonly List<PendingDeath> pendingDeaths = new List<PendingDeath>();
    private readonly List<PendingSpawn> pendingSpawns = new List<PendingSpawn>();

    public IReadOnlyList<CardView> PlayerFieldViews => playerFieldViews;
    public IReadOnlyList<CardView> PlayerStandbyViews => playerStandbyViews;
    public IReadOnlyList<CardView> OpponentFieldViews => opponentFieldViews;
    public IReadOnlyList<CardView> OpponentStandbyViews => opponentStandbyViews;

    private void Start()
    {
        if (battle == null)
        {
            Debug.LogWarning("[BattleSceneUI] battle 참조가 비어있음. 인스펙터에서 연결 필요.");
            return;
        }

        playerFieldViews = SpawnRow(battle.Player, battle.Player.field, playerFieldSlots, faceUp: true);
        playerStandbyViews = SpawnRow(battle.Player, battle.Player.standby, playerStandbySlots, faceUp: false);
        opponentFieldViews = SpawnRow(battle.Opponent, battle.Opponent.field, opponentFieldSlots, faceUp: true);
        opponentStandbyViews = SpawnRow(battle.Opponent, battle.Opponent.standby, opponentStandbySlots, faceUp: false);

        foreach (var v in playerFieldViews)
        {
            if (v == null) continue;
            v.OnClicked += HandlePlayerCardClicked;
            v.OnAttackPressed += HandleAttackPressed;
            v.OnSkillPressed += HandleSkillPressed;
        }

        foreach (var v in opponentFieldViews)
        {
            if (v == null) continue;
            v.OnClicked += HandleEnemyFieldClicked;
            v.SetInteractable(false); // 평소엔 클릭 안 받음. AwaitTargetSelect 진입 시 valid만 켬.
        }

        battle.OnTurnStarting += HandleTurnStarting;
        battle.OnStateChanged += HandleStateChanged;
        battle.Resolver.OnDamageDealt += HandleDamageDealt;
        battle.Resolver.OnHealApplied += HandleHealApplied;
        battle.Resolver.OnCardDied += HandleCardDied;
        battle.Resolver.OnCardSpawned += HandleCardSpawned;
    }

    private void OnDestroy()
    {
        if (battle != null)
        {
            battle.OnTurnStarting -= HandleTurnStarting;
            battle.OnStateChanged -= HandleStateChanged;
            if (battle.Resolver != null)
            {
                battle.Resolver.OnDamageDealt -= HandleDamageDealt;
                battle.Resolver.OnHealApplied -= HandleHealApplied;
                battle.Resolver.OnCardDied -= HandleCardDied;
                battle.Resolver.OnCardSpawned -= HandleCardSpawned;
            }
        }

        if (playerFieldViews != null)
        {
            foreach (var v in playerFieldViews)
            {
                if (v == null) continue;
                v.OnClicked -= HandlePlayerCardClicked;
                v.OnAttackPressed -= HandleAttackPressed;
                v.OnSkillPressed -= HandleSkillPressed;
            }
        }

        if (opponentFieldViews != null)
        {
            foreach (var v in opponentFieldViews)
            {
                if (v == null) continue;
                v.OnClicked -= HandleEnemyFieldClicked;
            }
        }
    }

    /// <summary>
    /// 플레이어 field 카드 클릭. 상황별 분기:
    ///   - AwaitTargetSelect + 아군 타깃 스킬 진행 중: 클릭한 아군을 대상으로 ExecutePlayerAction.
    ///   - AwaitTargetSelect + 그 외: 취소 후 새 선택 흐름.
    ///   - AwaitCardSelect: 액션 오버레이 토글(같은 카드 닫기 / 다른 카드 전환).
    /// </summary>
    private void HandlePlayerCardClicked(CardView v)
    {
        if (battle == null) return;

        if (battle.State == BattleState.AwaitTargetSelect)
        {
            if (IsAllyTargetSkillInProgress() && v != null && v.Bound != null && !v.Bound.IsDead)
            {
                battle.ExecutePlayerAction(v.SlotIndex);
                return;
            }
            battle.CancelTargetSelect();
        }

        if (!IsSelectable(v)) return;

        if (openedCard == v)
        {
            CloseOpened();
            return;
        }

        if (openedCard != null) openedCard.HideActionPanel();
        v.ShowActionPanel();
        openedCard = v;
    }

    private bool IsSelectable(CardView v)
    {
        if (v == null || v.Bound == null || v.Bound.IsDead) return false;
        if (v.OwningSide == null || !v.OwningSide.isPlayer) return false;
        if (!v.IsFaceUp) return false;
        if (battle != null && battle.State != BattleState.AwaitCardSelect) return false;
        return true;
    }

    private bool IsAllyTargetSkillInProgress()
    {
        if (battle == null) return false;
        if (battle.PendingActionKind != PendingActionKind.Skill) return false;
        int idx = battle.PendingAttackerIdx;
        if (idx < 0 || idx >= battle.Player.field.Length) return false;
        var attacker = battle.Player.field[idx];
        if (attacker == null || attacker.Skill == null) return false;
        return attacker.Skill.TargetMode == SkillTargetMode.Ally;
    }

    private void CloseOpened()
    {
        if (openedCard != null) openedCard.HideActionPanel();
        openedCard = null;
    }

    /// <summary>공격 버튼 → 적 대상 선택 단계 진입.</summary>
    private void HandleAttackPressed(CardView v)
    {
        if (v == null || v.Bound == null || v.Bound.IsDead) return;
        if (battle == null) return;
        if (battle.State != BattleState.AwaitCardSelect && battle.State != BattleState.AwaitActionSelect) return;

        CloseOpened();
        battle.EnterTargetSelect(v.SlotIndex, PendingActionKind.Attack);
    }

    /// <summary>스킬 버튼 → Skill.TargetMode 따라 즉시 발동 또는 대상 선택 진입.</summary>
    private void HandleSkillPressed(CardView v)
    {
        if (v == null || v.Bound == null || v.Bound.IsDead) return;
        if (battle == null) return;
        if (battle.State != BattleState.AwaitCardSelect && battle.State != BattleState.AwaitActionSelect) return;

        var skill = v.Bound.Skill;
        if (skill == null || !skill.IsActive || v.Bound.SkillUsed) return;

        CloseOpened();
        if (skill.TargetMode == SkillTargetMode.None)
            battle.ExecutePlayerSkillImmediate(v.SlotIndex);
        else
            battle.EnterTargetSelect(v.SlotIndex, PendingActionKind.Skill);
    }

    /// <summary>적 field 슬롯 클릭 → AwaitTargetSelect 일 때만 라우팅.</summary>
    private void HandleEnemyFieldClicked(CardView v)
    {
        if (battle == null) return;
        if (battle.State != BattleState.AwaitTargetSelect) return;
        if (v == null || v.Bound == null || v.Bound.IsDead) return;

        battle.ExecutePlayerAction(v.SlotIndex);
    }

    /// <summary>
    /// 상태 전이 훅. AwaitTargetSelect 진입 시 PendingActionKind 에 따라 적 또는 아군 진영을 하이라이트.
    /// 그 외 상태 진입 시 모두 클리어.
    /// </summary>
    private void HandleStateChanged(BattleState s)
    {
        if (s == BattleState.AwaitTargetSelect)
            HighlightValidTargets(battle.PendingAttackerIdx);
        else
            ClearHighlights();
    }

    /// <summary>
    /// 공격이면 적 field, 스킬이면 Skill.TargetMode 에 따라 적/아군 field 의 valid 인덱스만 하이라이트.
    /// </summary>
    private void HighlightValidTargets(int attackerIdx)
    {
        if (battle == null) { ClearHighlights(); return; }
        if (attackerIdx < 0 || attackerIdx >= battle.Player.field.Length) { ClearHighlights(); return; }

        var attacker = battle.Player.field[attackerIdx];
        if (attacker == null || attacker.IsDead) { ClearHighlights(); return; }

        var ctx = battle.BuildContext(battle.Player, attackerIdx);

        HashSet<int> valid;
        CardView[] targetViews;
        bool targetIsEnemy;

        if (battle.PendingActionKind == PendingActionKind.Attack)
        {
            valid = new HashSet<int>(attacker.Attack.GetValidTargets(ctx));
            targetViews = opponentFieldViews;
            targetIsEnemy = true;
        }
        else
        {
            var skill = attacker.Skill;
            valid = new HashSet<int>(skill.GetValidTargets(ctx));
            targetViews = skill.TargetMode == SkillTargetMode.Ally ? playerFieldViews : opponentFieldViews;
            targetIsEnemy = skill.TargetMode != SkillTargetMode.Ally;
        }

        // 양쪽 모두 일단 클리어한 뒤 대상 진영에만 적용.
        ClearHighlightsInternal();

        if (targetViews == null) return;
        for (int i = 0; i < targetViews.Length; i++)
        {
            var view = targetViews[i];
            if (view == null) continue;
            bool on = valid.Contains(i);
            view.SetHighlight(on);
            if (targetIsEnemy) view.SetInteractable(on);
            // 아군 타깃: 플레이어 카드 interactable 은 그대로 유지(이미 true) — HandlePlayerCardClicked가 분기.
        }
    }

    private void ClearHighlights() => ClearHighlightsInternal();

    private void ClearHighlightsInternal()
    {
        if (opponentFieldViews != null)
        {
            foreach (var view in opponentFieldViews)
            {
                if (view == null) continue;
                view.SetHighlight(false);
                view.SetInteractable(false);
            }
        }
        if (playerFieldViews != null)
        {
            foreach (var view in playerFieldViews)
            {
                if (view == null) continue;
                view.SetHighlight(false);
                // 플레이어 interactable 은 건드리지 않음(평소 항상 활성).
            }
        }
    }

    private void HandleTurnStarting(Side side) => RefreshAllField();

    private void HandleDamageDealt(Side side, int slotIdx, int amount)
    {
        var view = GetFieldView(side, slotIdx);
        if (view == null) return;
        view.Refresh();
        view.PlayHitFX();
        StartCoroutine(PlayHitShake(view));
    }

    private void HandleHealApplied(Side side, int slotIdx, int amount)
    {
        var view = GetFieldView(side, slotIdx);
        if (view == null) return;
        view.Refresh();
        view.PlayHealFX();
    }

    /// <summary>
    /// 사망 시 즉시 DeadOverlay 만 노출(Refresh) 후 큐에 적재.
    /// 실제 카드 교체/제거는 공격자 복귀 이후 ProcessPendingSpawns 에서 처리.
    /// </summary>
    private void HandleCardDied(Side side, int slotIdx, CardInstance dyingCard)
    {
        var view = GetFieldView(side, slotIdx);
        if (view != null) view.Refresh(); // Bound 아직 dyingCard → DeadOverlay 표시
        pendingDeaths.Add(new PendingDeath { side = side, fieldSlot = slotIdx });
    }

    /// <summary>
    /// standby→field 자동 배치 이벤트를 큐잉만 — 시각 처리는 ProcessPendingSpawns 에서.
    /// 큐잉 단계에선 어떠한 view 도 손대지 않음(데드뷰도 그대로, standby 뷰도 그대로).
    /// </summary>
    private void HandleCardSpawned(Side side, int fieldSlot, CardInstance newCard, int fromStandbyIdx)
    {
        pendingSpawns.Add(new PendingSpawn { side = side, fieldSlot = fieldSlot, newCard = newCard, fromStandbyIdx = fromStandbyIdx });
    }

    /// <summary>
    /// 한 액션이 끝난 뒤(공격자 복귀 또는 스킬 실행 직후) 호출 — 누적된 사망/스폰 이벤트를 시각화.
    /// 각 spawn 은 standby→field 이동 + 동시 플립 애니메이션. spawn 없는 사망은 dead view 를 정리.
    /// 여러 spawn 은 병렬로 진행해 총 시간 최소화.
    /// </summary>
    public IEnumerator ProcessPendingSpawns()
    {
        if (pendingDeaths.Count == 0 && pendingSpawns.Count == 0) yield break;

        var spawnsSnap = new List<PendingSpawn>(pendingSpawns);
        var deathsSnap = new List<PendingDeath>(pendingDeaths);
        pendingSpawns.Clear();
        pendingDeaths.Clear();

        // 스폰 병렬 실행
        var running = new List<Coroutine>(spawnsSnap.Count);
        foreach (var s in spawnsSnap)
            running.Add(StartCoroutine(AnimateStandbyToField(s.side, s.fromStandbyIdx, s.fieldSlot, s.newCard)));
        foreach (var c in running) yield return c;

        // 교체되지 않은 사망 슬롯은 명시적으로 비움(데드뷰 잔류 방지)
        foreach (var d in deathsSnap)
        {
            bool replaced = false;
            foreach (var s in spawnsSnap)
                if (s.side == d.side && s.fieldSlot == d.fieldSlot) { replaced = true; break; }
            if (!replaced)
            {
                var view = GetFieldView(d.side, d.fieldSlot);
                if (view != null) view.Bind(d.side, d.fieldSlot, null, faceUp: false);
            }
        }
    }

    private void RefreshAllField()
    {
        if (playerFieldViews != null) foreach (var v in playerFieldViews) v?.Refresh();
        if (opponentFieldViews != null) foreach (var v in opponentFieldViews) v?.Refresh();
    }

    /// <summary>외부(AIController 등)에서 임의 진영·슬롯의 field CardView 하이라이트를 토글.</summary>
    public void SetFieldHighlight(Side side, int slotIdx, bool on)
    {
        var view = GetFieldView(side, slotIdx);
        if (view != null) view.SetHighlight(on);
    }

    private CardView GetFieldView(Side side, int slotIdx)
    {
        var arr = side != null && side.isPlayer ? playerFieldViews : opponentFieldViews;
        if (arr == null || slotIdx < 0 || slotIdx >= arr.Length) return null;
        return arr[slotIdx];
    }

    private CardView GetStandbyView(Side side, int slotIdx)
    {
        var arr = side != null && side.isPlayer ? playerStandbyViews : opponentStandbyViews;
        if (arr == null || slotIdx < 0 || slotIdx >= arr.Length) return null;
        return arr[slotIdx];
    }

    // ───────── Animation coroutines (DOTween) ─────────

    /// <summary>공격자 카드를 살짝 띄운 뒤 대상 카드 위치로 슬램. 데미지 적용은 호출자가 본 코루틴 이후에 수행.</summary>
    public IEnumerator PlayAttackLunge(Side atkSide, int atkSlot, Side defSide, int defSlot)
    {
        var atkView = GetFieldView(atkSide, atkSlot);
        var defView = GetFieldView(defSide, defSlot);
        if (atkView == null || defView == null) yield break;

        var rt = (RectTransform)atkView.transform;
        RectTransform layer = animLayer;
        if (layer == null)
        {
            var canvas = atkView.GetComponentInParent<Canvas>();
            if (canvas != null) layer = (RectTransform)canvas.transform;
        }
        if (layer == null) yield break;

        // 캔버스/anim 레이어로 reparent (월드 위치 유지) + 최상단 렌더.
        rt.SetParent(layer, worldPositionStays: true);
        rt.SetAsLastSibling();

        // UI 라 Z축(카메라 방향) 부양이 깨끗하게 안 나옴 → lift 단계 생략.
        // 자기 슬롯에서 적 슬롯으로 단일 DOMove 로 직선 슬램.
        Vector3 slamPos = ((RectTransform)defView.transform).position;
        yield return rt.DOMove(slamPos, lungeSlamDuration).SetEase(Ease.InQuad).WaitForCompletion();
    }

    /// <summary>
    /// 원거리 공격 — 공격자는 제자리에 가만 있고 화살만 직선으로 대상까지 날아간다.
    /// 본 코루틴이 완료된 후 호출자가 데미지 적용 → 기존 OnDamageDealt 체인이 HitFX/Shake 를 자동 트리거.
    /// arrowProjectilePrefab 이 비어있으면 즉시 yield break(데미지만 처리되도록 호출자에 위임).
    /// </summary>
    public IEnumerator PlayRangedAttack(Side atkSide, int atkSlot, Side defSide, int defSlot)
    {
        var atkView = GetFieldView(atkSide, atkSlot);
        var defView = GetFieldView(defSide, defSlot);
        if (atkView == null || defView == null) yield break;
        if (arrowProjectilePrefab == null) yield break;

        Vector3 atkPos = ((RectTransform)atkView.transform).position;
        Vector3 defPos = ((RectTransform)defView.transform).position;

        RectTransform layer = animLayer;
        if (layer == null)
        {
            var canvas = atkView.GetComponentInParent<Canvas>();
            if (canvas != null) layer = (RectTransform)canvas.transform;
        }
        if (layer == null) yield break;

        var arrow = Instantiate(arrowProjectilePrefab, layer);
        var arrowRT = (RectTransform)arrow.transform;
        arrowRT.SetAsLastSibling();
        arrowRT.position = atkPos;
        Vector3 dirToTarget = defPos - atkPos;
        float angle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg + arrowSpriteAngleOffset;
        arrowRT.localEulerAngles = new Vector3(0f, 0f, angle);

        yield return arrowRT.DOMove(defPos, arrowFlyDuration).SetEase(Ease.Linear).WaitForCompletion();
        Destroy(arrow);
    }

    /// <summary>공격 후 원래 슬롯으로 복귀. 부모/스트레치 앵커 복원. 다른 tween 충돌 방지 위해 시작 전 DOKill.</summary>
    public IEnumerator PlayReturnToSlot(Side atkSide, int atkSlot)
    {
        var atkView = GetFieldView(atkSide, atkSlot);
        if (atkView == null) yield break;
        var rt = (RectTransform)atkView.transform;
        var slot = GetFieldSlot(atkSide, atkSlot);
        if (slot == null) yield break;

        // 카운터 데미지로 인한 PlayHitShake 등이 동시에 transform.position 을 만지면 DOMove 가 죽거나 충돌함.
        // 본 메서드가 복귀의 최종 권위 — 진입 시점에 기존 tween 모두 정리.
        rt.DOKill();

        yield return rt.DOMove(slot.position, lungeReturnDuration).SetEase(Ease.InOutQuad).WaitForCompletion();

        rt.SetParent(slot, worldPositionStays: false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 피격 흔들림. 슬롯에 박혀있는 카드에만 적용 — animLayer 로 reparent 된 카드(공격 중 lunge 카드)는 스킵.
    /// 그쪽까지 흔들면 lunge 의 DOMove 와 동일 transform.position 을 두고 충돌해서 복귀 실패 유발.
    /// </summary>
    private IEnumerator PlayHitShake(CardView view)
    {
        if (view == null) yield break;
        var rt = view.transform;
        var slot = GetFieldSlot(view.OwningSide, view.SlotIndex);
        if (slot != null && rt.parent != slot) yield break;

        yield return rt.DOShakePosition(hitShakeDuration, strength: hitShakeStrength, vibrato: 12, randomness: 90, snapping: false, fadeOut: true).WaitForCompletion();
    }

    /// <summary>
    /// standby 카드를 field 빈 슬롯으로 이동시키면서 동시에 플립.
    /// 이동의 주체는 standby 슬롯의 CardView 자체 — 이미 그 위치에 있어 출발점 정확.
    /// 도착 시점에 fieldView 로 인스턴트 스왑(같은 카드·같은 위치라 시각적 단절 없음) + standbyView 는 본래 슬롯으로 복귀해 비활성.
    /// 이동/회전 같은 duration. 회전은 0→90(faceDown)→SetFaceUp(true)→0(faceUp) 반바퀴 — 거울상 방지.
    /// </summary>
    private IEnumerator AnimateStandbyToField(Side side, int standbyIdx, int fieldSlot, CardInstance newCard)
    {
        var standbyView = GetStandbyView(side, standbyIdx);
        var fieldView = GetFieldView(side, fieldSlot);
        var standbySlot = GetStandbySlot(side, standbyIdx);
        var fieldSlotRT = GetFieldSlot(side, fieldSlot);

        // 어느 하나라도 빠지면 인스턴트 폴백
        if (standbyView == null || fieldView == null || standbySlot == null || fieldSlotRT == null)
        {
            if (fieldView != null) fieldView.Bind(side, fieldSlot, newCard, faceUp: true);
            if (standbyView != null) standbyView.Bind(side, standbyIdx, null, faceUp: false);
            yield break;
        }

        RectTransform layer = animLayer;
        if (layer == null)
        {
            var canvas = standbyView.GetComponentInParent<Canvas>();
            if (canvas != null) layer = (RectTransform)canvas.transform;
        }
        if (layer == null)
        {
            fieldView.Bind(side, fieldSlot, newCard, faceUp: true);
            standbyView.Bind(side, standbyIdx, null, faceUp: false);
            yield break;
        }

        var rt = (RectTransform)standbyView.transform;
        rt.DOKill();
        rt.SetParent(layer, worldPositionStays: true);
        rt.SetAsLastSibling();
        rt.localEulerAngles = Vector3.zero;

        Vector3 endPos = fieldSlotRT.position;
        float total = flipHalfDuration * 2f;

        // 이동(전체 duration) — 백그라운드. 회전 yield 와 동시 진행.
        rt.DOMove(endPos, total).SetEase(Ease.InOutQuad);

        // 회전 0→90 (뒷면이 사라지는 구간)
        yield return rt.DOLocalRotate(new Vector3(0f, 90f, 0f), flipHalfDuration).SetEase(Ease.InQuad).WaitForCompletion();
        standbyView.SetFaceUp(true);
        // 회전 90→0 (앞면이 나타나는 구간) — 이 사이 이동도 계속 진행
        yield return rt.DOLocalRotate(Vector3.zero, flipHalfDuration).SetEase(Ease.OutQuad).WaitForCompletion();

        // 이동 완료 보장.
        rt.position = endPos;

        // 스왑: field view 가 인계받음, standby view 는 비우고 본래 슬롯으로 복귀.
        fieldView.Bind(side, fieldSlot, newCard, faceUp: true);

        standbyView.Bind(side, standbyIdx, null, faceUp: false);
        rt.SetParent(standbySlot, worldPositionStays: false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private RectTransform GetFieldSlot(Side side, int slotIdx)
    {
        var arr = side != null && side.isPlayer ? playerFieldSlots : opponentFieldSlots;
        if (arr == null || slotIdx < 0 || slotIdx >= arr.Length) return null;
        return arr[slotIdx];
    }

    private RectTransform GetStandbySlot(Side side, int slotIdx)
    {
        var arr = side != null && side.isPlayer ? playerStandbySlots : opponentStandbySlots;
        if (arr == null || slotIdx < 0 || slotIdx >= arr.Length) return null;
        return arr[slotIdx];
    }

    // ───────── Spawn ─────────

    private CardView[] SpawnRow(Side side, CardInstance[] cards, RectTransform[] slots, bool faceUp)
    {
        var views = new CardView[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            var go = Instantiate(cardViewPrefab, slots[i]);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            var view = go.GetComponent<CardView>();
            var card = i < cards.Length ? cards[i] : null;
            view.Bind(side, i, card, faceUp);
            views[i] = view;
        }
        return views;
    }
}
