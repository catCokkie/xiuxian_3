using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class ProgressiveUnlockRules
    {
        private static readonly string[] AllActionModesInUnlockOrder =
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
        };

        private static readonly string[] BaseLeftTabs =
        {
            "CultivationTab",
            "BattleLogTab",
        };

        public static IReadOnlyList<string> GetAllActionModesInUnlockOrder()
        {
            return AllActionModesInUnlockOrder;
        }

        public static IReadOnlyList<string> GetUnlockedActionModes(int realmLevel)
        {
            int normalizedRealmLevel = Math.Max(1, realmLevel);
            List<string> result = new(AllActionModesInUnlockOrder.Length)
            {
                PlayerActionState.ModeDungeon,
                PlayerActionState.ModeCultivation,
                PlayerActionState.ModeAlchemy,
            };

            if (normalizedRealmLevel >= 2)
            {
                result.Add(PlayerActionState.ModeGarden);
                result.Add(PlayerActionState.ModeMining);
            }

            if (normalizedRealmLevel >= 3)
            {
                result.Add(PlayerActionState.ModeSmithing);
                result.Add(PlayerActionState.ModeFishing);
            }

            if (normalizedRealmLevel >= 4)
            {
                result.Add(PlayerActionState.ModeCooking);
                result.Add(PlayerActionState.ModeTalisman);
            }

            if (normalizedRealmLevel >= 5)
            {
                result.Add(PlayerActionState.ModeFormation);
                result.Add(PlayerActionState.ModeBodyCultivation);
            }

            return result;
        }

        public static bool IsActionModeUnlocked(string modeId, int realmLevel)
        {
            IReadOnlyList<string> unlockedModes = GetUnlockedActionModes(realmLevel);
            for (int i = 0; i < unlockedModes.Count; i++)
            {
                if (unlockedModes[i] == modeId)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsStatsTabUnlocked(int realmLevel)
        {
            return realmLevel >= 3;
        }

        public static bool IsShopTabUnlocked(int realmLevel)
        {
            return ShopRules.IsShopTabUnlocked(realmLevel);
        }

        public static bool IsValidationTabUnlocked(bool showValidationPanel)
        {
            return showValidationPanel;
        }

        public static bool HasUnlockedEquipmentTab(BackpackState? backpackState, EquippedItemsState? equippedItemsState)
        {
            bool hasBackpackEquipment = backpackState != null
                && (backpackState.GetEquipmentProfiles().Length > 0 || backpackState.GetEquipmentInstances().Length > 0);
            bool hasEquippedItems = equippedItemsState != null && !equippedItemsState.IsEmpty;
            return HasUnlockedEquipmentTab(hasBackpackEquipment, hasEquippedItems);
        }

        public static bool HasUnlockedEquipmentTab(bool hasBackpackEquipment, bool hasEquippedItems)
        {
            return hasEquippedItems || hasBackpackEquipment;
        }

        public static bool HasUnlockedBackpackTab(BackpackState? backpackState, PotionInventoryState? potionInventoryState)
        {
            bool hasBackpackItems = backpackState != null && backpackState.GetItemEntries().Count > 0;
            bool hasBackpackEquipment = backpackState != null
                && (backpackState.GetEquipmentProfiles().Length > 0 || backpackState.GetEquipmentInstances().Length > 0);
            bool hasPotions = potionInventoryState != null && potionInventoryState.GetPotionEntries().Count > 0;
            return HasUnlockedBackpackTab(hasBackpackItems, hasBackpackEquipment, hasPotions);
        }

        public static bool HasUnlockedBackpackTab(bool hasBackpackItems, bool hasBackpackEquipment, bool hasPotions)
        {
            return hasBackpackItems || hasBackpackEquipment || hasPotions;
        }

        public static IReadOnlyList<string> GetUnlockedLeftTabs(
            int realmLevel,
            bool equipmentTabUnlocked,
            bool backpackTabUnlocked,
            bool showValidationPanel)
        {
            List<string> result = new(BaseLeftTabs.Length + 4);
            for (int i = 0; i < BaseLeftTabs.Length; i++)
            {
                result.Add(BaseLeftTabs[i]);
            }

            if (equipmentTabUnlocked)
            {
                result.Add("EquipmentTab");
            }

            if (backpackTabUnlocked)
            {
                result.Add("BackpackTab");
            }

            if (IsShopTabUnlocked(realmLevel))
            {
                result.Add("ShopTab");
            }

            if (IsStatsTabUnlocked(realmLevel))
            {
                result.Add("StatsTab");
            }

            if (IsValidationTabUnlocked(showValidationPanel))
            {
                result.Add("ValidationTab");
            }

            return result;
        }
    }
}
