using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class SharedGrowthClockTests
{
    private static SharedGrowthClock Seed(double phase = 0)
    {
        var clock = new SharedGrowthClock(); clock.Restore(new Dictionary<string, object> { ["last_settled_unix"] = 1000.0, ["cycle_elapsed_seconds"] = phase }); return clock;
    }
    [Theory]
    [InlineData(0, 3600, 600)]
    [InlineData(1800, 3600, 480)]
    [InlineData(14400, 3600, 360)]
    [InlineData(28800, 3600, 180)]
    [InlineData(86040, 720, 90)]
    public void SharedBudgetUsesTheExistingCurveAcrossCycleBoundaries(double phase, double seconds, double expected)
        => Assert.Equal(expected, SharedGrowthClock.BudgetBetween(phase, seconds), 6);
    [Theory]
    [InlineData(0)]
    [InlineData(1800)]
    [InlineData(20000)]
    [InlineData(86000)]
    public void AnyFullDayHasTheSameFiniteBudgetAndLongAbsencesHaveNoAdditionalClaim(double phase)
    {
        var day = Seed(phase); var week = Seed(phase);
        Assert.Equal(6360, day.Claim(1000 + 86400), 6);
        Assert.Equal(6360, week.Claim(1000 + 7 * 86400), 6);
        Assert.Equal(phase, week.CycleElapsedSeconds, 6);
        Assert.Equal(0, week.Claim(1000 + 7 * 86400));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1700)]
    [InlineData(14300)]
    [InlineData(86300)]
    public void SplittingOnlineOfflineAndSaveLoadsCannotRefreshHighRate(double phase)
    {
        var single = Seed(phase); double expected = single.Claim(1000 + 86400);
        var fragmented = Seed(phase); double actual = 0;
        for (int minute = 1; minute <= 1440; minute++)
        {
            actual += fragmented.Claim(1000 + minute * 60);
            var saved = fragmented.ToPlainDictionary(); fragmented = new(); fragmented.Restore(saved);
        }
        Assert.Equal(expected, actual, 6); Assert.Equal(single.CycleElapsedSeconds, fragmented.CycleElapsedSeconds, 6);
    }
    [Fact]
    public void FreshClockHasNoHistoricalGiftAndLegacyClockStartsAtSavedTime()
    {
        var fresh = new SharedGrowthClock(); Assert.Equal(0, fresh.Claim(1000000000)); Assert.Equal(12, fresh.Claim(1000000060), 6);
        var legacy = new SharedGrowthClock(); Assert.Equal(600, legacy.Claim(4600, legacySavedUnix: 1000), 6);
    }
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1)]
    [InlineData(500)]
    [InlineData(1000)]
    public void InvalidRepeatedOrRewoundClockDoesNotConsumeOrDuplicateTheBudget(double now)
    {
        var clock = Seed(20000); Assert.Equal(0, clock.Claim(now));
        Assert.Equal(1000, clock.LastSettledUnix); Assert.Equal(20000, clock.CycleElapsedSeconds);
        Assert.Equal(6, clock.Claim(1060), 6);
    }
    [Fact]
    public void RateRecoversAtTheSharedCycleBoundaryInsteadOfAtEveryStartup()
    {
        var clock = Seed(86340); Assert.Equal(3, clock.InputsPerMinute);
        clock.Claim(1060); Assert.Equal(12, clock.InputsPerMinute);
        clock.Claim(2860); Assert.Equal(8, clock.InputsPerMinute);
        var restored = new SharedGrowthClock(); restored.Restore(clock.ToPlainDictionary()); Assert.Equal(8, restored.InputsPerMinute);
    }
    [Fact]
    public void ProgressionMigrationAnchorsOnlyMissingFieldsWithoutGrantingResources()
    {
        var store = new SaveMigrationRules.MigrationStore(); store.SetValue("meta", "version", 22); store.SetValue("meta", "last_saved_unix", 5000000000L);
        store.SetValue("resource", "wallet", new Dictionary<string, object> { ["lingqi"] = 123.5 });
        store.SetValue("progress", "player", new Dictionary<string, object> { ["realm_level"] = 4, ["realm_exp"] = 42.5 });
        SaveMigrationRules.MigrateToLatest(store, 22);
        var growth = (Dictionary<string, object>)store.GetValue("growth", "baseline", new Dictionary<string, object>());
        Assert.Equal(5000000000.0, growth["last_settled_unix"]); Assert.Equal(0.0, growth["cycle_elapsed_seconds"]);
        Assert.Equal(123.5, ((Dictionary<string, object>)store.GetValue("resource", "wallet", new Dictionary<string, object>()))["lingqi"]);
        Assert.Equal(42.5, ((Dictionary<string, object>)store.GetValue("progress", "player", new Dictionary<string, object>()))["realm_exp"]);
        growth["cycle_elapsed_seconds"] = 18000.0; growth["last_settled_unix"] = 5000000001.0; store.SetValue("growth", "baseline", growth);
        SaveMigrationRules.MigrateToLatest(store, 22);
        growth = (Dictionary<string, object>)store.GetValue("growth", "baseline", new Dictionary<string, object>());
        Assert.Equal(18000.0, growth["cycle_elapsed_seconds"]); Assert.Equal(5000000001.0, growth["last_settled_unix"]);
    }
}
