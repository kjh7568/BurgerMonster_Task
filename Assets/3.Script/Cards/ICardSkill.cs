using System.Collections.Generic;

/// <summary>
/// 액티브 스킬 시 대상 진영 표시.
/// None: 대상 없음(자기 시전 또는 AoE).
/// Enemy: 적 field 1슬롯 선택.
/// Ally: 아군 field 1슬롯 선택(자기 포함 가능).
/// </summary>
public enum SkillTargetMode { None, Enemy, Ally }

/// <summary>
/// 카드 생존 중 한 번만 사용 가능한 별도 능력.
/// IsActive=true: 플레이어 버튼/UI로 발동.
/// IsActive=false: 패시브, 게임 이벤트(데미지·턴 시작 등)로 자동 트리거. Execute 호출되지 않을 수 있음.
/// </summary>
public interface ICardSkill
{
    bool IsActive { get; }
    SkillTargetMode TargetMode { get; }
    IEnumerable<int> GetValidTargets(BattleContext ctx);
    void Execute(BattleContext ctx, int targetIndex);
}
