using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ProgressiveUnlockRulesTests
{
    [Fact]
    public void ActionModes_UnlockByRealmWave()
    {
        Assert.Equal(
            new[]
            {
                PlayerActionState.ModeDungeon,
                PlayerActionState.ModeCultivation,
                PlayerActionState.ModeAlchemy,
            },
            ProgressiveUnlockRules.GetUnlockedActionModes(1));

        Assert.Equal(
            new[]
            {
                PlayerActionState.ModeDungeon,
                PlayerActionState.ModeCultivation,
                PlayerActionState.ModeAlchemy,
                PlayerActionState.ModeGarden,
                PlayerActionState.ModeMining,
            },
            ProgressiveUnlockRules.GetUnlockedActionModes(2));

        Assert.Equal(
            new[]
            {
                PlayerActionState.ModeDungeon,
                PlayerActionState.ModeCultivation,
                PlayerActionState.ModeAlchemy,
                PlayerActionState.ModeGarden,
                PlayerActionState.ModeMining,
                PlayerActionState.ModeSmithing,
                PlayerActionState.ModeFishing,
            },
            ProgressiveUnlockRules.GetUnlockedActionModes(3));

        Assert.Equal(
            new[]
            {
                PlayerActionState.ModeDungeon,
                PlayerActionState.ModeCultivation,
                PlayerActionState.ModeAlchemy,
                PlayerActionState.ModeGarden,
                PlayerActionState.ModeMining,
                PlayerActionState.ModeSmithing,
                PlayerActionState.ModeFishing,
                PlayerActionState.ModeCooking,
                PlayerActionState.ModeTalisman,
            },
            ProgressiveUnlockRules.GetUnlockedActionModes(4));

        Assert.Equal(
            new[]
            {
                PlayerActionState.ModeDungeon,
                PlayerActionState.ModeCultivation,
                PlayerActionState.ModeAlchemy,
                PlayerActionState.ModeGarden,
                PlayerActionState.ModeMining,
                PlayerActionState.ModeSmithing,
                PlayerActionState.ModeFishing,
                PlayerActionState.ModeCooking,
                PlayerActionState.ModeTalisman,
                PlayerActionState.ModeFormation,
                PlayerActionState.ModeBodyCultivation,
            },
            ProgressiveUnlockRules.GetUnlockedActionModes(5));
    }

    [Fact]
    public void LeftTabs_UnlockByRealmFlagsAndValidationToggle()
    {
        Assert.Equal(
            new[]
            {
                "CultivationTab",
                "BattleLogTab",
            },
            ProgressiveUnlockRules.GetUnlockedLeftTabs(1, equipmentTabUnlocked: false, backpackTabUnlocked: false, showValidationPanel: false));

        Assert.Equal(
            new[]
            {
                "CultivationTab",
                "BattleLogTab",
                "ShopTab",
            },
            ProgressiveUnlockRules.GetUnlockedLeftTabs(2, equipmentTabUnlocked: false, backpackTabUnlocked: false, showValidationPanel: false));

        Assert.Equal(
            new[]
            {
                "CultivationTab",
                "BattleLogTab",
                "EquipmentTab",
                "BackpackTab",
                "ShopTab",
                "StatsTab",
                "ValidationTab",
            },
            ProgressiveUnlockRules.GetUnlockedLeftTabs(3, equipmentTabUnlocked: true, backpackTabUnlocked: true, showValidationPanel: true));
    }

    [Fact]
    public void EquipmentAndBackpackUnlocks_TrackPossessionFlags()
    {
        Assert.False(ProgressiveUnlockRules.HasUnlockedEquipmentTab(hasBackpackEquipment: false, hasEquippedItems: false));
        Assert.True(ProgressiveUnlockRules.HasUnlockedEquipmentTab(hasBackpackEquipment: true, hasEquippedItems: false));
        Assert.True(ProgressiveUnlockRules.HasUnlockedEquipmentTab(hasBackpackEquipment: false, hasEquippedItems: true));

        Assert.False(ProgressiveUnlockRules.HasUnlockedBackpackTab(hasBackpackItems: false, hasBackpackEquipment: false, hasPotions: false));
        Assert.True(ProgressiveUnlockRules.HasUnlockedBackpackTab(hasBackpackItems: true, hasBackpackEquipment: false, hasPotions: false));
        Assert.True(ProgressiveUnlockRules.HasUnlockedBackpackTab(hasBackpackItems: false, hasBackpackEquipment: true, hasPotions: false));
        Assert.True(ProgressiveUnlockRules.HasUnlockedBackpackTab(hasBackpackItems: false, hasBackpackEquipment: false, hasPotions: true));
    }
}
