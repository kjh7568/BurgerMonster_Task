using System.Collections.Generic;
using System.Linq;

public class Side
{
    public readonly bool isPlayer;
    public readonly CardInstance[] field;
    public readonly CardInstance[] standby;

    /// <summary>
    /// 한 진영의 시작 상태를 만든다. startingCards의 앞 fieldSize장은 field에, 다음 fieldSize장은 standby의 같은 인덱스 슬롯에 들어간다. BattleController가 전투 시작 시 양 진영에 대해 호출.
    /// </summary>
    /// <param name="isPlayer">true면 플레이어 진영, false면 적 진영.</param>
    /// <param name="startingCards">초기 카드 SO 목록(보통 6장). 앞 fieldSize장이 전장, 다음 fieldSize장이 대기 슬롯.</param>
    /// <param name="fieldSize">전장 슬롯 개수(보통 3). standby 슬롯도 동일 크기로 만든다.</param>
    public Side(bool isPlayer, IReadOnlyList<CardDataSO> startingCards, int fieldSize)
    {
        this.isPlayer = isPlayer;
        field = new CardInstance[fieldSize];
        standby = new CardInstance[fieldSize];

        for (int i = 0; i < startingCards.Count; i++)
        {
            var inst = new CardInstance(startingCards[i]);
            if (i < fieldSize) field[i] = inst;
            else if (i < fieldSize * 2) standby[i - fieldSize] = inst;
        }
    }

    public int StandbyCount => standby.Count(c => c != null);

    public bool IsDefeated => field.All(c => c == null) && StandbyCount == 0;

    /// <summary>
    /// standby 슬롯에서 비어있지 않은 인덱스 중 하나를 무작위로 골라 카드를 꺼내고, 해당 standby 슬롯을 비운다. 카드 사망 시 자동 배치를 위해 DamageResolver가 호출. UI는 반환된 fromStandbyIdx를 받아 뒤집기·이동 연출 타깃으로 사용.
    /// </summary>
    /// <returns>(꺼낸 카드, 비워진 standby 슬롯 인덱스). 대기 카드가 없으면 (null, -1).</returns>
    public (CardInstance card, int fromStandbyIdx) PopRandomStandby()
    {
        var aliveIdx = new List<int>();
        for (int i = 0; i < standby.Length; i++)
            if (standby[i] != null) aliveIdx.Add(i);
        if (aliveIdx.Count == 0) return (null, -1);

        int pick = aliveIdx[UnityEngine.Random.Range(0, aliveIdx.Count)];
        var card = standby[pick];
        standby[pick] = null;
        return (card, pick);
    }

    /// <summary>
    /// 전장에서 살아있는(=null 아니고 IsDead=false) 카드의 슬롯 인덱스를 lazy 시퀀스로 돌려준다. 공격 대상 선택, 인접 카드 탐색 등에서 사용. 호출자가 .Any()/.First()/.Count() 등으로 필요한 만큼만 평가하면 된다.
    /// </summary>
    /// <returns>살아있는 카드의 슬롯 인덱스 시퀀스(IEnumerable, lazy).</returns>
    public IEnumerable<int> AliveIndices() =>
        Enumerable.Range(0, field.Length).Where(i => field[i] != null && !field[i].IsDead);
}
