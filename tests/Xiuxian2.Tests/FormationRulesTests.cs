using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class FormationRulesTests
{
    [Fact]
    public void SpiritPlate_ProvidesLingqiRateBonus()
    {
        Assert.Equal(0.08, FormationRules.GetLingqiRewardRate("formation_spirit_plate"), 6);
    }

    [Fact]
    public void GuardFlag_ProvidesDefenseRateBonus()
    {
        CharacterStatModifier modifier = FormationRules.GetModifier("formation_guard_flag");
        Assert.Equal(0.05, modifier.DefenseRate, 6);
    }

    [Fact]
    public void GetMaxSlotCount_OneAtLevel1_TwoAtLevel3()
    {
        Assert.Equal(1, FormationRules.GetMaxSlotCount(1));
        Assert.Equal(2, FormationRules.GetMaxSlotCount(3));
    }

    [Fact]
    public void HasSelfRepair_FalseAtLevel3_TrueAtLevel4()
    {
        Assert.False(FormationRules.HasSelfRepair(3));
        Assert.True(FormationRules.HasSelfRepair(4));
    }

    [Fact]
    public void GetModifier_MasteryLevel2BoostsEffectByTenPercent()
    {
        CharacterStatModifier baseMod = FormationRules.GetModifier("formation_guard_flag", masteryLevel: 1);
        CharacterStatModifier boostedMod = FormationRules.GetModifier("formation_guard_flag", masteryLevel: 2);

        Assert.True(boostedMod.DefenseRate > baseMod.DefenseRate);
    }

    [Fact]
    public void GetModifier_ReturnsDefaultForUnknownRecipe()
    {
        CharacterStatModifier modifier = FormationRules.GetModifier("unknown_recipe");
        Assert.Equal(default, modifier);
    }

    [Fact]
    public void GetLingqiRewardRate_ReturnsZeroForUnknownRecipe()
    {
        Assert.Equal(0.0, FormationRules.GetLingqiRewardRate("unknown_recipe"));
    }
}
