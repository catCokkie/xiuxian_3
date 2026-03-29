namespace Xiuxian.Scripts.Services
{
    public static class CookingRules
    {
        public const int DefaultDurationBattles = 3;
        public const double SpiritPorridgeHpRate = 0.15;
        public const double FruitJellyAllStatRate = 0.05;
        public const double DragonSoupLingqiRate = 0.30;

        public static CharacterStatModifier GetModifier(string recipeId)
        {
            return recipeId switch
            {
                "food_spirit_porridge" => new CharacterStatModifier(MaxHpRate: SpiritPorridgeHpRate),
                "food_fruit_jelly" => new CharacterStatModifier(MaxHpRate: FruitJellyAllStatRate, AttackRate: FruitJellyAllStatRate, DefenseRate: FruitJellyAllStatRate, SpeedRate: FruitJellyAllStatRate),
                "food_dragon_soup" => default,
                _ => default,
            };
        }

        public static double GetLingqiRewardRate(string recipeId)
        {
            return recipeId == "food_dragon_soup" ? DragonSoupLingqiRate : 0.0;
        }

        public static int GetEffectiveDuration(int masteryLevel)
        {
            double durationBonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeCooking, masteryLevel, SubsystemMasteryRules.CookingDurationBonusEffectId);
            return DefaultDurationBattles + (int)durationBonus;
        }

        public static double GetDoubleOutputChance(int masteryLevel)
        {
            return SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeCooking, masteryLevel, SubsystemMasteryRules.CookingDoubleOutputEffectId);
        }

        public static bool HasExtraEffect(int masteryLevel)
        {
            return SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeCooking, masteryLevel, SubsystemMasteryRules.CookingExtraEffectId) >= 1.0;
        }
    }
}
