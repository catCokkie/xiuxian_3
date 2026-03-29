using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class TalismanRulesTests
{
    [Fact]
    public void FireCharm_ProvidesAttackRateBonus()
    {
        CharacterStatModifier modifier = TalismanRules.GetModifier("talisman_fire_charm");
        Assert.Equal(0.15, modifier.AttackRate, 6);
    }

    [Fact]
    public void ShieldCharm_ProvidesDefenseRateBonus()
    {
        CharacterStatModifier modifier = TalismanRules.GetModifier("talisman_shield_charm");
        Assert.Equal(0.20, modifier.DefenseRate, 6);
    }

    [Fact]
    public void GetDoubleOutputChance_ZeroAtLevel1_TenPercentAtLevel2()
    {
        Assert.Equal(0.0, TalismanRules.GetDoubleOutputChance(1));
        Assert.Equal(0.10, TalismanRules.GetDoubleOutputChance(2));
    }

    [Fact]
    public void GetMaterialDiscount_ZeroAtLevel1_TenPercentAtLevel3()
    {
        Assert.Equal(0.0, TalismanRules.GetMaterialDiscount(1));
        Assert.Equal(0.10, TalismanRules.GetMaterialDiscount(3));
    }

    [Fact]
    public void GetEnchantChance_ZeroAtLevel3_TenPercentAtLevel4()
    {
        Assert.Equal(0.0, TalismanRules.GetEnchantChance(3));
        Assert.Equal(0.10, TalismanRules.GetEnchantChance(4));
    }

    [Fact]
    public void GetModifier_ReturnsDefaultForUnknownRecipe()
    {
        CharacterStatModifier modifier = TalismanRules.GetModifier("unknown_recipe");
        Assert.Equal(default, modifier);
    }
}
