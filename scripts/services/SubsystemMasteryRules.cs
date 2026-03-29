using System;
using System.Collections.Generic;
using System.Linq;

namespace Xiuxian.Scripts.Services
{
    public static class SubsystemMasteryRules
    {
        public const string DungeonBossWeaknessEffectId = "dungeon_boss_weakness_reduction";
        public const string CultivationLingqiBonusEffectId = "cultivation_lingqi_bonus";

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
            PlayerActionState.ModeEnlightenment,
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
                Def(PlayerActionState.ModeGarden, 2, 20, 1, "garden_unlock_spirit_flower", "灵田精通 Lv2", 1.0),
                Def(PlayerActionState.ModeGarden, 3, 45, 2, "garden_bonus_yield", "灵田精通 Lv3", 1.0),
                Def(PlayerActionState.ModeGarden, 4, 75, 3, "garden_unlock_spirit_fruit", "灵田精通 Lv4", 1.0),

                Def(PlayerActionState.ModeMining, 1, 0, 1, "mining_base", "矿脉精通 Lv1", 0.0),
                Def(PlayerActionState.ModeMining, 2, 25, 1, "mining_unlock_spirit_jade", "矿脉精通 Lv2", 1.0),
                Def(PlayerActionState.ModeMining, 3, 55, 2, "mining_speed_bonus", "矿脉精通 Lv3", 0.15),
                Def(PlayerActionState.ModeMining, 4, 85, 3, "mining_unlock_mithril", "矿脉精通 Lv4", 1.0),

                Def(PlayerActionState.ModeFishing, 1, 0, 1, "fishing_base", "灵渔精通 Lv1", 0.0),
                Def(PlayerActionState.ModeFishing, 2, 20, 1, "fishing_unlock_deep_pond", "灵渔精通 Lv2", 1.0),
                Def(PlayerActionState.ModeFishing, 3, 45, 2, "fishing_speed_bonus", "灵渔精通 Lv3", 0.15),
                Def(PlayerActionState.ModeFishing, 4, 75, 3, "fishing_rare_bonus", "灵渔精通 Lv4", 1.0),

                Def(PlayerActionState.ModeTalisman, 1, 0, 1, "talisman_base", "符箓精通 Lv1", 0.0),
                Def(PlayerActionState.ModeTalisman, 2, 25, 1, "talisman_unlock_haste", "符箓精通 Lv2", 1.0),
                Def(PlayerActionState.ModeTalisman, 3, 55, 2, "talisman_material_discount", "符箓精通 Lv3", 0.10),
                Def(PlayerActionState.ModeTalisman, 4, 85, 3, "talisman_unlock_burst", "符箓精通 Lv4", 1.0),

                Def(PlayerActionState.ModeCooking, 1, 0, 1, "cooking_base", "烹饪精通 Lv1", 0.0),
                Def(PlayerActionState.ModeCooking, 2, 20, 1, "cooking_unlock_sweet", "烹饪精通 Lv2", 1.0),
                Def(PlayerActionState.ModeCooking, 3, 45, 2, "cooking_duration_bonus", "烹饪精通 Lv3", 1.0),
                Def(PlayerActionState.ModeCooking, 4, 75, 3, "cooking_unlock_dragon_soup", "烹饪精通 Lv4", 1.0),

                Def(PlayerActionState.ModeFormation, 1, 0, 1, "formation_base", "阵法精通 Lv1", 0.0),
                Def(PlayerActionState.ModeFormation, 2, 25, 1, "formation_unlock_iron_wall", "阵法精通 Lv2", 1.0),
                Def(PlayerActionState.ModeFormation, 3, 55, 2, "formation_slot_count", "阵法精通 Lv3", 2.0),
                Def(PlayerActionState.ModeFormation, 4, 85, 3, "formation_unlock_spirit_eye", "阵法精通 Lv4", 1.0),

                Def(PlayerActionState.ModeEnlightenment, 1, 0, 1, "enlightenment_base", "悟道精通 Lv1", 0.0),
                Def(PlayerActionState.ModeEnlightenment, 2, 25, 1, "enlightenment_unlock_hp", "悟道精通 Lv2", 1.0),
                Def(PlayerActionState.ModeEnlightenment, 3, 50, 2, "enlightenment_speed_bonus", "悟道精通 Lv3", 0.20),
                Def(PlayerActionState.ModeEnlightenment, 4, 80, 3, "enlightenment_unlock_lingqi", "悟道精通 Lv4", 1.0),

                Def(PlayerActionState.ModeBodyCultivation, 1, 0, 1, "body_cultivation_base", "体修精通 Lv1", 0.0),
                Def(PlayerActionState.ModeBodyCultivation, 2, 25, 1, "body_cultivation_unlock_spirit_skin", "体修精通 Lv2", 1.0),
                Def(PlayerActionState.ModeBodyCultivation, 3, 50, 2, "body_cultivation_material_discount", "体修精通 Lv3", 0.15),
                Def(PlayerActionState.ModeBodyCultivation, 4, 80, 3, "body_cultivation_unlock_blood_flow", "体修精通 Lv4", 1.0),
            };
        }

        private static MasteryDefinition Def(string systemId, int level, double cost, int requiredRealmLevel, string effectId, string displayName, double effectValue)
        {
            return new MasteryDefinition(systemId, level, cost, requiredRealmLevel, effectId, displayName, effectValue);
        }
    }
}
