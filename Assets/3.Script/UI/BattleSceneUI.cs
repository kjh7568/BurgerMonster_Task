using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BattleScene의 카드 슬롯 6개(플레이어 3 + 적 3)에 CardView 프리팹을 인스턴스화하고
/// BattleController의 Side.field 데이터로 초기 Bind 한다.
/// 슬롯 컨테이너(RectTransform)의 레이아웃은 유지한 채 그 자식으로 풀-스트레치 배치.
/// HP 갱신·사망 처리 같은 이벤트 구독은 별도 컨트롤러 책임.
/// </summary>
public class BattleSceneUI : MonoBehaviour
{
    [SerializeField] private BattleController battle;
    [SerializeField] private GameObject cardViewPrefab;
    [SerializeField] private RectTransform[] playerSlots;
    [SerializeField] private RectTransform[] opponentSlots;

    private CardView[] playerViews;
    private CardView[] opponentViews;

    public IReadOnlyList<CardView> PlayerViews => playerViews;
    public IReadOnlyList<CardView> OpponentViews => opponentViews;

    private void Start()
    {
        playerViews = SpawnSide(battle.Player, playerSlots);
        opponentViews = SpawnSide(battle.Opponent, opponentSlots);
    }

    private CardView[] SpawnSide(Side side, RectTransform[] slots)
    {
        var views = new CardView[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            var go = Instantiate(cardViewPrefab, slots[i]);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            var view = go.GetComponent<CardView>();
            var card = i < side.field.Length ? side.field[i] : null;
            view.Bind(side, i, card);
            views[i] = view;
        }
        return views;
    }
}
