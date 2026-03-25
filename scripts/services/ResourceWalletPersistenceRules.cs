using Godot;

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

        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(WalletSnapshot snapshot)
        {
            return new Godot.Collections.Dictionary<string, Variant>
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

        public static WalletSnapshot FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            return new WalletSnapshot(
                Lingqi: data.ContainsKey("lingqi") ? data["lingqi"].AsDouble() : 0.0,
                Insight: data.ContainsKey("insight") ? data["insight"].AsDouble() : 0.0,
                PetAffinity: data.ContainsKey("pet_affinity") ? data["pet_affinity"].AsDouble() : 0.0,
                SpiritStones: data.ContainsKey("spirit_stones") ? data["spirit_stones"].AsInt32() : 0,
                TotalEarnedLingqi: data.ContainsKey("total_earned_lingqi") ? data["total_earned_lingqi"].AsDouble() : 0.0,
                TotalEarnedInsight: data.ContainsKey("total_earned_insight") ? data["total_earned_insight"].AsDouble() : 0.0,
                TotalEarnedPetAffinity: data.ContainsKey("total_earned_pet_affinity") ? data["total_earned_pet_affinity"].AsDouble() : 0.0,
                TotalEarnedSpiritStones: data.ContainsKey("total_earned_spirit_stones") ? data["total_earned_spirit_stones"].AsInt32() : 0);
        }
    }
}
