using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class CultivationRhythmRulesTests
{
    [Fact]
    public void NormalizeCycleMinutes_ReturnsSupportedValuesAndFallsBackForUnknown()
    {
        Assert.Equal(25, CultivationRhythmRules.NormalizeCycleMinutes(25));
        Assert.Equal(45, CultivationRhythmRules.NormalizeCycleMinutes(45));
        Assert.Equal(60, CultivationRhythmRules.NormalizeCycleMinutes(60));
        Assert.Equal(90, CultivationRhythmRules.NormalizeCycleMinutes(90));
        Assert.Equal(CultivationRhythmRules.DefaultCycleMinutes, CultivationRhythmRules.NormalizeCycleMinutes(30));
    }

    [Fact]
    public void GetSmallCycleInsightReward_AlwaysFallsWithinExpectedRange()
    {
        for (int i = -100; i <= 100; i++)
        {
            int reward = CultivationRhythmRules.GetSmallCycleInsightReward(i);
            Assert.InRange(reward, 2, 5);
        }
    }

    [Fact]
    public void GetGrandCycleSpiritStoneReward_AlwaysFallsWithinExpectedRange()
    {
        for (int i = -100; i <= 100; i++)
        {
            int reward = CultivationRhythmRules.GetGrandCycleSpiritStoneReward(i);
            Assert.InRange(reward, 5, 15);
        }
    }

    [Fact]
    public void ShouldGrantMeditationBonus_UsesFivePercentThreshold()
    {
        Assert.True(CultivationRhythmRules.ShouldGrantMeditationBonus(0));
        Assert.True(CultivationRhythmRules.ShouldGrantMeditationBonus(4));
        Assert.True(CultivationRhythmRules.ShouldGrantMeditationBonus(104));
        Assert.False(CultivationRhythmRules.ShouldGrantMeditationBonus(5));
        Assert.False(CultivationRhythmRules.ShouldGrantMeditationBonus(99));
    }

    [Fact]
    public void GetMeditationBonusType_ReturnsKnownPermanentStatTypes()
    {
        HashSet<string> types = new();
        for (int i = 0; i < 12; i++)
        {
            types.Add(CultivationRhythmRules.GetMeditationBonusType(i));
        }

        Assert.Contains(CultivationRhythmRules.MeditationBonusMaxHp, types);
        Assert.Contains(CultivationRhythmRules.MeditationBonusAttack, types);
        Assert.Contains(CultivationRhythmRules.MeditationBonusDefense, types);
    }
}
