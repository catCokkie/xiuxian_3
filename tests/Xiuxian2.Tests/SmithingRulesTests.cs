using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class SmithingRulesTests
{
    [Fact]
    public void GetMaxEnhanceLevel_ScalesWithRarity()
    {
        Assert.Equal(3, SmithingRules.GetMaxEnhanceLevel(1));
        Assert.Equal(6, SmithingRules.GetMaxEnhanceLevel(2));
        Assert.Equal(9, SmithingRules.GetMaxEnhanceLevel(3));
    }

    [Fact]
    public void GetCost_UsesTieredMaterialBands()
    {
        Assert.Equal(new SmithingCost(3, 0, 30.0, 100), SmithingRules.GetCost(0));
        Assert.Equal(new SmithingCost(5, 1, 80.0, 250), SmithingRules.GetCost(3));
        Assert.Equal(new SmithingCost(8, 3, 150.0, 400), SmithingRules.GetCost(6));
    }

    [Fact]
    public void GetEnhancedValue_AppliesEightPercentCompounding()
    {
        double enhanced = EquipmentStatProfile.GetEnhancedValue(10.0, 3);

        Assert.Equal(12.59712, enhanced, 5);
    }
}
