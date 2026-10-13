using Godot;
using System;
using System.Linq;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests
{
    public partial class GameplaySmokeTest
    {
        private void RunPreparationAndProjectChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
        {
            s.ResourceWalletState!.AddLingqi(100000);
            foreach (string item in new[] { "spirit_herb", "broken_talisman", "spirit_ink", "spirit_fish", "spirit_spring_water", "cold_scale_fish", "spirit_flower", "dragon_saliva", "spirit_jade", "cold_iron_ore", "lingqi_shard" }) s.BackpackState!.AddItem(item, 100);
            CheckProject("alchemy", s.AlchemyState!, s.AlchemyState!.Production, () => s.AlchemyState.SelectRecipe("recipe_huiqi_dan"), () => s.AlchemyState.SelectRecipe("recipe_juling_san"));
            CheckProject("talisman", s.TalismanState!, s.TalismanState!.Production, () => s.TalismanState.SelectRecipe("talisman_fire_charm"), () => s.TalismanState.SelectRecipe("talisman_shield_charm"));
            CheckProject("cooking", s.CookingState!, s.CookingState!.Production, () => s.CookingState.SelectRecipe("cooking_spirit_porridge"), () => s.CookingState.SelectRecipe("cooking_fruit_jelly"));
            CheckProject("formation", s.FormationState!, s.FormationState!.Production, () => s.FormationState.SelectRecipe("formation_guard_flag"), () => s.FormationState.SelectRecipe("formation_spirit_plate"));
            var equipment = s.EquippedItemsState!.GetEquippedProfiles().Take(2).ToArray();
            Check(equipment.Length == 2, "project fixture has distinct equipped instances");
            foreach (var item in equipment) s.EquippedItemsState.Equip(item with { EnhanceLevel = 0 });
            void SelectEquipment(int i) { s.EquippedItemsState.TryGetEquippedProfileById(equipment[i].EquipmentId, out var item); s.SmithingState!.SelectTarget(item.EquipmentId, item.EnhanceLevel); }
            CheckProject("smithing", s.SmithingState!, s.SmithingState!.Production, () => SelectEquipment(0), () => SelectEquipment(1));
            s.SmithingState!.FromDictionary(new()); s.EquippedItemsState.Equip(equipment[0] with { EnhanceLevel = 0 }); SelectEquipment(0);
            s.SmithingState.Production.Start(false, 3); s.SmithingState.AdvanceProgress(100); SelectEquipment(1);
            s.EquippedItemsState.Equip(equipment[0] with { EnhanceLevel = 2 }); SelectEquipment(0);
            Check(s.SmithingState.CurrentProgress == 0 && s.SmithingState.RequiredProgress == 200, "external enhancement cannot reuse archived ready low-tier progress");

            var tabs = Descendants(root).OfType<BookTabsController>().Single(); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            s.PlayerProgressState!.FromDictionary(new() { ["realm_level"] = 9 }); s.PlayerActionState!.SetConsumablePolicy("boss_only");
            s.LevelConfigLoader!.TrySetActiveLevel("lv_qi_003");
            var panel = (Control)tabs.FindChild("PreparationDetails", true, false);
            var recovery = (Control)panel.FindChild("Preparation_recipe_huiqi_dan", true, false);
            s.PotionInventoryState!.FromDictionary(new());
            tabs._Process(1.1);
            var target = (SpinBox)recovery.FindChild("StockTarget", true, false); target.GetLineEdit().Text = "5";
            int herbsBefore = s.BackpackState!.GetItemCount("spirit_herb");
            ((Button)recovery.FindChild("ArrangeProduction", true, false)).EmitSignal(Button.SignalName.Pressed);
            Check(s.AlchemyState!.Production.RoundLimit == 2 && s.AlchemyState.Production.Paused && s.PlayerActionState.ActionId == "alchemy", "preparation UI turns editable stock target into funded batch count without starting");
            Check(s.BackpackState.GetItemCount("spirit_herb") == herbsBefore && s.PotionInventoryState.GetPotionCount("potion_huiqi_dan") == 0, "preparation and project switching never charge materials or grant output");
            Check(((Label)recovery.FindChild("PreparationDetail", true, false)).Text.Contains("灵草"), "preparation UI exposes concrete material requirements and sources");
            s.PotionInventoryState.AddPotion("potion_huiqi_dan", 10); tabs._Process(1.1);
            Check(((Button)recovery.FindChild("ArrangeProduction", true, false)).Disabled, "sufficient stock disables redundant preparation");
            s.PlayerActionState.SetConsumablePolicy("none"); tabs._Process(1.1);
            Check(!recovery.Visible, "preparation respects disabled automatic consumables");

            var runtime = s.LevelConfigLoader.ToRuntimeDictionary(); var cleared = new Godot.Collections.Array<Variant>();
            foreach (string id in s.LevelConfigLoader.GetLevelIds()) cleared.Add(id);
            runtime["boss_cleared_level_ids"] = cleared; s.LevelConfigLoader.FromRuntimeDictionary(runtime);
            s.PlayerProgressState.FromDictionary(new() { ["realm_level"] = 9, ["realm_exp"] = 100000.0 });
            double required = s.PlayerProgressState.RealmExpRequired;
            int breakthroughEvents = s.EventLogState!.GetEntries().Count(x => x.CategoryId == EventLogState.CategoryBreakthrough);
            s.PlayerProgressState.AutoBreakthrough = true; s.PlayerProgressState.AddRealmExp(1);
            Check(s.PlayerProgressState.RealmLevel == 10 && Math.Abs(s.PlayerProgressState.RealmExp - (100001 - required)) < .001, "automatic overflow breakthrough stops at chapter end and preserves experience");
            double bank = s.PlayerProgressState.RealmExp;
            Check(!s.PlayerProgressState.TryBreakthrough() && s.PlayerProgressState.RealmExp == bank, "manual breakthrough cannot spend banked experience beyond content cap");
            s.PlayerProgressState.AddRealmExp(42);
            Check(s.PlayerProgressState.RealmLevel == 10 && s.PlayerProgressState.RealmExp == bank + 42, "continued cultivation still banks experience at the chapter end");
            Check(s.EventLogState.GetEntries().Count(x => x.CategoryId == EventLogState.CategoryBreakthrough) == breakthroughEvents + 1, "chapter milestone produces one breakthrough event without repeated reward");
            double stones = s.ResourceWalletState.SpiritStones;
            Check(!s.ShopState!.CanPurchase(ShopRules.ItemBreakthroughPill, out string reason) && reason.Contains("上限"), "chapter end blocks a breakthrough purchase without usable next realm");
            Check(!s.ShopState.TryPurchase(ShopRules.ItemBreakthroughPill, out _) && s.ResourceWalletState.SpiritStones == stones, "blocked chapter-end purchase never spends stones");
            s.PlayerProgressState.FromDictionary(Roundtrip(s.PlayerProgressState.ToDictionary())); tabs._Process(1.1);
            Check(((Label)tabs.FindChild("ChapterSummary", true, false)).Text.Contains("炼气篇完成"), "native chapter card derives completion from persisted realm and five-region records");
            s.PlayerProgressState.FromDictionary(new() { ["realm_level"] = 12, ["realm_exp"] = 3000.0 }); s.PlayerProgressState.AddRealmExp(1);
            Check(s.PlayerProgressState.RealmLevel == 12 && s.PlayerProgressState.RealmExp == 3001 && !s.PlayerProgressState.CanBreakthrough, "legacy higher realm and bank are preserved without permitting further unsupported levels");
            s.PlayerProgressState.AutoBreakthrough = false;

            void CheckProject(string system, IDictionaryPersistable state, CraftingProductionPlan plan, Action selectA, Action selectB)
            {
                state.FromDictionary(new()); s.PlayerActionState!.SetAction(system);
                selectA(); plan.Start(false, 3); controller.AdvanceOfflineCrafting(50);
                double aProgress = state.ToDictionary()["progress"].AsDouble();
                selectB(); plan.Start(false, 2); controller.AdvanceOfflineCrafting(40);
                double bProgress = state.ToDictionary()["progress"].AsDouble();
                state.FromDictionary(Roundtrip(state.ToDictionary())); selectA();
                Check(state.ToDictionary()["progress"].AsDouble() == aProgress && plan.RoundLimit == 3 && plan.Paused, system + ": A-B-save-A restores original frozen progress and rounds");
                controller.AdvanceOfflineCrafting(5000);
                Check(state.ToDictionary()["progress"].AsDouble() == aProgress && plan.CompletedRounds == 0, system + ": returned project waits for continue");
                plan.Start(false, 3); controller.AdvanceOfflineCrafting(5000);
                Check(plan.CompletedRounds == 3 && plan.Finished, system + ": resumed archive completes exactly its own rounds");
                selectB();
                Check(state.ToDictionary()["progress"].AsDouble() == bProgress && plan.RoundLimit == 2 && plan.CompletedRounds == 0, system + ": inactive B never advances while A runs");
                selectA(); state.FromDictionary(Roundtrip(state.ToDictionary())); controller.AdvanceOfflineCrafting(5000);
                Check(plan.CompletedRounds == 3 && plan.Finished, system + ": finished project never revives on switching or reloading");
            }
        }
    }
}
