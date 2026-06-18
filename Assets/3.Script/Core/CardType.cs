public enum CardType
{
    Normal,   // 일반: HP만큼 피해 + 반격
    Ranged,   // 원거리: HP만큼 피해, 반격 없음
    Mighty,   // 무쌍: HP 100% + 인접 1장 50%
    Healer    // 힐러: 턴 시작 시 아군 +1, 공격은 일반과 동일
}
