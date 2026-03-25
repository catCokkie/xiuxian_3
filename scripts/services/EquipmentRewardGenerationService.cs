using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public sealed class EquipmentRewardGenerationService
    {
        public static EquipmentInstanceData[] GenerateDropInstances(
            Dictionary<string, Godot.Collections.Dictionary<string, Variant>> equipmentTemplateById,
            IReadOnlyList<string> templateIds,
            string levelId,
            Func<int, int> rollWeight,
            Func<double, double, double> rollValue,
            Func<long> nowUnix)
        {
            var specsByTemplateId = new Dictionary<string, EquipmentGenerationRules.EquipmentTemplateGenerationSpec>();
            foreach (string templateId in templateIds)
            {
                if (specsByTemplateId.ContainsKey(templateId))
                {
                    continue;
                }

                if (equipmentTemplateById.TryGetValue(templateId, out var templateDict))
                {
                    specsByTemplateId[templateId] = EquipmentGenerationRules.FromTemplateDictionary(templateDict);
                }
            }

            return EquipmentDropInstanceGenerationRules.GenerateInstances(
                specsByTemplateId,
                templateIds,
                levelId,
                EquipmentSourceStage.Normal,
                rollWeight,
                rollValue,
                nowUnix);
        }

        public static EquipmentInstanceData[] GenerateFirstClearRewards(
            Dictionary<string, Godot.Collections.Dictionary<string, Variant>> equipmentTemplateById,
            Godot.Collections.Dictionary<string, Variant> firstClearRewards,
            string levelId,
            Func<int, int> rollWeight,
            Func<double, double, double> rollValue,
            Func<long> nowUnix)
        {
            if (!firstClearRewards.ContainsKey("equipment_templates") || firstClearRewards["equipment_templates"].VariantType != Variant.Type.Array)
            {
                return Array.Empty<EquipmentInstanceData>();
            }

            var rewardSpecs = new List<FirstClearEquipmentRewardRules.FirstClearEquipmentRewardSpec>();
            foreach (Variant item in (Godot.Collections.Array<Variant>)firstClearRewards["equipment_templates"])
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string templateId = LevelConfigProvider.GetString(dict, "equipment_template_id", "");
                string rarityText = LevelConfigProvider.GetString(dict, "rarity_tier", "");
                int qty = Math.Max(0, dict.ContainsKey("qty") ? dict["qty"].AsInt32() : 1);
                EquipmentRarityTier? rarityOverride = ParseOptionalRarity(rarityText);
                rewardSpecs.Add(new FirstClearEquipmentRewardRules.FirstClearEquipmentRewardSpec(templateId, rarityOverride, qty));
            }

            var specsByTemplateId = new Dictionary<string, EquipmentGenerationRules.EquipmentTemplateGenerationSpec>();
            foreach (FirstClearEquipmentRewardRules.FirstClearEquipmentRewardSpec reward in rewardSpecs)
            {
                if (string.IsNullOrEmpty(reward.EquipmentTemplateId) || specsByTemplateId.ContainsKey(reward.EquipmentTemplateId))
                {
                    continue;
                }

                if (equipmentTemplateById.TryGetValue(reward.EquipmentTemplateId, out var templateDict))
                {
                    specsByTemplateId[reward.EquipmentTemplateId] = EquipmentGenerationRules.FromTemplateDictionary(templateDict);
                }
            }

            return FirstClearEquipmentRewardRules.GenerateInstances(
                specsByTemplateId,
                rewardSpecs,
                levelId,
                rollWeight,
                rollValue,
                nowUnix);
        }

        private static EquipmentRarityTier? ParseOptionalRarity(string rarity)
        {
            return rarity switch
            {
                "common_tool" => EquipmentRarityTier.CommonTool,
                "artifact" => EquipmentRarityTier.Artifact,
                "spirit" => EquipmentRarityTier.Spirit,
                "treasure" => EquipmentRarityTier.Treasure,
                _ => null,
            };
        }
    }
}
