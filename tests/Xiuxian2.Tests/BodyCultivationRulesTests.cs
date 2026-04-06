using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class BodyCultivationRulesTests
{
    [Fact]
    public void Temper_HasTwentyCapAndFlatHpReward()
    {
        Assert.True(BodyCultivationRules.CanApply("body_cultivation_temper", 19));
        Assert.False(BodyCultivationRules.CanApply("body_cultivation_temper", 20));
        Assert.Equal(6, BodyCultivationRules.GetRateModifier("body_cultivation_temper").MaxHpFlat);
    }

    [Fact]
    public void Boneforge_HasFifteenCapAndFlatDefenseReward()
    {
        Assert.True(BodyCultivationRules.CanApply("body_cultivation_boneforge", 14));
        Assert.False(BodyCultivationRules.CanApply("body_cultivation_boneforge", 15));
        Assert.Equal(2, BodyCultivationRules.GetRateModifier("body_cultivation_boneforge").DefenseFlat);
    }

    [Fact]
    public void Bloodflow_HasTenCapAndPostBattleHealRate()
    {
        Assert.True(BodyCultivationRules.CanApply("body_cultivation_bloodflow", 9));
        Assert.False(BodyCultivationRules.CanApply("body_cultivation_bloodflow", 10));
        Assert.Equal(2, BodyCultivationRules.GetRateModifier("body_cultivation_bloodflow").AttackFlat);
        Assert.Equal(0.02, BodyCultivationRules.GetPostBattleHealRate("body_cultivation_bloodflow"), 6);
    }

    [Fact]
    public void GetMaterialDiscount_ZeroAtLevel2_FifteenPercentAtLevel3()
    {
        Assert.Equal(0.0, BodyCultivationRules.GetMaterialDiscount(2));
        Assert.Equal(0.15, BodyCultivationRules.GetMaterialDiscount(3), 6);
    }

    [Fact]
    public void GetEffectiveCaps_IncreaseByFiveAtMasteryLevel4()
    {
        Assert.Equal(20, BodyCultivationRules.GetEffectiveTemperCap(1));
        Assert.Equal(25, BodyCultivationRules.GetEffectiveTemperCap(4));
        Assert.Equal(15, BodyCultivationRules.GetEffectiveBoneforgeCap(1));
        Assert.Equal(20, BodyCultivationRules.GetEffectiveBoneforgeCap(4));
        Assert.Equal(10, BodyCultivationRules.GetEffectiveBloodflowCap(1));
        Assert.Equal(15, BodyCultivationRules.GetEffectiveBloodflowCap(4));
    }

    [Fact]
    public void CanApply_MasteryLevel4ExtendsCap()
    {
        Assert.False(BodyCultivationRules.CanApply("body_cultivation_temper", 20, masteryLevel: 1));
        Assert.True(BodyCultivationRules.CanApply("body_cultivation_temper", 20, masteryLevel: 4));
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
