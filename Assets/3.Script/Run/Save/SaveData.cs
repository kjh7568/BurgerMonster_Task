using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 영속 데이터 루트. JsonUtility 직렬화 대상. settings 만 단독 저장하는 경로도 있지만 한 파일에 같이 둔다.
/// </summary>
[Serializable]
public class SaveData
{
    public const string CurrentVersion = "1.0";

    public string version = CurrentVersion;
    public SettingsState settings = new SettingsState();
    public RunSnapshot run;       // null 이면 진행 중 Run 없음
    public BattleSnapshot battle; // null 이면 전투 중 아님 (= 지도에서 종료됨)
    public string activeScene;    // 이어하기 시 어느 씬으로 갈지 결정. "" 면 Map.
    public bool hasSeenIntro;     // 오프닝 컷씬을 1회라도 본 적 있으면 true. 새 게임 시 false 면 오프닝 재생.
    public bool hasClearedRun;    // Run 을 1회라도 클리어한 적 있으면 true. 첫 클리어에만 엔딩 컷씬 재생.
}

[Serializable]
public class SettingsState
{
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
}

[Serializable]
public class RunSnapshot
{
    public string mapId;
    public int currentNodeIndex;
    public int stage;
    public int gold;
    public int score;
    public int globalHpBonus;
    public List<string> deckCardIds = new List<string>();
    public List<int> perCardHpBonus = new List<int>();
    public List<int> perCardSkillBonus = new List<int>();
}

[Serializable]
public class BattleSnapshot
{
    public string enemyPoolId; // 승리 시 골드 보상 재현용
    public int turnNumber;
    public bool isPlayerTurn;  // CurrentSide 가 Player 인가
    public List<CardInstanceSnapshot> playerField = new List<CardInstanceSnapshot>();
    public List<CardInstanceSnapshot> playerStandby = new List<CardInstanceSnapshot>();
    public List<CardInstanceSnapshot> opponentField = new List<CardInstanceSnapshot>();
    public List<CardInstanceSnapshot> opponentStandby = new List<CardInstanceSnapshot>();
}

[Serializable]
public class CardInstanceSnapshot
{
    public string cardId;       // 비어있으면 슬롯이 빈 칸(null CardInstance)
    public int maxHP;
    public int currentHP;
    public int skillBonus;
    public bool skillUsed;
    public bool isTaunting;
    public bool lastStandUsed;
}
