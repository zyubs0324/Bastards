# 횡스크롤 이동 전투 전환 계획

**Review Scope**: arch, lang, quality, security, perf, test, comments
**Status**: 진행 중 — Phase 0 문서 정합 완료(2026-10-01, 커밋 `94c4529`·`28fb462`). **Phase A 완료**(2026-10-02, 커밋 `d8125ff`·`62bff36`·`3f4595d`). **Phase B 구현·검증 완료, 커밋 대기**(2026-10-06: 테스트 333개 통과·`TAoB.Combat` 커버리지 99.2%·스모크 오류 0·Windows/APK 빌드 성공·자동 캡처로 등장·교전·보스·결과 화면 확인, `/plan-review --code` 반영). **다음 = 폰에서 1차 재미 판정**. 직전 계획서는 `.claude/plans/completed/vertical-slice.plan.md`(Phase 0~5 완료, 최종 커밋 `0895f20`)

## Context

수직 슬라이스(정적 2열 대치 + 웨이브 교체)를 Phase 0~5 까지 구현·커밋하고 APK 까지 만들었다. 그런데 사용자가 폰에서 재미 판정을 하기 전에 **"전투 진행이 내 생각과 다르다 — 횡스크롤로 이동하면서 전투해야 한다"** 고 판단했다. 레퍼런스는 **블루 아카이브**(부대가 자동 전진하다 교전 구역에서 싸우고 다시 전진)와 **트릭컬 리바이브**(오토배틀러·배속)다.

조사로 확인한 사실:
- 전투 규칙 모듈(`TAoB.Combat`)에는 **거리·사거리·이동 개념이 없다**(25개 파일 전수 검색).
- `Row`(Front/Back)는 좌표가 아니라 열거값이고 전투 중 불변이며, **피해 배율(`Formation.DamageTakenPermille`)과 기본 대상 선택(`TurnRules` 앞열 우선) 두 곳**에서 쓰인다.
- 유닛 화면 위치는 `BattleLayout.UnitPosition` 이 생성 시 1회 정하고 그 뒤 움직일 경로가 없다.

이번 변경은 연출 추가가 아니라 **위치를 규칙으로 들이는 일**이다. 목표는 등장 → 행군 → 조우 → 섬멸 → 재행군이 끊김 없이 이어지고 동료가 각자 성향대로 싸우는 전투를 폰에서 보고 재미를 판정하는 것이다. 그림은 색 네모를 유지한다.

## 사용자 요구 (정본 — 이 절에만 적는다)

**흐름**
1. 전장 진입 → 진형을 갖추고 맵 중앙 등장
2. 배경이 흐르며 오른쪽으로 행군
3. 적 등장 → 가급적 진형을 유지한 채 전투. 적이 뒤로 돌아 뒷열을 공격하기도 한다. 아군은 적이 다가오면 물러서거나 맞선다
4. 섬멸 → 재정렬 → 다시 행군
5. 일반 조우 3회, 보스 전장은 4회째에 보스
6. 이동-전투가 딱딱하게 끊기지 않고 심리스

**조작**: 진형유지·이동·전투·기술은 자동. 사용자 수단은 기술 강제 발동(발동 가능해지면 동료 몸이 빛난다), 과부하, 구조, 소모품, 퇴각.

**기술**: 자동 사용과 강제 발동은 **별개 규칙**이다. 자동 사용에는 재사용 대기가 없고 숨은 스탯 "기술 사용 빈도"로 나간다 — 자주 31~39% / 종종 20~29% / 가끔 10~19% / 낮음 5~9% / 매우 낮음 1~4%. 보통 기술이 강할수록 빈도가 낮고 강제 발동 쿨타임이 길다.

**구조**: 전투가 일단락되면 이동 전에 AI 가 수행. 전투 중 수동 가능. 5초 걸리고, 그동안 구조자는 전투에서 빠지며, 피격당하면 진행이 초기화된다.

**선호·규율**: 동료마다 "선호"와 숨은 "진형유지 속성값"이 있다. 축구 포메이션 비유 — 진형은 가급적 유지하되 흐트러지고, 역할을 잘 수행하면 유리하고 제멋대로면 불리하다. 1차 구현은 선호 4종(사용자 결정 2026-10-01).

**제한 시간 60초**.

## 전역 설계 결정

### D1. 전환을 상태로 만들지 않는다 (심리스의 근거)
규칙이 갖는 것은 정수 위치 `PosX` 와 진형 기준점 `AnchorX`·`EnemyAnchorX` 뿐이다. `이동 중/전투 중` 모드는 없다. 표시 계층도 모드를 모르고 `AnchorX` 를 보간한다.

### D2. 앵커 전진·정지 (정본 — 이 목록이 전부다)
앵커는 아래 중 **하나라도 참이면 전진하지 않는다**. 그 외에는 매 틱 `marchSpeedFu` 만큼 전진한다.
1. **기준 열의 생존자 중 하나라도** 적을 **사거리에서 `engageMarginFu` 를 뺀 거리** 안에 두고 있다(교전 시작 — 경계에서 멈추면 한 걸음에 들락거리며 떨린다). 기준 열 = 아군 **앞열 생존자가 있으면 앞열, 없으면 뒷열**
2. 흩어진 아군이 있다
3. 구조 캐스팅이 진행 중이다(Phase D 에서 추가)

정지선을 앞열이 정하는 이유: 뒷열 사거리가 더 길어 뒷열 기준으로 멈추면 앞열이 사거리 밖에 남아 "진형을 유지한 채 전투"와 반대가 된다. 폴백이 필요한 이유: 앞열이 전멸했거나 전원 뒷열 편성이면 조건 1 이 영원히 거짓이 되어 **교전 중에도 행군한다**(재리뷰 지적).

### D3. 1차원 정수 위치 + `Row` 유지
`PosX` 만 규칙에 넣고 Y 는 표시 전담. 단위는 fu(전장 단위). 설계 시작값으로 기준 해상도에서 1fu = 1px 로 잡지만, 환산은 표시 계층의 `BattleLayout.PxPerFu` **한 지점**에서만 한다 — 값이 1이어도 상수로 남긴다(기준 해상도가 바뀌면 여기만 고친다).

### D4. Row 에서 파생되는 값은 모두 `Formation` 이 갖는다
`Formation.DamageTakenPermille`(기존) · `Formation.ReachFu(Row, lane)` · `Formation.SlotOffsetFu(Row, lane)`. `Lane` 은 Row 를 모르는 순수 정수 산술(거리·한 걸음·클램프)만 갖는다. 사거리와 간격은 교착 불변식에서 함께 쓰이는 한 쌍이므로 소유 타입이 갈리면 안 된다.

### D5. 플래그를 늘리지 않는다
`IsOnField`(등장 여부)·`CanAct`·`IsTargetable` 의 의미를 바꾸지 않는다. "사거리 안인가"는 쌍 술어 `Engagement.CanReach(actor, target, lane)`(비대칭 — 행위자 기준)로 둔다.

### D6. 사거리 판정의 정본은 하나다
사거리 필터는 `Engagement` 에만 둔다. `BattleSimulation` 은 "이 유닛이 때릴 수 있는 대상이 있는가" 단일 질의만 호출한다.
- 대기 명령은 **비파괴 조회**로 확인한다(`CommandQueue` 에 `TryPeek` 추가 — 지금은 꺼내면서 지우는 `TryTake` 뿐이다). 사거리 밖이라 그 틱을 건너뛰면 **명령과 행동 게이지를 모두 유지**한다(사용자가 누른 기술이 조용히 사라지지 않게).
- 순서: **사거리 질의 → 대기 명령 조회 → (대기 명령이 없을 때만) 기술 빈도 롤**. 건너뛴 틱에는 난수를 소비하지 않는다.
- 사거리 밖에서 대기하는 동안 행동 게이지 상한은 **1×Max**로 둔다(기존 2×Max 그대로면 접촉 직후 2연타가 터진다 — 재리뷰 지적).
- **대상 선택(`TargetSelector`)에는 사거리 필터를 두지 않는다.** 관문은 "때릴 상대가 있는가" 하나뿐이고, 열별 좌표가 고정인 Phase A 에서는 앞열 우선 선택이 곧 최근접 선택이다. 대상별 필터는 선호 기반 대상 선택과 같은 커밋(Phase C)에서 들어온다 — 그때까지 앞열이 사거리 밖인데 대상으로 뽑히는 구성은 Phase B 실값에서 교착 테스트로 확인한다.
- `Rescue`·`Overload`·폭주 대상 추첨에는 사거리를 적용하지 않는다(위치와 무관한 행동). 폭주 후보 집합이 바뀌면 난수 소비가 달라지므로 기존 동작을 보존한다.
- 대상이 **아예 없을 때**(같은 틱에 마지막 적이 쓰러진 경우)는 기존처럼 게이지를 소비하고 `ActionStarted` 를 내보낸다 — 사거리 때문에 건너뛰는 경우와 구분한다.

### D7. 기술 자동 사용과 강제 발동은 별개
- **자동**: 행동할 때마다 `Chance.Roll(random, skillChance)` 성공이면 `UseSkill`, 아니면 `BasicAttack`. **재사용 대기를 보지도 걸지도 않는다.**
- **강제 발동**: 사용자 버튼 전용. 기존 `Cooldown` 은 이제 **강제 발동 전용 쿨타임**이다. 자동 사용이 이 쿨타임을 소모하지 않으므로 개입 가치가 유지된다.
- 적도 같은 규칙을 쓴다(기존 "적은 쿨다운 0이면 기술" 규칙을 대체).
- **구분 수단**(재리뷰 HIGH — 지금 구조에는 출처 표식이 없다): `BattleCommand` 에 `IsForced` 를 추가한다. 사용자 경로만 `true` 로 만들고, `ActionResolver` 는 `IsForced` 일 때만 `Cooldown` 을 설정한다. 명령 큐는 `IsForced` 가 거짓인 명령을 받지 않으므로(자동 명령은 큐를 거치지 않는다) 큐에서 나온 기술은 항상 강제 발동이고, 재사용 대기 검사를 따로 갈라 둘 필요가 없다. `AutoPilot.Choose` 는 `IRandom` 을 받는 시그니처로 바뀌고(`BattleSimulation` 이 이미 난수를 들고 있다) 빈도는 `unit.Setup.SkillChance` 에서 읽는다 — 빈도를 따로 넘기면 정본이 둘이 된다. `ActionStarted` 의 `Detail`(= `CommandKind`)은 바뀌지 않아 표시 계층 영향이 없다.
- `UnitSetup` 은 `skillChance` 를 0~`ChanceRange` 로 받고(규칙 모듈 하한), 기획 대역(100~3900)은 `CharacterReader` 가 검증한다. **테스트 기본값은 0** — 그래야 고정 난수를 쓰는 기존 테스트가 기술 롤에 값을 빼앗기지 않는다(재리뷰 HIGH).

### D8. 확률 판정은 공용 가드를 쓴다
`Chance.Roll(IRandom, int chancePermyriad)` 하나만 쓴다 — 0 이하면 `false`, `CombatScale.ChanceRange` 이상이면 `true`, **두 경우 모두 난수를 소비하지 않는다**(기존 `RollCritical` 전례). 호출부마다 가드를 복제하지 않는다.

### D9. 결정 순서
- **조우 단위 결정**(선호 따름 여부·자세·침투 지정)은 `_unitsById` **ID 오름차순**으로 돈다.
- **행동 중 판정**(기술 빈도 롤·크리티컬)은 기존대로 **행동 순서**(SPD 내림차순·ID 오름차순)를 따른다. 기존 크리티컬 판정이 이미 그 순서이므로 둘을 섞으면 정본이 둘이 된다.

### D10. 해시는 Phase 마다 덧붙인다
**이번 변경으로 추가되는** 지속 상태는 예외 없이 해시에 넣고, 그 Phase 에서 함께 갱신한다. Phase A: `PosX`·`AnchorX`·`EnemyAnchorX`. Phase C: 자세·침투 지정. Phase D: 구조 진행 틱·대상·소모품 잔량.
기존에 해시에 없던 상태(대기 명령 큐·후퇴 요청)도 이번에 **함께 넣는다** — 대기 명령은 D6 으로 수명이 길어져 더 중요해진다.

### D11. 기존 226개 테스트 보호 — 테스트 픽스처를 "정지 프리셋"으로
테스트용 `LaneConfig` 는 **불변식을 만족하는 정지 프리셋**을 쓴다: 사거리는 전장보다 크게(모든 쌍이 사거리 안), `marchSpeedFu` 1, 자리 간격·여유 0, 접근 거리는 최소값 1(사거리가 덮으므로 거리가 행동을 바꾸지 않는다). 결과적으로 교전 조건이 항상 참이라 앵커가 멈추고, 목표 위치가 제 자리라 아무도 움직이지 않는다.
`Disabled` 전용 팩토리를 두지 않는 이유: 비활성 분기가 `Movement`·`Engagement` 에 영구히 남아 실값을 쓰는 Phase B 이후 **죽은 경로**가 되기 때문이다(재리뷰 지적). 같은 보호를 분기 없이 얻는다.
Phase A 의 `balance.json` 도 같은 프리셋 값으로 커밋한다 — "꺼짐"이라는 특수 값을 데이터에 만들지 않는다.
단 이 장치는 **위치 관련 회귀만** 막는다. D7(기술 자동 사용)은 수치와 무관하게 기존 동작을 바꾸므로 영향 테스트를 Phase A 에 따로 명시한다.

### D12. 표시 계층 구조
`Lane` 을 **중첩 캔버스**로 둔다. 스크롤로 인한 재배칭이 HUD·개입 바·팝업으로 번지지 않게 하기 위함이다. 월드 스페이스(SpriteRenderer) 이행은 Phase F-2 로 미루되 **판정 기준을 수치로 정한다**(아래 Phase B 검증).
근거 정정: "지금도 매 프레임 모든 유닛이 움직인다"는 틀렸다 — 현재 매 프레임 움직이는 것은 유닛당 박스 1개(약 2개 요소)이고, 변경 후에는 유닛 전체(7명 × 약 12요소)와 배경이 움직인다. 7명 규모에서 감당 가능하다고 보지만 측정으로 확인한다.

### D13. 이동에는 이벤트를 만들지 않는다
틱당 유닛당 이벤트는 "틱당 할당 없음" 결정을 깬다. 위치는 스냅샷으로만 흐른다. `Movement`·`Engagement` 는 호출자 버퍼를 재사용하고 튜플·람다 캡처·박싱 열거를 쓰지 않는다.

### D14. 용어는 Phase 0 에서 용어집에 등재한다
**정본은 `docs/references/project_facts.md` §0-B** 다(이 계획서에 표를 복제하지 않는다). 등재 원칙만 적는다 — 기획 용어 ↔ 식별자 ↔ 단위를 한 줄로 두고, 사용자 명령 `Retreat`(후퇴)와 비겁 성향의 **뒤로 서기**(`StepBack`)를 다른 개념으로 구분하며, 데이터·코드·UI 의 `Wave` 는 그대로 두고 개명은 Phase F-1 로 미룬다.

## Phase 0 — 문서 정합 (코드 변경 없음)
**목표**: 새 대화가 잘못된 방향을 이어받지 않는다.

**변경 파일**: `docs/HANDOFF.md`(§1 진행·마지막 커밋 `0895f20`·§2 다음 할 일) · `docs/references/project_facts.md`(§0 현재 단계의 끊긴 계획서 경로, §0-B 에 새 식별자 등재) · `The_Age_of_Bastards_게임기획서_v0.6.md`(13장·4-1 의 끊긴 경로와 방향 전환 표시 — 본문 개정은 Phase E) · `.claude/plans/completed/vertical-slice.plan.md`(Status 잔재 정리)

**검증**: `grep -rn "plans/vertical-slice.plan" --include=*.md` 가 `completed/` 와 결정 로그, 이 계획서를 뺀 곳에서 0건. 기획서 v0.6 의 전투 서술(4-1)과 13장이 이 계획과 어긋나는 지점을 Phase E 에서 고친다는 표시를 남긴다. `/plan-review --code`(문서 버킷) 후 커밋.

## Phase A — 규칙에 위치·이동·교전을 넣는다 (이동 끔 상태로 커밋)
**목표**: 규칙이 위치·거리·교전을 다루고 기술 자동 사용이 빈도 기반으로 바뀐다. 데이터는 아직 이동을 켜지 않아 화면 동작이 지금과 같다.

**변경 파일**
- 신규 `Scripts/Combat/`: `Lane.cs`(Row 를 모르는 정수 산술), `LaneConfig.cs`(수치·범위·불변식 정본), `Movement.cs`(틱당 이동 1단계), `Engagement.cs`(사거리 쌍 술어·접근 목표), `SectionProgress.cs`(앵커 전진·정지 판정 — `BattleSimulation` 비대 방지), `Chance.cs`(확률 가드)
- 수정 `Scripts/Combat/`: `UnitState.cs`(`PosX`), `BattleSimulation.cs`(틱 단계 삽입·스폰 위치·해시·`FillSnapshot`·`_unitsById` 필드 승격), `BattleSnapshot.cs`(`UnitSnapshot.PosX` **끝에 추가**, `BattleSnapshot.AnchorX`), `TurnRules.cs`(사거리 밖 게이지 상한·`AutoPilot` 이 난수를 받고 빈도는 `Setup.SkillChance` 에서 읽음), `ActionResolver.cs`(강제 발동만 쿨타임 설정), `BattleCommand.cs`(`IsForced`), `CommandQueue.cs`(`TryPeek` — 비파괴 조회), `Formation.cs`(`ReachFu`·`SlotOffsetFu`), `BalanceConfig.cs`(`LaneConfig` 를 **23번째 인수**로), `BattleSetup.cs`(조우별 `WaveApproachFu` + 좌표 상한), `UnitSetup.cs`(`SkillChance`), `AssemblyInfo.cs`(신규 — 테스트에 internal 공개)
- 수정 `Scripts/Data/`: `Dtos.cs`·`BalanceReader.cs`(`movement` 블록 + `ReadLane`), `CharacterReader.cs`(`skillChance`), `StageReader.cs`(`approachFu`·보스 위치), `GameData.cs`(조립 지점에서 파일 간 시간 예산 검증)
- 수정 데이터: `balance.json`(`movement` — **정지 프리셋**), `characters.json`(`skillChance`), `stage_slice.json`(`approachFu` — 최소값 1)
- 수정 테스트 지원: `TestBalance.cs`(`StaticLane`)·`TestUnits.cs`(`skillChance` 기본 0·`Approach`)·`TestJson.cs`·`Snapshots.cs`·`TestField.cs`(신규 — 좌표만 꾸민 유닛 상태)

**결정**
- **틱 순서**: `events.Clear` → 종료 확인 → `Tick++` → 후퇴 → 흩어짐 피해 → 재사용 대기 감소 → **이동(신규)** → 게이지 충전 → 행동 → 조우 전환 → 승패. 이동이 게이지 충전 앞에 와야 사거리 판정 전에 위치가 확정된다.
- `StartWave` 는 틱 끝에 남고, **생성자에서 모든 유닛의 `PosX` 를 스폰 위치로 확정**한다(표시 계층이 직전 스냅샷의 낡은 좌표에서 미끄러지지 않게).
- **`LaneConfig` 수치**(이름·단위·범위, 생성자가 정본):

  | 필드 | 단위 | 범위 | 뜻 |
  |---|---|---|---|
  | `marchSpeedFu` | fu/틱 | 1~1000 | 행군·접근 속도 |
  | `frontSlotOffsetFu` / `backSlotOffsetFu` | fu | 0~10000 | 기준점에서 앞열·뒷열까지 |
  | `frontReachFu` / `backReachFu` | fu | 1~20000 | 앞열·뒷열 사거리 |
  | `engageMarginFu` | fu | 0~1000 | 사거리 경계보다 안쪽에 서는 여유(떨림 방지) |

  이 Phase 가 소비하는 6개만 둔다. 이탈 허용치(`formationDriftFu`·`chargeDriftFu`)·위협 거리·물러섬 거리·침투·비겁 확률은 **쓰는 코드와 같은 커밋**(Phase C)에서 들어온다 — 소비처 없는 키를 데이터 스키마에 먼저 굳히면 뜻이 정해지기 전에 파일·DTO·테스트가 그 이름에 묶인다.

- **교착 방지 불변식**(생성자에서 수치를 담은 예외로 검증). 전제: 진형은 앞열이 `frontReachFu − engageMarginFu` 거리에 닿으면 멈추고, 뒷열은 거기서 진형 간격(`frontSlotOffsetFu + backSlotOffsetFu`)만큼 더 뒤에 선다.
  - `marchSpeedFu ≥ 1`
  - `frontReachFu − engageMarginFu ≥ marchSpeedFu` 와 `backReachFu − engageMarginFu ≥ marchSpeedFu` — 교전 창이 한 걸음보다 좁으면 한 틱에 창을 건너뛰어 정지선이 영영 걸리지 않는다(앞열 전멸 시 정지선이 뒷열로 떨어지므로 두 열 모두 필요하다)
  - `backReachFu ≥ frontReachFu − engageMarginFu + frontSlotOffsetFu + backSlotOffsetFu` — 덮지 못하면 뒷열이 전투 내내 한 번도 때리지 못한다
- **정수 함정 방어**: 한 걸음은 `min(속도, |목표−현재|)` 로 클램프한다. 거리 계산은 `long` 으로 하고 `CombatScale.ClampToInt` 로 좁힌다. ‰ 곱을 절삭해 0이 되는 경로를 만들지 않는다. `PosX` 와 앵커의 상한은 `BattleSetup` 생성자가 조우당 `MaxApproachFu`(20000)와 접근 거리 합 `MaxFieldSpanFu`(10,000,000)로 건다 — 전투 중 전진분(속도 ≤ 1000 × 제한 시간 ≤ 6000틱)을 더해도 `int` 범위의 1% 안이다.
- **기술 자동 사용(D7) 영향**: `AutoPilotTests` 3개(아군 기본 공격 전용·적 쿨다운 기반)와 `TurnRules.cs:50` 주석이 거짓이 된다. 피해량·처치 시점을 단정하는 시뮬레이션 테스트도 영향을 받는다. 구현에서 `AutoPilotTests` 3개를 빈도 기반으로 고쳤고, 빈도 0·10000 에서 난수를 소비하지 않음과 대기 명령이 있으면 롤을 돌지 않음을 테스트로 고정했다.
- **데이터 검증**(로드 시점, 실패는 파일명·필드 경로를 담은 예외):

  | 파일 | 필드 | 검증 |
  |---|---|---|
  | `balance.json` | `movement.*` | `LaneConfig` 생성자 범위·불변식, 누락 금지(`ReadLane` 이 `movement.` 접두를 붙여 다시 던진다) |
  | `characters.json` | `skillChance` | 100~3900, 누락 금지 |
  | `characters.json` | `preference`(Phase C) | 허용 이름 4종, **DTO 는 `int` + `Missing` 으로 읽는다** — 열거형으로 두면 누락이 0(첫 값 "동료 보호")으로 조용히 통과한다 |
  | `characters.json` | `formationChance`(Phase C) | 0~10000, 누락 금지 |
  | `stage_slice.json` | `approachFu` | 1~20000, 조우마다 필수 |
  | `stage_slice.json` | 보스 | `isBoss` 캐릭터는 마지막 조우에만 |
  | `consumables.json`(Phase D) | ID·이름·회복량·초기 수량 | 배열 ≤ 10, ID 중복 금지, 회복량 1~`CombatScale.MaxValue`, 수량 1~99 |
  | `balance.json` | `rescueTicks`(Phase D) | 1~제한 시간 틱 — 너무 크면 자동 구조가 앵커를 제한 시간까지 묶는다 |
  | `stage_slice.json` + `balance.json` | 시간 예산 | `접근 거리 합 ≤ (long)marchSpeedFu × 제한 시간 틱`. 두 파일을 함께 아는 **`GameData` 조립 지점**에서 검증한다(`StageReader` 는 stage 파일만 읽으므로 `LaneConfig` 를 선택 인자로 받으면 조용히 건너뛸 수 있다) |

  새 DTO 정수 필드는 전부 `FieldCheck.Missing`(-1)로 초기화해 누락이 0 으로 통과하지 않게 한다.
- `BalanceConfig` 의 "생성자 매개변수 이름 = JSON 키" 규약을 지키는 리플렉션 테스트가 `int` 인수만 돌아 `LaneConfig` 를 건너뛴다 → **그 테스트를 `LaneConfig` 생성자까지 확장**한다.
- 데이터의 이동 수치는 이 Phase 에서 **정지 프리셋**(D11)으로 커밋한다. 실값·조우 4개·제한 시간 60초는 Phase B 와 같은 커밋에 넣는다(규칙은 움직이는데 화면은 고정인 상태를 커밋하지 않기 위함).
- **소모품 틱 위치**(Phase D 에서 쓰지만 순서는 여기서 정한다): 후퇴 확인 **직후**, 흩어짐 피해 앞. 소모품은 유닛 슬롯 큐와 **분리된 파티 단위 대기열**을 쓴다 — 지금 큐는 유닛 ID 가 키라 같은 큐에 넣으면 그 유닛의 기술 명령을 덮어쓴다(재리뷰 지적).

**검증**
- 기존 226개: 위치 관련은 **정지 프리셋**(D11) 픽스처로 보존, 기술 자동 사용 영향분(`AutoPilotTests` 3개)은 수정.
- **실측 결과 313개 전부 통과**(신규 87). `Lane` 7 · `Movement` 10 · `Engagement` 8(사거리 경계 `=`/`+1` 포함) · `SectionProgress` 9 · `LaneConfig` 9 · `Chance` 4 · 접근 거리 검증 6 · 기술 빈도 5 · 전투 중 위치 11 · 데이터 검증 14 · 결정론 3
- `StaticPreset_NeverMovesAndConsumesNoRandom` · `LastEnemyFallingInTheSameTick_StillLetsTheNextAllyAct` · `OutOfReach_KeepsThePendingCommandAndTheGauge` · `OutOfReachTicks_ConsumeNoRandom` · `OnlyPosXDiffers_HashDiffers`
- 결정론: 이동이 켜진 시드 시나리오 2회 실행·배속 실행 동일. 커버리지 `TAoB.Combat` **99.2%**(기준 80%).

## Phase B — 횡스크롤 화면 (재미 판정 1차)
**목표**: 진형이 등장해 배경이 흐르며 전진하고, 조우 4회를 거쳐 보스까지 간다.

**변경 파일**
- 신규 `Scripts/View/`: `LaneView.cs`(중첩 캔버스·스크롤·띠 재사용·패럴랙스 — 카메라 리드는 Phase E), `LaneScroll.cs`(순수 계산)
- 수정 `Scripts/View/`: `BattleLayout.cs`(`UnitY` 분리·`PxPerFu`·띠 상수), `UnitView.cs`(루트 X 를 `PosX` 보간, `!previous.IsOnField` 면 보간 없이 현재 위치), `BattleBootstrap.cs`(`Lane` 생성·`PositionOf` 가 `Lane` 오프셋과 `Field` 흔들림을 모두 반영), `BattleHud.cs`(조우 배너 제거·상시 카운터·성능 표시)
- 수정 데이터: `balance.json`(`movement` 실값), `stage_slice.json`(조우 4개·`approachFu` 실값·60초·사전 조건), `characters.json`(조우 4회에 필요한 적 2명 추가 — 한 캐릭터를 두 조우에 둘 수 없다)
- 수정: `UiText.cs`(조우 배너 문구 제거 — 소비처가 없어졌다)
- 수정: `Editor/PlayModeSmoke.cs`(계층 경로), `Scripts/View/DevCapture.cs`(이동 구간 캡처)

**결정**
- 계층: `Field`(흔들림) → `Lane`(중첩 캔버스, 스크롤) → 배경 띠 + 유닛. `ScreenShake`·`PopupPool`·`MotionCurves`·`BattleEffects` 는 코드 변경 없음.
- 스크롤 양은 `AnchorX` 스냅샷 보간에서만 구한다(별도 시간 누적 금지 — 배속에서 어긋난다).
- 배경은 단색 띠 재사용(랩어라운드). `RawImage.uvRect` 스크롤·머티리얼 런타임 복제 금지.
- 등장 연출은 표시 전담(틱 루프를 잠깐 멈추는 기존 구조 재사용). 규칙·해시 영향 없음.
- 성능 표시는 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 안에 두고 **0.5초 창의 최대 프레임 시간·한 판 누적 33ms 초과 횟수·창 안 프레임당 실행 틱 최댓값**을 0.5초마다 갱신한다(평균 fps 는 끊김을 가린다). 분위수를 만들 표본은 남기지 않는다.
- **이동 실값**(구현 중 측정으로 확정): 행군 40fu/틱 · 자리 간격 앞뒤 120 · 사거리 앞 320 / 뒤 560 · 교전 여유 40 · 조우마다 접근 2000.
  앞열은 적 앞열과 280fu(≈280px) 떨어진 자리에 멈춰 서고, 뒷열은 그 자리에서 적 앞열에 닿지만 적 뒷열에는 닿지 않는다 — 앞열이 쓰러지면 진형이 한 걸음 더 나아가 뒷열을 정리한다.
- **배경**: 하늘·바닥은 단색이라 흐를 것이 없어 전장 층에 고정하고, 흐르는 것은 스크롤 층 안의 바닥 표식과 먼 층(패럴랙스 0.45)뿐이다.
- **사전 조건을 `None` 으로 바꾼다**. `NoAllyDowned` 는 수직 슬라이스가 그 규칙을 보려고 둔 값인데, 조우 4회에서는 보스 직전에 한 명이 쓰러지며 **다섯 시드 모두 25초쯤에 패배로 끝나** 보스를 보지 못한다. 규칙 자체는 규칙 모듈 테스트가 계속 덮는다.

**검증**
- **기준값**: 성능 표시가 Phase B 에서 새로 생겼으므로 옛 APK 로는 같은 지표를 잴 수 없다. 대신 **같은 빌드 안에서 교전 구간(기준점 정지 = 옛 정적 장면과 같은 부하)과 행군 구간(스크롤)을 비교**한다 — 기기·빌드가 같아 비교가 더 정확하다. **판정 기준(표시 도구로 읽을 수 있는 값)**: 중급 기기에서 한 판을 돌린 뒤 ① 누적 끊김 횟수가 **30회를 넘거나**(60fps 기준 한 판 약 3600프레임의 1%) ② **행군 구간의 창 최대 프레임 시간이 교전 구간보다 두 배 이상 반복해서 크면** Phase F-2 착수로 판정한다. 눈대중이 아니라 읽은 값을 결정 로그에 남긴다.
- 신규 ~17: `LaneScroll` 랩어라운드(음수 나머지·큰 `AnchorX`)·보간 α 0/1·패럴랙스·`PxPerFu` 반올림, 좌표 테스트 2개 재작성, **실값으로 "N틱 안에 조우가 전환된다"**(교착 조기 발견), **이동을 켠 상태의 배속 결정론**(1틱씩 vs 묶어 실행 결과 동일), 그리고 Phase A 코드 리뷰가 실값으로 미룬 3건 — **조우 전환 뒤 재행군·재교전**, **행군 중 제한 시간 만료**, **행군 중 후퇴**
- 스모크 오류 0건 + 재시작 누수 · 자동 캡처로 등장·행군·교전 확인 · 폰 수동 확인
- 실값 한 판(구현 중 측정, 시드 1~5): **모두 승리 · 32~48초 · 조우 전환 ≈ 43 / 100~122 / 173~195틱 · 빈사 아군 1~2명**. 조우 사이 행군이 약 5초씩 들어가고 보스전이 가장 길다.

## Phase C — 선호와 규율 (역할 행동)
**목표**: 동료가 성향대로 움직이고, 적이 뒷열로 파고들며, 규율이 낮으면 진형이 무너진다.

**변경 파일**
- 신규 `Scripts/Combat/Preference.cs`(선호 4종의 이동 의도 — **대상별 사거리 필터를 여기서 함께 넣는다**), `Tactics.cs`(규율 판정·조우 시작 결정)
- 수정: `characters.json`(`preference`·`formationChance`), `CharacterReader.cs`·`Dtos.cs`, `UnitSetup.cs`(불변 설정), **`UnitState.cs`(조우마다 뽑는 자세·전술 — 지속 상태이므로 해시에 포함)**, `Movement.cs`·`Engagement.cs`·`TurnRules.cs`(도발 가중치)
- 수정 `Scripts/View/`: `BattleEffects.cs`·`UnitView.cs`(침투 궤적, 성향 표식)

**결정**
- 선호 **4종**(사용자 결정 — 색 네모에서 움직임으로 구분 가능한 것만):

  | 선호 | 이동 의도 | 비고 |
  |---|---|---|
  | 동료 보호 | 진형 전방부, 피격당한 아군 근처 | 도발 — 적의 대상 선택에서 뽑힐 가중치 상승 |
  | 적극 돌진 | 이탈 허용치 `chargeDriftFu` | 적진 깊숙이 |
  | 후방 유지 | 뒤에 선다 | 구조를 우선 수행 |
  | 비겁 | 위협 시 `stepBackFu` 만큼 물러섬 | |

  대상 선택 선호(가까운 적·약한 적 등)는 Phase F-3 으로 미룬다.
- 적의 **측면 침투**는 조우 시작 때 적 앞열이 **유닛별로** `flankChance` 를 굴려 정한다(인원수에 비율을 곱해 절삭하면 2명×30% 가 0 이 된다). 침투한 적은 이탈 허용치가 커져 뒷열까지 걸어가고, **앞열 우선 대상 선택은 그대로 둔 채** 사거리 안에 뒷열만 남아 자연히 뒷열을 때린다.
- 물러섬은 적을 쫓지 않고 자기 자리보다 뒤에 선을 잡는 것이다(쫓으면 사거리 경계에서 진동).
- 조우 단위 상태는 **자세**(`Hold`/`StepBack`)와 **침투 지정**(`IsFlanking`) 둘뿐이다. `UnitState` 에 두고 **해시에 포함**한다(D10). 한 번 뽑으면 틱마다 다시 뽑지 않는다. 결정은 `Chance.Roll` 을 쓰고 ID 오름차순으로 돈다.

**검증**
- 신규 ~35: 선호 4종 × (이동 목표·경계) · 규율 0/10000 난수 미소비 · `RollBelowFormationChance_FollowsPreference` / `RollAtFormationChance_FallsBack`(경계는 `<`) · 침투 유닛별 롤 · 도발 가중치 · 난수 호출 횟수·순서 단정 · 조우마다 재결정 · 해시 포함
- **동적 교착 검증**: `AllCowardAllies_BattleStillProgresses`, `AllFrontDowned_AnchorHolds`(D2 폴백), 유효 수치 격자 × 시드에서 "N틱 연속 앵커·사거리 변화 없음"이 발생하지 않음. 빈사 유닛이 앵커를 붙잡지 않음.
- 수동 시나리오: 침투한 적이 뒷열로 돌아가는 궤적, 비겁 성향이 물러서는 모습
- 커버리지 80%+ 측정

## Phase D — 개입 수단
**목표**: 사용자가 쓰는 다섯 가지가 모두 동작한다.

**변경 파일**
- 수정 `Scripts/Combat/`: `UnitState.cs`(구조 진행 틱·대상·소모품 잔량), `ActionResolver.cs`(구조 캐스팅·피격 초기화), `BattleCommand.cs`(`UseItem`), `BattleSimulation.cs`(자동 구조·앵커 정지 조건 3번·해시 갱신)
- 신규 데이터: `Resources/Data/consumables.json`, `ConsumableReader.cs`·`Dtos.cs`
- 수정 `Scripts/View/`: `UnitView.cs`(강제 발동 가능 시 빛남), `InterventionBar.cs`(소모품 버튼), `BattleHud.cs`

**결정**
- **구조 캐스팅**: `rescueTicks`(= `TicksPerSecond × 5`, 데이터 값). 규칙을 다음과 같이 못 박는다.
  - 시작한 틱이 진행 1 로 세어진다. 진행이 `rescueTicks` 에 **도달한 틱에 완료**되고, 완료 판정은 행동 단계 **뒤**에 둔다(같은 틱 피격이 완료보다 먼저 평가되도록).
  - 진행 중에는 **게이지를 충전하지 않고 행동도 하지 않는다**. `CanAct` 의 의미는 그대로 두고(D5), `ActionGaugeScheduler.Fill` 과 `IsReady` 가 구조 진행을 함께 본다. 완료 시 게이지는 기존 `rescueExtraGauge` 규칙을 따른다.
  - **피격(적 공격·폭주 유탄)이면 진행 0 으로 초기화**하고, 자해·흩어짐 피해는 초기화하지 않는다.
  - 구조자나 대상이 빈사·흩어짐이 되면 취소. 조우 전환·퇴각·제한 시간과 겹치면 취소.
  - 기존 **즉시 구조**를 전제한 테스트(`ScatterRescueTests`, `CommandQueueTests` 의 Rescue, `DeterminismTests` 명령 스크립트, `rescueExtraGauge`)의 영향 목록을 구현 착수 때 먼저 작성한다.
- **자동 구조**: 교전 중인 적이 없고 흩어진 아군이 있으면 후방 유지 선호 우선으로 시작하고, 완료까지 앵커가 전진하지 않는다(D2 조건 3).
- **빛남 조건**: 순수 함수 `UiText`/`BattleLayout` 수준의 `IsSkillReady(UnitSnapshot)` 로 두고 테스트한다. `CooldownRemaining == 0 && Condition == Normal` 만으로는 **캐스팅 중·전장 밖에서도 참**이 되므로 스냅샷에 캐스팅 표시를 넣어 함께 본다.
- **소모품**: 잔량은 로드 데이터가 아니라 **시뮬레이션이 소유**한다. 알 수 없는 ID·잔량 ≤ 0 이면 거부한다. 회복 계산은 `long` 으로 하고 최대 HP 로 제한한다. 파티 단위 대기열을 거쳐 후퇴 확인 직후에 처리한다(Phase A 틱 순서). 대상은 HP 비율 최저 아군이고 동률이면 낮은 ID, 유효 대상이 없으면 잔량을 보존한다.

**검증**
- 신규 ~25: `Hit_Tick49_ResetsProgress` · `NoHit_Tick50_Rescued` · `TargetDownedMidCast_Cancels` · `RescuerDownedMidCast_Cancels` · 같은 틱 피격·완료 순서 · 자동 구조가 전진을 막음 · 소모품 잔량 소진·거부 · `Casting_NotSkillReady` · 해시에 구조 진행·잔량 포함
- 수동 시나리오: 빛남이 강제 발동 가능 시에만 켜지는지, 소모품 버튼, 구조 중 피격
- 커버리지 80%+ 측정

## Phase E — 60초 밸런스와 심리스 다듬기 (재미 판정 2차)
**목표**: 한 판이 60초(600틱) 안에 끝나고 이동-전투 경계가 느껴지지 않는다.

**변경 파일**: `balance.json`·`stage_slice.json`(수치만), `LaneView.cs`·`UnitView.cs`(카메라 리드·걷기 보폭), 기획서 v0.7(1장 참고작·4-1 전투 진행·13장·15장), `docs/HANDOFF.md`

**결정**
- 시간 예산을 수치로 잡는다: 600틱 = 조우 4회 전투 + 행군 3회 + 여유. 행군 1회를 약 20~30틱(2~3초)으로 두면 행군 총합 60~90틱이고 전투에 약 500틱이 남는다. 구조 50틱은 이 예산을 크게 먹으므로 자동 구조가 잦으면 조우 수나 제한 시간을 데이터에서 조정한다.
- 기획서 4-1 의 "구조는 행동 게이지 소모" 서술을 5초 캐스팅으로 갱신한다.

**검증**: 폰 재미 판정 — 개입하고 싶은 순간 / 지루함 / 역할 차이가 보이는가 / 진행이 자연스러운가 / 한 판 길이. 성능 지표 재측정.

## Phase F — 조건부 (재미 판정 뒤)
- **F-1** 용어 개명(웨이브 → 조우): 데이터 키·코드·UI 일괄.
- **F-2** 월드 스페이스 이행: Phase B 판정 기준(누적 끊김 30회 초과 또는 행군 구간 창 최대 시간이 교전 구간의 두 배 이상)에 걸릴 때만.
- **F-3** 대상 선택 선호 5종 추가·치료·어그로 수치화·직업별 사거리.

## 범위 밖
Live2D, 픽셀아트, 사운드, 덱세팅 화면, 거점, 부활, 저장, 스토리·컷씬, iOS 빌드, 스토어 계정.

## Known Pitfalls
- Unity 에디터가 열려 있으면 batchmode 테스트가 프로젝트 잠금으로 실패 → 실행 전 종료.
- batchmode 라이선스 오류는 코드 문제가 아니라 토큰 만료 — 사용자가 Unity Hub 로그인 상태를 확인해야 한다.
- `.meta` 파일은 반드시 커밋.
- `PlayModeSmoke` 가 계층 경로를 하드코딩하므로 `Lane` 도입과 같은 커밋에서 고친다.
- 안드로이드 빌드는 URP 에셋·`m_BuildTargetBatching` 을 자동으로 다시 쓴다.
- `UnitSnapshot` 은 `int` 인수가 연속이라 새 필드를 중간에 끼우면 순서가 뒤바뀌어도 컴파일된다 → **끝에 추가**한다.

## 결정 로그
- 2026-09-29: 사용자가 "전투 진행이 생각과 다르다 — 횡스크롤 이동 전투"로 판단. 레퍼런스 블루 아카이브·트릭컬 리바이브. 수직 슬라이스는 폰 재미 판정 없이 종료.
- 2026-10-01 전문 검토 4건(arch·planner·perf·code-explorer): 위치를 규칙에 넣는 설계 **채택**, 월드 스페이스 이행 **보류**, "연출만" 안 **기각**(요구 3 이 규칙이므로).
- 2026-10-01 사용자 결정: 선호·진형유지 속성값 도입 / 과부하 수동 유지 / 구조 5초 캐스팅 / 소모품 간단히 / 제한 시간 60초 / 그림은 색 네모 유지.
- 2026-10-01 **1차 `/plan-review` (7 scope, PROCEED-WITH-FIXES — CRITICAL 0 · HIGH 9 · MEDIUM 17 · REVISE 5)** 반영:
  - "수치 전부 0" 장치가 생성자 불변식과 모순(arch·lang·security·test 동시 지적) → D11 `Disabled` 팩토리 + 데이터 경로는 항상 검증.
  - 아군 자동 기술이 수치와 무관하게 기존 테스트를 깸(test·lang·comments) → Phase A 에 영향 목록 작성을 명시. 사용자 결정으로 **빈도 기반**(D7)으로 바뀌어 쿨타임 의미도 분리.
  - 앵커 정지 주체 미정의(arch) → D2 에 정본 목록.
  - 사거리 선검사가 사용자 명령을 삼킴(arch) → D6 비파괴 조회·명령/게이지 유지.
  - 해시에 새 상태 누락(arch·test) → D10 Phase 별 덧붙임.
  - 데이터 검증 부재(security) → Phase A 검증 표 + `FieldCheck.Missing` 규약.
  - 확률 결정론 제어 부족(test) → D8 `Chance.Roll` 공용 가드 + 0/10000 미소비 테스트.
  - 구조 경계 미정의(test) → Phase D 결정에 피격 종류·취소 조건·경계 테스트.
  - 수치 정의 위치 없음(quality) → Phase A `LaneConfig` 표.
  - 용어집 미등재(quality·comments) → D14 표, Phase 0 에서 등재.
  - Row 값이 두 타입으로 쪼개짐(arch) → D4 `Formation` 단일 소유.
  - 선호 9종 과다(arch) → 사용자 결정으로 **4종**.
  - Phase A 가 규칙·화면 어긋난 상태를 커밋(arch) → 데이터 실값을 Phase B 로 이동.
  - `LaneConfig` 가 "매개변수 = JSON 키" 규약·리플렉션 테스트를 우회(arch) → `ReadLane` 접두 + 테스트 확장.
  - 성능 근거 오류(perf) → D12 근거 정정 + `Lane` 중첩 캔버스 + 측정 지표·임계값 확정.
  - 정수 함정·스폰 점프·난수 가드 복제(lang) → Phase A 결정에 반영.
  - 사실 오류 정정(comments): `Row` 는 피해 배율 **과 대상 선택** 양쪽에 쓰인다 / `LaneConfig` 는 **23번째** 인수 / 근거 없는 "웨이브 테스트 12개" 서술 삭제 / `HANDOFF.md`·`project_facts.md` 갱신을 **Phase 0** 으로 선행.
  - 커버리지 측정 단계 추가(test), 테스트 수 추정 상향(A ~55 · B ~14 · C ~35 · D ~25).
- 2026-10-01 **재리뷰 (arch·lang·security·test, PROCEED-WITH-FIXES — CRITICAL 0 · HIGH 3 · MEDIUM 11)** 반영. 1차 수정이 만든 새 구멍이 대부분이다:
  - HIGH: 자동/강제 `UseSkill` 을 구분할 수단이 구조에 없음(arch·lang·test 동시) → D7 에 `BattleCommand.IsForced`·`AutoPilot` 시그니처·`IsExecutable` 분기 명시, `BattleCommand.cs` 를 변경 파일에 추가.
  - HIGH: D2 조건 1 이 앞열 생존을 전제 → 앞열 전멸·전원 뒷열이면 교전 중에도 전진 → 기준 열 폴백 추가 + `AllFrontDowned_AnchorHolds`.
  - HIGH: 테스트 기본 `skillChance` 미명시 → 고정 난수를 쓰는 기존 테스트가 기술 롤에 값을 빼앗김 → 기본값 0 명시.
  - MEDIUM: D6 이 요구하는 `CommandQueue` 비파괴 조회가 변경 파일에 없음 → `TryPeek` 추가 + 질의·롤 순서 고정.
  - MEDIUM: `Disabled` 분기가 규칙 런타임에 영구 상주(죽은 경로) → D11 을 **정지 프리셋**으로 교체(분기 없음). 데이터의 "꺼짐 값" 모순도 함께 해소.
  - MEDIUM: D9 가 D7·기존 크리티컬과 순서 모순 → 조우 단위 결정(ID 순)과 행동 중 판정(행동 순서)을 분리 기술.
  - MEDIUM: 조우 단위 상태를 자세·침투 둘로 정리하고 해시에 포함. D10 범위를 "이번 추가분 + 대기 명령·후퇴 요청"으로 한정.
  - MEDIUM: 소모품이 유닛 1슬롯 큐를 덮어씀 → 파티 단위 대기열로 분리, 틱 위치(후퇴 직후) 고정, 알 수 없는 ID 거부·`long` 회복 계산.
  - MEDIUM: 사거리 대기 중 게이지가 2×Max 까지 쌓여 접촉 직후 2연타 → 대기 중 상한 1×Max.
  - MEDIUM: 구조 규칙(시작 틱 카운트·완료 판정 위치·캐스팅 중 게이지·`CanAct` 충돌)과 기존 즉시 구조 테스트 영향 목록 → Phase D 결정에 명시.
  - MEDIUM: Phase C 새 필드 검증 행·소모품/구조 수치 범위·파일 간 불변식 위치 → Phase A 검증 표 확장(`preference` 는 `int`+`Missing` 으로 읽는다).
  - MEDIUM: Phase B 에 실값 교착·배속 결정론 테스트 추가, Phase C 수동 시나리오 추가, Phase 0 검증을 grep 으로 구체화.
- 2026-10-02 **Phase A 구현 중 결정**(계획에 없던 것만):
  - 뒷열 사거리 불변식을 실제 기하에 맞춰 **교체**했다. 옛 식(`frontSlotOffsetFu + backSlotOffsetFu + engageMarginFu`)은 앞열이 멈춰 서는 거리를 빼먹어 앞열 사거리가 긴 구성에서 뒷열이 영원히 못 때리는 설정을 통과시켰다. 두 식은 포함 관계가 아니다(`frontReachFu < 2 × engageMarginFu` 면 옛 식이 더 엄격하다) — 새 식이 기하를 그대로 옮긴 것이므로 옛 식은 버린다.
  - 조우별 접근 거리는 `BattleSetup` 이 들고 간다(`WaveApproachFu`). 적 진형 기준점이 그 누적 합이라 규칙 모듈이 알아야 하고, 스테이지 데이터에서 오는 값이라 밸런스에 둘 수 없다.
  - `approachFu` 는 **Phase A 에서** `stage_slice.json` 에 최소값 1 로 넣는다. 리더가 필수로 요구하는 필드를 Phase B 까지 비워 두면 그 사이 실행이 로드 실패로 죽는다. 조우 4개·실값·60초는 계획대로 Phase B.
  - 규칙 모듈에 `AssemblyInfo.cs` 를 두어 `TAoB.Combat.Tests` 에만 internal 을 연다. 대안인 "검증할 타입을 public" 은 표시 계층에까지 내부 규칙을 노출해 한 방향 참조 경계가 흐려진다.
  - `characters.json` 의 기술 빈도는 **위력의 역순**으로 두고 광역(`All`) 기술은 한 단계 더 낮춘다 — 단일 1300~2000 에 3400·3200·2600·2200·1400, 광역 1500·1800 에 1200·800. 산식 없는 값이 흩어지면 Phase E 밸런스에서 무엇을 움직여야 하는지 알 수 없다.
- 2026-10-06 **Phase B 구현 중 결정**(계획에 없던 것만):
  - 사전 조건을 `NoAllyDowned` → `None`. 수직 슬라이스가 그 규칙을 보려고 둔 값인데, 조우 4회에서는 보스 직전에 한 명이 쓰러지며 **다섯 시드 모두 25초쯤 패배**해 보스를 볼 수 없었다(측정). 규칙 자체는 규칙 모듈 테스트가 계속 덮는다.
  - 조우 4회에 적 캐릭터 2명(104·105)을 추가했다 — 같은 캐릭터를 두 조우에 둘 수 없다.
  - 조우 배너를 쓰지 않는다. 이동-전투 흐름(D1)을 배너가 끊고 같은 사실이 상시 카운터에 있다. 소비처가 없어진 `UiText.WaveBanner` 도 지웠다.
  - 하늘·바닥은 단색이라 흐를 것이 없어 전장 층에 고정하고, 스크롤 층 안에는 바닥 표식(1배)과 먼 층(0.45배)만 둔다.
  - 성능 기준값은 같은 빌드 안에서 교전 구간과 행군 구간을 비교하는 방식으로 바꿨다(성능 표시가 Phase B 에서 새로 생겨 옛 APK 로 같은 지표를 잴 수 없다). F-2 판정 기준을 p95 에서 표시 도구로 읽을 수 있는 값(누적 끊김 30회 초과 / 행군 구간 창 최대 시간이 교전 구간의 두 배 이상)으로 바꿨다.
- 2026-10-06 **`/plan-review --code` Phase B (코드 6 scope + 재리뷰 2)** 반영:
  - **HIGH(5개 리뷰어가 독립적으로 지적)**: 띠 묶음을 진형 기준점(화면 −500)에 맞춰 펼쳐 화면 오른쪽 20~259px 가 늘 비고 띠가 화면 안에서 튀어나왔다. 이를 통과시키던 테스트 공식(`간격 × 개수 ≥ 폭 + 2×치우침`)은 묶음 폭을 한 칸 과대평가했다 → 묶음을 **화면 중앙** 기준으로 펼치고, 합성식(`LaneOffsetX`·`CenterLocalX`·`TileLocalXAt`)을 `LaneScroll` 에 올려 `LaneView` 와 테스트가 같은 함수를 쓴다. 옛 구성이 실제로 가장자리를 못 덮는다는 것도 테스트로 고정했다.
  - lang HIGH 가설(중첩 캔버스가 HUD·팝업 위로 올라온다)은 **반증**했다 — 결과 화면 캡처에서 딤이 유닛과 버튼을 모두 덮는다. `overrideSorting = false` 는 기본값이라 코드에서 지웠다.
  - 시작 좌표 조회 실패를 0 으로 덮던 것 → 예외. 띠 치수를 `BattleLayout` 으로 통합. `RealStageFlowTests` 는 시드 1 승리 단언을 버리고 시드 1~5 로 "제한 시간 만료 아님"만 본다(난이도는 Phase E 소관). 거짓이 된 주석 6건 정정.
  - 연기: 표시 계층(`LaneView`·`UnitView` 좌표 적용)의 자동 테스트는 프로젝트 전제(표시 계층 = 수동 시나리오 + 스모크)에 따라 두지 않는다. 대신 합성식을 순수 함수로 올려 검증 가능한 부분을 넓혔다.
- 2026-10-02 **`/plan-review --code` (11 리뷰: 코드 7 scope + 자산 2 + 문서 2 — CRITICAL 0 · HIGH 3 · MEDIUM 11 · LOW 9)** 반영:
  - HIGH: `ActionResolver.RollCritical` 이 `Chance.Roll` 과 같은 가드를 복제해 D8 "단일 진입점"이 거짓이었다(arch·quality·lang·comments 4중 지적) → `Chance.Roll` 호출로 교체.
  - HIGH: 교전 창(`사거리 − 여유`)이 한 걸음보다 좁으면 정지선이 영영 안 걸려 적을 지나쳐 행군한다. 뒷열은 `backReachFu ≤ engageMarginFu` 도 통과했다 → 두 열 모두 창 ≥ 한 걸음 불변식 추가.
  - HIGH: 난수 소비 순서와 `BattleSetup.ValidateApproach` 에 검증이 없었다 → `OutOfReachTicks_ConsumeNoRandom`·`PendingForcedCommand_SkipsTheFrequencyRoll`·접근 거리 6개 테스트 추가.
  - MEDIUM: 상대가 사거리 밖으로 바뀐 틱에 모아둔 초과 게이지가 몰수돼 D6 "게이지 유지"가 깨졌다 → 상한을 충전분에만 적용한다.
  - MEDIUM: Phase C 전용 수치 6개와 `Lane.Clamp` 가 소비처 없이 들어와 있었다 → 지우고 Phase C 로 미뤘다(위 수치 표).
  - MEDIUM: 사거리 밖에서 실행 불가가 된 구조·과부하 명령이 관문을 통과해 폴백 기본 공격이 사거리 밖을 때릴 수 있었다 → 관문이 실행 가능 여부까지 본다.
  - MEDIUM: 큐가 `IsForced` 거짓인 명령을 받으면 강제 발동 쿨타임을 우회할 수 있었다 → `Enqueue` 가 거른다.
  - MEDIUM: `OnlyPosXDiffers_HashDiffers`·`MovingFormation_ChangesTheOutcome` 가 이름과 달리 아무것도 분리하지 못했다 → 자리 간격만 다른 쌍, 이벤트 해시·전투 길이 비교로 교체.
  - MEDIUM: 여유 0 픽스처가 `IsEngaged`/`CanReach` 를 구분하지 못하고 전원 뒷열 폴백 테스트가 없었다 → 두 테스트 추가.
  - MEDIUM: 데이터 경계(빈도 대역 양끝·접근 거리 0/상한/초과·시간 예산 경계)가 비어 있었다 → 5개 추가.
  - LOW·주석: 범위 상한을 `LaneConfig` 상수로 모으고, 거짓이 된 주석 6건(빈사와 기준점, `IsForced` 검사 위치, 해시 순회, 테스트 요약)을 고쳤다. 완료된 수직 슬라이스 계획서를 가리키는 주석 51곳에 `수직 슬라이스` 접두를 붙여 활성 계획서와 구분했다(커밋 `62bff36`). 호칭 규약은 `project_facts.md` §0 에 등재.
