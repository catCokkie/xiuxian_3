using System;
using System.Collections.Generic;
using Xiuxian.Scripts.Game;

namespace Xiuxian.Scripts.Services
{
    public readonly record struct PreparationMaterial(string ItemId, int Required, int Owned)
    {
        public int Missing => Math.Max(0, Required - Owned);
    }
    public sealed record PreparationRecommendation(string RecipeId, string OutputId, int Target, int Owned, int PerBatch,
        int Rounds, double LingqiCost, IReadOnlyList<PreparationMaterial> Materials)
    {
        public int Missing => Math.Max(0, Target - Owned);
    }
    public static class ChallengePreparationRules
    {
        public static int? EstimateBaselineRounds(int attack, MonsterStatProfile boss)
        {
            int damage = BattleRules.CalculateAttackDamage(attack, boss.BaseStats.Defense);
            if (damage >= boss.BaseStats.MaxHp) return 1;
            int net = damage - Math.Max(0, boss.HealPerRound);
            return net <= 0 ? null : Math.Max(1, (int)Math.Ceiling((boss.BaseStats.MaxHp - boss.HealPerRound) / (double)net));
        }
        public static string ChooseTalisman(string strategyTag, int masteryLevel) => strategyTag switch
        {
            "armor" => "talisman_fire_charm",
            "fast" when masteryLevel >= 2 => "talisman_shield_charm",
            _ => "",
        };
        public static PreparationRecommendation? Build(string recipeId, int target, int owned, int masteryLevel,
            int yieldMultiplier, int extraOutput, IReadOnlyDictionary<string, int> inventory)
        {
            var recipe = ActivityRegistry.GetRecipe(recipeId);
            if (recipe == null || recipe.Outputs.Count == 0 || masteryLevel < recipe.RequiredMasteryLevel) return null;
            int baseOutput = recipe.SystemId == "alchemy" ? AlchemyRules.CompleteRecipe(recipeId, masteryLevel).PotionCount : recipe.Outputs[0].Count;
            int output = Math.Max(1, baseOutput * Math.Max(1, yieldMultiplier) + Math.Max(0, extraOutput));
            int desired = Math.Clamp(target, 1, 99), have = Math.Max(0, owned);
            int rounds = (int)Math.Ceiling(Math.Max(0, desired - have) / (double)output);
            var materials = new List<PreparationMaterial>();
            foreach (var input in CraftingProgressionService.GetEffectiveInputs(recipe, masteryLevel))
                materials.Add(new(input.ItemId, input.Count * rounds, inventory.TryGetValue(input.ItemId, out int count) ? Math.Max(0, count) : 0));
            return new(recipeId, recipe.Outputs[0].ItemId, desired, have, output, rounds, recipe.LingqiCost * rounds, materials);
        }
    }
}
