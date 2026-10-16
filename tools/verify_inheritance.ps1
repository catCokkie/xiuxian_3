param([string]$GodotPath = "")
$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not $GodotPath) {
    $godotCommand = Get-Command godot -ErrorAction SilentlyContinue
    if ($godotCommand) { $GodotPath = $godotCommand.Source }
    else { $GodotPath = Join-Path ([System.IO.Path]::GetPathRoot($projectRoot)) 'Godot_v4.5-stable_mono_win64\Godot_v4.5-stable_mono_win64\Godot_v4.5-stable_mono_win64_console.exe' }
}
if (-not (Test-Path -LiteralPath $GodotPath -PathType Leaf)) { throw 'Supply the Godot .NET console executable with -GodotPath.' }
Push-Location $projectRoot
try {
    New-Item -ItemType Directory -Path .godot -Force | Out-Null
    & dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj --no-restore -v quiet -p:WarningLevel=0 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~EquipmentInheritanceRulesTests'
    if ($LASTEXITCODE -ne 0) { throw 'Inheritance unit checks failed.' }
    & $GodotPath --headless --path $projectRoot --log-file "$projectRoot/.godot/verify_inheritance.log" --scene res://scenes/GameplaySmokeTest.tscn -- --smoke-test --inherit-smoke
    if ($LASTEXITCODE -ne 0) { throw 'Inheritance gameplay checks failed.' }
}
finally { Pop-Location }
