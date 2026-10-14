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
        private void RunConsumptionChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
        {
            var backpack = s.BackpackState!; var cooking = s.CookingState!;
            s.SubsystemMasteryState!.FromDictionary(new() { ["alchemy"] = 4, ["smithing"] = 4, ["talisman"] = 4, ["formation"] = 4, ["cooking"] = 4 });
            backpack.FromDictionary(new()); cooking.FromDictionary(new());
            const string soup = "food_clear_fish_soup"; double duration = MealRules.Duration(soup, 4);
            backpack.AddItem(soup, 10);
            Check(cooking.TryActivateMeal(soup, false, 3) && backpack.GetItemCount(soup) == 9 && cooking.MealUsage.ConsumedPortions == 1,
                "meal custom budget includes the first actually consumed portion");
            cooking.AdvanceMeal(duration + 10);
            Check(cooking.MealUsage.ConsumedPortions == 2 && backpack.GetItemCount(soup) == 8 && Math.Abs(cooking.ActiveMealRemainingSeconds - duration + 10) < .01,
                "meal renewal consumes exactly one item and one budget portion");
            double remaining = cooking.ActiveMealRemainingSeconds;
            s.PlayerActionState!.SetAction("alchemy"); controller.AdvanceOfflineCrafting(5000);
            cooking.FromDictionary(Roundtrip(cooking.ToDictionary())); backpack.FromDictionary(Roundtrip(backpack.ToDictionary()));
            Check(cooking.ActiveMealRemainingSeconds == remaining && cooking.MealUsage.ConsumedPortions == 2 && backpack.GetItemCount(soup) == 8,
                "activity switching offline crafting and reload do not spend meals or reset usage budget");
            cooking.AdvanceMeal(duration * 10);
            Check(cooking.MealUsage.ConsumedPortions == 3 && backpack.GetItemCount(soup) == 7 && cooking.ActiveMealRemainingSeconds == 0,
                "long active tick never exceeds finite total meal budget");
            Check(cooking.TryActivateMeal(soup), "new meal use defaults to a single portion"); cooking.AdvanceMeal(duration * 2);
            Check(cooking.MealUsage.PortionLimit == 1 && cooking.MealUsage.ConsumedPortions == 1 && backpack.GetItemCount(soup) == 6,
                "single meal expires without automatic renewal despite surplus stock");
            Check(cooking.TryActivateMeal(soup, true), "explicit continuous meal plan starts"); cooking.AdvanceMeal(10);
            remaining = cooking.ActiveMealRemainingSeconds; int stock = backpack.GetItemCount(soup);
            Check(cooking.TryActivateMeal(soup, false, 2) && backpack.GetItemCount(soup) == stock && cooking.ActiveMealRemainingSeconds == remaining,
                "applying a budget to the current meal neither consumes another item nor refreshes duration");
            cooking.StopMealRenewal();
            Check(cooking.ActiveMealRemainingSeconds == remaining && cooking.ActiveMeal.InsightRate > 0, "stopping renewal preserves the current buff");
            cooking.FromDictionary(Roundtrip(cooking.ToDictionary())); cooking.AdvanceMeal(duration * 2);
            Check(cooking.MealUsage.RenewalStopped && backpack.GetItemCount(soup) == stock && cooking.ActiveMealId == "",
                "stopped renewal survives reload and spends no further portions");
            backpack.SetReservedCount(soup, stock - 2);
            Check(cooking.TryActivateMeal(soup, true), "continuous plan may use stock above the reserve floor"); cooking.AdvanceMeal(duration * 5);
            Check(cooking.MealUsage.ConsumedPortions == 2 && backpack.GetItemCount(soup) == stock - 2 && cooking.ActiveMealId == "",
                "automatic meal renewal stops at the shared inventory reserve floor");
            backpack.AddItem(soup, 2); cooking.AdvanceMeal(10);
            Check(cooking.ActiveMealId == "" && backpack.GetItemCount(soup) == stock, "expired shortage does not silently resume consuming newly acquired meals");
            backpack.SetReservedCount(soup, 0); cooking.FromDictionary(new() { ["active_meal_id"] = soup, ["active_meal_remaining_seconds"] = 123.0 });
            Check(cooking.MealUsage.Continuous && cooking.ActiveMealRemainingSeconds == 123 && backpack.GetItemCount(soup) == stock,
                "legacy active meals preserve continuous policy and remaining buff without duplicate consumption");
            backpack.AddItem("food_iron_fish_meal", 1);
            Check(cooking.TryActivateMeal("food_iron_fish_meal", false, 3), "meal replacement starts its own explicitly selected budget");
            cooking.FromDictionary(Roundtrip(cooking.ToDictionary()));
            Check(cooking.TryActivateMeal(soup, false, 2) && cooking.ActiveMealRemainingSeconds == 123 && cooking.MealUsage.ConsumedPortions == 1,
                "switching back after reload preserves refunded partial duration and counts that portion once");

            var tabs = Descendants(root).OfType<BookTabsController>().Single();
            tabs.RestoreActiveTabs("CultivationTab", "BugTab"); s.PlayerActionState.SetAction("cooking");
            var mealRoot = (Control)tabs.FindChild("MealUsage", true, false);
            var mealMode = mealRoot.GetNode<OptionButton>("MealUsageActions/MealMode");
            var mealPortions = mealRoot.GetNode<SpinBox>("MealUsageActions/MealPortions");
            var dirty = typeof(PrototypeRootController).GetField("_saveDirty", BindingFlags.NonPublic | BindingFlags.Instance)!;
            dirty.SetValue(root, false); mealMode.Select(1); mealPortions.GetLineEdit().Text = "4";
            mealRoot.GetNode<Button>("MealUsageActions/MealUse").EmitSignal(Button.SignalName.Pressed);
            Check(cooking.MealUsage.PortionLimit == 4 && cooking.ActiveMealRemainingSeconds == 123 && (bool)dirty.GetValue(root)!,
                "typed meal budget applies through real UI and marks autosave dirty without spending a new item");
            mealRoot.GetNode<Button>("MealUsageActions/MealStop").EmitSignal(Button.SignalName.Pressed);
            Check(cooking.MealUsage.RenewalStopped && cooking.ActiveMealRemainingSeconds == 123, "UI stop renewal preserves current portion");

            cooking.FromDictionary(new()); s.ResourceWalletState!.AddLingqi(100000);
            foreach (string system in new[] { "alchemy", "smithing", "talisman", "cooking", "formation" })
            {
                backpack.FromDictionary(new());
                foreach (string id in new[] { "spirit_herb", "broken_talisman", "spirit_ink", "spirit_fish", "spirit_spring_water", "spirit_jade", "cold_iron_ore", "lingqi_shard" }) backpack.AddItem(id, 100);
                s.PlayerActionState.SetAction(system);
                IDictionaryPersistable state; CraftingProductionPlan plan; string recipe = "";
                EquipmentStatProfile profile = default;
                if (system == "alchemy") { state = s.AlchemyState!; state.FromDictionary(new()); s.AlchemyState!.SelectRecipe("recipe_huiqi_dan"); plan = s.AlchemyState.Production; }
                else if (system == "smithing")
                {
                    state = s.SmithingState!; state.FromDictionary(new()); profile = s.EquippedItemsState!.GetEquippedProfiles().First() with { EnhanceLevel = 0 };
                    s.EquippedItemsState.Equip(profile); s.SmithingState!.SelectTarget(profile.EquipmentId, 0); plan = s.SmithingState.Production;
                }
                else if (system == "formation")
                {
                    recipe = "formation_guard_flag"; state = s.FormationState!; state.FromDictionary(new());
                    s.FormationState!.SelectRecipe(recipe); plan = s.FormationState.Production;
                }
                else
                {
                    recipe = system == "cooking" ? "cooking_spirit_porridge" : "talisman_fire_charm";
                    RecipeProgressState target = system == "cooking" ? cooking : s.TalismanState!;
                    state = target; state.FromDictionary(new()); target.SelectRecipe(recipe); plan = target.Production;
                }
                string protectedItem = system == "alchemy" ? "spirit_herb" : system == "smithing" ? "lingqi_shard"
                    : ActivityRegistry.GetRecipe(recipe)!.Inputs[0].ItemId;
                backpack.SetReservedCount(protectedItem, 100); plan.Configure(false, 2);
                var partial = state.ToDictionary(); partial["progress"] = 75f; state.FromDictionary(partial);
                double qi = s.ResourceWalletState.Lingqi; var original = backpack.GetItemEntries();
                controller.AdvanceOfflineCrafting(10000);
                Check(plan.CompletedRounds == 0 && state.ToDictionary()["progress"].AsDouble() == 75 && s.ResourceWalletState.Lingqi == qi
                    && original.All(x => backpack.GetItemCount(x.Key) == x.Value), system + ": offline reserve shortage consumes no ingredients qi or round budget");
                bool completed = system == "alchemy" ? CraftingProgressionService.TryCompleteAlchemyBatch(s.AlchemyState, backpack, s.ResourceWalletState, 4, s.PotionInventoryState, 1, out _)
                    : system == "smithing" ? CraftingProgressionService.TryCompleteSmithingBatch(s.SmithingState, s.EquippedItemsState, backpack, s.ResourceWalletState, 4, profile, out _, out _)
                    : CraftingProgressionService.TryCompleteGenericBatch(recipe, backpack, s.ResourceWalletState, 4, 1, out _);
                Check(!completed && s.ResourceWalletState.Lingqi == qi && backpack.GetItemCount(protectedItem) == 100,
                    system + ": final batch completion independently rechecks the reserve floor");
                double exp = s.PlayerProgressState!.RealmExp;
                for (int i = 0; i < 400; i++) s.InputActivityState!.RegisterKeyDown(); s.InputActivityState!._Process(61);
                Check(plan.CompletedRounds == 0 && state.ToDictionary()["progress"].AsDouble() == 75 && backpack.GetItemCount(protectedItem) == 100 && s.PlayerProgressState.RealmExp > exp,
                    system + ": online protected crafting stays frozen while common input growth continues");
                backpack.FromDictionary(Roundtrip(backpack.ToDictionary())); state.FromDictionary(Roundtrip(state.ToDictionary()));
                Check(backpack.GetReservedCount(protectedItem) == 100 && plan.RoundLimit == 2 && plan.CompletedRounds == 0,
                    system + ": saved shared reserve and pending production budget restore together");
                backpack.SetReservedCount(protectedItem, 0); controller.AdvanceOfflineCrafting(10000);
                Check(plan.CompletedRounds == 2 && plan.Finished && backpack.GetItemCount(protectedItem) < 100,
                    system + ": releasing the reserve completes exactly the pending two rounds");
            }
            backpack.SetReservedCount("spirit_herb", backpack.GetItemCount("spirit_herb") + 10);
            Check(backpack.GetSpendableItemCount("spirit_herb") == 0 && !backpack.RemoveItem("spirit_herb", 1), "reserve above current stock blocks spending without deleting stock");
            var choice = (OptionButton)tabs.FindChild("ReserveChoice", true, false);
            int herbIndex = Enumerable.Range(0, choice.ItemCount).First(i => choice.GetItemMetadata(i).AsString() == "spirit_herb");
            choice.Select(herbIndex); choice.EmitSignal(OptionButton.SignalName.ItemSelected, (long)herbIndex);
            var reserveCount = (SpinBox)tabs.FindChild("ReserveCount", true, false); reserveCount.GetLineEdit().Text = "17"; dirty.SetValue(root, false);
            ((Button)tabs.FindChild("ReserveApply", true, false)).EmitSignal(Button.SignalName.Pressed);
            Check(backpack.GetReservedCount("spirit_herb") == 17 && (bool)dirty.GetValue(root)!, "typed shared reserve applies through UI and marks autosave dirty");
            ((Button)tabs.FindChild("ReserveClear", true, false)).EmitSignal(Button.SignalName.Pressed);
            Check(backpack.GetReservedCount("spirit_herb") == 0, "UI clears the selected reserve without changing stock");
            s.GardenState!.FromDictionary(new()); s.GardenState.SelectCrop("garden_spirit_herb"); s.GardenState.SelectPlot(0);
            backpack.AddItem("seed_spirit_herb", 5); backpack.SetReservedCount("seed_spirit_herb", 5);
            int stones = s.ResourceWalletState.SpiritStones;
            Check(!s.GardenState.CanPlantSelectedCrop(out string seedReason) && seedReason.Contains("保留5")
                && !s.GardenState.TryPlantSelectedCrop(out _) && backpack.GetItemCount("seed_spirit_herb") == 5 && s.ResourceWalletState.SpiritStones == stones,
                "protected seeds disable planting with a reason and spend neither seeds nor coins");
            backpack.SetReservedCount("spirit_herb", backpack.GetItemCount("spirit_herb"));
            foreach (var input in ActivityRegistry.GetRecipe("body_cultivation_boneforge")!.Inputs)
                if (input.ItemId != "spirit_herb") backpack.AddItem(input.ItemId, 100);
            double bodyQi = s.ResourceWalletState.Lingqi, bodyDefense = s.PlayerProgressState!.BodyCultivationDefenseRate;
            Check(!CraftingProgressionService.TryCompleteGenericBatch("body_cultivation_boneforge", backpack, s.ResourceWalletState, 4, 1, out _)
                && s.ResourceWalletState.Lingqi == bodyQi && s.PlayerProgressState.BodyCultivationDefenseRate == bodyDefense,
                "body training also respects shared material reserves without charging or granting permanent stats");
        }
    }
}
