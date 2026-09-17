<#
.SYNOPSIS
  에디터 도구(`Assets/_Project/Editor/`)를 배치모드로 실행한다.

.DESCRIPTION
  도구마다 배치모드 인자가 다르다. 이 스크립트가 그 차이를 담는다.
    scene — 전투 씬을 새로 만든다(기존 씬과 빌드 씬 목록을 덮어쓴다). `-quit` 사용.
    tmp   — TMP 필수 리소스를 가져온다. 패키지 가져오기가 비동기라 `-quit` 없이 실행하고 도구가 완료 콜백에서 종료한다.
    smoke — 전투 씬을 6초 플레이해 실행 오류를 센다. 플레이 모드라 `-nographics` 를 쓰지 않는다. 오류가 없으면 종료 코드 0.
    win   — Windows 개발 빌드(`Game/Builds/Windows/TAoB.exe`). 제한 시간 기본값이 길다(실측 첫 빌드 약 2분).
    apk   — 안드로이드 개발 빌드(`Game/Builds/Android/TAoB-slice.apk`). IL2CPP 라 첫 빌드가 오래 걸린다(실측 약 12분, PC 에 따라 다름).
  에디터가 프로젝트를 열고 있으면 프로젝트 잠금으로 실패한다 — 먼저 Unity 에디터를 닫는다.

.EXAMPLE
  ./Tools/run-editor-tool.ps1 -Tool smoke
  ./Tools/run-editor-tool.ps1 -Tool scene
  ./Tools/run-editor-tool.ps1 -Tool apk
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("scene", "tmp", "smoke", "win", "apk")]
    [string]$Tool,

    [string]$OutDir = (Join-Path $env:TEMP "TAoB-tests"),

    # 이 시간이 지나면 강제 종료한다(초). 빌드는 아래에서 기본값을 늘린다.
    [int]$TimeoutSeconds = 300
)

# 빌드는 기본 300초를 넘기기 쉽다(특히 IL2CPP 인 apk, 실측 약 12분). 사용자가 직접 준 값은 그대로 존중한다.
if (-not $PSBoundParameters.ContainsKey('TimeoutSeconds') -and $Tool -in @('win', 'apk')) { $TimeoutSeconds = 3600 }

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo "Game"

$versionFile = Join-Path $project "ProjectSettings\ProjectVersion.txt"
$version = (Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion:\s*(\S+)').Matches[0].Groups[1].Value
$editor = "E:\Unity\Editors\$version\Editor\Unity.exe"
if (-not (Test-Path $editor)) { throw "Unity 실행 파일이 없습니다: $editor (project_facts.md §0 'Unity 실행 파일' 참고)" }

$method = switch ($Tool) {
    "scene" { "TAoB.EditorTools.ProjectSetupTools.CreateBattleScene" }
    "tmp"   { "TAoB.EditorTools.ProjectSetupTools.ImportTmpEssentials" }
    "smoke" { "TAoB.EditorTools.PlayModeSmoke.Run" }
    "win"   { "TAoB.EditorTools.BuildTools.BuildWindows" }
    "apk"   { "TAoB.EditorTools.BuildTools.BuildAndroid" }
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$log = Join-Path $OutDir ("{0}-{1}.log" -f $Tool, (Get-Date -Format 'yyyyMMdd-HHmmss'))

$argList = @('-batchmode', '-projectPath', "`"$project`"", '-executeMethod', $method, '-logFile', "`"$log`"")
if ($Tool -ne "smoke") { $argList += '-nographics' }              # 플레이 모드는 그래픽이 필요하다.
# 빌드 도구는 스스로 Exit 하지만, 예외가 새어 나와도 에디터가 남지 않도록 `-quit` 를 함께 준다. tmp·smoke 는 도구가 직접 닫는다.
if ($Tool -in @('scene', 'win', 'apk')) { $argList += '-quit' }

"$Tool 실행 ($version) — 로그: $log"
$p = Start-Process -FilePath $editor -ArgumentList $argList -PassThru -NoNewWindow
if (-not $p.WaitForExit($TimeoutSeconds * 1000)) {
    "시간 초과 ${TimeoutSeconds}초 — 강제 종료합니다."
    $p.Kill()
    exit 1
}

# 시간 제한 있는 WaitForExit 뒤에는 인자 없는 호출을 한 번 더 해야 종료 코드가 채워진다(.NET 규약).
$p.WaitForExit()

$compileErrors = Select-String -LiteralPath $log -Pattern 'error CS\d+' -ErrorAction SilentlyContinue |
    ForEach-Object { ($_.Line -replace '^.*Assets[\\/]_Project[\\/]', '') } | Sort-Object -Unique
if ($compileErrors) {
    "컴파일 오류:"
    $compileErrors | Select-Object -First 20
    exit 1
}

Select-String -LiteralPath $log -Pattern '\[스모크\]|\[빌드\]|전투 씬 생성|패키지 가져오기|한글 폰트가 없습니다' -ErrorAction SilentlyContinue |
    ForEach-Object { $_.Line } | Select-Object -First 10

# Unity 를 `Start-Process -PassThru` 로 띄우면 종료 코드가 비어 돌아오는 경우가 있어(실패를 0 으로 오해하게 된다)
# 도구가 로그에 남긴 완료 표시로 성공을 판정한다.
$done = switch ($Tool) {
    "scene" { Select-String -LiteralPath $log -Pattern '전투 씬 생성' -Quiet }
    "tmp"   { Select-String -LiteralPath $log -Pattern '패키지 가져오기 완료' -Quiet }
    "smoke" {
        $line = Select-String -LiteralPath $log -Pattern '\[스모크\].*오류 (\d+)건' | Select-Object -First 1
        if ($line) { [int]$line.Matches[0].Groups[1].Value -eq 0 } else { $false }
    }
    "win"   { Select-String -LiteralPath $log -Pattern '\[빌드\] 성공' -Quiet }
    "apk"   { Select-String -LiteralPath $log -Pattern '\[빌드\] 성공' -Quiet }
}

if ($done) { "성공"; exit 0 }
"실패 — 완료 표시를 찾지 못했습니다. 로그를 확인하세요: $log"
exit 1
