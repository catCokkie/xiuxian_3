using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public readonly record struct BackpackConsumableUsage(string ItemId, string Reason);

    public static class ActivityEffectRules
    {
        public static List<BackpackConsumableUsage> DetermineAutoUseBackpackConsumables(
            BattleConsumableState state,
            IReadOnlyDictionary<string, int> items,
            int maxTalismansPerBattle = 1)
        {
            var result = new List<BackpackConsumableUsage>();
            int porridge = items != null && items.TryGetValue("food_spirit_porridge", out int porridgeCount) ? porridgeCount : 0;
            int jelly = items != null && items.TryGetValue("food_fruit_jelly", out int jellyCount) ? jellyCount : 0;
            int soup = items != null && items.TryGetValue("food_dragon_soup", out int soupCount) ? soupCount : 0;

            if (state.IsBattleStart)
            {
                foreach (string talismanId in GetAutoUseTalismans(items, maxTalismansPerBattle))
                {
                    result.Add(new BackpackConsumableUsage(talismanId, "battle_start"));
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
                "talisman_burst_charm" => TalismanRules.GetModifier(itemId),
                "food_spirit_porridge" => CookingRules.GetModifier(itemId),
                "food_fruit_jelly" => CookingRules.GetModifier(itemId),
                "food_dragon_soup" => CookingRules.GetModifier(itemId),
                _ => default,
            };
        }

        public static CharacterStatModifier CollectFormationModifier(string activePrimaryId, string activeSecondaryId, IReadOnlyDictionary<string, int> items, int masteryLevel)
        {
            FormationEffectProfile profile = GetActiveFormationProfile(activePrimaryId, activeSecondaryId, items, masteryLevel);
            return profile.BattleModifier;
        }

        public static double CollectFormationLingqiRate(string activePrimaryId, string activeSecondaryId, IReadOnlyDictionary<string, int> items, int masteryLevel)
        {
            return GetActiveFormationProfile(activePrimaryId, activeSecondaryId, items, masteryLevel).LingqiRewardRate;
        }

        public static double CollectFormationGatherSpeedRate(string activePrimaryId, string activeSecondaryId, IReadOnlyDictionary<string, int> items, int masteryLevel)
        {
            return GetActiveFormationProfile(activePrimaryId, activeSecondaryId, items, masteryLevel).GatherSpeedRate;
        }

        public static double CollectFormationCraftSpeedRate(string activePrimaryId, string activeSecondaryId, IReadOnlyDictionary<string, int> items, int masteryLevel)
        {
            return GetActiveFormationProfile(activePrimaryId, activeSecondaryId, items, masteryLevel).CraftSpeedRate;
        }

        public static CharacterStatModifier CollectPermanentProgressModifier(PlayerProgressPersistenceRules.PlayerProgressSnapshot snapshot)
        {
            return new CharacterStatModifier(
                MaxHpFlat: snapshot.BodyCultivationMaxHpFlat,
                AttackFlat: snapshot.BodyCultivationAttackFlat,
                DefenseFlat: snapshot.BodyCultivationDefenseFlat,
                MaxHpRate: snapshot.ZhouTianMaxHpRate,
                AttackRate: snapshot.ZhouTianAttackRate,
                DefenseRate: snapshot.ZhouTianDefenseRate);
        }

        private static FormationEffectProfile GetActiveFormationProfile(string activePrimaryId, string activeSecondaryId, IReadOnlyDictionary<string, int> items, int masteryLevel)
        {
            FormationEffectProfile primary = GetOwnedFormationProfile(activePrimaryId, items, masteryLevel);
            if (FormationRules.GetMaxSlotCount(masteryLevel) < 2 || string.IsNullOrEmpty(activeSecondaryId) || activeSecondaryId == activePrimaryId)
            {
                return primary;
            }

            FormationEffectProfile secondary = FormationRules.ScaleEffectProfile(
                GetOwnedFormationProfile(activeSecondaryId, items, masteryLevel),
                FormationRules.SecondarySlotEffectRatio);
            return FormationRules.CombineEffectProfiles(primary, secondary);
        }

        private static FormationEffectProfile GetOwnedFormationProfile(string formationId, IReadOnlyDictionary<string, int> items, int masteryLevel)
        {
            if (string.IsNullOrEmpty(formationId) || items == null || !items.TryGetValue(formationId, out int count) || count <= 0)
            {
                return default;
            }

            return FormationRules.GetEffectProfile(formationId, masteryLevel);
        }

        private static IEnumerable<string> GetAutoUseTalismans(IReadOnlyDictionary<string, int> items, int maxTalismansPerBattle)
        {
            if (items == null || maxTalismansPerBattle <= 0)
            {
                yield break;
            }

            string[] priority =
            {
                "talisman_burst_charm",
                "talisman_shield_charm",
                "talisman_fire_charm",
            };

            int used = 0;
            for (int i = 0; i < priority.Length && used < maxTalismansPerBattle; i++)
            {
                string talismanId = priority[i];
                if (items.TryGetValue(talismanId, out int count) && count > 0)
                {
                    yield return talismanId;
                    used++;
                }
            }
        }
    }
}
