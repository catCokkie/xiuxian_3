using Godot;
using System.Collections.Generic;
using System.Linq;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests
{
    public partial class GameplaySmokeTest
    {
        private void RunProductionChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
        {
            s.ResourceWalletState!.AddLingqi(100000);
            s.SubsystemMasteryState!.FromDictionary(new() { ["alchemy"] = 4, ["talisman"] = 4, ["cooking"] = 4, ["formation"] = 4, ["smithing"] = 4 });
            foreach (string item in new[] { "spirit_herb", "broken_talisman", "spirit_ink", "spirit_fish", "spirit_spring_water", "spirit_jade", "cold_iron_ore", "lingqi_shard" }) s.BackpackState!.AddItem(item, 100);
            s.CookingState!.AdvanceMeal(100000);
            foreach (string system in new[] { "alchemy", "talisman", "cooking", "formation", "smithing" })
            {
                s.PlayerActionState!.SetAction(system);
                CraftingProductionPlan plan;
                IDictionaryPersistable state;
                if (system == "alchemy") { state = s.AlchemyState!; s.AlchemyState!.FromDictionary(new()); s.AlchemyState.SelectRecipe("recipe_huiqi_dan"); plan = s.AlchemyState.Production; }
                else if (system == "smithing")
                {
                    state = s.SmithingState!; s.SmithingState!.FromDictionary(new());
                    var weapon = s.EquippedItemsState!.GetEquippedProfiles().First();
                    s.EquippedItemsState.Equip(weapon with { EnhanceLevel = 0 });
                    s.SmithingState.SelectTarget(weapon.EquipmentId, 0); plan = s.SmithingState.Production;
                }
                else if (system == "formation") { state = s.FormationState!; s.FormationState!.FromDictionary(new()); s.FormationState.SelectRecipe("formation_guard_flag"); plan = s.FormationState.Production; }
                else
                {
                    var recipeState = system == "talisman" ? s.TalismanState! : s.CookingState!;
                    state = recipeState; recipeState.FromDictionary(new());
                    recipeState.SelectRecipe(system == "talisman" ? "talisman_fire_charm" : "cooking_spirit_porridge"); plan = recipeState.Production;
                }
                Check(plan.RoundLimit == 1, system + ": newly selected production defaults to one round");
                controller.AdvanceOfflineCrafting(50000);
                Check(plan.CompletedRounds == 0 && plan.Paused, system + ": selecting a target waits for explicit start before consuming materials");
                plan.Start(false, 1);
                controller.AdvanceOfflineCrafting(50000);
                Check(plan.CompletedRounds == 1 && plan.Finished, system + ": large offline budget stops after one committed round");
                state.FromDictionary(Roundtrip(state.ToDictionary())); controller.AdvanceOfflineCrafting(50000);
                Check(plan.CompletedRounds == 1 && !plan.CanProduce, system + ": reloading completed plan never restarts it");
                plan.Configure(false, 2); controller.AdvanceOfflineCrafting(1); plan.Stop();
                var stopped = state.ToDictionary(); state.FromDictionary(Roundtrip(stopped)); controller.AdvanceOfflineCrafting(50000);
                Check(plan.Paused && plan.CompletedRounds == 0 && state.ToDictionary()["progress"].AsDouble() == stopped["progress"].AsDouble(), system + ": manual stop preserves partial batch and survives save");
                plan.Start(false, 2); controller.AdvanceOfflineCrafting(50000);
                Check(plan.CompletedRounds == 2 && plan.Finished, system + ": resume completes exactly the remaining rounds");
            }
            s.PlayerActionState!.SetAction("alchemy");
            s.AlchemyState!.Production.Configure(true); int before = s.PotionInventoryState!.GetPotionEntries().GetValueOrDefault("potion_huiqi_dan");
            controller.AdvanceOfflineCrafting(400);
            Check(s.AlchemyState.Production.CompletedRounds == 2 && s.AlchemyState.Production.CanProduce && s.PotionInventoryState.GetPotionEntries().GetValueOrDefault("potion_huiqi_dan") == before + 6,
                "continuous alchemy counts two rounds independently of six output items");
            s.BackpackState!.FromDictionary(new()); s.AlchemyState.Production.Configure(false, 3);
            s.AlchemyState.AdvanceProgress(75); controller.AdvanceOfflineCrafting(1000);
            Check(s.AlchemyState.Production.CompletedRounds == 0 && s.AlchemyState.CurrentProgress == 75, "missing materials neither consume round budget nor advance partial production");
            s.BackpackState.AddItem("spirit_herb", 30);
            stateRoundtrip();
            controller.AdvanceOfflineCrafting(125);
            Check(s.AlchemyState.Production.CompletedRounds == 1 && s.AlchemyState.CurrentProgress == 0, "funding a restored partial batch completes exactly once");
            for (int i = 0; i < 400; i++) s.InputActivityState!.RegisterKeyDown(); s.InputActivityState!._Process(61);
            Check(s.AlchemyState.Production.CompletedRounds == 3 && s.AlchemyState.Production.Finished, "online inputs and offline crafting consume the same finite plan");

            var tabs = Descendants(root).OfType<BookTabsController>().Single(); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            var production = (Control)tabs.FindChild("HomeProduction", true, false);
            var mode = production.GetNode<OptionButton>("ProductionActions/ProductionMode");
            var rounds = production.GetNode<SpinBox>("ProductionActions/ProductionRounds");
            var dirty = typeof(PrototypeRootController).GetField("_saveDirty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            dirty.SetValue(root, false);
            mode.Select(0); rounds.GetLineEdit().Text = "4"; production.GetNode<Button>("ProductionActions/ProductionStart").EmitSignal(Button.SignalName.Pressed);
            Check(s.AlchemyState.Production.RoundLimit == 4 && s.AlchemyState.Production.CompletedRounds == 0, "UI custom rounds starts a fresh completed plan");
            Check((bool)dirty.GetValue(root)!, "production settings mark autosave dirty even before new inputs");
            dirty.SetValue(root, false);
            production.GetNode<Button>("ProductionActions/ProductionStop").EmitSignal(Button.SignalName.Pressed);
            Check(s.AlchemyState.Production.Paused, "UI stop freezes the selected plan");
            Check((bool)dirty.GetValue(root)!, "manual production stop marks autosave dirty without changing action mode");
            mode.Select(1); production.GetNode<Button>("ProductionActions/ProductionStart").EmitSignal(Button.SignalName.Pressed);
            Check(s.AlchemyState.Production.Continuous && s.AlchemyState.Production.CanProduce, "UI continuous mode explicitly enables unlimited production");
            void stateRoundtrip() => s.AlchemyState.FromDictionary(Roundtrip(s.AlchemyState.ToDictionary()));
        }
    }
}
