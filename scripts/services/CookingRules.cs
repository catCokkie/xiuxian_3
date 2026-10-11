namespace Xiuxian.Scripts.Services
{
    public static class CookingRules
    {
        public const int DefaultDurationBattles = 3;
        public const double SpiritPorridgeHpRate = 0.15;
        public const double FruitJellyAllStatRate = 0.05;
        public const double DragonSoupLingqiRate = 0.30;

        public static CharacterStatModifier GetModifier(string recipeId)
            => new(DefenseRate: MealRules.Get(recipeId).DefenseRate);
        public static double GetLingqiRewardRate(string recipeId) => MealRules.Get(recipeId).LingqiRate;
        public static double GetEffectiveDuration(int masteryLevel) => MealRules.Duration("food_clear_fish_soup", masteryLevel);
        public static double GetDoubleOutputChance(int masteryLevel) => 0;

        public static bool HasExtraEffect(int masteryLevel)
        {
            return SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeCooking, masteryLevel, SubsystemMasteryRules.CookingExtraEffectId) >= 1.0;
        }
    }
}
