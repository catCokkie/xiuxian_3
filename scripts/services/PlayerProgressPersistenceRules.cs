using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class PlayerProgressPersistenceRules
    {
        public readonly record struct PlayerProgressSnapshot(
            int RealmLevel,
            double RealmExp,
            int PetMood,
            bool AdvancedAlchemyStudyUnlocked,
            double CurrentRealmActiveSeconds,
            double EnlightenmentInsightBonusRate = 0.0,
            double EnlightenmentLingqiBonusRate = 0.0,
            int BodyCultivationMaxHpFlat = 0,
            int BodyCultivationAttackFlat = 0,
            int BodyCultivationDefenseFlat = 0,
            int MeditationCount = 0,
            int ContemplationCount = 0,
            int TemperCount = 0,
            int BoneforgeCount = 0);

        public static Dictionary<string, object> ToPlainDictionary(PlayerProgressSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["realm_level"] = snapshot.RealmLevel,
                ["realm_exp"] = snapshot.RealmExp,
                ["pet_mood"] = snapshot.PetMood,
                ["advanced_alchemy_study_unlocked"] = snapshot.AdvancedAlchemyStudyUnlocked,
                ["current_realm_active_seconds"] = snapshot.CurrentRealmActiveSeconds,
                ["enlightenment_insight_bonus_rate"] = snapshot.EnlightenmentInsightBonusRate,
                ["enlightenment_lingqi_bonus_rate"] = snapshot.EnlightenmentLingqiBonusRate,
                ["body_cultivation_max_hp_flat"] = snapshot.BodyCultivationMaxHpFlat,
                ["body_cultivation_attack_flat"] = snapshot.BodyCultivationAttackFlat,
                ["body_cultivation_defense_flat"] = snapshot.BodyCultivationDefenseFlat,
                ["enlightenment_meditation_count"] = snapshot.MeditationCount,
                ["enlightenment_contemplation_count"] = snapshot.ContemplationCount,
                ["body_cultivation_temper_count"] = snapshot.TemperCount,
                ["body_cultivation_boneforge_count"] = snapshot.BoneforgeCount,
            };
        }

        public static PlayerProgressSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            return new PlayerProgressSnapshot(
                RealmLevel: System.Math.Max(1, SaveValueConversionRules.GetInt(data, "realm_level", 1)),
                RealmExp: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "realm_exp", 0.0)),
                PetMood: System.Math.Clamp(SaveValueConversionRules.GetInt(data, "pet_mood", 60), 0, 100),
                AdvancedAlchemyStudyUnlocked: SaveValueConversionRules.GetBool(data, "advanced_alchemy_study_unlocked"),
                CurrentRealmActiveSeconds: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "current_realm_active_seconds", 0.0)),
                EnlightenmentInsightBonusRate: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "enlightenment_insight_bonus_rate", 0.0)),
                EnlightenmentLingqiBonusRate: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "enlightenment_lingqi_bonus_rate", 0.0)),
                BodyCultivationMaxHpFlat: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "body_cultivation_max_hp_flat", 0)),
                BodyCultivationAttackFlat: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "body_cultivation_attack_flat", 0)),
                BodyCultivationDefenseFlat: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "body_cultivation_defense_flat", 0)),
                MeditationCount: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "enlightenment_meditation_count", 0)),
                ContemplationCount: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "enlightenment_contemplation_count", 0)),
                TemperCount: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "body_cultivation_temper_count", 0)),
                BoneforgeCount: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "body_cultivation_boneforge_count", 0)));
        }
    }
}
