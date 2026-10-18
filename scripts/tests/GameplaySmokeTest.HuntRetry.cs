using Godot;
using System;
using System.Linq;
using System.Reflection;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests;

public partial class GameplaySmokeTest
{
    private void RunHuntRetryChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
    {
        var loader = s.LevelConfigLoader!; var action = s.PlayerActionState!; var goal = s.ChallengeGoalState!;
        var input = s.InputActivityState!; var player = s.PlayerProgressState!; var bag = s.BackpackState!;
        var tabs = Descendants(root).OfType<BookTabsController>().Single();
        var book = Descendants(root).OfType<SubmenuWindowController>().Single();
        IDictionaryPersistable[] states = { player, action, goal, bag, s.EquippedItemsState!, s.ResourceWalletState!,
            s.SubsystemMasteryState!, s.SmithingState!, s.CookingState!, s.FormationState!, s.BodyCultivationState!, s.PlayerStatsState!, input };
        var saved = states.Select(x => x.ToDictionary()).ToArray(); var region = loader.ToRuntimeDictionary(); var runtime = controller.ToRuntimeDictionary();
        bool paused = s.InputHookService!.IsPaused, auto = player.AutoBreakthrough; int limit = controller.MaxBossBattleRounds;
        void Keys(int count)
        {
            for (int i = 0; i < count; i += 10)
            {
                for (int j = 0; j < Math.Min(10, count - i); j++) input.RegisterKeyDown();
                controller.AdvanceHealthRecovery(2.1); input._Process(2.1);
            }
        }
        void BeginBoss()
        {
            action.SetAction("dungeon"); controller.AdvanceHealthRecovery(100);
            if (!controller.StartBossChallengeFromUi()) throw new InvalidOperationException("retry fixture failed to start real Boss");
            Keys(500);
        }
        try
        {
            s.InputHookService.SetPaused(false); input.ResetCurrentTick(); player.AutoBreakthrough = false;
            action.FromDictionary(new()); player.FromDictionary(new() { ["realm_level"] = 2 });
            s.SubsystemMasteryState!.FromDictionary(new());
            loader.FromRuntimeDictionary(new());
            Check(!loader.TrySetHuntTarget("monster_slime_moss"), "hunt: realm gate rejects direct calls");
            player.FromDictionary(new() { ["realm_level"] = 3 });
            Check(!loader.TrySetHuntTarget("monster_slime_moss"), "hunt: first-clear gate rejects direct calls");
            loader.FromRuntimeDictionary(new() { ["active_level_id"] = "lv_qi_002", ["unlocked_level_ids"] = new Godot.Collections.Array<string> { "lv_qi_001", "lv_qi_002" },
                ["boss_cleared_level_ids"] = new Godot.Collections.Array<string> { "lv_qi_001" } });
            loader.TrySetActiveLevelIfUnlocked("lv_qi_002");
            Check(!loader.TrySetHuntTarget(loader.GetBossMonsterId()) && !loader.TrySetHuntTarget("monster_bone_servant"), "hunt: no Boss or foreign monster can enter the target pool");
            var baseline = loader.GetOfflineWeightedMonsters();
            int waveIndex = loader.ToRuntimeDictionary()["active_wave_index"].AsInt32();
            Check(loader.TrySetHuntTarget("monster_slime_moss") && loader.HuntExploreRate() == .8, "hunt: valid target enables the published speed tradeoff");
            var hunting = loader.GetOfflineWeightedMonsters();
            Check(hunting.All(x => x.Weight == baseline.First(b => b.Monster.MonsterId == x.Monster.MonsterId).Weight * (x.Monster.MonsterId == "monster_slime_moss" ? 2 : 1)),
                "hunt: offline and online source tables use the same selective weight multiplier");
            int targets = 0; for (int i = 0; i < 4000; i++) if (loader.RollSpawnMonsterId() == "monster_slime_moss") targets++;
            GD.Print($"HUNT DISTRIBUTION: level={loader.ActiveLevelId} count={targets}/4000 wave={loader.TryGetActiveWaveProgress(out _, out _, out _)}");
            Check(targets is > 1700 and < 2450, "hunt: real spawn selection follows a biased distribution without guaranteeing the target");
            Check(!loader.TryGetActiveWaveProgress(out _, out _, out _) && loader.ToRuntimeDictionary()["active_wave_index"].AsInt32() == waveIndex,
                "hunt: weighted hunting suspends the original sequence cursor without pretending to follow it");
            action.SetAction("alchemy"); action.FromDictionary(Roundtrip(action.ToDictionary())); action.SetAction("dungeon");
            Check(loader.HuntTarget() == "monster_slime_moss", "hunt: target survives action switches and native serialization");
            player.FromDictionary(new() { ["realm_level"] = 2 });
            Check(loader.HuntTarget() == "" && loader.HuntExploreRate() == 1, "hunt: restored preference cannot bypass a missing realm gate");
            player.FromDictionary(new() { ["realm_level"] = 3 });
            loader.TrySetActiveLevelIfUnlocked("lv_qi_001"); Check(loader.HuntTarget() == "", "hunt: another region does not inherit a foreign target");
            loader.TrySetActiveLevelIfUnlocked("lv_qi_002");
            Check(loader.HuntTarget() == "monster_slime_moss", "hunt: returning restores that region's own target");
            controller.FromRuntimeDictionary(new() { ["battle_state"] = "exploring", ["explore_progress"] = 0f });
            var advance = typeof(ExploreProgressController).GetMethod("AdvanceExploreByInput", BindingFlags.Instance | BindingFlags.NonPublic)!;
            advance.Invoke(controller, new object[] { 10 });
            Check(Math.Abs(controller.ExploreProgressValue - controller.ProgressPerInput * 10 * .8) < .0001, "hunt: actual online exploration pays the 80 percent speed cost");
            var offlineHunt = root.BuildOfflineDungeonSettlement(60, 0, 100);
            loader.TrySetHuntTarget(""); var offlineNormal = root.BuildOfflineDungeonSettlement(60, 0, 100); loader.TrySetHuntTarget("monster_slime_moss");
            Check(Math.Abs(offlineHunt.ExploreProgressGain - offlineNormal.ExploreProgressGain * .8) < .0001,
                "hunt: real offline settlement builder applies the same exploration rate");
            controller.FromRuntimeDictionary(new() { ["battle_state"] = "boss_ready", ["explore_progress"] = 100f });
            Check((bool)advance.Invoke(controller, new object[] { 10 })! && controller.ExploreProgressValue == 100, "hunt: explicit hunting keeps ordinary encounters available at BossReady");
            var offlineReady = root.BuildOfflineDungeonSettlement(3600, 0, 10000);
            Check(!offlineReady.HasAnyReward, "hunt: offline BossReady neither farms resources nor starts or resolves a Boss");
            loader.TrySetHuntTarget("");
            Check(!(bool)advance.Invoke(controller, new object[] { 10 })! && controller.ExploreProgressValue == 100, "hunt: returning to normal stops farming without losing challenge progress");

            // Controlled equipment removes crit variance; monsters, battle math, rewards and enhancement costs remain real.
            s.EquippedItemsState!.FromDictionary(new()); bag.FromDictionary(new()); s.SmithingState!.FromDictionary(new());
            s.SubsystemMasteryState!.FromDictionary(new()); s.CookingState!.FromDictionary(new()); s.FormationState!.FromDictionary(new()); s.BodyCultivationState!.FromDictionary(new());
            s.EquippedItemsState.Equip(new("retry_weapon", "对照武器", EquipmentSlotType.Weapon, new(AttackFlat: 3), Rarity: 1));
            s.EquippedItemsState.Equip(new("retry_armor", "对照护具", EquipmentSlotType.Armor, new(MaxHpFlat: 200, DefenseFlat: 10, CritChanceDelta: -1), Rarity: 1));
            action.SetConsumablePolicy("none"); goal.FromDictionary(new());
            goal.TryStart("lv_qi_002", loader.GetBossMonsterId(), new System.Collections.Generic.Dictionary<string, int>());
            controller.MaxBossBattleRounds = 20;
            controller.FromRuntimeDictionary(new() { ["battle_state"] = "boss_ready", ["explore_progress"] = 100f });
            book.SetVisibleImmediate(false); BeginBoss();
            var before = goal.LastBossReport!;
            GD.Print($"HUNT BASELINE: {before.Result} rounds={before.Rounds} atk={before.StartingStats?.Attack} hp={before.StartingHp} remaining={before.EnemyHp}");
            Check(before.Result == "timeout" && before.StartingStats?.Attack == 10 && before.StartingHp > 0, "retry: unmodified Boss fight records an actual baseline and starting attributes");
            s.SmithingState.SelectTarget("retry_weapon", 0);
            book.SetVisibleImmediate(true); tabs.RestoreActivePage("EquipmentTab"); tabs._Process(.3);
            var huntButton = ((Control)tabs.FindChild("EquipmentProduction", true, false)).GetNode<Button>("ProductionRecovery/ProductionHunt");
            Check(huntButton.Visible && !huntButton.Disabled, "retry: missing enhancement material offers the real current-region hunting route");
            huntButton.EmitSignal(Button.SignalName.Pressed);
            Check(action.IsDungeonMode && loader.HuntTarget() != "" && goal.Goal.LevelId == "lv_qi_002" && !goal.Goal.Completed,
                "retry: material navigation preserves the tracked Boss and does not auto-start production or challenge");
            book.SetVisibleImmediate(false); controller.AdvanceHealthRecovery(100);
            int farmingInputs = 0;
            while ((bag.GetSpendableItemCount("lingqi_shard") < 9 || controller.IsInBattle) && farmingInputs < 10000)
            { Keys(10); farmingInputs += 10; }
            Check(bag.GetSpendableItemCount("lingqi_shard") >= 9 && controller.ExploreProgressValue == 100 && goal.LastBossReport == before,
                "retry: ordinary input-driven victories supply shards at BossReady without replacing the Boss baseline");
            int shards = bag.GetItemCount("lingqi_shard"); double spent = s.PlayerStatsState!.TotalSpentLingqi;
            action.SetAction("smithing"); s.SmithingState.Production.Start(false, 3); Keys(450);
            s.EquippedItemsState.TryGetEquippedProfileById("retry_weapon", out var enhanced);
            Check(enhanced.EnhanceLevel == 3 && s.SmithingState.Production.Finished && bag.GetItemCount("lingqi_shard") == shards - 9,
                "retry: finite real enhancement spends nine acquired shards and stops at exactly three rounds");
            Check(controller.GetPreparationStats().Attack == 11 && s.PlayerStatsState.TotalSpentLingqi == spent + 90,
                "retry: actual paid enhancement changes final attack without granting a free stat reward");
            loader.TrySetHuntTarget(""); BeginBoss(); var after = goal.LastBossReport!;
            Check(after.Result == "victory" && after.BossId == before.BossId && goal.PreviousBossReport == before && goal.Goal.Completed,
                "retry: the same Boss is beaten through real combat and the previous attempt remains available");
            Check(after.StartingStats?.Attack == 11 && after.StartingStats.Value.MaxHp == before.StartingStats!.Value.MaxHp && after.RoundLimit == before.RoundLimit,
                "retry: comparison uses actual starting snapshots and equal battle limits");
            goal.FromDictionary(Roundtrip(goal.ToDictionary()));
            book.SetVisibleImmediate(true); tabs.RestoreActivePage("CultivationTab");
            ((Button)tabs.FindChild("BossReviewToggle", true, false)).ButtonPressed = true;
            Check(((Label)tabs.FindChild("BossReviewComparison", true, false)).Text.Contains("攻击10 → 11") && goal.PreviousBossReport?.Result == "timeout",
                "retry: persisted comparison is visible in the existing real Boss review");
            string retained = goal.LastBossReport!.BossId;
            goal.RecordBossReport(after with { BossId = "other" });
            Check(goal.PreviousBossReport == null && retained != goal.LastBossReport.BossId, "retry: unrelated Boss reports cannot be compared as preparation gains");
            GD.Print($"HUNT RETRY SAMPLE: farming_inputs={farmingInputs} shards_spent=9 attack=10->11 baseline={before.Result}/{before.Rounds} result={after.Result}/{after.Rounds}; controlled fixture, not balance evidence");
        }
        finally
        {
            player.AutoBreakthrough = auto; controller.MaxBossBattleRounds = limit;
            for (int i = 0; i < states.Length; i++) states[i].FromDictionary(saved[i]);
            input.ResetCurrentTick(); loader.FromRuntimeDictionary(region); controller.FromRuntimeDictionary(runtime);
            s.InputHookService.SetPaused(paused); book.SetVisibleImmediate(false);
        }
    }
}
