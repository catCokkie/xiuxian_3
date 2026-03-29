using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class AlchemyRulesTests
{
    [Fact]
    public void CanStartRecipe_RequiresEnoughMaterialsAndLingqi()
    {
        var items = new Dictionary<string, int>
        {
            ["spirit_herb"] = 2,
        };

        Assert.True(AlchemyRules.CanStartRecipe("recipe_huiqi_dan", 50.0, items, masteryLevel: 1));
        Assert.False(AlchemyRules.CanStartRecipe("recipe_huiqi_dan", 40.0, items, masteryLevel: 1));
        Assert.False(AlchemyRules.CanStartRecipe("recipe_huiqi_dan", 50.0, new Dictionary<string, int>(), masteryLevel: 1));
    }

    [Fact]
    public void CanStartRecipe_JulingSanRequiresAlchemyMasteryLevel2()
    {
        var items = new Dictionary<string, int>
        {
            ["spirit_herb"] = 3,
        };

        Assert.False(AlchemyRules.CanStartRecipe("recipe_juling_san", 80.0, items, masteryLevel: 1));
        Assert.True(AlchemyRules.CanStartRecipe("recipe_juling_san", 80.0, items, masteryLevel: 2));
    }

    [Fact]
    public void AdvanceProgress_CompletesBatchWhenThresholdReached()
    {
        AlchemyRules.AlchemyProgressDecision partial = AlchemyRules.AdvanceProgress(50.0f, 60, 200);
        AlchemyRules.AlchemyProgressDecision complete = AlchemyRules.AdvanceProgress(180.0f, 30, 200);

        Assert.False(partial.CompletedBatch);
        Assert.Equal(110.0f, partial.NextProgress);
        Assert.True(complete.CompletedBatch);
        Assert.Equal(0.0f, complete.NextProgress);
    }

    [Fact]
    public void CompleteRecipe_ReturnsExpectedPotionOutput()
    {
        AlchemyRules.AlchemyCompletionResult result = AlchemyRules.CompleteRecipe("recipe_huiqi_dan");

        Assert.Equal("potion_huiqi_dan", result.PotionItemId);
        Assert.Equal(2, result.PotionCount);
        Assert.Equal("spirit_herb", result.MaterialItemId);
        Assert.Equal(2, result.MaterialCount);
        Assert.Equal(50.0, result.LingqiCost);
    }

    [Fact]
    public void CompleteRecipe_AlchemyMasteryLevel3AddsBonusOutput()
    {
        AlchemyRules.AlchemyCompletionResult result = AlchemyRules.CompleteRecipe("recipe_huiqi_dan", masteryLevel: 3);

        Assert.Equal(3, result.PotionCount);
    }
}
