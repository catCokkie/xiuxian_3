using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class SubsystemMasteryPersistenceRules
    {
        public static Dictionary<string, object> ToPlainDictionary(IReadOnlyDictionary<string, int> levels)
        {
            var result = new Dictionary<string, object>(System.StringComparer.Ordinal);
            foreach (string systemId in SubsystemMasteryRules.GetAllSystemIds())
            {
                int level = levels != null && levels.TryGetValue(systemId, out int currentLevel) ? currentLevel : 1;
                result[systemId] = System.Math.Clamp(level, 1, 4);
            }

            return result;
        }

        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(IReadOnlyDictionary<string, int> levels)
        {
            return SaveValueConversionRules.ToVariantDictionary(ToPlainDictionary(levels));
        }

        public static Dictionary<string, int> FromPlainDictionary(IDictionary<string, object> data)
        {
            var result = new Dictionary<string, int>(System.StringComparer.Ordinal);
            foreach (string systemId in SubsystemMasteryRules.GetAllSystemIds())
            {
                result[systemId] = System.Math.Clamp(SaveValueConversionRules.GetInt(data, systemId, 1), 1, 4);
            }

            return result;
        }

        public static Dictionary<string, int> FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            return FromPlainDictionary(SaveValueConversionRules.ToPlainDictionary(data));
        }
    }
}
