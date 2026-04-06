using System;

namespace Xiuxian.Scripts.Services
{
    public static class CultivationRhythmRules
    {
        public const string StrengthNone = "none";
        public const string StrengthWeak = "weak";
        public const string StrengthStrong = "strong";

        public const string MeditationBonusMaxHp = "max_hp_rate";
        public const string MeditationBonusAttack = "attack_rate";
        public const string MeditationBonusDefense = "defense_rate";

        public const int DefaultCycleMinutes = 25;
        public const int GrandCycleSmallCycleCount = 4;
        public const double RestDurationSeconds = 300.0;
        public const double RestLingqiBonusRate = 0.50;
        public const double MeditationBonusRate = 0.002;

        private static readonly int[] AllowedCycleMinutes = { 25, 45, 60, 90 };
        private static readonly string[] MeditationBonusTypes =
        {
            MeditationBonusMaxHp,
            MeditationBonusAttack,
            MeditationBonusDefense,
        };

        public static string NormalizeStrength(string? strength)
        {
            return strength switch
            {
                StrengthNone => StrengthNone,
                StrengthStrong => StrengthStrong,
                _ => StrengthWeak,
            };
        }

        public static int NormalizeCycleMinutes(int cycleMinutes)
        {
            for (int i = 0; i < AllowedCycleMinutes.Length; i++)
            {
                if (AllowedCycleMinutes[i] == cycleMinutes)
                {
                    return cycleMinutes;
                }
            }

            return DefaultCycleMinutes;
        }

        public static double GetCycleDurationSeconds(int cycleMinutes)
        {
            return NormalizeCycleMinutes(cycleMinutes) * 60.0;
        }

        public static int GetSmallCycleInsightReward(int roll)
        {
            return 2 + Math.Abs(roll % 4);
        }

        public static int GetGrandCycleSpiritStoneReward(int roll)
        {
            return 5 + Math.Abs(roll % 11);
        }

        public static bool ShouldGrantMeditationBonus(int rollPercent)
        {
            return Math.Abs(rollPercent % 100) < 5;
        }

        public static string GetMeditationBonusType(int roll)
        {
            return MeditationBonusTypes[Math.Abs(roll % MeditationBonusTypes.Length)];
        }
    }
}
