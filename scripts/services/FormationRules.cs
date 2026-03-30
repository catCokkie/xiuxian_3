namespace Xiuxian.Scripts.Services
{
    public readonly record struct FormationEffectProfile(
        CharacterStatModifier BattleModifier,
        double LingqiRewardRate,
        double InsightRewardRate,
        double GatherSpeedRate,
        double CraftSpeedRate,
        bool IsKnownFormation);

    public static class FormationRules
    {
        public const double SpiritPlateLingqiRate = 0.08;
        public const double GuardFlagDefenseRate = 0.05;
        public const double HarvestArrayGatherSpeedRate = 0.12;
        public const double CraftArrayCraftSpeedRate = 0.12;
        public const double SecondarySlotEffectRatio = 0.5;

        public static FormationEffectProfile GetEffectProfile(string recipeId, int masteryLevel = 1)
        {
            double effectBonus = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeFormation, masteryLevel, SubsystemMasteryRules.FormationEffectBonusEffectId);
            FormationEffectProfile baseProfile = recipeId switch
            {
                "formation_spirit_plate" => new FormationEffectProfile(
                    BattleModifier: new CharacterStatModifier(AttackFlat: 2),
                    LingqiRewardRate: SpiritPlateLingqiRate,
                    InsightRewardRate: 0.0,
                    GatherSpeedRate: 0.0,
                    CraftSpeedRate: 0.0,
                    IsKnownFormation: true),
                "formation_guard_flag" => new FormationEffectProfile(
                    BattleModifier: new CharacterStatModifier(DefenseRate: GuardFlagDefenseRate),
                    LingqiRewardRate: 0.0,
                    InsightRewardRate: 0.0,
                    GatherSpeedRate: 0.0,
                    CraftSpeedRate: 0.0,
                    IsKnownFormation: true),
                "formation_harvest_array" => new FormationEffectProfile(
                    BattleModifier: default,
                    LingqiRewardRate: 0.0,
                    InsightRewardRate: 0.0,
                    GatherSpeedRate: HarvestArrayGatherSpeedRate,
                    CraftSpeedRate: 0.0,
                    IsKnownFormation: true),
                "formation_craft_array" => new FormationEffectProfile(
                    BattleModifier: default,
                    LingqiRewardRate: 0.0,
                    InsightRewardRate: 0.0,
                    GatherSpeedRate: 0.0,
                    CraftSpeedRate: CraftArrayCraftSpeedRate,
                    IsKnownFormation: true),
                _ => default,
            };

            if (!baseProfile.IsKnownFormation || effectBonus <= 0.0)
            {
                return baseProfile;
            }

            CharacterStatModifier baseModifier = baseProfile.BattleModifier;
            return new FormationEffectProfile(
                BattleModifier: new CharacterStatModifier(
                    MaxHpFlat: baseModifier.MaxHpFlat,
                    AttackFlat: baseModifier.AttackFlat,
                    DefenseFlat: baseModifier.DefenseFlat,
                    SpeedFlat: baseModifier.SpeedFlat,
                    MaxHpRate: baseModifier.MaxHpRate * (1.0 + effectBonus),
                    AttackRate: baseModifier.AttackRate * (1.0 + effectBonus),
                    DefenseRate: baseModifier.DefenseRate * (1.0 + effectBonus),
                    SpeedRate: baseModifier.SpeedRate * (1.0 + effectBonus),
                    CritChanceDelta: baseModifier.CritChanceDelta,
                    CritDamageDelta: baseModifier.CritDamageDelta),
                LingqiRewardRate: baseProfile.LingqiRewardRate * (1.0 + effectBonus),
                InsightRewardRate: baseProfile.InsightRewardRate * (1.0 + effectBonus),
                GatherSpeedRate: baseProfile.GatherSpeedRate * (1.0 + effectBonus),
                CraftSpeedRate: baseProfile.CraftSpeedRate * (1.0 + effectBonus),
                IsKnownFormation: true);
        }

        public static CharacterStatModifier GetModifier(string recipeId, int masteryLevel = 1)
        {
            return GetEffectProfile(recipeId, masteryLevel).BattleModifier;
        }

        public static double GetLingqiRewardRate(string recipeId)
        {
            return GetEffectProfile(recipeId).LingqiRewardRate;
        }

        public static double GetGatherSpeedRate(string recipeId, int masteryLevel = 1)
        {
            return GetEffectProfile(recipeId, masteryLevel).GatherSpeedRate;
        }

        public static double GetCraftSpeedRate(string recipeId, int masteryLevel = 1)
        {
            return GetEffectProfile(recipeId, masteryLevel).CraftSpeedRate;
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

        public static FormationEffectProfile ScaleEffectProfile(FormationEffectProfile profile, double ratio)
        {
            if (!profile.IsKnownFormation || ratio <= 0.0)
            {
                return default;
            }

            CharacterStatModifier battle = profile.BattleModifier;
            return new FormationEffectProfile(
                BattleModifier: new CharacterStatModifier(
                    MaxHpFlat: (int)System.Math.Round(battle.MaxHpFlat * ratio),
                    AttackFlat: (int)System.Math.Round(battle.AttackFlat * ratio),
                    DefenseFlat: (int)System.Math.Round(battle.DefenseFlat * ratio),
                    SpeedFlat: (int)System.Math.Round(battle.SpeedFlat * ratio),
                    MaxHpRate: battle.MaxHpRate * ratio,
                    AttackRate: battle.AttackRate * ratio,
                    DefenseRate: battle.DefenseRate * ratio,
                    SpeedRate: battle.SpeedRate * ratio,
                    CritChanceDelta: battle.CritChanceDelta * ratio,
                    CritDamageDelta: battle.CritDamageDelta * ratio),
                LingqiRewardRate: profile.LingqiRewardRate * ratio,
                InsightRewardRate: profile.InsightRewardRate * ratio,
                GatherSpeedRate: profile.GatherSpeedRate * ratio,
                CraftSpeedRate: profile.CraftSpeedRate * ratio,
                IsKnownFormation: true);
        }

        public static FormationEffectProfile CombineEffectProfiles(FormationEffectProfile primary, FormationEffectProfile secondary)
        {
            if (!primary.IsKnownFormation)
            {
                return secondary;
            }

            if (!secondary.IsKnownFormation)
            {
                return primary;
            }

            CharacterStatModifier a = primary.BattleModifier;
            CharacterStatModifier b = secondary.BattleModifier;
            return new FormationEffectProfile(
                BattleModifier: new CharacterStatModifier(
                    MaxHpFlat: a.MaxHpFlat + b.MaxHpFlat,
                    AttackFlat: a.AttackFlat + b.AttackFlat,
                    DefenseFlat: a.DefenseFlat + b.DefenseFlat,
                    SpeedFlat: a.SpeedFlat + b.SpeedFlat,
                    MaxHpRate: a.MaxHpRate + b.MaxHpRate,
                    AttackRate: a.AttackRate + b.AttackRate,
                    DefenseRate: a.DefenseRate + b.DefenseRate,
                    SpeedRate: a.SpeedRate + b.SpeedRate,
                    CritChanceDelta: a.CritChanceDelta + b.CritChanceDelta,
                    CritDamageDelta: a.CritDamageDelta + b.CritDamageDelta),
                LingqiRewardRate: primary.LingqiRewardRate + secondary.LingqiRewardRate,
                InsightRewardRate: primary.InsightRewardRate + secondary.InsightRewardRate,
                GatherSpeedRate: primary.GatherSpeedRate + secondary.GatherSpeedRate,
                CraftSpeedRate: primary.CraftSpeedRate + secondary.CraftSpeedRate,
                IsKnownFormation: true);
        }
    }
}
