using System;

namespace Xiuxian.Scripts.Services
{
    public readonly record struct SmithingCost(int Shards, int Talismans, double Lingqi, int RequiredInputs);

    public static class SmithingRules
    {
        public static int GetMaxEnhanceLevel(int rarityTier)
        {
            return Math.Max(1, rarityTier) * 3;
        }

        public static SmithingCost GetCost(int currentLevel)
        {
            int nextLevel = Math.Max(0, currentLevel) + 1;
            if (nextLevel <= 3)
            {
                return new SmithingCost(3, 0, 30.0, 100 + currentLevel * 50);
            }

            if (nextLevel <= 6)
            {
                return new SmithingCost(5, 1, 80.0, 100 + currentLevel * 50);
            }

            return new SmithingCost(8, 3, 150.0, 100 + currentLevel * 50);
        }

        public static double GetEnhanceMultiplier(int enhanceLevel)
        {
            return Math.Pow(1.08, Math.Max(0, enhanceLevel));
        }

        public static bool CanEnhance(EquipmentStatProfile equipment, BackpackState backpack, ResourceWalletState wallet)
        {
            int maxLevel = GetMaxEnhanceLevel(equipment.Rarity);
            if (equipment.EnhanceLevel >= maxLevel)
            {
                return false;
            }

            SmithingCost cost = GetCost(equipment.EnhanceLevel);
            return backpack.GetItemCount("lingqi_shard") >= cost.Shards
                && backpack.GetItemCount("broken_talisman") >= cost.Talismans
                && wallet.Lingqi >= cost.Lingqi;
        }
    }
}
