using System;
using System.Collections.Generic;
using UnityEngine;

public enum PendingActionKind { Attack, Skill }

[DefaultExecutionOrder(-100)]
public class BattleController : MonoBehaviour
{
    [SerializeField] BattleConfigSO config;

    public BattleConfigSO Config => config;

    public Side Player { get; private set; }
    public Side Opponent { get; private set; }
    public BattleState State { get; private set; }
    public Side CurrentSide { get; private set; }
    public Side Winner { get; private set; }
    public DamageResolver Resolver { get; private set; }

    /// <summary>현재 턴 번호. 양 진영 각자의 TurnStart 진입 시 1씩 증가.</summary>
    public int TurnNumber { get; private set; }

    /// <summary>AwaitTargetSelect 동안 임시 저장된 공격자 슬롯. 그 외 상태에선 -1.</summary>
    public int PendingAttackerIdx { get; private set; } = -1;

    /// <summary>AwaitTargetSelect로 진입한 행동 종류. Attack 또는 Skill.</summary>
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
        TurnStartEffects.Apply(side);
        OnTurnStarted?.Invoke(side);
    }

    /// <summary>
    /// 플레이어 행동(공격/스킬)으로 대상 선택 단계 진입. kind에 따라 valid 후보 진영이 결정됨(UI 책임).
    /// CurrentSide==Player + State 가 AwaitCardSelect/AwaitActionSelect + 공격자 살아있어야 수락.
    /// Skill kind 진입 시 SkillUsed/IsActive 가드는 호출자(UI)가 책임.
    /// </summary>
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

    /// <summary>
    /// AwaitTargetSelect에서 대상 클릭 시 호출. PendingActionKind에 따라 Attack 또는 Skill 분기.
    /// Skill 실행 후 attacker.SkillUsed=true.
    /// </summary>
    public void ExecutePlayerAction(int targetIdx)
    {
        if (State != BattleState.AwaitTargetSelect) return;
        if (PendingAttackerIdx < 0) return;

        var attacker = Player.field[PendingAttackerIdx];
        if (attacker == null || attacker.IsDead)
        {
            PendingAttackerIdx = -1;
            SetState(BattleState.AwaitCardSelect);
            return;
        }

        var ctx = BuildContext(Player, PendingAttackerIdx);
        HashSet<int> valid;
        if (PendingActionKind == PendingActionKind.Attack)
            valid = new HashSet<int>(attacker.Attack.GetValidTargets(ctx));
        else
            valid = new HashSet<int>(attacker.Skill.GetValidTargets(ctx));

        if (!valid.Contains(targetIdx))
        {
            Debug.LogWarning($"[Battle] target {targetIdx} not valid for attacker slot {PendingAttackerIdx} ({PendingActionKind})");
            return;
        }

        SetState(BattleState.ResolveAction);
        if (PendingActionKind == PendingActionKind.Attack)
        {
            attacker.Attack.Execute(ctx, targetIdx);
        }
        else
        {
            attacker.Skill.Execute(ctx, targetIdx);
            attacker.SkillUsed = true;
        }
        PendingAttackerIdx = -1;

        SetState(BattleState.PlayerTurnEnd);
        EndCurrentTurn();
    }

    /// <summary>
    /// 대상 없는 액티브 스킬(TargetMode=None) 즉시 발동 — 카드 선택 단계에서 바로 호출.
    /// EnterTargetSelect 우회 경로. 도발/일제사격용.
    /// </summary>
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

        SetState(BattleState.ResolveAction);
        var ctx = BuildContext(Player, attackerIdx);
        skill.Execute(ctx, -1);
        attacker.SkillUsed = true;

        SetState(BattleState.PlayerTurnEnd);
        EndCurrentTurn();
    }

    /// <summary>
    /// AI가 결정한 (attacker, target) 페어를 일반 공격으로 실행. AI는 스킬을 사용하지 않음(별도 작업).
    /// </summary>
    public void ExecuteOpponentAction(int attackerIdx, int targetIdx)
    {
        if (State != BattleState.OpponentTurnAction) return;
        if (attackerIdx < 0 || attackerIdx >= Opponent.field.Length) return;

        var attacker = Opponent.field[attackerIdx];
        if (attacker == null || attacker.IsDead) return;

        var ctx = BuildContext(Opponent, attackerIdx);
        var attack = attacker.Attack;
        var valid = new HashSet<int>(attack.GetValidTargets(ctx));
        if (!valid.Contains(targetIdx))
        {
            Debug.LogWarning($"[Battle/AI] target {targetIdx} not valid for opponent slot {attackerIdx}");
            return;
        }

        SetState(BattleState.ResolveAction);
        attack.Execute(ctx, targetIdx);

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
