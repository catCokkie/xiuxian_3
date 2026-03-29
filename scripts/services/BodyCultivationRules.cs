namespace Xiuxian.Scripts.Services
{
    public static class BodyCultivationRules
    {
        public const int TemperMaxCount = 20;
        public const int BoneforgeMaxCount = 15;
        public const double TemperHpRate = 0.01;
        public const double BoneforgeDefenseRate = 0.01;

        public static int GetEffectiveTemperCap(int masteryLevel)
        {
            double bonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeBodyCultivation, masteryLevel, SubsystemMasteryRules.BodyCultivationTemperCapBonusEffectId);
            return TemperMaxCount + (int)bonus;
        }

        public static int GetEffectiveBoneforgeCap(int masteryLevel)
        {
            double bonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeBodyCultivation, masteryLevel, SubsystemMasteryRules.BodyCultivationBoneforgeCapBonusEffectId);
            return BoneforgeMaxCount + (int)bonus;
        }

        public static double GetEfficiencyMultiplier(int masteryLevel)
        {
            double bonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeBodyCultivation, masteryLevel, SubsystemMasteryRules.BodyCultivationEfficiencyBonusEffectId);
            return 1.0 + bonus;
        }

        public static bool CanApply(string recipeId, int currentCount, int masteryLevel = 1)
        {
            return recipeId switch
            {
                "body_cultivation_temper" => currentCount < GetEffectiveTemperCap(masteryLevel),
                "body_cultivation_boneforge" => currentCount < GetEffectiveBoneforgeCap(masteryLevel),
                _ => false,
            };
        }

        public static CharacterStatModifier GetRateModifier(string recipeId, int masteryLevel = 1)
        {
            double efficiency = GetEfficiencyMultiplier(masteryLevel);
            return recipeId switch
            {
                "body_cultivation_temper" => new CharacterStatModifier(MaxHpRate: TemperHpRate * efficiency),
                "body_cultivation_boneforge" => new CharacterStatModifier(DefenseRate: BoneforgeDefenseRate * efficiency),
                _ => default,
            };
        }
    }
}
