using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class FormationPersistenceRules
    {
        public readonly record struct FormationSnapshot(
            string SelectedRecipeId,
            float Progress,
            float RequiredProgress,
            string ActivePrimaryId,
            string ActiveSecondaryId,
            IReadOnlyList<string> CraftedIds,
            IReadOnlyDictionary<string, int> Inventory);

        public static Dictionary<string, object> ToPlainDictionary(FormationSnapshot snapshot)
        {
            var inventory = new Dictionary<string, object>(System.StringComparer.Ordinal);
            foreach ((string formationId, int count) in snapshot.Inventory)
            {
                inventory[formationId] = System.Math.Max(0, count);
            }

            var craftedIds = new List<object>(snapshot.CraftedIds.Count);
            foreach (string formationId in snapshot.CraftedIds)
            {
                craftedIds.Add(formationId);
            }

            return new Dictionary<string, object>(System.StringComparer.Ordinal)
            {
                ["selected_recipe"] = snapshot.SelectedRecipeId,
                ["progress"] = snapshot.Progress,
                ["required_progress"] = snapshot.RequiredProgress,
                ["active_primary_id"] = snapshot.ActivePrimaryId,
                ["active_secondary_id"] = snapshot.ActiveSecondaryId,
                ["crafted_ids"] = craftedIds,
                ["inventory"] = inventory,
            };
        }

        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(FormationSnapshot snapshot)
        {
            return SaveValueConversionRules.ToVariantDictionary(ToPlainDictionary(snapshot));
        }

        public static FormationSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            object craftedRaw = data.TryGetValue("crafted_ids", out object? craftedValue)
                ? craftedValue
                : new List<object>();
            object inventoryRaw = data.TryGetValue("inventory", out object? inventoryValue)
                ? inventoryValue
                : new Dictionary<string, object>(System.StringComparer.Ordinal);

            var craftedIds = new List<string>();
            foreach (object entry in SaveValueConversionRules.GetList(new Dictionary<string, object> { ["crafted_ids"] = craftedRaw }, "crafted_ids"))
            {
                string formationId = entry?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(formationId) && !craftedIds.Contains(formationId))
                {
                    craftedIds.Add(formationId);
                }
            }

            var inventory = new Dictionary<string, int>(System.StringComparer.Ordinal);
            if (inventoryRaw is IDictionary<string, object> inventorySource)
            {
                foreach ((string key, object value) in inventorySource)
                {
                    inventory[key] = System.Math.Max(0, System.Convert.ToInt32(value));
                }
            }

            return new FormationSnapshot(
                SaveValueConversionRules.GetString(data, "selected_recipe", string.Empty),
                System.Math.Max(0.0f, (float)SaveValueConversionRules.GetDouble(data, "progress", 0.0)),
                System.Math.Max(1.0f, (float)SaveValueConversionRules.GetDouble(data, "required_progress", 100.0)),
                SaveValueConversionRules.GetString(data, "active_primary_id", string.Empty),
                SaveValueConversionRules.GetString(data, "active_secondary_id", string.Empty),
                craftedIds,
                inventory);
        }

        public static FormationSnapshot FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            return FromPlainDictionary(SaveValueConversionRules.ToPlainDictionary(data));
        }
    }
}
