using System;

namespace Xiuxian.Scripts.Services
{
    public static class AfkDetectionRules
    {
        public const double SlowdownThresholdSeconds = 60.0;
        public const double AfkThresholdSeconds = 120.0;

        public static bool IsAfk(double secondsSinceLastInput, double threshold = AfkThresholdSeconds)
        {
            return secondsSinceLastInput >= Math.Max(0.0, threshold);
        }

        public static double GetProgressMultiplier(double secondsSinceLastInput)
        {
            if (secondsSinceLastInput >= AfkThresholdSeconds)
            {
                return 0.0;
            }

            if (secondsSinceLastInput >= SlowdownThresholdSeconds)
            {
                return 0.5;
            }

            return 1.0;
        }
    }
}
