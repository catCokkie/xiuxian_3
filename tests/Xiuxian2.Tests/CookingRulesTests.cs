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
}
