using UnityEngine;

/// <summary>
/// Battle 씬 로딩 시 인게임 BGM 재생, DamageResolver 이벤트에 SFX 바인딩.
/// BattleController 가 Init() 에서 Resolver 를 생성하므로, Start 가 BattleController 보다 늦게 돌도록 ExecutionOrder 후순위.
/// </summary>
[DefaultExecutionOrder(100)]
public class BattleSceneAudio : MonoBehaviour
{
    [SerializeField] private BattleController battle;
    [SerializeField] private AudioClip battleBGM;
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private AudioClip healSfx;

    private DamageResolver subscribed;

    private void Start()
    {
        if (AudioManager.Instance != null && battleBGM != null)
            AudioManager.Instance.PlayBGM(battleBGM);

        if (battle == null)
        {
            Debug.LogWarning("[BattleSceneAudio] battle 참조 누락. SFX 비활성.");
            return;
        }
        TrySubscribe();
    }

    private void Update()
    {
        if (subscribed == null) TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (battle == null || battle.Resolver == null) return;
        subscribed = battle.Resolver;
        subscribed.OnDamageDealt += HandleDamage;
        subscribed.OnHealApplied += HandleHeal;
    }

    private void OnDestroy()
    {
        if (subscribed != null)
        {
            subscribed.OnDamageDealt -= HandleDamage;
            subscribed.OnHealApplied -= HandleHeal;
        }
    }

    private void HandleDamage(Side _, int __, int ___)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(hitSfx);
    }

    private void HandleHeal(Side _, int __, int ___)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(healSfx);
    }
}
