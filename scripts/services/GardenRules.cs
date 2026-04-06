using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class GardenRules
    {
        public const int BasePlotCount = 2;
        public const int MaxPlotCount = 6;
        public const int BaseRareSeedChancePercent = 5;
        public const int EnhancedRareSeedChancePercent = 10;

        public readonly record struct CropSpec(
            string RecipeId,
            string DisplayName,
            string SeedItemId,
            string OutputItemId,
            int MinOutputCount,
            int MaxOutputCount,
            int GrowthSeconds,
            int RequiredMasteryLevel = 1)
        {
            // Compatibility aliases used by a few existing registry/tests.
            public int OutputCount => MaxOutputCount;
            public int RequiredInputs => GrowthSeconds;
        }

        public readonly record struct GardenProgressDecision(float NextProgress, bool CompletedBatch);
        public readonly record struct GatherResult(string ItemId, int ItemCount);
        public readonly record struct HarvestResult(string ItemId, int ItemCount, string BonusSeedItemId, int BonusSeedCount);

        private static readonly CropSpec[] Crops =
        {
            new("garden_spirit_herb", "种植灵草", "seed_spirit_herb", "spirit_herb", 2, 3, 30 * 60, 1),
            new("garden_spirit_flower", "种植灵花", "seed_spirit_flower", "spirit_flower", 1, 2, 2 * 60 * 60, 2),
            new("garden_spirit_fruit", "种植灵果", "seed_spirit_fruit", "spirit_fruit", 1, 1, 8 * 60 * 60, 4),
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

        public static int GetUnlockedPlotCount(int purchasedExpansionCount = 0)
        {
            return Math.Clamp(BasePlotCount + Math.Max(0, purchasedExpansionCount), BasePlotCount, MaxPlotCount);
        }

        public static int GetEffectiveGrowthSeconds(string recipeId, int masteryLevel = 1)
        {
            if (!TryGetCrop(recipeId, out CropSpec crop))
            {
                return 0;
            }

            double speedBonus = SubsystemMasteryRules.GetEffectValue(
                PlayerActionState.ModeGarden,
                masteryLevel,
                SubsystemMasteryRules.GardenGrowthSpeedBonusEffectId);
            return Math.Max(1, (int)Math.Ceiling(crop.GrowthSeconds * (1.0 - speedBonus)));
        }

        public static int GetRareSeedChancePercent(int masteryLevel = 1)
        {
            double rareBonus = SubsystemMasteryRules.GetEffectValue(
                PlayerActionState.ModeGarden,
                masteryLevel,
                SubsystemMasteryRules.GardenRareSpawnBonusEffectId);
            return rareBonus > 0.0 ? EnhancedRareSeedChancePercent : BaseRareSeedChancePercent;
        }

        public static HarvestResult ResolveHarvest(string recipeId, int masteryLevel, int yieldRollPercent, int seedRollPercent)
        {
            if (!TryGetCrop(recipeId, out CropSpec crop))
            {
                return default;
            }

            int range = Math.Max(0, crop.MaxOutputCount - crop.MinOutputCount);
            int baseYield = crop.MinOutputCount;
            if (range > 0)
            {
                baseYield += Math.Abs(yieldRollPercent % (range + 1));
            }

            int masteryBonus = masteryLevel >= 3 ? 1 : 0;
            int rareSeedCount = Math.Abs(seedRollPercent % 100) < GetRareSeedChancePercent(masteryLevel) ? 1 : 0;
            return new HarvestResult(crop.OutputItemId, baseYield + masteryBonus, crop.SeedItemId, rareSeedCount);
        }

        // Kept for compatibility with older tests/callers; the real-time garden now uses ResolveHarvest.
        public static GatherResult HarvestCrop(string recipeId, int masteryLevel = 1)
        {
            HarvestResult harvest = ResolveHarvest(recipeId, masteryLevel, yieldRollPercent: 0, seedRollPercent: 99);
            return new GatherResult(harvest.ItemId, harvest.ItemCount);
        }

        // Kept for compatibility with older tests; the real-time garden no longer uses input-driven progress.
        public static GardenProgressDecision AdvanceProgress(float currentProgress, int inputEvents, int requiredInputs, int masteryLevel = 1)
        {
            double speedBonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeGarden, masteryLevel, SubsystemMasteryRules.GardenGrowthSpeedBonusEffectId);
            float effectiveThreshold = (float)(Math.Max(1, requiredInputs) * (1.0 - speedBonus));
            float next = Math.Max(0.0f, currentProgress) + Math.Max(0, inputEvents);
            bool completed = next >= effectiveThreshold;
            return new GardenProgressDecision(completed ? 0.0f : next, completed);
        }

        public static bool IsOfflineFullySupported(int masteryLevel)
        {
            return true;
        }
    }
}
