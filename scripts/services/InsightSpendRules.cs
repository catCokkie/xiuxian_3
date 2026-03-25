using System;

namespace Xiuxian.Scripts.Services
{
    public static class InsightSpendRules
    {
        public const double BossWeaknessInsightCost = 50.0;
        public const double AdvancedAlchemyStudyInsightCost = 20.0;
        public const double BossWeaknessReductionFactor = 0.10;

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
