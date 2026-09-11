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
| 현재 단계 | 첫 수직 슬라이스 — 전투 1판 프로토타입. 계획: `.claude/plans/vertical-slice.plan.md` |
| 기획 정본 | 저장소 루트의 가장 높은 버전 `The_Age_of_Bastards_게임기획서_v*.md` · `The_Age_of_Bastards_세계관_설정집_v*.md` |
| 폐기 문서 | `PROJECT_CONTEXT_백업.md` — 이전 컨셉. 리뷰 근거로 쓰지 않는다 |
| 저장소 레이아웃 | `Game/` Unity 프로젝트 · `Game/Assets/_Project/` 자체 코드·데이터 · `docs/references/` 리뷰 표준 · 루트 `*.md` 기획 문서 |
| 코드 위치 | 규칙 모듈 `Game/Assets/_Project/Scripts/Combat/` · 데이터 로더 `Scripts/Data/` · 표시 계층 `Scripts/View/` · 테스트 `Game/Assets/_Project/Tests/EditMode/` |
| 검증 방식 | 규칙 모듈·데이터 로더 = EditMode 자동 테스트(규칙 모듈 커버리지 80% 이상) · 표시 계층 = 활성 계획서의 수동 시나리오 |
| Unity 실행 파일 | `C:\Program Files\Unity\Hub\Editor\<버전>\Editor\Unity.exe` — `<버전>` 은 `Game/ProjectSettings/ProjectVersion.txt` 의 `m_EditorVersion` |
| 테스트 실행 | 에디터를 닫은 상태에서 `"<Unity.exe>" -batchmode -projectPath Game -runTests -testPlatform EditMode -testResults Game/results-editmode.xml` |
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
| 고유 기술 · 재사용 대기 | `Skill` · `SkillDefinition` · `Cooldown` | 기획서 4-1 (재사용 대기는 기획서에 없고 `.claude/plans/vertical-slice.plan.md` Phase 2 에서 정의) |
| 과부하 발동 · 과부하 위험 단계 | `Overload` · `OverloadRisk` | 기획서 4-1 |
| 흩어짐 · 폭주 · 한계돌파 | `Scatter` · `Runaway` · `Breakthrough` | 설정집 5-2~5-4 |
| 구조 | `Rescue` | 기획서 4-1 |
| 후퇴 | `Retreat` | 기획서 3장 |
| 사전 설정 조건 | `Precondition` | 기획서 3장 |
| 제한 시간 | `TimeLimit` | 기획서 3장 |
