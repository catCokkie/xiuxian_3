using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class InsightSpendRulesTests
{
    [Fact]
    public void CanUnlockAdvancedAlchemy_RequiresEnoughInsightAndFreshState()
    {
        Assert.True(InsightSpendRules.CanUnlockAdvancedAlchemy(false, 20));
        Assert.False(InsightSpendRules.CanUnlockAdvancedAlchemy(true, 20));
        Assert.False(InsightSpendRules.CanUnlockAdvancedAlchemy(false, 19));
    }

    [Fact]
    public void ApplyBossWeaknessMultiplier_ReducesStatsByTenPercent()
    {
        Assert.Equal(90.0, InsightSpendRules.ApplyBossWeaknessMultiplier(100.0));
        Assert.Equal(9.0, InsightSpendRules.ApplyBossWeaknessMultiplier(10.0));
    }

    [Fact]
    public void GetBossWeaknessInsightCost_ScalesByZoneWithin30To80()
    {
        Assert.Equal(30.0, InsightSpendRules.GetBossWeaknessInsightCost(0));
        Assert.Equal(42.0, InsightSpendRules.GetBossWeaknessInsightCost(1));
        Assert.Equal(54.0, InsightSpendRules.GetBossWeaknessInsightCost(2));
        Assert.Equal(66.0, InsightSpendRules.GetBossWeaknessInsightCost(3));
        Assert.Equal(78.0, InsightSpendRules.GetBossWeaknessInsightCost(4));
        // Clamped to max 80
        Assert.Equal(80.0, InsightSpendRules.GetBossWeaknessInsightCost(10));
    }
}
