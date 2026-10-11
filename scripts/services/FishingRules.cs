using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class FishingRules
    {
        public readonly record struct PondSpec(
            string RecipeId,
            string DisplayName,
            string OutputItemId,
            int OutputCount,
            int RequiredInputs,
            int RequiredMasteryLevel,
            bool IsRarePond);

        public readonly record struct FishingProgressDecision(float NextProgress, bool CompletedBatch);
        public readonly record struct CatchResult(string ItemId, int ItemCount);
        public readonly record struct RareCatchResult(string ItemId, int ItemCount);

        private static readonly PondSpec[] Ponds =
        {
            new("fishing_spirit_fish", "灵泉浅滩", "spirit_fish", 1, 120, 1, false),
            new("fishing_spirit_pearl", "幽潭", "cold_scale_fish", 1, 150, 2, false),
            new("fishing_deep_pond", "龙涎渊", "deep_abyss_fish", 1, 180, 3, true),
            new("fishing_hualong_pond", "化龙泊", "armored_fish", 1, 200, 4, true),
        };

        public static IReadOnlyList<PondSpec> GetPonds() => Ponds;

        public static bool TryGetPond(string recipeId, out PondSpec pond)
        {
            for (int i = 0; i < Ponds.Length; i++)
            {
                if (Ponds[i].RecipeId == recipeId)
                {
                    pond = Ponds[i];
                    return true;
                }
            }

            pond = default;
            return false;
        }

        public static bool CanFishPond(string recipeId, int masteryLevel = 1)
        {
            return TryGetPond(recipeId, out PondSpec pond) && masteryLevel >= pond.RequiredMasteryLevel;
        }

        public static FishingProgressDecision AdvanceProgress(float currentProgress, int inputEvents, int requiredInputs, int masteryLevel = 1)
        {
            double speedBonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeFishing, masteryLevel, SubsystemMasteryRules.FishingSpeedBonusEffectId);
            float next = BatchProgressRules.Advance(currentProgress, inputEvents, requiredInputs, 1.0 / (1 - speedBonus));
            bool completed = next >= Math.Max(1, requiredInputs);
            return new FishingProgressDecision(next, completed);
        }

        public static CatchResult CatchFish(string recipeId, int masteryLevel = 1, int rollPercent = 0)
        {
            if (!TryGetPond(recipeId, out var pond) || masteryLevel < pond.RequiredMasteryLevel) return default;
            int roll = Math.Abs(rollPercent % 100);
            string output = recipeId switch
            {
                "fishing_spirit_fish" => roll < 80 ? "spirit_fish" : "spirit_pearl",
                "fishing_spirit_pearl" => roll < 60 ? "cold_scale_fish" : roll < 90 ? "spirit_pearl" : "spirit_fish",
                "fishing_deep_pond" => roll < 50 ? "deep_abyss_fish" : roll < 80 ? "dragon_saliva" : "spirit_pearl",
                "fishing_hualong_pond" => roll < 40 ? "armored_fish" : roll < 75 ? "dragon_saliva" : "spirit_pearl",
                _ => pond.OutputItemId
            };
            return new(output, 1);
        }
        public static double GetDoubleOutputChance(int masteryLevel) => 0; // Lv4 boosts exclusive rare drops instead.
        public static int Capacity(string id, int mastery = 1)
            => (id switch { "fishing_spirit_pearl" => 25, "fishing_deep_pond" => 20, "fishing_hualong_pond" => 15, _ => 30 }) + (mastery >= 4 ? 10 : 0);

        public static bool TryRollRareCatch(string recipeId, int rollPercent, bool baitActive, out RareCatchResult rareCatch, int masteryLevel = 1)
        {
            rareCatch = default;
            int normalizedRoll = Math.Abs(rollPercent % 100);
            int threshold = (masteryLevel >= 4 ? 10 : 5) + (baitActive ? 5 : 0);
            if (normalizedRoll >= threshold)
            {
                return false;
            }

            rareCatch = recipeId switch
            {
                "fishing_spirit_fish" => new RareCatchResult("spirit_spring_water", 1),
                "fishing_spirit_pearl" => new RareCatchResult("sunk_jade_box", 1),
                "fishing_deep_pond" => new RareCatchResult("water_spirit_amber", 1),
                "fishing_hualong_pond" => new RareCatchResult("sunken_equipment", 1),
                _ => default,
            };
            return !string.IsNullOrEmpty(rareCatch.ItemId) && rareCatch.ItemCount > 0;
        }
    }
}
