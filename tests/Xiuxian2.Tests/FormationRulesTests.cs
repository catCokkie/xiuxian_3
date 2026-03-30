using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class FormationRulesTests
{
    [Fact]
    public void SpiritPlate_ProfileProvidesBattleAndLingqiBonuses()
    {
        FormationEffectProfile profile = FormationRules.GetEffectProfile("formation_spirit_plate");

        Assert.True(profile.IsKnownFormation);
        Assert.Equal(2, profile.BattleModifier.AttackFlat);
        Assert.Equal(0.08, profile.LingqiRewardRate, 6);
    }

    [Fact]
    public void GuardFlag_ProfileProvidesDefenseBonus()
    {
        FormationEffectProfile profile = FormationRules.GetEffectProfile("formation_guard_flag");

        Assert.Equal(0.05, profile.BattleModifier.DefenseRate, 6);
        Assert.Equal(0.0, profile.GatherSpeedRate, 6);
    }

    [Fact]
    public void HarvestArray_ProfileProvidesGatherSpeedBonus()
    {
        FormationEffectProfile profile = FormationRules.GetEffectProfile("formation_harvest_array");

        Assert.Equal(0.12, profile.GatherSpeedRate, 6);
        Assert.Equal(default, profile.BattleModifier);
    }

    [Fact]
    public void CraftArray_ProfileProvidesCraftSpeedBonus()
    {
        FormationEffectProfile profile = FormationRules.GetEffectProfile("formation_craft_array");

        Assert.Equal(0.12, profile.CraftSpeedRate, 6);
        Assert.Equal(0.0, profile.LingqiRewardRate, 6);
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
    public void GetEffectProfile_MasteryLevel2BoostsRateBasedEffectsByTenPercent()
    {
        FormationEffectProfile baseProfile = FormationRules.GetEffectProfile("formation_harvest_array", masteryLevel: 1);
        FormationEffectProfile boostedProfile = FormationRules.GetEffectProfile("formation_harvest_array", masteryLevel: 2);

        Assert.True(boostedProfile.GatherSpeedRate > baseProfile.GatherSpeedRate);
    }

    [Fact]
    public void GetEffectProfile_ReturnsDefaultForUnknownRecipe()
    {
        FormationEffectProfile profile = FormationRules.GetEffectProfile("unknown_recipe");
        Assert.False(profile.IsKnownFormation);
        Assert.Equal(default, profile.BattleModifier);
    }

    [Fact]
    public void GetDerivedEffectAccessors_ReturnZeroForUnknownRecipe()
    {
        Assert.Equal(0.0, FormationRules.GetLingqiRewardRate("unknown_recipe"));
        Assert.Equal(0.0, FormationRules.GetGatherSpeedRate("unknown_recipe"));
        Assert.Equal(0.0, FormationRules.GetCraftSpeedRate("unknown_recipe"));
    }

    [Fact]
    public void ScaleEffectProfile_HalvesSecondaryEffect()
    {
        FormationEffectProfile scaled = FormationRules.ScaleEffectProfile(
            FormationRules.GetEffectProfile("formation_craft_array"),
            FormationRules.SecondarySlotEffectRatio);

        Assert.Equal(0.06, scaled.CraftSpeedRate, 6);
    }

    [Fact]
    public void CombineEffectProfiles_AddsDifferentPrimaryAndSecondaryBonuses()
    {
        FormationEffectProfile combined = FormationRules.CombineEffectProfiles(
            FormationRules.GetEffectProfile("formation_spirit_plate"),
            FormationRules.ScaleEffectProfile(FormationRules.GetEffectProfile("formation_harvest_array"), FormationRules.SecondarySlotEffectRatio));

        Assert.Equal(2, combined.BattleModifier.AttackFlat);
        Assert.Equal(0.08, combined.LingqiRewardRate, 6);
        Assert.Equal(0.06, combined.GatherSpeedRate, 6);
    }
}
