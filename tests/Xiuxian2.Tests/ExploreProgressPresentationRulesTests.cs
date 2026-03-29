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
    }

    [Fact]
    public void BuildRecentBattleLogText_FormatsEntries()
    {
        string text = ExploreProgressPresentationRules.BuildRecentBattleLogText(
            new[] { ("12:34", "幽泉洞窟", "苔痕史莱姆", "胜利", "灵气+12") });

        Assert.Contains("最近战斗日志", text);
        Assert.Contains("[12:34] 幽泉洞窟 | 苔痕史莱姆", text);
        Assert.Contains("结果：胜利", text);
        Assert.Contains("收益：灵气+12", text);
    }
}
