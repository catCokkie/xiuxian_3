using System;

namespace Xiuxian.Scripts.Services
{
    public static class ResourceConversionRules
    {
        public readonly record struct Result(double Lingqi, double Insight, double RealmExp);

        public static Result Calculate(string actionId, double ap, int inputs, double activeSeconds,
            double realmMultiplier, int cultivationMastery, double formationLingqiRate,
            double restMultiplier = 1.0, bool inputExpEnabled = true,
            double lingqiFactor = 0.9, double insightFactor = 0.08,
            double expFromLingqi = 0.25, double expPerInput = 0.35)
        {
            double cultivationBonus = actionId == PlayerActionState.ActionCultivation
                ? SubsystemMasteryRules.GetEffectValue(actionId, cultivationMastery, SubsystemMasteryRules.CultivationLingqiBonusEffectId) : 0.0;
            double lingqi = Math.Max(0, ap) * lingqiFactor * realmMultiplier * (1.0 + cultivationBonus) * (1.0 + formationLingqiRate);
            // Rest affects only passive recovery; reading/working without repeated clicks keeps a short active window.
            lingqi += Math.Max(0, activeSeconds) * GameBalanceConstants.ResourceConversion.PassiveLingqiPerMinute / 60.0 * Math.Max(1.0, restMultiplier);
            bool directExp = inputExpEnabled && PlayerActionCapabilityRules.HasCapability(actionId, PlayerActionCapability.GrantsCultivationInputExp);
            return new Result(lingqi, Math.Max(0, ap) * insightFactor,
                directExp ? Math.Max(0, inputs) * expPerInput : lingqi * expFromLingqi);
        }
    }
}
