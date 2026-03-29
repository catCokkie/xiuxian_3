using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class InsightSpendRulesTests
{
    [Fact]
    public void CanUnlockMastery_RequiresEnoughInsightAndFreshState()
    {
        Assert.True(InsightSpendRules.CanUnlockMastery(PlayerActionState.ModeAlchemy, 1, 20.0, 1));
        Assert.False(InsightSpendRules.CanUnlockMastery(PlayerActionState.ModeAlchemy, 2, 45.0, 1));
        Assert.False(InsightSpendRules.CanUnlockMastery(PlayerActionState.ModeAlchemy, 1, 19.0, 1));
    }

    [Fact]
    public void GetMasteryCost_ReturnsConfiguredCost()
    {
        Assert.Equal(30.0, InsightSpendRules.GetMasteryCost(PlayerActionState.ModeDungeon, 1));
        Assert.Equal(20.0, InsightSpendRules.GetMasteryCost(PlayerActionState.ModeAlchemy, 1));
    }

    [Fact]
    public void SpendInsightForMastery_DeductsInsightAndAdvancesLevel()
    {
        bool unlocked = InsightSpendRules.SpendInsightForMastery(PlayerActionState.ModeDungeon, 1, 30.0, 2, out int nextLevel, out double remainingInsight);

        Assert.True(unlocked);
        Assert.Equal(2, nextLevel);
        Assert.Equal(0.0, remainingInsight);
    }

    [Fact]
    public void SpendInsightForMastery_ReturnsFalseWhenRealmTooLow()
    {
        bool unlocked = InsightSpendRules.SpendInsightForMastery(PlayerActionState.ModeAlchemy, 2, 45.0, 1, out int nextLevel, out double remainingInsight);

        Assert.False(unlocked);
        Assert.Equal(2, nextLevel);
        Assert.Equal(45.0, remainingInsight);
    }

    [Fact]
    public void GetMasteryCost_ReturnsZeroAtMaxLevel()
    {
        Assert.Equal(0.0, InsightSpendRules.GetMasteryCost(PlayerActionState.ModeDungeon, 4));
    }
}
