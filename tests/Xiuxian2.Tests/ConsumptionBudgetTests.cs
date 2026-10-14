using Xiuxian.Scripts.Services;
using Xunit;

namespace Xiuxian2.Tests;

[Collection("ActivityRegistry")]
public class ConsumptionBudgetTests
{
    [Fact]
    public void FiniteBudgetCountsCurrentPortionAndNeverRefillsBeyondRemainingBudget()
    {
        var plan = new MealUsagePlan(); plan.Configure(false, 3, true);
        Assert.Equal(1, plan.ConsumedPortions);
        var tick = MealRules.Advance(60, 4000, plan.AllowedRefills(100), 1800);
        plan.RecordPortions(tick.RefillsConsumed);
        Assert.Equal(0, tick.Remaining); Assert.Equal(3, plan.ConsumedPortions); Assert.Equal(0, plan.AllowedRefills(100));
    }
    [Fact]
    public void SinglePortionAndStopDoNotPermitRenewal()
    {
        var plan = new MealUsagePlan(); plan.Configure(false, 1, true);
        Assert.Equal(0, plan.AllowedRefills(100));
        plan.Configure(true, 1, true); Assert.Equal(100, plan.AllowedRefills(100));
        plan.StopRenewal(); Assert.Equal(0, plan.AllowedRefills(100)); Assert.Equal(1, plan.ConsumedPortions);
    }
    [Fact]
    public void UsageRoundtripPreservesCountersAndStopsAndLegacyActiveDefaultsToContinuous()
    {
        var original = new MealUsagePlan(); original.Configure(false, 7, true); original.RecordPortions(2); original.StopRenewal();
        var restored = new MealUsagePlan(); restored.Restore(original.ToPlainDictionary(), true);
        Assert.Equal(7, restored.PortionLimit); Assert.Equal(3, restored.ConsumedPortions); Assert.True(restored.RenewalStopped);
        restored.Restore(null, true); Assert.True(restored.Continuous); Assert.Equal(1, restored.ConsumedPortions);
        restored.Restore(null, false); Assert.Equal(1, restored.PortionLimit); Assert.Equal(0, restored.ConsumedPortions);
    }
    [Fact]
    public void InvalidUsageCountsAreConservativeAndLongTicksOnlyTakeAvailablePortions()
    {
        var plan = new MealUsagePlan(); plan.Restore(new Dictionary<string, object> { ["portion_limit"] = "invalid", ["consumed_portions"] = long.MaxValue, ["renewal_stopped"] = "invalid" }, true);
        Assert.Equal(1, plan.PortionLimit); Assert.Equal(1, plan.ConsumedPortions); Assert.Equal(0, plan.AllowedRefills(100));
        Assert.Equal(new MealRules.TickResult(0, 3), MealRules.Advance(60, 1e10, 3, 1800));
        Assert.Equal(new MealRules.TickResult(1800, 1), MealRules.Advance(60, 60, 3, 1800));
    }
    [Theory]
    [InlineData(10, 3, 7)] [InlineData(3, 10, 0)] [InlineData(10, 0, 10)] [InlineData(-2, 3, 0)]
    public void ReserveIsAFloorRatherThanAnInventoryMutation(int owned, int reserved, int expected)
        => Assert.Equal(expected, InventoryReserveRules.Spendable(owned, reserved));

    [Fact]
    public void BackpackReserveRoundtripDoesNotTreatMetadataAsInventoryAndPreservesItems()
    {
        var items = new Dictionary<string, int> { ["spirit_herb"] = 20 };
        var snapshot = new BackpackPersistenceRules.BackpackSnapshot(items, Array.Empty<EquipmentStatProfile>(), Array.Empty<EquipmentInstanceData>(),
            new Dictionary<string, int> { ["spirit_herb"] = 12, ["spirit_fish"] = 99 });
        var restored = BackpackPersistenceRules.FromPlainDictionary(BackpackPersistenceRules.ToPlainDictionary(snapshot));
        Assert.Equal(items, restored.Items); Assert.Equal(12, restored.ReservedCounts!["spirit_herb"]); Assert.Equal(99, restored.ReservedCounts["spirit_fish"]);
        Assert.False(restored.Items.ContainsKey(InventoryReserveRules.SaveKey));
        var legacy = BackpackPersistenceRules.FromPlainDictionary(new Dictionary<string, object> { ["spirit_herb"] = 20 });
        Assert.Empty(legacy.ReservedCounts!); Assert.Equal(20, legacy.Items["spirit_herb"]);
    }
    [Fact]
    public void InvalidReservesNeverCrashOrChangeStockAndPreparationUsesSpendableStock()
    {
        var reserves = InventoryReserveRules.Read(new Dictionary<string, object> { ["spirit_herb"] = -1, ["spirit_fish"] = double.NaN,
            ["bad"] = "bad", ["__equipment_instances"] = 5, ["spirit_ink"] = 4L, ["future_material"] = long.MaxValue });
        Assert.Equal(2, reserves.Count); Assert.Equal(4, reserves["spirit_ink"]); Assert.Equal(InventoryReserveRules.MaxReserve, reserves["future_material"]);
        var recommendation = ChallengePreparationRules.Build("recipe_huiqi_dan", 3, 0, 1, 1, 0,
            new Dictionary<string, int> { ["spirit_herb"] = 5 }, new Dictionary<string, int> { ["spirit_herb"] = 4 })!;
        Assert.Equal(5, recommendation.Materials[0].Owned); Assert.Equal(1, recommendation.Materials[0].Spendable); Assert.Equal(3, recommendation.Materials[0].Missing);
    }
    [Fact]
    public void PreparationExcludesProtectedFinishedTalismansFromUsableStock()
    {
        var result = ChallengePreparationRules.Build("talisman_fire_charm", 2, 5, 1, 1, 0,
            new Dictionary<string, int>(), new Dictionary<string, int> { ["talisman_fire_charm"] = 4 })!;
        Assert.Equal(5, result.Owned); Assert.Equal(1, result.Spendable); Assert.Equal(1, result.Missing); Assert.Equal(1, result.Rounds);
    }
    [Fact]
    public void MigrationPreservesLegacyBuffInventoryAndExistingBudgetWithoutIssuingPortions()
    {
        var store = new SaveMigrationRules.MigrationStore();
        var cooking = new Dictionary<string, object> { ["active_meal_id"] = "food_clear_fish_soup", ["active_meal_remaining_seconds"] = 321.5 };
        store.SetValue("cooking", "state", cooking); store.SetValue("backpack", "items", new Dictionary<string, object> { ["food_clear_fish_soup"] = 7 });
        SaveMigrationRules.MigrateToLatest(store, 18);
        Assert.Equal(SaveMigrationRules.LatestVersion, store.GetValue("meta", "version", 0));
        var usage = (Dictionary<string, object>)cooking["meal_usage"];
        Assert.Equal(0, usage["portion_limit"]); Assert.Equal(1, usage["consumed_portions"]); Assert.Equal(321.5, cooking["active_meal_remaining_seconds"]);
        var backpack = (Dictionary<string, object>)store.GetValue("backpack", "items", null!);
        Assert.Equal(7, backpack["food_clear_fish_soup"]); Assert.Empty((Dictionary<string, object>)backpack[InventoryReserveRules.SaveKey]);
        usage["portion_limit"] = 2; usage["renewal_stopped"] = true;
        SaveMigrationRules.MigrateToLatest(store, 18);
        Assert.Equal(2, usage["portion_limit"]); Assert.Equal(true, usage["renewal_stopped"]); Assert.Equal(7, backpack["food_clear_fish_soup"]);
    }
}
