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
            new("fishing_spirit_fish", "垂钓灵鱼", "spirit_fish", 2, 120, 1, false),
            new("fishing_spirit_pearl", "打捞灵珠", "spirit_pearl", 1, 180, 1, false),
            new("fishing_deep_pond", "深潭寻珍", "dragon_saliva", 1, 240, 2, true),
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
            float effectiveThreshold = (float)(Math.Max(1, requiredInputs) * (1.0 - speedBonus));
            float next = Math.Max(0.0f, currentProgress) + Math.Max(0, inputEvents);
            bool completed = next >= effectiveThreshold;
            return new FishingProgressDecision(completed ? 0.0f : next, completed);
        }

        public static CatchResult CatchFish(string recipeId, int masteryLevel = 1)
        {
            if (!TryGetPond(recipeId, out PondSpec pond))
            {
                return default;
            }

            int bonusYield = masteryLevel >= 4 ? 1 : 0;
            return new CatchResult(pond.OutputItemId, pond.OutputCount + bonusYield);
        }

        public static double GetDoubleOutputChance(int masteryLevel)
        {
            return SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeFishing, masteryLevel, SubsystemMasteryRules.FishingDoubleOutputEffectId);
        }

        public static bool TryRollRareCatch(string recipeId, int rollPercent, bool baitActive, out RareCatchResult rareCatch)
        {
            rareCatch = default;
            int normalizedRoll = Math.Abs(rollPercent % 100);
            int threshold = baitActive ? 10 : 5;
            if (normalizedRoll >= threshold)
            {
                return false;
            }

            rareCatch = recipeId switch
            {
                "fishing_spirit_fish" => new RareCatchResult("spirit_pearl", 1),
                "fishing_spirit_pearl" => new RareCatchResult("spirit_pearl", 2),
                "fishing_deep_pond" => new RareCatchResult("dragon_saliva", 1),
                _ => default,
            };
            return !string.IsNullOrEmpty(rareCatch.ItemId) && rareCatch.ItemCount > 0;
        }
    }
}
