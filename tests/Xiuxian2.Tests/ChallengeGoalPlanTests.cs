using Xiuxian.Scripts.Services;
namespace Xiuxian.Tests;

public class ChallengeGoalPlanTests
{
    [Fact]
    public void ExplicitGoalTracksOneBossAndRetainsItsSettingsUntilReplaced()
    {
        var goal = new ChallengeGoalPlan();
        Assert.True(goal.Start("lv_qi_005", "boss5", new Dictionary<string, int> { ["recipe_huiqi_dan"] = 2, ["talisman_fire_charm"] = 3 }));
        Assert.False(goal.Start("lv_qi_005", "boss5", new Dictionary<string, int>()));
        Assert.Equal(2, goal.GetTarget("recipe_huiqi_dan")); Assert.False(goal.Completed);
        Assert.True(goal.Start("lv_qi_004", "boss4", new Dictionary<string, int> { ["recipe_huiqi_dan"] = 1 }));
        Assert.Equal("boss4", goal.BossId); Assert.Equal(0, goal.GetTarget("talisman_fire_charm"));
    }
    [Fact]
    public void OnlyANewMatchingVictoryCompletesAChallengeIncludingRepeatGoals()
    {
        var goal = new ChallengeGoalPlan(); goal.Start("zone", "boss", new Dictionary<string, int>());
        Assert.False(goal.CompleteVictory("other", "boss")); Assert.False(goal.CompleteVictory("zone", "normal"));
        Assert.True(goal.CompleteVictory("zone", "boss")); Assert.False(goal.CompleteVictory("zone", "boss"));
        var loaded = new ChallengeGoalPlan(); loaded.Restore(goal.ToPlainDictionary()); Assert.True(loaded.Completed);
        Assert.True(loaded.Start("zone", "boss", new Dictionary<string, int>())); Assert.False(loaded.Completed);
    }
    [Fact]
    public void SkippedQuantitiesAndPendingIdentitySurviveRoundtripAndCancelClearsOnlyTracking()
    {
        var goal = new ChallengeGoalPlan(); goal.Start("zone", "boss", new Dictionary<string, int> { ["recipe_huiqi_dan"] = 3 });
        Assert.True(goal.SetTarget("recipe_huiqi_dan", 7)); Assert.True(goal.SkipPreparation()); Assert.False(goal.SetTarget("unknown", 5));
        var loaded = new ChallengeGoalPlan(); loaded.Restore(goal.ToPlainDictionary());
        Assert.True(loaded.HasGoal); Assert.Equal("zone", loaded.LevelId); Assert.Equal(0, loaded.GetTarget("recipe_huiqi_dan"));
        Assert.True(loaded.Cancel()); Assert.False(loaded.HasGoal); Assert.False(loaded.Cancel());
    }
    [Fact]
    public void CorruptOrMissingFieldsHaveConservativeDefaults()
    {
        var goal = new ChallengeGoalPlan(); goal.Restore(new Dictionary<string, object>()); Assert.False(goal.HasGoal);
        goal.Restore(new Dictionary<string, object> { ["level_id"] = "zone", ["boss_id"] = "boss", ["completed"] = "true",
            ["stock_targets"] = new Dictionary<string, object> { ["recipe_huiqi_dan"] = 1000, ["talisman_fire_charm"] = "bad", ["talisman_shield_charm"] = -5, ["unknown"] = 50 } });
        Assert.True(goal.HasGoal); Assert.False(goal.Completed); Assert.Equal(99, goal.GetTarget("recipe_huiqi_dan")); Assert.Equal(0, goal.GetTarget("talisman_fire_charm")); Assert.Equal(0, goal.GetTarget("talisman_shield_charm"));
        goal.Restore(new Dictionary<string, object> { ["level_id"] = "zone" }); Assert.False(goal.HasGoal);
    }
    [Fact]
    public void MigrationAddsEmptyGoalWithoutInventingACompletedTargetAndPreservesExistingGoal()
    {
        var store = new SaveMigrationRules.MigrationStore(); var items = new Dictionary<string, object> { ["spirit_herb"] = 8 };
        store.SetValue("backpack", "items", items); store.SetValue("progress", "player", new Dictionary<string, object> { ["realm_level"] = 10 });
        SaveMigrationRules.MigrateToLatest(store, 19);
        var goal = new ChallengeGoalPlan(); goal.Restore((Dictionary<string, object>)store.GetValue("challenge_goal", "state", null!));
        Assert.False(goal.HasGoal); Assert.Equal(8, items["spirit_herb"]); Assert.Equal(20, store.GetValue("meta", "version", 0));
        goal.Start("zone", "boss", new Dictionary<string, int> { ["recipe_huiqi_dan"] = 2 }); var saved = goal.ToPlainDictionary(); store.SetValue("challenge_goal", "state", saved);
        SaveMigrationRules.MigrateToLatest(store, 19); Assert.Same(saved, store.GetValue("challenge_goal", "state", null!));
    }
}
