using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Central registry for all activity systems (alchemy, smithing, garden, mining, etc.).
    /// Provides cross-system lookup by SystemId or RecipeId.
    /// </summary>
    public static class ActivityRegistry
    {
        private static readonly Dictionary<string, IActivityDefinition> Activities = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, IRecipeDefinition> RecipeIndex = new(StringComparer.Ordinal);
        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            RegisterBuiltInActivities();
        }

        public static void Register(IActivityDefinition activity)
        {
            Activities[activity.SystemId] = activity;
            foreach (IRecipeDefinition recipe in activity.GetRecipes())
            {
                RecipeIndex[recipe.RecipeId] = recipe;
            }
        }

        public static IActivityDefinition? GetBySystem(string systemId)
        {
            EnsureInitialized();
            return Activities.TryGetValue(systemId, out IActivityDefinition? def) ? def : null;
        }

        public static IRecipeDefinition? GetRecipe(string recipeId)
        {
            EnsureInitialized();
            return RecipeIndex.TryGetValue(recipeId, out IRecipeDefinition? def) ? def : null;
        }

        public static IReadOnlyDictionary<string, IActivityDefinition> GetAll()
        {
            EnsureInitialized();
            return Activities;
        }

        /// <summary>Reset state — intended for testing only.</summary>
        public static void ResetForTesting()
        {
            Activities.Clear();
            RecipeIndex.Clear();
            _initialized = false;
        }

        private static void RegisterBuiltInActivities()
        {
            RegisterAlchemy();
            RegisterSmithing();
        }

        private static void RegisterAlchemy()
        {
            var alchemy = new SimpleActivityDefinition
            {
                SystemId = PlayerActionState.ModeAlchemy,
                DisplayName = "炼丹",
                Category = ActivityCategory.Processing,
                SupportsOffline = true,
                OfflineEfficiency = 0.5,
            };

            foreach (AlchemyRules.RecipeSpec spec in AlchemyRules.GetRecipes())
            {
                alchemy.AddRecipe(new SimpleRecipeDefinition
                {
                    RecipeId = spec.RecipeId,
                    SystemId = PlayerActionState.ModeAlchemy,
                    DisplayName = spec.DisplayName,
                    Inputs = new[] { new MaterialCost(spec.MaterialItemId, spec.MaterialCount) },
                    LingqiCost = spec.LingqiCost,
                    RequiredInputEvents = spec.RequiredInputs,
                    Outputs = new[] { new MaterialOutput(spec.OutputPotionId, spec.OutputCount) },
                });
            }

            Register(alchemy);
        }

        private static void RegisterSmithing()
        {
            // Smithing is level-based (not fixed-recipe). We register a single placeholder
            // recipe representing the base enhancement tier so the system is discoverable.
            var smithing = new SimpleActivityDefinition
            {
                SystemId = PlayerActionState.ModeSmithing,
                DisplayName = "炼器",
                Category = ActivityCategory.Processing,
                SupportsOffline = true,
                OfflineEfficiency = 0.5,
            };

            SmithingCost baseCost = SmithingRules.GetCost(0);
            smithing.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "smithing_enhance_base",
                SystemId = PlayerActionState.ModeSmithing,
                DisplayName = "基础强化",
                Inputs = new[]
                {
                    new MaterialCost("lingqi_shard", baseCost.Shards),
                },
                LingqiCost = baseCost.Lingqi,
                RequiredInputEvents = baseCost.RequiredInputs,
                Outputs = System.Array.Empty<MaterialOutput>(),
            });

            Register(smithing);
        }
    }
}
