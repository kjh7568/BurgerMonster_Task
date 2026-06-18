using System;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    [SerializeField] BattleConfigSO config;

    public Side Player { get; private set; }
    public Side Opponent { get; private set; }
    public BattleState State { get; private set; }
    public Side CurrentSide { get; private set; }
    public Side Winner { get; private set; }
    public DamageResolver Resolver { get; private set; }

    public event Action<BattleState> OnStateChanged;
    public event Action<Side> OnTurnStarting;
    public event Action<Side> OnTurnStarted;
    public event Action<Side> OnTurnEnded;
    public event Action<Side> OnGameEnded;

    /// <summary>
    /// Unity 라이프사이클 진입점. 씬 로드 후 자동 호출되어 Init으로 전투 초기화를 위임.
    /// </summary>
    private void Start() => Init();

    /// <summary>
    /// 양 진영 Side를 생성하고 상태를 PlayerTurnStart로 진입시켜 전투를 시작한다. Start에서 1회 호출.
    /// </summary>
    private void Init()
    {
        State = BattleState.Init;
        Player = new Side(true, config.playerStartingCards, config.fieldSlotCount);
        Opponent = new Side(false, config.opponentStartingCards, config.fieldSlotCount);
        Resolver = new DamageResolver();
        SetState(BattleState.PlayerTurnStart);
    }

    /// <summary>
    /// 스킬 실행에 필요한 BattleContext를 조립한다. 공격 진영을 기준으로 방어 진영을 자동 결정하고 공용 Resolver를 주입. 카드 선택/AI 행동 시 ICardSkill 호출 직전에 사용.
    /// </summary>
    /// <param name="attacker">공격 측 진영. Player 또는 Opponent 중 하나여야 한다.</param>
    /// <param name="attackerIdx">공격 카드의 field 슬롯 인덱스(0~2).</param>
    /// <returns>내용이 채워진 BattleContext. ICardSkill.GetValidTargets·Execute에 그대로 전달.</returns>
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

    /// <summary>
    /// FSM 상태를 전이시키고 OnStateChanged 이벤트를 발행한다. 턴 시작 상태일 경우 CurrentSide를 갱신하고 후속 상태로 자동 연쇄 전이. 내부 전용.
    /// </summary>
    /// <param name="s">전이할 다음 BattleState.</param>
    private void SetState(BattleState s)
    {
        State = s;
        Debug.Log($"[State] {s}");
        OnStateChanged?.Invoke(s);

        switch (s)
        {
            case BattleState.PlayerTurnStart:
                CurrentSide = Player;
                RunTurnStart(CurrentSide);
                SetState(BattleState.AwaitCardSelect);
                break;
            case BattleState.OpponentTurnStart:
                CurrentSide = Opponent;
                RunTurnStart(CurrentSide);
                SetState(BattleState.OpponentTurnAction);
                break;
        }
    }

    /// <summary>
    /// 턴 시작 시퀀스. OnTurnStarting 발행 → TurnStartEffects.Apply(힐러 회복 등) → OnTurnStarted 발행.
    /// UI는 OnTurnStarting 직후 뷰를 갱신하면 회복 결과를 반영한 상태에서 OnTurnStarted를 받게 된다.
    /// </summary>
    private void RunTurnStart(Side side)
    {
        OnTurnStarting?.Invoke(side);
        TurnStartEffects.Apply(side);
        OnTurnStarted?.Invoke(side);
    }

    /// <summary>
    /// 현재 턴을 종료하고 상대 진영의 턴 시작 상태로 전이한다. 게임 종료 조건을 먼저 검사하므로 종료 시 전이는 일어나지 않는다. UI의 턴 종료 버튼/AI 행동 완료 시점에 호출.
    /// </summary>
    public void EndCurrentTurn()
    {
        OnTurnEnded?.Invoke(CurrentSide);
        if (CheckGameEnd()) return;
        SetState(CurrentSide == Player
            ? BattleState.OpponentTurnStart
            : BattleState.PlayerTurnStart);
    }

    /// <summary>
    /// 양 진영의 패배 조건을 확인하고 한 쪽이 패배했으면 Winner를 정한 뒤 GameEnd 상태로 전이, OnGameEnded를 발행한다. 매 턴 종료 시점에 호출.
    /// </summary>
    /// <returns>게임이 종료되었으면 true, 아직 진행 중이면 false.</returns>
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
