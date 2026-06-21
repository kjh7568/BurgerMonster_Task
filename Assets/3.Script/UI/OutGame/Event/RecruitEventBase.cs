using System;
using UnityEngine;

/// <summary>
/// Recruit 노드의 단일 이벤트 베이스. UpgradeEventBase 와 동일한 패턴 — 후보 배열에서 CanRun()==true 인 것 중 1개를 골라 Show 호출.
/// 현재는 PaidRecruitEvent 하나만 구현. 무료 영입은 추후.
/// </summary>
public abstract class RecruitEventBase : MonoBehaviour
{
    /// <summary>현재 RunState 상태에서 의미 있게 실행 가능한지. 풀 비어있거나 덱 0장이면 false.</summary>
    public abstract bool CanRun();

    /// <summary>패널을 띄우고 사용자 결정 후 onConfirmed 콜백. 호출자가 노드 진행 처리.</summary>
    public abstract void Show(Action onConfirmed);
}
