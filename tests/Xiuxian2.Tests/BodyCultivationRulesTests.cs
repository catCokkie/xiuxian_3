using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class BodyCultivationRulesTests
{
    [Fact]
    public void Temper_HasTwentyCapAndHpRateBonus()
    {
        Assert.True(BodyCultivationRules.CanApply("body_cultivation_temper", 19));
        Assert.False(BodyCultivationRules.CanApply("body_cultivation_temper", 20));
        Assert.Equal(0.01, BodyCultivationRules.GetRateModifier("body_cultivation_temper").MaxHpRate, 6);
    }

    [Fact]
    public void Boneforge_HasFifteenCapAndDefenseRateBonus()
    {
        Assert.True(BodyCultivationRules.CanApply("body_cultivation_boneforge", 14));
        Assert.False(BodyCultivationRules.CanApply("body_cultivation_boneforge", 15));
        Assert.Equal(0.01, BodyCultivationRules.GetRateModifier("body_cultivation_boneforge").DefenseRate, 6);
    }
}
