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
}
