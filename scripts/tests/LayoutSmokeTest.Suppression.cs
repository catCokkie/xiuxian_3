using Godot;
using System.Linq;
using System.Threading.Tasks;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests;

public partial class LayoutSmokeTest
{
    private async Task CheckSuppressionPresentation(Vector2I size)
    {
        var s = ServiceLocator.Instance!; var action = s.PlayerActionState!; var loader = s.LevelConfigLoader!;
        IDictionaryPersistable[] states = { action, s.PlayerProgressState!, s.SubsystemMasteryState!, s.BackpackState!, s.PotionInventoryState!, s.ChallengeGoalState!, s.FishingState! };
        var saved = states.Select(x => x.ToDictionary()).ToArray(); var regions = loader.ToRuntimeDictionary();
        try
        {
            s.PlayerProgressState!.FromDictionary(new() { ["realm_level"] = 4 });
            s.SubsystemMasteryState!.FromDictionary(new() { ["alchemy"] = 2, ["talisman"] = 2 });
            s.ChallengeGoalState!.Cancel();
            loader.FromRuntimeDictionary(new() { ["active_level_id"] = "lv_qi_004",
                ["unlocked_level_ids"] = new Godot.Collections.Array<string> { "lv_qi_001", "lv_qi_002", "lv_qi_003", "lv_qi_004" },
                ["boss_cleared_level_ids"] = new Godot.Collections.Array<string> { "lv_qi_001", "lv_qi_002", "lv_qi_003" } });
            loader.TrySetActiveLevelIfUnlocked("lv_qi_004"); action.SetAction("dungeon"); action.SetWorkMode(false); action.SetConsumablePolicy("boss_only");
            s.BackpackState!.FromDictionary(new()); s.BackpackState.AddItem("spirit_herb", 30); s.PotionInventoryState!.FromDictionary(new());
            await Select("CultivationTab");
            ((Button)_tabs.FindChild("PreparationToggle", true, false)).ButtonPressed = true;
            var panel = (Control)_tabs.FindChild("Preparation_" + RecipeUnlockRules.Yangyuan, true, false);
            var target = (SpinBox)panel.FindChild("StockTarget", true, false);
            target.Value = 1; target.GetLineEdit().Text = "1";
            var choice = (OptionButton)_tabs.FindChild("SuppressionChoice", true, false);
            choice.Select(1); choice.EmitSignal(OptionButton.SignalName.ItemSelected, 1L); await Settle();
            var scroll = (ScrollContainer)PageRoot("CultivationScroll"); scroll.EnsureControlVisible(choice); await Settle();
            Check(choice.IsVisibleInTree() && !choice.Disabled && Inside(scroll.GetGlobalRect(), choice.GetGlobalRect()), size + ": antiheal selector fits and is reachable");
            var detail = (Label)panel.FindChild("PreparationDetail", true, false);
            Check(panel.Visible && !((Control)_tabs.FindChild("Preparation_" + RecipeUnlockRules.Zhenling, true, false)).Visible
                && detail.Text.Contains("灵珠") && detail.Text.Contains("概率产出") && detail.Text.Contains("70"), size + ": potion-only recommendation explains both costs and random pearl source");
            var source = (Button)panel.FindChild("PreparationSource", true, false);
            scroll.EnsureControlVisible(source); await Settle();
            Check(source.IsVisibleInTree() && Inside(scroll.GetGlobalRect(), source.GetGlobalRect()) && detail.Size.X <= scroll.Size.X,
                size + ": ingredient source button and wrapped costs fit paper");
            if (size.X is 800 or 1600) await Capture("suppression_preparation_" + size.X);
            source.EmitSignal(Button.SignalName.Pressed); await Settle();
            Check(action.ActionId == "fishing" && s.FishingState!.SelectedRecipeId == FishingRules.GetPonds()[0].RecipeId,
                size + ": missing pearl opens actual basic pond without creating materials");
            action.SetAction("dungeon"); await Settle();
            choice.Select(2); choice.EmitSignal(OptionButton.SignalName.ItemSelected, 2L); await Settle();
            Check(!panel.Visible && action.SuppressionChoice == "none", size + ": equipment route hides antiheal preparation");
            action.SetConsumablePolicy("none"); await Settle();
            Check(choice.Disabled, size + ": total consumable policy disables preparation choice");
            s.PotionInventoryState.AddPotion(SuppressionRules.PotionId, 1); await Select("BackpackTab");
            Check(Xiuxian.Scripts.UI.GameArtCatalog.Item(SuppressionRules.PotionId)?.ResourcePath.EndsWith("potion_yangyuan_dan_v1.svg") == true, size + ": new potion has its own simple icon");
            var cell = _tabs.FindChildren("*", "Label", true, false).Cast<Label>().Single(x => x.Text == "抑生丹").GetParent<Control>();
            cell.EmitSignal(Control.SignalName.MouseEntered); await Settle();
            var popup = (Control)_tabs.FindChild("InventoryDetailPopup", true, false);
            Check(popup.Visible && Inside(GetViewport().GetVisibleRect(), popup.GetGlobalRect())
                && popup.GetChild<Label>(0).Text.Contains("不恢复自身气血")
                && cell.GetNode<TextureRect>("ItemIcon").Texture?.ResourcePath.EndsWith("potion_yangyuan_dan_v1.svg") == true,
                size + ": actual inventory icon and antiheal explanation fit the viewport");
            if (size.X == 800) await Capture("suppression_inventory_800");
            cell.EmitSignal(Control.SignalName.MouseExited);
        }
        finally
        {
            ((Button)_tabs.FindChild("PreparationToggle", true, false)).ButtonPressed = false;
            for (int i = 0; i < states.Length; i++) states[i].FromDictionary(saved[i]); loader.FromRuntimeDictionary(regions);
            await Select("CultivationTab"); ((ScrollContainer)PageRoot("CultivationScroll")).ScrollVertical = 0; await Settle();
        }
    }
}
