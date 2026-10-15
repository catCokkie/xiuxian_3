param(
    [string]$GodotPath = "",
    [switch]$RenderArt
)
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
    & dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj --no-restore -v quiet -p:WarningLevel=0 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~SupplyRulesTests|FullyQualifiedName~TalismanRulesTests|FullyQualifiedName~SuppressionExpiresAfterFiveRounds'
    if ($LASTEXITCODE -ne 0) { throw 'Supply unit checks failed.' }
    & $GodotPath --headless --path $projectRoot --log-file "$projectRoot/.godot/verify_supply.log" --scene res://scenes/GameplaySmokeTest.tscn -- --smoke-test --supply-smoke
    if ($LASTEXITCODE -ne 0) { throw 'Supply gameplay checks failed.' }
    & python tools/art/validate_assets.py
    if ($LASTEXITCODE -ne 0) { throw 'Asset validation failed.' }
    if ($RenderArt) {
        $renderArgs = @('--path', $projectRoot, '--log-file', "$projectRoot/.godot/verify_art.log", '--rendering-method', 'gl_compatibility', '--scene', 'res://scenes/ArtSmokeTest.tscn', '--', '--smoke-test', '--render-smoke')
        $renderProcess = Start-Process -FilePath $GodotPath -ArgumentList $renderArgs -WindowStyle Hidden -Wait -PassThru
        if ($renderProcess.ExitCode -ne 0) { throw 'Art rendering checks failed.' }
    }
}
finally { Pop-Location }
