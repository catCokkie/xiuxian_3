namespace Xiuxian.Scripts.Services
{
    public static class EnlightenmentRules
    {
        public const int MeditationMaxCount = 20;
        public const int ContemplationMaxCount = 10;
        public const double MeditationInsightRate = 0.02;
        public const double ContemplationInsightRate = 0.05;

        public static bool CanApply(string recipeId, int currentCount)
        {
            return recipeId switch
            {
                "enlightenment_meditation" => currentCount < MeditationMaxCount,
                "enlightenment_contemplation" => currentCount < ContemplationMaxCount,
                _ => false,
            };
        }

        public static double GetInsightRateGain(string recipeId)
        {
            return recipeId switch
            {
                "enlightenment_meditation" => MeditationInsightRate,
                "enlightenment_contemplation" => ContemplationInsightRate,
                _ => 0.0,
            };
        }
    }
}
