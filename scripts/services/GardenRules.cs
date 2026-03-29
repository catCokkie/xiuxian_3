using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class GardenRules
    {
        public readonly record struct CropSpec(
            string RecipeId,
            string DisplayName,
            string OutputItemId,
            int OutputCount,
            int RequiredInputs,
            int RequiredMasteryLevel = 1);

        public readonly record struct GardenProgressDecision(float NextProgress, bool CompletedBatch);
        public readonly record struct GatherResult(string ItemId, int ItemCount);

        private static readonly CropSpec[] Crops =
        {
            new("garden_spirit_herb", "种植灵草", "spirit_herb", 2, 200, 1),
            new("garden_spirit_flower", "种植灵花", "spirit_flower", 2, 240, 2),
            new("garden_spirit_fruit", "种植灵果", "spirit_fruit", 2, 280, 4),
        };

        public static IReadOnlyList<CropSpec> GetCrops() => Crops;

        public static bool TryGetCrop(string recipeId, out CropSpec crop)
        {
            for (int i = 0; i < Crops.Length; i++)
            {
                if (Crops[i].RecipeId == recipeId)
                {
                    crop = Crops[i];
                    return true;
                }
            }

            crop = default;
            return false;
        }

        public static bool CanPlantCrop(string recipeId, int masteryLevel = 1)
        {
            return TryGetCrop(recipeId, out CropSpec crop) && masteryLevel >= crop.RequiredMasteryLevel;
        }

        public static GardenProgressDecision AdvanceProgress(float currentProgress, int inputEvents, int requiredInputs, int masteryLevel = 1)
        {
            double speedBonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeGarden, masteryLevel, SubsystemMasteryRules.GardenGrowthSpeedBonusEffectId);
            float effectiveThreshold = (float)(Math.Max(1, requiredInputs) * (1.0 - speedBonus));
            float next = Math.Max(0.0f, currentProgress) + Math.Max(0, inputEvents);
            bool completed = next >= effectiveThreshold;
            return new GardenProgressDecision(completed ? 0.0f : next, completed);
        }

        public static GatherResult HarvestCrop(string recipeId, int masteryLevel = 1)
        {
            if (!TryGetCrop(recipeId, out CropSpec crop))
            {
                return default;
            }

            int bonusYield = masteryLevel >= 3 ? 1 : 0;
            double rareBonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeGarden, masteryLevel, SubsystemMasteryRules.GardenRareSpawnBonusEffectId);
            int rareExtra = rareBonus > 0.0 && crop.RequiredMasteryLevel >= 2 ? 1 : 0;
            return new GatherResult(crop.OutputItemId, crop.OutputCount + bonusYield + rareExtra);
        }

        public static bool IsOfflineFullySupported(int masteryLevel)
        {
            return SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeGarden, masteryLevel, SubsystemMasteryRules.GardenOfflineFullEffectId) >= 1.0;
        }
    }
}
