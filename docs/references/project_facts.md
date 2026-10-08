# 프로젝트 사실 (정본)

> 리뷰어와 메인 에이전트가 전제로 삼는 사실입니다. 스택·도구·경로가 바뀌면 이 문서와 스택 문서 §0 만 고칩니다(README P4).

## 0. 프로젝트 사실

| 항목 | 값 |
|---|---|
| 제품 | The Age of Bastards — 덱세팅 횡스크롤 반자동 방치형 RPG (2D) |
| 수익 모델 | 프리미엄(1회 구매). 광고·인앱결제·서버 없음 |
| 대상 플랫폼 | Android(1차) → iOS → PC(Steam/Epic) |
| 화면 | 가로 고정, 기준 해상도 1920×1080 |
| 엔진 / 언어 | Unity 6.3 LTS / C# |
| 개발 형태 | 1인(비개발자) + AI 협업 |
| 현재 단계 | 횡스크롤 이동 전투 전환 — 등장·행군·조우 4회(마지막이 보스). 계획: `.claude/plans/sidescroll-battle.plan.md`. 끝난 계획은 `.claude/plans/completed/` |
| 계획서 호칭 | 코드 주석의 **"계획서"** 와 `D1~D14`·`Phase A~F` 는 활성 계획서(위 경로)를 뜻한다. **"수직 슬라이스 계획서 Phase 0~5"** 는 완료된 `.claude/plans/completed/vertical-slice.plan.md` 다 — 두 문서가 같은 낱말을 쓰지 않도록 숫자 Phase 참조에는 접두를 붙인다 |
| 이어받기 문서 | `docs/HANDOFF.md` — 새 대화에서 먼저 읽는다. 진행 상태·다음 할 일·문서 지도를 담고, 사실은 이 문서를 가리킨다 |
| 편의 스크립트 | `Tools/run-tests.ps1`(테스트·커버리지), `Tools/run-editor-tool.ps1 -Tool scene\|tmp\|smoke\|win\|apk`(에디터 도구·개발 빌드 — 산출물 `Game/Builds/`, git 제외). 아래 "테스트 실행" 명령을 그대로 실행한다. PowerShell 5.1 이 BOM 없는 파일을 ANSI 로 읽어 한글이 깨지므로 UTF-8 BOM 으로 저장한다 |
| 기획 정본 | 저장소 루트의 가장 높은 버전 `The_Age_of_Bastards_게임기획서_v*.md` · `The_Age_of_Bastards_세계관_설정집_v*.md` |
| 폐기 문서 | `PROJECT_CONTEXT_백업.md` — 이전 컨셉. 리뷰 근거로 쓰지 않는다 |
| 저장소 레이아웃 | `Game/` Unity 프로젝트 · `Game/Assets/_Project/` 자체 코드·데이터 · `docs/references/` 리뷰 표준 · `docs/HANDOFF.md` 이어받기 · `Tools/` 실행 스크립트 · 루트 `*.md` 기획 문서 |
| 코드 위치 | 규칙 모듈 `Game/Assets/_Project/Scripts/Combat/` · 데이터 로더 `Scripts/Data/` · 표시 계층 `Scripts/View/` · 테스트 `Game/Assets/_Project/Tests/EditMode/Combat/`(규칙 모듈)·`Tests/EditMode/Data/`(데이터 로더)·`Tests/EditMode/View/`(표시 계층 중 엔진 비의존 부분: 틱 드라이버·배치·문구) · 에디터 도구 `Game/Assets/_Project/Editor/` |
| 검증 방식 | 규칙 모듈·데이터 로더 = EditMode 자동 테스트(규칙 모듈 커버리지 80% 이상) · 표시 계층 = 활성 계획서의 수동 시나리오 + 배치모드 플레이 스모크(`-executeMethod TAoB.EditorTools.PlayModeSmoke.Run`, `-quit`·`-nographics` 없이 — 6초 플레이 후 오류 0건이면 exit 0) |
| Unity 실행 파일 | `E:\Unity\Editors\<버전>\Editor\Unity.exe` — `<버전>` 은 `Game/ProjectSettings/ProjectVersion.txt` 의 `m_EditorVersion` |
| Unity Hub 명령줄 | Hub 는 winget MSIX 로 설치됨. 실행 파일 = `(Get-AppxPackage UnityTechnologies.UnityHub).InstallLocation` + `\app\Unity Hub.exe`, 호출 형식 `& "<Unity Hub.exe>" -- --headless <명령>`. Hub 에 내장된 `app\resources\cli\unity.exe` 는 WindowsApps 권한 제한으로 직접 실행 불가 |
| 라이선스 | Unity Personal — Hub 로그인 시 자동 활성화. 라이선스 파일은 MSIX 가상화 폴더(`%LOCALAPPDATA%\Packages\UnityTechnologies.UnityHub_*\LocalCache\Local\Unity\licenses`)에 있지만 batchmode 에디터가 정상 인식함(2026-09-11 확인) |
| Android 모듈 ID (6000.3.24f1) | `android`, `android-sdk-ndk-tools`, `android-open-jdk-17.0.18+8` — OpenJDK ID 에 버전이 붙는다 |
| 테스트 실행 | 에디터를 닫은 상태에서 `"<Unity.exe>" -batchmode -projectPath Game -runTests -testPlatform EditMode -testResults Game/results-editmode.xml` — `-quit` 는 붙이지 않는다. PowerShell 에서 `Start-Process -Wait` 를 쓰면 Unity 보조 프로세스까지 기다려 멈춘 것처럼 보이므로 `-PassThru` 후 `WaitForExit()` 로 본체만 기다린다. 컴파일 오류가 있으면 결과 파일 없이 종료된다. `-PassThru` 로 받은 프로세스의 `ExitCode` 는 비어 돌아오는 경우가 있어 성공 판정에 쓰지 않는다 — 결과 XML 이나 도구가 로그에 남긴 완료 표시로 판정한다 |
| 커밋 규칙 | Conventional Commits, 메시지 한국어, 커밋 전 사용자 승인 (CLAUDE.md) |
| 보안 주의 | 릴리스 서명 키(`*.keystore`, `*.jks`)와 그 비밀번호는 저장소·코드에 두지 않는다 |
| CLAUDE.md 참고 | 「프로젝트 스택」의 fastlane·`MARKETING_VERSION` 줄은 이전 프로젝트 잔재로 이 프로젝트에 적용되지 않는다. 이 문서가 우선한다 |

## 0-A. 활성 스택 표

변경 파일을 위에서 아래로 **first-match** 로 배정한다.

| 순서 | glob | 스택 ID | 스택 문서 | `lang` 에이전트 |
|---|---|---|---|---|
| 1 | `Game/Assets/**/*.cs` | unity-csharp | `stacks/unity_csharp.md` | `csharp-reviewer` |
| 2 | `Game/Assets/**/*.{asmdef,json,unity,prefab,asset,meta}` · `Game/ProjectSettings/**` · `Game/Packages/**` | unity-assets | `stacks/unity_csharp.md` | — |
| 3 | `*.md` · `docs/**` · `.claude/**` · `.gitignore` | docs | `stacks/unity_csharp.md` | — |
| 4 | 그 외 | (미분류) | `stacks/unity_csharp.md` | — |

> 2~4번도 스택 문서를 지정하는 이유: 코어 scope 리뷰어가 §0 전제 표를 읽어야 전제 게이트를 판정할 수 있다. `lang` 에이전트가 `—` 인 버킷은 `lang` scope 만 제외한다.

## 0-B. 도메인 용어집

코드 식별자는 아래 영문 이름을 쓴다. 기획서 용어와 1:1 로 대응해야 하며, 다른 이름을 새로 만들면 quality §1.6 판정 대상이다.

| 기획 용어 | 식별자 | 출처 |
|---|---|---|
| 공격력 · 방어력 · 생명력 · 속도 | `Atk` · `Def` · `Hp` · `Spd` | 기획서 4-1 |
| 마나 친화 | `Mnd` | 기획서 4-1 |
| 반발력 내성 | `Res` | 기획서 4-1 |
| 원석 촉매 | `Catalyst` | 기획서 4-1 |
| 마도구 | `Magitool` | 기획서 4-1 |
| 내구력 | `Durability` | 기획서 4-1 |
| 행동 게이지 | `ActionGauge` | 기획서 4-1 |
| 진형 · 앞열 · 뒷열 | `Formation` · `Front` · `Back` | 기획서 4-1 |
| 직업 상성 | `Affinity` | 기획서 4-1 |
| 크리티컬 | `Critical` | 기획서 4-1 |
| 빈사 | `Downed` | 기획서 4-1 |
| 기본 공격 | `BasicAttack` | 기획서 4-1 |
| 고유 기술 · 재사용 대기 | `Skill` · `SkillDefinition` · `Cooldown` | 기획서 4-1 (재사용 대기는 기획서에 없고 `.claude/plans/completed/vertical-slice.plan.md` Phase 2 에서 정의. `Cooldown` 은 Phase A 부터 **강제 발동 전용** 쿨타임이 된다 — 자동 사용은 쿨타임을 보지 않는다(D7)) |
| 과부하 발동 · 과부하 위험 단계 | `Overload` · `OverloadRisk` | 기획서 4-1 |
| 흩어짐 · 폭주 · 한계돌파 | `Scatter` · `Runaway` · `Breakthrough` | 설정집 5-2~5-4 |
| 구조 | `Rescue` | 기획서 4-1 |
| 후퇴 | `Retreat` | 기획서 3장 |
| 사전 설정 조건 | `Precondition` | 기획서 3장 |
| 전장 위치 | `PosX` | 횡스크롤 전환 D3 (단위 fu, 계획: `.claude/plans/sidescroll-battle.plan.md`) |
| 진형 기준점 | `AnchorX` · `EnemyAnchorX` | 횡스크롤 전환 D1·D2 (단위 fu) |
| 사거리 | `ReachFu` | 횡스크롤 전환 D4 (`Formation` 이 Row 로 고른다. 데이터 키는 `movement.frontReachFu`·`movement.backReachFu`) |
| 진형 자리 간격 | `SlotOffsetFu` | 횡스크롤 전환 D4 (데이터 키는 `movement.frontSlotOffsetFu`·`movement.backSlotOffsetFu`) |
| 행군 속도 | `MarchSpeedFu` | 횡스크롤 전환 D2 (fu/틱, 데이터 키 `movement.marchSpeedFu`) |
| 교전 여유 | `EngageMarginFu` | 횡스크롤 전환 D2 (사거리 경계보다 안쪽에 서는 거리. 교전 판정은 `사거리 − 여유` 를 본다) |
| 교전 | `Engagement` | 횡스크롤 전환 D5·D6 — 사거리 쌍 술어. 전투 한 판의 구획인 **조우**(`Wave`)와 다른 개념이다 |
| 스크롤 층 · 진형 화면 위치 | `Lane`(표시 계층의 `LaneView`) · `AllyAnchorScreenX` | 횡스크롤 전환 D12 — 표시 계층의 중첩 캔버스. 규칙 모듈 설정의 `Balance.Lane`(이동 수치 묶음)과 **다른 개념**이다. 전장 좌표 → 화면 환산은 `PxPerFu` 한 지점 |
| 조우 접근 거리 | `WaveApproachFu` · `ApproachFu` | 횡스크롤 전환 (조우마다 그 적을 만나기까지 행군하는 거리. 적 기준점은 누적 합) |
| 진형유지 속성값 | `FormationChance` | 횡스크롤 전환 Phase C (만분율 0~10000, 숨은 스탯 — 조우마다 통과해야 성향대로 움직인다) |
| 선호 | `Preference` | 횡스크롤 전환 Phase C (없음 `None` + 동료 보호 `Guardian`·적극 돌진 `Charger`·후방 유지 `Rearguard`·비겁 `Coward`) |
| 기술 사용 빈도 | `SkillChance` | 횡스크롤 전환 D7 (만분율, 기획 대역 100~3900. 숨은 스탯이라 `Stats` 밖에 둔다) |
| 자세 · 침투 지정 | `Stance`(`Hold`·`Forward`·`Charge`·`HoldBack`·`StepBack`) · `IsFlanking` | 횡스크롤 전환 Phase C (조우 시작 때 한 번 정한다). 화면의 성향 표식 글자는 보호·돌진·후방·비겁·침투 |
| 뒤로 서기(비겁 성향의 이동) | `StepBack` · `StepBackFu` | 횡스크롤 전환 Phase C. 사용자 명령 `Retreat`(후퇴)와 다른 개념 |
| 진형 이탈 거리 · 돌진 거리 · 침투 확률 | `FormationDriftFu`(보호는 앞·후방은 뒤) · `ChargeDriftFu` · `FlankChance` | 횡스크롤 전환 Phase C (데이터 키는 `movement.*`) |
| 도발 | `IsTaunting`(= `Stance.Forward`) | 횡스크롤 전환 Phase C — 동료 보호를 따르기로 한 유닛. 사거리 안이면 상대편의 단일 대상 공격이 먼저 이쪽을 향한다 |
| 조우 | 코드·데이터·UI 는 `Wave` 유지 | 횡스크롤 전환 D14 (개명은 Phase F-1) |
| 제한 시간 | `TimeLimit` | 기획서 3장 |
