# 이어받기 — 새 대화에서 이 프로젝트를 이어갈 때 먼저 읽는 문서

> **이 문서의 역할**: 지금 어디까지 했고 다음에 무엇을 하는지, 그리고 어느 문서가 무엇의 정본인지만 적는다.
> 사실(스택·경로·명령)은 여기에 적지 않는다 — `docs/references/project_facts.md` §0 과 `docs/references/stacks/unity_csharp.md` §0 이 정본이다(`docs/references/README.md` P4).

## 1. 지금 상태 (2026-09-17)

| 항목 | 값 |
|---|---|
| 무엇을 만드는가 | The Age of Bastards — 덱세팅 횡스크롤 반자동 방치형 RPG(2D). 지금은 **전투 1판짜리 수직 슬라이스**로 "자동 전투를 보다가 개입하는 게 재밌나"를 검증하는 단계 |
| 진행 | Phase 0~4.5 구현·커밋 완료. 사용자 PC 에서 에디터가 너무 느려 시연을 빌드(exe·APK)로 전환했다(2026-09-15). Phase 5 빌드 도구 구현·빌드 완료(Windows exe·APK 45MB), 커밋 전 `/plan-review --code` 반영 완료. **다음 = 커밋 승인 → 사용자가 폰에서 시나리오·재미 판정** |
| 마지막 커밋 | `6e49bce` feat: 전투 연출 보강 추가 (Phase 4.5). Phase 5 는 아직 커밋 전 |
| 자동 검증 상태 | EditMode 테스트 226개 통과 · 배치모드 플레이 스모크 오류 0건 · 재시작 5회 누수 검사 통과(오브젝트 167개 유지) |
| 진행 상황 정본 | `.claude/plans/vertical-slice.plan.md` — 헤더 `**Status**` 줄과 각 Phase 의 "검증" 체크가 정본. 결정 이력은 문서 하단 `## 결정 로그` |

## 2. 다음에 할 일

1. **Phase 5 커밋** — 커밋 메시지 사용자 승인 후 실행. 산출물(`Game/Builds/`)은 git 에 들어가지 않으니 필요하면 `run-editor-tool.ps1 -Tool apk` 로 다시 만든다.
2. **사용자가 APK 를 폰으로 옮겨 설치**(USB 디버깅 없이 파일 복사)하고 Phase 4 시나리오 1~6·Phase 4.5 시나리오 9 와 재미 판정 5문항을 수행 → 결과를 계획서·기획서에 기록.

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
# 연출 확인용 자동 캡처(에디터 없이): TAoB.exe -captureShots <폴더> -screen-fullscreen 0 → 2·5·9초 PNG 후 자동 종료
```

스크립트가 담고 있는 배치모드 주의점(`-quit` 를 붙이는 경우와 아닌 경우, `Start-Process -Wait` 가 멈추는 문제 등)은 `project_facts.md` §0 "테스트 실행" 이 정본이다.
