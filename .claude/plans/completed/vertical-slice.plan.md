# 첫 수직 슬라이스(전투 프로토타입) 착수 계획

**Review Scope**: arch, lang, quality, security, perf, test, comments
**Status**: 진행 중 — Phase 0 완료 (2026-09-11: Unity 6000.3.24f1 + Android 모듈 설치, `Game/` Universal 2D 프로젝트 생성, batchmode 열기 검증 exit 0, 캐시 폴더 git 제외 확인) · Phase 1 완료 (1차 `/plan-review` 와 첫 커밋 전 `/plan-review --code` 모두 PROCEED-WITH-FIXES, 지적 전부 결정에 반영) · Phase 2 완료 (2026-09-11, 커밋 8681f3b — 테스트 148개 통과·커버리지 98.9%, `/plan-review --code` 재리뷰 후 PASS) · Phase 3 완료 (2026-09-14, 커밋 c07139e — 테스트 190개 통과, `TAoB.Data` 커버리지 98.3%, `/plan-review --code` 재리뷰 후 PASS) · Phase 4 구현 완료 (2026-09-15 — 커밋 전 리뷰 지적 반영, 테스트 214개 통과, 플레이 스모크 오류 0건, 커밋 5250269) · Phase 4.5 연출 보강 구현 완료 (2026-09-15 계획, 2026-09-17 구현 — 에디터가 사용자 PC 에서 너무 느려 시연을 빌드로 전환. 테스트 226개 통과·플레이 스모크 오류 0건, 커밋 전 `/plan-review --code` PROCEED-WITH-FIXES 반영 완료, 커밋 승인 대기) · Phase 5 빌드 도구 구현 완료 (2026-09-17 — Windows 163MB·APK 45MB 빌드 성공, 스모크 재시작 누수 검사 통과, 커밋 전 `/plan-review --code` 반영 완료, 커밋 승인 대기) · 다음 = 사용자 폰 설치·재미 판정

## Context
기획서 v0.5·설정집 v1.1 정리 후 사용자가 "제작 착수 가능한가"를 물음. 결론: 게임 전체가 아니라 **전투 1판짜리 수직 슬라이스**로 재미("자동 전투를 보다가 개입하는 게 재밌나")를 먼저 검증한다. 동료 100·마도구 200·거점 설계는 검증 후로 미룬다.

사용자 결정(2026-09-10):
- 엔진 **Unity 확정**, 화면 **가로**, 슬라이스 범위 **전투만**
- 리뷰 방식: CLAUDE.md 워크플로(`/plan-review`)를 정식으로 쓰기 위해 **참조 문서부터 구축**

현재 상태(2026-09-10):
- git 저장소 초기화됨(커밋 전). `.gitignore`, 기획서 v0.6, `docs/references/` 9종 작성 완료.
- Unity 설치·`Game/` 프로젝트 생성 완료(2026-09-11) — Phase 0 결정 참조.
- `.claude/` 와 `CLAUDE.md` 는 다른 iOS 프로젝트에서 복사된 것. 참조 문서는 새로 작성했고 SKILL.md 는 에이전트 이름만 고쳤다.
- Unity 최신 LTS = **6.3 LTS**(2027-12까지 지원).

## Phase 0 — 환경 준비
**목표**: Unity 6.3 LTS와 git 저장소가 준비된 상태.

**변경 파일**
- `.gitignore` (신규), `Game/` (Unity 프로젝트, 신규)
- `The_Age_of_Bastards_게임기획서_v0.6.md` (v0.5 복사 후 갱신)

**결정**
- 설치(2026-09-11, 사용자 다운로드 허가·약관 동의 후 Claude 수행): winget 으로 Unity Hub → Hub 명령줄로 에디터·Android 모듈 설치. 경로·버전·모듈 ID·Hub 명령줄 사용법의 정본은 `docs/references/project_facts.md` §0.
- 사용자 수행: Unity 계정 로그인과 Personal 라이선스 활성화(비밀번호 입력은 Claude 불가).
- Claude 수행: `git init`, Unity 표준 `.gitignore`(+ `*.keystore`, `*.jks`, `fastlane/.env`), `Unity.exe -batchmode -quit -nographics -createProject Game -cloneFromTemplate <에디터>\Data\Resources\PackageManager\ProjectTemplates\com.unity.template.2d-cross-platform-2d-6.1.6.tgz` 로 프로젝트 생성. 이 파일의 패키지 이름은 `com.unity.template.universal-2d`(Universal 2D, URP 2D Renderer, test-framework·ugui 포함)이다. 파일명에 "universal" 이 없어 처음엔 템플릿 없이 빈 프로젝트가 생겼고, 그 폴더는 삭제 대신 스크래치 폴더로 옮겨 두었다.
- 기획서 v0.6: 10장 Unity 확정, 7장 조작 방식 "가로 고정", 13장 첫 수직 슬라이스 = 이 계획의 범위, 15장 미결 5번 삭제.
- 초기 커밋은 메시지 제시 → 사용자 승인 후 실행.

**검증**
- `Unity.exe -batchmode -quit -projectPath Game -logFile -` 가 에러 없이 종료. ✅ 완료(2026-09-11, exit 0)
- `git status` 에 `Library/`, `Temp/` 가 잡히지 않음. ✅ 완료(2026-09-11)

## Phase 1 — 리뷰 하네스(참조 문서) 구축
**목표**: `/plan-review` 가 이 프로젝트에서 BLOCKED 없이 실행된다.

**변경 파일** (`docs/references/`)
- `README.md` — 문서 체계 원칙(P1~P6, P5 = 스킬·코어 문서에 언어/엔진 이름 금지)
- `project_facts.md` — §0 스택 사실, §0-A 활성 스택 표, §0-B 도메인 용어집
- `stacks/unity_csharp.md` — 스택 문서
- 코어 6종: `software_architecture.md`, `code_quality.md`, `security_patterns.md`, `performance_patterns.md`, `testing_patterns.md`, `comment_standards.md`
- `.claude/skills/plan-review/SKILL.md` — 레지스트리 `subagent_type` 열만

**결정**
- **코어 6종 형식** (SKILL.md 커버리지 규칙 준수): 언어 중립 논리명으로 서술 / 판정 절에 번호 + `- **Flag:**` 불릿 + 심각도(CRITICAL~LOW) / 번호 붙은 "출력 형식" 절 / 전제 게이트는 스택 문서 §0 전제 값을 참조. 판정 절 중심으로 짧게 쓴다.
- **게임 특화 판정을 코어에 포함**:
  - arch: 규칙 계산(순수)과 표시 계층 분리, 엔진 비의존 로직 모듈 경계, 명령 큐를 통한 입력
  - quality: 밸런스 수치 하드코딩 금지(데이터 파일), 매직 넘버
  - perf: 프레임당 메모리 할당, 매 프레임 탐색 호출, 모바일 드로우 콜
  - test: 결정론(고정 틱·시드 난수) 검증, 로직 모듈 커버리지 80%
  - security: 서명 키·스토어 자격증명 커밋 금지, 외부 데이터 파일 검증(단일 플레이 프리미엄이라 네트워크 항목은 전제 꺼짐)
  - comments: 기획서 절 참조 주석(예: `// GDD 4-1`)의 부패 검사
- **스택 문서 `unity_csharp.md`**: §0 전제 표 / §1~§6 코어 논리명 → Unity/C# 표기 대응표 / §7 lang 판정 / §8 scope 추론 신호. 전제 값의 정본은 스택 문서 §0 이며 이 계획에 중복 기재하지 않는다.
- **§0-A 버킷** (first-match):
  | glob | 스택 ID | 스택 문서 | lang 에이전트 |
  |---|---|---|---|
  | `Game/Assets/**/*.cs` | unity-csharp | `stacks/unity_csharp.md` | `csharp-reviewer` |
  | `Game/Assets/**/*.{asmdef,json,unity,prefab,asset,meta}` · `Game/ProjectSettings/**` · `Game/Packages/**` | unity-assets | `stacks/unity_csharp.md` | — |
  | `*.md` · `docs/**` · `.claude/**` · `.gitignore` | docs | `stacks/unity_csharp.md` | — |
  | 그 외 | (미분류) | `stacks/unity_csharp.md` | — |
  - 모든 버킷에 스택 문서를 지정한다. 코어 scope 리뷰어가 §0 전제 표 없이는 전제 게이트를 판정할 수 없어 BLOCKED 되기 때문.
- **에이전트 이름**: SKILL.md 레지스트리는 접두어 없는 이름(`architect`, `code-reviewer`, `security-reviewer`, `performance-optimizer`, `tdd-guide`, `comment-analyzer`)을 쓴다.
- CLAUDE.md 는 수정하지 않는다.

**검증**
- `/plan-review .claude/plans/vertical-slice.plan.md` 실행 → 하네스 무결성 BLOCKED 0건. ✅ 1차 실행 완료(DEGRADED 1건은 스택 문서 수정으로 해소).
- SKILL.md Step 4 grep 으로 각 참조 문서 번호 집합 = 리뷰어 판별 ∪ 제외. ✅ 7개 문서 모두 일치.

## Phase 2 — 전투 로직 코어 + 테스트 (TDD)
**목표**: 화면 없이 전투 1판이 시뮬레이션되고 EditMode 테스트로 검증된다.

**변경 파일** (`Game/Assets/_Project/`)
- `Scripts/Combat/TAoB.Combat.asmdef` (`noEngineReferences: true`)
- `Scripts/Combat/` — `Stats`, `UnitState`, `BattleSnapshot`/`UnitSnapshot`, `BattleEvent`, `SkillDefinition`, `Catalyst`(원석), `Magitool`(마도구), `EquipRules`, `DamageCalculator`, `AffinityTable`, `Formation`, `ActionGaugeScheduler`, `OverloadResolver`, `AutoPilot`, `BattleCommand`, `CommandQueue`, `BattleOutcomeRules`, `BalanceConfig`, `BattleSimulation`, `BattleResult`, `StateHasher`, `IRandom`/`SeededRandom`
- `Tests/EditMode/Combat/TAoB.Combat.Tests.asmdef` + 규칙별 테스트 파일

**결정**
- **고정 틱**: 초당 10틱. `TicksPerSecond` 는 규칙 모듈 상수이며 밸런스 값이 아니므로 데이터 파일에 두지 않는다. 제한 시간·흩어짐 HP 감소 등 시간 규칙은 모두 틱 단위로 계산한다.
- **정수 연산**: 규칙 모듈은 `float` 를 쓰지 않는다. 게이지·HP·스탯은 정수, 배율은 ×1000 정수(1.25 → 1250), 확률은 만분율(0~10000). 나눗셈은 정해진 순서로 버림한다. 에디터와 기기에서 같은 결과를 내기 위함.
- **시드 난수**: 규칙 모듈은 `IRandom` 만 주입받는다. `SeededRandom` 은 자체 정수 알고리즘(xorshift 계열)으로 구현하고 엔진·표준 라이브러리 난수를 쓰지 않는다.
- **밸런스 수치 주입**: K, 상성 배율, 진형 배율, 크리티컬 확률·배율, RES 자해 계수, 장착 불가 임계, MND 효과 감소 계수, 흩어짐 틱당 HP 감소, 과부하 확률표, 구조 비용은 전부 `BalanceConfig` 로 받는다. 규칙 코드에 기본값 리터럴을 두지 않고, 테스트는 테스트용 `BalanceConfig` 를 직접 만든다. Phase 3 에서 `balance.json` 으로 채운다.
- **상태와 노출**: `UnitState` 는 `sealed class` 로 전투 시작 시 한 번 생성하고(유닛 10명 이하), `BattleSimulation` 내부에서만 갱신한다. 규칙 상태 전체를 틱마다 복제하지 않는다(표시용 스냅샷 값 복사는 아래 계약).
- **스냅샷·이벤트 계약**:
  - 틱 1회 = `Step(List<BattleEvent> buffer)` 1회. 시뮬레이션은 호출자가 넘긴 버퍼를 비우고 그 틱의 이벤트를 채운다(틱당 할당 없음). `BattleEvent` 는 `struct`(종류·주체·대상·수치).
  - `FillSnapshot(BattleSnapshot target)` 은 호출자 소유 스냅샷 버퍼에 현재 값을 값 복사한다(유닛 10명 × 필드 몇 개, 할당 없음). 표시 계층은 직전·현재 두 버퍼를 번갈아 써서 보간한다.
  - 표시에 필요한 계산값(HP, 게이지, 상태 = 정상/흩어짐/빈사, 과부하 위험 단계 0~3, 장비 내구력, 기술 재사용 대기)은 Combat 이 스냅샷에 담고, 피해·자해량·과부하 결과는 이벤트로 낸다. 표시 계층은 규칙을 다시 계산하지 않는다.
- **명령 경로 단일화**: `BattleCommand` 종류 = `BasicAttack(유닛)` · `UseSkill(유닛)` · `Overload(유닛)` · `Rescue(구조자, 대상)` · `Retreat`. 사용자 명령은 `CommandQueue` 를 거쳐 틱 경계에서 처리하고, 대기 명령이 없으면 자동 판단이 그 틱에 명령을 만들어 같은 실행 경로(`ActionResolver`)로 처리한다.
  - 자동 판단: 게이지가 찬 유닛에 대기 명령이 없으면 Combat 소속 `AutoPilot` 이 명령을 만든다.
    - 아군: `BasicAttack` 만 만든다. 고유 기술·과부하·구조는 사용자 입력 전용이다(개입의 가치를 선명하게 검증하기 위한 제안 — 사용자 조정 가능).
    - 적·보스: 기술 재사용 대기가 끝났으면 `UseSkill`, 아니면 `BasicAttack` 을 만든다. 적은 `Overload`·`Rescue` 를 쓰지 않는다.
  - 수락·대기: `UseSkill`·`Overload`·`Rescue` 는 유닛당 대기 명령 1개. 게이지가 차지 않았으면 대기했다가 다음 충전 틱에 실행하고, 새 명령이 오면 기존 대기 명령을 대체한다. 빈사·흩어짐 상태 유닛의 명령과 재사용 대기 중인 기술 명령은 거부한다.
  - `Retreat` 는 대기 없이 다음 틱 경계에서 처리한다.
  - 배속은 명령이 아니다(Phase 4 틱 드라이버 참조).
- **구현 규칙** (기획서 v0.6 4-1 중 아래만 구현, 나머지는 "범위 밖"):
  - 스탯 6종 ATK/DEF/HP/SPD/MND/RES
  - `최종 데미지 = (ATK + 장비ATK) × 상성 × 진형 × (1 − DEF/(DEF+K)) × 크리티컬`
  - 상성 3단계, 진형 앞열·뒷열 2줄
  - 마도구 2축: RES 미달 → 사용 시 부족분 비례 자해, 임계 이하 → 장착 불가 / MND 미달 → 효과 배율 감소
  - 원석(내구력 없음, MND 상한) vs 마도구(전투당 내구력 소모)
  - HP 0 → 빈사(전투 중 행동 불가, 결과에 집계 — 부활은 슬라이스 밖). 흩어진 채 승패가 정해지면 그 틱에 빈사로 확정하고 승패를 다시 판정(사용자 결정 2026-09-11)
  - 과부하: 성공 / 흩어짐(행동 불가 + 틱당 HP 감소, `Rescue` 로 해제) / 폭주(무작위 대상 피해 + 자해) / 한계돌파(성공과 같은 피해 + MND 상한 상승, 사용자 결정 2026-09-11)
- **고유 기술**: 캐릭터당 `SkillDefinition` 1개(피해 배율, 대상 규칙 = 앞열 우선·최저 HP·전체 중 하나, 과부하 가능 여부, 재사용 대기 틱). 값은 데이터 파일 필드로 받는다.
- **승패 판정** (`BattleOutcomeRules`, 한 틱에 여러 조건이 성립하면 위에서부터 우선 — 순서는 제안, 사용자 조정 가능):
  | 순위 | 결과 | 조건 |
  |---|---|---|
  | 1 | 패배(수동 후퇴) | `Retreat` 명령을 처리한 틱 |
  | 2 | 패배(조건 위반) | `Precondition` 이 깨진 틱 |
  | 3 | 승리 | 마지막 웨이브까지 보스 포함 모든 적이 빈사 |
  | 4 | 패배(전멸) | 아군 전원 빈사 |
  | 5 | 패배(자동 후퇴) | 제한 시간 틱에 도달했는데 승리하지 못함 |
- `BattleResult` 에 결과 종류·빈사자·내구력 소모와 함께 최종 상태의 정수 해시를 담는다(Phase 5 기기 비교용). 해시는 `StateHasher` 가 FNV-1a 64비트로 계산하며, 실행마다 값이 달라질 수 있는 `GetHashCode`·`HashCode` 는 쓰지 않는다.
- 임시 직업 3종(기획서 4-4 확정 전 가칭): 기사(앞열·RES), 마법사(뒷열·MND·과부하), 초감각 인간(SPD·크리티컬).

**검증**
- `project_facts.md` §0 "테스트 실행" 명령으로 EditMode 테스트 전부 통과(에디터 종료 상태). ✅ 완료(2026-09-11)
- 필수 테스트 케이스:
  - 결정론: 같은 시드 + 같은 틱별 명령 스크립트로 2회 실행 → `BattleResult` 동일
  - 배속 무관: 같은 명령 스크립트를 1틱씩 호출 vs 여러 틱 묶어 호출 → `BattleResult` 동일
  - 경계값: RES 요구치와 같음 / 1 부족 / 장착 불가 임계, MND 요구치와 같음 / 부족, HP 정확히 0, 제한 시간 1틱 전(미판정)·도달 틱(자동 후퇴)
  - 과부하: 고정 수열 `IRandom` 으로 성공·흩어짐·폭주·한계돌파를 각각 강제 + 흩어짐 `Rescue` 해제 / 미해제 시 빈사
  - 승패 표의 각 행 + 같은 틱 동시 성립 시 우선순위
  - `AutoPilot`: 아군은 `BasicAttack` 만 만듦 / 적은 게이지가 찬 틱에 재사용 대기가 끝나 있으면 `UseSkill`, 아니면 `BasicAttack` / 적은 `Overload`·`Rescue` 를 만들지 않음
  - 공식: `DamageCalculator` 의 상성·진형·크리티컬·방어 감소 결합값, `SkillDefinition` 대상 규칙 3종 각각, 마도구 사용 시 내구력 감소와 0 일 때 효과 상실, 원석 출력이 MND 상한에서 잘림
  - 명령: 게이지 미충전 시 대기 후 충전 틱에 실행, 새 명령이 대기 명령을 대체, 빈사·흩어짐 유닛 명령 거부, 재사용 대기 중 `UseSkill` 거부, 재사용 대기 만료 1틱 전 거부·만료 틱 수락
  - `StateHasher`: 같은 상태 → 같은 해시, 정수 필드 하나만 달라도 다른 해시
- 커버리지 도구로 `TAoB.Combat` 라인 커버리지 80% 이상. ✅ 98.9%(731/739줄, 메서드 97.6%) — 테스트 148개 전부 통과(재리뷰 반영 후 재측정)
- `/plan-review --code` 후 커밋(메시지 승인).

## Phase 3 — 밸런스 데이터 외부화
**목표**: 수치를 코드 수정 없이 텍스트 파일로 바꿀 수 있다.

**변경 파일**
- `Game/Assets/_Project/Resources/Data/` — `balance.json`(`BalanceConfig` 전 항목 + 상성표), `characters.json`(아군 3·적 3·보스 1, 이름·스탯·직업·진형·`SkillDefinition`·장비 ID), `equipment.json`(원석 2 + 마도구 5, 이름 포함), `stage_slice.json`(제한 시간(초)·`Precondition`·아군 ID·웨이브 2개)
- `Game/Assets/_Project/Scripts/Data/` — `TAoB.Data.asmdef`, `Dtos`, `FieldCheck`, `BalanceReader`·`EquipmentReader`·`CharacterReader`·`StageReader`, `GameData`(`CharacterData`·`EquipmentCatalog`·`StageData`), `GameDataLoader`(`DataFiles`·`GameDataFiles`), `GameDataException`
- `Game/Assets/_Project/Tests/EditMode/Data/` — `TAoB.Data.Tests.asmdef`, `JsonUtilityBehaviourTests`, `DataValidationTests`, `ResourceFilesTests`, `TestSupport/TestJson`

**결정**
- `JsonUtility` 호환 구조(딕셔너리 대신 배열, 최상위는 항상 객체). 열거형은 이름 문자열(`"Knight"`)로 쓰고 로더가 대소문자까지 정확히 대조한다.
- 로더는 별도 `TAoB.Data` 어셈블리에 둔다(참조 방향 View → Data → Combat). DTO 를 Combat 자체 타입(`BalanceConfig` 등)으로 변환해 넘긴다.
- **파일 접근과 해석 분리**: `GameDataLoader.LoadFromResources()` 는 네 파일 본문을 `GameDataFiles` 로 묶어 `Parse()` 에 넘긴다. 검증 테스트는 문자열만으로 `Parse()` 를 돌리고, 실제 파일은 별도 스모크 테스트가 로드한다.
- **로드 시점 검증** — 하나라도 실패하면 `GameDataException(파일명, 필드 경로, 사유)` 으로 중단한다(예: `characters.json — characters[2].stats.hp: 필드가 없습니다`):
  - 필수 필드 누락: 정수 DTO 필드 초기값을 -1 로 두고 검사. `JsonUtility` 는 배열 요소 안에서도 빠진 필드의 초기값을 유지한다(확인 완료, `JsonUtilityBehaviourTests` 가 고정). 빠진 객체는 초기값이 채워진 새 인스턴스, 빠진 배열·문자열은 null.
  - `bool` 은 누락을 구분할 수 없으므로 **생략 = false** 가 파일 규칙(`isBoss`, `canOverload`). 장비 ID 문자열은 비어 있거나 빠지면 "장비 없음".
  - 음수·0 불가 값, 존재하지 않는 ID 참조(장비 ID 는 문자열, 캐릭터 ID 는 정수), ID 중복, 진영 불일치, 같은 캐릭터 이중 배치, RES 부족으로 장착 불가.
  - 상한 초과: 제한 시간 ≤ `BattleSetup.MaxTimeLimitTicks`(600초), 스탯·장비 수치 ≤ `CombatScale.MaxValue`, 배율 ≤ `CombatScale.MaxPermille`, 확률 0~10000, 과부하 확률표 합 = 10000 — 로더는 같은 상수로 먼저 걸러 파일명·필드명을 붙인다. **밸런스 21개 값의 범위는 `BalanceConfig` 생성자만 안다**: 로더는 누락만 검사하고, 생성자가 던진 예외의 매개변수 이름(= JSON 키)을 필드명으로 옮긴다(하한을 두 곳에 적지 않기 위함).
  - 배열 크기 상한(로더 상수, 밸런스 값 아님): 아군·웨이브·웨이브당 적 ≤ 5(`StageReader`), 캐릭터 ≤ 50(`CharacterReader`), 원석·마도구 ≤ 50(`EquipmentReader`), 과부하 구간 ≤ 10, 상성표 ≤ 직업 수²(`BalanceReader`).
- **과부하 확률표 표기**: 마지막 구간은 상한이 없으므로 `maxExcessPermille` 을 쓰지 않는다(쓰면 오류). 나머지 구간은 오름차순.

**검증**
- 로더 EditMode 테스트: 정상 파일 로드 1 + 필드 누락·음수·미존재 ID 각 1 + 상한 규칙 5종(제한 시간, 웨이브 수, 웨이브당 적 수, 확률 범위, 과부하 확률표 합) 각 1. ✅ 42개(실제 파일 로드 + 실제 편성으로 입력 없이 전투가 끝나는 스모크 포함), `TAoB.Data` 라인 커버리지 98.3%
- 사용자가 `balance.json` 의 K 값을 바꾸고 재생했을 때 데미지 숫자 변화 확인. → 화면이 필요하므로 Phase 4 시나리오에서 수행.

## Phase 4 — 전투 화면
**목표**: 가로 화면에서 전투를 보고 터치로 개입할 수 있다.

**변경 파일**
- `Game/Assets/_Project/Scripts/View/TAoB.View.asmdef` — `BattleBootstrap`, `UnitView`, `BattleHud`, `InterventionBar`, `ResultPanel`, `DebugLoadoutPanel`, `PopupPool`, `UiFactory`(uGUI 생성), `TickDriver`, `BattleLayout`, `UiText`, `RescueSelector`
- `Game/Assets/_Project/Editor/TAoB.EditorTools.asmdef` — `ProjectSetupTools`(TMP 리소스 가져오기·전투 씬 생성), `PlayModeSmoke`(배치모드 플레이 스모크)
- `Game/Assets/_Project/Tests/EditMode/View/TAoB.View.Tests.asmdef` — 틱 드라이버·배치·문구 테스트(엔진 비의존 부분만)
- `Game/Assets/_Project/Scenes/Battle.unity` (오브젝트는 `BattleBootstrap` 1개, 씬 생성 도구가 만든다)
- `Game/Assets/_Project/Fonts/` — `NanumGothic-Regular.ttf` + `OFL.txt`
- `Game/Assets/TextMesh Pro/` — TMP 필수 리소스(설정·기본 폰트·셰이더, 도구로 가져옴)
- `Game/ProjectSettings/EditorBuildSettings.asset` — `Battle.unity` 를 유일한 빌드 씬으로 등록(씬 생성 도구가 목록을 대체한다)
- `Scripts/Data/GameData.cs` — `CharacterData.WithEquipment`, `CreateBattleSetup(교체 목록)`, `ResolveCharacter`(개발용 장비 교체 경로)
- `Scripts/Combat/OverloadResolver.cs` — `CanOverload(원석, 기술)` 공개(과부하 가능 판정의 정본을 표시 계층과 공유)

**결정**
- 씬에는 `BattleBootstrap` 만 두고, 전투 표시 객체는 코드로 하나의 `BattleRoot` 아래에 생성한다(`PopupPool` 만 예외로 캔버스 직속 = `BattleRoot` 형제) → 에디터 수작업·씬 YAML 충돌 최소화.
- **틱 드라이버**: `BattleBootstrap.Update` 가 누적 시간 × 10 × 배속만큼 `Step()` 을 호출한다. 프레임당 최대 20틱, 넘치는 틱은 다음 프레임으로 넘긴다. 배속은 호출 횟수만 바꾸고 규칙 모듈에 전달하지 않는다.
- **이벤트 전달**: `Step()` 1회가 끝날 때마다 `BattleBootstrap` 이 이벤트 버퍼를 곧바로 뷰들에 직접 호출로 넘긴 뒤 다음 `Step()` 을 부른다. 한 프레임에 여러 틱을 돌려도 이벤트가 덮여 사라지지 않는다. 전투 이벤트에는 구독(발행자) 구조를 두지 않는다.
- **표시 갱신**: 스냅샷 값이 바뀐 요소만 텍스트를 갱신한다(TMP `SetText` 수치 오버로드). 데미지 숫자·이펙트는 `PopupPool` 로 재사용한다.
- **재시작**: `BattleRoot` 를 파괴하고 `BattleSimulation` 을 새로 만든다. `PopupPool` 은 파괴하지 않고 전부 회수한다. 버튼 리스너 등 이벤트 구독은 `BattleRoot` 하위 객체만 하고 각자의 `OnDestroy` 에서 해제한다.
- 그래픽은 색 네모 + 직업 글자. HP 바·행동 게이지 바·데미지 숫자·과부하 위험 단계 표시·빈사 표시·보스 표시(`UnitSnapshot.IsBoss`).
- uGUI + TextMeshPro, 기준 해상도 1920×1080 가로 고정, Scale With Screen Size, 버튼 터치 영역 넉넉히.
- 한글 폰트: 기본 폰트는 한글 미지원 → **Nanum Gothic**(OFL, 고정 TTF 2.0MB, 사용자 승인 2026-09-14). TMP 폰트 에셋은 굽지 않고 `BattleBootstrap` 이 실행 시 동적 아틀라스로 만든다(필요한 글자만 넣음). 폰트 참조는 씬의 `[SerializeField]` 필드에 두고 씬 생성 도구가 채운다.
- **씬 생성·TMP 리소스는 에디터 도구**(`ProjectSetupTools`, 메뉴 `TAoB/…` 또는 `-executeMethod`)가 만든다. 카메라는 `BattleBootstrap` 과 같은 객체에 붙여 씬 객체를 하나로 유지한다(오버레이 캔버스라 카메라는 배경색만 담당).
- 입력: 프로젝트가 Input System 전용이라 `EventSystem` 에 `InputSystemUIInputModule` 을 붙인다.
- 구조 명령의 대상 선택: 구조자 = 정상 상태인 첫 아군, 대상 = 흩어진 첫 아군(선택 UI 없음, 프로토타입). 규칙을 `RescueSelector` 순수 함수로 빼 테스트로 고정하고, 버튼 활성 조건도 같은 함수를 쓴다.
- **자동 검증 범위**: 표시 계층 중 엔진 비의존 부분(`TickDriver`·`BattleLayout`·`UiText`)만 EditMode 테스트. `MonoBehaviour` 는 배치모드 플레이 스모크(6초 실행, 오류 0건, 유닛 표시 생성 확인)로 켜자마자 터지는 실수만 거르고, 동작은 아래 수동 시나리오로 본다.
- **개입 바**: 캐릭터별 고유 기술 버튼 3(`UseSkill`) + 과부하 버튼(마법사, 누를 때마다 `Overload` 명령 1회) + 구조 버튼(흩어짐 아군이 있을 때만 활성, `Rescue`) + 후퇴(`Retreat`). 배속 1x/2x. 기본 공격은 자동 전용이라 버튼이 없다.
- 결과 화면: 승패 사유, 빈사자, 마도구 내구력 소모, 결과 해시, 재시작.
- `DebugLoadoutPanel`: 캐릭터별 장비 교체 후 재시작(2축 규칙 체감용, 덱세팅 화면 아님). `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 안에서만 생성한다.

**검증**
- 자동: EditMode 테스트 전체 통과(✅ 214개, 2026-09-15 — 표시 계층 21개) · 배치모드 플레이 스모크 오류 0건(✅ 유닛 표시 7개 생성 확인).
- 아래는 수동 시나리오(시드 고정으로 재현). 사용자 PC 에서 에디터가 너무 느려 Phase 5 의 exe·APK 에서 수행한다(결정 로그 2026-09-15). 시나리오 8 은 Phase 5 에서 플레이 스모크로 자동화한다.
1. 기본 편성 승리 → 결과 화면
2. 제한 시간 초과 → 자동 후퇴 패배
3. 수동 후퇴
4. 사전 조건(빈사 금지) 위반 패배
5. 과부하 성공·흩어짐(구조로 해제)·폭주·한계돌파 각각 발생
6. RES 미달 마도구 장착 시 자해 피해 표시
7. 입력 없이(자동만) 배속 2x 로 돌린 결과 해시가 1x 와 동일(같은 시드)
8. 재시작 5회 후 씬의 `GameObject` 수가 첫 전투와 동일

## Phase 4.5 — 연출 보강 (그림 없이 움직임만)
**목표**: 아트 에셋 없이 코드만으로 공격·피격·빈사·과부하가 눈에 보이는 전투 화면.

**변경 파일** (`Game/Assets/_Project/`)
- `Scripts/View/MotionCurves.cs` (신규) — 엔진 비의존 순수 함수: 돌진·흔들림·튀어오름·맥동·숨쉬기 곡선(경과 시간 → 오프셋/배율)
- `Scripts/View/UnitView.cs` — 연출 상태와 `PlayLunge`·`PlayHit`·`PlaySelfDamageFlash`·`PlayDowned`·`ResetDownedPose` 진입점. 움직이는 것은 네모 박스(`_box`)뿐이고 막대·글자는 고정
- `Scripts/View/BattleEffects.cs` (신규) — 이벤트 → 연출 대응 전담(가해자 틱당 첫 피해 판정, 짧은 멈춤 타이머, 화면 흔들림·컷인 트리거, 크리티컬 판정 `IsCriticalDamage` 단일화). `BattleBootstrap.Dispatch` 는 팝업 처리 뒤 `BattleEffects.Handle(e)` 한 줄만 부른다
- `Scripts/View/ScreenShake.cs` (신규) — 전장 층(`Field`)을 감쇠하며 흔든다
- `Scripts/View/BattleBootstrap.cs` — `BattleRoot` 아래 `Field`(배경·유닛) 층 생성, `BattleEffects` 연결, 짧은 멈춤 중 틱 루프 생략(전투 종료 후에도 연출은 계속 흐름)
- `Scripts/View/PopupPool.cs` — 튀어오르는 배율 곡선
- `Scripts/View/BattleHud.cs` — 기술 컷인 전용 글자(웨이브·결과 배너와 별도 슬롯)
- `Scripts/View/BattleLayout.cs` — 배경(하늘·바닥·경계선) 색과 위치 상수
- `Scripts/View/TickDriver.cs` — `RestoreTicks`(짧은 멈춤이 틱 루프 중간에 걸릴 때 미실행 틱을 되돌림)
- `Editor/PlayModeSmoke.cs` — `Field` 층 도입으로 유닛 표시 경로가 `BattleRoot/Field` 로 바뀌어 `CheckHierarchy` 갱신
- `Tests/EditMode/View/MotionCurvesTests.cs` (신규), `Tests/EditMode/View/TickDriverTests.cs`(`RestoreTicks` 테스트)

**결정**
- 규칙 모듈(`TAoB.Combat`)·데이터(`TAoB.Data`)는 건드리지 않는다. 연출은 `BattleEvent`·스냅샷을 소비만 하므로 결정론·배속 무관 테스트가 그대로 유효하다.
- 이벤트 → 연출:
  | 입력 | 연출 |
  |---|---|
  | `Damage` 가해자(같은 틱 첫 피해만) | 상대 진영 쪽으로 돌진 후 복귀 |
  | `Damage` 피격자 | 흰색 번쩍임 + 좌우 흔들림. 크리티컬이면 흔들림 크게 + 화면 흔들림 + 짧은 멈춤 |
  | `SelfDamage` | 주황 번쩍임 |
  | `Downed` | 박스가 기울며 가라앉음 + 약한 화면 흔들림 + 짧은 멈춤 |
  | `Rescued` | 빈사 자세(기울기·가라앉음)를 되돌린다 |
  | 스냅샷 `Condition == Scattered` | 흩어진 동안 박스가 잘게 떨림 |
  | 스냅샷 `OverloadRisk > 0` | 위험 단계가 높을수록 빨라지는 맥동 |
  | `OverloadResolved` 폭주·한계돌파 | 화면 흔들림 |
  | `ActionStarted` 아군 `UseSkill` | 상단 컷인 배너 "{이름} 고유 기술!" (문구는 전투 시작 때 캐릭터별로 한 번 만든다) |
  | 상시(정상 상태) | 유닛마다 위상을 달리한 위아래 숨쉬기 |
- 연출 수치(지속 시간·진폭)는 규칙 밸런스가 아니라 표시 상수이므로 `balance.json` 에 두지 않는다. 색·위치처럼 여러 파일이 같이 쓰는 값만 `BattleLayout` 에 모으고, 한 파일에서만 쓰는 지속 시간·진폭·주파수는 그 파일의 `private const` 로 둔다(`PopupPool` 의 기존 `Lifetime`·`RiseSpeed` 와 같은 방식 — 코드 리뷰로 확정, 최초 계획의 "전부 BattleLayout" 방침을 대체).
- **층 분리**: `BattleRoot` 아래 첫 자식으로 전체 화면 `Field` 층을 두고 배경과 유닛 표시를 그 아래에 만든다. 화면 흔들림은 `Field` 만 움직인다 — HUD·개입 바·결과 화면은 흔들리지 않아 버튼을 누르는 위치가 어긋나지 않는다. 팝업은 흔들리지 않는 좌표에 뜨지만 흔들림 진폭(≤12px)·지속(≤0.25초)이 짧아 어긋남을 허용한다. 배경 띠는 화면보다 흔들림 진폭만큼 크게 만들어 가장자리가 드러나지 않게 한다.
- **짧은 멈춤**: 멈춤 시간이 남아 있으면 그 프레임은 `TickDriver.Advance` 를 부르지 않고 틱 루프를 건너뛴다. 틱 도중 멈춤이 걸리면 그 프레임의 남은 틱 루프를 즉시 끊는다(누적 틱은 다음 프레임에 이어서 처리). 틱 결과는 벽시계와 무관하므로 해시가 바뀌지 않는다. 멈춤 중에도 연출 애니메이션은 흐른다. 멈춤 타이머는 `BattleEffects` 가 갖고 `StartBattle` 에서 초기화한다.
- **정지 조건**: 모든 일회성 연출(돌진·피격·번쩍임·화면 흔들림·컷인)은 지속 시간이 지나면 원위치·원래 색으로 정확히 되돌리고, 이후 프레임에는 그 속성을 쓰지 않는다. 계속 도는 것은 숨쉬기·흩어짐 떨림·과부하 맥동뿐이며 해당 상태가 끝나면 멈춘다.
- **전투 종료 후에도 연출은 끝까지 재생**: `BattleBootstrap.Update` 는 전투가 끝나도(`_isEnded`) 틱 루프(`RunTick`)만 멈추고, `BattleEffects.Tick`·`ScreenShake.Tick`·`UnitView.Apply` 는 계속 부른다. 마지막 틱에 시작된 빈사 연출·화면 흔들림·짧은 멈춤이 시작하자마자 멈추지 않고 끝까지 재생되게 하기 위함(코드 리뷰 반영 — 최초 계획은 "이후 프레임은 다시 그리지 않는다"였으나 이 성질이 연출과 충돌해 대체).
- **색 소유권**: `_box.color` 는 `UnitView` 한 곳만 쓴다. 매 프레임 기준색(상태별: 정상 = 직업색, 빈사 = `DownedColor`)을 정하고 번쩍임은 그 위에 보간해 덮는다 — 번쩍임이 끝나면 자동으로 기준색이 된다.
- 팝업 겹침 처리는 2026-09-14 결정("같은 자리·같은 틱이면 위로 쌓기")을 그대로 두고, 등장 배율 곡선만 추가한다.
- 흔들림 방향은 난수 대신 시간 기반 사인 곡선을 쓴다(`UnityEngine.Random` 불사용 — 재현성과 규칙 난수 분리).
- 할당 없음: 연출 상태는 유닛당 float 몇 개, 프레임 갱신에서 문자열·컬렉션을 만들지 않는다. 박스 이동은 위치·회전·배율만 바꾸고 크기(`sizeDelta`)는 건드리지 않는다 — 행동 게이지 막대가 이미 매 프레임 폭을 바꾸므로 캔버스 갱신 빈도는 늘지 않는다.

**검증**
- `MotionCurvesTests` — 곡선 5종 각각:
  - 돌진·튀어오름: t=0 에서 원위치(튀어오름은 배율 1), 지속 시간 이상에서 원위치, 중간에 최대값
  - 흔들림: 지속 시간 이상에서 정확히 0, 뒤 구간 최대 진폭 < 앞 구간 최대 진폭
  - 맥동: 위험 단계 0 이면 항상 배율 1, 단계가 높을수록 주기가 짧음, 배율 범위 1.0~1.08
  - 숨쉬기: 한 주기 뒤 같은 값(주기성), 위상이 다르면 같은 시각 값이 다름, 진폭 상한 이내
  - 지속 시간 0 이하: 세 곡선(돌진·튀어오름·흔들림) 모두 예외 없이 원위치·기본값
- EditMode 테스트 전체 통과(✅ 226개, 2026-09-17) · 플레이 스모크 오류 0건(✅ 유닛 표시 7개 생성 확인).
- Phase 5 PC 빌드 자동 캡처 스크린샷으로 배경·박스 이동·번쩍임을 확인한다(에디터 GUI 대신).
- 수동(exe·APK, Phase 4 시나리오와 함께): 9. 크리티컬 때 전장만 흔들리고 버튼은 고정 · 기술 버튼을 누르면 컷인 문구 표시 · 과부하 위험 단계에 따라 맥동이 빨라짐 · 빈사 유닛이 기울어진 채 멈춤 · 재시작 직후 멈춤·흔들림이 남아 있지 않음.

## Phase 5 — 빌드 & 재미 판정
**목표**: 에디터를 열지 않고 사용자 폰(APK)과 PC(exe)에서 플레이해 계속 갈지 판단한다.

**변경 파일**
- `Game/Assets/_Project/Editor/BuildTools.cs` (신규) — `BuildAndroid()`·`BuildWindows()`. `BuildOptions.Development`, 씬 목록은 `EditorBuildSettings`. 출력 `Game/Builds/Android/TAoB-slice.apk`·`Game/Builds/Windows/TAoB.exe`(`.gitignore` 의 `/Game/[Bb]uilds/` 로 이미 제외)
- `Game/Assets/_Project/Editor/PlayModeSmoke.cs` — 재시작 5회 뒤 `GameObject` 수 비교 추가(Phase 4 시나리오 8 자동화)
- `Game/Assets/_Project/Scripts/View/BattleBootstrap.cs` — `Restart` 를 스모크가 부를 수 있게 공개 · `Awake` 에서 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 로 자기 객체에 `DevCapture` 부착(`DebugLoadoutPanel` 과 같은 구역화)
- `Game/Assets/_Project/Scripts/View/DevCapture.cs` (신규, `UNITY_EDITOR || DEVELOPMENT_BUILD` — 에디터에서도 컴파일돼 테스트·스모크가 컴파일 오류를 먼저 잡는다, 릴리스 제외) — 실행 인자 `-captureShots <폴더>` 가 있을 때만 몇 장 찍고 종료
- `Tools/run-editor-tool.ps1` — `-Tool apk|win` 추가
- `Tools/run-tests.ps1` — 결과 XML 을 UTF-8 로 읽도록 수정(제품명 한글화로 드러난 문제, 아래 결정 로그)
- `Game/Assets/_Project/Scripts/View/BattleLayout.cs` — `GroundTopY` 를 -60 → -190(바닥선이 아래열 유닛을 가로지르던 것, 캡처로 발견)
- `Game/ProjectSettings/ProjectSettings.asset` — 패키지명(Android·Standalone 둘 다)·제품명, 세로 자동 회전 끄기
- `docs/HANDOFF.md`·`docs/references/project_facts.md` — 다음 할 일·편의 스크립트 목록 갱신
- `Game/Assets/Scenes/`(`SampleScene.unity`·`.meta`·폴더 `Scenes.meta`) 삭제 — 템플릿 잔재
- 기획서 v0.6 13장·4-1 (판정 결과 반영)

**결정**
- Development Build + Unity 기본 디버그 서명(키스토어 파일을 만들지 않음 — 저장소에 비밀 정보 없음). 릴리스 키스토어는 출시 단계에서.
- 스크립팅 백엔드는 현행 IL2CPP·ARM64 유지 — Mono 는 ARMv7(32비트) 전용이라 32비트 앱을 지원하지 않는 최신 기기에 설치되지 않을 수 있다.
- 화면 방향: 현재 `defaultScreenOrientation: 4`(자동 회전)에 세로가 허용돼 있다 → 자동 회전은 유지하되 `allowedAutorotateToPortrait`·`PortraitUpsideDown` 을 0 으로(가로 두 방향만).
- 패키지명 `com.ageofbastards.slice`(시험판 전용 임시 ID — 정식 출시 ID 는 출시 단계에서 새로 정한다), 제품명 `TAoB 시험판`(사용자 결정 2026-09-15). 템플릿 기본값(Android `com.DefaultCompany.urp_2d` · Standalone `com.DefaultCompany.2D-URP` · 제품명 `Game`)을 모두 대체한다.
- **빌드 도구 실행**: `apk`·`win` 은 `-quit -nographics` 로 실행하고, 제한 시간 기본값을 3600초로 둔다(첫 IL2CPP 빌드가 기존 기본 300초를 넘기기 쉬움). `BuildTools` 는 `BuildReport.summary.result` 가 `Succeeded` 일 때만 로그에 `[빌드] 성공 <경로> — <MB>MB, <분>분` 을 남기고, 실패·예외면 `[빌드] 실패 — <사유>` 를 남긴 뒤 종료 코드 1 로 끝낸다. 스크립트는 기존 도구처럼 이 완료 표시로 성공을 판정한다.
- **`DevCapture`**: 캡처 시각은 2·5·9초. `ScreenCapture.CaptureScreenshot` 은 프레임 끝에 비동기로 파일을 쓰므로, 마지막 캡처 뒤 1초 이상 지나서 `Application.Quit()` 한다. 폴더 인자가 없거나 비었으면 아무것도 하지 않는다. 캡처 모드일 때만 `Application.runInBackground = true` — 프로젝트 기본값(꺼짐)이면 창이 포커스를 잃는 순간 멈춰 캡처가 끊긴다(실측).
- **재시작 스모크(시나리오 8)**: 한 프레임에 여러 번 부르지 않는다 — `Destroy` 가 프레임 끝에 처리되기 때문. 순서: 첫 전투 시작 → 1초 대기 → 기준 개수 측정 → (`Restart` → 2프레임 대기 → 개수 측정) × 5 → 모두 기준과 같으면 통과. 개수는 `PopupPool` 하위를 뺀 씬 전체 `GameObject` 수다. 팝업 풀은 전투 진행에 따라 커질 수 있고 재시작 누수 검사 대상이 아니기 때문이다. 제한 시간(6초) 안에 5회를 다 못 돌면 "미완료" 오류로 실패시킨다 — 검사를 안 한 것을 통과로 보고하지 않기 위함.
- Windows 개발 빌드를 PC 시연판으로 겸한다(에디터보다 가벼움). 설치는 USB 디버깅 없이 APK 파일을 폰으로 옮겨 직접 설치(사용자 환경 2026-09-15).
- TMP 이모지 에셋(Phase 4 보류)은 APK 크기를 보고 판단한다 — 수 MB 미만이면 그대로 둔다.
- 재미 판정 질문: ① 개입하고 싶은 순간이 있었나 ② 자동만 볼 때 지루했나 ③ 과부하에 긴장감이 있었나 ④ 장비 교체가 결과를 바꿨나 ⑤ 한 판 길이가 적당했나.

**검증**
- 스모크: 오류 0건 + 재시작 5회 뒤 `GameObject` 수가 첫 전투와 같음. ✅ 2026-09-17 — 기준 167개, 5회 뒤 167개(Phase 4 시나리오 8 완료)
- `run-editor-tool.ps1 -Tool win` 성공 → `TAoB.exe -captureShots <폴더>` 스크린샷을 Claude 가 열어 확인. ✅ 163MB. 배경·빈사 기울기·피격 흰 번쩍임·자해 주황 번쩍임·크리티컬 노란 숫자 확인(Phase 4.5 검증의 캡처 항목 완료)
- `run-editor-tool.ps1 -Tool apk` 성공, APK 크기 기록. ✅ 2026-09-17 — 45MB, 11.6분(첫 IL2CPP 빌드). TMP 이모지 에셋은 APK 대비 무시할 크기라 그대로 둔다(Phase 4 보류 항목 종결).
- 사용자: APK 설치 후 Phase 4 시나리오 1~6 과 Phase 4.5 시나리오 9 를 폰(또는 exe)에서 수행. 시나리오 7 은 배속 무관 자동 테스트가 보장하므로 결과 해시 확인 1회로 대체.
- 같은 시드로 입력 없이 돌린 결과 해시가 exe 와 폰에서 같음.
- 질문 답변을 기획서에 기록하고, 다음 단계(덱세팅 추가 / 규칙 수정 / 무료 스프라이트 / 방향 전환)를 사용자와 결정.

## 범위 밖 (이번 계획에서 안 함)
Live2D, 픽셀아트, 사운드, 덱세팅 화면, 거점, 부활, 저장, 스토리·컷씬, iOS 빌드, 스토어 계정.
기획서 4-1 중 슬라이스 제외분: 직업 시너지, 타겟 강제 지정, 소모 아이템, 흩어짐 생존 후 마나 감각 상실.

## Known Pitfalls
- Unity 에디터가 프로젝트를 열고 있으면 batchmode 테스트가 프로젝트 잠금으로 실패 → 테스트 실행 전 에디터 종료(또는 에디터 Test Runner 사용).
- `.meta` 파일은 반드시 커밋(누락 시 참조 깨짐).

## 결정 로그
- 2026-09-10: 엔진 Unity(Live2D 공식 지원) 확정. Godot 은 텍스트 씬으로 AI 협업이 편하나 Live2D 모바일 미검증으로 기각. Unity 에디터 수작업 부담은 "씬에 부트스트랩 1개 + 코드 생성" 결정으로 완화.
- 2026-09-10: 리뷰 방식 — 간이 리뷰(code-reviewer 1개) 대신 참조 문서 구축 선택(사용자). 착수가 늦어지는 대신 이후 모든 커밋에 CLAUDE.md 워크플로 적용.
- 2026-09-10: CLAUDE.md 「프로젝트 스택」의 fastlane·`MARKETING_VERSION` 줄은 이 프로젝트와 무관하나 수정하지 않음(사용자가 CLAUDE.md 수정 옵션을 고르지 않음). `project_facts.md` §0 에 "이 문서가 우선한다"고 명시.
- 2026-09-10: 전투 상태 전체 불변 복사는 틱당 GC 부담으로 기각, 규칙 계산 순수 함수 + 상태 캡슐화로 대체.
- 2026-09-10: 데이터 로더 위치를 "View asmdef" 에서 별도 `TAoB.Data` asmdef 로 변경. asmdef 는 폴더 단위로 적용되어 `Scripts/Data/` 가 `Scripts/View/` 의 asmdef 에 속할 수 없고, 로더 테스트를 표시 계층 없이 돌리기 위함.
- 2026-09-10: §0-A docs·(미분류) 버킷에도 스택 문서 지정(원안은 `—`). SKILL.md 가 스택 문서 부재 시 BLOCKED 로 규정하기 때문.
- 2026-09-10: SKILL.md 레지스트리의 `ecc:` 접두어 이름이 이 환경에 없어 호출 실패(1차 리뷰에서 확인) → 접두어 없는 이름으로 교체. 원안의 "실패하면 교체" 조건부 결정을 확정으로 대체.
- 2026-09-10 **1차 `/plan-review` (PROCEED-WITH-FIXES — CRITICAL 0 · HIGH 10 · MEDIUM 8 · LOW 1 · comments REVISE 4, 기각 0)** 반영:
  - arch: 과부하 토글 → 1회 `Overload` 명령 + `AutoPilot` 타입 신설 / `balance.json` 의 틱 속도 제거, 틱 드라이버 결정 신설 / `SkillDefinition`·승패 우선순위 표 신설 / 스냅샷·이벤트 계약 신설.
  - lang: `SeededRandom` 자체 정수 알고리즘(스택 §0 난수 비고 모순 해소 — arch DEGRADED 동일 건) / 규칙 모듈 정수 연산 + Phase 5 기기 해시 비교 / `UnitState` 를 `sealed class` 로 확정.
  - quality: 밸런스 수치를 Phase 2 부터 `BalanceConfig` 주입으로(리터럴 금지) / `AtbScheduler` → `ActionGaugeScheduler`(용어집 `ActionGauge`).
  - security: 로더 상한 검증 추가 / `DebugLoadoutPanel` 개발 전용 구역 한정.
  - perf: 재시작 시 `BattleRoot` 파괴·구독 해제 + 시나리오 8 / 프레임당 틱 상한 20 / `PopupPool` / 이벤트 버퍼 재사용.
  - test: 배속 무관·제한 시간 경계·과부하 4분기 강제를 Phase 2 자동 테스트로 / 로더 실패 유형별 테스트, `TAoB.Data.Tests.asmdef` 신설.
  - comments: Context "현재 상태" 갱신 / 참조 문서 줄 수 서술 삭제(실측과 불일치) / 규칙 출처 v0.5 → v0.6 / 에이전트 이름 조건부 서술 확정.
  - 기타(arch 렌즈 밖 메모): Phase 2 테스트 결과 경로를 `project_facts.md` §0 명령으로 통일.
- 2026-09-11 **첫 커밋 전 `/plan-review --code` (docs 버킷, PROCEED-WITH-FIXES — CRITICAL 0 · HIGH 3 · MEDIUM 3 · LOW 1)** 반영:
  - arch: "4-1 그대로" 표현 삭제, 제외 규칙 4개를 범위 밖에 명시 / `BasicAttack`·`CommandQueue` 신설, 명령 수락·대기·대체 규칙, `AutoPilot` 은 기본 공격만(제안) / 스냅샷 = 호출자 버퍼 값 복사 + 직전·현재 보간 / `Step` 1회마다 `BattleBootstrap` 직접 전달 / 재리뷰 HIGH: 적·보스 기술 실행 경로가 없던 문제 → 입력 전용 규칙은 아군만, 적 `AutoPilot` 은 재사용 대기 종료 시 `UseSkill`.
  - test: 공식·명령 규칙·해시 테스트를 필수 목록에 추가 / 로더 상한 규칙 5종 개별 테스트 / 재리뷰 MEDIUM: 재사용 대기 만료 경계 테스트 추가.
  - perf: `PopupPool` 을 `BattleRoot` 밖에 두고 재시작 시 회수만.
  - 렌즈 밖 메모 반영: 시나리오 7 "입력 없이" 조건 / `StateHasher` FNV-1a 고정 / 설정집 v1.1 12장에 7-5 재능 축 미결 추가.
  - 기각: `.gitignore` 의 `fastlane/.env` 잔재 지적(CLAUDE.md 보안 주의가 명시, 무해) / Combat 파일 분할 계획 없음 지적(목록이 이미 타입당 파일 단위).
  - 사용자 결정으로 반영 안 함: 기획서 v0.6 본문 버전 라벨·변경 기록 `YYYY-MM-DD` 행(comments REVISE 2건).
- 2026-09-11: `.claude/plans/app-icon-campaign.plan.md`(이전 iOS 프로젝트 잔재, 다른 회사 앱 내부 경로 포함)는 공개 저장소에 올리지 않기로 하고 `.gitignore` 로 제외. 파일은 로컬에 유지. `CLAUDE.md` 도 사용자 요청으로 같은 방식으로 제외.
- 2026-09-11: Phase 0 설치 주체를 "사용자 수행"에서 "Claude 수행"으로 변경. 사용자가 직접 설치를 요청했고, 파일 4종(이름·출처·크기)을 제시한 뒤 다운로드 허가와 약관(Unity 서비스 약관, Android SDK 라이선스) 동의를 받았다. 로그인·라이선스 활성화는 사용자가 직접 수행. Hub 명령줄의 첫 시도에서는 템플릿 파일명을 잘못 찾아 빈 프로젝트가 생겼고, 이 폴더는 삭제하지 않고 스크래치 폴더로 옮긴 뒤 Universal 2D 템플릿으로 다시 생성.
- 2026-09-11 **Phase 0 커밋 전 `/plan-review --code` (축소 scope: security·quality·comments, PASS — CRITICAL 0 · HIGH 0 · MEDIUM 1 반영 · comments REVISE 5 반영)**: 계획서 모듈 ID 중복을 project_facts 참조로 교체, `.gitattributes` 의 `.asmdef` 를 JSON 제목으로 분리, Context·Phase 0 검증·Known Pitfalls 를 완료 사실에 맞춰 갱신. arch·perf·test 는 코드 없는 템플릿 설정 파일이라 사용자 선택으로 생략(보고서에 DEGRADED 기록).
- 2026-09-11 **Phase 2 구현 중 확정한 세부 규칙** (계획서에 없던 빈칸을 채운 것):
  - 파일 구성: 관련 규칙끼리 묶었다. `Equipment.cs`(Catalyst·Magitool·EquipRules), `TurnRules.cs`(ActionGaugeScheduler·AutoPilot·TargetSelector). 행동 처리는 `ActionResolver` 로 분리해 `BattleSimulation` 을 400줄 아래로 유지. `CombatScale`·`OverloadTier`·`UnitSetup`·`BattleSetup` 추가.
  - 크리티컬 확률 = 기본 확률 + SPD × 계수(만분율). 확률이 0 또는 10000 이면 난수를 쓰지 않는다 — 결과가 확정인 판정이 난수 순서를 흔들지 않게 하려는 것.
  - 과부하 초과 비율 = (원석 위력 × 과부하 배율 − MND) ÷ MND(‰). 위험 단계 = 확률표 구간 번호(최대 3).
  - 폭주: 시전자를 뺀 공격 가능한 전원(아군 포함, ID 순) 중 난수로 1명. 피해 배율 = 기술 배율 × 폭주 배율, 크리티컬 없음. 자해 = 최대 HP × 자해 배율 + 마도구 반발.
  - 게이지: 넘친 양은 보존(단 최대치의 2배에서 멈춤 — 한 틱 1행동이라 동작 차이 없이 int 넘침만 막음). 같은 틱 행동 순서는 SPD 내림차순 → ID 오름차순. 구조 뒤 구조자 게이지에서 추가 차감(음수 허용 = 다음 행동이 늦어짐).
  - 마도구: 행동 1회에 한 번만 사용(대상 수와 무관), 반발 자해는 공격 뒤 적용. RES 부족으로 장착 불가인 편성은 `BattleSetup` 생성 시 예외.
  - 틱 처리 순서: 후퇴 확인 → 흩어짐 HP 감소 → 재사용 대기 감소 → 게이지 충전 → 행동 → 웨이브 전환 → 승패 판정.
  - 흩어짐 종료 처리는 구현 규칙의 "HP 0 → 빈사" 항목이 정본(리뷰 반영으로 결과 집계만 하던 방식을 대체).
  - `Game/Assets/Settings/UniversalRP.asset` 은 에디터가 URP 17.3 형식(12→13)으로 자동 업그레이드했다. 값 변경은 없으며 커밋에 포함.
  - 커버리지 측정 패키지 `com.unity.testtools.codecoverage` 1.3.0 추가(사용자 다운로드 허가).
- 2026-09-11 **Phase 2 커밋 전 `/plan-review --code` (코드 7 scope + 설정 4 + 문서 2, PROCEED-WITH-FIXES — CRITICAL 0 · HIGH 8)** 반영:
  - arch: 흩어짐 종료 처리 통일(구현 규칙, 사용자 결정) / 코드에만 있던 규칙을 기록 — 최소 피해는 `BalanceConfig.MinimumDamage` 로 이동, 흩어진 유닛도 공격 대상(행동만 불가), 한계돌파는 피해 + MND 상승(사용자 결정).
  - security: 곱셈 넘침 방지(`CombatScale.ClampToInt`, 데미지 계산은 단계마다 int 범위에서 멈춤) / 수치 상한 `CombatScale.MaxValue`(1,000,000)·`MaxPermille`(100,000)·`BattleSetup.MaxTimeLimitTicks`(6000틱)를 생성자에서 검증.
  - perf: 대상 선택의 정렬 제거 — ID 순으로 미리 정렬한 배열을 쓴다.
  - test: 크리티컬 확정 분기(0%·100% 난수 미사용), 넘친 게이지 보존, 같은 틱 행동 순서, 흩어짐 종료 처리, 수치 상한 테스트 추가.
  - lang: 스냅샷 유닛 목록을 읽기 전용 래퍼로 노출. quality: `Gauge` → `ActionGauge`(용어집), `IsBoss` 를 스냅샷에 노출(보스 표시용). comments: 이유 주석 2개 추가.
  - arch(설정): 테스트를 `Tests/EditMode/Combat/` 로 옮겨 Phase 3 `TAoB.Data.Tests` 와의 폴더 충돌을 막음. 명령 큐는 사용자 명령 전용이고 자동 판단은 같은 틱에 직접 명령을 만든다는 사실을 결정·스택 문서에 반영.
  - comments(문서): 검증 첫 항목 완료 표시, Status 의 Phase 순서 정리.
  - 기각: "배속 묶음 테스트는 항상 통과한다"는 arch 메모 — 규칙 모듈이 프레임·시계에 의존하게 되는 회귀를 막는 가드로 유지 / "`.cs.meta` 에 MonoImporter 블록이 없다"는 quality 메모 — Unity 6 에디터가 직접 생성한 형식.
- 2026-09-11 **Phase 2 재리뷰 (HIGH 1 · comments REVISE 2, 나머지 scope 해소 확인)** 반영:
  - test: 게이지 2배 상한이 테스트로 검증되지 않음 → `ActionGaugeCapTests` 추가(SPD 1,000,000·게이지 최대 1로 6000틱 동안 매 틱 행동). 상한을 잠시 끈 상태에서 실패(6000 기대, 3853)하는 것을 확인한 뒤 되돌림.
  - comments(코드): `DamageCalculator.Scale` 주석의 이유를 "long 넘침 방지"에서 "마지막 int 변환 보호"로 정정 — 수치 상한 안에서는 long 이 넘치지 않는다.
  - comments(문서): 테스트 개수 147 → 148 정정. arch(설정): README 용어집 「명령 큐」 정의를 자동 판단이 같은 틱에 명령을 만드는 방식에 맞춤.
- 2026-09-11 **Phase 3 구현 중 확정한 세부 규칙** (계획서에 없던 빈칸을 채운 것):
  - 상성표(`affinities`)는 `balance.json` 에 둔다 — 상성 배율과 같은 파일에서 함께 조정하기 위함. 등록하지 않은 조합은 보통.
  - 캐릭터·장비에 표시 이름(`name`)을 넣는다. Phase 4 화면이 쓴다.
  - 스테이지가 `allyIds` 로 아군 편성을 고른다. 덱세팅이 생기면 이 부분만 바뀐다.
  - 파일별 리더(`BalanceReader` 등)로 나누고, 규칙 모듈 생성자의 검증 실패(`ArgumentException`)는 `FieldCheck.Build` 가 파일명·필드명을 붙여 다시 던진다.
  - 실제 데이터 첫 값: 마법사(MND 90)의 맑은 원석(위력 120)은 과부하 출력 180 → 초과 1000‰ 로 위험 단계 1(`OverloadResolver.RiskLevel`, 0~3). 초감각 인간은 RES 35 로 요구 RES 40 의 바람 반지를 써 사용마다 자해 10(부족 5 × 2). 재미 판정(Phase 5)에서 조정한다.
- 2026-09-14 **Phase 3 커밋 전 `/plan-review --code` (코드 7 scope + 설정 4 + 문서 2, PROCEED-WITH-FIXES — CRITICAL 0 · HIGH 1 · MEDIUM 7 · LOW 1 · REVISE 1)** 반영:
  - security: 캐릭터·장비·과부하 구간·상성표 배열에 로더 상한 상수 추가(위 결정).
  - arch: 밸런스 21개 값의 하한이 로더와 생성자에 이중으로 적혀 있던 것을 제거 — 로더는 누락만 검사, 범위는 생성자 예외의 매개변수 이름으로 필드명 복원(위 결정). quality LOW(`V` 약어)도 같이 해소.
  - test: 원석 미존재 ID·장비 ID 중복·웨이브 수·웨이브당 적 수의 상한 통과·과부하 구간 수 상한·밸런스 누락/하한 테스트 추가. 실제 파일 스모크는 개수 대신 불변식(편성 ID 전부 해석)으로 완화.
  - comments(문서): 위험 단계 "2단계" → 1 로 정정(`RiskLevel` 은 0부터). arch(설정) 지적 반영으로 Phase 4 그래픽 항목에 보스 표시 명시.
  - 렌즈 밖 메모 반영: `EnumName<T>` 제약 `struct, Enum` / 정수 배열은 `Ints` 로 분리(null 검사 제거) / 캐릭터 조회를 `CharacterData.Find` 하나로.
  - 재리뷰(arch·security·test): 지적 전부 해소 확인. 추가 반영 — JSON 키 = `BalanceConfig` 생성자 매개변수 이름 규약을 21개 전부 리플렉션으로 고정하는 테스트(arch MEDIUM), 과부하 구간 정확히 10개 통과 테스트(test MEDIUM). arch 가 새로 낸 "과부하 구간·상성표 상한이 계획서에 없음" HIGH 는 같은 라운드에서 이미 결정에 적혀 있어 해당 없음.
  - 기각: 진영 일치·이중 배치 검사를 Combat 공개 함수로 올리라는 arch MEDIUM — 한 줄짜리 구조 불변식이고 로더 쪽은 필드 경로 표시가 목적이라 P6(규모 보정) 위반 / `isBoss` 소비처 없음(arch·설정) — Phase 2 결정으로 스냅샷에 노출한 표시용 값, 소비처는 Phase 4 / `Parse(null)` 의 `ArgumentNullException` — 호출자 버그 계약.
- 2026-09-14 **Phase 4 구현 중 확정한 세부 규칙** (계획서에 없던 빈칸을 채운 것):
  - 폰트를 Noto Sans KR 에서 Nanum Gothic 으로 변경(사용자 승인). Google Fonts 의 Noto Sans KR 은 가변 폰트(약 6MB)라 TMP 두께 처리가 불확실하고, Nanum Gothic 은 고정 TTF 2.0MB.
  - TMP 필수 리소스(`Assets/TextMesh Pro/`, 4MB)는 에디터 도구로 가져와 커밋한다. batchmode 에서는 패키지 가져오기가 비동기라 `-quit` 없이 실행하고 완료 콜백에서 종료한다.
  - 틱 드라이버는 초가 아니라 틱 수로 누적한다 — 0.1초씩 빼면 float 오차가 쌓여 40틱 뒤 1틱이 사라지는 것을 테스트가 잡았다.
  - `UnitView` 는 직전·현재 스냅샷을 `alpha` 로 보간해 막대만 부드럽게 움직이고, 글자는 값이 바뀔 때만 `SetText` 수치 오버로드로 갱신한다. 막대는 스프라이트 없이 채움 폭(`sizeDelta.x`)으로 그린다(`Image.fillAmount` 는 스프라이트가 필요).
  - 팝업 풀은 같은 자리에 같은 틱의 알림이 겹치면 위로 쌓는다. 라벨은 상수 문자열만 넘겨 프레임당 문자열 할당을 피한다.
  - 개발용 장비 교체는 `GameData.CreateBattleSetup(교체 목록)` 으로 들어간다. 패널은 RES 부족으로 장착 불가인 마도구를 목록에서 빼므로 `BattleSetup` 예외가 나지 않는다.
  - 배치모드 플레이 스모크(`PlayModeSmoke`)를 에디터 도구로 추가 — PlayMode 테스트(스택 §0 에서 범위 밖)가 아니라 실행 오류만 거르는 도구. batchmode 플레이는 도메인 재로드 없이 들어가므로 `Run()` 에서 바로 갱신 콜백을 등록한다.
  - `Game/ProjectSettings/URPProjectSettings.asset`·`SceneTemplateSettings.json` 은 에디터가 자동 갱신·생성한 것으로 값 변경 없이 커밋에 포함.
- 2026-09-15 **Phase 4 커밋 전 `/plan-review --code` (코드 7 scope + 설정 4 + 문서 2, PROCEED-WITH-FIXES — CRITICAL 0 · HIGH 2 · MEDIUM 5 · LOW 2 · REVISE 1 · ADD 2)** 반영:
  - arch: 흩어짐 피해 팝업이 `SourceId`(가해자 없음 = 비어 있음)를 읽어 화면 중앙에 겹치던 버그 → `TargetId` 로 수정 / 과부하 가능 판정을 표시 계층이 다시 구현하던 것을 `OverloadResolver.CanOverload` 공개로 통일(위 변경 파일) / 전투가 끝난 프레임은 보간을 1로 밀어 최종 상태를 그린다(중간값이 결과 화면 뒤에 남지 않게).
  - perf: HP·행동 게이지 막대는 보간 결과가 바뀔 때만 폭을 다시 쓴다(레이아웃 갱신 절약).
  - test: `RescueSelector` 추출 + 테스트 6개, `UiText.OverloadOutcome`·`Condition`·`WaveBanner`·`BattleLayout.JobColor` 테스트 추가(표시 계층 자동 검증 대상 전부 커버).
  - lang: 버튼 배경 이미지를 `UiFactory.CreateButton` 이 `out` 으로 돌려주게 해 `targetGraphic as Image` 캐스트 제거. quality: 팝업·토스트 색 리터럴을 `BattleLayout` 상수로 모으고, 불리언에 `Is/Has/Was` 접두를 붙였다.
  - comments: `PopupPool` 위치 서술 정정(진입점 직속 → 캔버스 직속), `UiFactory.CreateBar` 에 `Image.fillAmount` 를 못 쓰는 이유 추가, `ProjectSetupTools` 에 "다시 실행하면 씬·빌드 목록을 덮어쓴다" 경고 추가. arch(설정) 지적으로 `EditorBuildSettings.asset` 을 변경 파일에 명시.
  - 기각: `SetText("{0:1}")` 이 잘못된 서식이라는 quality HIGH — TMP 는 .NET 서식이 아니라 자체 파서를 쓰고 `'1'~'9'` 를 소수 자릿수로 읽는다(엔진 소스 `TMP_Text.cs` 확인). `F1` 이 오히려 글자로 찍히므로 현행이 맞다. 세 리뷰어가 같이 오독했으므로 두 호출부에 이유 주석을 남겼다.
  - 보류(렌즈 밖 메모): TMP 이모지 에셋이 `Resources` 에 남아 빌드에 포함되는 문제 — TMP 설정이 참조하므로 Phase 5 빌드 크기 확인 때 함께 본다. 템플릿 잔재 `Assets/Scenes/SampleScene.unity` 도 그때 정리.
- 2026-09-15: **시연 경로를 에디터에서 빌드로 전환, Phase 4.5 연출 보강 신설**(사용자 결정). 사용자 PC(i7-10700 · 16GB · GT 1030)에서 에디터가 너무 느려 수동 시나리오를 진행하지 못했고, 색 네모와 글자만으로는 재미를 판정할 수 없었다. "그래픽 = 색 네모"와 범위 밖의 픽셀아트는 그대로 두고, 코드로 만든 움직임(돌진·번쩍임·흔들림·컷인)만 범위에 넣는다. 수동 시나리오는 exe·APK 에서 진행하고, 시나리오 8 은 스모크로 자동화한다. 기각: 무료 스프라이트 적용(사용자가 연출 우선 선택), 시연 연기 후 기획 작업.
- 2026-09-17 **Phase 4.5 구현 중 확정한 세부 규칙** (계획서에 없던 빈칸을 채운 것): 흔들림·컷인·연출 수치는 각 소비 파일의 `private const` 로 둔다(위 결정 갱신 반영). `BattleEffects.IsCriticalDamage(e)` 를 크리티컬 판정의 유일한 해석 지점으로 두고 팝업·연출 양쪽이 같이 쓴다.
- 2026-09-17 **Phase 4.5 커밋 전 `/plan-review --code` (코드 7 scope + 설정 1(security) + 문서 1(comments), PROCEED-WITH-FIXES — CRITICAL 0 · HIGH 2 · MEDIUM 4 · LOW 2 · REVISE 9)** 반영:
  - arch·lang(HIGH): 전투 종료 게이트(`_isEnded`)가 연출까지 막아 마지막 틱의 화면 흔들림·빈사 연출이 시작하자마자 멈추던 문제 → `BattleBootstrap.Update` 는 틱 루프만 `_isEnded` 로 막고 `BattleEffects.Tick`·`ScreenShake.Tick`·`UnitView.Apply` 는 계속 부름(위 결정 신설) / 구조(Rescue) 후 빈사 자세가 영구히 남던 문제 → `BattleEffects.Handle` 에 `Rescued` 케이스 추가, `UnitView.ResetDownedPose` 신설.
  - arch(MEDIUM): 연출 수치 정본이 계획(BattleLayout)과 코드(각 파일 로컬 상수)가 어긋남 → 코드 쪽(기존 `PopupPool` 관례와 일치)을 정본으로 삼아 결정 갱신 / 크리티컬 판정(`e.Detail==1`)이 두 스위치에 중복 → `BattleEffects.IsCriticalDamage` 로 단일화.
  - quality(MEDIUM): `UnitView.UpdateBoxMotion` 이 5개 연출을 한 함수에 몰아넣어 64줄 → `ApplyLunge`·`ApplyHitFlash`·`ApplyConditionMotion`·`ApplyOverloadPulse`·`ApplyDowned` 로 분리, `UpdateBoxMotion` 은 호출 순서만 남김.
  - quality(LOW): 위상 오프셋 매직 넘버(`0.9137f`·`1.7f`) → `BreathePhaseIdMultiplier`·`ScatterYPhaseOffset` 로 명명.
  - test(MEDIUM): `MotionCurves.Lunge`/`Pop`/`Shake` 의 `duration<=0` 경계 미검증 → `ZeroOrNegativeDuration_ReturnsBaseline` 테스트 추가.
  - comments(REVISE, 코드 3건): `BattleBootstrap.cs` 짧은 멈춤 주석의 인과가 반대로 적힘(벽시계가 아니라 틱 루프가 멈춘다) / `UnitView.cs` "이 메서드만 `_box` 를 쓴다" 과장(생성은 `Build` 담당) / `ScreenShake.cs` "더 강한" → "더 강하거나 같은"(실제 비교가 `>=`) — 모두 정정.
  - comments(REVISE, 문서 6건): 위 항목들 계획서 반영, Status 줄 "계획 추가" → "구현 완료" 로 정정, `BattleLayout.cs` 변경 파일 설명을 배경 상수로 한정, `UnitView.cs` 변경 파일의 `PlayFlash` 오타를 `PlaySelfDamageFlash` 로 정정, `Editor/PlayModeSmoke.cs` 를 Phase 4.5 변경 파일에 추가(Field 층 도입에 따른 필수 변경), `docs/HANDOFF.md` "다음에 할 일" 을 구현 완료 상태에 맞게 갱신.
  - perf(LOW): `StartBattle` 마다 컷인 델리게이트·딕셔너리 재생성 — 재시작 단위 1회성 할당이라 조치 불요(YAGNI, 리뷰어도 동의).
  - security: 코드·설정(.meta) 버킷 모두 발견 없음(PASS).
  - 렌즈 밖 메모 반영: `TickDriver.RestoreTicks` 에 음수 입력 방어(`count<=0` 이면 무시) 추가.
  - 기각: `BattleEffects`·`ScreenShake` 의 "틱당 첫 피해 판정" 로직에 EditMode 테스트가 없다는 lang 렌즈 밖 메모 — 두 타입 모두 `internal` 이라 테스트 접근에는 `RescueSelector` 처럼 공개 순수 함수로 승격해야 하는데, 이번 라운드에서 새 CRITICAL/HIGH 로 지적되지 않았고 로직이 한 줄짜리 딕셔너리 비교라 P6(규모 보정) 위반 — Phase 5 이후 재발동 빈도가 문제로 드러나면 재검토.
- 2026-09-15 **Phase 4.5·5 계획 `/plan-review` (PROCEED-WITH-FIXES — CRITICAL 0 · HIGH 4 · MEDIUM 8 · LOW 1 · REVISE 3)** 반영:
  - arch·lang: 화면 흔들림이 `BattleRoot` 전체(버튼 포함)를 흔들고 팝업과 어긋나는 문제 → `Field` 층 분리 / `DevCapture` 생성 경로 → `BattleBootstrap.Awake` 구역화 / `Advance(0)` 로는 누적 틱이 돌아 멈춤이 안 됨 → 틱 루프 생략·즉시 중단 / 재시작 때 멈춤·흔들림 잔여 → `StartBattle` 초기화.
  - lang: `CaptureScreenshot` 비동기 저장 → 마지막 캡처 1초 뒤 종료. lang·test: `Destroy` 지연과 팝업 풀 성장으로 재시작 개수 비교가 흔들림 → 프레임 대기 순서·풀 제외 규칙.
  - perf: 일회성 연출·화면 흔들림의 정지 조건 명시. quality: `Dispatch` 비대화 → `BattleEffects` 분리, 박스 색 쓰기 경로 단일화(렌즈 밖 메모).
  - test: 곡선 5종 각각의 검증 항목, 연출 수동 시나리오 9 추가. comments: 팝업 겹침 결정과의 모순 정리, Standalone 식별자 명시, `HANDOFF.md` 갱신을 변경 파일에 추가. arch 렌즈 밖 메모: 빌드 제한 시간·완료 표시, 시나리오 8 서술 시제.
  - 기각: 상시 숨쉬기·맥동이 캔버스를 매 프레임 더럽힌다는 perf MEDIUM — 행동 게이지 막대가 이미 매 프레임 폭을 바꾸므로 "변화 없는 프레임"은 원래 없고, 유닛 7개의 위치·배율 변경은 새 갱신 종류를 만들지 않는다(크기 불변 결정으로 레이아웃 재계산도 없음). 하위 캔버스 분리는 규모 대비 과함.
- 2026-09-17 **Phase 5 구현 중 확정한 세부 규칙·발견** (계획서에 없던 빈칸을 채운 것):
  - 제품명을 한글(`TAoB 시험판`)로 바꾸자 EditMode 결과 XML 최상위 스위트 이름에 한글이 들어갔고, `run-tests.ps1` 이 PowerShell 5.1 기본값(ANSI)으로 읽어 XML 변환이 실패했다(테스트 자체는 전부 통과). 스크립트를 `-Encoding UTF8` 로 고쳤다. Unity 는 `ProjectSettings.asset` 의 한글 제품명을 `"TAoB \uC2DC\uD5D8\uD310"` 로 다시 직렬화한다(값 동일).
  - 첫 캡처에서 바닥선(`GroundTopY` -60)이 아래열 유닛 박스를 가로지르는 것을 발견 → -190(아래열 유닛의 가장 낮은 글자 약 -180 아래)으로 옮겼다. 에디터 없이 레이아웃 실수를 잡은 첫 사례.
  - 두 번째 캡처가 9초 샷 전에 멈춰 60초 시간 초과로 강제 종료됨 — 창이 포커스를 잃어 플레이어가 멈춘 것(`runInBackground: 0`). 캡처 모드에서만 켜도록 `DevCapture` 에 반영(위 결정 갱신). 프로젝트 전체 설정은 바꾸지 않는다(모바일 백그라운드 동작에 영향이 없도록).
  - `SampleScene` 삭제 시 폴더 메타(`Game/Assets/Scenes.meta`)도 함께 지웠다 — 남기면 Unity 가 빈 폴더 메타 경고를 낸다.
  - 안드로이드 빌드가 `Game/Assets/Settings/` 의 URP 에셋 3개(셰이더 키워드 사전 필터 값)와 `ProjectSettings.asset` 의 `m_BuildTargetBatching` 을 다시 써넣었다. 에디터·파이프라인 자동 갱신이라 값 변경을 검토한 뒤 커밋에 포함한다(Phase 2 URP 자동 업그레이드 선례). Unity 6 빌드 임시 폴더 `Game/.utmp/` 는 `.gitignore` 에 추가.
  - `BuildSummary.totalSize` 는 안드로이드에서 854MB 로 찍혔지만 실제 APK 는 45MB — 중간 산출물 합계라 로그를 실제 파일 크기로 바꿨다.
  - 빌드 후 에디터의 활성 빌드 타깃이 Android 로 남는다. 다음 `win` 빌드가 다시 전환한다(자동, 전환 시 재임포트로 시간이 더 걸림).
- 2026-09-17 **Phase 5 커밋 전 `/plan-review --code` (코드 7 scope + 설정 2(security·perf) + 스크립트 3(quality·security·comments) + 문서 1(comments), PROCEED-WITH-FIXES — CRITICAL 0 · HIGH 2 · MEDIUM 5 · LOW 1 · REVISE 7)** 반영:
  - test·lang(HIGH), arch(MEDIUM): 재시작 누수 검사가 6초 안에 끝나지 않아도 오류 0건이면 스모크가 성공으로 끝남 → 종료 시 검사 미완료면 `[스모크] 재시작 검사 미완료` 오류로 실패(위 결정 갱신).
  - arch(MEDIUM)·comments(REVISE): `DevCapture` 가 `DEVELOPMENT_BUILD` 전용이라 에디터에서 컴파일되지 않아 컴파일 오류가 11분짜리 APK 빌드에서야 드러남, 주석은 "디버그 패널과 같은 구역화"라 적혀 어긋남 → `UNITY_EDITOR || DEVELOPMENT_BUILD` 로 통일(인자가 없으면 아무것도 하지 않으므로 에디터 동작 불변).
  - lang(MEDIUM): 빌드 중 예외 시 실패 표시가 남지 않음 → `BuildTools.Build` 를 try/catch 로 감싸 `[빌드] 실패 — 예외 …` 로 통일.
  - quality(LOW): `"PopupPool"` 이름 문자열이 `PopupPool.cs`·`PlayModeSmoke.cs` 에 중복 → `BattleLayout.PopupPoolName`(공개 상수 — `PopupPool` 이 `internal` 이라 에디터 어셈블리에서 볼 수 없음).
  - comments(REVISE): `BuildTools` "약 18배" → 실측값(854MB 대 45MB)으로, 스크립트 도움말 "수십 분" → 실측 약 12분, 제한 시간 주석의 IL2CPP 서술을 apk 에 한정(Windows 는 Mono), 계획서 Status 줄·빌드 로그 형식 서술(`<바이트>` → MB·분)·`HANDOFF.md` 마지막 커밋과 다음 할 일 갱신.
  - security(코드·설정·스크립트)·perf(코드·설정)·quality(스크립트): 발견 없음. perf(설정)는 URP `m_Prefilter*` 변경이 대부분 스트립을 켜는 방향이라 셰이더 변형·APK 크기를 줄인다고 URP 소스로 확인.
  - 기각: `SwitchActiveBuildTarget` 뒤 도메인 재로드로 빌드가 조용히 끊겨 성공으로 오인된다는 lang HIGH — 스크립트는 `BuildPlayer` 성공 뒤에만 찍히는 `[빌드] 성공` 표시로 판정하므로 끊기면 실패로 나온다. 실제로 활성 타깃이 Windows 인 상태에서 apk 빌드가 전환·완료됐다(45MB 산출).
  - 기각: 팝업 풀 하위 누수를 못 잡는다는 test MEDIUM — 풀은 설계상 파괴하지 않고 회수만 하며, 크기는 재시작 횟수가 아니라 동시 팝업 수(사전 생성 24)에 묶인다. 재시작마다 늘어나는 경로가 없다.
  - 기각: TMP 서브메시 같은 일시 객체로 개수가 흔들려 오탐이 난다는 lang MEDIUM — 두 번 실측 모두 167=167 이고, 흔들리더라도 결과는 거짓 통과가 아니라 시끄러운 실패다. 재발하면 그때 `BattleRoot` 하위만 세는 방식으로 바꾼다.
  - 기각: `GroundTopY` 회귀 테스트 추가 test MEDIUM — `UnitView` 의 글자 오프셋(비공개 리터럴)을 공개 상수로 끌어내야 하는 규모에 비해, 이번에 도입한 exe 자동 캡처가 같은 종류의 레이아웃 실수를 잡는다(P6 규모 보정).
