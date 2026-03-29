using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class FishingPersistenceRules
    {
        public readonly record struct FishingSnapshot(string SelectedRecipeId, float Progress, float RequiredProgress);

        public static Dictionary<string, object> ToPlainDictionary(FishingSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["selected_recipe"] = snapshot.SelectedRecipeId,
                ["progress"] = snapshot.Progress,
                ["required_progress"] = snapshot.RequiredProgress,
            };
        }

        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(FishingSnapshot snapshot)
        {
            return SaveValueConversionRules.ToVariantDictionary(ToPlainDictionary(snapshot));
        }

        public static FishingSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            return new FishingSnapshot(
                SaveValueConversionRules.GetString(data, "selected_recipe", string.Empty),
                System.Math.Max(0.0f, (float)SaveValueConversionRules.GetDouble(data, "progress", 0.0)),
                System.Math.Max(1.0f, (float)SaveValueConversionRules.GetDouble(data, "required_progress", 120.0)));
        }

        public static FishingSnapshot FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            return FromPlainDictionary(SaveValueConversionRules.ToPlainDictionary(data));
        }
    }
}
