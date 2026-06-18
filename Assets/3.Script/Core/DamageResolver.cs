// 최소 스텁. 본 구현은 "DamageResolver (피해/반격/제거/자동 배치)" 작업에서 진행.
public class DamageResolver
{
    public void DealDamage(Side side, int index, int amount)
    {
        var card = side.field[index];
        if (card == null) return;
        card.TakeDamage(amount);
        if (card.IsDead) side.field[index] = null;
    }
}
