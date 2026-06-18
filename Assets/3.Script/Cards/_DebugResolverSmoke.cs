using UnityEngine;

public class _DebugResolverSmoke : MonoBehaviour
{
    public BattleConfigSO config;

    void Awake()
    {
        if (config == null)
        {
            Debug.LogError("[ResolverSmoke] config is null. Inspector에서 BattleConfigSO 더미를 연결하세요.");
            return;
        }

        var side = new Side(true, config.playerStartingCards, config.fieldSlotCount);
        var resolver = new DamageResolver();

        resolver.OnDamageDealt += (s, i, a) =>
            Debug.Log($"[ResolverSmoke] OnDamageDealt slot={i} amount={a} hp={s.field[i]?.CurrentHP}");
        resolver.OnCardDied += (s, i, c) =>
            Debug.Log($"[ResolverSmoke] OnCardDied slot={i} card={c.data.cardName}");
        resolver.OnCardSpawned += (s, i, c) =>
            Debug.Log($"[ResolverSmoke] OnCardSpawned slot={i} card={c.data.cardName} standby={s.standby.Count}");

        int slot = 0;
        var target = side.field[slot];
        int overkill = target.CurrentHP + 10;
        int beforeStandby = side.standby.Count;

        Debug.Log($"[ResolverSmoke] before deal: target={target.data.cardName} hp={target.CurrentHP} standby={beforeStandby}");
        resolver.DealDamage(side, slot, overkill);

        var after = side.field[slot];
        Debug.Log($"[ResolverSmoke] after deal: filled={after != null} card={after?.data.cardName} standby={side.standby.Count}");

        // 이미 죽은(=교체된 자리에 들어온 새 카드 아닌) 슬롯에 추가 데미지: 가드 통과 여부
        resolver.DealDamage(side, slot, 0);   // 0 가드
        resolver.DealDamage(side, slot, -1);  // 음수 가드
    }
}
