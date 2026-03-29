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
}
