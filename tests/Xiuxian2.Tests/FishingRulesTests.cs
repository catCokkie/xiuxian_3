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
}
