using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 무작위로 자기 카드 1장을 골라 그 스킬의 valid target 중 무작위 1장을 친다.
/// 휴리스틱 AI의 기준선이자 폴백.
/// </summary>
public class RandomAIStrategy : IAIStrategy
{
    public (int attacker, int target)? Decide(BattleController battle)
    {
        if (battle == null) return null;

        var attacker = battle.Opponent;
        var defender = battle.Player;
        if (attacker == null || defender == null) return null;

        var aliveAttackers = attacker.AliveIndices().ToList();
        if (aliveAttackers.Count == 0) return null;

        int atkIdx = aliveAttackers[Random.Range(0, aliveAttackers.Count)];
        var atkCard = attacker.field[atkIdx];
        var attack = atkCard.Attack;

        var ctx = new BattleContext
        {
            attackerSide = attacker,
            attackerIndex = atkIdx,
            defenderSide = defender,
            resolver = battle.Resolver,
        };

        var targets = new List<int>(attack.GetValidTargets(ctx));
        if (targets.Count == 0) return null;

        int tgtIdx = targets[Random.Range(0, targets.Count)];
        return (atkIdx, tgtIdx);
    }
}
