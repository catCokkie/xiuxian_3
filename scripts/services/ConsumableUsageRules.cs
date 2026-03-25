using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public readonly record struct PotionUsage(string PotionId, string Reason, double EffectValue);

    public readonly record struct BattleConsumableState(
        int PlayerCurrentHp,
        int PlayerMaxHp,
        bool IsBattleStart,
        bool HasLingqiDropBuff,
        bool UsedHealPotionThisBattle);

    public static class ConsumableUsageRules
    {
        public const double HuiqiDanHealPercent = 0.30;
        public const double JulingSanLingqiBuffPercent = 0.25;

        public static List<PotionUsage> DetermineAutoConsume(BattleConsumableState state, IReadOnlyDictionary<string, int> potions)
        {
            var result = new List<PotionUsage>();
            int huiqiDanCount = potions != null && potions.TryGetValue("potion_huiqi_dan", out int healCount) ? healCount : 0;
            int julingSanCount = potions != null && potions.TryGetValue("potion_juling_san", out int buffCount) ? buffCount : 0;

            if (state.IsBattleStart && !state.HasLingqiDropBuff && julingSanCount > 0)
            {
                result.Add(new PotionUsage("potion_juling_san", "battle_start", JulingSanLingqiBuffPercent));
            }

            bool hpBelowHalf = state.PlayerMaxHp > 0 && state.PlayerCurrentHp * 2 < state.PlayerMaxHp;
            if (hpBelowHalf && !state.UsedHealPotionThisBattle && huiqiDanCount > 0)
            {
                result.Add(new PotionUsage("potion_huiqi_dan", "hp_below_50%", HuiqiDanHealPercent));
            }

            return result;
        }
    }
}
