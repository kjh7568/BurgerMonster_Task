/// <summary>
/// AI가 한 번의 상대 턴에 어떤 자기 카드로 어떤 적 카드를 공격할지 결정하는 전략 인터페이스.
/// 구현체는 무상태(stateless) 권장 — 결정은 BattleController의 현재 스냅샷으로만 내림.
/// </summary>
public interface IAIStrategy
{
    /// <summary>
    /// 현재 상태에서 실행할 (Opponent의 attacker 슬롯, Player의 target 슬롯) 페어를 결정.
    /// 살아있는 공격자가 없거나 valid target이 없으면 null.
    /// </summary>
    (int attacker, int target)? Decide(BattleController battle);
}
