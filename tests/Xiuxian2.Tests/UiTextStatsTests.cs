using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class UiTextStatsTests
{
    [Fact]
    public void StatsOverview_FormatsExpandedMetrics()
    {
        string text = UiText.StatsOverview(
            keyCount: 100,
            clickCount: 50,
            scrollSteps: 12,
            moveDistance: 3456,
            activeSeconds: 5400,
            realmLevel: 3,
            currentRealmDays: 1.5,
            battleCount: 20,
            winRate: 0.75,
            totalLingqi: 888.0,
            totalInsight: 66.0,
            totalSpiritStones: 40,
            totalSmallCycles: 7,
            totalGrandCycles: 1,
            totalRestCount: 7,
            totalMeditationInsights: 2);

        Assert.Contains("累计在线时长", text);
        Assert.Contains("历史最高境界：炼气 3 层", text);
        Assert.Contains("战斗胜率：", text);
        Assert.Contains("累计获得灵石：40", text);
        Assert.Contains("输入统计", text);
        Assert.Contains("成长统计", text);
        Assert.Contains("战斗统计", text);
        Assert.Contains("资源统计", text);
        Assert.Contains("周天统计", text);
        Assert.Contains("小周天完成数：7", text);
        Assert.Contains("大周天完成数：1", text);
        Assert.Contains("调息次数：7", text);
        Assert.Contains("入定领悟次数：2", text);
    }

    [Fact]
    public void MasteryTextHelpers_FormatCurrentAndNextUnlockState()
    {
        string line = UiText.MasteryStatusLine(
            PlayerActionState.ModeAlchemy,
            2,
            UiText.MasteryEffectDescription("alchemy_unlock_juling_san", 1.0),
            "Lv3（45悟性，炼气2层）");

        Assert.Contains("炼丹 Lv2", line);
        Assert.Contains("解锁聚灵散", line);
        Assert.Contains("45悟性", line);
    }
}
