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

    [Fact]
    public void GetEffectiveTemperCap_IncreasesAtMasteryLevel3()
    {
        Assert.Equal(20, BodyCultivationRules.GetEffectiveTemperCap(1));
        Assert.Equal(30, BodyCultivationRules.GetEffectiveTemperCap(3));
    }

    [Fact]
    public void GetEffectiveBoneforgeCap_IncreasesAtMasteryLevel4()
    {
        Assert.Equal(15, BodyCultivationRules.GetEffectiveBoneforgeCap(1));
        Assert.Equal(20, BodyCultivationRules.GetEffectiveBoneforgeCap(4));
    }

    [Fact]
    public void GetRateModifier_BoostsByEfficiencyAtMasteryLevel2()
    {
        double baseMod = BodyCultivationRules.GetRateModifier("body_cultivation_temper", masteryLevel: 1).MaxHpRate;
        double boostedMod = BodyCultivationRules.GetRateModifier("body_cultivation_temper", masteryLevel: 2).MaxHpRate;

        Assert.Equal(0.01, baseMod, 6);
        Assert.True(boostedMod > baseMod);
    }

    [Fact]
    public void CanApply_MasteryLevel3ExtendsTemperCap()
    {
        Assert.False(BodyCultivationRules.CanApply("body_cultivation_temper", 20, masteryLevel: 1));
        Assert.True(BodyCultivationRules.CanApply("body_cultivation_temper", 20, masteryLevel: 3));
    }

    [Fact]
    public void CanApply_ReturnsFalseForUnknownRecipe()
    {
        Assert.False(BodyCultivationRules.CanApply("unknown_recipe", 0));
    }

    [Fact]
    public void GetRateModifier_ReturnsDefaultForUnknownRecipe()
    {
        CharacterStatModifier modifier = BodyCultivationRules.GetRateModifier("unknown_recipe");
        Assert.Equal(default, modifier);
    }
}
