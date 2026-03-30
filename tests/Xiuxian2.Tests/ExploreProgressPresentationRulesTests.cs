using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ExploreProgressPresentationRulesTests
{
    [Fact]
    public void ActionModeTexts_MapExpandedModeSet()
    {
        Assert.Equal("副本｜材料装备", ExploreProgressPresentationRules.GetActionModeOptionText(0));
        Assert.Equal("灵田｜草花果", ExploreProgressPresentationRules.GetActionModeOptionText(4));
        Assert.Equal("灵渔｜鱼珠涎", ExploreProgressPresentationRules.GetActionModeOptionText(6));
        Assert.Equal("体修｜永久属性", ExploreProgressPresentationRules.GetActionModeOptionText(11));
        Assert.Equal("炼丹｜战斗丹药", ExploreProgressPresentationRules.GetActionModeOptionText(2));
        Assert.Equal("主行为：炼器", ExploreProgressPresentationRules.GetPausedModeLabel(PlayerActionState.ActionSmithing));
        Assert.Equal("主行为：灵田", ExploreProgressPresentationRules.GetPausedModeLabel(PlayerActionState.ActionGarden));
    }

    [Fact]
    public void ProgressTexts_FormatCraftingAndGatheringStatus()
    {
        Assert.Equal("炼制：回气丹 50%", ExploreProgressPresentationRules.BuildAlchemyProgressText("回气丹", 100, 200));
        Assert.Equal("强化：青锋剑 +2->3 25%", ExploreProgressPresentationRules.BuildSmithingProgressText("青锋剑", 2, 50, 200));
        Assert.Equal("种植：灵花 50%", ExploreProgressPresentationRules.BuildGardenProgressText("灵花", 120, 240));
        Assert.Equal("开采：灵玉 50% | 耐久 73", ExploreProgressPresentationRules.BuildMiningProgressText("灵玉", 110, 220, 73));
        Assert.Equal("垂钓：灵鱼 50%", ExploreProgressPresentationRules.BuildFishingProgressText("灵鱼", 60, 120));
        Assert.Equal("阵法：护体阵旗（当前生效）｜战斗防御 +5%", ExploreProgressPresentationRules.BuildFormationStatusText("护体阵旗", "战斗防御 +5%", true));
        Assert.Equal("阵法：未激活", ExploreProgressPresentationRules.BuildFormationStatusText(string.Empty, string.Empty, false));
    }

    [Fact]
    public void BuildRecentBattleLogText_FormatsEntries()
    {
        string text = ExploreProgressPresentationRules.BuildRecentBattleLogText(
            new[] { ("12:34", "幽泉洞窟", "苔痕史莱姆", "normal", 3, "胜利", "灵气+12") });

        Assert.Contains("最近战斗日志", text);
        Assert.Contains("遭遇 苔痕史莱姆", text);
        Assert.Contains("战斗胜利", text);
        Assert.Contains("3 回合", text);
        Assert.Contains("灵气+12", text);
    }

    [Fact]
    public void BuildRecentBattleLogText_EliteBadge()
    {
        string text = ExploreProgressPresentationRules.BuildRecentBattleLogText(
            new[] { ("12:34", "幽泉洞窟", "暗穴蛛", "elite", 5, "胜利", "灵草x2") });

        Assert.Contains("(精英)", text);
    }

    [Fact]
    public void BuildRecentBattleLogText_DefeatRedColor()
    {
        string text = ExploreProgressPresentationRules.BuildRecentBattleLogText(
            new[] { ("12:34", "幽泉洞窟", "阴潮蛇", "normal", 2, "失败", "none") });

        Assert.Contains("战斗失败", text);
        Assert.Contains("#c85050", text);
    }
}
