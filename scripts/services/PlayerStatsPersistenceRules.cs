using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class PlayerStatsPersistenceRules
    {
        public readonly record struct PlayerStatsSnapshot(
            int TotalBattleLosses = 0,
            int TotalBossBattles = 0,
            int TotalEliteBattles = 0,
            int TotalMonsterKills = 0,
            int HighestWinStreak = 0,
            int CurrentWinStreak = 0,
            int TotalAlchemyCrafts = 0,
            int TotalSmithingCrafts = 0,
            int TotalTalismanCrafts = 0,
            int TotalCookingCrafts = 0,
            int TotalFormationCrafts = 0,
            int TotalMiningCompletions = 0,
            int TotalFishingCompletions = 0,
            int TotalPotionsConsumedInBattle = 0,
            int TotalTalismansConsumedInBattle = 0,
            int TotalGardenPlants = 0,
            int TotalGardenHarvests = 0,
            int TotalGardenAutoHarvests = 0,
            double TotalSpentLingqi = 0.0,
            int TotalSpentSpiritStones = 0,
            int SpentSpiritStonesOnShop = 0,
            int SpentSpiritStonesOnSeeds = 0,
            int SpentSpiritStonesOnOther = 0,
            double TotalSpentInsight = 0.0);

        public static Dictionary<string, object> ToPlainDictionary(PlayerStatsSnapshot snapshot)
        {
            return new Dictionary<string, object>
            {
                ["total_battle_losses"] = snapshot.TotalBattleLosses,
                ["total_boss_battles"] = snapshot.TotalBossBattles,
                ["total_elite_battles"] = snapshot.TotalEliteBattles,
                ["total_monster_kills"] = snapshot.TotalMonsterKills,
                ["highest_win_streak"] = snapshot.HighestWinStreak,
                ["current_win_streak"] = snapshot.CurrentWinStreak,
                ["total_alchemy_crafts"] = snapshot.TotalAlchemyCrafts,
                ["total_smithing_crafts"] = snapshot.TotalSmithingCrafts,
                ["total_talisman_crafts"] = snapshot.TotalTalismanCrafts,
                ["total_cooking_crafts"] = snapshot.TotalCookingCrafts,
                ["total_formation_crafts"] = snapshot.TotalFormationCrafts,
                ["total_mining_completions"] = snapshot.TotalMiningCompletions,
                ["total_fishing_completions"] = snapshot.TotalFishingCompletions,
                ["total_potions_consumed_in_battle"] = snapshot.TotalPotionsConsumedInBattle,
                ["total_talismans_consumed_in_battle"] = snapshot.TotalTalismansConsumedInBattle,
                ["total_garden_plants"] = snapshot.TotalGardenPlants,
                ["total_garden_harvests"] = snapshot.TotalGardenHarvests,
                ["total_garden_auto_harvests"] = snapshot.TotalGardenAutoHarvests,
                ["total_spent_lingqi"] = snapshot.TotalSpentLingqi,
                ["total_spent_spirit_stones"] = snapshot.TotalSpentSpiritStones,
                ["spent_spirit_stones_on_shop"] = snapshot.SpentSpiritStonesOnShop,
                ["spent_spirit_stones_on_seeds"] = snapshot.SpentSpiritStonesOnSeeds,
                ["spent_spirit_stones_on_other"] = snapshot.SpentSpiritStonesOnOther,
                ["total_spent_insight"] = snapshot.TotalSpentInsight,
            };
        }

        public static PlayerStatsSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            return new PlayerStatsSnapshot(
                TotalBattleLosses: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_battle_losses", 0)),
                TotalBossBattles: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_boss_battles", 0)),
                TotalEliteBattles: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_elite_battles", 0)),
                TotalMonsterKills: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_monster_kills", 0)),
                HighestWinStreak: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "highest_win_streak", 0)),
                CurrentWinStreak: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "current_win_streak", 0)),
                TotalAlchemyCrafts: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_alchemy_crafts", 0)),
                TotalSmithingCrafts: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_smithing_crafts", 0)),
                TotalTalismanCrafts: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_talisman_crafts", 0)),
                TotalCookingCrafts: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_cooking_crafts", 0)),
                TotalFormationCrafts: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_formation_crafts", 0)),
                TotalMiningCompletions: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_mining_completions", 0)),
                TotalFishingCompletions: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_fishing_completions", 0)),
                TotalPotionsConsumedInBattle: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_potions_consumed_in_battle", 0)),
                TotalTalismansConsumedInBattle: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_talismans_consumed_in_battle", 0)),
                TotalGardenPlants: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_garden_plants", 0)),
                TotalGardenHarvests: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_garden_harvests", 0)),
                TotalGardenAutoHarvests: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_garden_auto_harvests", 0)),
                TotalSpentLingqi: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "total_spent_lingqi", 0.0)),
                TotalSpentSpiritStones: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "total_spent_spirit_stones", 0)),
                SpentSpiritStonesOnShop: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "spent_spirit_stones_on_shop", 0)),
                SpentSpiritStonesOnSeeds: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "spent_spirit_stones_on_seeds", 0)),
                SpentSpiritStonesOnOther: System.Math.Max(0, SaveValueConversionRules.GetInt(data, "spent_spirit_stones_on_other", 0)),
                TotalSpentInsight: System.Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "total_spent_insight", 0.0)));
        }
    }
}
