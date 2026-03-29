using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class MiningPersistenceRules
    {
        public readonly record struct MiningSnapshot(string SelectedRecipeId, float Progress, float RequiredProgress, int CurrentDurability);

        public static Dictionary<string, object> ToPlainDictionary(MiningSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["selected_recipe"] = snapshot.SelectedRecipeId,
                ["progress"] = snapshot.Progress,
                ["required_progress"] = snapshot.RequiredProgress,
                ["current_durability"] = snapshot.CurrentDurability,
            };
        }

        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(MiningSnapshot snapshot)
        {
            return SaveValueConversionRules.ToVariantDictionary(ToPlainDictionary(snapshot));
        }

        public static MiningSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            return new MiningSnapshot(
                SaveValueConversionRules.GetString(data, "selected_recipe", string.Empty),
                System.Math.Max(0.0f, (float)SaveValueConversionRules.GetDouble(data, "progress", 0.0)),
                System.Math.Max(1.0f, (float)SaveValueConversionRules.GetDouble(data, "required_progress", 180.0)),
                System.Math.Clamp(SaveValueConversionRules.GetInt(data, "current_durability", MiningRules.DefaultNodeDurability), 1, MiningRules.DefaultNodeDurability));
        }

        public static MiningSnapshot FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            return FromPlainDictionary(SaveValueConversionRules.ToPlainDictionary(data));
        }
    }
}
