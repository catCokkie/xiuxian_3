param(
    [string]$GodotPath = '', [int[]]$ExploreBudgets = @(1500,2500,5000), [int[]]$RoundInputs = @(2,4,8),
    [int[]]$Rates = @(2,30,90), [ValidateRange(1,10)][int]$Repeats = 3,
    [ValidateRange(1,7)][int]$Days = 3, [ValidateRange(1,4)][int]$MaxParallel = 3,
    [string]$Report = 'docs/balance/first_boss_input_matrix.json', [switch]$SkipBuild, [switch]$SkipBaseline,
    [ValidateSet('funded-early','after-failure')][string]$Preparation = 'funded-early'
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not $GodotPath) {
    $command = Get-Command godot -ErrorAction SilentlyContinue
    if ($command) { $GodotPath = $command.Source }
    else { $GodotPath = Join-Path ([System.IO.Path]::GetPathRoot($projectRoot)) 'Godot_v4.5-stable_mono_win64\Godot_v4.5-stable_mono_win64\Godot_v4.5-stable_mono_win64_console.exe' }
}
if (-not (Test-Path -LiteralPath $GodotPath -PathType Leaf)) { throw 'Supply the Godot .NET console executable with -GodotPath.' }
if (@($ExploreBudgets | Where-Object { $_ -lt 100 -or $_ -gt 10000 }).Count -gt 0 -or $ExploreBudgets.Count -eq 0) { throw 'Explore budgets must be100..10000.' }
if (@($RoundInputs | Where-Object { $_ -lt 1 -or $_ -gt 50 }).Count -gt 0 -or $RoundInputs.Count -eq 0) { throw 'Round thresholds must be1..50.' }
if (@($Rates | Where-Object { $_ -lt 0 -or $_ -gt 90 }).Count -gt 0 -or $Rates.Count -eq 0) { throw 'Rates must be0..90.' }
$reportPath = [System.IO.Path]::GetFullPath((Join-Path $projectRoot $Report))
if (-not $reportPath.StartsWith($projectRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Report must remain inside the project.' }
function Get-Median($values) {
    $sorted = @($values | Where-Object { $null -ne $_ } | Sort-Object)
    if ($sorted.Count -eq 0) { return $null }
    $middle = [int][math]::Floor($sorted.Count / 2)
    if ($sorted.Count % 2) { return $sorted[$middle] }
    return ($sorted[$middle - 1] + $sorted[$middle]) / 2
}
$active = New-Object System.Collections.ArrayList
$samples = New-Object System.Collections.ArrayList
Push-Location $projectRoot
try {
    New-Item -ItemType Directory -Path .godot/input_matrix -Force | Out-Null
    if (-not $SkipBuild) {
        & dotnet build xiuxian2.csproj --no-restore -v quiet -p:WarningLevel=0 -p:UseSharedCompilation=false
        if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    }
    $jobs = New-Object System.Collections.Generic.Queue[object]
    foreach ($rate in $Rates) {
        for ($trial=1; $trial -le $Repeats; $trial++) {
            if (-not $SkipBaseline) { $jobs.Enqueue(@{ Explore=5000; Round=0; Rate=$rate; Trial=$trial }) }
            foreach ($explore in $ExploreBudgets) { foreach ($round in $RoundInputs) { $jobs.Enqueue(@{ Explore=$explore; Round=$round; Rate=$rate; Trial=$trial }) } }
        }
    }
    $total = $jobs.Count
    while ($jobs.Count -gt 0 -or $active.Count -gt 0) {
        while ($jobs.Count -gt 0 -and $active.Count -lt $MaxParallel) {
            $job = $jobs.Dequeue(); $id = "e$($job.Explore)_b$($job.Round)_r$($job.Rate)_t$($job.Trial)"; $prefix = ".godot/input_matrix/${Preparation}_$id"
            $arguments = @('--headless','--path',$projectRoot.Replace('\','/'),'--log-file',"$projectRoot/$prefix.log",'--scene','res://scenes/FirstBossRouteSimulation.tscn','--','--smoke-test',"--route-rate=$($job.Rate)","--route-days=$Days",'--route-schedule=mixed',"--route-explore-inputs=$($job.Explore)","--route-round-inputs=$($job.Round)","--route-preparation=$Preparation","--route-output=res://$prefix.json")
            $process = Start-Process -FilePath $GodotPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput "$prefix.stdout.log" -RedirectStandardError "$prefix.stderr.log"
            $null = $process.Handle
            $null = $active.Add(@{ Process=$process; Prefix=$prefix; Id=$id; Trial=$job.Trial })
        }
        foreach ($run in @($active.ToArray())) {
            if (-not $run.Process.HasExited) { continue }
            $run.Process.WaitForExit()
            if ($run.Process.ExitCode -ne 0) { Get-Content "$($run.Prefix).stderr.log" -Tail 20; throw "Failed native sample $($run.Id)." }
            $sample = Get-Content "$($run.Prefix).json" -Raw -Encoding UTF8 | ConvertFrom-Json
            $onlineSeconds = ($sample.OnlineSecondsByAction.PSObject.Properties | Measure-Object -Property Value -Sum).Sum
            $expectedKeys = [math]::Floor($onlineSeconds * $sample.RatePerMinute / 60 + .000001)
            if ($sample.InputLedger.total_key_down -ne $expectedKeys) { throw "Input ledger mismatch $($run.Id)." }
            if ($sample.Wallet.lingqi -lt 0 -or $sample.Wallet.insight -lt 0 -or $sample.Wallet.spirit_stones -lt 0 -or @($sample.Inventory.PSObject.Properties | Where-Object Value -lt 0).Count -gt 0) { throw "Unfunded sample $($run.Id)." }
            if ($sample.Cleared -and $sample.FirstClearHours -lt $sample.FirstBossAttemptHours) { throw 'Invalid milestones.' }
            $null = $samples.Add([pscustomobject][ordered]@{
                Id=$run.Id; Trial=$run.Trial; ExploreInputs=$sample.ExploreInputs; RoundInputs=$sample.RoundInputOverride; Rate=$sample.RatePerMinute; PreparationPolicy=$Preparation;
                Cleared=$sample.Cleared; ClearHours=$sample.FirstClearHours; ElapsedHours=$sample.ElapsedHours; OnlineHours=$sample.OnlineHours;
                ReadyHours=$sample.BossReadyHours; FirstAttemptHours=$sample.FirstBossAttemptHours; ClearRealm=$sample.Realm;
                FirstAttemptRealm=if ($sample.BossAttempts.Count) { $sample.BossAttempts[0].StartRealm } else { $null };
                BossAttempts=$sample.BossAttempts; Stats=$sample.Stats; Wallet=$sample.Wallet; BattleStats=$sample.BattleStats;
                RealInputs=$expectedKeys; OnlineSecondsByAction=$sample.OnlineSecondsByAction; ManagementActions=$sample.ManagementActions; ManagementChecks=$sample.ManagementChecks;
                FirstSwapHours=$sample.FirstEquipmentSwapHours; PostClearSwapHours=$sample.FirstPostClearSwapHours;
                Milestones=@($sample.Trace | Where-Object { $_.Kind -in @('boss_start','boss_result','first_clear','leave','return','start_smithing','stop_smithing') })
            })
            Write-Output ("{0}/{1} {2} cleared={3} hours={4} realm={5} attempts={6}" -f $samples.Count,$total,$run.Id,$sample.Cleared,$sample.FirstClearHours,$sample.Realm,$sample.BossAttempts.Count)
            $run.Process.Dispose(); $active.Remove($run)
        }
        if ($active.Count -gt 0) { Start-Sleep -Milliseconds 100 }
    }
    $groups = @($samples | Group-Object ExploreInputs,RoundInputs,Rate | ForEach-Object {
        $all = @($_.Group); $cleared = @($all | Where-Object Cleared); $first = $all[0]
        [pscustomobject][ordered]@{
            ExploreInputs=$first.ExploreInputs; RoundInputs=$first.RoundInputs; Rate=$first.Rate; Samples=$all.Count; Cleared=$cleared.Count;
            MedianClearHours=Get-Median @($cleared.ClearHours); MinClearHours=($cleared.ClearHours | Measure-Object -Minimum).Minimum; MaxClearHours=($cleared.ClearHours | Measure-Object -Maximum).Maximum;
            MedianOnlineHours=Get-Median @($cleared.OnlineHours); MedianFirstAttemptRealm=Get-Median @($all.FirstAttemptRealm);
            MedianClearRealm=Get-Median @($cleared.ClearRealm); MedianFirstFightMinutes=Get-Median @($all | ForEach-Object { if ($_.BossAttempts.Count) { $_.BossAttempts[0].DurationMinutes } });
            TotalBossLosses=@($all.BossAttempts | Where-Object Result -ne 'victory').Count;
            MedianSmithingMinutes=Get-Median @($all | ForEach-Object { if ($_.OnlineSecondsByAction.smithing) { $_.OnlineSecondsByAction.smithing / 60 } else { 0 } });
            MedianQi=Get-Median @($all.Wallet.lingqi); MedianInsight=Get-Median @($all.Wallet.insight); MedianStones=Get-Median @($all.Wallet.spirit_stones)
        }
    } | Sort-Object ExploreInputs,RoundInputs,Rate)
    $reportData = [ordered]@{
        Model='native_input_threshold_comparison'; Schedule='8h online /16h offline'; ObservationDays=$Days; Repeats=$Repeats; PreparationPolicy=$Preparation;
        Scope='First-region-only in-memory parameter experiments; input-driven combat and exploration; no shipped balance changes.';
        Assumptions=@('Fresh zero-resource game for every sample; breakthrough when affordable, equip by current stronger-item hint. Funded-early enhances to+3 whenever earned materials permit; after-failure defers enhancement until a failed Boss attempt.',
            'No time-driven online exploration, battle or crafting. Shared time growth, recovery and existing offline projection retain their shipped rules.',
            'Round override applies to all spawn-table monsters and the Boss in the first region; baseline0 keeps shipped heterogeneous thresholds.',
            'Runtime randomness is unseeded; a small number of repeated samples is a screening comparison, not a precise win-rate estimate or paired-seed experiment.',
            'Clear-time statistics include completed samples only; Cleared/Samples and censored samples must be considered before selecting a candidate.',
            'Management actions/checks are automated policy operations, not measured human clicks or attention. Presentation remains on settings for speed; raw native logs and full case JSON remain in .godot/input_matrix.');
        Groups=$groups; Samples=@($samples | Sort-Object ExploreInputs,RoundInputs,Rate,Trial)
    }
    [System.IO.File]::WriteAllText($reportPath, ($reportData | ConvertTo-Json -Depth 100) + "`n", (New-Object System.Text.UTF8Encoding($false)))
    Write-Output "REPORT: $reportPath"
}
finally {
    foreach ($run in @($active.ToArray())) { if (-not $run.Process.HasExited) { $run.Process.Kill(); $null = $run.Process.WaitForExit(5000) }; $run.Process.Dispose() }
    Pop-Location
}
