using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class EnlightenmentRulesTests
{
    [Fact]
    public void Meditation_HasTwentyCapAndTwoPercentGain()
    {
        Assert.True(EnlightenmentRules.CanApply("enlightenment_meditation", 19));
        Assert.False(EnlightenmentRules.CanApply("enlightenment_meditation", 20));
        Assert.Equal(0.02, EnlightenmentRules.GetInsightRateGain("enlightenment_meditation"), 6);
    }

    [Fact]
    public void Contemplation_HasTenCapAndFivePercentGain()
    {
        Assert.True(EnlightenmentRules.CanApply("enlightenment_contemplation", 9));
        Assert.False(EnlightenmentRules.CanApply("enlightenment_contemplation", 10));
        Assert.Equal(0.05, EnlightenmentRules.GetInsightRateGain("enlightenment_contemplation"), 6);
    }
}
