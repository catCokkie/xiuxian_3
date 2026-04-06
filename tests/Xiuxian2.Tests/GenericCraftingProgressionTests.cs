using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

[Collection("ActivityRegistry")]
public sealed class GenericCraftingProgressionTests
{
    [Fact]
    public void AdvanceGenericRecipe_AdvancesProgress()
    {
        ActivityRegistry.ResetForTesting();
        var result = CraftingProgressionService.AdvanceGenericRecipe("recipe_huiqi_dan", 0f, 50);

        Assert.False(result.CompletedBatch);
        Assert.True(result.ProgressPercent > 0f);
        Assert.True(result.ProgressPercent < 100f);
    }

    [Fact]
    public void AdvanceGenericRecipe_CompletesBatch_WhenThresholdReached()
    {
        ActivityRegistry.ResetForTesting();
        var result = CraftingProgressionService.AdvanceGenericRecipe("recipe_huiqi_dan", 0f, 200);

        Assert.True(result.CompletedBatch);
        Assert.Equal(100f, result.ProgressPercent);
    }

    [Fact]
    public void AdvanceGenericRecipe_ReturnsZero_ForUnknownRecipe()
    {
        ActivityRegistry.ResetForTesting();
        var result = CraftingProgressionService.AdvanceGenericRecipe("nonexistent", 0f, 100);

        Assert.False(result.CompletedBatch);
        Assert.Equal(0f, result.ProgressPercent);
    }

    [Fact]
    public void AdvanceGenericRecipe_ReturnsZero_WhenNoInputEvents()
    {
        ActivityRegistry.ResetForTesting();
        var result = CraftingProgressionService.AdvanceGenericRecipe("recipe_huiqi_dan", 50f, 0);

        Assert.False(result.CompletedBatch);
        Assert.Equal(0f, result.ProgressPercent);
    }
}
