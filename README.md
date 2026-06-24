# BurgerMonster — 턴제 카드 배틀 프로토타입

> 1주 Unity 채용 과제. 4종 카드를 활용한 세로형 모바일 턴제 카드 배틀에, 맵 탐색 / 덱 편성 / 도감 / 세이브를 얹은 미니 로그라이크 구조입니다.

---

## 1. 개요
- **한 줄 소개**: 4종 직업 카드를 골라 적과 턴제로 맞붙고, 노드 맵을 따라 진행하는 모바일 카드 배틀.
- **플랫폼**: Android (ARM64), 세로 모드
- **장르**: 턴제 카드 배틀 + 노드 기반 진행
- **개발 기간**: 1주 (2026-06-18 ~ 2026-06-25)

## 2. 환경
- **Unity 버전**: `6000.3.6f1` (Unity 6)
- **렌더 파이프라인**: URP 2D (`com.unity.render-pipelines.universal` 17.3.0)
- **입력**: 신규 Input System (`com.unity.inputsystem` 1.18.0)
- **주요 외부 패키지**
  - TextMeshPro (Unity 내장)
  - DOTween (Free) — `Assets/Plugins/Demigiant`

## 3. 빌드 & 실행
### 빌드 산출물
- APK: `Build/Task.apk`
- 최소 사양: Android 7.0 (API 24) 이상 권장

### 에디터에서 실행
1. 프로젝트를 Unity Hub에 추가하고 `6000.3.6f1` 로 연다.
2. `Assets/1.Scene/1. TitleScene.unity` 를 연다.
3. **Play** 버튼 — Title → Opening → Map → Battle → Ending 순으로 진행.

### 디버그용 진입
- `Assets/1.Scene/Test.unity` — 단일 전투 테스트 씬
- `Assets/3.Script/SO/MapData/Debug.asset` — 디버그용 맵 데이터

## 4. 구현 기능
### 필수 (과제 명세)
- 턴 진행 / 카드 선택 / 공격·스킬 사용 / HP·반격·회복
- 자동 배치 / 승패 판정 / AI 적
- **4종 카드**
  - 일반 — **Knight** (탱커, `TauntSkill` 도발)
  - 원거리 — **Archer** (원거리 공격, `VolleySkill` 일제 사격)
  - 무쌍 — **Berserker** (높은 공격력, `LastStandSkill` 최후의 발악)
  - 힐러 — **Priest** (회복, `HealSkill` 광역 힐)
- 전투 UI / 카드 선택 / 턴 표시 / HP 바 / 결과 화면

### 추가 구현 (가산점)
- **로그라이크식 진행**: 노드 맵 탐색 (전투/이벤트/업그레이드)
- **덱 편성**: 첫 진입 시 덱 구성 + 이후 덱 상세 보기
- **카드 도감**: 보유 카드 일러스트/스탯/스킬 설명
- **세이브 시스템**: 진행 상황 자동 저장 (`SaveSystem`, `AutoSaveHook`)
- **DOTween 트윈**: 카드 이동/공격 모션/HP 팝업
- **원거리 화살 발사체**, 피격/공격 FX
- **AI 일러스트**: 카드 4종 아트
- 사운드 (BGM/SFX)

## 5. 코드 구조
```
Assets/3.Script/
├── Core/                  # 전투 흐름
│   ├── BattleController.cs    # 전투 FSM, 턴 루프
│   ├── DamageResolver.cs      # 데미지/반격/회복 계산
│   ├── TurnStartEffects.cs    # 턴 시작 이펙트
│   └── Side.cs                # 아군/적군 사이드 정의
├── Cards/                 # 카드 도메인
│   ├── CardInstance.cs        # 런타임 카드 상태
│   ├── ICardAttack.cs         # 공격 전략 인터페이스
│   ├── Attack/                # Normal / Ranged / Mighty / Healer
│   ├── ICardSkill.cs          # 스킬 전략 인터페이스
│   ├── Skill/                 # Taunt / Volley / LastStand / Heal
│   └── SkillFactory.cs
├── AI/                    # 적 AI 전략 (Strategy)
│   ├── IAIStrategy.cs
│   ├── RandomAIStrategy.cs
│   ├── HeuristicAIStrategy.cs
│   └── AIController.cs
├── Run/                   # 런 진행 (메타 게임)
│   ├── RunState.cs            # 현재 런의 상태
│   ├── MapDataSO.cs           # 맵 노드 데이터
│   ├── NodeType.cs
│   ├── SceneNames.cs
│   └── Save/                  # SaveData / SaveSystem / CardDex 등
├── SO/                    # ScriptableObject 데이터
│   ├── CardData/              # 4종 카드 SO
│   ├── EnemyPool/             # 적 풀
│   ├── MapData/               # 맵 노드 시드
│   └── Battle/Mercenary/...   # 전투/덱 설정
├── UI/                    # 화면
│   ├── BattleSceneUI.cs       # 전투 HUD
│   ├── CardView.cs            # 카드 한 장 표시
│   ├── TurnIndicator.cs
│   ├── ResultUI.cs
│   ├── DialogueBox.cs
│   ├── SceneLoader.cs
│   └── OutGame/               # 맵/덱/도감/이벤트 패널
└── Audio/                 # BGM/SFX 매니저
```

### 핵심 패턴
- **Strategy**: `ICardAttack` / `ICardSkill` / `IAIStrategy` — 카드 종류와 AI 행동을 자료로만 갈아끼움.
- **ScriptableObject 데이터 분리**: 카드/맵/적/난이도/덱 모두 SO 에셋. 코드 수정 없이 밸런스 조정.
- **FSM**: `BattleController` 가 전투 상태(턴 시작 → 카드 선택 → 해상 → 결과)를 명시적으로 관리.

## 6. 설계 의도 — 왜 이렇게 짰나

### 6.1 UI / 로직 / 데이터 3계층 분리
`Assets/3.Script` 는 `UI/` · `Core·Cards·AI·Run` · `SO` 세 묶음으로 나뉜다. 로직은 데이터(SO)에만 의존하고 UI 를 모름, UI 는 로직의 이벤트(`OnStateChanged` 등)를 구독만 한다. → 헤드리스로 로직 단독 테스트 가능, 평가자가 폴더만 봐도 책임이 보임.

### 6.2 Strategy 패턴 — `if (cardType == ...)` 분기를 코드에서 박멸
카드 종류는 4개로 시작했지만 *언제든 늘어나는 자료*라고 봤다. `BattleController` 안에 카드 종류별 if-else 를 두면 **카드 추가 = `BattleController` 수정**이 되어 코드가 썩는다. 그래서 행동을 인터페이스 뒤로 숨겼다:
- `ICardAttack` — 일반 공격 (유효 타겟 + `Execute`)
- `ICardSkill` — 카드별 1회성 스킬 (`IsActive` 로 액티브/패시브, `SkillTargetMode` 로 타겟팅)
- `IAIStrategy` — 상대 턴 결정

신규 카드 = 새 `ICardAttack` + `ICardSkill` 구현체 + SO 에셋 1개. 전투 흐름 코드는 손대지 않는다.

### 6.3 공격과 스킬을 분리한 이유
"모든 카드가 가지는 일반 공격"과 "카드별 1회성 능력"은 발동 조건·UI·쿨다운이 다르다. 한 인터페이스로 묶으면 `bool isSkill` 같은 플래그가 생기고 호출부가 분기 폭탄이 된다. → 두 인터페이스로 쪼개니 각각 책임이 한 줄로 설명됨.

### 6.4 AI 는 무상태(stateless)
`IAIStrategy.Decide(BattleController)` — 구현체는 현재 스냅샷만 보고 (공격자, 타겟) 페어를 반환. 내부 상태가 없어서 `Random` ↔ `Heuristic` 을 런타임에 교체 가능, 단위 테스트도 쉬움.

### 6.5 슬롯은 비어 있을 수 있고, 당겨오지 않는다
보드 행은 `List<CardInstance>` 가 아니라 **고정 크기 슬롯 배열 + null 허용**. 카드가 죽거나 1회성 효과로 사라져도 인접 카드가 자리를 메우지 않는다. "사라진 자리 = 인접 단절"이 이 게임 전략의 핵심이라 자료구조 단계에서 못박았다.

### 6.6 전투 중에도 끊기지 않는 세이브
`BattleSnapshot` 한 객체로 진영 슬롯 + 턴 번호 + 진행 상태를 떠서 저장한다. `BattleController.PendingRestoreSnapshot` 가 설정되어 있으면 `Init` 이 일반 빌드 경로 대신 복원 경로를 탄다. → 전투 중간에 앱을 꺼도 정확히 그 턴에서 재개.

### 6.7 이벤트로 UI 분리
`BattleController` 는 `OnStateChanged` · `OnTurnStarted` · `OnGameEnded` 만 broadcast. `BattleSceneUI` 는 이걸 구독해 트윈을 재생할 뿐, 로직은 UI 의 애니메이션 완료를 기다리지 않는다 (씬 UI 가 `null` 이어도 게임은 굴러간다는 주석이 `BattleController.cs:13` 에 박혀 있다). → 평가용 자동 테스트 가능, 트윈 길이 바꿔도 로직 영향 없음.

## 7. 씬 구성
| 씬 | 역할 |
|---|---|
| `1. TitleScene` | 타이틀, 새 게임 / 이어하기 |
| `1.5 OpeningScene` | 인트로 컷씬 |
| `2. MapScene` | 노드 맵 탐색 + 덱/도감 패널 |
| `3. BattleScene` | 전투 |
| `4. EndingScene` | 결과/엔딩 |
| `Test.unity` | 개발 테스트용 |

## 8. AI 도구 활용
- **코드**: Claude Code — 페어 프로그래밍 (전투 FSM 구조 잡기, Strategy 인터페이스 리팩터, UI 패널 구현)
- **카드 일러스트**: (TODO — 도구명/프롬프트 요지)
- **기획/태스크 관리**: Notion + Claude — 7일 일정 분해
- **사운드**: (TODO — 사용했다면 출처)

## 9. 플레이 영상
- YouTube: (TODO — Day 7 업로드 후 링크 삽입)

## 10. 알려진 한계
- 1주 프로토타입이라 밸런스 정밀 튜닝 없이 기본 수치 위주.
- 사운드 일부 자리표시(placeholder)일 수 있음.
- 세이브는 단일 슬롯, 암호화 없음.
- (Day 7 폴리시 결과에 따라 추가/수정)

## 11. 라이선스 / 자원 출처
- 사용 외부 자원: TextMeshPro, DOTween (Free), Unity 표준 패키지
- 카드 일러스트: AI 생성 (위 6번 참조)
- 폰트: Maplestory Light (Assets/TextMesh Pro/Fonts)
