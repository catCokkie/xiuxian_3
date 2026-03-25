using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentInstanceCodec
    {
        public static Dictionary<string, object> ToPlainDictionary(EquipmentInstanceData instance)
        {
            var subStats = new List<object>();
            IReadOnlyList<EquipmentSubStatData> items = instance.SubStats ?? System.Array.Empty<EquipmentSubStatData>();
            for (int i = 0; i < items.Count; i++)
            {
                subStats.Add(new Dictionary<string, object>
                {
                    ["stat"] = items[i].Stat,
                    ["value"] = items[i].Value,
                });
            }

            return new Dictionary<string, object>
            {
                ["equipment_id"] = instance.EquipmentId,
                ["equipment_template_id"] = instance.EquipmentTemplateId,
                ["display_name"] = instance.DisplayName,
                ["slot"] = instance.Slot.ToString(),
                ["series_id"] = instance.SeriesId,
                ["rarity_tier"] = instance.RarityTier.ToString(),
                ["source_stage"] = instance.SourceStage.ToString(),
                ["source_level_id"] = instance.SourceLevelId,
                ["main_stat_key"] = instance.MainStatKey,
                ["main_stat_value"] = instance.MainStatValue,
                ["sub_stats"] = subStats,
                ["enhance_level"] = instance.EnhanceLevel,
                ["power_budget"] = instance.PowerBudget,
                ["obtained_unix"] = instance.ObtainedUnix,
                ["is_equipped"] = instance.IsEquipped,
            };
        }

        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(EquipmentInstanceData instance)
        {
            return SaveValueConversionRules.ToVariantDictionary(ToPlainDictionary(instance));
        }

        public static EquipmentInstanceData FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            return FromPlainDictionary(SaveValueConversionRules.ToPlainDictionary(data));
        }

        public static EquipmentInstanceData FromPlainDictionary(IDictionary<string, object> data)
        {
            var subStats = new List<EquipmentSubStatData>();
            foreach (object item in SaveValueConversionRules.GetList(data, "sub_stats"))
            {
                if (item is not IDictionary<string, object> dict)
                {
                    continue;
                }

                subStats.Add(new EquipmentSubStatData(
                    SaveValueConversionRules.GetString(dict, "stat", string.Empty),
                    SaveValueConversionRules.GetDouble(dict, "value", 0.0)));
            }

            return new EquipmentInstanceData(
                EquipmentId: SaveValueConversionRules.GetString(data, "equipment_id", string.Empty),
                EquipmentTemplateId: SaveValueConversionRules.GetString(data, "equipment_template_id", string.Empty),
                DisplayName: SaveValueConversionRules.GetString(data, "display_name", string.Empty),
                Slot: ParseSlot(SaveValueConversionRules.GetString(data, "slot", string.Empty)),
                SeriesId: SaveValueConversionRules.GetString(data, "series_id", string.Empty),
                RarityTier: ParseRarity(SaveValueConversionRules.GetString(data, "rarity_tier", string.Empty)),
                SourceStage: ParseSourceStage(SaveValueConversionRules.GetString(data, "source_stage", string.Empty)),
                SourceLevelId: SaveValueConversionRules.GetString(data, "source_level_id", string.Empty),
                MainStatKey: SaveValueConversionRules.GetString(data, "main_stat_key", string.Empty),
                MainStatValue: SaveValueConversionRules.GetDouble(data, "main_stat_value", 0.0),
                SubStats: subStats,
                EnhanceLevel: SaveValueConversionRules.GetInt(data, "enhance_level", 0),
                PowerBudget: SaveValueConversionRules.GetInt(data, "power_budget", 0),
                ObtainedUnix: SaveValueConversionRules.GetLong(data, "obtained_unix", 0L),
                IsEquipped: SaveValueConversionRules.GetBool(data, "is_equipped"));
        }

        private static EquipmentSlotType ParseSlot(string slot)
        {
            return System.Enum.TryParse(slot, true, out EquipmentSlotType parsed) ? parsed : EquipmentSlotType.Weapon;
        }

        private static EquipmentRarityTier ParseRarity(string rarity)
        {
            return System.Enum.TryParse(rarity, true, out EquipmentRarityTier parsed) ? parsed : EquipmentRarityTier.Artifact;
        }

        private static EquipmentSourceStage ParseSourceStage(string sourceStage)
        {
            return System.Enum.TryParse(sourceStage, true, out EquipmentSourceStage parsed) ? parsed : EquipmentSourceStage.Normal;
        }
    }
}
