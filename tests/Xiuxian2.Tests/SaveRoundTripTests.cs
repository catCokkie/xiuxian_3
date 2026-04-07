using System.Collections.Generic;
using System.Linq;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class SaveRoundTripTests
{
    [Fact]
    public void BackpackPersistenceRules_RoundTripsItemsProfilesAndInstances()
    {
        EquipmentStatProfile legacyProfile = BuildProfile(
            equipmentId: "legacy_profile_001",
            displayName: "Legacy Charm",
            slot: EquipmentSlotType.Accessory,
            modifier: new CharacterStatModifier(CritChanceDelta: 0.03),
            setTag: "legacy_set",
            rarity: 2,
            enhanceLevel: 1,
            isEquipped: false);
        EquipmentInstanceData instance = BuildInstance(
            equipmentId: "instance_weapon_001",
            templateId: "tpl_weapon_001",
            displayName: "Moss Blade",
            slot: EquipmentSlotType.Weapon,
            seriesId: "series_qi_outer",
            rarityTier: EquipmentRarityTier.Spirit,
            sourceStage: EquipmentSourceStage.Elite,
            sourceLevelId: "lv_qi_002",
            mainStatKey: "attack_flat",
            mainStatValue: 11,
            subStats: new[]
            {
                new EquipmentSubStatData("crit_chance_delta", 0.02),
                new EquipmentSubStatData("speed_flat", 3),
            },
            enhanceLevel: 2,
            powerBudget: 21,
            obtainedUnix: 123456789L,
            isEquipped: false);
        BackpackPersistenceRules.BackpackSnapshot snapshot = new(
            Items: new Dictionary<string, int>
            {
                ["spirit_herb"] = 5,
                ["ore_copper"] = 2,
            },
            EquipmentProfiles: new[]
            {
                legacyProfile,
                EquipmentInstanceRules.ToStatProfile(instance) with { IsEquipped = false },
            },
            EquipmentInstances: new[] { instance });

        Dictionary<string, object> data = BackpackPersistenceRules.ToPlainDictionary(snapshot);
        BackpackPersistenceRules.BackpackSnapshot restored = BackpackPersistenceRules.FromPlainDictionary(data);

        Assert.Equal(5, restored.Items["spirit_herb"]);
        Assert.Equal(2, restored.Items["ore_copper"]);
        Assert.True(data.ContainsKey("__equipment_profiles"));
        Assert.True(data.ContainsKey("__equipment_instances"));
        Assert.Equal(2, restored.EquipmentProfiles.Count);
        Assert.Single(restored.EquipmentInstances);

        EquipmentStatProfile restoredLegacyProfile = restored.EquipmentProfiles.Single(x => x.EquipmentId == "legacy_profile_001");
        EquipmentStatProfile restoredInstanceProjection = restored.EquipmentProfiles.Single(x => x.EquipmentId == "instance_weapon_001");
        EquipmentInstanceData restoredInstance = restored.EquipmentInstances.Single();

        Assert.Equal("Legacy Charm", restoredLegacyProfile.DisplayName);
        Assert.Equal(0.03, restoredLegacyProfile.Modifier.CritChanceDelta, 6);
        Assert.False(restoredLegacyProfile.IsEquipped);
        Assert.Equal(11, restoredInstanceProjection.Modifier.AttackFlat);
        Assert.Equal(3, restoredInstanceProjection.Modifier.SpeedFlat);
        Assert.False(restoredInstanceProjection.IsEquipped);
        AssertEquipmentInstanceEqual(instance, restoredInstance);
    }

    [Fact]
    public void ResourceWalletPersistenceRules_RoundTripsCurrentAndLifetimeValues()
    {
        ResourceWalletPersistenceRules.WalletSnapshot expected = new(
            Lingqi: 100.5,
            Insight: 14.5,
            SpiritStones: 12,
            TotalEarnedLingqi: 120.5,
            TotalEarnedInsight: 18.0,
            TotalEarnedSpiritStones: 16);

        Dictionary<string, object> data = ResourceWalletPersistenceRules.ToPlainDictionary(expected);
        ResourceWalletPersistenceRules.WalletSnapshot restored = ResourceWalletPersistenceRules.FromPlainDictionary(data);

        Assert.Equal(100.5, restored.Lingqi, 6);
        Assert.Equal(14.5, restored.Insight, 6);
        Assert.Equal(12, restored.SpiritStones);
        Assert.Equal(120.5, restored.TotalEarnedLingqi, 6);
        Assert.Equal(18.0, restored.TotalEarnedInsight, 6);
        Assert.Equal(16, restored.TotalEarnedSpiritStones);
    }

    [Fact]
    public void PlayerProgressPersistenceRules_RoundTripsRealmProgressMoodAndUnlocks()
    {
        PlayerProgressPersistenceRules.PlayerProgressSnapshot expected = new(
            RealmLevel: 2,
            RealmExp: 37.5,
            AdvancedAlchemyStudyUnlocked: true,
            CurrentRealmActiveSeconds: 123.4,
            BodyCultivationMaxHpFlat: 10,
            BodyCultivationAttackFlat: 4,
            BodyCultivationDefenseFlat: 3,
            ZhouTianMaxHpRate: 0.004,
            ZhouTianAttackRate: 0.006,
            ZhouTianDefenseRate: 0.002);

        Dictionary<string, object> data = PlayerProgressPersistenceRules.ToPlainDictionary(expected);
        PlayerProgressPersistenceRules.PlayerProgressSnapshot restored = PlayerProgressPersistenceRules.FromPlainDictionary(data);

        Assert.Equal(2, restored.RealmLevel);
        Assert.Equal(37.5, restored.RealmExp, 6);
        Assert.True(restored.AdvancedAlchemyStudyUnlocked);
        Assert.Equal(123.4, restored.CurrentRealmActiveSeconds, 6);
        Assert.Equal(10, restored.BodyCultivationMaxHpFlat);
        Assert.Equal(4, restored.BodyCultivationAttackFlat);
        Assert.Equal(3, restored.BodyCultivationDefenseFlat);
        Assert.Equal(0.004, restored.ZhouTianMaxHpRate, 6);
        Assert.Equal(0.006, restored.ZhouTianAttackRate, 6);
        Assert.Equal(0.002, restored.ZhouTianDefenseRate, 6);
    }

    [Fact]
    public void CultivationRhythmPersistenceRules_RoundTripsSettingsProgressAndRestState()
    {
        CultivationRhythmPersistenceRules.RhythmSnapshot expected = new(
            Enabled: true,
            Strength: CultivationRhythmRules.StrengthStrong,
            CycleMinutes: 45,
            CurrentCycleActiveSeconds: 780.0,
            SmallCycleCount: 2,
            TotalSmallCycles: 6,
            TotalGrandCycles: 1,
            RestRemainingSeconds: 120.0,
            IsGrandRest: true,
            TotalRestCount: 6,
            TotalMeditationInsights: 1);

        Dictionary<string, object> data = CultivationRhythmPersistenceRules.ToPlainDictionary(expected);
        CultivationRhythmPersistenceRules.RhythmSnapshot restored = CultivationRhythmPersistenceRules.FromPlainDictionary(data);

        Assert.Equal(expected.Enabled, restored.Enabled);
        Assert.Equal(expected.Strength, restored.Strength);
        Assert.Equal(expected.CycleMinutes, restored.CycleMinutes);
        Assert.Equal(expected.CurrentCycleActiveSeconds, restored.CurrentCycleActiveSeconds, 6);
        Assert.Equal(expected.SmallCycleCount, restored.SmallCycleCount);
        Assert.Equal(expected.TotalSmallCycles, restored.TotalSmallCycles);
        Assert.Equal(expected.TotalGrandCycles, restored.TotalGrandCycles);
        Assert.Equal(expected.RestRemainingSeconds, restored.RestRemainingSeconds, 6);
        Assert.Equal(expected.IsGrandRest, restored.IsGrandRest);
        Assert.Equal(expected.TotalRestCount, restored.TotalRestCount);
        Assert.Equal(expected.TotalMeditationInsights, restored.TotalMeditationInsights);
    }

    [Fact]
    public void PlayerStatsPersistenceRules_RoundTripsLifetimeCounters()
    {
        PlayerStatsPersistenceRules.PlayerStatsSnapshot expected = new(
            TotalBattleLosses: 4,
            TotalBossBattles: 7,
            TotalEliteBattles: 9,
            TotalAlchemyCrafts: 12,
            TotalSmithingCrafts: 6,
            TotalTalismanCrafts: 8,
            TotalCookingCrafts: 5,
            TotalFormationCrafts: 3,
            TotalMiningCompletions: 14,
            TotalFishingCompletions: 11,
            TotalGardenPlants: 15,
            TotalGardenHarvests: 13,
            TotalGardenAutoHarvests: 4,
            TotalSpentSpiritStones: 220,
            TotalSpentInsight: 88.5);

        Dictionary<string, object> data = PlayerStatsPersistenceRules.ToPlainDictionary(expected);
        PlayerStatsPersistenceRules.PlayerStatsSnapshot restored = PlayerStatsPersistenceRules.FromPlainDictionary(data);

        Assert.Equal(expected.TotalBattleLosses, restored.TotalBattleLosses);
        Assert.Equal(expected.TotalBossBattles, restored.TotalBossBattles);
        Assert.Equal(expected.TotalEliteBattles, restored.TotalEliteBattles);
        Assert.Equal(expected.TotalAlchemyCrafts, restored.TotalAlchemyCrafts);
        Assert.Equal(expected.TotalSmithingCrafts, restored.TotalSmithingCrafts);
        Assert.Equal(expected.TotalTalismanCrafts, restored.TotalTalismanCrafts);
        Assert.Equal(expected.TotalCookingCrafts, restored.TotalCookingCrafts);
        Assert.Equal(expected.TotalFormationCrafts, restored.TotalFormationCrafts);
        Assert.Equal(expected.TotalMiningCompletions, restored.TotalMiningCompletions);
        Assert.Equal(expected.TotalFishingCompletions, restored.TotalFishingCompletions);
        Assert.Equal(expected.TotalGardenPlants, restored.TotalGardenPlants);
        Assert.Equal(expected.TotalGardenHarvests, restored.TotalGardenHarvests);
        Assert.Equal(expected.TotalGardenAutoHarvests, restored.TotalGardenAutoHarvests);
        Assert.Equal(expected.TotalSpentSpiritStones, restored.TotalSpentSpiritStones);
        Assert.Equal(expected.TotalSpentInsight, restored.TotalSpentInsight, 6);
    }

    [Fact]
    public void ShopPersistenceRules_RoundTripsPurchasesDailyStateAndActiveBuff()
    {
        ShopPersistenceRules.ShopSnapshot expected = new(
            LifetimePurchases: new Dictionary<string, int>
            {
                [ShopRules.ItemBackpackExpand1] = 1,
                [ShopRules.ItemBreakthroughPill] = 1,
            },
            DailyPurchases: new Dictionary<string, int>
            {
                [ShopRules.ItemDoubleYield] = 2,
                [ShopRules.ItemPageFragment] = 1,
            },
            DailyResetDate: "2026-04-06",
            ActiveDoubleYieldSeconds: 1800.0);

        Dictionary<string, object> data = ShopPersistenceRules.ToPlainDictionary(expected);
        ShopPersistenceRules.ShopSnapshot restored = ShopPersistenceRules.FromPlainDictionary(data);

        Assert.Equal(1, restored.LifetimePurchases[ShopRules.ItemBackpackExpand1]);
        Assert.Equal(1, restored.LifetimePurchases[ShopRules.ItemBreakthroughPill]);
        Assert.Equal(2, restored.DailyPurchases[ShopRules.ItemDoubleYield]);
        Assert.Equal(1, restored.DailyPurchases[ShopRules.ItemPageFragment]);
        Assert.Equal("2026-04-06", restored.DailyResetDate);
        Assert.Equal(1800.0, restored.ActiveDoubleYieldSeconds, 6);
    }

    [Fact]
    public void EquippedItemsPersistenceRules_RoundTripsProfilesBySlot()
    {
        EquipmentStatProfile weapon = BuildProfile(
            equipmentId: "weapon_eq_001",
            displayName: "Storm Saber",
            slot: EquipmentSlotType.Weapon,
            modifier: new CharacterStatModifier(AttackFlat: 7, CritDamageDelta: 0.15),
            setTag: "storm_set",
            rarity: 3,
            enhanceLevel: 2,
            isEquipped: true);
        EquipmentStatProfile armor = BuildProfile(
            equipmentId: "armor_eq_001",
            displayName: "Stone Armor",
            slot: EquipmentSlotType.Armor,
            modifier: new CharacterStatModifier(MaxHpFlat: 25, DefenseFlat: 4),
            setTag: "stone_set",
            rarity: 2,
            enhanceLevel: 1,
            isEquipped: true);

        Dictionary<string, object> data = EquippedItemsPersistenceRules.ToPlainDictionary(
            new Dictionary<EquipmentSlotType, EquipmentStatProfile>
            {
                [EquipmentSlotType.Weapon] = weapon,
                [EquipmentSlotType.Armor] = armor,
            });
        Dictionary<EquipmentSlotType, EquipmentStatProfile> restored = EquippedItemsPersistenceRules.FromPlainDictionary(data);

        Assert.True(restored.TryGetValue(EquipmentSlotType.Weapon, out EquipmentStatProfile restoredWeapon));
        Assert.True(restored.TryGetValue(EquipmentSlotType.Armor, out EquipmentStatProfile restoredArmor));
        AssertEquipmentProfileEqual(weapon, restoredWeapon);
        AssertEquipmentProfileEqual(armor, restoredArmor);
    }

    [Fact]
    public void SubsystemMasteryPersistenceRules_RoundTripsMasteryLevelsAndDefaultsMissingSystems()
    {
        Dictionary<string, int> levels = new()
        {
            [PlayerActionState.ModeDungeon] = 4,
            [PlayerActionState.ModeAlchemy] = 3,
        };

        Dictionary<string, object> data = SubsystemMasteryPersistenceRules.ToPlainDictionary(levels);
        Dictionary<string, int> restored = SubsystemMasteryPersistenceRules.FromPlainDictionary(data);

        Assert.Equal(4, restored[PlayerActionState.ModeDungeon]);
        Assert.Equal(3, restored[PlayerActionState.ModeAlchemy]);
        Assert.Equal(1, restored[PlayerActionState.ModeFishing]);
        Assert.Equal(SubsystemMasteryRules.GetAllSystemIds().Count, restored.Count);
    }

    [Fact]
    public void GatheringPersistenceRules_RoundTripGardenMiningAndFishingState()
    {
        GardenPersistenceRules.GardenPlotSnapshot[] plots = GardenPersistenceRules.CreateEmptyPlots();
        plots[0] = new GardenPersistenceRules.GardenPlotSnapshot("garden_spirit_flower", 123456789L, false);
        plots[2] = new GardenPersistenceRules.GardenPlotSnapshot("garden_spirit_herb", 123456999L, true);
        GardenPersistenceRules.GardenSnapshot garden = new("garden_spirit_flower", 2, plots);
        MiningPersistenceRules.MiningSnapshot mining = new("mining_spirit_jade", 42.0f, 220.0f, 73);
        FishingPersistenceRules.FishingSnapshot fishing = new("fishing_deep_pond", 95.0f, 240.0f);

        GardenPersistenceRules.GardenSnapshot restoredGarden = GardenPersistenceRules.FromPlainDictionary(GardenPersistenceRules.ToPlainDictionary(garden));
        MiningPersistenceRules.MiningSnapshot restoredMining = MiningPersistenceRules.FromPlainDictionary(MiningPersistenceRules.ToPlainDictionary(mining));
        FishingPersistenceRules.FishingSnapshot restoredFishing = FishingPersistenceRules.FromPlainDictionary(FishingPersistenceRules.ToPlainDictionary(fishing));

        Assert.Equal(garden.SelectedRecipeId, restoredGarden.SelectedRecipeId);
        Assert.Equal(garden.SelectedPlotIndex, restoredGarden.SelectedPlotIndex);
        Assert.Equal(GardenRules.MaxPlotCount, restoredGarden.Plots.Length);
        Assert.Equal("garden_spirit_flower", restoredGarden.Plots[0].CropId);
        Assert.Equal(123456789L, restoredGarden.Plots[0].PlantedAtUnix);
        Assert.False(restoredGarden.Plots[0].IsReady);
        Assert.Equal("garden_spirit_herb", restoredGarden.Plots[2].CropId);
        Assert.Equal(123456999L, restoredGarden.Plots[2].PlantedAtUnix);
        Assert.True(restoredGarden.Plots[2].IsReady);
        Assert.Equal(mining, restoredMining);
        Assert.Equal(fishing, restoredFishing);
    }

    [Fact]
    public void FormationPersistenceRules_RoundTripsCraftedInventoryActiveSlotsAndProgress()
    {
        FormationPersistenceRules.FormationSnapshot formation = new(
            SelectedRecipeId: "formation_guard_flag",
            Progress: 64.0f,
            RequiredProgress: 220.0f,
            ActivePrimaryId: "formation_spirit_plate",
            ActiveSecondaryId: "formation_guard_flag",
            CraftedIds: new[] { "formation_spirit_plate", "formation_guard_flag" },
            Inventory: new Dictionary<string, int>
            {
                ["formation_spirit_plate"] = 2,
                ["formation_guard_flag"] = 1,
            });

        FormationPersistenceRules.FormationSnapshot restored = FormationPersistenceRules.FromPlainDictionary(
            FormationPersistenceRules.ToPlainDictionary(formation));

        Assert.Equal(formation.SelectedRecipeId, restored.SelectedRecipeId);
        Assert.Equal(formation.Progress, restored.Progress);
        Assert.Equal(formation.RequiredProgress, restored.RequiredProgress);
        Assert.Equal(formation.ActivePrimaryId, restored.ActivePrimaryId);
        Assert.Equal(formation.ActiveSecondaryId, restored.ActiveSecondaryId);
        Assert.Equal(formation.CraftedIds, restored.CraftedIds);
        Assert.Equal(2, restored.Inventory["formation_spirit_plate"]);
        Assert.Equal(1, restored.Inventory["formation_guard_flag"]);
    }

    [Fact]
    public void FormationPersistenceRules_DefaultsEmptyStructureWhenFieldsMissing()
    {
        FormationPersistenceRules.FormationSnapshot restored = FormationPersistenceRules.FromPlainDictionary(
            new Dictionary<string, object>());

        Assert.Equal(string.Empty, restored.SelectedRecipeId);
        Assert.Equal(0.0f, restored.Progress);
        Assert.Equal(100.0f, restored.RequiredProgress);
        Assert.Equal(string.Empty, restored.ActivePrimaryId);
        Assert.Equal(string.Empty, restored.ActiveSecondaryId);
        Assert.Empty(restored.CraftedIds);
        Assert.Empty(restored.Inventory);
    }

    [Fact]
    public void EquipmentInstanceCodec_RoundTripsAllFields()
    {
        EquipmentInstanceData expected = BuildInstance(
            equipmentId: "instance_accessory_001",
            templateId: "tpl_accessory_001",
            displayName: "Moon Pendant",
            slot: EquipmentSlotType.Accessory,
            seriesId: "series_moon",
            rarityTier: EquipmentRarityTier.Treasure,
            sourceStage: EquipmentSourceStage.Boss,
            sourceLevelId: "lv_qi_005",
            mainStatKey: "crit_chance_delta",
            mainStatValue: 0.08,
            subStats: new[]
            {
                new EquipmentSubStatData("attack_flat", 6),
                new EquipmentSubStatData("crit_damage_delta", 0.20),
            },
            enhanceLevel: 4,
            powerBudget: 35,
            obtainedUnix: 99887766L,
            isEquipped: true);

        EquipmentInstanceData restored = EquipmentInstanceCodec.FromPlainDictionary(
            EquipmentInstanceCodec.ToPlainDictionary(expected));

        AssertEquipmentInstanceEqual(expected, restored);
    }

    [Fact]
    public void EquipmentProfileCodec_RoundTripsAllFields()
    {
        EquipmentStatProfile expected = BuildProfile(
            equipmentId: "profile_armor_001",
            displayName: "Azure Robe",
            slot: EquipmentSlotType.Armor,
            modifier: new CharacterStatModifier(
                MaxHpFlat: 18,
                DefenseFlat: 5,
                MaxHpRate: 0.12,
                DefenseRate: 0.08,
                CritChanceDelta: 0.01),
            setTag: "azure_set",
            rarity: 4,
            enhanceLevel: 3,
            isEquipped: false);

        EquipmentStatProfile restored = EquipmentProfileCodec.FromPlainDictionary(
            EquipmentProfileCodec.ToPlainDictionary(expected));

        AssertEquipmentProfileEqual(expected, restored);
    }

    private static EquipmentStatProfile BuildProfile(
        string equipmentId,
        string displayName,
        EquipmentSlotType slot,
        CharacterStatModifier modifier,
        string setTag,
        int rarity,
        int enhanceLevel,
        bool isEquipped)
    {
        return new EquipmentStatProfile(
            EquipmentId: equipmentId,
            DisplayName: displayName,
            Slot: slot,
            Modifier: modifier,
            SetTag: setTag,
            Rarity: rarity,
            EnhanceLevel: enhanceLevel,
            IsEquipped: isEquipped);
    }

    private static EquipmentInstanceData BuildInstance(
        string equipmentId,
        string templateId,
        string displayName,
        EquipmentSlotType slot,
        string seriesId,
        EquipmentRarityTier rarityTier,
        EquipmentSourceStage sourceStage,
        string sourceLevelId,
        string mainStatKey,
        double mainStatValue,
        EquipmentSubStatData[] subStats,
        int enhanceLevel,
        int powerBudget,
        long obtainedUnix,
        bool isEquipped)
    {
        return new EquipmentInstanceData(
            EquipmentId: equipmentId,
            EquipmentTemplateId: templateId,
            DisplayName: displayName,
            Slot: slot,
            SeriesId: seriesId,
            RarityTier: rarityTier,
            SourceStage: sourceStage,
            SourceLevelId: sourceLevelId,
            MainStatKey: mainStatKey,
            MainStatValue: mainStatValue,
            SubStats: subStats,
            EnhanceLevel: enhanceLevel,
            PowerBudget: powerBudget,
            ObtainedUnix: obtainedUnix,
            IsEquipped: isEquipped);
    }

    private static void AssertEquipmentProfileEqual(EquipmentStatProfile expected, EquipmentStatProfile actual)
    {
        Assert.Equal(expected.EquipmentId, actual.EquipmentId);
        Assert.Equal(expected.DisplayName, actual.DisplayName);
        Assert.Equal(expected.Slot, actual.Slot);
        Assert.Equal(expected.Modifier, actual.Modifier);
        Assert.Equal(expected.SetTag, actual.SetTag);
        Assert.Equal(expected.Rarity, actual.Rarity);
        Assert.Equal(expected.EnhanceLevel, actual.EnhanceLevel);
        Assert.Equal(expected.IsEquipped, actual.IsEquipped);
    }

    private static void AssertEquipmentInstanceEqual(EquipmentInstanceData expected, EquipmentInstanceData actual)
    {
        Assert.Equal(expected.EquipmentId, actual.EquipmentId);
        Assert.Equal(expected.EquipmentTemplateId, actual.EquipmentTemplateId);
        Assert.Equal(expected.DisplayName, actual.DisplayName);
        Assert.Equal(expected.Slot, actual.Slot);
        Assert.Equal(expected.SeriesId, actual.SeriesId);
        Assert.Equal(expected.RarityTier, actual.RarityTier);
        Assert.Equal(expected.SourceStage, actual.SourceStage);
        Assert.Equal(expected.SourceLevelId, actual.SourceLevelId);
        Assert.Equal(expected.MainStatKey, actual.MainStatKey);
        Assert.Equal(expected.MainStatValue, actual.MainStatValue, 6);
        Assert.Equal(expected.EnhanceLevel, actual.EnhanceLevel);
        Assert.Equal(expected.PowerBudget, actual.PowerBudget);
        Assert.Equal(expected.ObtainedUnix, actual.ObtainedUnix);
        Assert.Equal(expected.IsEquipped, actual.IsEquipped);

        EquipmentSubStatData[] expectedSubStats = expected.SubStats.ToArray();
        EquipmentSubStatData[] actualSubStats = actual.SubStats.ToArray();
        Assert.Equal(expectedSubStats.Length, actualSubStats.Length);
        for (int i = 0; i < expectedSubStats.Length; i++)
        {
            Assert.Equal(expectedSubStats[i].Stat, actualSubStats[i].Stat);
            Assert.Equal(expectedSubStats[i].Value, actualSubStats[i].Value, 6);
        }
    }

    [Fact]
    public void EquipmentProfileCodec_FromEmptyDictionary_ReturnsDefaults()
    {
        var emptyData = new Dictionary<string, object>();
        EquipmentStatProfile profile = EquipmentProfileCodec.FromPlainDictionary(emptyData);

        Assert.Equal(string.Empty, profile.EquipmentId);
        Assert.Equal(string.Empty, profile.DisplayName);
        Assert.Equal(0, profile.Modifier.AttackFlat);
        Assert.Equal(0.0, profile.Modifier.AttackRate);
    }

    [Fact]
    public void EquipmentProfileCodec_FromMissingModifier_ReturnsDefaultModifier()
    {
        var data = new Dictionary<string, object>
        {
            ["equipment_id"] = "test_sword",
            ["display_name"] = "Test Sword",
            ["slot"] = "Weapon",
            ["set_tag"] = "",
            ["rarity"] = 1,
            ["enhance_level"] = 0,
        };
        EquipmentStatProfile profile = EquipmentProfileCodec.FromPlainDictionary(data);

        Assert.Equal("test_sword", profile.EquipmentId);
        Assert.Equal(0, profile.Modifier.AttackFlat);
        Assert.Equal(0.0, profile.Modifier.DefenseRate);
    }
}
