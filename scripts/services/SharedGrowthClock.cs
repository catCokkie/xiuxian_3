using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    // One time ledger across online/offline settlement. Real input is a separate bonus channel.
    public sealed class SharedGrowthClock
    {
        public const double CycleSeconds = OfflineSettlementRules.MaxOfflineSeconds;
        public double LastSettledUnix { get; private set; }
        public double CycleElapsedSeconds { get; private set; }
        public double InputsPerMinute => CycleElapsedSeconds < 1800 ? 12 : CycleElapsedSeconds < 14400 ? 8 : CycleElapsedSeconds < 28800 ? 6 : 3;

        public static double BudgetBetween(double phase, double seconds)
        {
            if (!double.IsFinite(seconds) || seconds <= 0) return 0;
            phase = NormalizePhase(phase);
            seconds = Math.Min(seconds, CycleSeconds);
            double first = Math.Min(seconds, CycleSeconds - phase);
            return OfflineSettlementRules.CalculateOfflineInputBudget(phase + first)
                - OfflineSettlementRules.CalculateOfflineInputBudget(phase)
                + OfflineSettlementRules.CalculateOfflineInputBudget(seconds - first);
        }

        public double Claim(double nowUnix, double legacySavedUnix = 0)
        {
            if (!double.IsFinite(nowUnix) || nowUnix <= 0) return 0;
            if (LastSettledUnix <= 0)
                LastSettledUnix = double.IsFinite(legacySavedUnix) && legacySavedUnix > 0 ? legacySavedUnix : nowUnix;
            if (nowUnix <= LastSettledUnix) return 0;
            double elapsed = nowUnix - LastSettledUnix;
            double budget = BudgetBetween(CycleElapsedSeconds, elapsed);
            CycleElapsedSeconds = (CycleElapsedSeconds + elapsed % CycleSeconds) % CycleSeconds;
            LastSettledUnix = nowUnix;
            return budget;
        }

        public Dictionary<string, object> ToPlainDictionary() => new()
        {
            ["last_settled_unix"] = LastSettledUnix, ["cycle_elapsed_seconds"] = CycleElapsedSeconds,
        };
        public void Restore(IDictionary<string, object> data)
        {
            double timestamp = SaveValueConversionRules.GetDouble(data, "last_settled_unix", 0);
            LastSettledUnix = double.IsFinite(timestamp) ? Math.Max(0, timestamp) : 0;
            CycleElapsedSeconds = NormalizePhase(SaveValueConversionRules.GetDouble(data, "cycle_elapsed_seconds", 0));
        }
        private static double NormalizePhase(double phase) => double.IsFinite(phase) ? Math.Max(0, phase) % CycleSeconds : 0;
    }
}
