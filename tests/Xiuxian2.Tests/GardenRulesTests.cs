using System.Collections.Generic;
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
    public void GetUnlockedPlotCount_UsesBasePlusShopExpansion()
    {
        Assert.Equal(2, GardenRules.GetUnlockedPlotCount());
        Assert.Equal(4, GardenRules.GetUnlockedPlotCount(2));
        Assert.Equal(6, GardenRules.GetUnlockedPlotCount(10));
    }

    [Fact]
    public void GetEffectiveGrowthSeconds_Level2ShortensGrowth()
    {
        Assert.Equal(1800, GardenRules.GetEffectiveGrowthSeconds("garden_spirit_herb", masteryLevel: 1));
        Assert.Equal(1530, GardenRules.GetEffectiveGrowthSeconds("garden_spirit_herb", masteryLevel: 2));
    }

    [Fact]
    public void ResolveHarvest_Level3AddsBonusYield()
    {
        GardenRules.HarvestResult normal = GardenRules.ResolveHarvest("garden_spirit_herb", masteryLevel: 1, yieldRollPercent: 0, seedRollPercent: 99);
        GardenRules.HarvestResult boosted = GardenRules.ResolveHarvest("garden_spirit_herb", masteryLevel: 3, yieldRollPercent: 0, seedRollPercent: 99);

        Assert.Equal("spirit_herb", normal.ItemId);
        Assert.Equal(2, normal.ItemCount);
        Assert.Equal(3, boosted.ItemCount);
    }

    [Fact]
    public void ResolveHarvest_CanGrantBonusSeed()
    {
        GardenRules.HarvestResult harvest = GardenRules.ResolveHarvest("garden_spirit_flower", masteryLevel: 3, yieldRollPercent: 0, seedRollPercent: 0);

        Assert.Equal("spirit_flower", harvest.ItemId);
        Assert.Equal("seed_spirit_flower", harvest.BonusSeedItemId);
        Assert.Equal(1, harvest.BonusSeedCount);
    }

    [Fact]
    public void IsOfflineFullySupported_AlwaysTrue()
    {
        Assert.True(GardenRules.IsOfflineFullySupported(1));
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
        GardenPersistenceRules.GardenPlotSnapshot[] plots = GardenPersistenceRules.CreateEmptyPlots();
        plots[1] = new GardenPersistenceRules.GardenPlotSnapshot("garden_spirit_flower", 123456L, true);
        var original = new GardenPersistenceRules.GardenSnapshot("garden_spirit_flower", 1, plots);

        Dictionary<string, object> dict = GardenPersistenceRules.ToPlainDictionary(original);
        GardenPersistenceRules.GardenSnapshot restored = GardenPersistenceRules.FromPlainDictionary(dict);

        Assert.Equal(original.SelectedRecipeId, restored.SelectedRecipeId);
        Assert.Equal(original.SelectedPlotIndex, restored.SelectedPlotIndex);
        Assert.Equal(GardenRules.MaxPlotCount, restored.Plots.Length);
        Assert.Equal("garden_spirit_flower", restored.Plots[1].CropId);
        Assert.Equal(123456L, restored.Plots[1].PlantedAtUnix);
        Assert.True(restored.Plots[1].IsReady);
        Assert.Equal(string.Empty, restored.Plots[0].CropId);
    }

    [Fact]
    public void GardenPersistence_LegacySinglePlotSnapshotUpgradesToPlots()
    {
        Dictionary<string, object> legacy = new()
        {
            ["selected_recipe"] = "garden_spirit_herb",
            ["progress"] = 100.0,
            ["required_progress"] = 200.0,
        };

        GardenPersistenceRules.GardenSnapshot restored = GardenPersistenceRules.FromPlainDictionary(legacy);

        Assert.Equal("garden_spirit_herb", restored.SelectedRecipeId);
        Assert.Equal(0, restored.SelectedPlotIndex);
        Assert.Equal(GardenRules.MaxPlotCount, restored.Plots.Length);
        Assert.Equal("garden_spirit_herb", restored.Plots[0].CropId);
        Assert.False(restored.Plots[0].IsReady);
        Assert.True(restored.Plots[0].PlantedAtUnix > 0);
    }
}
