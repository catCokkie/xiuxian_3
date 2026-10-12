using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class DungeonOfflineSettlementRules
    {
        public readonly record struct WeightedMonsterProfile(
            MonsterStatProfile Monster,
            int Weight);

        public static int CalculateEstimatedEncounters(
            double offlineInputBudget,
            double dungeonProgressPerInput,
            double encounterProgressThreshold)
        {
            if (offlineInputBudget <= 0.0 || dungeonProgressPerInput <= 0.0 || encounterProgressThreshold <= 0.0)
            {
                return 0;
            }

            double progress = offlineInputBudget * dungeonProgressPerInput;
            return Math.Max(0, (int)Math.Floor(progress / encounterProgressThreshold));
        }

        public static double CalculateWeightedVictoryFactor(
            CharacterStatBlock playerStats,
            IReadOnlyList<WeightedMonsterProfile> weightedMonsters)
        {
            if (weightedMonsters.Count == 0)
            {
                return 0.0;
            }

            int totalWeight = 0;
            double totalScore = 0.0;
            for (int i = 0; i < weightedMonsters.Count; i++)
            {
                int weight = Math.Max(0, weightedMonsters[i].Weight);
                if (weight <= 0)
                {
                    continue;
                }

                totalWeight += weight;
                totalScore += weight * CalculateMonsterVictoryFactor(playerStats, weightedMonsters[i].Monster.ToStatBlock(), weightedMonsters[i].Monster.HealPerRound);
            }

            return totalWeight > 0 ? totalScore / totalWeight : 0.0;
        }

        public static ActionSettlementResult BuildDungeonOfflineSettlement(
            string actionTargetId,
            double offlineInputBudget,
            double dungeonProgressPerInput,
            double encounterProgressThreshold,
            CharacterStatBlock playerStats,
            IReadOnlyList<WeightedMonsterProfile> weightedMonsters,
            double averageLingqiPerVictory,
            double averageInsightPerVictory,
            IReadOnlyDictionary<string, double> averageItemDropsPerVictory,
            int remainingDailyRolls,
            double averageDropRollsPerVictory,
            int equipmentDropCap,
            double estimatedEquipmentDropsPerVictory,
            Func<int, EquipmentInstanceData>? createEquipment = null,
            double remainingExploreProgress = 100.0,
            double equipmentRoll = 1.0)
        {
            // Offline exploration stops at BossReady. A first clear always needs the player's challenge.
            if (dungeonProgressPerInput > 0)
                offlineInputBudget = Math.Min(Math.Max(0, offlineInputBudget), Math.Max(0, remainingExploreProgress) / dungeonProgressPerInput);
            double exploreProgressGain = offlineInputBudget * dungeonProgressPerInput;
            int estimatedEncounters = CalculateEstimatedEncounters(offlineInputBudget, dungeonProgressPerInput, encounterProgressThreshold);
            double weightedVictoryFactor = CalculateWeightedVictoryFactor(playerStats, weightedMonsters);
            double effectiveVictories = estimatedEncounters * weightedVictoryFactor;
            double dropQuotaScale = CalculateDropQuotaScale(remainingDailyRolls, averageDropRollsPerVictory, effectiveVictories);

            var itemDrops = new Dictionary<string, int>();
            foreach (KeyValuePair<string, double> entry in averageItemDropsPerVictory)
            {
                int qty = (int)Math.Floor(Math.Max(0.0, entry.Value) * effectiveVictories * dropQuotaScale);
                if (qty > 0)
                {
                    itemDrops[entry.Key] = qty;
                }
            }

            double expectedEquipment = Math.Max(0.0, estimatedEquipmentDropsPerVictory) * effectiveVictories * dropQuotaScale;
            int rolledEquipment = (int)Math.Floor(expectedEquipment);
            if (equipmentRoll >= 0 && equipmentRoll < expectedEquipment - rolledEquipment) rolledEquipment++;
            int equipmentCount = Math.Min(Math.Max(0, equipmentDropCap), rolledEquipment);
            var equipmentDrops = new List<EquipmentInstanceData>(equipmentCount);
            if (createEquipment != null)
                for (int i = 0; i < equipmentCount; i++) equipmentDrops.Add(createEquipment(i));

            return ActionSettlementRules.BuildDungeonRewardSettlement(
                actionTargetId: actionTargetId,
                sourceTag: "offline_dungeon",
                exploreProgressGain: exploreProgressGain,
                battleRoundsAdvanced: estimatedEncounters,
                itemDrops: itemDrops,
                equipmentDrops: equipmentDrops,
                lingqiGain: averageLingqiPerVictory * effectiveVictories,
                insightGain: averageInsightPerVictory * effectiveVictories);
        }

        private static double CalculateDropQuotaScale(int remainingDailyRolls, double averageDropRollsPerVictory, double effectiveVictories)
        {
            if (remainingDailyRolls == int.MaxValue)
            {
                return 1.0;
            }

            if (remainingDailyRolls <= 0)
            {
                return 0.0;
            }

            double projectedDropRolls = Math.Max(0.0, averageDropRollsPerVictory) * Math.Max(0.0, effectiveVictories);
            if (projectedDropRolls <= 0.0)
            {
                return 1.0;
            }

            return Math.Clamp(remainingDailyRolls / projectedDropRolls, 0.0, 1.0);
        }

        public static double CalculateMonsterVictoryFactor(CharacterStatBlock playerStats, CharacterStatBlock monsterStats, int monsterHealPerRound = 0)
        {
            const int samples = 32;
            int wins = 0;
            for (int sample = 0; sample < samples; sample++)
            {
                var player = new CharacterBattleSnapshot(playerStats.MaxHp, playerStats.MaxHp, playerStats.Attack, playerStats.Defense, playerStats.Speed, playerStats.CritChance, playerStats.CritDamage);
                var monster = new CharacterBattleSnapshot(monsterStats.MaxHp, monsterStats.MaxHp, monsterStats.Attack, monsterStats.Defense, monsterStats.Speed, monsterStats.CritChance, monsterStats.CritDamage);
                for (int round = 0; round < 256; round++)
                {
                    var result = BattleRules.ResolvePlayerVsMonsterRound(player, monster, 4, 1,
                        ((sample * 7 + round * 13) % samples + 0.5) / samples,
                        ((sample * 11 + round * 17) % samples + 0.5) / samples);
                    player = result.Player;
                    monster = result.Monster;
                    if (result.Outcome == BattleOutcome.Ongoing)
                    {
                        monster = monster with { CurrentHp = Math.Min(monster.MaxHp, monster.CurrentHp + Math.Max(0, monsterHealPerRound)) };
                        continue;
                    }
                    if (result.Outcome == BattleOutcome.PlayerWon) wins++;
                    break;
                }
            }
            return wins / (double)samples;
        }
    }
}
