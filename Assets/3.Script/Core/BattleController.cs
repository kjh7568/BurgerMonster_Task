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

    public event Action<BattleState> OnStateChanged;
    public event Action<Side> OnTurnStarted;
    public event Action<Side> OnGameEnded;

    void Start() => Init();

    void Init()
    {
        State = BattleState.Init;
        Player = new Side(true, config.playerStartingCards, config.fieldSlotCount);
        Opponent = new Side(false, config.opponentStartingCards, config.fieldSlotCount);
        SetState(BattleState.PlayerTurnStart);
    }

    void SetState(BattleState s)
    {
        State = s;
        Debug.Log($"[State] {s}");
        OnStateChanged?.Invoke(s);

        switch (s)
        {
            case BattleState.PlayerTurnStart:
                CurrentSide = Player;
                OnTurnStarted?.Invoke(CurrentSide);
                SetState(BattleState.AwaitCardSelect);
                break;
            case BattleState.OpponentTurnStart:
                CurrentSide = Opponent;
                OnTurnStarted?.Invoke(CurrentSide);
                SetState(BattleState.OpponentTurnAction);
                break;
        }
    }

    public void EndCurrentTurn()
    {
        if (CheckGameEnd()) return;
        SetState(CurrentSide == Player
            ? BattleState.OpponentTurnStart
            : BattleState.PlayerTurnStart);
    }

    bool CheckGameEnd()
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
