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
            totalPetAffinity: 22.0,
            totalSpiritStones: 40);

        Assert.Contains("累计活跃时间: 1小时30分", text);
        Assert.Contains("当前境界: 炼气3层", text);
        Assert.Contains("战斗胜率:", text);
        Assert.Contains("累计获得灵石: 40", text);
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
