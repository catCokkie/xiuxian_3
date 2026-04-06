using Godot;
using System;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Player growth state and realm progression.
    /// </summary>
    public partial class PlayerProgressState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void RealmProgressChangedEventHandler(int realmLevel, double realmExp, double realmExpRequired);

        [Signal]
        public delegate void RealmLevelUpEventHandler(int newRealmLevel);

        public int RealmLevel { get; private set; } = 1;
        public double RealmExp { get; private set; }
        public bool HasUnlockedAdvancedAlchemyStudy { get; private set; }
        public double CurrentRealmActiveSeconds { get; private set; }
        public int BodyCultivationMaxHpFlat { get; private set; }
        public int BodyCultivationAttackFlat { get; private set; }
        public int BodyCultivationDefenseFlat { get; private set; }
        public int TemperCount { get; private set; }
        public int BoneforgeCount { get; private set; }
        public int BloodflowCount { get; private set; }
        public double BodyCultivationPostBattleHealRate { get; private set; }
        public double ZhouTianMaxHpRate { get; private set; }
        public double ZhouTianAttackRate { get; private set; }
        public double ZhouTianDefenseRate { get; private set; }
        [Export] public bool AutoBreakthrough = false;

        public double RealmExpRequired => GetExpRequired(RealmLevel, GetRealmExpReductionRate());
        public bool CanBreakthrough => RealmExp >= RealmExpRequired;

        public void AddRealmExp(double amount)
        {
            if (amount <= 0.0)
            {
                return;
            }

            RealmExp += amount;

            if (!AutoBreakthrough)
            {
                EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
                return;
            }

            bool leveled = false;
            while (TryBreakthrough())
            {
                leveled = true;
            }
            if (!leveled)
            {
                EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
            }
        }

        public bool TryBreakthrough()
        {
            if (!CanBreakthrough)
            {
                return false;
            }

            RealmExp -= RealmExpRequired;
            RealmLevel++;
            CurrentRealmActiveSeconds = 0.0;
            EmitSignal(SignalName.RealmLevelUp, RealmLevel);
            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
            GD.Print($"PlayerProgressState: realm level up -> {RealmLevel}");
            return true;
        }

        public double GetRealmMultiplier()
        {
            return 1.0 + (RealmLevel - 1) * 0.06;
        }

        public bool UnlockAdvancedAlchemyStudy()
        {
            if (HasUnlockedAdvancedAlchemyStudy)
            {
                return false;
            }

            HasUnlockedAdvancedAlchemyStudy = true;
            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
            return true;
        }

        public void AddRealmActiveSeconds(double seconds)
        {
            if (seconds <= 0.0)
            {
                return;
            }

            CurrentRealmActiveSeconds += seconds;
        }

        public bool CanApplyBodyCultivationReward(string recipeId, int masteryLevel = 1)
        {
            return BodyCultivationRules.CanApply(recipeId, GetBodyCultivationCount(recipeId), masteryLevel);
        }

        public bool TryApplyBodyCultivationReward(string recipeId, int masteryLevel, out string rewardText)
        {
            rewardText = string.Empty;
            if (!BodyCultivationRules.TryGetTechnique(recipeId, out BodyCultivationRules.TechniqueSpec technique)
                || !CanApplyBodyCultivationReward(recipeId, masteryLevel))
            {
                return false;
            }

            CharacterStatModifier modifier = BodyCultivationRules.GetRateModifier(recipeId, masteryLevel);
            BodyCultivationMaxHpFlat += modifier.MaxHpFlat;
            BodyCultivationAttackFlat += modifier.AttackFlat;
            BodyCultivationDefenseFlat += modifier.DefenseFlat;
            BodyCultivationPostBattleHealRate += BodyCultivationRules.GetPostBattleHealRate(recipeId);

            switch (recipeId)
            {
                case "body_cultivation_temper":
                    TemperCount++;
                    break;
                case "body_cultivation_boneforge":
                    BoneforgeCount++;
                    break;
                case "body_cultivation_bloodflow":
                    BloodflowCount++;
                    break;
            }

            rewardText = BuildBodyCultivationRewardText(technique.DisplayName, modifier, BodyCultivationRules.GetPostBattleHealRate(recipeId));
            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
            return true;
        }

        public int GetBodyCultivationCount(string recipeId)
        {
            return recipeId switch
            {
                "body_cultivation_temper" => TemperCount,
                "body_cultivation_boneforge" => BoneforgeCount,
                "body_cultivation_bloodflow" => BloodflowCount,
                _ => 0,
            };
        }

        public bool TryApplyZhouTianMeditationBonus(string bonusType, double rate, out string rewardText)
        {
            rewardText = string.Empty;
            if (rate <= 0.0)
            {
                return false;
            }

            switch (bonusType)
            {
                case CultivationRhythmRules.MeditationBonusMaxHp:
                    ZhouTianMaxHpRate += rate;
                    rewardText = $"入定领悟：气血 +{rate * 100:0.0}%";
                    break;
                case CultivationRhythmRules.MeditationBonusAttack:
                    ZhouTianAttackRate += rate;
                    rewardText = $"入定领悟：攻击 +{rate * 100:0.0}%";
                    break;
                case CultivationRhythmRules.MeditationBonusDefense:
                    ZhouTianDefenseRate += rate;
                    rewardText = $"入定领悟：防御 +{rate * 100:0.0}%";
                    break;
                default:
                    return false;
            }

            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
            return true;
        }

        public static double GetExpRequired(int realmLevel, double reductionRate = 0.0)
        {
            int r = Math.Max(1, realmLevel);
            double baseRequired = 120.0 * Math.Pow(r, 1.32) + 180.0;
            double clampedReduction = Math.Clamp(reductionRate, 0.0, 0.50);
            return Math.Max(1.0, baseRequired * (1.0 - clampedReduction));
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            PlayerProgressPersistenceRules.PlayerProgressSnapshot snapshot = new(
                RealmLevel,
                RealmExp,
                HasUnlockedAdvancedAlchemyStudy,
                CurrentRealmActiveSeconds,
                BodyCultivationMaxHpFlat,
                BodyCultivationAttackFlat,
                BodyCultivationDefenseFlat,
                TemperCount,
                BoneforgeCount,
                BloodflowCount,
                BodyCultivationPostBattleHealRate,
                ZhouTianMaxHpRate,
                ZhouTianAttackRate,
                ZhouTianDefenseRate);
            return SaveValueConversionRules.ToVariantDictionary(PlayerProgressPersistenceRules.ToPlainDictionary(snapshot));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            PlayerProgressPersistenceRules.PlayerProgressSnapshot snapshot = PlayerProgressPersistenceRules.FromPlainDictionary(
                SaveValueConversionRules.ToPlainDictionary(data));
            RealmLevel = snapshot.RealmLevel;
            RealmExp = snapshot.RealmExp;
            HasUnlockedAdvancedAlchemyStudy = snapshot.AdvancedAlchemyStudyUnlocked;
            CurrentRealmActiveSeconds = snapshot.CurrentRealmActiveSeconds;
            BodyCultivationMaxHpFlat = snapshot.BodyCultivationMaxHpFlat;
            BodyCultivationAttackFlat = snapshot.BodyCultivationAttackFlat;
            BodyCultivationDefenseFlat = snapshot.BodyCultivationDefenseFlat;
            TemperCount = snapshot.TemperCount;
            BoneforgeCount = snapshot.BoneforgeCount;
            BloodflowCount = snapshot.BloodflowCount;
            BodyCultivationPostBattleHealRate = snapshot.BodyCultivationPostBattleHealRate;
            ZhouTianMaxHpRate = snapshot.ZhouTianMaxHpRate;
            ZhouTianAttackRate = snapshot.ZhouTianAttackRate;
            ZhouTianDefenseRate = snapshot.ZhouTianDefenseRate;
            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
        }

        private static string BuildBodyCultivationRewardText(string displayName, CharacterStatModifier modifier, double postBattleHealRate)
        {
            string reward = displayName;
            if (modifier.MaxHpFlat > 0)
            {
                reward += $" 完成，永久气血 +{modifier.MaxHpFlat}";
            }
            else if (modifier.AttackFlat > 0)
            {
                reward += $" 完成，永久攻击 +{modifier.AttackFlat}";
            }
            else if (modifier.DefenseFlat > 0)
            {
                reward += $" 完成，永久防御 +{modifier.DefenseFlat}";
            }
            else
            {
                reward += " 完成";
            }

            if (postBattleHealRate > 0.0)
            {
                reward += $"，战后恢复 +{postBattleHealRate * 100:0}%";
            }

            return reward;
        }

        private static double GetRealmExpReductionRate()
        {
            double masteryReduction = 0.0;
            if (ServiceLocator.Instance?.SubsystemMasteryState is SubsystemMasteryState masteryState)
            {
                masteryReduction = SubsystemMasteryRules.GetEffectValue(
                    PlayerActionState.ModeCultivation,
                    masteryState.GetLevel(PlayerActionState.ModeCultivation),
                    "cultivation_breakthrough_exp_reduction");
            }

            double shopReduction = ServiceLocator.Instance?.ShopState?.BreakthroughExpReductionRate ?? 0.0;
            return Math.Clamp(masteryReduction + shopReduction, 0.0, 0.50);
        }
    }
}
