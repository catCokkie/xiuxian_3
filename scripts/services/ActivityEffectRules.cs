using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public readonly record struct BackpackConsumableUsage(string ItemId, string Reason);

    public static class ActivityEffectRules
    {
        public static List<BackpackConsumableUsage> DetermineAutoUseBackpackConsumables(BattleConsumableState state, IReadOnlyDictionary<string, int> items)
        {
            var result = new List<BackpackConsumableUsage>();
            int fireCharm = items != null && items.TryGetValue("talisman_fire_charm", out int fireCount) ? fireCount : 0;
            int shieldCharm = items != null && items.TryGetValue("talisman_shield_charm", out int shieldCount) ? shieldCount : 0;
            int porridge = items != null && items.TryGetValue("food_spirit_porridge", out int porridgeCount) ? porridgeCount : 0;
            int jelly = items != null && items.TryGetValue("food_fruit_jelly", out int jellyCount) ? jellyCount : 0;
            int soup = items != null && items.TryGetValue("food_dragon_soup", out int soupCount) ? soupCount : 0;

            if (state.IsBattleStart)
            {
                if (fireCharm > 0)
                {
                    result.Add(new BackpackConsumableUsage("talisman_fire_charm", "battle_start"));
                }
                if (shieldCharm > 0)
                {
                    result.Add(new BackpackConsumableUsage("talisman_shield_charm", "battle_start"));
                }
                if (porridge > 0)
                {
                    result.Add(new BackpackConsumableUsage("food_spirit_porridge", "battle_start"));
                }
                if (jelly > 0)
                {
                    result.Add(new BackpackConsumableUsage("food_fruit_jelly", "battle_start"));
                }
                if (soup > 0)
                {
                    result.Add(new BackpackConsumableUsage("food_dragon_soup", "battle_start"));
                }
            }

            return result;
        }

        public static CharacterStatModifier GetBackpackConsumableModifier(string itemId)
        {
            return itemId switch
            {
                "talisman_fire_charm" => TalismanRules.GetModifier(itemId),
                "talisman_shield_charm" => TalismanRules.GetModifier(itemId),
                "food_spirit_porridge" => CookingRules.GetModifier(itemId),
                "food_fruit_jelly" => CookingRules.GetModifier(itemId),
                "food_dragon_soup" => CookingRules.GetModifier(itemId),
                _ => default,
            };
        }

        public static CharacterStatModifier CollectFormationModifier(string activeRecipeId, IReadOnlyDictionary<string, int> items)
        {
            if (string.IsNullOrEmpty(activeRecipeId) || items == null || !items.TryGetValue(activeRecipeId, out int count) || count <= 0)
            {
                return default;
            }

            return FormationRules.GetModifier(activeRecipeId);
        }

        public static double CollectFormationLingqiRate(string activeRecipeId, IReadOnlyDictionary<string, int> items)
        {
            if (string.IsNullOrEmpty(activeRecipeId) || items == null || !items.TryGetValue(activeRecipeId, out int count) || count <= 0)
            {
                return 0.0;
            }

            return FormationRules.GetLingqiRewardRate(activeRecipeId);
        }

        public static CharacterStatModifier CollectPermanentProgressModifier(PlayerProgressPersistenceRules.PlayerProgressSnapshot snapshot)
        {
            return new CharacterStatModifier(
                MaxHpFlat: snapshot.BodyCultivationMaxHpFlat,
                AttackFlat: snapshot.BodyCultivationAttackFlat,
                DefenseFlat: snapshot.BodyCultivationDefenseFlat);
        }
    }
}
