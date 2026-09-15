# 스택 문서 — unity-csharp

> 코어 6종의 논리명이 이 스택에서 무엇인지(§1~§6), 언어·엔진 고유 결함이 무엇인지(§7), diff 에서 scope 를 어떻게 추론하는지(§8)를 정합니다. 전제 값의 정본은 §0 입니다.

## 0. 전제 표

`—` 는 해당 전제가 꺼졌다는 뜻입니다(README P3). 빈칸·TODO 는 두지 않습니다.

| 전제 | 값 | 비고 |
|---|---|---|
| 엔진 | Unity 6.3 LTS | 정확한 패치 버전은 `Game/ProjectSettings/ProjectVersion.txt` |
| 언어 | C# 9 | `record`·`init` 전용 세터는 쓰지 않는다(런타임 지원 제약) |
| 스레딩 모델 | 메인 스레드 단일 | 백그라운드 스레드·Job System 미사용 |
| 시간 모델 | 규칙 = 고정 틱(초당 10틱, 정수 틱 카운트) · 표시 = 직전·현재 틱 스냅샷 사이 보간 | |
| 수치 표현 | 규칙 모듈 = 정수만(`float` 없음). 배율은 ×1000 정수, 확률은 만분율(0~10000) | 에디터·기기 간 결정론 |
| 난수 | 규칙 모듈은 `IRandom` 주입만 사용 | `SeededRandom` 은 자체 정수 알고리즘으로 구현. `UnityEngine.Random`·`System.Random` 사용 금지 |
| 모듈 구성 | `TAoB.Combat`(규칙, 엔진 참조 차단) ← `TAoB.Data`(로더) ← `TAoB.View`(표시) | 화살표는 참조 방향의 역. View→Data→Combat 단방향 |
| 직렬화 | `JsonUtility` | |
| 데이터 파일 위치 | `Game/Assets/_Project/Resources/Data/*.json` | `Resources.Load<TextAsset>` 로 로드 |
| UI 시스템 | uGUI + TextMeshPro | |
| 로깅 | `Debug.Log*` — 개발 빌드 한정 | |
| 테스트 프레임워크 | Unity Test Framework(NUnit) EditMode | |
| PlayMode 테스트 | — | 슬라이스 범위 밖 |
| 커버리지 도구 | `com.unity.testtools.codecoverage` | |
| 네트워크 | — | 서버·온라인 기능 없음 |
| 사용자 계정 | — | |
| 영속 저장 | — | 슬라이스는 저장 기능 없음 |
| 광고·결제 SDK | — | 프리미엄 모델 |
| 대상 플랫폼 | Android (IL2CPP, ARM64) | |

## 1. arch 대응표

| 논리명 | 이 스택의 표기 |
|---|---|
| 규칙 모듈 | `TAoB.Combat` 어셈블리 (`Scripts/Combat/TAoB.Combat.asmdef`) |
| 데이터 로더 | `TAoB.Data` 어셈블리 (`Scripts/Data/TAoB.Data.asmdef`) — `GameDataLoader` |
| 표시 계층 | `TAoB.View` 어셈블리 (`Scripts/View/TAoB.View.asmdef`) — `MonoBehaviour` 파생 |
| 모듈 경계 선언 | `.asmdef` 의 `references` |
| 엔진 참조 차단 설정 | `.asmdef` 의 `"noEngineReferences": true` |
| 진입점 객체 | `BattleBootstrap` (씬의 유일한 `GameObject`) |
| 명령 큐 | `BattleCommand` → `CommandQueue` (유닛당 대기 명령 1개, 틱 경계에서 처리) |
| 자동 판단 | `AutoPilot` (규칙 모듈 소속, 대기 명령이 없을 때 그 틱에 직접 `BattleCommand` 를 만든다 — 명령 큐는 사용자 명령 전용) |
| 밸런스 수치 주입 | `BalanceConfig` |
| 상태 스냅샷 | `BattleSnapshot`/`UnitSnapshot` — `FillSnapshot(target)` 이 호출자 소유 버퍼에 값 복사, 읽기는 `IReadOnlyList<T>`·getter-only 프로퍼티 |
| 틱 결과 통지 | `BattleSimulation.Step(List<BattleEvent> buffer)` — 호출자 소유 버퍼를 비우고 채움 (`BattleEvent` 는 `struct`). `Step` 1회마다 `BattleBootstrap` 이 뷰에 직접 전달 |
| 고정 틱 | `BattleSimulation.Step()` 1회 호출 |
| 시드 난수 | `IRandom` 인터페이스 / `SeededRandom` 구현 |
| 데이터 파일 | `Resources/Data/*.json` |

## 2. quality 대응표

| 논리명 | 이 스택의 표기 |
|---|---|
| 함수 | 메서드, 로컬 함수, 람다 |
| 상수 선언 | `const`, `static readonly` |
| 에디터 노출 필드 | `[SerializeField] private` 필드 |
| 로그 호출 | `Debug.Log`, `Debug.LogWarning`, `Debug.LogError` |
| 개발 전용 구역 | `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` |
| 읽기 전용 노출 | `IReadOnlyList<T>`, `readonly` 필드, getter-only 프로퍼티 |
| 네이밍 규칙 | 타입·메서드·프로퍼티·상수 `PascalCase` · 지역변수·매개변수 `camelCase` · private 필드 `_camelCase` · 불리언 `Is`/`Has`/`Can` 접두 |

## 3. security 대응표

| 논리명 | 이 스택의 표기 |
|---|---|
| 서명 키 파일 | `*.keystore`, `*.jks` |
| 서명 비밀번호 지정 | `PlayerSettings.Android.keystorePass`, `PlayerSettings.Android.keyaliasPass` 를 코드·빌드 스크립트에 문자열로 지정 |
| 자격증명 파일 | `fastlane/.env`, 스토어 서비스 계정 JSON |
| 무시 목록 | 저장소 루트 `.gitignore` |
| 데이터 역직렬화 | `JsonUtility.FromJson<T>` |
| 개발 전용 기능 | `DebugLoadoutPanel`, 치트·디버그 명령 |
| 서드파티 의존성 목록 | `Game/Packages/manifest.json`, `packages-lock.json` |

## 4. perf 대응표

| 논리명 | 이 스택의 표기 |
|---|---|
| 프레임 콜백 | `Update`, `LateUpdate`, `FixedUpdate`, `OnGUI` |
| 컴포넌트 탐색 호출 | `GetComponent*`, `FindObjectOfType`, `FindObjectsByType`, `GameObject.Find*` |
| 힙 할당 | 참조 타입·배열 `new`, `System.Linq`, 문자열 보간·`+` 연결, 캡처 람다, 값 타입의 `object`·인터페이스 박싱 |
| 객체 생성·파괴 호출 | `Instantiate`, `Destroy` |
| UI 재구성 | `TMP_Text.text` 대입(`SetText` 수치 오버로드는 할당 없음), `LayoutGroup` 자식 변경, `Canvas` 활성 토글 |
| 재질 복제 | `renderer.material` 접근, 런타임 `new Material` |
| 누수 요인 | `event +=` 후 `-=` 누락, 정지하지 않은 코루틴, 정적 컬렉션에 남은 파괴된 객체 |
| 동기 로딩 | `Resources.Load`, `File.ReadAllText` |

## 5. test 대응표

| 논리명 | 이 스택의 표기 |
|---|---|
| 테스트 모듈 | `Tests/EditMode/Combat/TAoB.Combat.Tests.asmdef`, `Tests/EditMode/Data/TAoB.Data.Tests.asmdef`, `Tests/EditMode/View/TAoB.View.Tests.asmdef`(Unity 는 폴더당 asmdef 1개) — 모두 `includePlatforms: ["Editor"]`, `defineConstraints: ["UNITY_INCLUDE_TESTS"]`, `overrideReferences: true` + `precompiledReferences: ["nunit.framework.dll"]` |
| 테스트 선언 | `[Test]`, `[TestCase(...)]` |
| 테스트 실행 명령 | `project_facts.md` §0 "테스트 실행" |
| 커버리지 측정 | 테스트 실행 명령 + `-enableCodeCoverage -coverageResultsPath Game/CodeCoverage -coverageOptions "assemblyFilters:+TAoB.Combat,+TAoB.Data,+TAoB.View;generateHtmlReport"` |
| 난수 대체 | 고정 수열을 돌려주는 테스트용 `IRandom` 구현 |
| 수동 시나리오 | 활성 계획 문서의 해당 Phase "검증" 목록 |

## 6. comments 대응표

| 논리명 | 이 스택의 표기 |
|---|---|
| 문서 주석 | `/// <summary>` XML 주석 |
| 기획 참조 주석 | `// GDD 4-1`, `// 설정집 5-2` 형식 (기획서 절 번호) |
| 할 일 표기 | `// TODO(조건): 내용` |
| 주석 처리된 코드 | `//` 로 막은 코드 문장, `#if false` 블록 |

## 7. lang 판정 (unity-csharp)

### 7.1 Unity 가짜 null
- **Flag:** `UnityEngine.Object` 파생 타입에 `?.`, `??`, `??=`, `is null`, `is not null` 사용 → HIGH. 파괴된 객체가 살아있는 것으로 판정된다. `== null` 또는 암시적 bool 변환을 쓴다.

### 7.2 수명주기
- **Flag:** 다른 오브젝트의 `Awake`/`Start` 가 먼저 끝났다고 가정하는 초기화 → MEDIUM. 진입점 객체에서 생성·초기화 순서를 명시한다.
- **Flag:** `OnEnable` 구독과 `OnDisable`(또는 `OnDestroy`) 해제의 짝이 맞지 않음 → HIGH.
- **Note:** 해제 누락으로 인한 누수 영향은 performance §1.6 에 위임한다.

### 7.3 에디터 노출과 엔진 특성
- **Flag:** 인스펙터 노출만을 위한 `public` 필드 → MEDIUM. `[SerializeField] private` 로 바꾼다.
- **Flag:** 규칙 모듈 타입에 `SerializeField` 등 `UnityEngine` 특성이 필요해지는 설계 → HIGH. 엔진 의존이 규칙 모듈로 새어 들어온다(arch §1.1 과 함께 보고).

### 7.4 공유 에셋의 런타임 변경
- **Flag:** 플레이 중 `ScriptableObject`·공유 `Material` 의 필드를 변경 → HIGH. 에디터에서는 에셋 파일에 영구 반영되고, 모든 사용처가 함께 바뀐다.

### 7.5 코루틴·비동기
- **Flag:** 이벤트 핸들러가 아닌 곳의 `async void` → HIGH. 예외가 삼켜지고 완료 시점을 알 수 없다.
- **Flag:** `await` 이후 컴포넌트 파괴 여부를 확인하지 않고 멤버 접근 → HIGH.
- **Flag:** 비활성화될 수 있는 오브젝트에서 `StartCoroutine` 을 시작하고 정지·재시작 관리가 없음 → MEDIUM.

### 7.6 어셈블리 정의
- **Flag:** `TAoB.Combat.asmdef` 의 `noEngineReferences` 해제, 또는 `UnityEngine.*`·`TAoB.Data`·`TAoB.View` 참조 → CRITICAL.
- **Flag:** `.asmdef` 순환 참조, 또는 §0 "모듈 구성" 방향과 다른 참조 → HIGH.
- **Flag:** 테스트 `.asmdef` 에 `UNITY_INCLUDE_TESTS` 제약이나 Editor 플랫폼 한정이 없음 → HIGH. 테스트 코드가 플레이어 빌드에 포함된다.

### 7.7 에디터 전용 API
- **Flag:** 런타임 어셈블리에서 `UnityEditor` 네임스페이스를 `#if UNITY_EDITOR` 없이 사용 → CRITICAL. 플레이어 빌드가 실패한다.

### 7.8 JsonUtility 제약
- **Flag:** `Dictionary`, 프로퍼티, 다형 필드, 최상위 배열을 `JsonUtility` 로 읽거나 쓰기를 기대 → HIGH. 오류 없이 조용히 무시된다.
- **Flag:** `FromJson` 결과를 필드 누락 검증 없이 사용 → HIGH. 누락 필드는 기본값(0, null)으로 채워져 잘못된 밸런스로 조용히 동작한다.
- **Note:** 신뢰 경계 관점의 판정은 security §3 에 위임한다.

### 7.9 C# 관용
- **Flag:** 가변 `List<T>`·배열을 공개 프로퍼티로 그대로 반환 → MEDIUM. `IReadOnlyList<T>` 로 노출한다.
- **Flag:** `record` 또는 `init` 전용 세터 사용 → MEDIUM (§0 언어 비고).
- **Flag:** 가변 `struct` 를 컬렉션에서 꺼내 수정하고 원본이 바뀌길 기대 → HIGH.
- **Flag:** 규칙 모듈에서 시간·게이지를 `float` 누적으로 계산 → HIGH. 플랫폼 간 오차로 결정론이 깨질 수 있다. 정수 틱과 정수·고정 배율 연산을 쓴다.

### 7.10 Resources 사용
- **Flag:** `Resources.Load` 경로 문자열 리터럴이 여러 파일에 흩어짐 → LOW. 로더 한곳의 상수로 모은다.
- **Flag:** `Resources/` 에 데이터 파일이 아닌 대용량 에셋을 둠 → MEDIUM. 사용 여부와 무관하게 빌드에 포함된다.

## 8. scope 추론 신호

소스 확장자: `.cs`, `.asmdef`, `.json`, `.unity`, `.prefab`, `.asset`

| scope | diff 내용 신호 |
|---|---|
| arch | `.asmdef` 변경, `interface `, `namespace `, `Bootstrap`, `BattleCommand`, `IRandom`, 새 폴더 추가 |
| lang | `.cs` 변경 전부 |
| quality | `.cs` 변경 전부, `Resources/Data/*.json` 변경 |
| security | `keystore`, `.jks`, `keystorePass`, `keyaliasPass`, `password`, `JsonUtility.FromJson`, `File.`, `persistentDataPath`, `manifest.json` |
| perf | `Update(`, `LateUpdate(`, `FixedUpdate(`, `Instantiate(`, `Destroy(`, `GetComponent`, `Find`, `System.Linq`, `new List<`, `$"`, `.text =`, `Material` |
| test | `[Test]`, `[TestCase`, `NUnit`, `Tests.asmdef`, `Scripts/Combat/` 아래 `.cs` 변경 |
| comments | `///`, `// GDD`, `// 설정집`, `TODO` |
