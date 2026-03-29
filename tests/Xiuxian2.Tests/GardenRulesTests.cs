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
}
