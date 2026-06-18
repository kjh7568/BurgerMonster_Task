public enum BattleState
{
    Init,
    PlayerTurnStart,
    AwaitCardSelect,
    AwaitActionSelect,
    AwaitTargetSelect,
    ResolveAction,
    PlayerTurnEnd,
    OpponentTurnStart,
    OpponentTurnAction,
    OpponentTurnEnd,
    GameEnd
}
