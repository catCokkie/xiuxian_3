using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ActivityEffectRulesTests
{
    [Fact]
    public void DetermineAutoUseBackpackConsumables_ReturnsBattleStartItems()
    {
        List<BackpackConsumableUsage> usages = ActivityEffectRules.DetermineAutoUseBackpackConsumables(
            new BattleConsumableState(20, 40, true, false, false),
            new Dictionary<string, int>
            {
                ["talisman_fire_charm"] = 1,
                ["food_dragon_soup"] = 1,
            });

        Assert.Contains(usages, x => x.ItemId == "talisman_fire_charm");
        Assert.Contains(usages, x => x.ItemId == "food_dragon_soup");
    }

    [Fact]
    public void CollectFormationModifier_ReturnsPassiveStatsWhenItemsOwned()
    {
        CharacterStatModifier modifier = ActivityEffectRules.CollectFormationModifier(
            "formation_spirit_plate",
            new Dictionary<string, int>
            {
                ["formation_spirit_plate"] = 1,
                ["formation_guard_flag"] = 1,
            });

        Assert.Equal(2, modifier.AttackFlat);
        Assert.Equal(0, modifier.DefenseFlat);
    }

    [Fact]
    public void CollectPermanentProgressModifier_ReturnsBodyCultivationStats()
    {
        CharacterStatModifier modifier = ActivityEffectRules.CollectPermanentProgressModifier(
            new PlayerProgressPersistenceRules.PlayerProgressSnapshot(1, 0.0, 60, false, 0.0, 0.05, 0.08, 10, 4, 3, 2, 1, 3, 2));

        Assert.Equal(10, modifier.MaxHpFlat);
        Assert.Equal(4, modifier.AttackFlat);
        Assert.Equal(3, modifier.DefenseFlat);
    }

    [Fact]
    public void DetermineAutoUseBackpackConsumables_ReturnsEmptyWhenNotBattleStart()
    {
        List<BackpackConsumableUsage> usages = ActivityEffectRules.DetermineAutoUseBackpackConsumables(
            new BattleConsumableState(20, 40, false, false, false),
            new Dictionary<string, int>
            {
                ["talisman_fire_charm"] = 1,
                ["food_dragon_soup"] = 1,
            });

        Assert.Empty(usages);
    }

    [Fact]
    public void GetBackpackConsumableModifier_ReturnsTalismanStats()
    {
        CharacterStatModifier modifier = ActivityEffectRules.GetBackpackConsumableModifier("talisman_fire_charm");
        Assert.Equal(0.15, modifier.AttackRate, 6);
    }

    [Fact]
    public void GetBackpackConsumableModifier_ReturnsDefaultForUnknown()
    {
        CharacterStatModifier modifier = ActivityEffectRules.GetBackpackConsumableModifier("unknown_item");
        Assert.Equal(default, modifier);
    }

    [Fact]
    public void CollectFormationLingqiRate_ReturnsRateWhenItemOwned()
    {
        double rate = ActivityEffectRules.CollectFormationLingqiRate(
            "formation_spirit_plate",
            new Dictionary<string, int> { ["formation_spirit_plate"] = 1 });

        Assert.Equal(0.08, rate, 6);
    }

    [Fact]
    public void CollectFormationLingqiRate_ReturnsZeroWhenNoItems()
    {
        double rate = ActivityEffectRules.CollectFormationLingqiRate(
            "formation_spirit_plate",
            new Dictionary<string, int>());

        Assert.Equal(0.0, rate);
    }

    [Fact]
    public void CollectFormationModifier_ReturnsDefaultWhenRecipeEmpty()
    {
        CharacterStatModifier modifier = ActivityEffectRules.CollectFormationModifier(
            string.Empty,
            new Dictionary<string, int> { ["formation_spirit_plate"] = 1 });

        Assert.Equal(default, modifier);
    }
}
