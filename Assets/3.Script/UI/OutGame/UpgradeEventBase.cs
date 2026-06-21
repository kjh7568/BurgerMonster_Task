using System;
using UnityEngine;

/// <summary>
/// Upgrade 노드의 단일 이벤트 베이스. Slay the Spire 식으로 3종 이벤트 중 1개가 랜덤 선택되어 실행된다.
/// MapPanelController 가 후보 배열에서 <see cref="CanRun"/>==true 인 것 중 1개를 골라 <see cref="Show"/> 호출.
/// </summary>
public abstract class UpgradeEventBase : MonoBehaviour
{
    /// <summary>이 이벤트가 현재 RunState 상태에서 의미 있게 실행 가능한지. 예: 스킬 강화는 수치형 스킬 카드가 있어야.</summary>
    public abstract bool CanRun();

    /// <summary>패널을 띄우고 사용자 결정 후 onConfirmed 콜백 호출. 호출자는 그 시점에 노드 진행 처리.</summary>
    public abstract void Show(Action onConfirmed);
}
