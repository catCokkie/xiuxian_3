namespace Xiuxian.Scripts.Services
{
    public static class InsightSpendRules
    {
        public const double AdvancedAlchemyStudyInsightCost = 20.0;

        public static bool CanAfford(double currentInsight, double cost)
        {
            return currentInsight >= System.Math.Max(0.0, cost);
        }

        public static bool CanUnlockAdvancedAlchemy(bool alreadyUnlocked, double currentInsight)
        {
            return !alreadyUnlocked && CanAfford(currentInsight, AdvancedAlchemyStudyInsightCost);
        }

        public static bool CanUnlockMastery(string systemId, int currentLevel, double currentInsight, int currentRealmLevel)
        {
            return SubsystemMasteryRules.CanUnlock(systemId, currentLevel, currentInsight, currentRealmLevel);
        }

        public static double GetMasteryCost(string systemId, int currentLevel)
        {
            return SubsystemMasteryRules.GetUnlockCost(systemId, currentLevel);
        }

        public static bool SpendInsightForMastery(string systemId, int currentLevel, double currentInsight, int currentRealmLevel, out int nextLevel, out double remainingInsight)
        {
            return SubsystemMasteryRules.TryUnlock(systemId, currentLevel, currentInsight, currentRealmLevel, out nextLevel, out remainingInsight);
        }
    }
}
