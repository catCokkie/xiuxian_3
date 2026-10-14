using Godot;
using System.Linq;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests
{
    public partial class GameplaySmokeTest
    {
        private void RunProductionBlockChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
        {
            s.ChallengeGoalState!.Cancel(); s.PlayerProgressState!.FromDictionary(new() { ["realm_level"] = 5 });
            s.SubsystemMasteryState!.FromDictionary(new() { ["formation"] = 2 }); s.CookingState!.FromDictionary(new()); s.FormationState!.FromDictionary(new());
            var tabs = Descendants(root).OfType<BookTabsController>().Single(); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            var panel = (Control)tabs.FindChild("HomeProduction", true, false);
            var status = panel.GetNode<Label>("ProductionStatus");
            foreach (string system in new[] { "alchemy", "talisman", "cooking", "formation", "smithing" })
            {
                s.BackpackState!.FromDictionary(new()); s.ResourceWalletState!.FromDictionary(new() { ["lingqi"] = 1000.0 });
                CraftingProductionPlan plan;
                if (system == "alchemy") { s.AlchemyState!.FromDictionary(new()); s.AlchemyState.SelectRecipe("recipe_huiqi_dan"); plan = s.AlchemyState.Production; }
                else if (system == "smithing")
                {
                    var weapon = s.EquippedItemsState!.GetEquippedProfiles().First(); s.EquippedItemsState.Equip(weapon with { EnhanceLevel = 0, Rarity = 3 });
                    s.SmithingState!.FromDictionary(new()); s.SmithingState.SelectTarget(weapon.EquipmentId, 0); plan = s.SmithingState.Production;
                }
                else if (system == "formation") { s.FormationState!.SelectRecipe("formation_guard_flag"); plan = s.FormationState.Production; }
                else
                {
                    var state = system == "talisman" ? s.TalismanState! : s.CookingState!; state.FromDictionary(new());
                    state.SelectRecipe(system == "talisman" ? "talisman_fire_charm" : "cooking_spirit_porridge"); plan = state.Production;
                }
                s.PlayerActionState!.SetAction(system); plan.Start(true, 1);
                var missing = controller.GetProductionCheck(system);
                foreach (var material in missing.Materials) s.BackpackState.AddItem(material.ItemId, 100);
                string id = missing.Materials.First().ItemId; s.BackpackState.SetReservedCount(id, 100);
                var check = controller.GetProductionCheck(system); string reason = ProductionBlockRules.Summary(check);
                double qi = s.ResourceWalletState.Lingqi;
                string offline = controller.AdvanceOfflineCrafting(10000); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
                Check(check.Primary?.Kind == ProductionBlockKind.Reserve && offline.Contains(reason) && status.Text.Contains(reason), system + ": UI and offline use identical exact reserve blocker");
                Check(plan.CompletedRounds == 0 && s.ResourceWalletState.Lingqi == qi && s.BackpackState.GetItemCount(id) == 100, system + ": reserve blockage cannot advance or spend offline");
                bool committed = system switch
                {
                    "alchemy" => CraftingProgressionService.TryCompleteAlchemyBatch(s.AlchemyState, s.BackpackState, s.ResourceWalletState, 1, s.PotionInventoryState, 1, out _),
                    "smithing" => CraftingProgressionService.TryCompleteSmithingBatch(s.SmithingState, s.EquippedItemsState, s.BackpackState, s.ResourceWalletState, 1,
                        s.EquippedItemsState!.GetEquippedProfiles().First(x => x.EquipmentId == s.SmithingState!.TargetEquipmentId), out _, out _),
                    _ => CraftingProgressionService.TryCompleteGenericBatch(system == "formation" ? s.FormationState.SelectedRecipeId : system == "talisman" ? s.TalismanState!.SelectedRecipeId : s.CookingState.SelectedRecipeId,
                        s.BackpackState, s.ResourceWalletState, s.SubsystemMasteryState.GetLevel(system), 1, out _, plan)
                };
                Check(!committed && s.ResourceWalletState.Lingqi == qi && s.BackpackState.GetItemCount(id) == 100, system + ": final transaction honors the same reserve check");
                panel.GetNode<Button>("ProductionRecovery/ProductionHelp").EmitSignal(Button.SignalName.Pressed);
                var choice = (OptionButton)tabs.FindChild("ReserveChoice", true, false);
                Check(tabs.ActivePageName == "BackpackTab" && choice.GetItemMetadata(choice.Selected).AsString() == id && s.BackpackState.GetReservedCount(id) == 100,
                    system + ": recovery locates the exact reserve without silently clearing it");
                s.BackpackState.SetReservedCount(id, 0); s.ResourceWalletState.FromDictionary(new() { ["lingqi"] = 1.0 });
                tabs.RestoreActiveTabs("CultivationTab", "BugTab");
                check = controller.GetProductionCheck(system);
                if (check.LingqiRequired > 0)
                {
                    offline = controller.AdvanceOfflineCrafting(10000);
                    Check(check.Primary?.Kind == ProductionBlockKind.Lingqi && offline.Contains(ProductionBlockRules.Summary(check)) && status.Text.Contains("灵气不足") && plan.CompletedRounds == 0,
                        system + ": qi shortages are not mislabeled as missing materials or enhancement caps");
                }
                else Check(check.CanProduce && status.Text.Contains("材料与灵气满足"), "formation: zero-qi recipe does not invent a qi requirement");
                s.ResourceWalletState.AddLingqi(1000); plan.Stop(); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
                Check(controller.AdvanceOfflineCrafting(10000).Contains("手动停止") && status.Text.Contains("手动停止") && plan.CompletedRounds == 0,
                    system + ": explicit pause remains distinct from resource blockage");
                plan.Start(false, 1); offline = controller.AdvanceOfflineCrafting(10000); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
                Check(plan.CompletedRounds == 1 && offline.Contains("指定轮数已完成") && status.Text.Contains("指定轮数已完成"), system + ": finite completion is reported separately from inability to fund another batch");
            }
            s.BackpackState!.FromDictionary(new()); s.ResourceWalletState!.AddLingqi(1000);
            s.AlchemyState!.SelectRecipe("recipe_huiqi_dan"); s.AlchemyState.Production.Start(true, 1); s.PlayerActionState!.SetAction("alchemy");
            s.AlchemyState.AdvanceProgress(75);
            for (int i = 0; i < 50; i++) s.InputActivityState!.RegisterKeyDown(); s.InputActivityState!._Process(61);
            var info = root.GetNode<Label>("MainBarWindow/Chrome/BattleTrack/BattleInfoLabel");
            Check(s.AlchemyState.CurrentProgress == 75 && info.Text.Contains("材料不足") && info.Text.Contains("可用0"), "online input freezes the same partial batch and reports the precise shortage");
            tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            panel.GetNode<Button>("ProductionRecovery/ProductionHelp").EmitSignal(Button.SignalName.Pressed);
            Check(s.PlayerActionState.ActionId == "garden" && s.GardenState!.SelectedRecipeId == "garden_spirit_herb" && s.AlchemyState.CurrentProgress == 75,
                "material recovery selects the real herb crop and preserves the crafting project");
            s.BackpackState.AddItem("spirit_herb", 100); s.AlchemyState.SelectRecipe("recipe_juling_san"); s.AlchemyState.Production.Start(true, 1); s.PlayerActionState.SetAction("alchemy");
            string locked = controller.AdvanceOfflineCrafting(1000); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(locked.Contains("需Lv2") && s.AlchemyState.SelectedRecipeId == "recipe_juling_san" && s.AlchemyState.CurrentProgress == 0, "locked recipe retains its identity and reports actual mastery requirements");
            panel.GetNode<Button>("ProductionRecovery/ProductionHelp").EmitSignal(Button.SignalName.Pressed);
            Check(((Button)tabs.FindChild("MasteryToggle", true, false)).ButtonPressed, "mastery recovery opens the real research controls without purchasing upgrades");
            var equipped = s.EquippedItemsState!.GetEquippedProfiles().First(); s.EquippedItemsState.Equip(equipped with { EnhanceLevel = 3, Rarity = 3 });
            s.SmithingState!.SelectTarget(equipped.EquipmentId, 3); s.SmithingState.Production.Start(true, 1); s.PlayerActionState.SetAction("smithing");
            Check(controller.AdvanceOfflineCrafting(1000).Contains("精通Lv2可提高上限") && controller.GetProductionCheck("smithing").Materials.Count == 0,
                "mastery enhancement cap recommends the next real unlock instead of nonexistent material costs");
            s.EquippedItemsState.Equip(equipped with { EnhanceLevel = 3, Rarity = 1 }); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(controller.GetProductionCheck("smithing").Primary?.RequiredLevel == 0 && panel.GetNode<Button>("ProductionRecovery/ProductionHelp").Text == "比较其他装备",
                "rarity enhancement cap does not recommend a mastery upgrade that cannot help");
            s.EquippedItemsState.Equip(equipped with { EnhanceLevel = 0, Rarity = 3 }); s.SmithingState.SelectTarget(equipped.EquipmentId, 0);
            s.PlayerProgressState.FromDictionary(new() { ["realm_level"] = 1 });
            Check(controller.GetProductionCheck("smithing").Primary?.Kind == ProductionBlockKind.ActivityLocked && controller.AdvanceOfflineCrafting(1000).Contains("活动尚未开放"),
                "restored unavailable activity cannot bypass its realm gate during offline crafting");
            s.PlayerProgressState.FromDictionary(new() { ["realm_level"] = 5 });
            s.BackpackState.FromDictionary(new()); s.BackpackState.AddItem("spirit_herb", 2);
            s.ResourceWalletState.FromDictionary(new() { ["lingqi"] = 1000.0 });
            s.AlchemyState.FromDictionary(new()); s.AlchemyState.SelectRecipe("recipe_huiqi_dan"); s.AlchemyState.Production.Start(true, 1); s.PlayerActionState.SetAction("alchemy");
            string depleted = controller.AdvanceOfflineCrafting(400);
            Check(s.AlchemyState.Production.CompletedRounds == 1 && depleted.Contains("炼丹完成") && depleted.Contains("灵草需2 / 持有0"),
                "offline summary reports the exact final shortage after committing a funded batch");
            s.BackpackState.AddItem("spirit_herb", 100); s.ResourceWalletState.FromDictionary(new() { ["lingqi"] = 0.0 }); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            panel.GetNode<Button>("ProductionRecovery/ProductionHelp").EmitSignal(Button.SignalName.Pressed);
            Check(s.PlayerActionState.ActionId == "cultivation" && s.AlchemyState.Production.CompletedRounds == 1 && s.AlchemyState.Production.Continuous,
                "qi recovery switches to actual cultivation without changing the independent production budget");
            s.ResourceWalletState.AddLingqi(1000); s.PlayerActionState.SetAction("formation"); s.FormationState.SelectRecipe("formation_guard_flag"); s.FormationState.Production.Start(true, 1);
            s.SubsystemMasteryState.TrySetLevel("mining", 2); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            panel.GetNode<Button>("ProductionRecovery/ProductionHelp").EmitSignal(Button.SignalName.Pressed);
            Check(s.PlayerActionState.ActionId == "mining" && s.MiningState!.SelectedRecipeId == "mining_cold_iron_ore", "material recovery chooses the actual mine for formation ore");
            s.PlayerActionState.SetAction("cooking"); s.CookingState.SelectRecipe("cooking_spirit_porridge"); s.CookingState.Production.Start(true, 1); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            panel.GetNode<Button>("ProductionRecovery/ProductionHelp").EmitSignal(Button.SignalName.Pressed);
            Check(s.PlayerActionState.ActionId == "fishing" && s.FishingState!.SelectedRecipeId == "fishing_spirit_fish", "material recovery selects a genuinely available pond for cooking fish");
            tabs.RestoreActiveTabs("EquipmentTab", "BugTab");
            s.BackpackState.AddItem("lingqi_shard", 5); s.SmithingState.Production.Start(true, 1); s.SmithingState.NotifyProductionChanged();
            var equipmentPanel = (Control)tabs.FindChild("EquipmentProduction", true, false);
            s.BackpackState.SetReservedCount("lingqi_shard", 5);
            Check(equipmentPanel.GetNode<Label>("ProductionStatus").Text.Contains("保留量阻挡"), "equipment blockers refresh immediately after a reserve edit without waiting for a timer");
            s.PlayerActionState.SetAction("alchemy"); s.AlchemyState.SelectRecipe("recipe_huiqi_dan"); s.AlchemyState.Production.Stop();
            s.PlayerActionState.SetWorkMode(true); controller.AdvanceOfflineCrafting(10); controller._Process(.1);
            Check(!info.Visible, "precise production reasons remain quiet in work mode"); s.PlayerActionState.SetWorkMode(false);
        }
    }
}
