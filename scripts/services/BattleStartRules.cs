using System;

namespace Xiuxian.Scripts.Services
{
    public static class BattleStartRules
    {
        public static double CalculateEncounterRate(double baseEncounterRate, int playerRealmLevel, int zoneDangerLevel)
        {
            int levelDiff = playerRealmLevel - zoneDangerLevel;
            double scaledRate = baseEncounterRate * (1.0 + (levelDiff * 0.05));
            return Math.Clamp(scaledRate, 0.05, 0.95);
        }

        public static BattleEncounterDecision DetermineEncounterStart(
            int candidateIndex,
            float candidateX,
            float battleTriggerX,
            string monsterId,
            double baseEncounterRate,
            int playerRealmLevel,
            int zoneDangerLevel,
            double randomRoll)
        {
            BattleEncounterDecision decision = BattleLifecycleRules.DetermineEncounterStart(candidateIndex, candidateX, battleTriggerX, monsterId);
            if (!decision.ShouldStart)
            {
                return decision;
            }

            double encounterRate = CalculateEncounterRate(baseEncounterRate, playerRealmLevel, zoneDangerLevel);
            return randomRoll <= encounterRate ? decision : new BattleEncounterDecision(false, -1, string.Empty);
        }

        public static BattleEncounterDecision BuildBossEncounter(string bossMonsterId)
        {
            if (string.IsNullOrEmpty(bossMonsterId))
            {
                return new BattleEncounterDecision(false, -1, string.Empty);
            }

            return new BattleEncounterDecision(true, -1, bossMonsterId);
        }

        public static BattleStartSetup BuildStartSetup(string monsterId, MonsterStatProfile? profile, int defaultInputsPerRound)
        {
            if (profile.HasValue)
            {
                MonsterStatProfile value = profile.Value;
                return new BattleStartSetup(
                    string.IsNullOrEmpty(monsterId) ? value.MonsterId : monsterId,
                    value.DisplayName,
                    value.BaseStats.MaxHp,
                    value.BaseStats.Attack,
                    Math.Max(1, value.InputsPerRound),
                    0,
                    0);
            }

            return new BattleStartSetup(
                monsterId,
                UiText.DefaultMonsterName,
                24,
                4,
                Math.Max(1, defaultInputsPerRound),
                0,
                0);
        }
    }
}
