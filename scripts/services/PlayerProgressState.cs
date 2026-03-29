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
        public int PetMood { get; private set; } = 60;
        public bool HasUnlockedAdvancedAlchemyStudy { get; private set; }
        public double CurrentRealmActiveSeconds { get; private set; }
        public double EnlightenmentInsightBonusRate { get; private set; }
        public double EnlightenmentLingqiBonusRate { get; private set; }
        public int BodyCultivationMaxHpFlat { get; private set; }
        public int BodyCultivationAttackFlat { get; private set; }
        public int BodyCultivationDefenseFlat { get; private set; }
        public int MeditationCount { get; private set; }
        public int ContemplationCount { get; private set; }
        public int TemperCount { get; private set; }
        public int BoneforgeCount { get; private set; }
        [Export] public bool AutoBreakthrough = false;

        public double RealmExpRequired => GetExpRequired(RealmLevel);
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

        public void SetPetMood(int mood)
        {
            int clamped = Math.Clamp(mood, 0, 100);
            if (clamped == PetMood)
            {
                return;
            }

            PetMood = clamped;
            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
        }

        public double GetMoodMultiplier()
        {
            if (PetMood >= 80)
            {
                return 1.10;
            }

            if (PetMood <= 30)
            {
                return 0.85;
            }

            return 1.00;
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

        public void ApplyEnlightenmentReward(string recipeId)
        {
            if (recipeId == "enlightenment_meditation" && EnlightenmentRules.CanApply(recipeId, MeditationCount))
            {
                EnlightenmentInsightBonusRate += EnlightenmentRules.GetInsightRateGain(recipeId);
                MeditationCount++;
            }
            else if (recipeId == "enlightenment_contemplation" && EnlightenmentRules.CanApply(recipeId, ContemplationCount))
            {
                EnlightenmentInsightBonusRate += EnlightenmentRules.GetInsightRateGain(recipeId);
                ContemplationCount++;
            }

            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
        }

        public void ApplyBodyCultivationReward(string recipeId)
        {
            if (recipeId == "body_cultivation_temper" && BodyCultivationRules.CanApply(recipeId, TemperCount))
            {
                BodyCultivationMaxHpFlat += 6;
                TemperCount++;
            }
            else if (recipeId == "body_cultivation_boneforge" && BodyCultivationRules.CanApply(recipeId, BoneforgeCount))
            {
                BodyCultivationDefenseFlat += 2;
                BoneforgeCount++;
            }

            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
        }

        public static double GetExpRequired(int realmLevel)
        {
            int r = Math.Max(1, realmLevel);
            return 120.0 * Math.Pow(r, 1.32) + 180.0;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            PlayerProgressPersistenceRules.PlayerProgressSnapshot snapshot = new(
                RealmLevel,
                RealmExp,
                PetMood,
                HasUnlockedAdvancedAlchemyStudy,
                CurrentRealmActiveSeconds,
                EnlightenmentInsightBonusRate,
                EnlightenmentLingqiBonusRate,
                BodyCultivationMaxHpFlat,
                BodyCultivationAttackFlat,
                BodyCultivationDefenseFlat,
                MeditationCount,
                ContemplationCount,
                TemperCount,
                BoneforgeCount);
            return SaveValueConversionRules.ToVariantDictionary(PlayerProgressPersistenceRules.ToPlainDictionary(snapshot));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            PlayerProgressPersistenceRules.PlayerProgressSnapshot snapshot = PlayerProgressPersistenceRules.FromPlainDictionary(
                SaveValueConversionRules.ToPlainDictionary(data));
            RealmLevel = snapshot.RealmLevel;
            RealmExp = snapshot.RealmExp;
            PetMood = snapshot.PetMood;
            HasUnlockedAdvancedAlchemyStudy = snapshot.AdvancedAlchemyStudyUnlocked;
            CurrentRealmActiveSeconds = snapshot.CurrentRealmActiveSeconds;
            EnlightenmentInsightBonusRate = snapshot.EnlightenmentInsightBonusRate;
            EnlightenmentLingqiBonusRate = snapshot.EnlightenmentLingqiBonusRate;
            BodyCultivationMaxHpFlat = snapshot.BodyCultivationMaxHpFlat;
            BodyCultivationAttackFlat = snapshot.BodyCultivationAttackFlat;
            BodyCultivationDefenseFlat = snapshot.BodyCultivationDefenseFlat;
            MeditationCount = snapshot.MeditationCount;
            ContemplationCount = snapshot.ContemplationCount;
            TemperCount = snapshot.TemperCount;
            BoneforgeCount = snapshot.BoneforgeCount;
            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
        }
    }
}
