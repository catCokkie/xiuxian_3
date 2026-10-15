using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

[Collection("ActivityRegistry")]
public sealed class PreparationAndProjectTests
{
    [Fact]
    public void SkippingPreparationHasNoDeficitOrCraftingBudget()
    {
        var result = ChallengePreparationRules.Build("recipe_huiqi_dan", 0, 0, 1, 1, 0, new Dictionary<string, int>())!;
        Assert.Equal(0, result.Target); Assert.Equal(0, result.Missing); Assert.Equal(0, result.Rounds); Assert.Equal(0, result.LingqiCost);
    }
    [Fact]
    public void ArchivedProjects_RetainIndependentProgressAndPlans()
    {
        var projects = new CraftingProjectRecords(); var plan = new CraftingProductionPlan();
        plan.Configure(false, 5); plan.CommitBatch(); plan.Stop(); projects.Capture("A", 75, 200, plan);
        plan.Configure(true); plan.CommitBatch(); projects.Capture("B", 40, 220, plan);
        var restored = new CraftingProjectRecords(); restored.Restore(projects.ToPlainDictionary());
        Assert.True(restored.TryRestore("A", plan, out float progress, out float required, out _));
        Assert.Equal(75, progress); Assert.Equal(200, required); Assert.Equal(5, plan.RoundLimit); Assert.Equal(1, plan.CompletedRounds); Assert.True(plan.Paused);
        Assert.True(restored.TryRestore("B", plan, out progress, out required, out _));
        Assert.Equal(40, progress); Assert.Equal(220, required); Assert.True(plan.Continuous); Assert.Equal(1, plan.CompletedRounds);
        Assert.True(plan.Paused);
    }
    [Fact]
    public void InvalidArchivedNumbersFallBackWithoutReactivatingProduction()
    {
        var records = new CraftingProjectRecords();
        records.Restore(new Dictionary<string, object> { ["A"] = new Dictionary<string, object> { ["progress"] = "bad", ["required_progress"] = double.NaN, ["enhance_level"] = "bad" } });
        var plan = new CraftingProductionPlan();
        Assert.True(records.TryRestore("A", plan, out float progress, out float required, out int level));
        Assert.Equal(0, progress); Assert.Equal(100, required); Assert.Equal(-1, level); Assert.True(plan.Paused);
    }
    [Fact]
    public void CompletedArchive_DoesNotReviveAndUnknownTargetDoesNotBorrowProgress()
    {
        var plan = new CraftingProductionPlan(); plan.CommitBatch(); var records = new CraftingProjectRecords();
        records.Capture("A", 0, 200, plan, 2);
        Assert.True(records.TryRestore("A", plan, out _, out _, out int level));
        Assert.Equal(2, level); Assert.True(plan.Finished); Assert.False(plan.CanProduce);
        Assert.False(records.TryRestore("B", plan, out float progress, out _, out _));
        Assert.Equal(0, progress);
    }
    [Fact]
    public void BaselineEstimateAccountsForHealingExceptAfterTheLethalRound()
    {
        var boss = new MonsterStatProfile("boss", "boss", new CharacterStatBlock(100, 10, 5, 100, 0, 1.5), 24, HealPerRound: 2);
        Assert.Equal(8, ChallengePreparationRules.EstimateBaselineRounds(20, boss));
        Assert.Equal(7, ChallengePreparationRules.EstimateBaselineRounds(20, boss with { HealPerRound = 0 }));
        Assert.Null(ChallengePreparationRules.EstimateBaselineRounds(6, boss));
        Assert.Equal(1, ChallengePreparationRules.EstimateBaselineRounds(105, boss with { HealPerRound = 1000 }));
    }
    [Fact]
    public void Preparation_DefaultOneChallengeAccountsForBatchYieldAndMaterialDeficit()
    {
        var result = ChallengePreparationRules.Build("recipe_huiqi_dan", 1, 0, 1, 1, 0, new Dictionary<string, int>())!;
        Assert.Equal(2, result.PerBatch); Assert.Equal(1, result.Rounds); Assert.Equal(1, result.Missing);
        Assert.Equal(50, result.LingqiCost); Assert.Equal(2, result.Materials.Single().Missing);
    }
    [Fact]
    public void Preparation_UsesActualMasteryDoubleYieldAndExtraOutput()
    {
        var result = ChallengePreparationRules.Build("recipe_huiqi_dan", 9, 1, 3, 2, 1, new Dictionary<string, int> { ["spirit_herb"] = 3 })!;
        Assert.Equal(7, result.PerBatch); Assert.Equal(2, result.Rounds);
        Assert.Equal(4, result.Materials.Single().Required); Assert.Equal(1, result.Materials.Single().Missing);
        Assert.Equal(100, result.LingqiCost);
    }
    [Fact]
    public void Preparation_ExistingInventoryNeverRequestsUnnecessaryProduction()
    {
        var result = ChallengePreparationRules.Build("recipe_huiqi_dan", 1, 2, 1, 1, 0, new Dictionary<string, int>())!;
        Assert.Equal(0, result.Rounds); Assert.Equal(0, result.LingqiCost); Assert.All(result.Materials, x => Assert.Equal(0, x.Required));
    }
    [Fact]
    public void Preparation_MasteryLockedOrUnimplementedRecipesAreExcluded()
    {
        Assert.Null(ChallengePreparationRules.Build("talisman_shield_charm", 1, 0, 1, 1, 0, new Dictionary<string, int>()));
        Assert.NotNull(ChallengePreparationRules.Build("talisman_zhenling", 1, 0, 4, 1, 0, new Dictionary<string, int>()));
        Assert.Equal("talisman_fire_charm", ChallengePreparationRules.ChooseTalisman("armor", 1));
        Assert.Equal("", ChallengePreparationRules.ChooseTalisman("fast", 1));
        Assert.Equal("talisman_shield_charm", ChallengePreparationRules.ChooseTalisman("fast", 2));
    }
    [Theory]
    [InlineData("alchemy")]
    [InlineData("smithing")]
    [InlineData("talisman")]
    [InlineData("cooking")]
    [InlineData("formation")]
    public void Migration_CurrentProjectAndProductionRemainAuthoritative(string system)
    {
        var store = new SaveMigrationRules.MigrationStore();
        var production = new Dictionary<string, object> { ["round_limit"] = 3, ["completed_rounds"] = 1, ["paused"] = true };
        store.SetValue(system, "state", new Dictionary<string, object>
        {
            [system == "smithing" ? "target_equipment_id" : "selected_recipe"] = "A",
            ["progress"] = 75.0, ["required_progress"] = 200.0, ["production"] = production,
            ["projects"] = new Dictionary<string, object> { ["B"] = new Dictionary<string, object> { ["progress"] = 40.0 } },
        });
        SaveMigrationRules.MigrateToLatest(store, 17);
        var state = (Dictionary<string, object>)store.GetValue(system, "state", null!);
        var projects = (Dictionary<string, object>)state["projects"];
        Assert.Equal(75.0, state["progress"]); Assert.Same(production, state["production"]);
        Assert.Equal(75.0, ((Dictionary<string, object>)projects["A"])["progress"]);
        Assert.Equal(40.0, ((Dictionary<string, object>)projects["B"])["progress"]);
        if (system == "smithing") Assert.Equal(2, state["enhance_level"]);
    }
    [Theory]
    [InlineData(8, false, true)]
    [InlineData(9, false, false)]
    [InlineData(9, true, true)]
    [InlineData(10, true, false)]
    [InlineData(12, true, false)]
    public void ChapterBoundary_BlocksFurtherBreakthroughWithoutChangingLegacyLevel(int level, bool cleared, bool allowed)
    {
        Assert.Equal(allowed, RealmBreakthroughRules.CanCrossRealm(level, cleared ? new[] { "lv_qi_005" } : Array.Empty<string>()));
        Assert.Equal(level >= 10, RealmBreakthroughRules.IsAtContentLimit(level));
    }
    [Fact]
    public void ChapterCompletionRequiresFinalRegionEvenForHighLegacyRealms()
    {
        Assert.False(RealmBreakthroughRules.IsChapterComplete(12, Array.Empty<string>()));
        Assert.True(RealmBreakthroughRules.IsChapterComplete(12, new[] { "lv_qi_005" }));
    }
}
