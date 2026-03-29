using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class FishingRulesTests
{
    [Fact]
    public void CanFishPond_RequiresMasteryForRarePond()
    {
        Assert.True(FishingRules.CanFishPond("fishing_spirit_fish", masteryLevel: 1));
        Assert.False(FishingRules.CanFishPond("fishing_deep_pond", masteryLevel: 1));
        Assert.True(FishingRules.CanFishPond("fishing_deep_pond", masteryLevel: 2));
    }

    [Fact]
    public void AdvanceProgress_CompletesBatchWhenThresholdReached()
    {
        FishingRules.FishingProgressDecision partial = FishingRules.AdvanceProgress(50.0f, 40, 120);
        FishingRules.FishingProgressDecision completed = FishingRules.AdvanceProgress(90.0f, 40, 120);

        Assert.False(partial.CompletedBatch);
        Assert.Equal(90.0f, partial.NextProgress);
        Assert.True(completed.CompletedBatch);
        Assert.Equal(0.0f, completed.NextProgress);
    }

    [Fact]
    public void CatchFish_FishingMasteryLevel4AddsBonusYield()
    {
        FishingRules.CatchResult normal = FishingRules.CatchFish("fishing_spirit_fish", masteryLevel: 1);
        FishingRules.CatchResult boosted = FishingRules.CatchFish("fishing_spirit_fish", masteryLevel: 4);

        Assert.Equal("spirit_fish", normal.ItemId);
        Assert.Equal(2, normal.ItemCount);
        Assert.Equal(3, boosted.ItemCount);
    }

    [Fact]
    public void AdvanceProgress_MasteryLevel2ReducesThreshold()
    {
        FishingRules.FishingProgressDecision atLv1 = FishingRules.AdvanceProgress(90.0f, 12, 120, masteryLevel: 1);
        FishingRules.FishingProgressDecision atLv2 = FishingRules.AdvanceProgress(90.0f, 12, 120, masteryLevel: 2);

        Assert.False(atLv1.CompletedBatch);
        Assert.True(atLv2.CompletedBatch);
    }

    [Fact]
    public void GetDoubleOutputChance_ZeroAtLevel1_EightPercentAtLevel4()
    {
        Assert.Equal(0.0, FishingRules.GetDoubleOutputChance(1));
        Assert.Equal(0.08, FishingRules.GetDoubleOutputChance(4));
    }

    [Fact]
    public void TryGetPond_ReturnsFalseForUnknownRecipe()
    {
        bool found = FishingRules.TryGetPond("fishing_nonexistent", out _);
        Assert.False(found);
    }

    [Fact]
    public void CatchFish_ReturnsDefaultForUnknownRecipe()
    {
        FishingRules.CatchResult result = FishingRules.CatchFish("fishing_nonexistent");
        Assert.Null(result.ItemId);
        Assert.Equal(0, result.ItemCount);
    }

    [Fact]
    public void FishingPersistence_RoundTripsSnapshotViaPlainDictionary()
    {
        var original = new FishingPersistenceRules.FishingSnapshot("fishing_spirit_pearl", 60.0f, 180.0f);
        var dict = FishingPersistenceRules.ToPlainDictionary(original);
        var restored = FishingPersistenceRules.FromPlainDictionary(dict);

        Assert.Equal(original.SelectedRecipeId, restored.SelectedRecipeId);
        Assert.Equal(original.Progress, restored.Progress, 2);
        Assert.Equal(original.RequiredProgress, restored.RequiredProgress, 2);
    }
}
