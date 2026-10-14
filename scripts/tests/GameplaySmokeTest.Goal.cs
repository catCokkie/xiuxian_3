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
        private void RunChallengeGoalChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
        {
            var state = s.ChallengeGoalState!; var loader = s.LevelConfigLoader!; var backpack = s.BackpackState!;
            var regions = loader.ToRuntimeDictionary(); regions["unlocked_level_ids"] = loader.GetLevelIds(); loader.FromRuntimeDictionary(regions);
            state.FromDictionary(new()); backpack.FromDictionary(new()); s.PotionInventoryState!.FromDictionary(new()); s.CookingState!.FromDictionary(new());
            s.SubsystemMasteryState!.FromDictionary(new() { ["alchemy"] = 3, ["talisman"] = 2 });
            loader.TrySetActiveLevelIfUnlocked("lv_qi_005"); string bossId = loader.GetBossMonsterId();
            s.PlayerActionState!.SetConsumablePolicy("boss_only"); s.PlayerActionState.SetAction("dungeon");
            s.AlchemyState!.FromDictionary(new()); s.AlchemyState.SelectRecipe("recipe_huiqi_dan"); s.AlchemyState.Production.Start(false, 4); s.AlchemyState.AdvanceProgress(75);
            var tabs = Descendants(root).OfType<BookTabsController>().Single(); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            var panel = (Control)tabs.FindChild("PreparationDetails", true, false);
            var recovery = (Control)panel.FindChild("Preparation_recipe_huiqi_dan", true, false);
            var target = (SpinBox)recovery.FindChild("StockTarget", true, false); target.GetLineEdit().Text = "2";
            ((SpinBox)panel.FindChild("Preparation_talisman_fire_charm", true, false).FindChild("StockTarget", true, false)).Value = 1;
            var pin = (Button)tabs.FindChild("ChallengeGoalPin", true, false);
            var summary = (Label)tabs.FindChild("ChallengeGoalSummary", true, false);
            var dirty = typeof(PrototypeRootController).GetField("_saveDirty", BindingFlags.NonPublic | BindingFlags.Instance)!;
            dirty.SetValue(root, false); double qi = s.ResourceWalletState!.Lingqi;
            pin.EmitSignal(Button.SignalName.Pressed);
            Check(state.Goal.LevelId == "lv_qi_005" && state.Goal.BossId == bossId && state.Goal.GetTarget("recipe_huiqi_dan") == 2 && !state.Goal.Completed,
                $"pinning saves an explicit Boss and edited stock quantities rather than inheriting prior first clear completion (goal {state.Goal.LevelId}/{state.Goal.BossId}, count {state.Goal.GetTarget("recipe_huiqi_dan")}, active {loader.ActiveLevelId}, expected {bossId}, disabled {pin.Disabled})");
            Check((bool)dirty.GetValue(root)! && s.ResourceWalletState.Lingqi == qi && s.AlchemyState.CurrentProgress == 75 && s.AlchemyState.Production.RoundLimit == 4,
                "pinning marks autosave dirty without paying or altering an independent production plan");
            Check(!state.TryStart("locked_zone", "unknown_boss", new System.Collections.Generic.Dictionary<string, int>()) && state.Goal.LevelId == "lv_qi_005",
                "unavailable target cannot replace a valid goal");
            loader.TrySetActiveLevelIfUnlocked("lv_qi_001"); s.PlayerActionState.SetAction("garden"); tabs._Process(1.1);
            Check(state.Goal.LevelId == "lv_qi_005" && summary.Text.Contains("其他区域") && loader.ActiveLevelId == "lv_qi_001",
                "farming in another region keeps the original Boss goal and never switches regions merely to inspect it");
            ((Button)panel.FindChild("PreparationGarden", true, false)).EmitSignal(Button.SignalName.Pressed);
            Check(s.GardenState!.SelectedRecipeId == "garden_spirit_herb" && state.Goal.LevelId == "lv_qi_005" && s.ResourceWalletState.Lingqi == qi,
                "herb source navigation selects the actual crop without planting charging or replacing the Boss goal");
            dirty.SetValue(root, false); target.GetLineEdit().Text = "5"; tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(target.GetLineEdit().Text.StartsWith("5") && state.Goal.GetTarget("recipe_huiqi_dan") == 2, "live panel refresh preserves an unapplied quantity draft without saving it");
            target.Apply();
            Check(state.Goal.GetTarget("recipe_huiqi_dan") == 5 && (bool)dirty.GetValue(root)!, "typed goal quantity updates the saved plan and dirties autosave");
            target.GetLineEdit().Text = "88";
            state.FromDictionary(Roundtrip(state.ToDictionary())); controller.AdvanceOfflineCrafting(10000); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(target.GetLineEdit().Text.StartsWith("5"), "explicit reload discards stale text drafts in favor of saved quantities");
            Check(state.Goal.LevelId == "lv_qi_005" && target.Value == 5 && !state.Goal.Completed && s.AlchemyState.CurrentProgress == 75,
                "native reload and offline fallback preserve goal identity counts and independent inactive production");
            s.PotionInventoryState.AddPotion("potion_huiqi_dan", 5); backpack.AddItem("talisman_fire_charm", 1);
            Check(summary.Text.Contains("库存已满足"), "goal readiness derives from current usable inventory without completing the challenge");
            backpack.SetReservedCount("talisman_fire_charm", 1);
            Check(summary.Text.Contains("可用0/1") && !state.Goal.Completed, "protected talisman is excluded from goal preparation stock");
            backpack.SetReservedCount("talisman_fire_charm", 0); s.PotionInventoryState.ConsumePotion("potion_huiqi_dan");
            Check(summary.Text.Contains("可用4/5") && !summary.Text.Contains("库存已满足"), "consuming previously sufficient preparation restores the live deficit");
            s.PlayerActionState.SetConsumablePolicy("none"); Check(summary.Text.Contains("自动消耗已关闭"), "goal guidance respects disabled automatic consumption");
            s.PlayerActionState.SetConsumablePolicy("boss_only");
            ((Button)tabs.FindChild("ChallengeGoalSkip", true, false)).EmitSignal(Button.SignalName.Pressed);
            state.FromDictionary(Roundtrip(state.ToDictionary())); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(ChallengeGoalPlan.Recipes.All(id => state.Goal.GetTarget(id) == 0) && target.Value == 0 && !state.Goal.Completed,
                "skip preparation saves zero quantities without declaring victory or blocking the goal");
            loader.TrySetActiveLevelIfUnlocked("lv_qi_004"); tabs.RestoreActiveTabs("CultivationTab", "BugTab"); pin.EmitSignal(Button.SignalName.Pressed);
            Check(state.Goal.LevelId == "lv_qi_004" && !state.Goal.Completed && s.AlchemyState.CurrentProgress == 75, "explicit replacement is the only operation that changes the tracked Boss");
            ((Button)tabs.FindChild("ChallengeGoalCancel", true, false)).EmitSignal(Button.SignalName.Pressed);
            Check(!state.Goal.HasGoal && s.AlchemyState.CurrentProgress == 75 && s.AlchemyState.Production.RoundLimit == 4 && s.AlchemyState.Production.CanProduce,
                "cancelling tracking retains production progress rounds and running policy");
            loader.TrySetActiveLevelIfUnlocked("lv_qi_005"); tabs.RestoreActiveTabs("CultivationTab", "BugTab"); pin.EmitSignal(Button.SignalName.Pressed);
            Check(!state.Goal.Completed, "creating a repeated challenge in an already cleared region still needs a new victory");
            state.RecordBossVictory("lv_qi_001", loader.GetBossMonsterId("lv_qi_001")); Check(!state.Goal.Completed, "unrelated Boss victory leaves the selected goal pending");
            s.PlayerActionState.SetAction("dungeon"); controller.FromRuntimeDictionary(new() { ["battle_state"] = "boss_ready", ["explore_progress"] = 100f, ["player_hp"] = 0 });
            controller.AdvanceHealthRecovery(20);
            Check(controller.CanStartBossChallenge, "skipped preparation does not add a new challenge gate");
            controller.StartBossChallengeFromUi(); var battle = controller.ToRuntimeDictionary(); battle["enemy_hp"] = 1; controller.FromRuntimeDictionary(battle);
            for (int i = 0; i < 50; i++) s.InputActivityState!.RegisterKeyDown(); s.InputActivityState!._Process(61);
            Check(state.Goal.Completed && summary.Text.Contains("目标已完成"), "actual matching Boss victory completes tracking through the real combat entry point");
            double rewardQi = s.ResourceWalletState.Lingqi; dirty.SetValue(root, false); state.RecordBossVictory("lv_qi_005", bossId);
            state.FromDictionary(Roundtrip(state.ToDictionary())); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(state.Goal.Completed && s.ResourceWalletState.Lingqi == rewardQi && !(bool)dirty.GetValue(root)!, "completion is idempotent after native save and never grants an extra reward");
            pin.EmitSignal(Button.SignalName.Pressed); Check(!state.Goal.Completed && state.Goal.LevelId == loader.ActiveLevelId, "repeat tracking starts a fresh explicit goal after completion");
            state.SetTarget("recipe_huiqi_dan", 4); s.PotionInventoryState.FromDictionary(new());
            s.SubsystemMasteryState.FromDictionary(new() { ["alchemy"] = 3, ["cooking"] = 4, ["talisman"] = 2 });
            backpack.AddItem("food_focus_fish_stew", 1); s.CookingState.TryActivateMeal("food_focus_fish_stew"); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(((Label)recovery.FindChild("PreparationDetail", true, false)).Text.Contains("建议1轮"), "saved goal uses current meal output when suggesting finite rounds");
            s.CookingState.AdvanceMeal(100000);
            tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(((Label)recovery.FindChild("PreparationDetail", true, false)).Text.Contains("建议2轮") && state.Goal.GetTarget("recipe_huiqi_dan") == 4,
                "meal expiration recalculates live deficits without rewriting saved stock targets");
            state.Cancel();
        }
    }
}
