using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ActivityRegistryTests
{
    [Fact]
    public void Registry_ContainsAlchemySystem()
    {
        ActivityRegistry.ResetForTesting();
        IActivityDefinition? alchemy = ActivityRegistry.GetBySystem(PlayerActionState.ModeAlchemy);

        Assert.NotNull(alchemy);
        Assert.Equal("炼丹", alchemy!.DisplayName);
        Assert.Equal(ActivityCategory.Processing, alchemy.Category);
    }

    [Fact]
    public void Registry_ContainsSmithingSystem()
    {
        ActivityRegistry.ResetForTesting();
        IActivityDefinition? smithing = ActivityRegistry.GetBySystem(PlayerActionState.ModeSmithing);

        Assert.NotNull(smithing);
        Assert.Equal("炼器", smithing!.DisplayName);
        Assert.Equal(ActivityCategory.Processing, smithing.Category);
    }

    [Fact]
    public void Registry_AlchemyRecipesMatchAlchemyRules()
    {
        ActivityRegistry.ResetForTesting();
        IActivityDefinition? alchemy = ActivityRegistry.GetBySystem(PlayerActionState.ModeAlchemy);
        Assert.NotNull(alchemy);

        IReadOnlyList<IRecipeDefinition> recipes = alchemy!.GetRecipes();
        IReadOnlyList<AlchemyRules.RecipeSpec> originalRecipes = AlchemyRules.GetRecipes();
        Assert.Equal(originalRecipes.Count, recipes.Count);

        for (int i = 0; i < originalRecipes.Count; i++)
        {
            Assert.Equal(originalRecipes[i].RecipeId, recipes[i].RecipeId);
            Assert.Equal(originalRecipes[i].DisplayName, recipes[i].DisplayName);
            Assert.Equal(originalRecipes[i].LingqiCost, recipes[i].LingqiCost);
            Assert.Equal(originalRecipes[i].RequiredInputs, recipes[i].RequiredInputEvents);
        }
    }

    [Fact]
    public void GetRecipe_FindsAlchemyRecipeById()
    {
        ActivityRegistry.ResetForTesting();
        IRecipeDefinition? recipe = ActivityRegistry.GetRecipe("recipe_huiqi_dan");

        Assert.NotNull(recipe);
        Assert.Equal("回气丹", recipe!.DisplayName);
        Assert.Equal(PlayerActionState.ModeAlchemy, recipe.SystemId);
    }

    [Fact]
    public void GetRecipe_FindsSmithingBaseRecipe()
    {
        ActivityRegistry.ResetForTesting();
        IRecipeDefinition? recipe = ActivityRegistry.GetRecipe("smithing_enhance_base");

        Assert.NotNull(recipe);
        Assert.Equal(PlayerActionState.ModeSmithing, recipe!.SystemId);
    }

    [Fact]
    public void GetAll_Returns2BuiltInSystems()
    {
        ActivityRegistry.ResetForTesting();
        IReadOnlyDictionary<string, IActivityDefinition> all = ActivityRegistry.GetAll();

        Assert.Equal(2, all.Count);
        Assert.True(all.ContainsKey(PlayerActionState.ModeAlchemy));
        Assert.True(all.ContainsKey(PlayerActionState.ModeSmithing));
    }

    [Fact]
    public void Register_CustomActivity_IsDiscoverable()
    {
        ActivityRegistry.ResetForTesting();

        var garden = new SimpleActivityDefinition
        {
            SystemId = PlayerActionState.ModeGarden,
            DisplayName = "灵田",
            Category = ActivityCategory.Gathering,
            SupportsOffline = true,
            OfflineEfficiency = 0.8,
        };
        garden.AddRecipe(new SimpleRecipeDefinition
        {
            RecipeId = "garden_spirit_herb",
            SystemId = PlayerActionState.ModeGarden,
            DisplayName = "种植灵草",
            RequiredInputEvents = 300,
            Outputs = new[] { new MaterialOutput("spirit_herb", 3) },
        });

        ActivityRegistry.Register(garden);

        Assert.NotNull(ActivityRegistry.GetBySystem(PlayerActionState.ModeGarden));
        Assert.NotNull(ActivityRegistry.GetRecipe("garden_spirit_herb"));
        Assert.Equal(3, ActivityRegistry.GetAll().Count);
    }

    [Fact]
    public void GetRecipe_ReturnsNull_ForUnknownId()
    {
        ActivityRegistry.ResetForTesting();
        Assert.Null(ActivityRegistry.GetRecipe("nonexistent_recipe"));
    }

    [Fact]
    public void GetBySystem_ReturnsNull_ForUnknownSystem()
    {
        ActivityRegistry.ResetForTesting();
        Assert.Null(ActivityRegistry.GetBySystem("nonexistent_system"));
    }
}
