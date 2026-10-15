using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests
{
    public partial class GameplaySmokeTest
    {
        private void RunSupplyChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
        {
            var bag = s.BackpackState!; var wallet = s.ResourceWalletState!; var mining = s.MiningState!; var loader = s.LevelConfigLoader!;
            var tabs = Descendants(root).OfType<BookTabsController>().Single();
            void Realm(int level) => s.PlayerProgressState!.FromDictionary(new() { ["realm_level"] = level });
            void Input(int count) { for (int i = 0; i < count; i++) s.InputActivityState!.RegisterKeyDown(); s.InputActivityState!._Process(61); }
            void Mine(int count) => typeof(ExploreProgressController).GetMethod("AdvanceMiningByInput", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(controller, new object[] { count });
            s.ChallengeGoalState!.Cancel(); s.PlayerActionState!.SetConsumablePolicy("none");
            s.SubsystemMasteryState!.FromDictionary(new() { ["mining"] = 1, ["talisman"] = 2, ["smithing"] = 1, ["cooking"] = 4 });
            bag.FromDictionary(new()); wallet.FromDictionary(new() { ["lingqi"] = 10000.0 });
            s.CookingState!.FromDictionary(new()); s.FormationState!.FromDictionary(new()); mining.FromDictionary(new());
            mining.SelectNode(MiningRules.GetNodes()[0].RecipeId); mining.AdvanceProgress(40); int durability = mining.CurrentDurability;
            bag.AddItem("cold_iron_ore", 30); Realm(2); mining.SetRecycling(true); mining.RecyclingProduction.Start(false, 2); s.PlayerActionState.SetAction("mining");
            Mine(300);
            Check(mining.RecycleProgress == 0 && controller.GetProductionCheck("mining").Primary?.Kind == ProductionBlockKind.ActivityLocked, "recycle: actual input cannot bypass realm3 gate");
            Realm(3); double qi = wallet.Lingqi; Mine(250);
            Check(bag.GetItemCount("lingqi_shard") == 2 && bag.GetItemCount("cold_iron_ore") == 24 && wallet.Lingqi == qi - 40, "recycle: finite two batches consume exact costs and award two shards");
            Check(mining.RecyclingProduction.Finished && mining.RecycleProgress == 0, "recycle: finite limit discards excess input without a third reward");
            Check(mining.CurrentProgress == 40 && mining.CurrentDurability == durability, "recycle: gathering progress durability and rewards remain independent");
            mining.RecyclingProduction.Start(false, 2); Mine(50); mining.RecyclingProduction.Stop(); Mine(1000);
            Check(mining.RecycleProgress == 50 && mining.RecyclingProduction.CompletedRounds == 0, "recycle: explicit stop freezes partial progress");
            mining.FromDictionary(Roundtrip(mining.ToDictionary()));
            Check(mining.Recycling && mining.RecycleProgress == 50 && mining.RecyclingProduction.Paused && mining.CurrentProgress == 40, "recycle: native serialization preserves both projects and pause");
            mining.SetRecycling(false); mining.SetRecycling(true);
            Check(mining.RecycleProgress == 50 && mining.RecyclingProduction.Paused, "recycle: returning to operation requires explicit continuation");
            mining.RecyclingProduction.Start(true, 1); bag.SetReservedCount("cold_iron_ore", 24); qi = wallet.Lingqi; Mine(1000);
            Check(mining.RecycleProgress == 50 && wallet.Lingqi == qi && controller.GetProductionCheck("mining").Primary?.Kind == ProductionBlockKind.Reserve, "recycle: reserve floor freezes progress and spending");
            bag.SetReservedCount("cold_iron_ore", 0); Mine(50);
            Check(bag.GetItemCount("lingqi_shard") == 3 && mining.RecyclingProduction.CompletedRounds == 1, "recycle: releasing reserve resumes the saved half batch");
            string offline = controller.AdvanceOfflineCrafting(10000);
            Check(bag.GetItemCount("lingqi_shard") == 3 && mining.RecyclingProduction.CompletedRounds == 1 && offline.Contains("仅在线"), "recycle: offline cannot produce mining or recycling rewards");
            bag.AddItem("food_focus_fish_stew", 1); s.CookingState.TryActivateMeal("food_focus_fish_stew");
            Check(CraftingProgressionService.TryCompleteGenericBatch(RecipeUnlockRules.Recycle, bag, wallet, 4, 10, out _, mining.RecyclingProduction)
                && bag.GetItemCount("lingqi_shard") == 4, "recycle: even double yield and active meal never multiply conversion output");
            s.CookingState.FromDictionary(new());
            var weapon = s.EquippedItemsState!.GetEquippedProfiles().First(); weapon = weapon with { EnhanceLevel = 0 }; s.EquippedItemsState.Equip(weapon);
            s.SmithingState!.FromDictionary(new()); s.SmithingState.SelectTarget(weapon.EquipmentId, 0); s.SmithingState.Production.Start(false, 1);
            Check(CraftingProgressionService.TryCompleteSmithingBatch(s.SmithingState, s.EquippedItemsState, bag, wallet, 1, weapon, out _, out _), "supply chain: recycled shards fund real equipped gear enhancement");
            Check(s.EquippedItemsState.TryGetEquippedProfileById(weapon.EquipmentId, out var improved) && improved.EnhanceLevel == 1, "supply chain: enhancement reaches the actual equipped instance");
            var oldMining = new Godot.Collections.Dictionary<string, Variant> { ["selected_recipe"] = mining.SelectedRecipeId, ["progress"] = 33f, ["current_durability"] = 2 };
            mining.FromDictionary(oldMining);
            Check(!mining.Recycling && mining.RecycleProgress == 0 && mining.RecyclingProduction.Paused && mining.CurrentProgress == 33, "legacy mining: gathering remains intact with inactive paused recycling");
            // Source navigation must use the actual new operation, with no automatic spending.
            bag.FromDictionary(new()); s.SmithingState.SelectTarget(weapon.EquipmentId, 1); s.SmithingState.Production.Start(true, 1); s.PlayerActionState.SetAction("smithing");
            tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            var production = (Control)tabs.FindChild("HomeProduction", true, false);
            production.GetNode<Button>("ProductionRecovery/ProductionHelp").EmitSignal(Button.SignalName.Pressed);
            Check(s.PlayerActionState.ActionId == "mining" && mining.Recycling && mining.RecyclingProduction.Paused, "supply UI: shard deficit opens paused recycling without starting or spending");
            Check(!((Control)tabs.FindChild("HomeAction_mining", true, false)).GetChildren().OfType<OptionButton>().Last().Visible,
                "supply UI: gathering picker is hidden while recycling is selected");
            bag.AddItem("broken_talisman", 30); bag.AddItem("spirit_ink", 20); Realm(4);
            var regional = loader.ToRuntimeDictionary(); regional["boss_cleared_level_ids"] = new Godot.Collections.Array<Variant>(); loader.FromRuntimeDictionary(regional);
            s.TalismanState!.SelectRecipe(RecipeUnlockRules.Zhenling); s.TalismanState.Production.Start(false, 1); qi = wallet.Lingqi;
            Check(!CraftingProgressionService.TryCompleteGenericBatch(RecipeUnlockRules.Zhenling, bag, wallet, 2, 1, out _) && wallet.Lingqi == qi,
                "zhenling: final transaction enforces third-region clear before spending");
            Check(controller.GetProductionCheck("talisman").Blocks.Any(x => x.Kind == ProductionBlockKind.RegionClear), "zhenling: first-clear blocker is separate from mastery or material shortage");
            regional["boss_cleared_level_ids"] = new Godot.Collections.Array<Variant> { "lv_qi_001", "lv_qi_002", "lv_qi_003" };
            regional["unlocked_level_ids"] = new Godot.Collections.Array<Variant> { "lv_qi_001", "lv_qi_002", "lv_qi_003", "lv_qi_004" }; loader.FromRuntimeDictionary(regional);
            s.PlayerActionState.SetAction("talisman"); Input(280);
            Check(bag.GetItemCount(RecipeUnlockRules.Zhenling) == 1 && s.TalismanState.Production.CompletedRounds == 1 && wallet.Lingqi >= qi - 70,
                "zhenling: actual input completes the finite recipe and creates inventory item");
            bag.FromDictionary(Roundtrip(bag.ToDictionary()));
            Check(bag.GetItemCount(RecipeUnlockRules.Zhenling) == 1, "zhenling: crafted talisman survives native inventory serialization");
            loader.TrySetActiveLevelIfUnlocked("lv_qi_004"); s.PlayerActionState.SetAction("dungeon");
            var panel = (Control)tabs.FindChild("Preparation_" + RecipeUnlockRules.Zhenling, true, false); s.PlayerActionState.SetConsumablePolicy("boss_only"); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(panel.Visible && ((Label)panel.FindChild("PreparationDetail", true, false)).Text.Contains("持有1张"), "zhenling UI: healing Boss preparation sees actual crafted stock");
            int oldLimit = controller.MaxBossBattleRounds; controller.MaxBossBattleRounds = 10;
            void Begin(string policy, bool reserve = false)
            {
                s.PlayerActionState.SetConsumablePolicy(policy); bag.SetReservedCount(RecipeUnlockRules.Zhenling, reserve ? bag.GetItemCount(RecipeUnlockRules.Zhenling) : 0);
                controller.FromRuntimeDictionary(new() { ["explore_progress"] = 100f, ["battle_state"] = "boss_ready" }); controller.AdvanceHealthRecovery(100);
                if (!controller.StartBossChallengeFromUi()) throw new InvalidOperationException("suppression fixture could not start Boss");
            }
            Begin("none"); Check(bag.GetItemCount(RecipeUnlockRules.Zhenling) == 1, "zhenling: disabled consumption leaves talisman untouched");
            Begin("boss_only", true); Check(bag.GetItemCount(RecipeUnlockRules.Zhenling) == 1, "zhenling: Boss policy still honors reserved inventory");
            Begin("boss_only"); Check(bag.GetItemCount(RecipeUnlockRules.Zhenling) == 0, "zhenling: healing Boss consumes exactly one available talisman");
            var battle = controller.ToRuntimeDictionary(); battle["enemy_hp"] = 100000; battle["enemy_max_hp"] = 100000; battle["enemy_attack_power"] = 1;
            controller.FromRuntimeDictionary(battle); int threshold = battle["inputs_per_battle_round_runtime"].AsInt32();
            for (int round = 1; round <= 6; round++)
            {
                Input(threshold); var mid = controller.ToRuntimeDictionary(); var attempt = (Godot.Collections.Dictionary<string, Variant>)mid["boss_attempt"];
                Check(mid["battle_round_counter"].AsInt32() == round && (round <= 5 ? attempt["enemy_healing"].AsInt32() == 0 : attempt["enemy_healing"].AsInt32() > 0),
                    $"zhenling real battle: round{round} has expected actual healing");
                if (round == 3) controller.FromRuntimeDictionary(Roundtrip(mid));
                Check(bag.GetItemCount(RecipeUnlockRules.Zhenling) == 0, $"zhenling real battle: round{round} never consumes a second talisman");
            }
            // Finish this attempt through timeout and inspect the existing factual report archive.
            Input(threshold * 4); var report = s.ChallengeGoalState.LastBossReport!;
            Check(report.Result == "timeout" && report.Talismans.GetValueOrDefault(RecipeUnlockRules.Zhenling) == 1 && report.EnemyHealing > 0,
                "zhenling report: successful consumption and post-expiry healing survive reload and timeout");
            loader.TrySetActiveLevelIfUnlocked("lv_qi_003"); bag.AddItem(RecipeUnlockRules.Zhenling, 1); Begin("all");
            Check(bag.GetItemCount(RecipeUnlockRules.Zhenling) == 1, "zhenling: nonhealing Boss never wastes suppression talisman");
            controller.MaxBossBattleRounds = oldLimit; s.PlayerActionState.SetConsumablePolicy("none");
        }
    }
}
