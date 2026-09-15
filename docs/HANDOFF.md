# 이어받기 — 새 대화에서 이 프로젝트를 이어갈 때 먼저 읽는 문서

> **이 문서의 역할**: 지금 어디까지 했고 다음에 무엇을 하는지, 그리고 어느 문서가 무엇의 정본인지만 적는다.
> 사실(스택·경로·명령)은 여기에 적지 않는다 — `docs/references/project_facts.md` §0 과 `docs/references/stacks/unity_csharp.md` §0 이 정본이다(`docs/references/README.md` P4).

## 1. 지금 상태 (2026-09-15)

| 항목 | 값 |
|---|---|
| 무엇을 만드는가 | The Age of Bastards — 덱세팅 횡스크롤 반자동 방치형 RPG(2D). 지금은 **전투 1판짜리 수직 슬라이스**로 "자동 전투를 보다가 개입하는 게 재밌나"를 검증하는 단계 |
| 진행 | Phase 0~4 구현·커밋 완료. **다음 = Phase 4 수동 시나리오 8개(사용자 수행) → Phase 5 안드로이드 빌드·재미 판정** |
| 마지막 커밋 | `5250269` feat: 전투 화면과 개입 UI 추가 (Phase 4) |
| 자동 검증 상태 | EditMode 테스트 214개 통과 · 배치모드 플레이 스모크 오류 0건 |
| 진행 상황 정본 | `.claude/plans/vertical-slice.plan.md` — 헤더 `**Status**` 줄과 각 Phase 의 "검증" 체크가 정본. 결정 이력은 문서 하단 `## 결정 로그` |

## 2. 다음에 할 일

1. **사용자가 에디터에서 수동 시나리오 8개를 확인한다** — 목록은 계획서 `## Phase 4` 의 "검증" 절. Unity Hub 에서 `Game` 프로젝트를 열고 `Assets/_Project/Scenes/Battle.unity` 를 플레이한다.
2. 시나리오 결과를 계획서 Phase 4 "검증" 에 기록하고, 고칠 것이 나오면 고친 뒤 커밋(커밋마다 `/plan-review --code`).
3. Phase 5 착수 — 안드로이드 빌드와 재미 판정 5문항. Phase 5 절에 미리 적어 둔 보류 항목도 이때 처리한다(TMP 이모지 에셋이 `Resources` 에 남아 빌드에 포함되는 문제, 템플릿 잔재 `Game/Assets/Scenes/SampleScene.unity` 정리).

## 3. 일하는 방식 (이 프로젝트의 규칙)

- 워크플로·리뷰·커밋 규칙의 정본은 저장소 루트 `CLAUDE.md` 다(git 에서 제외되어 있으니 파일을 직접 읽는다). 요약: 계획 문서를 먼저 쓰고 `/plan-review` 로 병렬 적대 리뷰, **커밋마다 `/plan-review --code`**, **커밋 메시지는 사용자 최종 승인 후 실행**, `git push` 는 사용자가 따로 승인.
- 리뷰 범위는 매번 사용자에게 묻되, 지금까지 고른 방식은 **"코드 전체 7 scope + 설정·문서 버킷 축소"** 다.
- 리뷰 참조 문서 체계는 `docs/references/README.md` 가 설명한다(코어 6종 + 스택 문서 + `project_facts.md`).
- 기획 정본은 저장소 루트의 가장 높은 버전 `The_Age_of_Bastards_게임기획서_v*.md` · `The_Age_of_Bastards_세계관_설정집_v*.md`. `PROJECT_CONTEXT_백업.md` 는 폐기된 이전 컨셉이라 절대 근거로 쓰지 않는다.

## 4. 코드 구조 한눈에

참조 방향은 한 방향이다: `TAoB.View`(표시) → `TAoB.Data`(로더) → `TAoB.Combat`(규칙, 엔진 참조 차단).

| 어셈블리 | 위치 | 하는 일 |
|---|---|---|
| `TAoB.Combat` | `Game/Assets/_Project/Scripts/Combat/` | 전투 규칙 전부. 초당 10틱 고정, 정수 연산만, 난수는 주입. 엔진 API 를 쓰지 않아 테스트가 쉽다 |
| `TAoB.Data` | `Scripts/Data/` | `Resources/Data/*.json` 을 읽어 검증하고 규칙 모듈 타입으로 바꾼다. 실패는 파일명·필드 경로를 담은 예외 |
| `TAoB.View` | `Scripts/View/` | 화면·입력. 씬에는 `BattleBootstrap` 하나만 있고 나머지는 코드로 만든다 |
| `TAoB.EditorTools` | `Editor/` | 씬 생성·TMP 리소스 가져오기·플레이 스모크. 에디터 전용 |
| 테스트 | `Tests/EditMode/{Combat,Data,View}/` | 폴더당 어셈블리 1개 |

밸런스 수치는 코드에 없다 — `Game/Assets/_Project/Resources/Data/balance.json` 등 네 파일에 있고, 범위 검증의 정본은 규칙 모듈 생성자다.

## 5. 자주 쓰는 명령

에디터를 닫은 상태에서 실행한다(열려 있으면 프로젝트 잠금으로 실패).

```powershell
./Tools/run-tests.ps1                    # EditMode 테스트 전체
./Tools/run-tests.ps1 -Coverage          # 커버리지까지
./Tools/run-tests.ps1 -Filter TAoB.View.Tests
./Tools/run-editor-tool.ps1 -Tool smoke  # 전투 씬 6초 플레이, 오류 0건이면 종료 코드 0
./Tools/run-editor-tool.ps1 -Tool scene  # 전투 씬 재생성 (기존 씬·빌드 목록을 덮어쓴다)
```

스크립트가 담고 있는 배치모드 주의점(`-quit` 를 붙이는 경우와 아닌 경우, `Start-Process -Wait` 가 멈추는 문제 등)은 `project_facts.md` §0 "테스트 실행" 이 정본이다.
