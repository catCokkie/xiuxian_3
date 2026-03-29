namespace Xiuxian.Scripts.Services
{
    public static class EnlightenmentRules
    {
        public const int MeditationMaxCount = 20;
        public const int ContemplationMaxCount = 10;
        public const double MeditationInsightRate = 0.02;
        public const double ContemplationInsightRate = 0.05;

        public static int GetEffectiveMeditationCap(int masteryLevel)
        {
            double bonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeEnlightenment, masteryLevel, SubsystemMasteryRules.EnlightenmentMeditationCapBonusEffectId);
            return MeditationMaxCount + (int)bonus;
        }

        public static int GetEffectiveContemplationCap(int masteryLevel)
        {
            double bonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeEnlightenment, masteryLevel, SubsystemMasteryRules.EnlightenmentContemplationCapBonusEffectId);
            return ContemplationMaxCount + (int)bonus;
        }

        public static double GetEfficiencyMultiplier(int masteryLevel)
        {
            double bonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeEnlightenment, masteryLevel, SubsystemMasteryRules.EnlightenmentEfficiencyBonusEffectId);
            return 1.0 + bonus;
        }

        public static bool CanApply(string recipeId, int currentCount, int masteryLevel = 1)
        {
            return recipeId switch
            {
                "enlightenment_meditation" => currentCount < GetEffectiveMeditationCap(masteryLevel),
                "enlightenment_contemplation" => currentCount < GetEffectiveContemplationCap(masteryLevel),
                _ => false,
            };
        }

        public static double GetInsightRateGain(string recipeId, int masteryLevel = 1)
        {
            double baseRate = recipeId switch
            {
                "enlightenment_meditation" => MeditationInsightRate,
                "enlightenment_contemplation" => ContemplationInsightRate,
                _ => 0.0,
            };
            return baseRate * GetEfficiencyMultiplier(masteryLevel);
        }
    }
}
