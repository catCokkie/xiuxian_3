using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class ShopPersistenceRules
    {
        public readonly record struct ShopSnapshot(
            Dictionary<string, int> LifetimePurchases,
            Dictionary<string, int> DailyPurchases,
            string DailyResetDate,
            double ActiveDoubleYieldSeconds);

        public static ShopSnapshot CreateDefault()
        {
            return new ShopSnapshot(
                new Dictionary<string, int>(),
                new Dictionary<string, int>(),
                string.Empty,
                0.0);
        }

        public static Dictionary<string, object> ToPlainDictionary(ShopSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["lifetime_purchases"] = BoxDictionary(snapshot.LifetimePurchases),
                ["daily_purchases"] = BoxDictionary(snapshot.DailyPurchases),
                ["daily_reset_date"] = snapshot.DailyResetDate,
                ["active_double_yield_seconds"] = snapshot.ActiveDoubleYieldSeconds,
            };
        }

        public static ShopSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            return new ShopSnapshot(
                ReadIntDictionary(data, "lifetime_purchases"),
                ReadIntDictionary(data, "daily_purchases"),
                SaveValueConversionRules.GetString(data, "daily_reset_date", string.Empty),
                System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "active_double_yield_seconds", 0.0)));
        }

        private static Dictionary<string, int> ReadIntDictionary(IDictionary<string, object> data, string key)
        {
            Dictionary<string, int> result = new();
            if (!data.TryGetValue(key, out object? boxed))
            {
                return result;
            }

            if (boxed is IDictionary<string, object> objectDict)
            {
                foreach ((string entryKey, object entryValue) in objectDict)
                {
                    result[entryKey] = System.Convert.ToInt32(entryValue);
                }
                return result;
            }

            if (boxed is Godot.Collections.Dictionary<string, Variant> variantDict)
            {
                foreach (string entryKey in variantDict.Keys)
                {
                    result[entryKey] = variantDict[entryKey].AsInt32();
                }
            }

            return result;
        }

        private static Dictionary<string, object> BoxDictionary(IReadOnlyDictionary<string, int> source)
        {
            Dictionary<string, object> result = new();
            foreach ((string key, int value) in source)
            {
                result[key] = value;
            }

            return result;
        }
    }
}
