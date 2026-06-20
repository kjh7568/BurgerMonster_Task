using System.Collections.Generic;

/// <summary>
/// 점수 기반 AI. 모든 (attacker, target) 페어를 평가해 최고점 1개를 선택. 동점은 첫 매치(결정론적).
/// 점수 규칙(우선순위 명세 그대로):
/// 1) 처치 가능(공격자 HP ≥ 방어자 HP)이면 +10
/// 2) 힐러 타격 보너스 +5
/// 3) 반격에 자신 사망 시 페널티 −4 (단, 공격자가 원거리면 면제 — 반격을 안 받음)
/// 4) 원거리 딜러를 적극 처치 +3 (장기적 위협 제거)
/// 5) 약체 타겟 우선 +(5 − 방어자 현재 HP)
/// </summary>
public class HeuristicAIStrategy : IAIStrategy
{
    public (int attacker, int target)? Decide(BattleController battle)
    {
        if (battle == null) return null;

        var atkSide = battle.Opponent;
        var defSide = battle.Player;
        if (atkSide == null || defSide == null) return null;

        int bestScore = int.MinValue;
        (int atk, int tgt)? best = null;

        foreach (int atkIdx in atkSide.AliveIndices())
        {
            var atkCard = atkSide.field[atkIdx];
            var attack = atkCard.Attack;
            var ctx = new BattleContext
            {
                attackerSide = atkSide,
                attackerIndex = atkIdx,
                defenderSide = defSide,
                resolver = battle.Resolver,
            };

            foreach (int tgtIdx in attack.GetValidTargets(ctx))
            {
                var defCard = defSide.field[tgtIdx];
                if (defCard == null) continue;

                int score = ScoreAction(atkCard, defCard, atkCard.data.type);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = (atkIdx, tgtIdx);
                }
            }
        }

        return best;
    }

    private static int ScoreAction(CardInstance atk, CardInstance def, CardType atkType)
    {
        int score = 0;

        // 1) 처치 가능
        if (atk.CurrentHP >= def.CurrentHP) score += 10;

        // 2) 힐러 타격 보너스
        if (def.data.type == CardType.Healer) score += 5;

        // 3) 반격에 자신 사망 시 페널티 (원거리 면제)
        if (atkType != CardType.Ranged && def.CurrentHP >= atk.CurrentHP) score -= 4;

        // 4) 원거리 딜러 우선 처치
        if (def.data.type == CardType.Ranged) score += 3;

        // 5) 약체 타겟 우선
        score += (5 - def.CurrentHP);

        return score;
    }
}
