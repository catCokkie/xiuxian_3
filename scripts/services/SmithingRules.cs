using System;

namespace Xiuxian.Scripts.Services
{
    public readonly record struct SmithingCost(int Shards, int Talismans, double Lingqi, int RequiredInputs);

    public static class SmithingRules
    {
        public static int GetMaxEnhanceLevel(int masteryLevel)
        {
            return masteryLevel switch
            {
                >= 4 => 9,
                >= 2 => 6,
                _ => 3,
            };
        }

        public static SmithingCost GetCost(int currentLevel, int masteryLevel = 1)
        {
            int nextLevel = Math.Max(0, currentLevel) + 1;
            bool discounted = masteryLevel >= 3;
            if (nextLevel <= 3)
            {
                return new SmithingCost(ApplyDiscount(3, discounted), 0, 30.0, 100 + currentLevel * 50);
            }

            if (nextLevel <= 6)
            {
                return new SmithingCost(ApplyDiscount(5, discounted), ApplyDiscount(1, discounted), 80.0, 100 + currentLevel * 50);
            }

            return new SmithingCost(ApplyDiscount(8, discounted), ApplyDiscount(3, discounted), 150.0, 100 + currentLevel * 50);
        }

        public static double GetEnhanceMultiplier(int enhanceLevel)
        {
            return Math.Pow(1.08, Math.Max(0, enhanceLevel));
        }

        public static bool CanEnhance(EquipmentStatProfile equipment, BackpackState backpack, ResourceWalletState wallet, int masteryLevel = 1)
        {
            int maxLevel = GetMaxEnhanceLevel(masteryLevel);
            if (equipment.EnhanceLevel >= maxLevel)
            {
                return false;
            }

            SmithingCost cost = GetCost(equipment.EnhanceLevel, masteryLevel);
            return backpack.GetItemCount("lingqi_shard") >= cost.Shards
                && backpack.GetItemCount("broken_talisman") >= cost.Talismans
                && wallet.Lingqi >= cost.Lingqi;
        }

        private static int ApplyDiscount(int amount, bool discounted)
        {
            if (!discounted)
            {
                return amount;
            }

            return Math.Max(0, (int)Math.Floor(amount * 0.9));
        }
    }
}
