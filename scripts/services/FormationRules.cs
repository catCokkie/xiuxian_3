namespace Xiuxian.Scripts.Services
{
    public static class FormationRules
    {
        public const double SpiritPlateLingqiRate = 0.08;
        public const double GuardFlagDefenseRate = 0.05;

        public static CharacterStatModifier GetModifier(string recipeId, int masteryLevel = 1)
        {
            double effectBonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeFormation, masteryLevel, SubsystemMasteryRules.FormationEffectBonusEffectId);
            CharacterStatModifier baseModifier = recipeId switch
            {
                "formation_spirit_plate" => new CharacterStatModifier(AttackFlat: 2),
                "formation_guard_flag" => new CharacterStatModifier(DefenseRate: GuardFlagDefenseRate),
                _ => default,
            };
            if (effectBonus <= 0.0)
            {
                return baseModifier;
            }

            return new CharacterStatModifier(
                MaxHpFlat: baseModifier.MaxHpFlat,
                AttackFlat: baseModifier.AttackFlat,
                DefenseFlat: baseModifier.DefenseFlat,
                SpeedFlat: baseModifier.SpeedFlat,
                MaxHpRate: baseModifier.MaxHpRate * (1.0 + effectBonus),
                AttackRate: baseModifier.AttackRate * (1.0 + effectBonus),
                DefenseRate: baseModifier.DefenseRate * (1.0 + effectBonus),
                SpeedRate: baseModifier.SpeedRate * (1.0 + effectBonus));
        }

        public static double GetLingqiRewardRate(string recipeId)
        {
            return recipeId == "formation_spirit_plate" ? SpiritPlateLingqiRate : 0.0;
        }

        public static int GetMaxSlotCount(int masteryLevel)
        {
            double dualSlot = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeFormation, masteryLevel, SubsystemMasteryRules.FormationDualSlotEffectId);
            return dualSlot >= 2.0 ? 2 : 1;
        }

        public static bool HasSelfRepair(int masteryLevel)
        {
            return SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeFormation, masteryLevel, SubsystemMasteryRules.FormationSelfRepairEffectId) >= 1.0;
        }
    }
}
