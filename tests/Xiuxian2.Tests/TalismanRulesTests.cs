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
    public void ShieldCharm_ProvidesSpeedAndAttackBonus()
    {
        CharacterStatModifier modifier = TalismanRules.GetModifier("talisman_shield_charm");
        Assert.Equal(0.10, modifier.AttackRate, 6);
        Assert.Equal(0.25, modifier.SpeedRate, 6);
    }

    [Fact]
    public void BurstCharm_ProvidesHighestAttackRateBonus()
    {
        CharacterStatModifier modifier = TalismanRules.GetModifier("talisman_burst_charm");
        Assert.Equal(0.35, modifier.AttackRate, 6);
    }

    [Fact]
    public void GetRecipes_ReturnsThreeRecipesWithMasteryThresholds()
    {
        IReadOnlyList<TalismanRules.RecipeSpec> recipes = TalismanRules.GetRecipes();

        Assert.Equal(3, recipes.Count);
        Assert.Equal(1, recipes[0].RequiredMasteryLevel);
        Assert.Equal(2, recipes[1].RequiredMasteryLevel);
        Assert.Equal(4, recipes[2].RequiredMasteryLevel);
    }

    [Fact]
    public void GetMaterialDiscount_ZeroAtLevel2_TenPercentAtLevel3()
    {
        Assert.Equal(0.0, TalismanRules.GetMaterialDiscount(2));
        Assert.Equal(0.10, TalismanRules.GetMaterialDiscount(3));
    }

    [Fact]
    public void GetMaxTalismansPerBattle_OneBeforeLevel4_TwoAtLevel4()
    {
        Assert.Equal(1, TalismanRules.GetMaxTalismansPerBattle(3));
        Assert.Equal(2, TalismanRules.GetMaxTalismansPerBattle(4));
    }

    [Fact]
    public void GetModifier_ReturnsDefaultForUnknownRecipe()
    {
        CharacterStatModifier modifier = TalismanRules.GetModifier("unknown_recipe");
        Assert.Equal(default, modifier);
    }
}
