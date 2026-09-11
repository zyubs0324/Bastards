# 첫 수직 슬라이스(전투 프로토타입) 착수 계획

**Review Scope**: arch, lang, quality, security, perf, test, comments
**Status**: 진행 중 — Phase 0 완료 (2026-09-11: Unity 6000.3.24f1 + Android 모듈 설치, `Game/` Universal 2D 프로젝트 생성, batchmode 열기 검증 exit 0, 캐시 폴더 git 제외 확인) · Phase 1 완료 (1차 `/plan-review` 와 첫 커밋 전 `/plan-review --code` 모두 PROCEED-WITH-FIXES, 지적 전부 결정에 반영) · Phase 2 구현·테스트 완료 (2026-09-11, 커밋 전 리뷰·재리뷰 지적 반영 완료 — 테스트 148개 통과·커버리지 98.9%, 커밋 승인 대기)

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
- `Game/Assets/_Project/Resources/Data/` — `balance.json`(`BalanceConfig` 전 항목), `characters.json`(아군 3·적 3·보스 1, 스탯·직업·`SkillDefinition`·장비), `equipment.json`(원석 2 + 마도구 5), `stage_slice.json`(웨이브 2개, 제한 시간(초), `Precondition`)
- `Game/Assets/_Project/Scripts/Data/` — `TAoB.Data.asmdef` + DTO + `GameDataLoader`
- `Game/Assets/_Project/Tests/EditMode/Data/TAoB.Data.Tests.asmdef` + 로더 테스트

**결정**
- `JsonUtility` 호환 구조(딕셔너리 대신 배열).
- 로더는 별도 `TAoB.Data` 어셈블리에 둔다(참조 방향 View → Data → Combat). DTO 를 Combat 자체 타입(`BalanceConfig` 등)으로 변환해 넘긴다.
- **로드 시점 검증** — 하나라도 실패하면 로드를 중단하고 파일명·필드명·값을 오류에 담는다:
  - 필수 필드 누락: DTO 필드 초기값을 센티널(예: -1)로 두고 검사. `JsonUtility` 가 누락 필드에 초기값을 유지하는지는 로더 첫 테스트로 확인한다.
  - 음수·0 불가 값
  - 존재하지 않는 ID 참조
  - 상한 초과: 제한 시간 ≤ `BattleSetup.MaxTimeLimitTicks`(600초), 스탯·장비·밸런스 수치 ≤ `CombatScale.MaxValue`, 배율 ≤ `CombatScale.MaxPermille`, 확률 0~10000, 과부하 확률표 합 = 10000 — 이 상한들은 규칙 모듈 생성자가 정본이며 로더는 같은 상수로 먼저 걸러 파일명·필드명을 붙인다. 웨이브 ≤ 5, 웨이브당 적 ≤ 5 는 로더 상수(밸런스 값 아님).

**검증**
- 로더 EditMode 테스트: 정상 파일 로드 1 + 필드 누락·음수·미존재 ID 각 1 + 상한 규칙 5종(제한 시간, 웨이브 수, 웨이브당 적 수, 확률 범위, 과부하 확률표 합) 각 1.
- 사용자가 `balance.json` 의 K 값을 바꾸고 재생했을 때 데미지 숫자 변화 확인.

## Phase 4 — 전투 화면
**목표**: 가로 화면에서 전투를 보고 터치로 개입할 수 있다.

**변경 파일**
- `Game/Assets/_Project/Scripts/View/TAoB.View.asmdef` — `BattleBootstrap`, `UnitView`, `BattleHud`, `InterventionBar`, `ResultPanel`, `DebugLoadoutPanel`, `PopupPool`
- `Game/Assets/_Project/Scenes/Battle.unity` (오브젝트는 `BattleBootstrap` 1개)
- `Game/Assets/_Project/Fonts/` — 한글 OFL 폰트 TMP 에셋

**결정**
- 씬에는 `BattleBootstrap` 만 두고, 전투 표시 객체는 코드로 하나의 `BattleRoot` 아래에 생성한다(`PopupPool` 만 예외로 `BattleBootstrap` 직속) → 에디터 수작업·씬 YAML 충돌 최소화.
- **틱 드라이버**: `BattleBootstrap.Update` 가 누적 시간 × 10 × 배속만큼 `Step()` 을 호출한다. 프레임당 최대 20틱, 넘치는 틱은 다음 프레임으로 넘긴다. 배속은 호출 횟수만 바꾸고 규칙 모듈에 전달하지 않는다.
- **이벤트 전달**: `Step()` 1회가 끝날 때마다 `BattleBootstrap` 이 이벤트 버퍼를 곧바로 뷰들에 직접 호출로 넘긴 뒤 다음 `Step()` 을 부른다. 한 프레임에 여러 틱을 돌려도 이벤트가 덮여 사라지지 않는다. 전투 이벤트에는 구독(발행자) 구조를 두지 않는다.
- **표시 갱신**: 스냅샷 값이 바뀐 요소만 텍스트를 갱신한다(TMP `SetText` 수치 오버로드). 데미지 숫자·이펙트는 `PopupPool` 로 재사용한다.
- **재시작**: `BattleRoot` 를 파괴하고 `BattleSimulation` 을 새로 만든다. `PopupPool` 은 파괴하지 않고 전부 회수한다. 버튼 리스너 등 이벤트 구독은 `BattleRoot` 하위 객체만 하고 각자의 `OnDestroy` 에서 해제한다.
- 그래픽은 색 네모 + 직업 글자. HP 바·행동 게이지 바·데미지 숫자·과부하 위험 단계 표시·빈사 표시.
- uGUI + TextMeshPro, 기준 해상도 1920×1080 가로 고정, Scale With Screen Size, 버튼 터치 영역 넉넉히.
- 한글 폰트: 기본 폰트는 한글 미지원 → Noto Sans KR(OFL). 폰트 파일 다운로드는 사용자 승인 후.
- **개입 바**: 캐릭터별 고유 기술 버튼 3(`UseSkill`) + 과부하 버튼(마법사, 누를 때마다 `Overload` 명령 1회) + 구조 버튼(흩어짐 아군이 있을 때만 활성, `Rescue`) + 후퇴(`Retreat`). 배속 1x/2x. 기본 공격은 자동 전용이라 버튼이 없다.
- 결과 화면: 승패 사유, 빈사자, 마도구 내구력 소모, 결과 해시, 재시작.
- `DebugLoadoutPanel`: 캐릭터별 장비 교체 후 재시작(2축 규칙 체감용, 덱세팅 화면 아님). `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 안에서만 생성한다.

**검증** (에디터 수동 시나리오, 시드 고정으로 재현)
1. 기본 편성 승리 → 결과 화면
2. 제한 시간 초과 → 자동 후퇴 패배
3. 수동 후퇴
4. 사전 조건(빈사 금지) 위반 패배
5. 과부하 성공·흩어짐(구조로 해제)·폭주·한계돌파 각각 발생
6. RES 미달 마도구 장착 시 자해 피해 표시
7. 입력 없이(자동만) 배속 2x 로 돌린 결과 해시가 1x 와 동일(같은 시드)
8. 재시작 5회 후 씬의 `GameObject` 수가 첫 전투와 동일

## Phase 5 — 안드로이드 빌드 & 재미 판정
**목표**: 사용자 폰에서 플레이해 계속 갈지 판단한다.

**변경 파일**
- `Game/ProjectSettings/` (Player Settings: 가로 고정, 패키지명, 최소 API)
- 기획서 v0.6 13장·4-1 (판정 결과 반영)

**결정**
- Development Build 로 디버그 서명 APK 를 만든다(디버그 패널 포함, 릴리스 키스토어는 출시 단계에서). 패키지명은 빌드 직전 사용자 확인.
- 재미 판정 질문: ① 개입하고 싶은 순간이 있었나 ② 자동만 볼 때 지루했나 ③ 과부하에 긴장감이 있었나 ④ 장비 교체가 결과를 바꿨나 ⑤ 한 판 길이가 적당했나.

**검증**
- USB 설치 후 Phase 4 시나리오 1·2·5를 폰에서 재현.
- 같은 시드로 입력 없이(자동만) 돌린 결과 해시가 에디터와 폰에서 같음.
- 질문 답변을 기획서에 기록하고, 다음 단계(덱세팅 추가 / 규칙 수정 / 방향 전환)를 사용자와 결정.

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
