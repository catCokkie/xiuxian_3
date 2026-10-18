using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests;

public partial class GameplaySmokeTest
{
    private void RunSuppressionChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
    {
        var action = s.PlayerActionState!; var player = s.PlayerProgressState!; var bag = s.BackpackState!;
        var potions = s.PotionInventoryState!; var wallet = s.ResourceWalletState!; var alchemy = s.AlchemyState!;
        var input = s.InputActivityState!; var loader = s.LevelConfigLoader!; var goal = s.ChallengeGoalState!;
        var tabs = Descendants(root).OfType<BookTabsController>().Single();
        IDictionaryPersistable[] states = { action, player, bag, potions, wallet, alchemy, input, goal,
            s.SubsystemMasteryState!, s.CookingState!, s.FormationState!, s.PlayerStatsState! };
        var saved = states.Select(x => x.ToDictionary()).ToArray(); var regions = loader.ToRuntimeDictionary(); var runtime = controller.ToRuntimeDictionary();
        int limit = controller.MaxBossBattleRounds, inputLimit = input.MaxInputPerMinute;
        bool paused = s.InputHookService!.IsPaused, auto = player.AutoBreakthrough;
        void Keys(int count) { for (int i = 0; i < count; i++) input.RegisterKeyDown(); input._Process(0); }
        void ClearRegions(bool cleared)
        {
            loader.FromRuntimeDictionary(new() { ["active_level_id"] = "lv_qi_004",
                ["unlocked_level_ids"] = new Godot.Collections.Array<string> { "lv_qi_001", "lv_qi_002", "lv_qi_003", "lv_qi_004" },
                ["boss_cleared_level_ids"] = cleared ? new Godot.Collections.Array<string> { "lv_qi_001", "lv_qi_002", "lv_qi_003" } : new Godot.Collections.Array<string>() });
            loader.TrySetActiveLevelIfUnlocked("lv_qi_004");
        }
        void Begin(string choice, string policy = "boss_only", string region = "lv_qi_004")
        {
            loader.TrySetActiveLevelIfUnlocked(region); action.SetAction("dungeon"); action.SetSuppressionChoice(choice); action.SetConsumablePolicy(policy);
            controller.FromRuntimeDictionary(new() { ["explore_progress"] = 100f, ["battle_state"] = "boss_ready" });
            controller.AdvanceHealthRecovery(10000);
            if (!controller.StartBossChallengeFromUi()) throw new InvalidOperationException("antiheal fixture could not begin Boss");
        }
        try
        {
            s.InputHookService.SetPaused(false); input.ResetCurrentTick(); input.MaxInputPerMinute = 0; player.AutoBreakthrough = false;
            player.FromDictionary(new() { ["realm_level"] = 4 }); action.FromDictionary(new()); goal.Cancel();
            s.CookingState!.FromDictionary(new()); s.FormationState!.FromDictionary(new());
            s.SubsystemMasteryState!.FromDictionary(new() { ["alchemy"] = 2, ["talisman"] = 2 });
            bag.FromDictionary(new()); potions.FromDictionary(new()); wallet.FromDictionary(new() { ["lingqi"] = 10000.0 });
            alchemy.FromDictionary(new()); alchemy.SelectRecipe(RecipeUnlockRules.Yangyuan); alchemy.Production.Start(false, 2);
            bag.AddItem("spirit_herb", 9); ClearRegions(false); action.SetAction("alchemy");
            Check(controller.GetProductionCheck("alchemy").Blocks.Any(x => x.Kind == ProductionBlockKind.RegionClear), "antiheal crafting: third-clear gate is shown by shared check");
            bag.AddItem("spirit_pearl", 3); double qi = wallet.Lingqi;
            Check(!CraftingProgressionService.TryCompleteAlchemyBatch(alchemy, bag, wallet, 2, potions, 1, out _) && wallet.Lingqi == qi,
                "antiheal crafting: final spend cannot bypass first-clear gate");
            ClearRegions(true); bag.SetReservedCount("spirit_pearl", 3); Keys(260);
            Check(alchemy.CurrentProgress == 0 && bag.GetItemCount("spirit_herb") == 9 && bag.GetItemCount("spirit_pearl") == 3 && wallet.Lingqi >= qi
                && controller.GetProductionCheck("alchemy").Primary?.Kind == ProductionBlockKind.Reserve,
                "antiheal crafting: secondary reserve freezes all progress costs and yield");
            bag.SetReservedCount("spirit_pearl", 0); Keys(130);
            Check(alchemy.CurrentProgress == 130 && potions.GetPotionCount(SuppressionRules.PotionId) == 0, "antiheal crafting: online input stores a real half batch");
            alchemy.SelectRecipe("recipe_huiqi_dan"); alchemy.SelectRecipe(RecipeUnlockRules.Yangyuan);
            alchemy.FromDictionary(Roundtrip(alchemy.ToDictionary()));
            Check(alchemy.Production.Paused && alchemy.CurrentProgress == 130, "antiheal crafting: switch and reload keep separate paused project");
            alchemy.Production.Start(false, 2); qi = wallet.Lingqi; controller.AdvanceOfflineCrafting(390);
            Check(potions.GetPotionCount(SuppressionRules.PotionId) == 2 && bag.GetItemCount("spirit_herb") == 3 && bag.GetItemCount("spirit_pearl") == 1
                && wallet.Lingqi == qi - 140 && alchemy.Production.Finished, "antiheal crafting: offline completes remaining finite budget using both costs once");
            controller.AdvanceOfflineCrafting(2600); Keys(260);
            Check(potions.GetPotionCount(SuppressionRules.PotionId) == 2 && bag.GetItemCount("spirit_pearl") == 1, "antiheal crafting: finished plan cannot spend or produce again");
            potions.FromDictionary(Roundtrip(potions.ToDictionary()));
            Check(potions.GetPotionCount(SuppressionRules.PotionId) == 2, "antiheal crafting: produced inventory survives native serialization");

            action.SetConsumablePolicy("boss_only"); action.SetAction("dungeon"); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            var choice = (OptionButton)tabs.FindChild("SuppressionChoice", true, false);
            choice.Select(1); choice.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
            action.FromDictionary(Roundtrip(action.ToDictionary()));
            Check(action.SuppressionChoice == "potion", "antiheal UI: actual selector persists potion preference");
            Check(((Control)tabs.FindChild("Preparation_" + RecipeUnlockRules.Yangyuan, true, false)).Visible
                && !((Control)tabs.FindChild("Preparation_" + RecipeUnlockRules.Zhenling, true, false)).Visible,
                "antiheal UI: only selected antiheal recipe is recommended");
            choice.Select(2); choice.EmitSignal(OptionButton.SignalName.ItemSelected, 2L);
            Check(!((Control)tabs.FindChild("Preparation_" + RecipeUnlockRules.Yangyuan, true, false)).Visible
                && !((Control)tabs.FindChild("Preparation_" + RecipeUnlockRules.Zhenling, true, false)).Visible,
                "antiheal UI: equipment route hides both consumable preparations");

            controller.MaxBossBattleRounds = 8;
            bag.FromDictionary(new()); bag.AddItem(RecipeUnlockRules.Zhenling, 20);
            potions.FromDictionary(new()); potions.AddPotion(SuppressionRules.PotionId, 20);
            var reports = new List<BossChallengeReport>();
            foreach (string route in new[] { "none", "talisman", "potion" })
            {
                int oldPotion = potions.GetPotionCount(SuppressionRules.PotionId), oldTalisman = bag.GetItemCount(RecipeUnlockRules.Zhenling);
                Begin(route);
                Check(potions.GetPotionCount(SuppressionRules.PotionId) == oldPotion - (route == "potion" ? 1 : 0)
                    && bag.GetItemCount(RecipeUnlockRules.Zhenling) == oldTalisman - (route == "talisman" ? 1 : 0),
                    $"antiheal {route}: battle start spends exactly the selected source");
                var battle = controller.ToRuntimeDictionary(); battle["enemy_hp"] = 100000; battle["enemy_max_hp"] = 100000; battle["enemy_attack_power"] = 1;
                controller.FromRuntimeDictionary(battle); int threshold = battle["inputs_per_battle_round_runtime"].AsInt32();
                int healingAtFive = 0;
                for (int round = 1; round <= 8; round++)
                {
                    Keys(threshold);
                    if (round < 8)
                    {
                        var mid = controller.ToRuntimeDictionary(); var attempt = (Godot.Collections.Dictionary<string, Variant>)mid["boss_attempt"];
                        int healing = attempt["enemy_healing"].AsInt32();
                        Check(mid["battle_round_counter"].AsInt32() == round && (route == "none" || round > 5 ? healing > 0 : healing == 0),
                            $"antiheal {route}: actual round{round} healing obeys absolute first-five window");
                        if (round == 3)
                        {
                            action.SetSuppressionChoice(route == "potion" ? "talisman" : "potion"); controller.FromRuntimeDictionary(Roundtrip(mid));
                        }
                        if (round == 5) healingAtFive = healing;
                        if (round == 6) Check(healing > healingAtFive, $"antiheal {route}: sixth round resumes real enemy healing after reload");
                    }
                }
                var report = goal.LastBossReport!; reports.Add(report);
                Check(report.Result == "timeout" && report.Potions.GetValueOrDefault(SuppressionRules.PotionId) == (route == "potion" ? 1 : 0)
                    && report.Talismans.GetValueOrDefault(RecipeUnlockRules.Zhenling) == (route == "talisman" ? 1 : 0)
                    && potions.GetPotionCount(SuppressionRules.PotionId) == oldPotion - (route == "potion" ? 1 : 0)
                    && bag.GetItemCount(RecipeUnlockRules.Zhenling) == oldTalisman - (route == "talisman" ? 1 : 0),
                    $"antiheal {route}: report and stock agree after timeout reload and changed next-battle choice");
                GD.Print($"ANTIHEAL COMPARISON: route={route} result={report.Result} rounds={report.Rounds} enemyHealing={report.EnemyHealing} damage={report.DamageToBoss} potion={report.Potions.GetValueOrDefault(SuppressionRules.PotionId)} talisman={report.Talismans.GetValueOrDefault(RecipeUnlockRules.Zhenling)}");
            }
            Check(reports.Select(x => x.LevelId + x.BossId).Distinct().Count() == 1 && reports[0].EnemyHealing > reports[1].EnemyHealing
                && reports[1].EnemyHealing == reports[2].EnemyHealing && reports[0].StartingStats == reports[1].StartingStats && reports[1].StartingStats == reports[2].StartingStats,
                "antiheal comparison: same Boss and starting stats show equal five-round alternatives and greater untreated healing");
            int count = potions.GetPotionCount(SuppressionRules.PotionId); Begin("potion", "none");
            Check(potions.GetPotionCount(SuppressionRules.PotionId) == count, "antiheal policy: disabled consumption preserves potion");
            Begin("potion", "all", "lv_qi_003");
            Check(potions.GetPotionCount(SuppressionRules.PotionId) == count, "antiheal threat: nonhealing Boss preserves potion");
            potions.FromDictionary(new()); int talismanCount = bag.GetItemCount(RecipeUnlockRules.Zhenling); Begin("potion");
            Check(bag.GetItemCount(RecipeUnlockRules.Zhenling) == talismanCount, "antiheal shortage: missing potion never falls back to talisman");
            potions.AddPotion(SuppressionRules.PotionId, 2); bag.FromDictionary(new()); Begin("talisman");
            Check(potions.GetPotionCount(SuppressionRules.PotionId) == 2, "antiheal shortage: missing talisman never falls back to potion");
            potions.AddPotion("potion_huiqi_dan", 2); Begin("potion");
            var lowHp = controller.ToRuntimeDictionary(); lowHp["player_hp"] = Math.Max(2, lowHp["player_max_hp"].AsInt32() / 4); lowHp["enemy_attack_power"] = 1; lowHp["enemy_hp"] = 100000; lowHp["enemy_max_hp"] = 100000;
            controller.FromRuntimeDictionary(lowHp); Keys(lowHp["inputs_per_battle_round_runtime"].AsInt32());
            Check(potions.GetPotionCount("potion_huiqi_dan") == 1 && potions.GetPotionCount(SuppressionRules.PotionId) == 1,
                "antiheal recovery: low-HP recovery potion remains independent of antiheal potion");
        }
        finally
        {
            for (int i = 0; i < states.Length; i++) states[i].FromDictionary(saved[i]);
            loader.FromRuntimeDictionary(regions); controller.FromRuntimeDictionary(runtime); controller.MaxBossBattleRounds = limit;
            input.MaxInputPerMinute = inputLimit; player.AutoBreakthrough = auto; s.InputHookService.SetPaused(paused);
            tabs.RestoreActiveTabs("CultivationTab", "BugTab");
        }
    }
}
