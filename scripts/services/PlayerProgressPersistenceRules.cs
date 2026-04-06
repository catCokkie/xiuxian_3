using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class PlayerProgressPersistenceRules
    {
        public readonly record struct PlayerProgressSnapshot(
            int RealmLevel,
            double RealmExp,
            bool AdvancedAlchemyStudyUnlocked,
            double CurrentRealmActiveSeconds,
            int BodyCultivationMaxHpFlat = 0,
            int BodyCultivationAttackFlat = 0,
            int BodyCultivationDefenseFlat = 0,
            int TemperCount = 0,
            int BoneforgeCount = 0,
            int BloodflowCount = 0,
            double BodyCultivationPostBattleHealRate = 0.0,
            double ZhouTianMaxHpRate = 0.0,
            double ZhouTianAttackRate = 0.0,
            double ZhouTianDefenseRate = 0.0);

        public static Dictionary<string, object> ToPlainDictionary(PlayerProgressSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["realm_level"] = snapshot.RealmLevel,
                ["realm_exp"] = snapshot.RealmExp,
                ["advanced_alchemy_study_unlocked"] = snapshot.AdvancedAlchemyStudyUnlocked,
                ["current_realm_active_seconds"] = snapshot.CurrentRealmActiveSeconds,
                ["body_cultivation_max_hp_flat"] = snapshot.BodyCultivationMaxHpFlat,
                ["body_cultivation_attack_flat"] = snapshot.BodyCultivationAttackFlat,
                ["body_cultivation_defense_flat"] = snapshot.BodyCultivationDefenseFlat,
                ["body_cultivation_temper_count"] = snapshot.TemperCount,
                ["body_cultivation_boneforge_count"] = snapshot.BoneforgeCount,
                ["body_cultivation_bloodflow_count"] = snapshot.BloodflowCount,
                ["body_cultivation_post_battle_heal_rate"] = snapshot.BodyCultivationPostBattleHealRate,
                ["zhoutian_max_hp_rate"] = snapshot.ZhouTianMaxHpRate,
                ["zhoutian_attack_rate"] = snapshot.ZhouTianAttackRate,
                ["zhoutian_defense_rate"] = snapshot.ZhouTianDefenseRate,
            };
        }

        public static PlayerProgressSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            return new PlayerProgressSnapshot(
                RealmLevel: System.Math.Max(1, SaveValueConversionRules.GetInt(data, "realm_level", 1)),
                RealmExp: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "realm_exp", 0.0)),
                AdvancedAlchemyStudyUnlocked: SaveValueConversionRules.GetBool(data, "advanced_alchemy_study_unlocked"),
                CurrentRealmActiveSeconds: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "current_realm_active_seconds", 0.0)),
                BodyCultivationMaxHpFlat: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "body_cultivation_max_hp_flat", 0)),
                BodyCultivationAttackFlat: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "body_cultivation_attack_flat", 0)),
                BodyCultivationDefenseFlat: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "body_cultivation_defense_flat", 0)),
                TemperCount: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "body_cultivation_temper_count", 0)),
                BoneforgeCount: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "body_cultivation_boneforge_count", 0)),
                BloodflowCount: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "body_cultivation_bloodflow_count", 0)),
                BodyCultivationPostBattleHealRate: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "body_cultivation_post_battle_heal_rate", 0.0)),
                ZhouTianMaxHpRate: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "zhoutian_max_hp_rate", 0.0)),
                ZhouTianAttackRate: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "zhoutian_attack_rate", 0.0)),
                ZhouTianDefenseRate: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "zhoutian_defense_rate", 0.0)));
        }
    }
}
