using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class EquipmentInheritanceRulesTests
{
    private static readonly EquipmentStatProfile Source = new("old", "旧剑", EquipmentSlotType.Weapon, new(AttackFlat: 5), Rarity: 3, EnhanceLevel: 6);
    private static readonly EquipmentStatProfile Target = new("new", "新剑", EquipmentSlotType.Weapon, new(AttackFlat: 8), Rarity: 1, IsEquipped: false);
    [Theory]
    [InlineData(0, 0)] [InlineData(1, 8)] [InlineData(2, 15)] [InlineData(3, 23)]
    [InlineData(4, 43)] [InlineData(5, 63)] [InlineData(6, 83)] [InlineData(7, 120)] [InlineData(9, 195)]
    public void PriceUsesCumulativeBaseQiAndRoundsUp(int level, double price)
        => Assert.Equal(price, EquipmentInheritanceRules.GetCost(level));

    [Theory]
    [InlineData(2, 2, true, false)] [InlineData(3, 1, true, false)]
    [InlineData(3, 2, false, false)] [InlineData(3, 2, true, true)]
    public void GatesRequireRealmMasteryFirstClearAndNoBattle(int realm, int mastery, bool cleared, bool battle)
        => Assert.False(EquipmentInheritanceRules.Evaluate(Source, Target, realm, mastery, cleared, 1000, battle).CanCommit);

    [Theory]
    [InlineData(1, 2, 3)] [InlineData(2, 2, 6)] [InlineData(3, 4, 6)]
    public void ResultReplacesExistingLevelAndPreservesRolls(int rarity, int mastery, int result)
    {
        var check = EquipmentInheritanceRules.Evaluate(Source, Target with { Rarity = rarity, EnhanceLevel = 2 }, 3, mastery, true, 83);
        Assert.True(check.CanCommit);
        Assert.Equal(result, check.ResultLevel);
        Assert.Equal(83, check.LingqiCost); // Cap loss does not discount the source's fee.
        Assert.Equal(Target.Modifier, check.Result.Modifier);
        Assert.Equal(Target.EquipmentId, check.Result.EquipmentId);
        Assert.True(check.Result.IsEquipped);
    }

    [Fact]
    public void InvalidPairsAndInsufficientFundsNeverPass()
    {
        var pairs = new[]
        {
            (default(EquipmentStatProfile), Target), (Source, default(EquipmentStatProfile)),
            (Source, Target with { EquipmentId = Source.EquipmentId }),
            (Source, Target with { Slot = EquipmentSlotType.Armor }),
            (Source with { EnhanceLevel = 0 }, Target),
            (Source, Target with { EnhanceLevel = 3 }),
            (Source with { IsEquipped = false }, Target), (Source, Target with { IsEquipped = true }),
        };
        foreach (var pair in pairs) Assert.False(EquipmentInheritanceRules.Evaluate(pair.Item1, pair.Item2, 3, 2, true, 1000).CanCommit);
        foreach (double qi in new[] { 82.99, double.NaN, double.PositiveInfinity })
            Assert.False(EquipmentInheritanceRules.Evaluate(Source, Target, 3, 2, true, qi).CanCommit);
    }

    [Fact]
    public void V23MigrationKeepsLegacyGearWithoutInventingSourceRecords()
    {
        var store = new SaveMigrationRules.MigrationStore();
        var gear = EquippedItemsPersistenceRules.ToPlainDictionary(new Dictionary<EquipmentSlotType, EquipmentStatProfile> { [Source.Slot] = Source });
        store.SetValue("equipment", "equipped", gear);
        SaveMigrationRules.MigrateToLatest(store, 23);
        var saved = (Dictionary<string, object>)store.GetValue("equipment", "equipped", null!);
        Assert.Empty((List<object>)saved["__instances"]);
        Assert.Equal(Source, EquippedItemsPersistenceRules.FromPlainDictionary(saved)[Source.Slot]);
        Assert.Equal(SaveMigrationRules.LatestVersion, store.GetValue("meta", "version", 0));
    }

    [Fact]
    public void V23MigrationCopyPreservesSourceAndExistingMetadataAndIsIdempotent()
    {
        var store = new SaveMigrationRules.MigrationStore(); store.SetValue("meta", "version", 23);
        var gear = EquippedItemsPersistenceRules.ToPlainDictionary(new Dictionary<EquipmentSlotType, EquipmentStatProfile> { [Source.Slot] = Source });
        store.SetValue("equipment", "equipped", gear);
        Assert.True(SaveMigrationRules.TryMigrateToLatestCopy(store, out var copy, out bool migrated, out _));
        Assert.True(migrated);
        Assert.False(gear.ContainsKey("__instances"));
        Assert.Equal(23, store.GetValue("meta", "version", 0));
        var saved = (Dictionary<string, object>)copy.GetValue("equipment", "equipped", null!);
        saved["__instances"] = new List<object> { new Dictionary<string, object> { ["equipment_id"] = "known" } };
        SaveMigrationRules.MigrateToLatest(copy, 23);
        SaveMigrationRules.MigrateToLatest(copy, 24);
        Assert.Single((List<object>)saved["__instances"]);
        Assert.Equal(Source, EquippedItemsPersistenceRules.FromPlainDictionary(saved)[Source.Slot]);
        Assert.False(SaveMigrationRules.NeedsMigration(SaveMigrationRules.LatestVersion));
    }
}
