using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class DungeonOfflineSettlementRulesTests
{
    [Fact]
    public void CalculateEstimatedEncounters_UsesExploreProgressThreshold()
    {
        int encounters = DungeonOfflineSettlementRules.CalculateEstimatedEncounters(
            offlineInputBudget: 600,
            dungeonProgressPerInput: 0.2,
            encounterProgressThreshold: 20.0);

        Assert.Equal(6, encounters);
    }

    [Fact]
    public void CalculateWeightedVictoryFactor_RespectsMonsterWeights()
    {
        CharacterStatBlock player = new(100, 14, 5, 100, 0.05, 1.5);
        var monsters = new[]
        {
            new DungeonOfflineSettlementRules.WeightedMonsterProfile(new MonsterStatProfile("m1", "弱怪", new CharacterStatBlock(30, 4, 1, 80, 0.0, 1.5), 10), 80),
            new DungeonOfflineSettlementRules.WeightedMonsterProfile(new MonsterStatProfile("m2", "强怪", new CharacterStatBlock(100, 1000, 4, 120, 0.0, 1.5), 10), 20)
        };

        double factor = DungeonOfflineSettlementRules.CalculateWeightedVictoryFactor(player, monsters);

        Assert.Equal(0.8, factor);
    }

    [Fact]
    public void BuildDungeonOfflineSettlement_CapsEquipmentDropsAndGeneratesRewards()
    {
        CharacterStatBlock player = new(100, 16, 6, 100, 0.1, 1.6);
        var monsters = new[]
        {
            new DungeonOfflineSettlementRules.WeightedMonsterProfile(new MonsterStatProfile("m1", "怪", new CharacterStatBlock(40, 4, 1, 80, 0.0, 1.5), 10), 100)
        };

        ActionSettlementResult result = DungeonOfflineSettlementRules.BuildDungeonOfflineSettlement(
            actionTargetId: "lv_qi_001",
            offlineInputBudget: 600,
            dungeonProgressPerInput: 0.2,
            encounterProgressThreshold: 20.0,
            playerStats: player,
            weightedMonsters: monsters,
            averageLingqiPerVictory: 3.0,
            averageInsightPerVictory: 1.0,
            averageItemDropsPerVictory: new Dictionary<string, double> { ["lingqi_shard"] = 0.5 },
            remainingDailyRolls: int.MaxValue,
            averageDropRollsPerVictory: 1.0,
            equipmentDropCap: 2,
            estimatedEquipmentDropsPerVictory: 0.8,
            createEquipment: index => EquipmentGenerationRules.GenerateFromSpec(
                new("test_sword", "测试剑", EquipmentSlotType.Weapon, "test", EquipmentRarityTier.Artifact,
                    new[] { new EquipmentGenerationRules.EquipmentStatRollDefinition("attack", 1, 2, 3) },
                    Array.Empty<EquipmentGenerationRules.EquipmentStatRollDefinition>(), 2, 3),
                "lv_qi_001", EquipmentSourceStage.Normal, uniqueSuffix: index.ToString()));

        Assert.Equal(PlayerActionState.ActionDungeon, result.ActionId);
        Assert.True(result.ExploreProgressGain > 0.0);
        Assert.True(result.BattleRoundsAdvanced > 0);
        Assert.True(result.LingqiGain > 0.0);
        Assert.True(result.InsightGain > 0.0);
        Assert.True(result.ItemDrops.ContainsKey("lingqi_shard"));
        Assert.Equal(2, result.EquipmentDrops.Count);
        Assert.Equal(100, result.ExploreProgressGain);
        Assert.NotEqual(result.EquipmentDrops[0].EquipmentId, result.EquipmentDrops[1].EquipmentId);
    }

    [Fact]
    public void BuildDungeonOfflineSettlement_TruncatesDropRewardsByRemainingDailyRolls()
    {
        CharacterStatBlock player = new(100, 30, 10, 100, 0.1, 1.6);
        var monsters = new[]
        {
            new DungeonOfflineSettlementRules.WeightedMonsterProfile(new MonsterStatProfile("m1", "怪", new CharacterStatBlock(10, 1, 0, 80, 0.0, 1.5), 10), 100)
        };

        ActionSettlementResult result = DungeonOfflineSettlementRules.BuildDungeonOfflineSettlement(
            actionTargetId: "lv_qi_001",
            offlineInputBudget: 400,
            dungeonProgressPerInput: 0.1,
            encounterProgressThreshold: 10.0,
            playerStats: player,
            weightedMonsters: monsters,
            averageLingqiPerVictory: 5.0,
            averageInsightPerVictory: 2.0,
            averageItemDropsPerVictory: new Dictionary<string, double> { ["lingqi_shard"] = 1.0 },
            remainingDailyRolls: 2,
            averageDropRollsPerVictory: 1.0,
            equipmentDropCap: 5,
            estimatedEquipmentDropsPerVictory: 1.0);

        Assert.Equal(2, result.ItemDrops["lingqi_shard"]);
        Assert.Empty(result.EquipmentDrops);
        Assert.True(result.LingqiGain > 10.0);
        Assert.True(result.BattleRoundsAdvanced > 0);
    }
}
