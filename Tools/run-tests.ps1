<#
.SYNOPSIS
  EditMode 자동 테스트를 배치모드로 돌린다.

.DESCRIPTION
  명령 형태와 주의점의 정본은 `docs/references/project_facts.md` §0 "테스트 실행" 이다. 이 스크립트는 그 명령을 그대로 실행하는 편의 도구다.
  에디터가 프로젝트를 열고 있으면 프로젝트 잠금으로 실패한다 — 먼저 Unity 에디터를 닫는다.

.EXAMPLE
  ./Tools/run-tests.ps1
  ./Tools/run-tests.ps1 -Coverage
  ./Tools/run-tests.ps1 -Filter TAoB.View.Tests
#>
param(
    # 결과·로그 파일 이름에 붙는 꼬리표.
    [string]$Tag = "run",

    # 네임스페이스·클래스·메서드 이름의 일부. 예: TAoB.Data.Tests
    [string]$Filter = "",

    # 커버리지까지 측정한다(느려진다).
    [switch]$Coverage,

    # 결과·로그를 둘 폴더. 기본값은 사용자 임시 폴더 — 저장소를 더럽히지 않는다.
    [string]$OutDir = (Join-Path $env:TEMP "TAoB-tests")
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo "Game"

$versionFile = Join-Path $project "ProjectSettings\ProjectVersion.txt"
$version = (Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion:\s*(\S+)').Matches[0].Groups[1].Value
$editor = "E:\Unity\Editors\$version\Editor\Unity.exe"
if (-not (Test-Path $editor)) { throw "Unity 실행 파일이 없습니다: $editor (project_facts.md §0 'Unity 실행 파일' 참고)" }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$results = Join-Path $OutDir "results-$Tag-$stamp.xml"
$log = Join-Path $OutDir "test-$Tag-$stamp.log"
$cov = Join-Path $OutDir "coverage-$Tag-$stamp"

# `-quit` 는 붙이지 않는다(테스트 러너가 스스로 종료한다).
$argList = @(
    '-batchmode', '-nographics',
    '-projectPath', "`"$project`"",
    '-runTests', '-testPlatform', 'EditMode',
    '-testResults', "`"$results`"",
    '-logFile', "`"$log`""
)
if ($Filter) { $argList += @('-testFilter', $Filter) }
if ($Coverage) {
    $argList += @(
        '-enableCodeCoverage',
        '-coverageResultsPath', "`"$cov`"",
        '-coverageOptions', '"generateAdditionalReports;assemblyFilters:+TAoB.Combat,+TAoB.Data,+TAoB.View"'
    )
}

"테스트 시작 ($version) — 로그: $log"
# `-Wait` 는 Unity 보조 프로세스까지 기다려 멈춘 것처럼 보인다. 본체만 기다린다.
$p = Start-Process -FilePath $editor -ArgumentList $argList -PassThru -NoNewWindow
$p.WaitForExit()

$compileErrors = Select-String -LiteralPath $log -Pattern 'error CS\d+' -ErrorAction SilentlyContinue |
    ForEach-Object { ($_.Line -replace '^.*Assets[\\/]_Project[\\/]', '') } | Sort-Object -Unique
if ($compileErrors) {
    "컴파일 오류:"
    $compileErrors | Select-Object -First 20
}

if (-not (Test-Path $results)) {
    "결과 파일이 없습니다(컴파일 오류이거나 프로젝트 잠금). 로그 마지막 줄:"
    Get-Content -LiteralPath $log -Tail 15
    exit 1
}

# 결과 XML 은 UTF-8 이다. PowerShell 5.1 의 Get-Content 기본값(ANSI)으로 읽으면 한글(제품명 등)이 깨져 XML 변환이 실패한다.
[xml]$x = Get-Content -LiteralPath $results -Raw -Encoding UTF8
"테스트 {0}개 — 통과 {1} · 실패 {2}" -f $x.'test-run'.total, $x.'test-run'.passed, $x.'test-run'.failed
foreach ($c in $x.SelectNodes("//test-case[@result='Failed']")) {
    "- {0}`n    {1}" -f $c.fullname, ((($c.failure.message.'#cdata-section') -replace '\s+', ' ').Trim())
}

if ($Coverage) {
    $summary = Get-ChildItem -LiteralPath $cov -Recurse -Filter "Summary.xml" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($summary) {
        [xml]$s = Get-Content -LiteralPath $summary.FullName -Raw
        "커버리지 전체 {0}% ({1}/{2}줄)" -f $s.CoverageReport.Summary.Linecoverage, $s.CoverageReport.Summary.Coveredlines, $s.CoverageReport.Summary.Coverablelines
        foreach ($a in $s.CoverageReport.Coverage.Assembly) {
            "  {0}: {1}% ({2}/{3}줄)" -f $a.name, $a.coverage, $a.coveredlines, $a.coverablelines
        }
    }
}

exit ([int]$x.'test-run'.failed)
