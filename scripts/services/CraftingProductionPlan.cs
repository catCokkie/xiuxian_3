using System;
using System.Collections.Generic;
using Godot;

namespace Xiuxian.Scripts.Services
{
    // Counts committed batches, never attempts or item quantity. Zero means continuous.
    public sealed class CraftingProductionPlan
    {
        public const int MaxRounds = 9999;
        public int RoundLimit { get; private set; } = 1;
        public int CompletedRounds { get; private set; }
        public bool Paused { get; private set; }
        public bool Continuous => RoundLimit == 0;
        public bool Finished => !Continuous && CompletedRounds >= RoundLimit;
        public bool CanProduce => !Paused && !Finished;
        public string Summary => (Continuous ? $"持续生产 · 已完成 {CompletedRounds} 轮" : $"已完成 {CompletedRounds}/{RoundLimit} 轮")
            + (Finished ? " · 已完成" : Paused ? " · 已停止" : "");

        public static bool Supports(string systemId) => systemId is "alchemy" or "smithing" or "talisman" or "cooking" or "formation";
        public void Configure(bool continuous, int rounds = 1)
        {
            RoundLimit = continuous ? 0 : Math.Clamp(rounds, 1, MaxRounds);
            CompletedRounds = 0;
            Paused = false;
        }
        public void Start(bool continuous, int rounds)
        {
            int requested = continuous ? 0 : Math.Clamp(rounds, 1, MaxRounds);
            if (requested != RoundLimit || Finished) Configure(continuous, rounds);
            else Paused = false;
        }
        public void Stop() => Paused = true;
        public void CommitBatch()
        {
            if (CanProduce && CompletedRounds < int.MaxValue) CompletedRounds++;
        }
        public float BoundProgress(float progress, float required)
            => Continuous ? progress : Math.Min(progress, Math.Max(1f, required) * Math.Max(0, RoundLimit - CompletedRounds));

        public Dictionary<string, object> ToPlainDictionary() => new()
        {
            ["round_limit"] = RoundLimit, ["completed_rounds"] = CompletedRounds, ["paused"] = Paused,
        };
        public void Restore(IDictionary<string, object> data)
        {
            int limit = ReadInt(data, "round_limit", 1);
            RoundLimit = limit == 0 ? 0 : Math.Clamp(limit, 1, MaxRounds);
            CompletedRounds = Math.Max(0, ReadInt(data, "completed_rounds", 0));
            if (!Continuous) CompletedRounds = Math.Min(CompletedRounds, RoundLimit);
            Paused = data.TryGetValue("paused", out object? value) && value is bool flag && flag;
        }
        private static int ReadInt(IDictionary<string, object> data, string key, int fallback)
        {
            try { return data.TryGetValue(key, out object? value) ? Convert.ToInt32(value) : fallback; }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { return fallback; }
        }
        public void WriteTo(Godot.Collections.Dictionary<string, Variant> data)
            => data["production"] = SaveValueConversionRules.ToVariantDictionary(ToPlainDictionary());
        public void ReadFrom(Godot.Collections.Dictionary<string, Variant> data, bool hasLegacyTarget)
        {
            if (data.TryGetValue("production", out Variant value) && value.VariantType == Variant.Type.Dictionary)
                Restore(SaveValueConversionRules.ToPlainDictionary((Godot.Collections.Dictionary<string, Variant>)value));
            else Configure(hasLegacyTarget); // Pre-v17 selections ran continuously.
        }
    }
}
