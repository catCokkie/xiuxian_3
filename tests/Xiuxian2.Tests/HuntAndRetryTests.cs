using Xiuxian.Scripts.Services;

namespace Xiuxian2.Tests;

public class HuntAndRetryTests
{
    [Fact]
    public void TargetingChangesDistributionWithoutDoublingEveryDrop()
    {
        int[] weights = { 60, 30, 10 };
        var targeted = weights.Select((w, i) => DungeonHuntRules.Weight(w, i == 0)).ToArray();
        Assert.Equal(.75, targeted[0] / (double)targeted.Sum(), 6);
        Assert.Equal(3, targeted[1] / (double)targeted[2]);
        Assert.Equal(.8, DungeonHuntRules.ExploreRate);
        Assert.Equal(0, DungeonHuntRules.Weight(-10, true));
    }
    [Theory]
    [InlineData(2, true, false)]
    [InlineData(3, false, false)]
    [InlineData(3, true, true)]
    public void HuntRequiresBothRealmAndFirstClear(int realm, bool clear, bool available)
        => Assert.Equal(available, DungeonHuntRules.IsUnlocked(realm, clear));

    private static BossChallengeReport Report(string result = "timeout", bool complete = true) =>
        new("region", "boss", result, 20, 30, 100, 100, 30, 20, new Dictionary<string, int>(),
            new Dictionary<string, int>(), complete, 100);
    [Fact]
    public void AttemptReloadPreservesItsOriginalStartingConditions()
    {
        var attempt = new BossChallengeAttempt();
        attempt.Begin("region", "boss", stats: new CharacterStatBlock(100, 20, 8, 12, .04, 1.5), hp: 80, limit: 20);
        attempt.Observe(new(8, 2, 0)); var saved = attempt.ToPlainDictionary();
        attempt.Restore(saved, "region", "boss"); var report = attempt.Finish("timeout", 20, 30, 100, 100);
        Assert.Equal(20, report.StartingStats!.Value.Attack); Assert.Equal(80, report.StartingHp);
        Assert.Equal(20, report.RoundLimit); Assert.Equal(8, report.DamageToBoss);
        var restored = BossChallengeReport.Restore(report.ToPlainDictionary())!;
        Assert.Equal(report, restored with { Potions = report.Potions, Talismans = report.Talismans });
    }
    [Fact]
    public void ComparisonShowsStartingConditionsAndActualOutcomeWithoutClaimingCausality()
    {
        var before = Report() with { StartingStats = new(100, 20, 8, 12, 0, 1.5), StartingHp = 80, RoundLimit = 20 };
        var after = before with { Result = "victory", Rounds = 18, EnemyHp = 0, StartingStats = new(100, 22, 8, 12, 0, 1.5), StartingHp = 100 };
        string text = BossChallengeReportRules.Comparison(after, before);
        Assert.Contains("攻击20 → 22", text); Assert.Contains("80/100 → 100/100", text);
        Assert.Contains("超时败退", text); Assert.Contains("胜利", text); Assert.Contains("独立收益或胜率", text);
    }
    [Fact]
    public void OtherBossAndMissingPreviousNeverProduceAComparison()
    {
        Assert.Equal("", BossChallengeReportRules.Comparison(Report(), null));
        Assert.Equal("", BossChallengeReportRules.Comparison(Report(), Report() with { BossId = "other" }));
        Assert.Equal("", BossChallengeReportRules.Comparison(Report(), Report() with { LevelId = "other" }));
    }
    [Fact]
    public void LegacyOrPartialReportDoesNotInventAnAttributeOrConsumptionDelta()
    {
        string text = BossChallengeReportRules.Comparison(Report(), Report(complete: false));
        Assert.Contains("缺少出战快照", text); Assert.Contains("不比较整场", text);
        Assert.DoesNotContain("出战基础属性", text); Assert.DoesNotContain("丹药使用", text);
    }
    [Fact]
    public void MigrationAddsEmptyDefaultsWithoutBackfillingAttemptsOrChangingExistingProgress()
    {
        var save = new SaveMigrationRules.MigrationStore();
        var action = new Dictionary<string, object> { ["action_id"] = "mining" };
        var goal = new Dictionary<string, object> { ["last_boss_report"] = Report().ToPlainDictionary() };
        save.SetValue("action", "mode", action); save.SetValue("challenge_goal", "state", goal);
        save.SetValue("dungeon", "runtime", new Dictionary<string, object> { ["player_hp"] = 7 });
        SaveMigrationRules.MigrateToLatest(save, 24);
        Assert.Equal(SaveMigrationRules.LatestVersion, save.GetValue("meta", "version", 0)); Assert.Equal("mining", action["action_id"]);
        Assert.Empty((Dictionary<string, object>)action["dungeon_hunts"]);
        Assert.Empty((Dictionary<string, object>)goal["previous_boss_report"]);
        Assert.Equal(7, ((Dictionary<string, object>)save.GetValue("dungeon", "runtime", null!))["player_hp"]);
        var hunts = action["dungeon_hunts"]; SaveMigrationRules.MigrateToLatest(save, 24);
        Assert.Same(hunts, action["dungeon_hunts"]); Assert.Equal(Report().BossId, ((Dictionary<string, object>)goal["last_boss_report"])["boss_id"]);
    }
}
