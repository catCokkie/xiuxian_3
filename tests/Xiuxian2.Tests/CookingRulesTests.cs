using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class CookingRulesTests
{
    [Fact]
    public void FruitJelly_ProvidesAllStatRateBonus()
    {
        CharacterStatModifier modifier = CookingRules.GetModifier("food_fruit_jelly");
        Assert.Equal(0.05, modifier.AttackRate, 6);
        Assert.Equal(0.05, modifier.DefenseRate, 6);
        Assert.Equal(0.05, modifier.SpeedRate, 6);
    }

    [Fact]
    public void DragonSoup_ProvidesLingqiRewardBonus()
    {
        Assert.Equal(0.30, CookingRules.GetLingqiRewardRate("food_dragon_soup"), 6);
    }

    [Fact]
    public void GetEffectiveDuration_BaseAtLevel1_PlusOneAtLevel2()
    {
        Assert.Equal(3, CookingRules.GetEffectiveDuration(1));
        Assert.Equal(4, CookingRules.GetEffectiveDuration(2));
    }

    [Fact]
    public void GetDoubleOutputChance_ZeroAtLevel1_FifteenPercentAtLevel3()
    {
        Assert.Equal(0.0, CookingRules.GetDoubleOutputChance(1));
        Assert.Equal(0.15, CookingRules.GetDoubleOutputChance(3));
    }

    [Fact]
    public void HasExtraEffect_FalseAtLevel3_TrueAtLevel4()
    {
        Assert.False(CookingRules.HasExtraEffect(3));
        Assert.True(CookingRules.HasExtraEffect(4));
    }

    [Fact]
    public void GetModifier_ReturnsDefaultForUnknownRecipe()
    {
        CharacterStatModifier modifier = CookingRules.GetModifier("unknown_recipe");
        Assert.Equal(default, modifier);
    }

    [Fact]
    public void GetLingqiRewardRate_ReturnsZeroForUnknownRecipe()
    {
        Assert.Equal(0.0, CookingRules.GetLingqiRewardRate("unknown_recipe"));
    }
}
