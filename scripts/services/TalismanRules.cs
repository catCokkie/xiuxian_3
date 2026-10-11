using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class TalismanRules
    {
        public readonly record struct BattleEffect(double DefenseReduction = 0, double RoundAcceleration = 0, bool ForceFirst = false, double FirstHitHpRate = 0);
        public static BattleEffect GetBattleEffect(string id, string strategyTag = "") => id switch
        {
            "talisman_fire_charm" => new(DefenseReduction: strategyTag == "armor" ? 0.60 : 0.30),
            "talisman_shield_charm" => new(RoundAcceleration: strategyTag == "fast" ? 0.50 : 0.25, ForceFirst: true),
            "talisman_burst_charm" => new(FirstHitHpRate: 0.15),
            _ => default
        };
        public readonly record struct RecipeSpec(
            string RecipeId,
            string DisplayName,
            MaterialCost[] Inputs,
            double LingqiCost,
            int RequiredInputEvents,
            int RequiredMasteryLevel,
            CharacterStatModifier Modifier);

        public const double ArmorBreakCharmAttackRate = 0.15;
        public const double SwiftCharmAttackRate = 0.10;
        public const double SwiftCharmSpeedRate = 0.25;
        public const double BurstCharmAttackRate = 0.35;

        private static readonly RecipeSpec[] Recipes =
        {
            new(
                "talisman_fire_charm",
                "破甲符",
                new[] { new MaterialCost("broken_talisman", 2), new MaterialCost("spirit_ink", 1) },
                45.0,
                180,
                1,
                default),
            new(
                "talisman_shield_charm",
                "疾风符",
                new[] { new MaterialCost("broken_talisman", 3), new MaterialCost("spirit_ink", 1) },
                60.0,
                220,
                2,
                default),
            new(
                "talisman_burst_charm",
                "炸裂符",
                new[] { new MaterialCost("broken_talisman", 4), new MaterialCost("spirit_ink", 2) },
                90.0,
                280,
                4,
                default),
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

        public static CharacterStatModifier GetModifier(string recipeId)
        {
            return TryGetRecipe(recipeId, out RecipeSpec recipe) ? recipe.Modifier : default;
        }

        public static double GetMaterialDiscount(int masteryLevel)
        {
            return SubsystemMasteryRules.GetEffectValue(
                PlayerActionState.ModeTalisman,
                masteryLevel,
                SubsystemMasteryRules.TalismanMaterialDiscountEffectId);
        }

        public static int GetMaxTalismansPerBattle(int masteryLevel)
        {
            return masteryLevel >= 4 ? 2 : 1;
        }
    }
}
