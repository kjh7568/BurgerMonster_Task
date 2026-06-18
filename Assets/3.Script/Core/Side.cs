using System.Collections.Generic;
using System.Linq;

public class Side
{
    public readonly bool isPlayer;
    public readonly CardInstance[] field;
    public readonly Queue<CardInstance> standby;

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

    public IEnumerable<int> AliveIndices() =>
        Enumerable.Range(0, field.Length).Where(i => field[i] != null && !field[i].IsDead);
}
