using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class GardenRulesTests
{
    [Fact]
    public void CanPlantCrop_RespectsMasteryGates()
    {
        Assert.True(GardenRules.CanPlantCrop("garden_spirit_herb", masteryLevel: 1));
        Assert.False(GardenRules.CanPlantCrop("garden_spirit_flower", masteryLevel: 1));
        Assert.True(GardenRules.CanPlantCrop("garden_spirit_flower", masteryLevel: 2));
        Assert.False(GardenRules.CanPlantCrop("garden_spirit_fruit", masteryLevel: 3));
        Assert.True(GardenRules.CanPlantCrop("garden_spirit_fruit", masteryLevel: 4));
    }

    [Fact]
    public void AdvanceProgress_CompletesBatchWhenThresholdReached()
    {
        GardenRules.GardenProgressDecision partial = GardenRules.AdvanceProgress(60.0f, 80, 200);
        GardenRules.GardenProgressDecision completed = GardenRules.AdvanceProgress(150.0f, 60, 200);

        Assert.False(partial.CompletedBatch);
        Assert.Equal(140.0f, partial.NextProgress);
        Assert.True(completed.CompletedBatch);
        Assert.Equal(0.0f, completed.NextProgress);
    }

    [Fact]
    public void HarvestCrop_GardenMasteryLevel3AddsBonusYield()
    {
        GardenRules.GatherResult normal = GardenRules.HarvestCrop("garden_spirit_herb", masteryLevel: 1);
        GardenRules.GatherResult boosted = GardenRules.HarvestCrop("garden_spirit_herb", masteryLevel: 3);

        Assert.Equal("spirit_herb", normal.ItemId);
        Assert.Equal(2, normal.ItemCount);
        Assert.Equal(3, boosted.ItemCount);
    }

    [Fact]
    public void AdvanceProgress_MasteryLevel2ReducesThreshold()
    {
        GardenRules.GardenProgressDecision atLv1 = GardenRules.AdvanceProgress(150.0f, 20, 200, masteryLevel: 1);
        GardenRules.GardenProgressDecision atLv2 = GardenRules.AdvanceProgress(150.0f, 20, 200, masteryLevel: 2);

        Assert.False(atLv1.CompletedBatch);
        Assert.True(atLv2.CompletedBatch);
    }

    [Fact]
    public void IsOfflineFullySupported_OnlyAtMasteryLevel4()
    {
        Assert.False(GardenRules.IsOfflineFullySupported(1));
        Assert.False(GardenRules.IsOfflineFullySupported(3));
        Assert.True(GardenRules.IsOfflineFullySupported(4));
    }

    [Fact]
    public void TryGetCrop_ReturnsFalseForUnknownRecipe()
    {
        bool found = GardenRules.TryGetCrop("garden_nonexistent", out _);
        Assert.False(found);
    }

    [Fact]
    public void HarvestCrop_ReturnsDefaultForUnknownRecipe()
    {
        GardenRules.GatherResult result = GardenRules.HarvestCrop("garden_nonexistent");
        Assert.Null(result.ItemId);
        Assert.Equal(0, result.ItemCount);
    }

    [Fact]
    public void GardenPersistence_RoundTripsSnapshotViaPlainDictionary()
    {
        var original = new GardenPersistenceRules.GardenSnapshot("garden_spirit_herb", 85.5f, 200.0f);
        var dict = GardenPersistenceRules.ToPlainDictionary(original);
        var restored = GardenPersistenceRules.FromPlainDictionary(dict);

        Assert.Equal(original.SelectedRecipeId, restored.SelectedRecipeId);
        Assert.Equal(original.Progress, restored.Progress, 2);
        Assert.Equal(original.RequiredProgress, restored.RequiredProgress, 2);
    }
}
