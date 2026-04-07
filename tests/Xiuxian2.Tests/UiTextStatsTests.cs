using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class UiTextStatsTests
{
    [Fact]
    public void StatsOverview_FormatsExpandedMetrics()
    {
        string text = UiText.StatsOverview(new UiText.StatsOverviewData(
            TotalInputs: 180,
            KeyCount: 100,
            ClickCount: 50,
            ScrollSteps: 12,
            JoypadButtonCount: 10,
            JoypadAxisCount: 8,
            MoveDistance: 3456,
            ActiveSeconds: 5400,
            RealmLevel: 3,
            RealmExp: 456.0,
            CurrentRealmDays: 1.5,
            CurrentActionName: "炼丹",
            MasterySummary: "副本 Lv2｜修炼 Lv2｜炼丹 Lv3",
            CurrentLingqi: 120.0,
            CurrentInsight: 18.0,
            CurrentSpiritStones: 23,
            TotalLingqi: 888.0,
            TotalInsight: 66.0,
            TotalSpiritStones: 40,
            TotalSpentLingqi: 120.0,
            TotalSpentInsight: 12.0,
            TotalSpentSpiritStones: 9,
            SpentSpiritStonesOnShop: 6,
            SpentSpiritStonesOnSeeds: 2,
            SpentSpiritStonesOnOther: 1,
            TotalSmallCycles: 7,
            TotalGrandCycles: 1,
            TotalRestCount: 7,
            TotalMeditationInsights: 2,
            BattleCount: 20,
            BattleWins: 15,
            BattleLosses: 5,
            WinRate: 0.75,
            TotalBossBattles: 3,
            TotalEliteBattles: 6,
            TotalMonsterKills: 15,
            HighestWinStreak: 8,
            TotalAlchemyCrafts: 12,
            TotalSmithingCrafts: 4,
            TotalTalismanCrafts: 5,
            TotalCookingCrafts: 2,
            TotalFormationCrafts: 1,
            TotalMiningCompletions: 9,
            TotalFishingCompletions: 7,
            TotalPotionsConsumedInBattle: 4,
            TotalTalismansConsumedInBattle: 3,
            TemperCount: 3,
            BoneforgeCount: 2,
            BloodflowCount: 1,
            UnlockedPlots: 4,
            ActivePlots: 3,
            ReadyPlots: 1,
            IdlePlots: 1,
            TotalGardenPlants: 11,
            TotalGardenHarvests: 9,
            TotalGardenAutoHarvests: 4,
            SelectedPlotSummary: "1号田：灵花已成熟，可立即收获"));

        Assert.Contains("累计在线时长", text);
        Assert.Contains("当前境界：炼气 3 层", text);
        Assert.Contains("战斗胜率：", text);
        Assert.Contains("累计获得灵石：40", text);
        Assert.Contains("累计消耗灵气：120", text);
        Assert.Contains("└─坊市 6｜种子 2｜其他 1", text);
        Assert.Contains("【总览】", text);
        Assert.Contains("【资源】", text);
        Assert.Contains("【战斗】", text);
        Assert.Contains("【制作与采集】", text);
        Assert.Contains("【灵田】", text);
        Assert.Contains("【输入明细】", text);
        Assert.Contains("小周天完成数：7", text);
        Assert.Contains("大周天完成数：1", text);
        Assert.Contains("调息次数：7", text);
        Assert.Contains("入定领悟次数：2", text);
        Assert.Contains("累计击杀怪物：15", text);
        Assert.Contains("最高连胜：8 场", text);
        Assert.Contains("累计消耗丹药：4", text);
        Assert.Contains("累计消耗符咒：3", text);
        Assert.Contains("炼丹完成次数：12", text);
        Assert.Contains("累计收获次数：9", text);
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
