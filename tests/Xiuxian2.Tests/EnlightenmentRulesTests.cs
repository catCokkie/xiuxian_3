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

    [Fact]
    public void GetEffectiveMeditationCap_IncreasesAtMasteryLevel3()
    {
        Assert.Equal(20, EnlightenmentRules.GetEffectiveMeditationCap(1));
        Assert.Equal(30, EnlightenmentRules.GetEffectiveMeditationCap(3));
    }

    [Fact]
    public void GetEffectiveContemplationCap_IncreasesAtMasteryLevel4()
    {
        Assert.Equal(10, EnlightenmentRules.GetEffectiveContemplationCap(1));
        Assert.Equal(15, EnlightenmentRules.GetEffectiveContemplationCap(4));
    }

    [Fact]
    public void GetInsightRateGain_BoostsByEfficiencyAtMasteryLevel2()
    {
        double basRate = EnlightenmentRules.GetInsightRateGain("enlightenment_meditation", masteryLevel: 1);
        double boostedRate = EnlightenmentRules.GetInsightRateGain("enlightenment_meditation", masteryLevel: 2);

        Assert.Equal(0.02, basRate, 6);
        Assert.True(boostedRate > basRate);
    }

    [Fact]
    public void CanApply_MasteryLevel3ExtendsMeditationCap()
    {
        Assert.False(EnlightenmentRules.CanApply("enlightenment_meditation", 20, masteryLevel: 1));
        Assert.True(EnlightenmentRules.CanApply("enlightenment_meditation", 20, masteryLevel: 3));
    }

    [Fact]
    public void CanApply_ReturnsFalseForUnknownRecipe()
    {
        Assert.False(EnlightenmentRules.CanApply("unknown_recipe", 0));
    }

    [Fact]
    public void GetInsightRateGain_ReturnsZeroForUnknownRecipe()
    {
        Assert.Equal(0.0, EnlightenmentRules.GetInsightRateGain("unknown_recipe"));
    }
}
