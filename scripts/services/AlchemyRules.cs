using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class AlchemyRules
    {
        public readonly record struct RecipeSpec(
            string RecipeId,
            string DisplayName,
            string MaterialItemId,
            int MaterialCount,
            double LingqiCost,
            string OutputPotionId,
            int OutputCount,
            int RequiredInputs,
            double InsightCost = 0.0,
            bool RequiresAdvancedStudy = false);

        public readonly record struct AlchemyProgressDecision(float NextProgress, bool CompletedBatch);

        public readonly record struct AlchemyCompletionResult(
            string PotionItemId,
            int PotionCount,
            string MaterialItemId,
            int MaterialCount,
            double LingqiCost,
            double InsightCost);

        private static readonly RecipeSpec[] Recipes =
        {
            new("recipe_huiqi_dan", "回气丹", "spirit_herb", 2, 50.0, "potion_huiqi_dan", 2, 200),
            new("recipe_juling_san", "聚灵散", "spirit_herb", 3, 80.0, "potion_juling_san", 1, 220),
        };

        public static IReadOnlyList<RecipeSpec> GetRecipes() => Recipes;

        public static bool TryGetRecipe(string recipeId, out RecipeSpec recipe)
        {
            for (int i = 0; i < Recipes.Length; i++)
            {
                if (Recipes[i].RecipeId == recipeId)
                {
                    recipe = Recipes[i];
                    return true;
                }
            }

            recipe = default;
            return false;
        }

        public static bool CanStartRecipe(string recipeId, double lingqi, IReadOnlyDictionary<string, int> backpackItems, bool advancedStudyUnlocked = false)
        {
            if (!TryGetRecipe(recipeId, out RecipeSpec recipe))
            {
                return false;
            }

            if (recipe.RequiresAdvancedStudy && !advancedStudyUnlocked)
            {
                return false;
            }

            int itemCount = backpackItems != null && backpackItems.TryGetValue(recipe.MaterialItemId, out int count) ? count : 0;
            return lingqi >= recipe.LingqiCost && itemCount >= recipe.MaterialCount;
        }

        public static AlchemyProgressDecision AdvanceProgress(float currentProgress, int inputEvents, int requiredInputs)
        {
            float next = Math.Max(0.0f, currentProgress) + Math.Max(0, inputEvents);
            float threshold = Math.Max(1, requiredInputs);
            bool completed = next >= threshold;
            return new AlchemyProgressDecision(completed ? 0.0f : next, completed);
        }

        public static AlchemyCompletionResult CompleteRecipe(string recipeId)
        {
            if (!TryGetRecipe(recipeId, out RecipeSpec recipe))
            {
                return default;
            }

            return new AlchemyCompletionResult(
                recipe.OutputPotionId,
                recipe.OutputCount,
                recipe.MaterialItemId,
                recipe.MaterialCount,
                recipe.LingqiCost,
                recipe.InsightCost);
        }
    }
}
