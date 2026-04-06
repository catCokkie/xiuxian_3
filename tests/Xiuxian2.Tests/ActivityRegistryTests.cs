using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

[Collection("ActivityRegistry")]
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
    public void Registry_ContainsGardenMiningAndFishingSystems()
    {
        ActivityRegistry.ResetForTesting();

        Assert.Equal("灵田", ActivityRegistry.GetBySystem(PlayerActionState.ModeGarden)!.DisplayName);
        Assert.Equal("矿脉", ActivityRegistry.GetBySystem(PlayerActionState.ModeMining)!.DisplayName);
        Assert.Equal("灵渔", ActivityRegistry.GetBySystem(PlayerActionState.ModeFishing)!.DisplayName);
        Assert.Equal("spirit_flower", ActivityRegistry.GetRecipe("garden_spirit_flower")!.Outputs[0].ItemId);
        Assert.Equal("spirit_jade", ActivityRegistry.GetRecipe("mining_spirit_jade")!.Outputs[0].ItemId);
        Assert.Equal("dragon_saliva", ActivityRegistry.GetRecipe("fishing_deep_pond")!.Outputs[0].ItemId);
    }

    [Fact]
    public void Registry_ContainsRemainingPhaseSevenSystems()
    {
        ActivityRegistry.ResetForTesting();

        Assert.Equal("符箓", ActivityRegistry.GetBySystem(PlayerActionState.ModeTalisman)!.DisplayName);
        Assert.Equal("烹饪", ActivityRegistry.GetBySystem(PlayerActionState.ModeCooking)!.DisplayName);
        Assert.Equal("阵法", ActivityRegistry.GetBySystem(PlayerActionState.ModeFormation)!.DisplayName);
        Assert.Equal("体修", ActivityRegistry.GetBySystem(PlayerActionState.ModeBodyCultivation)!.DisplayName);
        Assert.Equal("talisman_fire_charm", ActivityRegistry.GetRecipe("talisman_fire_charm")!.Outputs[0].ItemId);
        Assert.Equal("food_spirit_porridge", ActivityRegistry.GetRecipe("cooking_spirit_porridge")!.Outputs[0].ItemId);
        Assert.Equal("formation_spirit_plate", ActivityRegistry.GetRecipe("formation_spirit_plate")!.Outputs[0].ItemId);
        Assert.Empty(ActivityRegistry.GetRecipe("body_cultivation_temper")!.Outputs);
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
    public void GetAll_Returns10BuiltInSystems()
    {
        ActivityRegistry.ResetForTesting();
        IReadOnlyDictionary<string, IActivityDefinition> all = ActivityRegistry.GetAll();

        Assert.Equal(9, all.Count);
        Assert.True(all.ContainsKey(PlayerActionState.ModeAlchemy));
        Assert.True(all.ContainsKey(PlayerActionState.ModeSmithing));
        Assert.True(all.ContainsKey(PlayerActionState.ModeGarden));
        Assert.True(all.ContainsKey(PlayerActionState.ModeMining));
        Assert.True(all.ContainsKey(PlayerActionState.ModeFishing));
        Assert.True(all.ContainsKey(PlayerActionState.ModeTalisman));
        Assert.True(all.ContainsKey(PlayerActionState.ModeCooking));
        Assert.True(all.ContainsKey(PlayerActionState.ModeFormation));
        Assert.True(all.ContainsKey(PlayerActionState.ModeBodyCultivation));
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
        Assert.Equal(9, ActivityRegistry.GetAll().Count);
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

    [Fact]
    public void Registry_SystemCategoriesAreCorrect()
    {
        ActivityRegistry.ResetForTesting();

        Assert.Equal(ActivityCategory.Gathering, ActivityRegistry.GetBySystem(PlayerActionState.ModeGarden)!.Category);
        Assert.Equal(ActivityCategory.Gathering, ActivityRegistry.GetBySystem(PlayerActionState.ModeMining)!.Category);
        Assert.Equal(ActivityCategory.Gathering, ActivityRegistry.GetBySystem(PlayerActionState.ModeFishing)!.Category);
        Assert.Equal(ActivityCategory.Processing, ActivityRegistry.GetBySystem(PlayerActionState.ModeTalisman)!.Category);
        Assert.Equal(ActivityCategory.Processing, ActivityRegistry.GetBySystem(PlayerActionState.ModeCooking)!.Category);
        Assert.Equal(ActivityCategory.Processing, ActivityRegistry.GetBySystem(PlayerActionState.ModeFormation)!.Category);
        Assert.Equal(ActivityCategory.Cultivation, ActivityRegistry.GetBySystem(PlayerActionState.ModeBodyCultivation)!.Category);
    }

    [Fact]
    public void Registry_EachSystemHasAtLeastOneRecipe()
    {
        ActivityRegistry.ResetForTesting();
        IReadOnlyDictionary<string, IActivityDefinition> all = ActivityRegistry.GetAll();

        foreach (KeyValuePair<string, IActivityDefinition> kvp in all)
        {
            Assert.True(kvp.Value.GetRecipes().Count > 0, $"System '{kvp.Key}' has no recipes");
        }
    }

    [Fact]
    public void Register_ThrowsOnNull()
    {
        ActivityRegistry.ResetForTesting();
        Assert.Throws<System.ArgumentNullException>(() => ActivityRegistry.Register(null!));
    }

    [Fact]
    public void GetBySystem_ReturnsNullForUnknownSystem()
    {
        ActivityRegistry.ResetForTesting();
        Assert.Null(ActivityRegistry.GetBySystem("nonexistent_system"));
    }

    [Fact]
    public void GetRecipe_ReturnsNullForUnknownRecipe()
    {
        ActivityRegistry.ResetForTesting();
        Assert.Null(ActivityRegistry.GetRecipe("nonexistent_recipe"));
    }
}
