using System;

namespace Xiuxian.Scripts.Services
{
    public static class InsightSpendRules
    {
        public const double BossWeaknessMinCost = 30.0;
        public const double BossWeaknessMaxCost = 80.0;
        public const double AdvancedAlchemyStudyInsightCost = 20.0;
        public const double BossWeaknessReductionFactor = 0.10;

        public static double GetBossWeaknessInsightCost(int zoneIndex)
        {
            // Zone 0→30, 1→42, 2→54, 3→66, 4→78 (within 30-80 range)
            return Math.Clamp(BossWeaknessMinCost + zoneIndex * 12.0, BossWeaknessMinCost, BossWeaknessMaxCost);
        }

        public static bool CanAfford(double currentInsight, double cost)
        {
            return currentInsight >= Math.Max(0.0, cost);
        }

        public static double ApplyBossWeaknessMultiplier(double value)
        {
            return value * (1.0 - BossWeaknessReductionFactor);
        }

        public static bool CanUnlockAdvancedAlchemy(bool alreadyUnlocked, double currentInsight)
        {
            return !alreadyUnlocked && CanAfford(currentInsight, AdvancedAlchemyStudyInsightCost);
        }
    }
}
