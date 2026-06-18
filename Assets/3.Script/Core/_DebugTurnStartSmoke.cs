using UnityEngine;

/// <summary>
/// 노션 완료 기준 검증용 스모크: 사제 + 기사(HP 2/4) 셋업에서 TurnStartEffects.Apply 한 번 호출 후
/// 기사 HP=3/4, 사제 HP는 변화 없음을 로그로 확인한다.
/// Healer/Knight SO를 인스펙터에 직접 연결해 실행.
/// </summary>
public class _DebugTurnStartSmoke : MonoBehaviour
{
    public CardDataSO healerData;
    public CardDataSO knightData;

    void Awake()
    {
        if (healerData == null || knightData == null)
        {
            Debug.LogError("[TurnStartSmoke] healerData / knightData를 Inspector에 연결하세요.");
            return;
        }

        // 슬롯 0: 기사, 슬롯 1: 사제(자기 자신은 회복 X)
        var side = new Side(true, new[] { knightData, healerData }, 3);
        var knight = side.field[0];
        var healer = side.field[1];
        knight.TakeDamage(2); // 기사 HP 4 -> 2

        Debug.Log($"[TurnStartSmoke] before: knight={knight.CurrentHP}/{knight.data.baseHP} healer={healer.CurrentHP}/{healer.data.baseHP}");

        TurnStartEffects.Apply(side);

        Debug.Log($"[TurnStartSmoke] after : knight={knight.CurrentHP}/{knight.data.baseHP} healer={healer.CurrentHP}/{healer.data.baseHP}");

        bool knightOk = knight.CurrentHP == 3;
        bool healerOk = healer.CurrentHP == healer.data.baseHP; // 자신은 미회복
        Debug.Log($"[TurnStartSmoke] knightHpIs3={knightOk} healerUnchanged={healerOk}");
    }
}
