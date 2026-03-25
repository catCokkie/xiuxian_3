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
            double CurrentRealmActiveSeconds);

        public static Dictionary<string, object> ToPlainDictionary(PlayerProgressSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["realm_level"] = snapshot.RealmLevel,
                ["realm_exp"] = snapshot.RealmExp,
                ["pet_mood"] = snapshot.PetMood,
                ["advanced_alchemy_study_unlocked"] = snapshot.AdvancedAlchemyStudyUnlocked,
                ["current_realm_active_seconds"] = snapshot.CurrentRealmActiveSeconds,
            };
        }

        public static PlayerProgressSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            return new PlayerProgressSnapshot(
                RealmLevel: System.Math.Max(1, SaveValueConversionRules.GetInt(data, "realm_level", 1)),
                RealmExp: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "realm_exp", 0.0)),
                PetMood: System.Math.Clamp(SaveValueConversionRules.GetInt(data, "pet_mood", 60), 0, 100),
                AdvancedAlchemyStudyUnlocked: SaveValueConversionRules.GetBool(data, "advanced_alchemy_study_unlocked"),
                CurrentRealmActiveSeconds: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "current_realm_active_seconds", 0.0)));
        }
    }
}
