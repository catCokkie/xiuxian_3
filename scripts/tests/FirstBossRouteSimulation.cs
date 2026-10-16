using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests;

// Accelerated native gameplay sample. Only this isolated test scene shifts clocks;
// no player save, global input hook, planned recipe or free resource is used.
public partial class FirstBossRouteSimulation : Node
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const double Epoch = 1800000000;
    private ServiceLocator _s = null!;
    private PrototypeRootController _root = null!;
    private ExploreProgressController _controller = null!;
    private double _seconds, _onlineSeconds, _credit;
    private double? _readyAt, _attemptAt, _clearAt, _swapAt, _postClearSwapAt;
    private int _decisions, _lastBossRealm, _lastBossEnhance = -1;
    private int _managementChecks, _exploreInputs, _roundInputs;
    private double _bossStartedAt;
    private double _bossStartedOnlineAt;
    private int _bossStartRealm, _bossStartHp;
    private int _bossRoundThreshold;
    private CharacterStatBlock _bossStartStats;
    private string _preparation = "funded-early";
    private readonly List<object> _bosses = new();
    private CharacterStatBlock _lastBossStats;
    private bool _lastBossHadHeal;
    private readonly List<object> _trace = new();
    private readonly Dictionary<string, double> _onlineByAction = new();

    public override void _Ready()
    {
        if (!OS.GetCmdlineUserArgs().Contains("--smoke-test")) { GetTree().Quit(2); return; }
        _root = GD.Load<PackedScene>("res://scenes/PrototypeRoot.tscn").Instantiate<PrototypeRootController>(); AddChild(_root);
        CallDeferred(nameof(Run));
    }

    private static string Arg(string key, string fallback) => OS.GetCmdlineUserArgs().FirstOrDefault(x => x.StartsWith(key + "="))?.Substring(key.Length + 1) ?? fallback;
    private void Record(string kind, string detail) => _trace.Add(new { Hours = Math.Round(_seconds / 3600, 4), OnlineHours = Math.Round(_onlineSeconds / 3600, 4), Kind = kind, Detail = detail,
        Realm = _s.PlayerProgressState!.RealmLevel, Qi = Math.Round(_s.ResourceWalletState!.Lingqi, 2), Insight = Math.Round(_s.ResourceWalletState.Insight, 2), Stones = _s.ResourceWalletState.SpiritStones });
    private bool Cleared => _s.LevelConfigLoader!.GetBossDefeatedLevelIds().Contains("lv_qi_001");

    private async void Run()
    {
        try
        {
            // Let the real root finish its new-game load and deferred service wiring.
            for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _s = ServiceLocator.Instance!; _controller = _root.GetNode<ExploreProgressController>("ExploreProgressController");
            double rate = double.Parse(Arg("--route-rate", "30"), System.Globalization.CultureInfo.InvariantCulture);
            int days = Math.Clamp(int.Parse(Arg("--route-days", "3")), 1, 7);
            bool mixed = Arg("--route-schedule", "mixed") == "mixed";
            _exploreInputs = int.Parse(Arg("--route-explore-inputs", "5000"));
            _roundInputs = int.Parse(Arg("--route-round-inputs", "0"));
            _preparation = Arg("--route-preparation", "funded-early");
            if (_preparation != "funded-early" && _preparation != "after-failure") throw new ArgumentException("Unknown preparation policy.");
            string content = File.ReadAllText(ProjectSettings.GlobalizePath("res://docs/design/09_level_monster_drop_sample.json"));
            string equipment = File.ReadAllText(ProjectSettings.GlobalizePath("res://docs/design/12_equipment_sample_qi_refining.json"));
            if (!_s.LevelConfigLoader!.LoadConfigFromText(EquipmentConfigMergeRules.Merge(FirstBossScenarioRules.Apply(content, _exploreInputs, _roundInputs), equipment)))
                throw new InvalidOperationException("Experimental configuration failed to load.");
            if (Math.Abs(_controller.ProgressPerInput - 100.0 / _exploreInputs) > .000001)
                throw new InvalidOperationException("Experimental exploration rate was not applied.");
            if (_roundInputs > 0 && (!_s.LevelConfigLoader.TryGetMonsterStatProfile(_s.LevelConfigLoader.GetBossMonsterId(), out var bossProfile) || bossProfile.InputsPerRound != _roundInputs))
                throw new InvalidOperationException("Experimental round threshold was not applied.");
            // Keep the existing settings page selected: gameplay signals remain
            // connected, while expensive hidden home text is not rebuilt per input.
            var tabs = Descendants(_root).OfType<BookTabsController>().Single();
            tabs.GetNode<Button>("TopStrip/RightTabs/SettingsTab").EmitSignal(Button.SignalName.Pressed);
            if (!double.IsFinite(rate) || rate < 0 || rate > 90) throw new ArgumentException("Rate must be0..90/min.");
            // Only the simulator drives time after initialization. Signals still run the
            // real gameplay settlement, battle, inventory and root persistence wiring.
            foreach (Node node in Descendants(GetTree().Root)) node.SetProcess(false);
            _s.PlayerProgressState!.AutoBreakthrough = false;
            _s.InputHookService!.SetPaused(true);
            _s.ActivityConversionService!.FromDictionary(new() { ["last_settled_unix"] = Epoch, ["cycle_elapsed_seconds"] = 0.0 });
            Record("start", "new game; starter gear; zero wallet and inventory; boss-only consumables");
            if (_s.PlayerProgressState.RealmLevel != 1 || _s.ResourceWalletState!.Lingqi != 0 || _s.ResourceWalletState.Insight != 0 || _s.ResourceWalletState.SpiritStones != 0
                || _s.BackpackState!.GetEquipmentInstances().Length != 0 || _s.BackpackState.GetItemEntries().Count != 0 || _s.PotionInventoryState!.GetPotionEntries().Count != 0)
                throw new InvalidOperationException("Simulation did not start from a fresh game.");
            int previousBosses = 0, previousKills = 0;
            for (int day = 0; day < days && !Cleared; day++)
            {
                int sessionSeconds = mixed ? 8 * 3600 : 24 * 3600;
                for (int tick = 0; tick < sessionSeconds && !Cleared; tick++)
                {
                    ShiftQuotas(_seconds, _seconds + 1); _seconds++; _onlineSeconds++;
                    _controller.AdvanceHealthRecovery(1);
                    _s.CultivationRhythmState!._Process(1); _s.ActivityConversionService!._Process(1);
                    if ((int)_seconds % 10 == 0) _s.ActivityConversionService.ApplyOnlineBasicGrowth(Epoch + _seconds);
                    string mode = _s.PlayerActionState!.ActionId;
                    _onlineByAction[mode] = _onlineByAction.GetValueOrDefault(mode) + 1;
                    _credit += rate / 60; int inputs = (int)Math.Floor(_credit + 1e-9); _credit -= inputs;
                    for (int k = 0; k < inputs; k++) _s.InputActivityState!.RegisterKeyDown();
                    _s.InputActivityState!._Process(1);
                    bool completedBattle = previousKills != _s.PlayerStatsState!.TotalMonsterKills || previousBosses != _s.PlayerStatsState.TotalBossBattles;
                    if (previousBosses != _s.PlayerStatsState.TotalBossBattles)
                    {
                        var report = _s.ChallengeGoalState!.LastBossReport;
                        if (report != null) _bosses.Add(new { StartHours = Hours(_bossStartedAt), EndHours = Hours(_seconds), DurationMinutes = (_seconds - _bossStartedAt) / 60,
                            OnlineMinutes = (_onlineSeconds - _bossStartedOnlineAt) / 60, RoundThreshold = _bossRoundThreshold,
                            StartRealm = _bossStartRealm, StartHp = _bossStartHp, StartStats = _bossStartStats,
                            report.Result, report.Rounds, report.EnemyHp, Realm = _s.PlayerProgressState!.RealmLevel });
                        Record("boss_result", report == null ? "missing report" : $"{report.Result}; rounds={report.Rounds}; enemyHP={report.EnemyHp}");
                    }
                    previousKills = _s.PlayerStatsState.TotalMonsterKills; previousBosses = _s.PlayerStatsState.TotalBossBattles;
                    if (Cleared && _clearAt == null) { _clearAt = _seconds; Record("first_clear", "first region cleared and real rewards granted"); }
                    if (_controller.IsBossChallengeReady && _readyAt == null) { _readyAt = _seconds; Record("boss_ready", "exploration reached100%"); }
                    if ((int)_onlineSeconds % 60 == 0 || completedBattle) Manage();
                    if ((int)_onlineSeconds % 3600 == 0) GD.Print($"ROUTE TICK rate={rate} elapsedHours={_seconds / 3600:0.0} onlineHours={_onlineSeconds / 3600:0.0} realm={_s.PlayerProgressState.RealmLevel} explore={_controller.ExploreProgressValue:0.0}");
                    if (tick % 600 == 0) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
                if (!Cleared && mixed) { ApplyNight(16 * 3600); Manage(); }
            }
            Record("end", Cleared ? "first-clear sample completed" : "observation limit reached; no success inferred");
            var reportData = new
            {
                Model = "native_first_boss_route_sample", ExploreInputs = _exploreInputs, RoundInputOverride = _roundInputs,
                PreparationPolicy = _preparation,
                RatePerMinute = rate, Schedule = mixed ? "8h online /16h offline" : "continuous online", LimitDays = days,
                Cleared, BossReadyHours = Hours(_readyAt), FirstBossAttemptHours = Hours(_attemptAt), FirstClearHours = Hours(_clearAt),
                FirstEquipmentSwapHours = Hours(_swapAt), FirstPostClearSwapHours = Hours(_postClearSwapAt), ElapsedHours = Math.Round(_seconds / 3600, 4),
                OnlineHours = Math.Round(_onlineSeconds / 3600, 4), ManagementActions = _decisions, ManagementChecks = _managementChecks, BossAttempts = _bosses,
                Realm = _s.PlayerProgressState.RealmLevel, Experience = _s.PlayerProgressState.RealmExp,
                ExploreProgress = _controller.ExploreProgressValue, Stats = _controller.GetPreparationStats(),
                Wallet = SaveValueConversionRules.ToPlainDictionary(_s.ResourceWalletState!.ToDictionary()),
                BattleStats = SaveValueConversionRules.ToPlainDictionary(_s.PlayerStatsState!.ToDictionary()),
                Inventory = _s.BackpackState!.GetItemEntries(), Potions = _s.PotionInventoryState!.GetPotionEntries(), OnlineSecondsByAction = _onlineByAction,
                InputLedger = SaveValueConversionRules.ToPlainDictionary(_s.InputActivityState!.ToDictionary()),
                Equipped = _s.EquippedItemsState!.GetEquippedProfiles(), EquipmentInBag = _s.BackpackState.GetEquipmentInstances(), Trace = _trace,
                Assumptions = new[] { "native controller and real input aggregation, settlement, growth, battle, drops, crafting and swaps", "one-second online ticks; keyboard AP1; basic growth every10sec; no free resources",
                    "manage every60 online seconds and after combat; manual breakthrough whenever affordable; use UI stronger-item hint",
                    "spend earned stones on a five-heal bundle before Boss if affordable; enhance weapon up to+3 only from existing earned materials; no mining detour or mastery purchase",
                    "retry Boss only after realm, stats, enhancement or healing preparation improves; no future recipe is used",
                    "offline calls the actual root settlement with a shifted test clock and unchanged growth phase; hourly/day quota rollover is advanced in test memory",
                    "no automatic Boss or offline real input; current HP carries across battles and recovery uses runtime rules",
                    "random encounters, criticals and rewards use current unseeded runtime generators; this is a sample, not a win-rate estimate or exact replay",
                    "management actions are policy operations, not measured UI clicks or human attention; no conclusion about fun or comprehension",
                    "scenario overrides first-region exploration and first-region monsters' input thresholds only, in process memory; round override0 retains each monster's shipped value",
                    "funded-early enhances whenever materials are affordable; after-failure defers enhancement until the first failed Boss attempt; both may equip drops and buy earned healing before the first challenge",
                    "settings page stays selected to avoid rebuilding hidden home text; this sample does not validate presentation",
                    "no player save or global hook; native return occurs at a real process timestamp then the shared watermark is translated back to virtual time" }
            };
            string output = ProjectSettings.GlobalizePath(Arg("--route-output", "res://.godot/first_boss_route.json"));
            string repo = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"));
            if (!Path.GetFullPath(output).StartsWith(repo, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Report outside project.");
            File.WriteAllText(output, JsonSerializer.Serialize(reportData, new JsonSerializerOptions { WriteIndented = true }));
            if (!_s.InputHookService.IsPaused || _s.ResourceWalletState.Lingqi < 0 || _s.BackpackState.GetItemEntries().Values.Any(x => x < 0)) throw new InvalidOperationException("Simulation invariant violated.");
            GD.Print($"ROUTE COMPLETE rate={rate} cleared={Cleared} hours={_seconds / 3600:0.00} realm={_s.PlayerProgressState.RealmLevel} report={output}"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"ROUTE FAILURE: {error}"); GetTree().Quit(1); }
    }

    private void Manage()
    {
        _managementChecks++;
        while (_s.PlayerProgressState!.TryBreakthrough()) { _decisions++; Record("breakthrough", "confirmed manual breakthrough"); }
        if (_controller.IsInBattle) return;
        foreach (var instance in _s.BackpackState!.GetEquipmentInstances())
        {
            string comparison = EquipmentPresentationRules.BuildComparisonHint(instance, _s.EquippedItemsState!.GetEquippedProfiles());
            if ((comparison.StartsWith("更强") || comparison == "可直接穿戴") && EquipmentExchangeService.TryEquipById(_s, instance.EquipmentId))
            {
                _decisions++; _swapAt ??= _seconds; if (Cleared) _postClearSwapAt ??= _seconds;
                Record("equip", instance.DisplayName + ": " + comparison);
            }
        }
        if (Cleared) return;
        var weapon = _s.EquippedItemsState!.GetEquippedProfiles().First(p => p.Slot == EquipmentSlotType.Weapon);
        if (_s.PlayerActionState!.ActionId == "smithing")
        {
            if (_s.SmithingState!.Production.Finished || !_controller.GetProductionCheck("smithing").CanProduce)
            { _s.SmithingState.Production.Stop(); _s.PlayerActionState.SetAction("dungeon"); _decisions++; Record("stop_smithing", "one batch completed or blocked; return to dungeon"); }
            else return;
        }
        if ((_preparation == "funded-early" || _s.PlayerStatsState!.TotalBossBattles > 0)
            && _s.PlayerProgressState.RealmLevel >= 3 && weapon.EnhanceLevel < 3 && SmithingRules.CanEnhance(weapon, _s.BackpackState, _s.ResourceWalletState!, _s.SubsystemMasteryState!.GetLevel("smithing")))
        {
            _s.SmithingState!.SelectTarget(weapon.EquipmentId, weapon.EnhanceLevel); _s.SmithingState.Production.Start(false, 1);
            _s.PlayerActionState.SetAction("smithing"); _decisions++; Record("start_smithing", weapon.DisplayName + $" +{weapon.EnhanceLevel} → +{weapon.EnhanceLevel + 1}"); return;
        }
        if (!_controller.CanStartBossChallenge) return;
        if (_s.PotionInventoryState!.GetPotionCount("potion_huiqi_dan") == 0 && _s.ShopState!.TryPurchase(ShopRules.ItemHuiqiDanBundle, out _))
        { _decisions++; Record("buy_heal", "bought five huiqi pills with earned stones"); }
        var stats = _controller.GetPreparationStats(); bool heal = _s.PotionInventoryState.GetPotionCount("potion_huiqi_dan") > 0;
        bool improved = _s.PlayerProgressState.RealmLevel > _lastBossRealm || stats != _lastBossStats || weapon.EnhanceLevel > _lastBossEnhance || heal && !_lastBossHadHeal;
        if (!improved) return;
        if (_controller.StartBossChallengeFromUi())
        {
            _bossStartedAt = _seconds;
            _bossStartedOnlineAt = _onlineSeconds;
            _bossRoundThreshold = _controller.ToRuntimeDictionary()["inputs_per_battle_round_runtime"].AsInt32();
            _bossStartRealm = _s.PlayerProgressState.RealmLevel; _bossStartStats = stats;
            _bossStartHp = _controller.ToRuntimeDictionary()["player_hp"].AsInt32();
            if (_roundInputs > 0 && _controller.ToRuntimeDictionary()["inputs_per_battle_round_runtime"].AsInt32() != _roundInputs)
                throw new InvalidOperationException("Started battle did not use the experimental threshold.");
            _lastBossRealm = _s.PlayerProgressState.RealmLevel; _lastBossStats = stats; _lastBossEnhance = weapon.EnhanceLevel; _lastBossHadHeal = heal;
            _attemptAt ??= _seconds; _decisions++; Record("boss_start", $"real challenge; hp={_controller.ToRuntimeDictionary()["player_hp"].AsInt32()}; heal={heal}");
        }
    }

    private void ApplyNight(int duration)
    {
        Record("leave", $"action={_s.PlayerActionState!.ActionId}; explore={_controller.ExploreProgressValue:0.0}%; inBattle={_controller.IsInBattle}");
        // Test-only time translation: preserve the accrued phase, and make the
        // production root see exactly this unclaimed real-time interval once.
        _s.ActivityConversionService!.ApplyOnlineBasicGrowth(Epoch + _seconds);
        double phase = _s.ActivityConversionService.GrowthClock.CycleElapsedSeconds;
        ShiftQuotas(_seconds, _seconds + duration);
        long realNow = (long)Time.GetUnixTimeFromSystem();
        _s.ActivityConversionService.FromDictionary(new() { ["last_settled_unix"] = realNow - duration, ["cycle_elapsed_seconds"] = phase });
        typeof(PrototypeRootController).GetField("_lastLoadedSavedUnix", Private)!.SetValue(_root, realNow - duration);
        typeof(PrototypeRootController).GetField("_offlineSettlementApplied", Private)!.SetValue(_root, false);
        bool settled = (bool)typeof(PrototypeRootController).GetMethod("ApplyOfflineSettlementIfNeeded", Private)!.Invoke(_root, null)!;
        if (!settled) throw new InvalidOperationException("Native root did not settle the offline interval.");
        typeof(PrototypeRootController).GetMethod("ApplyOfflineRhythmIfNeeded", Private)!.Invoke(_root, null);
        // Reopening the application starts a fresh transient input window. Lifetime
        // input/activity counters remain intact; the night is never sent as online input.
        _s.InputActivityState!.ResetCurrentTick();
        foreach (string field in new[] { "_runtimeSeconds", "_activeUntilSeconds", "_acceptedApInWindow", "_pendingIdleSecondsBeforeBatch" })
            typeof(InputActivityState).GetField(field, Private)!.SetValue(_s.InputActivityState, 0.0);
        typeof(InputActivityState).GetProperty("SecondsSinceLastInput")!.SetValue(_s.InputActivityState, 0.0);
        phase = _s.ActivityConversionService.GrowthClock.CycleElapsedSeconds; _seconds += duration;
        _s.ActivityConversionService.FromDictionary(new() { ["last_settled_unix"] = Epoch + _seconds, ["cycle_elapsed_seconds"] = phase });
        Record("return", $"native root settled{duration / 3600} offline hours; explore={_controller.ExploreProgressValue:0.0}%");
        if (_controller.IsBossChallengeReady && _readyAt == null) _readyAt = _seconds;
    }

    private void ShiftQuotas(double before, double after)
    {
        var runtime = typeof(LevelConfigLoader).GetField("_runtimeStateService", Private)!.GetValue(_s.LevelConfigLoader)!;
        if ((long)((Epoch + before) / 3600) != (long)((Epoch + after) / 3600)) ((Dictionary<string, int>)runtime.GetType().GetField("_hourlyRollCountByTable", Private)!.GetValue(runtime)!).Clear();
        if ((long)((Epoch + before) / 86400) != (long)((Epoch + after) / 86400)) ((Dictionary<string, int>)runtime.GetType().GetField("_dailyRollCountByTable", Private)!.GetValue(runtime)!).Clear();
    }
    private static double? Hours(double? seconds) => seconds.HasValue ? Math.Round(seconds.Value / 3600, 4) : null;
    private static IEnumerable<Node> Descendants(Node node) { foreach (Node child in node.GetChildren()) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
}
