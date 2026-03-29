using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class BossEncounterRules
    {
        public static bool IsBossDefeated(string zoneId, IReadOnlyCollection<string> defeatedBossZones)
        {
            if (string.IsNullOrEmpty(zoneId) || defeatedBossZones == null)
            {
                return false;
            }

            foreach (string defeatedZone in defeatedBossZones)
            {
                if (defeatedZone == zoneId)
                {
                    return true;
                }
            }

            return false;
        }

        public static BattleOutcome ResolveBossTimeout(int currentRound, int maxRounds)
        {
            return currentRound >= Math.Max(1, maxRounds)
                ? BattleOutcome.MonsterWon
                : BattleOutcome.Ongoing;
        }

        public static MonsterStatProfile BuildBossProfile(
            string bossMonsterId,
            string bossDisplayName,
            MonsterStatProfile eliteProfile,
            double multiplier)
        {
            double clampedMultiplier = Math.Clamp(multiplier, 2.0, 3.0);
            CharacterStatBlock baseStats = eliteProfile.BaseStats;
            return MonsterStatRules.BuildProfile(
                bossMonsterId,
                string.IsNullOrEmpty(bossDisplayName) ? $"{eliteProfile.DisplayName} Boss" : bossDisplayName,
                maxHp: (int)Math.Round(baseStats.MaxHp * clampedMultiplier),
                attack: (int)Math.Round(baseStats.Attack * Math.Max(1.5, clampedMultiplier - 0.4)),
                defense: (int)Math.Round(baseStats.Defense * Math.Max(1.25, clampedMultiplier - 0.75)),
                speedFactor: baseStats.Speed / 100.0,
                inputsPerRound: eliteProfile.InputsPerRound,
                moveCategory: "boss",
                isBoss: true);
        }

        public static bool CanApplyWeaknessInsight(bool isBossBattle, bool alreadyApplied, int dungeonMasteryLevel)
        {
            return isBossBattle
                && !alreadyApplied
                && dungeonMasteryLevel >= 4;
        }
    }
}
