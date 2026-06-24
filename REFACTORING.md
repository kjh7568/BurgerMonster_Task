# BurgerMonster 리팩토링 계획

> 67개 C# 파일, ~6,000 LOC 전수 점검 결과. 아키텍처/생명주기/성능/품질 4관점 분석.
> 우선순위는 **버그 위험 → 결합도 → 중복 → 매직 넘버** 순.

---

## 0. 한눈에 보는 우선순위

| # | 항목 | 영향 | 난도 | 카테고리 |
|---|------|------|------|----------|
| 1 | OpeningController / EndingController 이벤트 중복 구독 | 🔴 버그 | 🟢 낮음 | 생명주기 |
| 2 | BattleSceneAudio 좀비 DamageResolver 리스너 | 🔴 버그 | 🟢 낮음 | 생명주기 |
| 3 | `RunState` static 전역 상태 → `RunContext` 주입 객체로 | 🔴 결합 | 🔴 높음 | 구조 |
| 4 | `BattleSceneUI` 619줄 / `BattleController` 501줄 분할 | 🟡 결합 | 🔴 높음 | 구조 |
| 5 | Event 패널 4종 / Attack 4종 / Skill 4종 베이스로 통합 | 🟡 중복 | 🟡 중간 | 품질 |
| 6 | SaveBridge 호출 빈도 축소 + 비동기 I/O | 🟡 성능 | 🟡 중간 | 성능 |
| 7 | 매 호출 `new HashSet<int>` / `new List<int>` / `new WaitForSeconds` | 🟢 성능 | 🟢 낮음 | 성능 |
| 8 | 매직 넘버 → SO 추출 (애니메이션, 점수, 색상, 맵 좌표) | 🟢 품질 | 🟢 낮음 | 품질 |
| 9 | `FindObjectOfType` 의존 제거 (`ResultUI`, `AutoSaveHook`) | 🟡 버그 | 🟢 낮음 | 구조 |
| 10 | BattleSceneUI ↔ BattleController 양방향 결합 제거 | 🟡 결합 | 🟡 중간 | 구조 |

---

## 1. 즉시 고쳐야 하는 버그성 결함

### 1-1. OpeningController / EndingController 이벤트 중복 구독
- **위치**: [OpeningController.cs:108](Assets/3.Script/UI/Title/OpeningController.cs#L108), [EndingController.cs:108](Assets/3.Script/UI/Title/EndingController.cs#L108)
- **현상**: `PlayCutRoutine` 이 매 컷마다 호출되는데 `OnAllEnd += OnCutLinesEnded` 만 누적됨. 컷 N개면 마지막 컷 종료 시 콜백이 N회 발화.
- **수정**: 진입 시 `-=` 먼저, 그 다음 `+=`. (현재 코드처럼 `OnDestroy` 한 번만으로는 부족)

### 1-2. BattleSceneAudio 좀비 리스너
- **위치**: [BattleSceneAudio.cs:30-41](Assets/3.Script/Audio/BattleSceneAudio.cs#L30)
- **현상**: `BattleController.Init()` 가 매번 새 `DamageResolver` 를 만든다([BattleController.cs:49](Assets/3.Script/Core/BattleController.cs#L49)). 재도전(같은 씬) 시 이전 Resolver 구독이 안 풀려 SFX 중복.
- **수정**: Resolver 교체 시 `subscribed` 를 해지하고 새것에 다시 붙이는 흐름 명확화. 또는 BattleController 가 `OnResolverReplaced` 같은 이벤트를 노출.

### 1-3. ResultUI 의 위험한 `FindObjectOfType`
- **위치**: [ResultUI.cs:45](Assets/3.Script/UI/ResultUI.cs#L45)
- **현상**: `Awake` 시점에 `FindObjectOfType<BattleController>()` 호출. 실행 순서 의존 + 중복 인스턴스 위험.
- **수정**: 인스펙터 SerializeField 강제 주입 또는 `Start` 로 이동 + null 명시 처리.

### 1-4. AutoSaveHook 의 `FindObjectOfType`
- **위치**: [AutoSaveHook.cs:33](Assets/3.Script/Run/Save/AutoSaveHook.cs#L33)
- **현상**: 동일 씬에 BattleController 여러 개가 들어가면 임의 인스턴스 선택.
- **수정**: 이벤트 기반 등록 (BattleController.OnEnable 시 AutoSaveHook 에 자기 등록).

### 1-5. MapPanelController OnEnable 중복 구독 — 근본 원인 추적 필요
- **위치**: [MapPanelController.cs:85-97](Assets/3.Script/UI/OutGame/MapPanelController.cs#L85)
- **현상**: 주석으로 "중복 구독 방지" 라고 적고 `-= … +=` 패턴을 쓰는데, **왜 OnEnable 이 두 번 호출되는지** 가 진짜 문제. SceneLoader 의 로드 순서 문제일 가능성.
- **조치**: 실 동작 확인 → 원인이 씬 재로드면 그쪽을 고치고, 그게 아니면 방어 코드 자체를 제거.

### 1-6. BattleController.BuildRestoredRow 실패 시 흐름
- **위치**: [BattleController.cs:105](Assets/3.Script/Core/BattleController.cs#L105)
- **현상**: 저장 데이터에서 카드 ID 를 못 찾으면 `Debug.LogError` 후 슬롯에 null 을 남기고 진행 → 이후 전투 로직에서 NRE 폭탄.
- **수정**: 복원 실패 시 명시적 `throw` 또는 "복원 불가" 상태로 전이.

---

## 2. 아키텍처 / 구조 결함

### 2-1. `RunState` 의 static 전역 의존
- **위치**: [RunState.cs](Assets/3.Script/Run/RunState.cs) 전체. 15개 파일이 `RunState.` 직접 참조.
- **문제**:
  - 테스트 불가 (모킹 안 됨)
  - 도메인(BattleController) 과 UI(MapPanelController, DeckLayoutPanel) 가 똑같이 전역에 매달려 있어 책임 경계 흐림
  - SaveBridge 가 `RunState` 와 `SaveData` 양쪽에 동기적으로 쓰는 구조 → 불일치 위험
- **개선안**: `RunState` → `class RunContext` 로 변환, 게임 시작 시 한 번 만들어 의존 객체에 주입. SaveBridge 는 `RunContext` 만 보면 됨.
- **난도**: 높음 — 호출처 광범위. 점진적으로 `RunState.Get*()` 한 군데로 모은 뒤 그것을 인스턴스로 교체하는 식.

### 2-2. BattleController ↔ BattleSceneUI 양방향 결합
- **위치**: [BattleController.cs:367](Assets/3.Script/Core/BattleController.cs#L367) 등 — `if (sceneUI != null) yield return sceneUI.PlayAttackLunge(...)`
- **문제**: 도메인 로직이 UI 존재 여부를 알고 분기. UI 가 있으면 애니메이션 끝까지 대기.
- **개선안**: BattleController 는 "사건"(OnCardAttacked, OnDamaged) 만 발신 → UI 가 사건을 듣고 자기 코루틴으로 애니메이션. Controller 는 시간 진행을 별도 신호로 대기 (Promise/Awaitable 또는 명시적 `ContinueAfter` 이벤트).

### 2-3. CardView 가 게임 규칙을 안다
- **위치**: [CardView.cs:131](Assets/3.Script/UI/CardView.cs#L131) — `if (Bound.IsDead) HideActionPanel()`
- **문제**: View 가 "죽은 카드는 행동 불가" 룰을 알고 UI 처리. 룰이 늘면 매번 View 수정.
- **개선안**: View 는 `actionEnabled` flag 만 받고, 누가 그 값을 결정하는지는 위 계층(Controller)이 정함.

### 2-4. 거대 클래스 분할
- **BattleSceneUI 619줄** ([BattleSceneUI.cs](Assets/3.Script/UI/BattleSceneUI.cs))
  - 책임: CardView 12장 관리 + 입력 라우팅 + 애니메이션 + DamageResolver 이벤트 처리
  - 분리: `CardViewRenderer` / `BattleAnimator` / `BattleInputRouter` 3개로
- **BattleController 501줄** ([BattleController.cs](Assets/3.Script/Core/BattleController.cs))
  - 분리: `CardBuilder` (BuildPlayerCards / BuildOpponentCards) / `BattleStateMachine` / `BattleSnapshotter` (Save/Restore)
- **MapPanelController 317줄** ([MapPanelController.cs](Assets/3.Script/UI/OutGame/MapPanelController.cs))
  - 분리: `MapNodeGenerator` (jitter, 배치) / `MapEventDispatcher` (노드 클릭 분기)
- **DeckLayoutPanel 308줄** ([DeckLayoutPanel.cs](Assets/3.Script/UI/OutGame/DeckLayoutPanel.cs))
  - FirstSetup / DeckView 두 모드가 한 클래스에 — 모드별 상태 객체나 전략 패턴으로

### 2-5. 카드 인스턴스 생성 경로 이원화
- **위치**: [BattleController.cs:158](Assets/3.Script/Core/BattleController.cs#L158) `BuildPlayerCards` vs [BattleController.cs:228](Assets/3.Script/Core/BattleController.cs#L228) `BuildOpponentCards`
- **문제**: 글로벌 HP 보너스 / 스킬 보너스 적용 로직이 두 경로에서 미묘하게 다를 위험. 한쪽만 수정하면 버그.
- **개선안**: `CardInstanceFactory.Create(CardDataSO, BonusContext)` 한 곳으로.

### 2-6. SaveBridge 의 책임 분산
- **위치**: [SaveBridge](Assets/3.Script/Run/Save/SaveBridge.cs)
- **문제**: `RunState.*` 와 `BattleController.*` 양쪽 스냅샷을 직접 읽고 `SaveData` 에 쓰기 — 데이터 모델이 바뀔 때마다 SaveBridge 도 같이 수정해야 함.
- **개선안**: 각 도메인이 `ToSnapshot()` / `RestoreFrom(snapshot)` 을 구현, SaveBridge 는 "언제 저장할지" 만 결정.

### 2-7. 인터페이스 부재로 인한 강결합
- `ICardAttack`, `ICardSkill` 외엔 추상화가 거의 없음.
- AI: `IAIStrategy` 있음 ✓
- UI ↔ Battle, Save ↔ Domain 사이엔 직접 클래스 참조뿐 → 테스트 시 mock 불가.

---

## 3. 이벤트 / 생명주기

### 3-1. AudioManager 크로스페이드 코루틴 핸들 미관리
- **위치**: [AudioManager.cs:71](Assets/3.Script/Audio/AudioManager.cs#L71)
- **현상**: 크로스페이드 중 다시 호출되면 두 코루틴이 동시 진행.
- **수정**: 이전 핸들을 `StopCoroutine` 하고 새로 시작.

### 3-2. DialogueBox 콜백 + 이벤트 이중 발화
- **위치**: [DialogueBox.cs:47-49](Assets/3.Script/UI/DialogueBox.cs#L47)
- **현상**: `Play(lines, onAllEnd)` 콜백과 `OnAllEnd` 이벤트가 모두 발화 → 호출자(OpeningController)가 둘 다 처리.
- **수정**: 콜백 파라미터 제거하고 이벤트만 노출, 또는 그 반대로 통일.

### 3-3. BattleSceneUI 의 동적 CardView 구독
- **위치**: [BattleSceneUI.cs:76-96](Assets/3.Script/UI/BattleSceneUI.cs#L76)
- **현상**: CardView N장 × 3개 이벤트를 루프로 구독/해지. 카드와 부모 UI 가 생명주기 동일하므로 이벤트 우회 가능.
- **수정**: CardView 가 부모 참조를 받아 메서드 직접 호출하거나, 클릭 이벤트만 단일 채널로 묶기.

### 3-4. 실행 순서 의존성
- [BattleController](Assets/3.Script/Core/BattleController.cs#L8) `DefaultExecutionOrder(-100)` ↔ [BattleSceneAudio](Assets/3.Script/Audio/BattleSceneAudio.cs#L7) `DefaultExecutionOrder(100)` ↔ [TurnIndicator](Assets/3.Script/UI/TurnIndicator.cs#L24)
- **개선안**: DamageResolver 를 lazy-init 대신 명시적으로 외부 주입하면 순서 의존 자체가 사라짐.

### 3-5. 이벤트가 적정량인 것
- `BattleController` 5개 이벤트(OnStateChanged 등) — 리스너 다수, 합리적 ✓
- 굳이 줄일 필요 없음.

---

## 4. 성능 / 메모리

### 4-1. SaveSystem 디스크 I/O 빈도 (최고 영향)
- **위치**: [BattleSaveTrigger.cs:37](Assets/3.Script/Run/Save/BattleSaveTrigger.cs#L37), [SaveSystem.cs:80](Assets/3.Script/Run/Save/SaveSystem.cs#L80)
- **현상**: 매 턴 AwaitCardSelect 진입마다 `JsonUtility.ToJson` + 파일 쓰기. 30턴 전투 = 30회 디스크 sync.
- **개선안**:
  - 체크포인트 빈도 축소 (3턴 또는 전투 종료 시점)
  - 직렬화 결과 캐시 → 직전과 동일하면 skip
  - 비동기 I/O (`File.WriteAllTextAsync`)

### 4-2. 반복되는 임시 컬렉션 할당
- [BattleSceneUI.cs:263, 270](Assets/3.Script/UI/BattleSceneUI.cs#L263), [BattleController.cs:352](Assets/3.Script/Core/BattleController.cs#L352)
  - `new HashSet<int>(GetValidTargets())` — 타겟 하이라이트마다
- [Side.cs:54-62](Assets/3.Script/Core/Side.cs#L54) — `PopRandomStandby` 매 호출 `new List<int>()` (최대 3원소)
- [MightyAttack.cs:34-44](Assets/3.Script/Cards/Attack/MightyAttack.cs#L34) — `GetAdjacentAliveIndices` 매 호출 `new List<int>`
- [VolleySkill.cs:19](Assets/3.Script/Cards/Skill/VolleySkill.cs#L19) — `.ToList()`
- **개선안**: 필드 크기 고정(최대 3)이라 stackalloc 또는 재사용 가능한 `int[]` 한 개 + 카운트로 충분.

### 4-3. `WaitForSeconds` 캐싱 부재
- [AIController.cs:49](Assets/3.Script/AI/AIController.cs#L49), BattleSceneUI 여러 곳
- **수정**: `static readonly WaitForSeconds wait04 = new(0.4f)` 패턴.

### 4-4. UI 프리뷰 텍스트 매번 문자열 생성
- [CardView.cs:147](Assets/3.Script/UI/CardView.cs#L147) — `$"+{hpBonus} HP"` 가 BindAll 마다 새로 생성.
- [DeckLayoutPanel.cs:37](Assets/3.Script/UI/OutGame/DeckLayoutPanel.cs#L37) — 슬라이드마다 전체 BindAll
- **개선안**: dirty flag 로 변경분만 갱신, 또는 작은 정수 → 문자열 캐시.

### 4-5. 좋은 점 (이미 잘 되어 있음)
- Update 안 `GetComponent` / `Find` / `Camera.main` — 없음 ✓
- `Resources.Load` — `GameAssetsSO` 통해 캐시됨 ✓
- Dictionary 키로 enum + 커스텀 comparer — 해당 없음 ✓

---

## 5. 중복 코드

### 5-1. Attack 4종 통합
- [NormalAttack](Assets/3.Script/Cards/Attack/NormalAttack.cs) / [HealerAttack](Assets/3.Script/Cards/Attack/HealerAttack.cs) / [RangedAttack](Assets/3.Script/Cards/Attack/RangedAttack.cs) / [MightyAttack](Assets/3.Script/Cards/Attack/MightyAttack.cs)
- 공통: `c.attackerSide.field[c.attackerIndex]`, `c.defenderSide.field[targetIdx]` 접근 패턴 반복.
- 차이: 상호/일방향/스플래시 — enum + 베이스 클래스로 통합 시 30~40줄 절감.
- HealerAttack 이 이미 NormalAttack 위임 패턴을 쓰므로 ✓ 그 방향을 일반화.

### 5-2. Skill 4종 통합
- 숫자형(Heal, Volley) vs 상태형(Taunt, LastStand) 2계열로 묶기.
- 보너스 적용 로직(`+ skillBonus`)이 각 스킬에 흩어져 있어 신규 스킬 추가 시 누락 가능.

### 5-3. UpgradeEvent 패널 통합
- [SingleHpUpgradeEvent](Assets/3.Script/UI/OutGame/Event/SingleHpUpgradeEvent.cs) (87줄), [SkillUpgradeEvent](Assets/3.Script/UI/OutGame/Event/SkillUpgradeEvent.cs) (101줄), [PaidRecruitEvent](Assets/3.Script/UI/OutGame/Event/PaidRecruitEvent.cs) (220줄)
- CardView 그리드 + Awake 구독 + OnDestroy 해지 패턴이 3곳에서 거의 그대로 반복.
- `CardViewGridUpgradeEventBase` 추상 클래스로 60~80줄 절감.

### 5-4. CardView Bind 책임 분리
- [CardView.cs](Assets/3.Script/UI/CardView.cs) 의 `Bind(CardInstance)` 와 `BindPreview(CardDataSO, bonuses)` 가 같은 클래스에 혼재.
- 전투 중 라이브 데이터 / 프리뷰 정적 데이터를 한 클래스가 둘 다 알면 분기 늘어남.
- → `LiveCardView` / `PreviewCardView` 또는 공통 인터페이스로 분리.

---

## 6. 매직 넘버 / 하드코딩

| 카테고리 | 위치 | 추출 대상 |
|---------|------|-----------|
| 애니메이션 | [BattleSceneUI.cs:31-44](Assets/3.Script/UI/BattleSceneUI.cs#L31) | `BattleAnimationConfigSO` |
| 점수/페널티 | [RunState.cs:24-28](Assets/3.Script/Run/RunState.cs#L24), [HeuristicAIStrategy.cs:59](Assets/3.Script/AI/HeuristicAIStrategy.cs#L59) | `ScoringSO` |
| UI 색상 | [TurnIndicator.cs:12-13](Assets/3.Script/UI/TurnIndicator.cs#L12) | `UIThemeSO` |
| 맵 좌표/jitter | [MapPanelController.cs:26-40](Assets/3.Script/UI/OutGame/MapPanelController.cs#L26) | `MapLayoutSO` |
| 씬 이름 | [SceneNames.cs](Assets/3.Script/Run/SceneNames.cs) | ✓ 이미 분리됨 — 활용도 점검만 |

---

## 7. 가시성 / 캡슐화

- `BattleController.CurrentEnemyPool` — 외부 1군데에서만 읽기, internal/private 검토
- `BattleSceneUI.PlayerFieldViews` 등 4개 배열 — `IReadOnlyList` 로
- `CardView.OwningSide, SlotIndex` — internal 가능
- `RunState` 의 다수 public setter — private setter + 메서드 호출만 허용

---

## 8. null 처리 / 예외

- [BattleSceneUI.cs:67-69](Assets/3.Script/UI/BattleSceneUI.cs#L67) — `battle == null` 시 경고만 + 진행 → 이후 NRE
- [MapPanelController.cs:72-75](Assets/3.Script/UI/OutGame/MapPanelController.cs#L72) — `mapPool` null 체크 미흡
- [CardInstance.CreateRestored](Assets/3.Script/Cards/CardInstance.cs) — `data == null` 시 null 리턴, 호출자 미처리
- `SaveSystem` 로드 실패 시 정책 불명확 — 무시인지 초기화인지 명시 필요

---

## 9. 테스트 가능성

- 순수 도메인으로 잘 분리된 것: `DamageResolver`, `CardInstance` (POCO) ✓
- 테스트 어려운 것:
  - `BattleController.BuildOpponentCards` — MonoBehaviour 안에 + EnemyPool 의존
  - `HeuristicAIStrategy` — 내부 `Random.Range` 직접 호출 → `IRandom` 주입
  - `MapPanelController.Rebuild` — UI 씬 필요. 노드 생성 로직만 떼면 unit test 가능
- 우선순위: AI 의 `Random` 추상화 (1시간 작업, 회귀 안전성 크게 ↑)

---

## 10. 추천 로드맵

### 1주차: 출혈 막기 (버그 수정)
- [ ] 1-1, 1-2 (OpeningController, BattleSceneAudio) 이벤트 누수
- [ ] 1-3, 1-4 (`FindObjectOfType` → SerializeField/이벤트 등록)
- [ ] 1-5 MapPanelController 중복 구독 근본 원인 추적
- [ ] 1-6 BuildRestoredRow 실패 정책

### 2주차: 작은 정리로 큰 효과
- [ ] 4-3 WaitForSeconds 캐싱 (10분 작업)
- [ ] 6 매직 넘버 → SO (1~2일)
- [ ] 7 가시성 좁히기 (수동 점검)
- [ ] 8 null 정책 명시

### 3~4주차: 중복 제거
- [ ] 5-1, 5-2 Attack/Skill 통합
- [ ] 5-3 UpgradeEvent 패널 통합
- [ ] 5-4 CardView 분리

### 5주차 이후: 구조 개혁 (위험도 높음, PR 작게 쪼개기)
- [ ] 2-1 RunState → RunContext 점진 마이그레이션
- [ ] 2-4 거대 클래스 분할 (BattleSceneUI / BattleController)
- [ ] 2-2 BattleController ↔ UI 양방향 결합 해소
- [ ] 2-6 SaveBridge 책임 도메인으로 위임
- [ ] 4-1 SaveSystem 비동기/캐시 I/O

---

## 11. 잘 되어 있어 건드릴 필요 없는 것

- `ICardAttack` / `ICardSkill` / `IAIStrategy` 인터페이스 분리 ✓
- `SkillFactory` 무상태 ✓
- `GameAssetsSO` 를 통한 Resources 캐시 ✓
- `SceneNames` 문자열 상수 분리 ✓
- Update 안에 비싼 호출이 거의 없음 (턴제 특성 잘 살림) ✓
- `BattleController` 의 이벤트 5종 — 적정 ✓
- `DamageResolver` 가 순수 클래스 ✓
