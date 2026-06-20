using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PendingActionKind { Attack, Skill }

[DefaultExecutionOrder(-100)]
public class BattleController : MonoBehaviour
{
    [SerializeField] BattleConfigSO config;
    [SerializeField] BattleSceneUI sceneUI; // 애니메이션 오케스트레이션용. null 이어도 게임 로직은 동작.

    public BattleConfigSO Config => config;

    public Side Player { get; private set; }
    public Side Opponent { get; private set; }
    public BattleState State { get; private set; }
    public Side CurrentSide { get; private set; }
    public Side Winner { get; private set; }
    public DamageResolver Resolver { get; private set; }

    public int TurnNumber { get; private set; }
    public int PendingAttackerIdx { get; private set; } = -1;
    public PendingActionKind PendingActionKind { get; private set; } = PendingActionKind.Attack;

    public event Action<BattleState> OnStateChanged;
    public event Action<Side> OnTurnStarting;
    public event Action<Side> OnTurnStarted;
    public event Action<Side> OnTurnEnded;
    public event Action<Side> OnGameEnded;

    private void Start() => Init();

    private void Init()
    {
        State = BattleState.Init;
        Player = new Side(true, config.playerStartingCards, config.fieldSlotCount);
        Opponent = new Side(false, config.opponentStartingCards, config.fieldSlotCount);
        Resolver = new DamageResolver();
        SetState(BattleState.PlayerTurnStart);
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
            SetState(BattleState.GameEnd);
            OnGameEnded?.Invoke(Winner);
            return true;
        }
        return false;
    }
}
