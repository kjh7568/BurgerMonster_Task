using System.Collections.Generic;
using System.Linq;

public class Side
{
    public readonly bool isPlayer;
    public readonly CardInstance[] field;
    public readonly Queue<CardInstance> standby;

    /// <summary>
    /// 한 진영의 시작 상태를 만든다. startingCards의 앞 fieldSize장은 field에, 나머지는 standby 큐에 들어간다. BattleController가 전투 시작 시 양 진영에 대해 호출.
    /// </summary>
    /// <param name="isPlayer">true면 플레이어 진영, false면 적 진영.</param>
    /// <param name="startingCards">초기 카드 SO 목록(보통 6장). 인덱스 순서가 배치 순서.</param>
    /// <param name="fieldSize">전장 슬롯 개수(보통 3). 나머지 카드는 대기로 간다.</param>
    public Side(bool isPlayer, IReadOnlyList<CardDataSO> startingCards, int fieldSize)
    {
        this.isPlayer = isPlayer;
        field = new CardInstance[fieldSize];
        standby = new Queue<CardInstance>();

        for (int i = 0; i < startingCards.Count; i++)
        {
            var inst = new CardInstance(startingCards[i]);
            if (i < fieldSize) field[i] = inst;
            else standby.Enqueue(inst);
        }
    }

    public bool IsDefeated => field.All(c => c == null) && standby.Count == 0;

    /// <summary>
    /// 전장의 빈 슬롯을 앞쪽(0→1→2 순)부터 대기 카드로 채운다. 카드 사망 후 DamageResolver가 호출. 사망 슬롯과 무관하게 가장 앞쪽 빈 슬롯이 먼저 채워진다.
    /// </summary>
    /// <returns>새로 채워진 슬롯 인덱스 목록. UI 등장 애니메이션의 타깃으로 사용.</returns>
    public List<int> RefillField()
    {
        var refilled = new List<int>();
        for (int i = 0; i < field.Length; i++)
        {
            if (field[i] == null && standby.Count > 0)
            {
                field[i] = standby.Dequeue();
                refilled.Add(i);
            }
        }
        return refilled;
    }

    /// <summary>
    /// 전장에서 살아있는(=null 아니고 IsDead=false) 카드의 슬롯 인덱스를 lazy 시퀀스로 돌려준다. 공격 대상 선택, 인접 카드 탐색 등에서 사용. 호출자가 .Any()/.First()/.Count() 등으로 필요한 만큼만 평가하면 된다.
    /// </summary>
    /// <returns>살아있는 카드의 슬롯 인덱스 시퀀스(IEnumerable, lazy).</returns>
    public IEnumerable<int> AliveIndices() =>
        Enumerable.Range(0, field.Length).Where(i => field[i] != null && !field[i].IsDead);
}
