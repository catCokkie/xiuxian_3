using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

[Collection("ActivityRegistry")]
public sealed class ProductionBlockRulesTests
{
    [Fact]
    public void OwnedStockAndReservedStockGiveDifferentRecoveryReasons()
    {
        var missing = ProductionBlockRules.Alchemy("recipe_huiqi_dan", 50, new Dictionary<string, int> { ["spirit_herb"] = 1 }, null, 1);
        var reserved = ProductionBlockRules.Alchemy("recipe_huiqi_dan", 50, new Dictionary<string, int> { ["spirit_herb"] = 2 }, new Dictionary<string, int> { ["spirit_herb"] = 1 }, 1);
        Assert.Equal(ProductionBlockKind.Material, missing.Primary!.Kind);
        Assert.Equal(ProductionBlockKind.Reserve, reserved.Primary!.Kind);
        Assert.Equal(1, reserved.Materials.Single().Missing);
        Assert.Contains("持有2 / 保留1 / 可用1", ProductionBlockRules.Summary(reserved));
        Assert.False(reserved.CanProduce);
    }
    [Fact]
    public void AllBlockersRemainVisibleWithOneStablePrimaryReason()
    {
        var plan = new CraftingProductionPlan(); plan.Stop();
        var check = ProductionBlockRules.Alchemy("recipe_juling_san", 10, new Dictionary<string, int>(), null, 1, plan, 3);
        Assert.Equal(new[] { ProductionBlockKind.Paused, ProductionBlockKind.ActivityLocked, ProductionBlockKind.Mastery,
            ProductionBlockKind.Material, ProductionBlockKind.Lingqi }, check.Blocks.Select(x => x.Kind));
        Assert.Contains("需Lv2", ProductionBlockRules.Details(check));
        Assert.Contains("缺70", ProductionBlockRules.Details(check));
        Assert.True(plan.Paused); Assert.Equal(0, plan.CompletedRounds);
    }
    [Fact]
    public void FinishedPlanDoesNotInventRequirementsForAnExtraBatch()
    {
        var plan = new CraftingProductionPlan(); plan.Start(false, 1); plan.CommitBatch(); plan.Stop();
        var check = ProductionBlockRules.Alchemy("recipe_huiqi_dan", 0, new Dictionary<string, int>(), null, 1, plan);
        Assert.Equal(ProductionBlockKind.Finished, Assert.Single(check.Blocks).Kind);
        Assert.Empty(check.Materials); Assert.Equal(0, check.LingqiRequired);
        plan.Start(false, 1);
        Assert.Equal(ProductionBlockKind.Material, ProductionBlockRules.Alchemy("recipe_huiqi_dan", 0, new Dictionary<string, int>(), null, 1, plan).Primary!.Kind);
    }
    [Theory]
    [InlineData(3, 3, 1, 2)]
    [InlineData(3, 1, 1, 0)]
    [InlineData(6, 3, 2, 4)]
    [InlineData(9, 4, 4, 0)]
    public void EnhancementCapDistinguishesMasteryFromEquipmentLimit(int level, int rarity, int mastery, int nextMastery)
    {
        var profile = new EquipmentStatProfile("weapon", "test", EquipmentSlotType.Weapon, default, Rarity: rarity, EnhanceLevel: level);
        var check = ProductionBlockRules.Smithing(profile, 0, new Dictionary<string, int>(), null, mastery);
        Assert.Equal(ProductionBlockKind.EnhanceCap, Assert.Single(check.Blocks).Kind);
        Assert.Equal(nextMastery, check.Primary!.RequiredLevel);
        Assert.Empty(check.Materials); Assert.Equal(0, check.LingqiRequired);
    }
    [Fact]
    public void EnhancementDiscountAndZeroCostInputsMatchActualSpending()
    {
        var profile = new EquipmentStatProfile("weapon", "test", EquipmentSlotType.Weapon, default, Rarity: 3, EnhanceLevel: 3);
        var check = ProductionBlockRules.Smithing(profile, 80, new Dictionary<string, int> { ["lingqi_shard"] = 4 }, new Dictionary<string, int> { ["broken_talisman"] = 99 }, 3);
        Assert.True(check.CanProduce);
        Assert.Equal(4, Assert.Single(check.Materials).Required);
    }
    [Fact]
    public void FractionalQiDeficitIsReportedWithoutRoundingItToZero()
    {
        var check = ProductionBlockRules.Alchemy("recipe_huiqi_dan", 49.75, new Dictionary<string, int> { ["spirit_herb"] = 2 }, null, 1);
        Assert.Equal(ProductionBlockKind.Lingqi, check.Primary!.Kind);
        Assert.Contains("缺0.25", ProductionBlockRules.Summary(check));
        Assert.False(AlchemyRules.CanStartRecipe("recipe_huiqi_dan", 49.75, new Dictionary<string, int> { ["spirit_herb"] = 2 }));
    }
    [Fact]
    public void MultipleEntriesForOneMaterialUseTheirCombinedCost()
    {
        var check = ProductionBlockRules.Check(new[] { new MaterialCost("spirit_ink", 2), new MaterialCost("spirit_ink", 2) }, 0, 0,
            new Dictionary<string, int> { ["spirit_ink"] = 3 }, null, 1);
        Assert.Equal(4, Assert.Single(check.Materials).Required);
        Assert.Equal(ProductionBlockKind.Material, check.Primary!.Kind);
    }
    [Fact]
    public void TalismanCostUsesTheSameDiscountAsCompletionAndPreparation()
    {
        var recipe = ActivityRegistry.GetRecipe("talisman_fire_charm")!;
        var inputs = CraftingProgressionService.GetEffectiveInputs(recipe, 4);
        var inventory = inputs.ToDictionary(x => x.ItemId, x => x.Count);
        var check = ProductionBlockRules.Check(inputs, recipe.LingqiCost, recipe.LingqiCost, inventory, null, 4, recipe.RequiredMasteryLevel);
        Assert.True(check.CanProduce);
        var preparation = ChallengePreparationRules.Build(recipe.RecipeId, 1, 0, 4, 1, 0, inventory)!;
        Assert.Equal(check.Materials.Select(x => (x.ItemId, x.Required)), preparation.Materials.Where(x => x.Required > 0).Select(x => (x.ItemId, x.Required)));
    }
    [Fact]
    public void InvalidAndMissingTargetsRemainDistinct()
    {
        Assert.Equal(ProductionBlockKind.NoTarget, ProductionBlockRules.Alchemy("", 100, new Dictionary<string, int>(), null, 4).Primary!.Kind);
        Assert.Equal(ProductionBlockKind.TargetUnavailable, ProductionBlockRules.Alchemy("unknown", 100, new Dictionary<string, int>(), null, 4).Primary!.Kind);
        Assert.Equal(3, ProgressiveUnlockRules.GetRequiredRealmLevel("smithing"));
        Assert.Equal(5, ProgressiveUnlockRules.GetRequiredRealmLevel("formation"));
    }
}
