namespace Xiuxian.Scripts.Services
{
    public static class FormationRules
    {
        public const double SpiritPlateLingqiRate = 0.08;
        public const double GuardFlagDefenseRate = 0.05;

        public static CharacterStatModifier GetModifier(string recipeId)
        {
            return recipeId switch
            {
                "formation_spirit_plate" => new CharacterStatModifier(AttackFlat: 2),
                "formation_guard_flag" => new CharacterStatModifier(DefenseRate: GuardFlagDefenseRate),
                _ => default,
            };
        }

        public static double GetLingqiRewardRate(string recipeId)
        {
            return recipeId == "formation_spirit_plate" ? SpiritPlateLingqiRate : 0.0;
        }
    }
}
