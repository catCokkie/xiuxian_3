using System;
using System.Collections.Generic;
using System.Linq;

namespace Xiuxian.Scripts.Services
{
    public static class SubsystemMasteryRules
    {
        public const string DungeonBossWeaknessEffectId = "dungeon_boss_weakness_reduction";
        public const string CultivationLingqiBonusEffectId = "cultivation_lingqi_bonus";

        public const string GardenGrowthSpeedBonusEffectId = "garden_growth_speed_bonus";
        public const string GardenRareSpawnBonusEffectId = "garden_rare_spawn_bonus";
        public const string GardenOfflineFullEffectId = "garden_offline_full";
        public const string MiningDurabilityBonusEffectId = "mining_durability_bonus";
        public const string MiningRareSpawnBonusEffectId = "mining_rare_spawn_bonus";
        public const string MiningDoubleOutputEffectId = "mining_double_output_chance";
        public const string FishingSpeedBonusEffectId = "fishing_speed_bonus";
        public const string FishingRarePondUnlockEffectId = "fishing_rare_pond_unlock";
        public const string FishingDoubleOutputEffectId = "fishing_double_output_chance";
        public const string TalismanDoubleOutputEffectId = "talisman_double_output_chance";
        public const string TalismanMaterialDiscountEffectId = "talisman_material_discount";
        public const string TalismanEnchantChanceEffectId = "talisman_enchant_chance";
        public const string CookingDurationBonusEffectId = "cooking_duration_bonus";
        public const string CookingDoubleOutputEffectId = "cooking_double_output_chance";
        public const string CookingExtraEffectId = "cooking_extra_effect";
        public const string FormationEffectBonusEffectId = "formation_effect_bonus";
        public const string FormationDualSlotEffectId = "formation_dual_slot";
        public const string FormationSelfRepairEffectId = "formation_self_repair";
        public const string BodyCultivationEfficiencyBonusEffectId = "body_cultivation_efficiency_bonus";
        public const string BodyCultivationTemperCapBonusEffectId = "body_cultivation_temper_cap_bonus";
        public const string BodyCultivationBoneforgeCapBonusEffectId = "body_cultivation_boneforge_cap_bonus";

        public readonly record struct MasteryDefinition(
            string SystemId,
            int Level,
            double InsightCost,
            int RequiredRealmLevel,
            string EffectId,
            string DisplayName,
            double EffectValue);

        private static readonly string[] OrderedSystemIds =
        {
            PlayerActionState.ModeDungeon,
            PlayerActionState.ModeCultivation,
            PlayerActionState.ModeAlchemy,
            PlayerActionState.ModeSmithing,
            PlayerActionState.ModeGarden,
            PlayerActionState.ModeMining,
            PlayerActionState.ModeFishing,
            PlayerActionState.ModeTalisman,
            PlayerActionState.ModeCooking,
            PlayerActionState.ModeFormation,
            PlayerActionState.ModeBodyCultivation,
        };

        private static readonly IReadOnlyList<MasteryDefinition> Definitions = BuildDefinitions();
        private static readonly Dictionary<string, MasteryDefinition[]> DefinitionsBySystem = Definitions
            .GroupBy(def => def.SystemId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(def => def.Level).ToArray(), StringComparer.Ordinal);

        public static IReadOnlyList<MasteryDefinition> GetAllDefinitions() => Definitions;

        public static IReadOnlyList<string> GetAllSystemIds() => OrderedSystemIds;

        public static bool TryGetDefinition(string systemId, int level, out MasteryDefinition definition)
        {
            definition = default;
            if (!DefinitionsBySystem.TryGetValue(systemId, out MasteryDefinition[]? definitions))
            {
                return false;
            }

            int clampedLevel = Math.Clamp(level, 1, 4);
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i].Level == clampedLevel)
                {
                    definition = definitions[i];
                    return true;
                }
            }

            return false;
        }

        public static int GetCurrentLevel(string systemId, IReadOnlyDictionary<string, int>? levels)
        {
            if (levels != null && levels.TryGetValue(systemId, out int level))
            {
                return Math.Clamp(level, 1, 4);
            }

            return 1;
        }

        public static bool CanUnlock(string systemId, int currentLevel, double currentInsight, int currentRealmLevel)
        {
            MasteryDefinition? next = GetNextDefinition(systemId, currentLevel);
            return next.HasValue
                && currentInsight >= next.Value.InsightCost
                && currentRealmLevel >= next.Value.RequiredRealmLevel;
        }

        public static bool TryUnlock(string systemId, int currentLevel, double currentInsight, int currentRealmLevel, out int nextLevel, out double remainingInsight)
        {
            nextLevel = Math.Clamp(currentLevel, 1, 4);
            remainingInsight = currentInsight;
            MasteryDefinition? next = GetNextDefinition(systemId, currentLevel);
            if (!next.HasValue)
            {
                return false;
            }

            if (currentInsight < next.Value.InsightCost || currentRealmLevel < next.Value.RequiredRealmLevel)
            {
                return false;
            }

            nextLevel = next.Value.Level;
            remainingInsight = currentInsight - next.Value.InsightCost;
            return true;
        }

        public static double GetUnlockCost(string systemId, int currentLevel)
        {
            return GetNextDefinition(systemId, currentLevel)?.InsightCost ?? 0.0;
        }

        public static double GetEffectValue(string systemId, int level, string effectId)
        {
            if (!DefinitionsBySystem.TryGetValue(systemId, out MasteryDefinition[]? definitions))
            {
                return 0.0;
            }

            double value = 0.0;
            int clampedLevel = Math.Clamp(level, 1, 4);
            for (int i = 0; i < definitions.Length; i++)
            {
                MasteryDefinition def = definitions[i];
                if (def.Level > clampedLevel)
                {
                    break;
                }

                if (def.EffectId == effectId)
                {
                    value = def.EffectValue;
                }
            }

            return value;
        }

        private static MasteryDefinition? GetNextDefinition(string systemId, int currentLevel)
        {
            if (!DefinitionsBySystem.TryGetValue(systemId, out MasteryDefinition[]? definitions))
            {
                return null;
            }

            int targetLevel = Math.Clamp(currentLevel, 1, 4) + 1;
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i].Level == targetLevel)
                {
                    return definitions[i];
                }
            }

            return null;
        }

        private static IReadOnlyList<MasteryDefinition> BuildDefinitions()
        {
            return new List<MasteryDefinition>
            {
                Def(PlayerActionState.ModeDungeon, 1, 0, 1, "dungeon_base", "副本精通 Lv1", 0.0),
                Def(PlayerActionState.ModeDungeon, 2, 30, 2, "dungeon_elite_rate_bonus", "副本精通 Lv2", 0.10),
                Def(PlayerActionState.ModeDungeon, 3, 60, 3, "dungeon_drop_rate_bonus", "副本精通 Lv3", 0.15),
                Def(PlayerActionState.ModeDungeon, 4, 100, 1, DungeonBossWeaknessEffectId, "副本精通 Lv4", 0.10),

                Def(PlayerActionState.ModeCultivation, 1, 0, 1, "cultivation_base", "修炼精通 Lv1", 0.0),
                Def(PlayerActionState.ModeCultivation, 2, 25, 1, CultivationLingqiBonusEffectId, "修炼精通 Lv2", 0.10),
                Def(PlayerActionState.ModeCultivation, 3, 50, 3, "cultivation_parallel_explore_factor", "修炼精通 Lv3", 0.30),
                Def(PlayerActionState.ModeCultivation, 4, 80, 1, "cultivation_breakthrough_exp_reduction", "修炼精通 Lv4", 0.08),

                Def(PlayerActionState.ModeAlchemy, 1, 0, 1, "alchemy_base", "炼丹精通 Lv1", 0.0),
                Def(PlayerActionState.ModeAlchemy, 2, 20, 1, "alchemy_unlock_juling_san", "炼丹精通 Lv2", 1.0),
                Def(PlayerActionState.ModeAlchemy, 3, 45, 2, "alchemy_bonus_output", "炼丹精通 Lv3", 1.0),
                Def(PlayerActionState.ModeAlchemy, 4, 75, 3, "alchemy_high_tier_formula", "炼丹精通 Lv4", 1.0),

                Def(PlayerActionState.ModeSmithing, 1, 0, 1, "smithing_max_enhance_level", "炼器精通 Lv1", 3.0),
                Def(PlayerActionState.ModeSmithing, 2, 25, 1, "smithing_max_enhance_level", "炼器精通 Lv2", 6.0),
                Def(PlayerActionState.ModeSmithing, 3, 50, 2, "smithing_material_discount", "炼器精通 Lv3", 0.10),
                Def(PlayerActionState.ModeSmithing, 4, 80, 3, "smithing_max_enhance_level", "炼器精通 Lv4", 9.0),

                Def(PlayerActionState.ModeGarden, 1, 0, 1, "garden_base", "灵田精通 Lv1", 0.0),
                Def(PlayerActionState.ModeGarden, 2, 25, 1, GardenGrowthSpeedBonusEffectId, "灵田精通 Lv2 — 生长+15%", 0.15),
                Def(PlayerActionState.ModeGarden, 3, 55, 2, GardenRareSpawnBonusEffectId, "灵田精通 Lv3 — 稀有+10%", 0.10),
                Def(PlayerActionState.ModeGarden, 4, 95, 3, GardenOfflineFullEffectId, "灵田精通 Lv4 — 离线100%", 1.0),

                Def(PlayerActionState.ModeMining, 1, 0, 1, "mining_base", "矿脉精通 Lv1", 0.0),
                Def(PlayerActionState.ModeMining, 2, 30, 1, MiningDurabilityBonusEffectId, "矿脉精通 Lv2 — 耐久+20%", 0.20),
                Def(PlayerActionState.ModeMining, 3, 60, 2, MiningRareSpawnBonusEffectId, "矿脉精通 Lv3 — 稀有+10%", 0.10),
                Def(PlayerActionState.ModeMining, 4, 95, 3, MiningDoubleOutputEffectId, "矿脉精通 Lv4 — 双倍10%", 0.10),

                Def(PlayerActionState.ModeFishing, 1, 0, 1, "fishing_base", "灵渔精通 Lv1", 0.0),
                Def(PlayerActionState.ModeFishing, 2, 25, 1, FishingSpeedBonusEffectId, "灵渔精通 Lv2 — 速度+15%", 0.15),
                Def(PlayerActionState.ModeFishing, 3, 55, 2, FishingRarePondUnlockEffectId, "灵渔精通 Lv3 — 稀有鱼塘", 1.0),
                Def(PlayerActionState.ModeFishing, 4, 90, 3, FishingDoubleOutputEffectId, "灵渔精通 Lv4 — 双倍8%", 0.08),

                Def(PlayerActionState.ModeTalisman, 1, 0, 1, "talisman_base", "符箓精通 Lv1", 0.0),
                Def(PlayerActionState.ModeTalisman, 2, 30, 1, TalismanDoubleOutputEffectId, "符箓精通 Lv2 — 双出10%", 0.10),
                Def(PlayerActionState.ModeTalisman, 3, 60, 2, TalismanMaterialDiscountEffectId, "符箓精通 Lv3 — 材料-10%", 0.10),
                Def(PlayerActionState.ModeTalisman, 4, 95, 3, TalismanEnchantChanceEffectId, "符箓精通 Lv4 — 附魔概率", 0.10),

                Def(PlayerActionState.ModeCooking, 1, 0, 1, "cooking_base", "烹饪精通 Lv1", 0.0),
                Def(PlayerActionState.ModeCooking, 2, 25, 1, CookingDurationBonusEffectId, "烹饪精通 Lv2 — 持续+1场", 1.0),
                Def(PlayerActionState.ModeCooking, 3, 55, 2, CookingDoubleOutputEffectId, "烹饪精通 Lv3 — 双出15%", 0.15),
                Def(PlayerActionState.ModeCooking, 4, 90, 3, CookingExtraEffectId, "烹饪精通 Lv4 — 额外效果", 1.0),

                Def(PlayerActionState.ModeFormation, 1, 0, 1, "formation_base", "阵法精通 Lv1", 0.0),
                Def(PlayerActionState.ModeFormation, 2, 30, 1, FormationEffectBonusEffectId, "阵法精通 Lv2 — 效果+10%", 0.10),
                Def(PlayerActionState.ModeFormation, 3, 60, 2, FormationDualSlotEffectId, "阵法精通 Lv3 — 双槽", 2.0),
                Def(PlayerActionState.ModeFormation, 4, 100, 3, FormationSelfRepairEffectId, "阵法精通 Lv4 — 自修复", 1.0),

                Def(PlayerActionState.ModeBodyCultivation, 1, 0, 1, "body_cultivation_base", "体修精通 Lv1", 0.0),
                Def(PlayerActionState.ModeBodyCultivation, 2, 30, 1, BodyCultivationEfficiencyBonusEffectId, "体修精通 Lv2 — 效率+10%", 0.10),
                Def(PlayerActionState.ModeBodyCultivation, 3, 65, 2, BodyCultivationTemperCapBonusEffectId, "体修精通 Lv3 — 淬体上限+10", 10.0),
                Def(PlayerActionState.ModeBodyCultivation, 4, 90, 3, BodyCultivationBoneforgeCapBonusEffectId, "体修精通 Lv4 — 炼骨上限+5", 5.0),
            };
        }

        private static MasteryDefinition Def(string systemId, int level, double cost, int requiredRealmLevel, string effectId, string displayName, double effectValue)
        {
            return new MasteryDefinition(systemId, level, cost, requiredRealmLevel, effectId, displayName, effectValue);
        }
    }
}
