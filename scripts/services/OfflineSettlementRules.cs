using System;

namespace Xiuxian.Scripts.Services
{
    public static class OfflineSettlementRules
    {
        public const double MaxOfflineSeconds = 24.0 * 60.0 * 60.0;

        public enum OfflineTimeGuardMode
        {
            Normal,
            Invalid,
        }

        public readonly record struct OfflineTimeEvaluation(
            double RawOfflineSeconds,
            double EffectiveOfflineSeconds,
            OfflineTimeGuardMode GuardMode);

        public static OfflineTimeEvaluation EvaluateOfflineSeconds(double offlineSeconds)
        {
            if (!double.IsFinite(offlineSeconds) || offlineSeconds <= 0.0)
            {
                return new OfflineTimeEvaluation(offlineSeconds, 0.0, OfflineTimeGuardMode.Invalid);
            }

            // A long absence alone is not evidence of clock manipulation.
            return new OfflineTimeEvaluation(offlineSeconds, ClampOfflineSeconds(offlineSeconds), OfflineTimeGuardMode.Normal);
        }

        public static double ClampOfflineSeconds(double offlineSeconds)
        {
            return double.IsFinite(offlineSeconds) ? Math.Clamp(offlineSeconds, 0.0, MaxOfflineSeconds) : 0.0;
        }

        public static double CalculateOfflineInputBudget(double offlineSeconds)
        {
            double remainingMinutes = EvaluateOfflineSeconds(offlineSeconds).EffectiveOfflineSeconds / 60.0;
            double totalInputs = 0.0;

            totalInputs += ConsumeSegment(ref remainingMinutes, 30.0, 12.0);
            totalInputs += ConsumeSegment(ref remainingMinutes, 210.0, 8.0);
            totalInputs += ConsumeSegment(ref remainingMinutes, 240.0, 6.0);
            totalInputs += ConsumeSegment(ref remainingMinutes, 960.0, 3.0);

            return totalInputs;
        }

        public static ActionSettlementResult BuildCultivationOfflineSettlement(
            double offlineSeconds,
            double apPerInput,
            double lingqiFactor,
            double insightFactor,
            double baseExpPerInput,
            double realmMultiplier,
            string actionTargetId = "")
        {
            OfflineTimeEvaluation evaluated = EvaluateOfflineSeconds(offlineSeconds);
            double inputBudget = CalculateOfflineInputBudget(evaluated.EffectiveOfflineSeconds);
            double offlineAp = inputBudget * apPerInput;

            double lingqiGain = offlineAp * lingqiFactor * realmMultiplier;
            double insightGain = offlineAp * insightFactor;
            // Offline gets the common input rate, without the online cultivation bonus.
            double realmExpGain = inputBudget * Math.Max(0.0, baseExpPerInput);

            return ActionSettlementRules.BuildCultivationSettlement(
                actionTargetId,
                offlineAp,
                lingqiGain,
                insightGain,
                realmExpGain);
        }

        private static double ConsumeSegment(ref double remainingMinutes, double segmentMinutes, double inputRatePerMinute)
        {
            if (remainingMinutes <= 0.0)
            {
                return 0.0;
            }

            double minutes = Math.Min(remainingMinutes, segmentMinutes);
            remainingMinutes -= minutes;
            return minutes * inputRatePerMinute;
        }
    }
}
