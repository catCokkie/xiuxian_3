param([string]$GodotPath = "", [int[]]$Rates = @(2, 30, 90), [ValidateRange(1,7)][int]$Days = 3, [switch]$Continuous, [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not $GodotPath) {
    $command = Get-Command godot -ErrorAction SilentlyContinue
    if ($command) { $GodotPath = $command.Source }
    else { $GodotPath = Join-Path ([System.IO.Path]::GetPathRoot($projectRoot)) 'Godot_v4.5-stable_mono_win64\Godot_v4.5-stable_mono_win64\Godot_v4.5-stable_mono_win64_console.exe' }
}
if (-not (Test-Path -LiteralPath $GodotPath -PathType Leaf)) { throw 'Supply the Godot .NET console executable with -GodotPath.' }
if ($Rates.Count -eq 0 -or @($Rates | Where-Object { $_ -lt 0 -or $_ -gt 90 }).Count -gt 0) { throw 'Rates must be0..90/min.' }
Push-Location $projectRoot
try {
    New-Item -ItemType Directory -Path .godot -Force | Out-Null
    if (-not $SkipBuild) {
        & dotnet build xiuxian2.csproj --no-restore -v quiet -p:WarningLevel=0 -p:UseSharedCompilation=false
        if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    }
    $cases = @(); $runs = @(); $schedule = if ($Continuous) { 'continuous' } else { 'mixed' }
    foreach ($rate in $Rates) {
        $prefix = ".godot/first_boss_${schedule}_$rate"
        $arguments = @('--headless','--path',$projectRoot.Replace('\','/'),'--log-file',"$projectRoot/$prefix.log",'--scene','res://scenes/FirstBossRouteSimulation.tscn','--','--smoke-test',"--route-rate=$rate","--route-days=$Days","--route-schedule=$schedule","--route-output=res://$prefix.json")
        $process = Start-Process -FilePath $GodotPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput "$prefix.stdout.log" -RedirectStandardError "$prefix.stderr.log"
        $null = $process.Handle
        $runs += @{ Process = $process; Prefix = $prefix; Rate = $rate }
    }
    foreach ($run in $runs) {
        $process = $run.Process; $prefix = $run.Prefix; $rate = $run.Rate
        while (-not $process.WaitForExit(1000)) { }
        if ($process.ExitCode -ne 0) { Get-Content "$prefix.stderr.log" -Tail 20; throw "Route sample$rate failed; inspect $prefix.stdout.log." }
        $case = Get-Content "$prefix.json" -Raw -Encoding UTF8 | ConvertFrom-Json; $cases += $case
        Write-Output ("rate={0}/min cleared={1} elapsed={2}h realm={3}" -f $rate,$case.Cleared,$case.ElapsedHours,$case.Realm)
    }
    $report = [ordered]@{ Model = 'native_first_boss_route_samples'; Scope = 'Current implemented gameplay only; stochastic policy samples, not human playtesting or a win-rate estimate'; Cases = $cases }
    $path = Join-Path $projectRoot 'docs/balance/first_boss_runtime.json'
    [System.IO.File]::WriteAllText($path, ($report | ConvertTo-Json -Depth 100) + "`n", (New-Object System.Text.UTF8Encoding($false)))
    Write-Output "Report: $path"
}
finally {
    foreach ($run in $runs) {
        if (-not $run.Process.HasExited) { $run.Process.Kill(); $null = $run.Process.WaitForExit(5000) }
        $run.Process.Dispose()
    }
    Pop-Location
}
