using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class ResourceWalletPersistenceRules
    {
        public readonly record struct WalletSnapshot(
            double Lingqi,
            double Insight,
            double PetAffinity,
            int SpiritStones,
            double TotalEarnedLingqi = 0.0,
            double TotalEarnedInsight = 0.0,
            double TotalEarnedPetAffinity = 0.0,
            int TotalEarnedSpiritStones = 0);

        public static Dictionary<string, object> ToPlainDictionary(WalletSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["lingqi"] = snapshot.Lingqi,
                ["insight"] = snapshot.Insight,
                ["pet_affinity"] = snapshot.PetAffinity,
                ["spirit_stones"] = snapshot.SpiritStones,
                ["total_earned_lingqi"] = snapshot.TotalEarnedLingqi,
                ["total_earned_insight"] = snapshot.TotalEarnedInsight,
                ["total_earned_pet_affinity"] = snapshot.TotalEarnedPetAffinity,
                ["total_earned_spirit_stones"] = snapshot.TotalEarnedSpiritStones,
            };
        }

        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(WalletSnapshot snapshot)
        {
            return SaveValueConversionRules.ToVariantDictionary(ToPlainDictionary(snapshot));
        }

        public static WalletSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            return new WalletSnapshot(
                Lingqi: SaveValueConversionRules.GetDouble(data, "lingqi", 0.0),
                Insight: SaveValueConversionRules.GetDouble(data, "insight", 0.0),
                PetAffinity: SaveValueConversionRules.GetDouble(data, "pet_affinity", 0.0),
                SpiritStones: SaveValueConversionRules.GetInt(data, "spirit_stones", 0),
                TotalEarnedLingqi: SaveValueConversionRules.GetDouble(data, "total_earned_lingqi", 0.0),
                TotalEarnedInsight: SaveValueConversionRules.GetDouble(data, "total_earned_insight", 0.0),
                TotalEarnedPetAffinity: SaveValueConversionRules.GetDouble(data, "total_earned_pet_affinity", 0.0),
                TotalEarnedSpiritStones: SaveValueConversionRules.GetInt(data, "total_earned_spirit_stones", 0));
        }

        public static WalletSnapshot FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            return FromPlainDictionary(SaveValueConversionRules.ToPlainDictionary(data));
        }
    }
}
