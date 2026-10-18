using Godot;
using System.Linq;
using System.Threading.Tasks;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests;

public partial class LayoutSmokeTest
{
    private async Task CheckHuntRetryPresentation(Vector2I dimensions)
    {
        var s = ServiceLocator.Instance!; var loader = s.LevelConfigLoader!; var action = s.PlayerActionState!; var goal = s.ChallengeGoalState!;
        var savedAction = action.ToDictionary(); var savedRegions = loader.ToRuntimeDictionary(); var savedGoal = goal.ToDictionary();
        try
        {
            var regions = loader.ToRuntimeDictionary();
            regions["boss_cleared_level_ids"] = new Godot.Collections.Array<string> { "lv_qi_001" }; loader.FromRuntimeDictionary(regions);
            action.SetAction("dungeon"); action.SetWorkMode(false); await Select("CultivationTab");
            var toggle = (Button)_tabs.FindChild("DungeonHuntToggle", true, false); toggle.ButtonPressed = true;
            var target = (OptionButton)_tabs.FindChild("DungeonHuntTarget", true, false);
            target.Select(1); target.EmitSignal(OptionButton.SignalName.ItemSelected, 1L); await Settle();
            var scroll = (ScrollContainer)PageRoot("CultivationScroll"); scroll.EnsureControlVisible(target); await Settle();
            Check(target.IsVisibleInTree() && Inside(scroll.GetGlobalRect(), target.GetGlobalRect()) && loader.HuntTarget() != "", dimensions + ": hunting picker is visible and changes a real route");
            var summary = (Label)_tabs.FindChild("DungeonHuntSummary", true, false);
            Check(summary.Size.X <= scroll.Size.X && summary.Text.Contains("80%") && summary.Text.Contains("×2") && summary.Text.Contains("原序列"), dimensions + ": hunt tradeoffs and original-sequence boundary wrap within paper");
            foreach (Control button in ((Control)_tabs.FindChild("DungeonHuntActions", true, false)).GetChildren())
            {
                scroll.EnsureControlVisible(button); await Settle();
                Check(Inside(scroll.GetGlobalRect(), button.GetGlobalRect()), dimensions + ": route return action is reachable within scroll");
            }
            scroll.EnsureControlVisible(target); await Settle();
            if (dimensions.X is 1600 or 800) await Capture("hunt_route_" + dimensions.X);
            var stats = new CharacterStatBlock(100, 10, 8, 100, .04, 1.5);
            var before = new BossChallengeReport(loader.ActiveLevelId, loader.GetBossMonsterId(), "timeout", 20, 20, 140, 120, 0, 0,
                new System.Collections.Generic.Dictionary<string, int>(), new System.Collections.Generic.Dictionary<string, int>(), true, 100, stats, 100, 20);
            goal.RecordBossReport(before); goal.RecordBossReport(before with { Result = "victory", EnemyHp = 0, DamageToBoss = 140, StartingStats = stats with { Attack = 11 } });
            await Settle(); ((Button)_tabs.FindChild("BossReviewToggle", true, false)).ButtonPressed = true;
            var comparison = (Label)_tabs.FindChild("BossReviewComparison", true, false);
            scroll.EnsureControlVisible(comparison); await Settle();
            Check(comparison.IsVisibleInTree() && comparison.Size.X <= scroll.Size.X && comparison.Text.Contains("攻击10 → 11"), dimensions + ": retry comparison retains real identities and wraps in its own label");
            if (dimensions.X == 800) await Capture("small_retry_comparison");
        }
        finally
        {
            ((Button)_tabs.FindChild("DungeonHuntToggle", true, false)).ButtonPressed = false;
            ((Button)_tabs.FindChild("BossReviewToggle", true, false)).ButtonPressed = false;
            action.FromDictionary(savedAction); loader.FromRuntimeDictionary(savedRegions); goal.FromDictionary(savedGoal);
            await Select("CultivationTab"); ((ScrollContainer)PageRoot("CultivationScroll")).ScrollVertical = 0; await Settle();
        }
    }
}
