using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PendingActionKind { Attack, Skill }

[DefaultExecutionOrder(-100)]
public class BattleController : MonoBehaviour
{
    [SerializeField] BattleConfigSO config;
    [SerializeField] DifficultyTableSO difficultyTable;
    [SerializeField] BattleSceneUI sceneUI; // 애니메이션 오케스트레이션용. null 이어도 게임 로직은 동작.

    [Header("Fallback (RunState.PlayerDeck 비어있을 때만)")]
    [Tooltip("MapScene 을 거치지 않고 BattleScene 을 단독 실행할 때 사용할 시작 풀. 정상 플로우에선 RunState.PlayerDeck 이 이미 채워져 있음.")]
    [SerializeField] StartingDeckSO fallbackStartingDeck;

    public BattleConfigSO Config => config;

    public Side Player { get; private set; }
    public Side Opponent { get; private set; }
    public BattleState State { get; private set; }
    public Side CurrentSide { get; private set; }
    public Side Winner { get; private set; }
    public DamageResolver Resolver { get; private set; }

    /// <summary>이번 전투에 적용된 적 풀. 승리 시 goldReward 가산을 위해 보관.</summary>
    public EnemyPoolSO CurrentEnemyPool { get; private set; }

    public int TurnNumber { get; private set; }
    public int PendingAttackerIdx { get; private set; } = -1;
    public PendingActionKind PendingActionKind { get; private set; } = PendingActionKind.Attack;

    public event Action<BattleState> OnStateChanged;
    public event Action<Side> OnTurnStarting;
    public event Action<Side> OnTurnStarted;
    public event Action<Side> OnTurnEnded;
    public event Action<Side> OnGameEnded;

    /// <summary>이 값이 설정된 상태로 BattleScene 이 로드되면 Init 이 정상 빌드 대신 복원 경로를 탄다. 1회용 — Init 진입 시 소비된다.</summary>
    public static BattleSnapshot PendingRestoreSnapshot;

    private void Start() => Init();

    private void Init()
    {
        State = BattleState.Init;
        Resolver = new DamageResolver();

        var pending = PendingRestoreSnapshot;
        PendingRestoreSnapshot = null;
        if (pending != null && TryRestoreFromSnapshot(pending)) return;

        var playerCards = BuildPlayerCards(config.fieldSlotCount);
        var opponentCards = BuildOpponentCards(config.fieldSlotCount);
        Player = new Side(true, playerCards, config.fieldSlotCount);
        Opponent = new Side(false, opponentCards, config.fieldSlotCount);
        SetState(BattleState.PlayerTurnStart);
    }

    /// <summary>스냅샷 → 양 진영 Side 재구성 + 턴 상태 복원. 실패 시 false 반환해 일반 Init 으로 폴백.</summary>
    private bool TryRestoreFromSnapshot(BattleSnapshot snap)
    {
        int slots = config.fieldSlotCount;
        if (snap.playerField == null || snap.opponentField == null) return false;

        var pField = BuildRestoredRow(snap.playerField, slots);
        var pStand = BuildRestoredRow(snap.playerStandby, slots);
        var oField = BuildRestoredRow(snap.opponentField, slots);
        var oStand = BuildRestoredRow(snap.opponentStandby, slots);
        if (pField == null || pStand == null || oField == null || oStand == null) return false;

        Player = Side.CreateRestored(true, pField, pStand);
        Opponent = Side.CreateRestored(false, oField, oStand);
        CurrentEnemyPool = GameAssetsSO.ResolveEnemyPool(snap.enemyPoolId);
        TurnNumber = Mathf.Max(0, snap.turnNumber);

        // 안전한 체크포인트만 저장되므로 무조건 플레이어 입력 대기 상태로 복원.
        CurrentSide = Player;
        State = BattleState.AwaitCardSelect;
        Debug.Log($"[Battle] Restored from snapshot — turn={TurnNumber}");
        return true;
    }

    private static CardInstance[] BuildRestoredRow(System.Collections.Generic.List<CardInstanceSnapshot> snaps, int slots)
    {
        var row = new CardInstance[slots];
        if (snaps == null) return row;
        for (int i = 0; i < slots && i < snaps.Count; i++)
        {
            var s = snaps[i];
            if (s == null || string.IsNullOrEmpty(s.cardId)) continue;
            var data = GameAssetsSO.ResolveCard(s.cardId);
            if (data == null) { Debug.LogError($"[Battle] 전투 복원 실패 — GameAssetsSO.allCards 에 cardId '{s.cardId}' 가 없음. Resources/GameAssets.asset 의 All Cards 배열에 등록 필요. 슬롯이 비워진 채로 전투 시작됨."); continue; }
            row[i] = CardInstance.CreateRestored(data, s.maxHP, s.currentHP, s.skillBonus, s.skillUsed, s.isTaunting, s.lastStandUsed);
        }
        return row;
    }

    /// <summary>현재 전투 상태를 직렬화. AwaitCardSelect 등 안정 시점에서 호출 권장.</summary>
    public BattleSnapshot CaptureSnapshot()
    {
        if (State == BattleState.Init || State == BattleState.GameEnd) return null;
        if (Player == null || Opponent == null) return null;
        var snap = new BattleSnapshot
        {
            enemyPoolId = GameAssetsSO.EnemyPoolId(CurrentEnemyPool),
            turnNumber = TurnNumber,
            isPlayerTurn = CurrentSide == Player,
        };
        FillRow(snap.playerField, Player.field);
        FillRow(snap.playerStandby, Player.standby);
        FillRow(snap.opponentField, Opponent.field);
        FillRow(snap.opponentStandby, Opponent.standby);
        return snap;
    }

    private static void FillRow(System.Collections.Generic.List<CardInstanceSnapshot> list, CardInstance[] row)
    {
        if (row == null) return;
        for (int i = 0; i < row.Length; i++)
        {
            var c = row[i];
            if (c == null) { list.Add(new CardInstanceSnapshot()); continue; }
            list.Add(new CardInstanceSnapshot
            {
                cardId = GameAssetsSO.CardId(c.data),
                maxHP = c.MaxHP,
                currentHP = c.CurrentHP,
                skillBonus = c.SkillBonus,
                skillUsed = c.SkillUsed,
                isTaunting = c.IsTaunting,
                lastStandUsed = c.LastStandUsed,
            });
        }
    }

    /// <summary>
    /// RunState.PlayerDeck 에서 (필요 슬롯 수 × 2) 만큼 인스턴스화. 비어있으면 fallbackStartingDeck 에서 셔플 픽.
    /// variance 는 CardInstance 생성자에서 자동 적용.
    /// </summary>
    private List<CardInstance> BuildPlayerCards(int fieldSize)
    {
        int needed = fieldSize * 2;
        var sources = new List<CardDataSO>();
        if (RunState.PlayerDeck != null && RunState.PlayerDeck.Count > 0)
        {
            sources.AddRange(RunState.PlayerDeck);
        }
        else if (fallbackStartingDeck != null && fallbackStartingDeck.cards != null && fallbackStartingDeck.cards.Length > 0)
        {
            sources.AddRange(ShufflePickWithRefill(fallbackStartingDeck.cards, needed));
            Debug.LogWarning("[Battle] RunState.PlayerDeck 비어있음 — fallbackStartingDeck 사용 (BattleScene 단독 테스트 모드).");
        }
        else
        {
            Debug.LogError("[Battle] 플레이어 카드 소스 없음 (RunState/fallback 둘 다 비어있음).");
        }

        // RunState.PlayerDeck 에서 가져온 카드만 강화 보너스 적용 (fallback 경로는 보너스 없이).
        bool fromRunState = RunState.PlayerDeck != null && RunState.PlayerDeck.Count > 0;
        int globalHpBonus = fromRunState ? RunState.GlobalHpBonus : 0;

        var result = new List<CardInstance>(needed);
        for (int i = 0; i < sources.Count && i < needed; i++)
        {
            int hpBonus = globalHpBonus + (fromRunState ? RunState.GetPerCardHpBonus(i) : 0);
            int skillBonus = fromRunState ? RunState.GetPerCardSkillBonus(i) : 0;
            result.Add(new CardInstance(sources[i], hpBonus, skillBonus));
        }
        return result;
    }

    /// <summary>
    /// 현재 stage 의 EnemyPool 에서 weighted pick. statBonusHP 를 CardInstance 생성자에 주입.
    /// 상호 HP 데미지 모델이므로 HP 보정 = 공격력 보정도 겸함.
    /// 풀/테이블 미설정 시 빈 리스트 — 즉시 게임 종료 판정.
    /// </summary>
    private List<CardInstance> BuildOpponentCards(int fieldSize)
    {
        int needed = fieldSize * 2;
        var result = new List<CardInstance>(needed);
        if (difficultyTable == null)
        {
            Debug.LogError("[Battle] DifficultyTableSO 미설정.");
            return result;
        }
        var pool = difficultyTable.Resolve(RunState.Stage);
        if (pool == null || pool.enemies == null || pool.enemies.Length == 0)
        {
            Debug.LogError($"[Battle] stage={RunState.Stage} 에 해당하는 EnemyPool 없음.");
            return result;
        }
        CurrentEnemyPool = pool;
        for (int i = 0; i < needed; i++)
        {
            var data = WeightedPick(pool.enemies);
            if (data == null) break;
            result.Add(new CardInstance(data, pool.statBonusHP));
        }
        Debug.Log($"[Battle] Opponent built from pool '{pool.name}' (stage={RunState.Stage}, +HP={pool.statBonusHP}, gold={pool.goldReward})");
        return result;
    }

    private static CardDataSO WeightedPick(EnemyPoolSO.EnemyEntry[] entries)
    {
        int total = 0;
        for (int i = 0; i < entries.Length; i++) total += Mathf.Max(0, entries[i].weight);
        if (total <= 0) return entries.Length > 0 ? entries[0].card : null;
        int roll = UnityEngine.Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            acc += Mathf.Max(0, entries[i].weight);
            if (roll < acc) return entries[i].card;
        }
        return entries[entries.Length - 1].card;
    }

    /// <summary>풀 전체 셔플로 우선 채우고, 부족분은 중복 허용 랜덤. #30 의 variety 우선 정책.</summary>
    private static List<CardDataSO> ShufflePickWithRefill(CardDataSO[] pool, int count)
    {
        var result = new List<CardDataSO>(count);
        var indices = new List<int>(pool.Length);
        for (int i = 0; i < pool.Length; i++) indices.Add(i);
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        foreach (var idx in indices)
        {
            if (result.Count >= count) break;
            result.Add(pool[idx]);
        }
        while (result.Count < count) result.Add(pool[UnityEngine.Random.Range(0, pool.Length)]);
        return result;
    }

    public BattleContext BuildContext(Side attacker, int attackerIdx)
    {
        return new BattleContext
        {
            attackerSide = attacker,
            attackerIndex = attackerIdx,
            defenderSide = attacker == Player ? Opponent : Player,
            resolver = Resolver,
        };
    }

    private void SetState(BattleState s)
    {
        State = s;
        Debug.Log($"[State] {s}");
        OnStateChanged?.Invoke(s);

        switch (s)
        {
            case BattleState.PlayerTurnStart:
                TurnNumber++;
                CurrentSide = Player;
                RunTurnStart(CurrentSide);
                SetState(BattleState.AwaitCardSelect);
                break;
            case BattleState.OpponentTurnStart:
                TurnNumber++;
                CurrentSide = Opponent;
                RunTurnStart(CurrentSide);
                SetState(BattleState.OpponentTurnAction);
                break;
        }
    }

    private void RunTurnStart(Side side)
    {
        OnTurnStarting?.Invoke(side);
        TurnStartEffects.Apply(side, Resolver);
        OnTurnStarted?.Invoke(side);
    }

    public void EnterTargetSelect(int attackerIdx, PendingActionKind kind)
    {
        if (CurrentSide != Player) return;
        if (State != BattleState.AwaitCardSelect && State != BattleState.AwaitActionSelect) return;
        if (attackerIdx < 0 || attackerIdx >= Player.field.Length) return;
        var card = Player.field[attackerIdx];
        if (card == null || card.IsDead) return;

        PendingAttackerIdx = attackerIdx;
        PendingActionKind = kind;
        SetState(BattleState.AwaitTargetSelect);
    }

    public void CancelTargetSelect()
    {
        if (State != BattleState.AwaitTargetSelect) return;
        PendingAttackerIdx = -1;
        PendingActionKind = PendingActionKind.Attack;
        SetState(BattleState.AwaitCardSelect);
    }

    /// <summary>플레이어 액션 실행. 공격이면 lunge 애니메이션 후 데미지 → 복귀. 스킬은 애니 없이 즉시.</summary>
    public void ExecutePlayerAction(int targetIdx)
    {
        if (State != BattleState.AwaitTargetSelect) return;
        if (PendingAttackerIdx < 0) return;
        StartCoroutine(ResolvePlayerActionCoroutine(targetIdx));
    }

    private IEnumerator ResolvePlayerActionCoroutine(int targetIdx)
    {
        var attackerIdx = PendingAttackerIdx;
        var kind = PendingActionKind;
        var attacker = Player.field[attackerIdx];
        if (attacker == null || attacker.IsDead)
        {
            PendingAttackerIdx = -1;
            SetState(BattleState.AwaitCardSelect);
            yield break;
        }

        var ctx = BuildContext(Player, attackerIdx);
        HashSet<int> valid = kind == PendingActionKind.Attack
            ? new HashSet<int>(attacker.Attack.GetValidTargets(ctx))
            : new HashSet<int>(attacker.Skill.GetValidTargets(ctx));
        if (!valid.Contains(targetIdx))
        {
            Debug.LogWarning($"[Battle] target {targetIdx} not valid for attacker slot {attackerIdx} ({kind})");
            yield break;
        }

        SetState(BattleState.ResolveAction);

        if (kind == PendingActionKind.Attack)
        {
            if (sceneUI != null) yield return sceneUI.PlayAttackLunge(Player, attackerIdx, Opponent, targetIdx);
            attacker.Attack.Execute(ctx, targetIdx);
            if (sceneUI != null) yield return sceneUI.PlayReturnToSlot(Player, attackerIdx);
        }
        else
        {
            attacker.Skill.Execute(ctx, targetIdx);
            attacker.SkillUsed = true;
        }
        PendingAttackerIdx = -1;

        // 복귀 후(또는 스킬 실행 후) 누적된 사망/스폰 시각 처리 — 카드 교체 애니메이션.
        if (sceneUI != null) yield return sceneUI.ProcessPendingSpawns();

        SetState(BattleState.PlayerTurnEnd);
        EndCurrentTurn();
    }

    /// <summary>대상 없는 액티브 스킬(도발/일제사격) 즉시 발동. lunge 애니는 없지만 스폰 큐는 정리해야 하므로 코루틴.</summary>
    public void ExecutePlayerSkillImmediate(int attackerIdx)
    {
        if (CurrentSide != Player) return;
        if (State != BattleState.AwaitCardSelect && State != BattleState.AwaitActionSelect) return;
        if (attackerIdx < 0 || attackerIdx >= Player.field.Length) return;

        var attacker = Player.field[attackerIdx];
        if (attacker == null || attacker.IsDead) return;
        var skill = attacker.Skill;
        if (skill == null || !skill.IsActive) return;
        if (skill.TargetMode != SkillTargetMode.None) return;
        if (attacker.SkillUsed) return;

        StartCoroutine(ResolvePlayerSkillImmediateCoroutine(attackerIdx));
    }

    private IEnumerator ResolvePlayerSkillImmediateCoroutine(int attackerIdx)
    {
        var attacker = Player.field[attackerIdx];
        if (attacker == null || attacker.IsDead) yield break;

        SetState(BattleState.ResolveAction);
        var ctx = BuildContext(Player, attackerIdx);
        attacker.Skill.Execute(ctx, -1);
        attacker.SkillUsed = true;

        if (sceneUI != null) yield return sceneUI.ProcessPendingSpawns();

        SetState(BattleState.PlayerTurnEnd);
        EndCurrentTurn();
    }

    /// <summary>AI 액션 실행. 플레이어 액션과 대칭 — lunge 애니 후 데미지 → 복귀.</summary>
    public void ExecuteOpponentAction(int attackerIdx, int targetIdx)
    {
        if (State != BattleState.OpponentTurnAction) return;
        if (attackerIdx < 0 || attackerIdx >= Opponent.field.Length) return;
        StartCoroutine(ResolveOpponentActionCoroutine(attackerIdx, targetIdx));
    }

    private IEnumerator ResolveOpponentActionCoroutine(int attackerIdx, int targetIdx)
    {
        var attacker = Opponent.field[attackerIdx];
        if (attacker == null || attacker.IsDead) yield break;

        var ctx = BuildContext(Opponent, attackerIdx);
        var attack = attacker.Attack;
        var valid = new HashSet<int>(attack.GetValidTargets(ctx));
        if (!valid.Contains(targetIdx))
        {
            Debug.LogWarning($"[Battle/AI] target {targetIdx} not valid for opponent slot {attackerIdx}");
            yield break;
        }

        SetState(BattleState.ResolveAction);
        if (sceneUI != null) yield return sceneUI.PlayAttackLunge(Opponent, attackerIdx, Player, targetIdx);
        attack.Execute(ctx, targetIdx);
        if (sceneUI != null) yield return sceneUI.PlayReturnToSlot(Opponent, attackerIdx);
        if (sceneUI != null) yield return sceneUI.ProcessPendingSpawns();

        SetState(BattleState.OpponentTurnEnd);
        EndCurrentTurn();
    }

    public void EndCurrentTurn()
    {
        OnTurnEnded?.Invoke(CurrentSide);
        if (CheckGameEnd()) return;
        SetState(CurrentSide == Player
            ? BattleState.OpponentTurnStart
            : BattleState.PlayerTurnStart);
    }

    private bool CheckGameEnd()
    {
        if (Player.IsDefeated || Opponent.IsDefeated)
        {
            Winner = Opponent.IsDefeated ? Player : Opponent;
            if (Winner == Player && CurrentEnemyPool != null)
                RunState.AddGold(CurrentEnemyPool.goldReward);
            SetState(BattleState.GameEnd);
            OnGameEnded?.Invoke(Winner);
            return true;
        }
        return false;
    }
}
