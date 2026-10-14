using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class InventoryReserveRules
    {
        public const int MaxReserve = 999999;
        public const string SaveKey = "__reserved_counts";
        public static int Spendable(int owned, int reserved) => Math.Max(0, Math.Max(0, owned) - Math.Max(0, reserved));
        public static Dictionary<string, int> Read(IDictionary<string, object> data)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var entry in data)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Key.StartsWith("__", StringComparison.Ordinal)) continue;
                if (entry.Value is not (int or long or double or float)) continue;
                double value = Convert.ToDouble(entry.Value);
                if (!double.IsFinite(value) || value <= 0) continue;
                result[entry.Key] = (int)Math.Clamp(value, 0, MaxReserve);
            }
            return result;
        }
    }
}
