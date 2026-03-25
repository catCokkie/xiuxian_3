using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ConsumableUsageRulesTests
{
    [Fact]
    public void DetermineAutoConsume_UsesJulingSanAtBattleStart()
    {
        BattleConsumableState state = new(36, 36, true, false, false);
        List<PotionUsage> usages = ConsumableUsageRules.DetermineAutoConsume(state, new Dictionary<string, int> { ["potion_juling_san"] = 1 });

        Assert.Single(usages);
        Assert.Equal("potion_juling_san", usages[0].PotionId);
    }

    [Fact]
    public void DetermineAutoConsume_UsesHuiqiDanWhenHpBelowHalf()
    {
        BattleConsumableState state = new(17, 40, false, false, false);
        List<PotionUsage> usages = ConsumableUsageRules.DetermineAutoConsume(state, new Dictionary<string, int> { ["potion_huiqi_dan"] = 2 });

        Assert.Single(usages);
        Assert.Equal("potion_huiqi_dan", usages[0].PotionId);
    }

    [Fact]
    public void DetermineAutoConsume_DoesNotRepeatHealPotionSameBattle()
    {
        BattleConsumableState state = new(10, 40, false, false, true);
        List<PotionUsage> usages = ConsumableUsageRules.DetermineAutoConsume(state, new Dictionary<string, int> { ["potion_huiqi_dan"] = 1 });

        Assert.Empty(usages);
    }
}
