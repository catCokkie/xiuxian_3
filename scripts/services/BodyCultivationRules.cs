namespace Xiuxian.Scripts.Services
{
    public static class BodyCultivationRules
    {
        public const int TemperMaxCount = 20;
        public const int BoneforgeMaxCount = 15;
        public const double TemperHpRate = 0.01;
        public const double BoneforgeDefenseRate = 0.01;

        public static bool CanApply(string recipeId, int currentCount)
        {
            return recipeId switch
            {
                "body_cultivation_temper" => currentCount < TemperMaxCount,
                "body_cultivation_boneforge" => currentCount < BoneforgeMaxCount,
                _ => false,
            };
        }

        public static CharacterStatModifier GetRateModifier(string recipeId)
        {
            return recipeId switch
            {
                "body_cultivation_temper" => new CharacterStatModifier(MaxHpRate: TemperHpRate),
                "body_cultivation_boneforge" => new CharacterStatModifier(DefenseRate: BoneforgeDefenseRate),
                _ => default,
            };
        }
    }
}
