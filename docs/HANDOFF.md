# 이어받기 — 새 대화에서 이 프로젝트를 이어갈 때 먼저 읽는 문서

> **이 문서의 역할**: 지금 어디까지 했고 다음에 무엇을 하는지, 그리고 어느 문서가 무엇의 정본인지만 적는다.
> 사실(스택·경로·명령)은 여기에 적지 않는다 — `docs/references/project_facts.md` §0 과 `docs/references/stacks/unity_csharp.md` §0 이 정본이다(`docs/references/README.md` P4).

## 1. 지금 상태 (2026-10-08)

| 항목 | 값 |
|---|---|
| 무엇을 만드는가 | The Age of Bastards — 덱세팅 횡스크롤 반자동 방치형 RPG(2D). 지금은 **횡스크롤 이동 전투**(등장 → 행군 → 조우 4회, 마지막이 보스)를 만들어 "진행과 개입이 재밌나"를 검증하는 단계 |
| 진행 | 수직 슬라이스(정적 2열 대치)는 Phase 0~5 완료. 사용자가 **"전투 진행이 생각과 다르다 — 횡스크롤 이동 전투여야 한다"**(2026-09-29)고 판단해 방향을 전환했다. 새 계획의 **Phase 0·A·B 는 커밋 완료, Phase C(선호와 규율)는 구현·검증 완료·커밋 대기**다. **1차 재미 판정(갤럭시 S20 울트라, 2026-10-08)은 "지금은 재미를 느낄 수가 없다 — 사각형이 등장에서 조금 움찔하다가 끝난다"** 였고, 사용자가 Phase C 로 계속 진행하기로 했다. **다음 = Phase C 폰 확인 → Phase D 개입 수단** |
| 마지막 커밋 | `fa4901b` fix: 해상도·세이프 에어리어에 맞춰 UI 를 배치한다 (하단 버튼 잘림) — Phase B 는 `c2fd6ab` |
| 자동 검증 상태 | EditMode 테스트 427개 통과 · 커버리지 `TAoB.Combat` 99% · `TAoB.Data` 99.1% · 배치모드 플레이 스모크 오류 0건 · 재시작 5회 누수 검사 통과. 실값 40시드 측정(승리 30/40 · 평균 35초 · 침투로 뒷열이 맞은 판 37/40)은 계획서 Phase C 검증 절 |
| 진행 상황 정본 | `.claude/plans/sidescroll-battle.plan.md` — 헤더 `**Status**` 줄과 각 Phase 의 "검증" 체크가 정본. 결정 이력은 문서 하단 `## 결정 로그`. 끝난 계획은 `.claude/plans/completed/` |

## 2. 다음에 할 일

1. **Phase C 폰 확인** — 새 APK 로 한 판을 본다. 이번에 달라진 것: 동료·적 머리 위에 **성향 표식**(보호·돌진·후방·비겁·침투)이 뜨고, 돌진은 앞으로 나서고 비겁·후방은 뒤로 서며, 적 앞열이 가끔 **앞으로 나와 뒷열(마법사)을 때린다**. 볼 것: 이 변화로 "움찔하다 끝난다"가 나아졌나 / 누가 뭘 하는지 보이나 / 침투가 위협으로 읽히나. 성능 표시(왼쪽 위)도 읽어 둔다 — 끊김 30회 초과면 계획서 Phase F-2.
2. **알려진 밸런스 문제(Phase E 소관)**: 기사·초감각 인간이 거의 매판 쓰러진다(40/40, 39/40). 승리 30/40 이라 판은 이기지만 앞열이 늘 전멸한다 — 재미 판정 결과를 보고 조정한다.
3. 이후 **Phase D 개입 수단**(구조 5초·소모품·강제 기술 연출) → **Phase E 60초 밸런스**. 이동·편성 수치는 구현 중 측정으로 잡은 **출발값**이다(행군 40fu/틱 · 사거리 앞 440/뒤 680 · 여유 120 · 접근 2000).

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
./Tools/run-editor-tool.ps1 -Tool win    # Windows 개발 빌드 → Game/Builds/Windows/TAoB.exe
./Tools/run-editor-tool.ps1 -Tool apk    # 안드로이드 개발 빌드 → Game/Builds/Android/TAoB-slice.apk
# 연출 확인용 자동 캡처(에디터 없이): TAoB.exe -captureShots <폴더> -screen-fullscreen 0 → 2·7·20·40·55초 PNG 후 자동 종료
# 해상도·노치 확인(개발 빌드): -screen-width 2400 -screen-height 1080 -safeInsets <왼> <아래> <오른> <위>  (px, 세이프 에어리어 시뮬레이션)
```

스크립트가 담고 있는 배치모드 주의점(`-quit` 를 붙이는 경우와 아닌 경우, `Start-Process -Wait` 가 멈추는 문제 등)은 `project_facts.md` §0 "테스트 실행" 이 정본이다.
