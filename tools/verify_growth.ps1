param([string]$GodotPath = "", [switch]$WriteReport)
$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not $GodotPath) {
    $godotCommand = Get-Command godot -ErrorAction SilentlyContinue
    if ($godotCommand) { $GodotPath = $godotCommand.Source }
    else { $GodotPath = Join-Path ([System.IO.Path]::GetPathRoot($projectRoot)) 'Godot_v4.5-stable_mono_win64\Godot_v4.5-stable_mono_win64\Godot_v4.5-stable_mono_win64_console.exe' }
}
if (-not (Test-Path -LiteralPath $GodotPath -PathType Leaf)) { throw 'Supply the Godot .NET console executable with -GodotPath.' }
$previousReport = [System.Environment]::GetEnvironmentVariable('XIUXIAN_GROWTH_REPORT')
Push-Location $projectRoot
try {
    New-Item -ItemType Directory -Path .godot -Force | Out-Null
    if ($WriteReport) { $env:XIUXIAN_GROWTH_REPORT = 'docs/balance/shared_growth_pace.json' }
    & dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj --no-restore -v quiet -p:WarningLevel=0 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~SharedGrowth|FullyQualifiedName~ProgressionBalanceTests'
    if ($LASTEXITCODE -ne 0) { throw 'Growth unit checks failed.' }
    & $GodotPath --headless --path $projectRoot --log-file "$projectRoot/.godot/verify_growth.log" --scene res://scenes/GameplaySmokeTest.tscn -- --smoke-test --growth-smoke
    if ($LASTEXITCODE -ne 0) { throw 'Growth gameplay checks failed.' }
}
finally {
    if ($null -eq $previousReport) { Remove-Item -LiteralPath Env:XIUXIAN_GROWTH_REPORT -ErrorAction SilentlyContinue }
    else { $env:XIUXIAN_GROWTH_REPORT = $previousReport }
    Pop-Location
}
