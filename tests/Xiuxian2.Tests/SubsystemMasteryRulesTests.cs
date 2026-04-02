using System.Linq;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class SubsystemMasteryRulesTests
{
    [Fact]
    public void Definitions_Cover12SystemsAcross4Levels()
    {
        var all = SubsystemMasteryRules.GetAllDefinitions();

        Assert.Equal(44, all.Count);
        Assert.Equal(11, all.Select(def => def.SystemId).Distinct().Count());
        Assert.All(all.GroupBy(def => def.SystemId), group =>
        {
            Assert.Equal(new[] { 1, 2, 3, 4 }, group.Select(def => def.Level).OrderBy(level => level).ToArray());
        });
    }

    [Fact]
    public void GetCurrentLevel_ReturnsOneByDefault()
    {
        Assert.Equal(1, SubsystemMasteryRules.GetCurrentLevel(PlayerActionState.ModeAlchemy, null));
    }

    [Fact]
    public void TryGetDefinition_ReturnsSystemDefinitionByLevel()
    {
        bool found = SubsystemMasteryRules.TryGetDefinition(PlayerActionState.ModeSmithing, 3, out var definition);

        Assert.True(found);
        Assert.Equal(PlayerActionState.ModeSmithing, definition.SystemId);
        Assert.Equal(3, definition.Level);
        Assert.Equal("smithing_material_discount", definition.EffectId);
    }

    [Fact]
    public void CanUnlock_RequiresEnoughInsightAndRealm()
    {
        Assert.True(SubsystemMasteryRules.CanUnlock(PlayerActionState.ModeAlchemy, 1, 20.0, 1));
        Assert.False(SubsystemMasteryRules.CanUnlock(PlayerActionState.ModeAlchemy, 1, 19.0, 1));
        Assert.False(SubsystemMasteryRules.CanUnlock(PlayerActionState.ModeAlchemy, 2, 45.0, 1));
        Assert.False(SubsystemMasteryRules.CanUnlock(PlayerActionState.ModeAlchemy, 4, 999.0, 99));
    }

    [Fact]
    public void TryUnlock_AdvancesLevelAndDeductsInsight()
    {
        bool unlocked = SubsystemMasteryRules.TryUnlock(PlayerActionState.ModeDungeon, 1, 30.0, 2, out int nextLevel, out double remainingInsight);

        Assert.True(unlocked);
        Assert.Equal(2, nextLevel);
        Assert.Equal(0.0, remainingInsight);
    }

    [Fact]
    public void TryUnlock_ReturnsFalseAtMaxLevel()
    {
        bool unlocked = SubsystemMasteryRules.TryUnlock(PlayerActionState.ModeDungeon, 4, 999.0, 99, out int nextLevel, out double remainingInsight);

        Assert.False(unlocked);
        Assert.Equal(4, nextLevel);
        Assert.Equal(999.0, remainingInsight);
    }

    [Fact]
    public void GetEffectValue_ReturnsDungeonBossWeaknessReductionAtLevel4()
    {
        double effect = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeDungeon, 4, SubsystemMasteryRules.DungeonBossWeaknessEffectId);

        Assert.Equal(0.10, effect);
    }

    [Fact]
    public void GetEffectValue_ReturnsCultivationLingqiBonusAtLevel2()
    {
        double effect = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeCultivation, 2, SubsystemMasteryRules.CultivationLingqiBonusEffectId);

        Assert.Equal(0.10, effect);
    }

    [Fact]
    public void GetEffectValue_ReturnsAlchemyUnlockAtLevel2()
    {
        double effect = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeAlchemy, 2, "alchemy_unlock_juling_san");

        Assert.Equal(1.0, effect);
    }

    [Fact]
    public void GetEffectValue_ReturnsSmithingMaxEnhanceAtLevel4()
    {
        double effect = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeSmithing, 4, "smithing_max_enhance_level");

        Assert.Equal(9.0, effect);
    }

    [Fact]
    public void GetEffectValue_ReturnsGardenGrowthSpeedBonusAtLevel2()
    {
        double effect = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeGarden, 2, SubsystemMasteryRules.GardenGrowthSpeedBonusEffectId);

        Assert.Equal(0.15, effect);
    }

    [Fact]
    public void GetEffectValue_ReturnsMiningDurabilityBonusAtLevel2()
    {
        double effect = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeMining, 2, SubsystemMasteryRules.MiningDurabilityBonusEffectId);

        Assert.Equal(0.20, effect);
    }

    [Fact]
    public void GetEffectValue_ReturnsFishingSpeedBonusAtLevel2()
    {
        double effect = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeFishing, 2, SubsystemMasteryRules.FishingSpeedBonusEffectId);

        Assert.Equal(0.15, effect);
    }

    [Fact]
    public void GetEffectValue_ReturnsTalismanDoubleOutputAtLevel2()
    {
        double effect = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeTalisman, 2, SubsystemMasteryRules.TalismanDoubleOutputEffectId);

        Assert.Equal(0.10, effect);
    }

    [Fact]
    public void GetEffectValue_ReturnsCookingDurationBonusAtLevel2()
    {
        double effect = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeCooking, 2, SubsystemMasteryRules.CookingDurationBonusEffectId);

        Assert.Equal(1.0, effect);
    }

    [Fact]
    public void GetEffectValue_ReturnsFormationEffectBonusAtLevel2()
    {
        double effect = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeFormation, 2, SubsystemMasteryRules.FormationEffectBonusEffectId);

        Assert.Equal(0.10, effect);
    }

    [Fact]
    public void GetEffectValue_ReturnsBodyCultivationTemperCapBonusAtLevel3()
    {
        double effect = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeBodyCultivation, 3, SubsystemMasteryRules.BodyCultivationTemperCapBonusEffectId);

        Assert.Equal(10.0, effect);
    }

    [Fact]
    public void TotalUnlockCost_AcrossAll12Systems_MatchesCurrentRegistryBudget()
    {
        double total = SubsystemMasteryRules.GetAllDefinitions().Sum(def => def.InsightCost);

        Assert.Equal(1900.0, total);
    }
}
