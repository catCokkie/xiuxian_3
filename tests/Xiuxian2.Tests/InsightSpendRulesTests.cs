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
}
