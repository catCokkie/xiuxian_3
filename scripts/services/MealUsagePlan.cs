using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>Counts portions actually taken, including the currently active portion.</summary>
    public sealed class MealUsagePlan
    {
        public const int MaxPortions = 9999;
        public int PortionLimit { get; private set; } = 1;
        public int ConsumedPortions { get; private set; }
        public bool RenewalStopped { get; private set; }
        public bool Continuous => PortionLimit == 0;
        public int AllowedRefills(int inventory) => RenewalStopped ? 0 : Math.Min(Math.Max(0, inventory),
            Continuous ? int.MaxValue : Math.Max(0, PortionLimit - ConsumedPortions));
        public string Summary => (Continuous ? $"一直续用 · 已用{ConsumedPortions}份" : $"已用{ConsumedPortions}/{PortionLimit}份")
            + (RenewalStopped ? " · 已停止续用" : "");

        public void Configure(bool continuous, int portions, bool hasActivePortion)
        {
            PortionLimit = continuous ? 0 : Math.Clamp(portions, 1, MaxPortions);
            ConsumedPortions = hasActivePortion ? 1 : 0;
            RenewalStopped = false;
        }
        public void RecordPortions(int count) => ConsumedPortions = (int)Math.Min(int.MaxValue,
            (long)ConsumedPortions + Math.Max(0, count));
        public void StopRenewal() => RenewalStopped = true;
        public Dictionary<string, object> ToPlainDictionary() => new()
        {
            ["portion_limit"] = PortionLimit, ["consumed_portions"] = ConsumedPortions, ["renewal_stopped"] = RenewalStopped,
        };
        public void Restore(IDictionary<string, object>? data, bool hasActivePortion)
        {
            // Legacy active meals automatically refilled; preserve that behavior without taking another portion.
            PortionLimit = data == null ? (hasActivePortion ? 0 : 1)
                : Math.Clamp(ReadInt(data, "portion_limit", 1), 0, MaxPortions);
            ConsumedPortions = data == null ? (hasActivePortion ? 1 : 0)
                : Math.Max(hasActivePortion ? 1 : 0, ReadInt(data, "consumed_portions", 0));
            RenewalStopped = data != null && data.TryGetValue("renewal_stopped", out var flag) && flag is bool stopped && stopped;
        }
        private static int ReadInt(IDictionary<string, object> data, string key, int fallback)
        {
            try { return data.TryGetValue(key, out var value) ? Convert.ToInt32(value) : fallback; }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { return fallback; }
        }
    }
}
