using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class BodyCultivationRules
    {
        public readonly record struct TechniqueSpec(
            string RecipeId,
            string DisplayName,
            MaterialCost[] Inputs,
            double LingqiCost,
            int RequiredInputEvents,
            int RequiredMasteryLevel,
            int BaseMaxCount,
            CharacterStatModifier RewardModifier,
            double PostBattleHealRate = 0.0);

        public const int IronBoneMaxCount = 20;
        public const int SpiritSkinMaxCount = 15;
        public const int BloodflowMaxCount = 10;
        public const int IronBoneMaxHpFlat = 6;
        public const int SpiritSkinDefenseFlat = 2;
        public const int BloodflowAttackFlat = 2;
        public const double BloodflowHealRate = 0.02;

        private static readonly TechniqueSpec[] Techniques =
        {
            new(
                "body_cultivation_temper",
                "铁骨功",
                new[] { new MaterialCost("beast_bone", 3) },
                40.0,
                260,
                1,
                IronBoneMaxCount,
                new CharacterStatModifier(MaxHpFlat: IronBoneMaxHpFlat)),
            new(
                "body_cultivation_boneforge",
                "灵肤术",
                new[] { new MaterialCost("spirit_fruit", 2), new MaterialCost("spirit_herb", 1) },
                48.0,
                300,
                2,
                SpiritSkinMaxCount,
                new CharacterStatModifier(DefenseFlat: SpiritSkinDefenseFlat)),
            new(
                "body_cultivation_bloodflow",
                "活血诀",
                new[] { new MaterialCost("spirit_fruit", 3) },
                64.0,
                320,
                4,
                BloodflowMaxCount,
                new CharacterStatModifier(AttackFlat: BloodflowAttackFlat),
                BloodflowHealRate),
        };

        public static IReadOnlyList<TechniqueSpec> GetTechniques() => Techniques;

        public static bool TryGetTechnique(string recipeId, out TechniqueSpec technique)
        {
            for (int i = 0; i < Techniques.Length; i++)
            {
                if (Techniques[i].RecipeId == recipeId)
                {
                    technique = Techniques[i];
                    return true;
                }
            }

            technique = default;
            return false;
        }

        public static double GetMaterialDiscount(int masteryLevel)
        {
            return SubsystemMasteryRules.GetEffectValue(
                PlayerActionState.ModeBodyCultivation,
                masteryLevel,
                SubsystemMasteryRules.BodyCultivationMaterialDiscountEffectId);
        }

        public static int GetEffectiveTemperCap(int masteryLevel)
        {
            return GetEffectiveCap("body_cultivation_temper", masteryLevel);
        }

        public static int GetEffectiveBoneforgeCap(int masteryLevel)
        {
            return GetEffectiveCap("body_cultivation_boneforge", masteryLevel);
        }

        public static int GetEffectiveBloodflowCap(int masteryLevel)
        {
            return GetEffectiveCap("body_cultivation_bloodflow", masteryLevel);
        }

        public static int GetEffectiveCap(string recipeId, int masteryLevel = 1)
        {
            if (!TryGetTechnique(recipeId, out TechniqueSpec technique))
            {
                return 0;
            }

            double bonus = SubsystemMasteryRules.GetEffectValue(
                PlayerActionState.ModeBodyCultivation,
                masteryLevel,
                SubsystemMasteryRules.BodyCultivationExtraCapEffectId);
            return technique.BaseMaxCount + (int)bonus;
        }

        public static bool CanApply(string recipeId, int currentCount, int masteryLevel = 1)
        {
            return currentCount < GetEffectiveCap(recipeId, masteryLevel);
        }

        public static CharacterStatModifier GetRateModifier(string recipeId, int masteryLevel = 1)
        {
            return TryGetTechnique(recipeId, out TechniqueSpec technique) ? technique.RewardModifier : default;
        }

        public static double GetPostBattleHealRate(string recipeId)
        {
            return TryGetTechnique(recipeId, out TechniqueSpec technique) ? technique.PostBattleHealRate : 0.0;
        }
    }
}
