using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class CraftingProductionPlanTests
{
    [Fact]
    public void FinitePlan_BoundsOverflowAndStopsAfterCommittedRounds()
    {
        var plan = new CraftingProductionPlan();
        plan.Configure(false, 3);
        Assert.Equal(600f, plan.BoundProgress(10000f, 200f));
        for (int i = 0; i < 3; i++) plan.CommitBatch();
        plan.CommitBatch();
        Assert.Equal(3, plan.CompletedRounds);
        Assert.True(plan.Finished);
        Assert.False(plan.CanProduce);
    }
    [Fact]
    public void StopAndResume_PreserveRemainingRoundsAndDoNotCountAttempts()
    {
        var plan = new CraftingProductionPlan();
        plan.Configure(false, 5); plan.CommitBatch(); plan.Stop(); plan.CommitBatch();
        Assert.Equal(1, plan.CompletedRounds);
        plan.Start(false, 5);
        Assert.Equal(1, plan.CompletedRounds);
        Assert.True(plan.CanProduce);
        plan.Start(false, 3);
        Assert.Equal(0, plan.CompletedRounds);
        Assert.Equal(3, plan.RoundLimit);
    }
    [Fact]
    public void ContinuousPlan_PreservesOverflowAndHasNoCompletionLimit()
    {
        var plan = new CraftingProductionPlan(); plan.Configure(true);
        for (int i = 0; i < 10001; i++) plan.CommitBatch();
        Assert.Equal(10001, plan.CompletedRounds);
        Assert.Equal(50000f, plan.BoundProgress(50000f, 200f));
        Assert.True(plan.CanProduce);
        Assert.False(plan.Finished);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveRoundtrip_PreservesModeCounterAndStoppedState(bool continuous)
    {
        var plan = new CraftingProductionPlan(); plan.Configure(continuous, 4); plan.CommitBatch(); plan.Stop();
        var restored = new CraftingProductionPlan(); restored.Restore(plan.ToPlainDictionary());
        Assert.Equal(plan.RoundLimit, restored.RoundLimit);
        Assert.Equal(1, restored.CompletedRounds);
        Assert.False(restored.CanProduce);
        restored.Start(continuous, 4);
        Assert.Equal(1, restored.CompletedRounds);
    }
    [Fact]
    public void FinishedSave_RemainsFinishedUntilExplicitRestart()
    {
        var plan = new CraftingProductionPlan(); plan.CommitBatch();
        var restored = new CraftingProductionPlan(); restored.Restore(plan.ToPlainDictionary());
        Assert.False(restored.CanProduce);
        restored.Start(false, 1);
        Assert.True(restored.CanProduce);
        Assert.Equal(0, restored.CompletedRounds);
    }
    [Fact]
    public void InvalidSavedCounts_AreBoundedAndMissingSettingsDefaultToOneRound()
    {
        var plan = new CraftingProductionPlan(); plan.Restore(new Dictionary<string, object> { ["round_limit"] = -4, ["completed_rounds"] = -10 });
        Assert.Equal(1, plan.RoundLimit); Assert.Equal(0, plan.CompletedRounds);
        plan.Restore(new Dictionary<string, object> { ["round_limit"] = "bad", ["completed_rounds"] = int.MaxValue });
        Assert.Equal(1, plan.RoundLimit); Assert.Equal(1, plan.CompletedRounds);
        plan.Restore(new Dictionary<string, object>());
        Assert.Equal(1, plan.RoundLimit); Assert.True(plan.CanProduce);
    }
    [Fact]
    public void V16Migration_PreservesSelectedContinuousBehaviorAndNewDefaults()
    {
        var store = new SaveMigrationRules.MigrationStore();
        store.SetValue("alchemy", "state", new Dictionary<string, object> { ["selected_recipe"] = "recipe_huiqi_dan", ["progress"] = 75.0 });
        store.SetValue("formation", "state", new Dictionary<string, object> { ["selected_recipe"] = "formation_guard_flag", ["production"] = new Dictionary<string, object> { ["round_limit"] = 5, ["completed_rounds"] = 2, ["paused"] = true } });
        SaveMigrationRules.MigrateToLatest(store, 16);
        var alchemy = (Dictionary<string, object>)store.GetValue("alchemy", "state", null!);
        var smithing = (Dictionary<string, object>)store.GetValue("smithing", "state", null!);
        var formation = (Dictionary<string, object>)store.GetValue("formation", "state", null!);
        Assert.Equal(75.0, alchemy["progress"]);
        Assert.Equal(0, ((Dictionary<string, object>)alchemy["production"])["round_limit"]);
        Assert.Equal(1, ((Dictionary<string, object>)smithing["production"])["round_limit"]);
        Assert.True((bool)((Dictionary<string, object>)formation["production"])["paused"]);
        Assert.Equal(17, store.GetValue("meta", "version", 0));
    }
}
