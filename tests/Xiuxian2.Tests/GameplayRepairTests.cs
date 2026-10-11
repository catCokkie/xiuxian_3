using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class GameplayRepairTests
{
    [Fact]
    public void DungeonInputGrantsExperienceWithoutDoubleDirectExp()
    {
        var result = ResourceConversionRules.Calculate("dungeon", 100, 100, 0, 1, 1, 0);
        Assert.Equal(90, result.Lingqi);
        Assert.Equal(22.5, result.RealmExp);
        var cultivation = ResourceConversionRules.Calculate("cultivation", 100, 100, 0, 1, 1, 0);
        Assert.Equal(35, cultivation.RealmExp);
    }

    [Fact]
    public void RestOnlyBoostsPassiveRecoveryAndMasteryActuallyChangesIncome()
    {
        var normal = ResourceConversionRules.Calculate("cultivation", 100, 100, 60, 1, 1, 0);
        var rest = ResourceConversionRules.Calculate("cultivation", 100, 100, 60, 1, 1, 0, 1.5);
        Assert.Equal(2.5, rest.Lingqi - normal.Lingqi, 6);
        var mastery = ResourceConversionRules.Calculate("cultivation", 100, 100, 60, 1, 2, 0);
        Assert.Equal(9, mastery.Lingqi - normal.Lingqi, 6);
    }

    [Fact]
    public void MovementApCannotExceedSharedWindowBudget()
    {
        Assert.Equal(0, InputActivityRules.ClampAp(10000, 720, 720));
        Assert.Equal(20, InputActivityRules.ClampAp(10000, 700, 720));
        Assert.Equal(0, InputActivityRules.ClampAp(double.PositiveInfinity, 0, 720));
    }

    [Fact]
    public void DefenseReducesDamageAndCritChangesOutput()
    {
        CharacterBattleSnapshot player = new(100, 100, 10, 0, 10, 1, 1.5);
        CharacterBattleSnapshot monster = new(100, 100, 40, 2, 10, 0, 1.5);
        var normal = BattleRules.ResolvePlayerVsMonsterRound(player, monster, 4, 1);
        var armored = BattleRules.ResolvePlayerVsMonsterRound(player with { Defense = 4 }, monster, 4, 1);
        var critical = BattleRules.ResolvePlayerVsMonsterRound(player, monster, 4, 1, 0);
        Assert.Equal(10, normal.DamageToPlayer);
        Assert.Equal(6, armored.DamageToPlayer);
        Assert.Equal(8, normal.DamageToMonster);
        Assert.Equal(12, critical.DamageToMonster);
    }

    [Fact]
    public void FasterLethalAttackPreventsRetaliation()
    {
        CharacterBattleSnapshot player = new(10, 10, 20, 0, 20, 0, 1.5);
        CharacterBattleSnapshot monster = new(10, 10, 20, 0, 10, 0, 1.5);
        var fast = BattleRules.ResolvePlayerVsMonsterRound(player, monster, 1, 1);
        var slow = BattleRules.ResolvePlayerVsMonsterRound(player with { Speed = 5 }, monster, 1, 1);
        Assert.Equal(BattleOutcome.PlayerWon, fast.Outcome);
        Assert.Equal(10, fast.Player.CurrentHp);
        Assert.Equal(BattleOutcome.MonsterWon, slow.Outcome);
    }

    [Fact]
    public void FractionalSpeedIsIndependentOfBatchSize()
    {
        var small = new ProgressRateAccumulator();
        int total = 0;
        for (int i = 0; i < 100; i++) total += small.Apply(1, 1.12);
        Assert.Equal(112, total);
        Assert.Equal(total, new ProgressRateAccumulator().Apply(100, 1.12));
        Assert.Equal(0, small.Apply(0, 1.12));
    }

    [Fact]
    public void UnfundedReadyBatchFreezesAndSuccessfulBatchesRetainOverflow()
    {
        float progress = BatchProgressRules.Advance(90, 250, 100);
        Assert.Equal(340, progress);
        Assert.Equal(340, BatchProgressRules.Advance(progress, 200, 100));
        int completed = 0;
        while (progress >= 100) { progress = BatchProgressRules.Consume(progress, 100); completed++; }
        Assert.Equal(3, completed);
        Assert.Equal(40, progress);
    }

    [Fact]
    public void MajorRealmRequiresFinalRegionBossAndMinorLevelsRemainFree()
    {
        Assert.True(RealmBreakthroughRules.CanCrossRealm(8, Array.Empty<string>()));
        Assert.False(RealmBreakthroughRules.CanCrossRealm(9, new[] { "lv_qi_001" }));
        Assert.True(RealmBreakthroughRules.CanCrossRealm(9, new[] { "lv_qi_005" }));
    }

    [Fact]
    public void OfflineCombatUsesTheSameDefenseAndInitiativeAsOnline()
    {
        CharacterStatBlock player = new(10, 10, 0, 5, 0, 1.5);
        CharacterStatBlock enemy = new(10, 80, 0, 10, 0, 1.5);
        Assert.Equal(0, DungeonOfflineSettlementRules.CalculateMonsterVictoryFactor(player, enemy));
        Assert.Equal(1, DungeonOfflineSettlementRules.CalculateMonsterVictoryFactor(player with { Speed = 20 }, enemy));
        Assert.Equal(1, DungeonOfflineSettlementRules.CalculateMonsterVictoryFactor(player with { Defense = 20 }, enemy));
    }

    [Fact]
    public void ActivityWindowCountsElapsedTimeAcrossFramesAndStopsAfterFiveSeconds()
    {
        double frames = Enumerable.Range(1, 600).Sum(i => InputActivityRules.MeasureActiveSeconds(.01, i * .01, 5));
        Assert.Equal(5, frames, 6);
        Assert.Equal(5, InputActivityRules.MeasureActiveSeconds(6, 6, 5));
        Assert.Equal(0, InputActivityRules.MeasureActiveSeconds(1, 7, 5));
    }

    [Fact]
    public void OfflineHealingEnemiesUseTheirActualRegeneration()
    {
        CharacterStatBlock player = new(100, 3, 0, 10, 0, 1.5);
        CharacterStatBlock enemy = new(20, 4, 0, 10, 0, 1.5);
        Assert.Equal(1, DungeonOfflineSettlementRules.CalculateMonsterVictoryFactor(player, enemy));
        Assert.Equal(0, DungeonOfflineSettlementRules.CalculateMonsterVictoryFactor(player, enemy, 3));
    }

    [Theory]
    [InlineData(1, 4, 3)]
    [InlineData(2, 4, 6)]
    [InlineData(3, 4, 9)]
    [InlineData(4, 1, 3)]
    public void EnhancementUsesBothRarityAndMasteryCaps(int rarity, int mastery, int cap)
        => Assert.Equal(cap, SmithingRules.GetMaxEnhanceLevel(mastery, rarity));

    [Fact]
    public void BodyCultivationCostsIncreaseAndMasteryDiscountsTheCurrentCost()
    {
        Assert.Equal(3, BodyCultivationRules.GetMaterialCount(3, 0, 1));
        Assert.Equal(13, BodyCultivationRules.GetMaterialCount(3, 10, 1));
        Assert.Equal(12, BodyCultivationRules.GetMaterialCount(3, 10, 3));
    }

    [Fact]
    public void FourFishingPondsHaveDistinctDropsAndMasteryRareCapacityBenefits()
    {
        Assert.Equal(4, FishingRules.GetPonds().Count);
        Assert.Equal("armored_fish", FishingRules.CatchFish("fishing_hualong_pond", 4, 0).ItemId);
        Assert.Equal("dragon_saliva", FishingRules.CatchFish("fishing_deep_pond", 3, 60).ItemId);
        Assert.False(FishingRules.TryRollRareCatch("fishing_spirit_fish", 7, false, out _, 1));
        Assert.True(FishingRules.TryRollRareCatch("fishing_spirit_fish", 7, false, out var rare, 4));
        Assert.Equal("spirit_spring_water", rare.ItemId);
        Assert.Equal(FishingRules.Capacity("fishing_spirit_fish", 1) + 10, FishingRules.Capacity("fishing_spirit_fish", 4));
    }

    [Fact]
    public void MiningRareMasteryProducesAnActualRareOre()
    {
        Assert.Null(MiningRules.RollRareOre("mining_cold_iron_ore", 2, 0).ItemId);
        Assert.Equal("spirit_jade", MiningRules.RollRareOre("mining_cold_iron_ore", 3, .05).ItemId);
        Assert.Null(MiningRules.RollRareOre("mining_cold_iron_ore", 3, .1).ItemId);
    }

    [Fact]
    public void OfflineCapabilitiesMatchTheActualSupportedActions()
    {
        foreach (string id in new[] { "mining", "fishing", "body_cultivation" })
            Assert.False(PlayerActionCapabilityRules.HasCapability(id, PlayerActionCapability.SupportsOfflineSettlement));
        foreach (string id in new[] { "dungeon", "cultivation", "alchemy", "smithing", "talisman", "cooking", "formation", "garden" })
            Assert.True(PlayerActionCapabilityRules.HasCapability(id, PlayerActionCapability.SupportsOfflineSettlement));
        Assert.Equal(0, OfflineCraftingRules.InputBudget(86400, "mining"));
        Assert.True(OfflineCraftingRules.InputBudget(86400, "alchemy") > 0);
    }

    [Fact]
    public void SaveMigrationPreservesExistingBonusesAndMergesLegacyFoodExactlyOnce()
    {
        var cfg = new SaveMigrationRules.MigrationStore();
        SaveMigrationRules.MigrateToLatest(cfg, 1);
        cfg.SetValue("meta", "version", 14);
        cfg.SetValue("backpack", "items", new Dictionary<string, object> { ["food_spirit_porridge"] = 2, ["food_clear_fish_soup"] = 3 });
        cfg.SetValue("progress", "player", new Dictionary<string, object> { ["body_cultivation_max_hp_flat"] = 18 });
        Assert.True(SaveMigrationRules.TryMigrateToLatestCopy(cfg, out var migrated, out var changed, out var error));
        Assert.True(changed); Assert.Null(error);
        var items = (Dictionary<string, object>)migrated.GetValue("backpack", "items", null!);
        Assert.Equal(5, items["food_clear_fish_soup"]); Assert.False(items.ContainsKey("food_spirit_porridge"));
        var bonuses = (Dictionary<string, object>)migrated.GetValue("progress", "player", null!);
        Assert.Equal(18, bonuses["body_cultivation_max_hp_flat"]); Assert.Equal(0.0, bonuses["body_cultivation_max_hp_rate"]);
        Assert.True(SaveMigrationRules.TryMigrateToLatestCopy(migrated, out var again, out changed, out error));
        Assert.False(changed); Assert.Equal(5, ((Dictionary<string, object>)again.GetValue("backpack", "items", null!))["food_clear_fish_soup"]);
        Assert.Equal(14, cfg.GetValue("meta", "version", 0));
    }

    [Fact]
    public void EquipmentSamplesEnterRuntimeDropsAndDoNotDuplicateOnReload()
    {
        const string level = "{\"levels\":[{\"level_id\":\"lv1\",\"rewards\":{\"first_clear\":{\"lingqi\":120}}}],\"drop_tables\":[{\"drop_table_id\":\"drop1\",\"entries\":[{\"item_id\":\"herb\",\"weight\":50}]}]}";
        const string equipment = "{\"equipment_templates\":[{\"equipment_template_id\":\"sword\"}],\"drop_table_equipment_examples\":[{\"drop_table_id\":\"drop1\",\"entries\":[{\"equipment_template_id\":\"sword\",\"entry_type\":\"equipment_template\",\"weight\":8}]}],\"first_clear_equipment_examples\":[{\"level_id\":\"lv1\",\"first_clear\":{\"equipment_templates\":[{\"equipment_template_id\":\"sword\"}]}}]}";
        string merged = EquipmentConfigMergeRules.Merge(level, equipment);
        using var doc = System.Text.Json.JsonDocument.Parse(EquipmentConfigMergeRules.Merge(merged, equipment));
        Assert.Equal(2, doc.RootElement.GetProperty("drop_tables")[0].GetProperty("entries").GetArrayLength());
        Assert.Equal(120, doc.RootElement.GetProperty("levels")[0].GetProperty("rewards").GetProperty("first_clear").GetProperty("lingqi").GetInt32());
        Assert.Equal("sword", doc.RootElement.GetProperty("equipment_templates")[0].GetProperty("equipment_template_id").GetString());
    }
}
