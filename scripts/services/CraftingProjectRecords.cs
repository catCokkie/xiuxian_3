using System;
using System.Collections.Generic;
using Godot;

namespace Xiuxian.Scripts.Services
{
    // Current state remains authoritative; records freeze projects while another is selected.
    public sealed class CraftingProjectRecords
    {
        private readonly Dictionary<string, Dictionary<string, object>> _records = new(StringComparer.Ordinal);
        public void Capture(string target, float progress, float required, CraftingProductionPlan plan, int enhanceLevel = -1)
        {
            if (string.IsNullOrEmpty(target)) return;
            _records[target] = new()
            {
                ["progress"] = float.IsFinite(progress) ? Math.Max(0, progress) : 0f,
                ["required_progress"] = float.IsFinite(required) ? Math.Max(1, required) : 100f,
                ["production"] = plan.ToPlainDictionary(), ["enhance_level"] = enhanceLevel,
            };
        }
        public bool TryRestore(string target, CraftingProductionPlan plan, out float progress, out float required, out int enhanceLevel)
        {
            progress = 0; required = 100; enhanceLevel = -1;
            if (!_records.TryGetValue(target, out var record)) return false;
            double rawProgress = ReadNumber(record, "progress", 0);
            double rawRequired = ReadNumber(record, "required_progress", 100);
            progress = double.IsFinite(rawProgress) ? (float)Math.Clamp(rawProgress, 0, float.MaxValue) : 0;
            required = double.IsFinite(rawRequired) ? (float)Math.Clamp(rawRequired, 1, float.MaxValue) : 100;
            double rawLevel = ReadNumber(record, "enhance_level", -1);
            enhanceLevel = double.IsFinite(rawLevel) ? (int)Math.Clamp(rawLevel, -1, int.MaxValue) : -1;
            if (record.TryGetValue("production", out object? value) && value is IDictionary<string, object> production) plan.Restore(production);
            else plan.Configure(false);
            plan.Stop();
            return true;
        }
        private static double ReadNumber(IDictionary<string, object> data, string key, double fallback)
        {
            try { return data.TryGetValue(key, out object? value) ? Convert.ToDouble(value) : fallback; }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { return fallback; }
        }
        public Dictionary<string, object> ToPlainDictionary()
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var pair in _records) result[pair.Key] = pair.Value;
            return result;
        }
        public void Restore(IDictionary<string, object> data)
        {
            _records.Clear();
            foreach (var pair in data)
                if (!string.IsNullOrEmpty(pair.Key) && pair.Value is IDictionary<string, object> record)
                    _records[pair.Key] = new Dictionary<string, object>(record, StringComparer.Ordinal);
        }
        public void WriteTo(Godot.Collections.Dictionary<string, Variant> data)
            => data["projects"] = SaveValueConversionRules.ToVariantDictionary(ToPlainDictionary());
        public void ReadFrom(Godot.Collections.Dictionary<string, Variant> data)
        {
            _records.Clear();
            if (data.TryGetValue("projects", out Variant value) && value.VariantType == Variant.Type.Dictionary)
                Restore(SaveValueConversionRules.ToPlainDictionary((Godot.Collections.Dictionary<string, Variant>)value));
        }
    }
}
