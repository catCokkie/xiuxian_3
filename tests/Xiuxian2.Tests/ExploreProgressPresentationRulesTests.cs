using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ExploreProgressPresentationRulesTests
{
    [Fact]
    public void ActionModeTexts_MapAllFourModes()
    {
        Assert.Equal(UiText.ActionModeDungeon, ExploreProgressPresentationRules.GetActionModeOptionText(0));
        Assert.Equal(UiText.ActionModeAlchemy, ExploreProgressPresentationRules.GetActionModeOptionText(2));
        Assert.Equal("主行为：炼器", ExploreProgressPresentationRules.GetPausedModeLabel(PlayerActionState.ActionSmithing));
    }

    [Fact]
    public void ProgressTexts_FormatAlchemyAndSmithingStatus()
    {
        Assert.Equal("炼制：回气丹 50%", ExploreProgressPresentationRules.BuildAlchemyProgressText("回气丹", 100, 200));
        Assert.Equal("强化：青锋剑 +2->3 25%", ExploreProgressPresentationRules.BuildSmithingProgressText("青锋剑", 2, 50, 200));
    }

    [Fact]
    public void BuildRecentBattleLogText_FormatsEntries()
    {
        string text = ExploreProgressPresentationRules.BuildRecentBattleLogText(
            new[] { ("12:34", "幽泉洞窟", "苔痕史莱姆", "胜利", "灵气+12") });

        Assert.Contains("最近战斗日志", text);
        Assert.Contains("[12:34] 幽泉洞窟 | 苔痕史莱姆 | 胜利", text);
    }
}
