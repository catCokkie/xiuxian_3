using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class CultivationRhythmPersistenceRules
    {
        public readonly record struct RhythmSnapshot(
            bool Enabled,
            string Strength,
            int CycleMinutes,
            double CurrentCycleActiveSeconds,
            int SmallCycleCount,
            int TotalSmallCycles,
            int TotalGrandCycles,
            double RestRemainingSeconds,
            bool IsGrandRest,
            int TotalRestCount,
            int TotalMeditationInsights);

        public static Dictionary<string, object> ToPlainDictionary(RhythmSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["enabled"] = snapshot.Enabled,
                ["strength"] = snapshot.Strength,
                ["cycle_minutes"] = snapshot.CycleMinutes,
                ["current_cycle_active_seconds"] = snapshot.CurrentCycleActiveSeconds,
                ["small_cycle_count"] = snapshot.SmallCycleCount,
                ["total_small_cycles"] = snapshot.TotalSmallCycles,
                ["total_grand_cycles"] = snapshot.TotalGrandCycles,
                ["rest_remaining_seconds"] = snapshot.RestRemainingSeconds,
                ["is_grand_rest"] = snapshot.IsGrandRest,
                ["total_rest_count"] = snapshot.TotalRestCount,
                ["total_meditation_insights"] = snapshot.TotalMeditationInsights,
            };
        }

        public static RhythmSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            return new RhythmSnapshot(
                Enabled: !data.ContainsKey("enabled") || SaveValueConversionRules.GetBool(data, "enabled", true),
                Strength: CultivationRhythmRules.NormalizeStrength(SaveValueConversionRules.GetString(data, "strength", CultivationRhythmRules.StrengthWeak)),
                CycleMinutes: CultivationRhythmRules.NormalizeCycleMinutes(SaveValueConversionRules.GetInt(data, "cycle_minutes", CultivationRhythmRules.DefaultCycleMinutes)),
                CurrentCycleActiveSeconds: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "current_cycle_active_seconds", 0.0)),
                SmallCycleCount: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "small_cycle_count", 0)),
                TotalSmallCycles: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_small_cycles", 0)),
                TotalGrandCycles: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_grand_cycles", 0)),
                RestRemainingSeconds: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "rest_remaining_seconds", 0.0)),
                IsGrandRest: SaveValueConversionRules.GetBool(data, "is_grand_rest"),
                TotalRestCount: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_rest_count", 0)),
                TotalMeditationInsights: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_meditation_insights", 0)));
        }
    }
}
