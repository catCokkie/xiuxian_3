using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class PlayerStatsPersistenceRules
    {
        public readonly record struct PlayerStatsSnapshot(
            int TotalBattleLosses = 0,
            int TotalBossBattles = 0,
            int TotalEliteBattles = 0,
            int TotalAlchemyCrafts = 0,
            int TotalSmithingCrafts = 0,
            int TotalTalismanCrafts = 0,
            int TotalCookingCrafts = 0,
            int TotalFormationCrafts = 0,
            int TotalMiningCompletions = 0,
            int TotalFishingCompletions = 0,
            int TotalGardenPlants = 0,
            int TotalGardenHarvests = 0,
            int TotalGardenAutoHarvests = 0,
            int TotalSpentSpiritStones = 0,
            double TotalSpentInsight = 0.0);

        public static Dictionary<string, object> ToPlainDictionary(PlayerStatsSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["total_battle_losses"] = snapshot.TotalBattleLosses,
                ["total_boss_battles"] = snapshot.TotalBossBattles,
                ["total_elite_battles"] = snapshot.TotalEliteBattles,
                ["total_alchemy_crafts"] = snapshot.TotalAlchemyCrafts,
                ["total_smithing_crafts"] = snapshot.TotalSmithingCrafts,
                ["total_talisman_crafts"] = snapshot.TotalTalismanCrafts,
                ["total_cooking_crafts"] = snapshot.TotalCookingCrafts,
                ["total_formation_crafts"] = snapshot.TotalFormationCrafts,
                ["total_mining_completions"] = snapshot.TotalMiningCompletions,
                ["total_fishing_completions"] = snapshot.TotalFishingCompletions,
                ["total_garden_plants"] = snapshot.TotalGardenPlants,
                ["total_garden_harvests"] = snapshot.TotalGardenHarvests,
                ["total_garden_auto_harvests"] = snapshot.TotalGardenAutoHarvests,
                ["total_spent_spirit_stones"] = snapshot.TotalSpentSpiritStones,
                ["total_spent_insight"] = snapshot.TotalSpentInsight,
            };
        }

        public static PlayerStatsSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            return new PlayerStatsSnapshot(
                TotalBattleLosses: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_battle_losses", 0)),
                TotalBossBattles: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_boss_battles", 0)),
                TotalEliteBattles: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_elite_battles", 0)),
                TotalAlchemyCrafts: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_alchemy_crafts", 0)),
                TotalSmithingCrafts: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_smithing_crafts", 0)),
                TotalTalismanCrafts: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_talisman_crafts", 0)),
                TotalCookingCrafts: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_cooking_crafts", 0)),
                TotalFormationCrafts: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_formation_crafts", 0)),
                TotalMiningCompletions: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_mining_completions", 0)),
                TotalFishingCompletions: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_fishing_completions", 0)),
                TotalGardenPlants: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_garden_plants", 0)),
                TotalGardenHarvests: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_garden_harvests", 0)),
                TotalGardenAutoHarvests: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_garden_auto_harvests", 0)),
                TotalSpentSpiritStones: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_spent_spirit_stones", 0)),
                TotalSpentInsight: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "total_spent_insight", 0.0)));
        }
    }
}
