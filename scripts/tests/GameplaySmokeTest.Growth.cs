using Godot;
using System;
using System.Linq;
using System.Reflection;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests
{
    public partial class GameplaySmokeTest
    {
        private void RunSharedGrowthChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
        {
            var service = s.ActivityConversionService!; var wallet = s.ResourceWalletState!; var player = s.PlayerProgressState!;
            var tabs = Descendants(root).OfType<BookTabsController>().Single();
            void Seed(double time, double phase = 0) => service.FromDictionary(new() { ["last_settled_unix"] = time, ["cycle_elapsed_seconds"] = phase });
            void Reset()
            {
                player.AutoBreakthrough = false; player.FromDictionary(new() { ["realm_level"] = 9, ["realm_exp"] = 0.0 });
                wallet.FromDictionary(new()); s.PlayerActionState!.SetConsumablePolicy("none");
            }
            foreach (string mode in ProgressiveUnlockRules.GetAllActionModesInUnlockOrder())
            {
                Reset(); s.PlayerActionState!.SetAction(mode); Seed(1000);
                var beforeInput = s.InputActivityState!.ToDictionary(); var beforeRhythm = s.CultivationRhythmState!.ToDictionary();
                double active = player.CurrentRealmActiveSeconds;
                var grant = service.ApplyOnlineBasicGrowth(4600);
                Check(Math.Abs(player.RealmExp - 168) < .00001 && Math.Abs(wallet.Insight - 48) < .00001 && wallet.Lingqi > 540,
                    mode + ": quiet online time pays shared basic experience insight and realm-scaled qi");
                Check(player.CurrentRealmActiveSeconds == active && beforeInput["total_active_seconds"].Equals(s.InputActivityState.ToDictionary()["total_active_seconds"])
                    && beforeRhythm["current_cycle_active_seconds"].Equals(s.CultivationRhythmState.ToDictionary()["current_cycle_active_seconds"]),
                    mode + ": quiet growth does not fabricate input activity or rhythm cycles");
                Check(service.ApplyOnlineBasicGrowth(4600).RealmExpGain == 0 && Math.Abs(player.RealmExp - 168) < .00001,
                    mode + ": duplicate time claim cannot pay twice");
            }
            Reset(); s.PlayerActionState!.SetAction("cultivation"); Seed(1000); service.ApplyOnlineBasicGrowth(2800);
            service.FromDictionary(Roundtrip(service.ToDictionary())); var tail = service.ClaimBasicGrowth(4600);
            wallet.AddLingqi(tail.LingqiGain); wallet.AddInsight(tail.InsightGain); player.AddRealmExp(tail.RealmExpGain);
            Check(Math.Abs(player.RealmExp - 168) < .00001 && service.GrowthClock.InputsPerMinute == 8,
                "shared growth: online then offline native save roundtrip keeps the same phase and hourly total");
            service.FromDictionary(Roundtrip(service.ToDictionary()));
            Check(service.ClaimBasicGrowth(4600).RealmExpGain == 0, "shared growth: restored time watermark prevents repeated offline claim");
            Reset(); Seed(1000); service.ApplyOnlineBasicGrowth(1000 + 7 * 86400 + 3600);
            Check(Math.Abs(player.RealmExp - 1780.8) < .00001 && service.GrowthClock.CycleElapsedSeconds == 3600,
                "shared growth: long suspended interval caps rewards at24h while advancing the real cycle phase");
            double exp = player.RealmExp; service.ApplyOnlineBasicGrowth(1000);
            Check(player.RealmExp == exp, "shared growth: clock rollback cannot award time or reset the phase");
            // Persistence manager writes the actual registered autoload, without touching a player save.
            var persistence = (StatePersistenceManager)typeof(PrototypeRootController).GetField("_persistenceManager", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(root)!;
            var config = new ConfigFile(); persistence.WriteAll(config);
            var growth = (Godot.Collections.Dictionary<string, Variant>)config.GetValue("growth", "baseline");
            Check(growth["last_settled_unix"].AsDouble() == service.GrowthClock.LastSettledUnix, "shared growth: root unified persistence includes the real time ledger");
            Reset(); Seed(1000); s.PlayerActionState.SetAction("talisman"); s.TalismanState!.SelectRecipe("talisman_fire_charm"); s.TalismanState.Production.Start(false, 1);
            s.BackpackState!.AddItem("broken_talisman", 10); s.BackpackState.AddItem("spirit_ink", 10);
            float progress = s.TalismanState.CurrentProgress; var battle = controller.ToRuntimeDictionary();
            s.BackpackState.AddItem("food_focus_fish_stew", 1); s.SubsystemMasteryState!.FromDictionary(new() { ["cooking"] = 4 });
            s.CookingState!.TryActivateMeal("food_focus_fish_stew"); double remaining = s.CookingState.ActiveMealRemainingSeconds;
            service.ApplyOnlineBasicGrowth(4600);
            Check(s.TalismanState.CurrentProgress == progress && s.TalismanState.Production.CompletedRounds == 0 && s.CookingState.ActiveMealRemainingSeconds == remaining,
                "shared growth: time grant neither advances a recipe nor consumes meal duration");
            Check(controller.ToRuntimeDictionary()["explore_progress"].Equals(battle["explore_progress"]) && controller.ToRuntimeDictionary()["battle_round_counter"].Equals(battle["battle_round_counter"]),
                "shared growth: passive budget does not fabricate exploration or combat rounds");
            double now = Math.Floor(Time.GetUnixTimeFromSystem()); Reset(); Seed(now - 3600); s.PlayerActionState.SetAction("cultivation");
            var savedAt = typeof(PrototypeRootController).GetField("_lastLoadedSavedUnix", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var applied = typeof(PrototypeRootController).GetField("_offlineSettlementApplied", BindingFlags.Instance | BindingFlags.NonPublic)!;
            savedAt.SetValue(root, (long)now - 3600); applied.SetValue(root, false);
            var applyOffline = typeof(PrototypeRootController).GetMethod("ApplyOfflineSettlementIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Check((bool)applyOffline.Invoke(root, null)! && Math.Abs(player.RealmExp - 168) < .05, "shared growth: real root offline settlement grants the common hourly budget once");
            exp = player.RealmExp;
            Check(!(bool)applyOffline.Invoke(root, null)! && player.RealmExp == exp, "shared growth: root repeat-offline guard prevents a second settlement");
            tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            var label = (Label)tabs.FindChild("HomeBasicGrowth", true, false);
            Check(label.TooltipText.Contains("线上线下") && label.Text.Contains("2.24")
                && label.Text.Contains(s.ActivityConversionService!.CurrentInputExperienceRate.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)),
                "shared growth UI: home distinguishes shared time growth from the current input experience rate: " + label.Text);
            Reset(); Seed(now - 3600, 28800); s.PlayerActionState.SetAction("talisman");
            s.TalismanState.FromDictionary(new()); s.TalismanState.SelectRecipe("talisman_fire_charm"); s.TalismanState.Production.Start(true, 1);
            s.FormationState!.FromDictionary(new()); s.CookingState.FromDictionary(new());
            savedAt.SetValue(root, (long)now - 3600); applied.SetValue(root, false); applyOffline.Invoke(root, null);
            Check(s.TalismanState.CurrentProgress == 90 && s.TalismanState.Production.CompletedRounds == 0 && Math.Abs(player.RealmExp - 50.4) < .05,
                "shared growth: offline crafting uses the persisted late-cycle budget at half efficiency");
            Reset(); Seed(now - 3600, 28800); s.PlayerActionState.SetAction("dungeon"); s.LevelConfigLoader!.TrySetActiveLevel("lv_qi_001");
            controller.FromRuntimeDictionary(new() { ["explore_progress"] = 0f, ["battle_state"] = "exploring" });
            savedAt.SetValue(root, (long)now - 3600); applied.SetValue(root, false); applyOffline.Invoke(root, null);
            Check(controller.ExploreProgressValue > 3.5 && controller.ExploreProgressValue < 3.7 && Math.Abs(player.RealmExp - 50.4) < .05,
                "shared growth: real offline exploration reuses the same late-cycle budget without an extra experience payment");
            // Exercise the same timer entry used in a normal session; smoke mode otherwise leaves it disabled.
            Reset(); Seed(Time.GetUnixTimeFromSystem() - 20); s.PlayerActionState.SetWorkMode(true); s.InputHookService!.SetPaused(true);
            var automatic = typeof(ActivityConversionService).GetField("_automaticGrowthEnabled", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var dirty = typeof(PrototypeRootController).GetField("_saveDirty", BindingFlags.Instance | BindingFlags.NonPublic)!;
            dirty.SetValue(root, false); automatic.SetValue(service, true); service._Process(0); automatic.SetValue(service, false);
            Check(player.RealmExp > 1 && wallet.Insight > 0, "shared growth: normal timer works in quiet work mode with input collection paused");
            Check((bool)dirty.GetValue(root)!, "shared growth: timer settlement marks the unified save dirty");
            Seed(Time.GetUnixTimeFromSystem() + 100); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(label.Text.Contains("等待时间恢复"), "shared growth UI: rewound clock is explained without claiming ongoing payment");
            s.PlayerActionState.SetWorkMode(false);
            // Restore a current watermark so the rest of an isolated test run cannot accidentally backdate time.
            Seed(Time.GetUnixTimeFromSystem()); savedAt.SetValue(root, 0L); applied.SetValue(root, false); s.CookingState.FromDictionary(new());
        }
    }
}
