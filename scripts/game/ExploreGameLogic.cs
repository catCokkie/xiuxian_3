using System;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    public sealed class ExploreGameLogic
    {
        public readonly record struct ExploreProgressChangedEvent(float Progress, bool CompletedLevel);
        public readonly record struct BattleStartedEvent(int MonsterIndex, string MonsterId, string MonsterName, bool IsBossBattle);
        public readonly record struct BattleEndedEvent(BattleOutcome Outcome, bool IsBossBattle, bool BossTimedOut);
        public readonly record struct LevelCompletedEvent(float Progress);
        public readonly record struct ExploreAdvanceResult(float Progress, bool CompletedLevel);
        public readonly record struct BattleAdvanceResult(
            int Threshold,
            int PendingInputs,
            int RoundsResolved,
            bool BattleEnded,
            BattleOutcome Outcome,
            bool BossTimedOut);

        public sealed class RuntimeState
        {
            public string ZoneName { get; init; } = "Unknown Zone";
            public float ExploreProgress { get; init; }
            public int MoveFrameCounter { get; init; }
            public int QueueMoveInputPending { get; init; }
            public bool InBattle { get; init; }
            public int PlayerHp { get; init; } = 36;
            public int PlayerMaxHp { get; init; } = 36;
            public double RecoveryRemainder { get; init; }
            public int EnemyHp { get; init; } = 24;
            public int EnemyMaxHp { get; init; } = 24;
            public int EnemyAttackPower { get; init; } = 4;
        public int InputsPerBattleRoundRuntime { get; init; } = GameBalanceConstants.Explore.InputsPerBattleRound;
            public int PlayerAttackPerRoundRuntime { get; init; } = 4;
            public int EnemyDamageDividerRuntime { get; init; } = 4;
            public int EnemyMinDamageRuntime { get; init; } = 1;
            public int BattleRoundCounter { get; init; }
            public int PendingBattleInputEvents { get; init; }
            public int BattleMonsterIndex { get; init; } = -1;
            public string BattleMonsterId { get; init; } = string.Empty;
            public string BattleMonsterName { get; init; } = "Enemy";
            public bool LastBattleEndedByBossTimeout { get; init; }
            public bool BossWeaknessInsightApplied { get; init; }
            public int TotalBattleCount { get; init; }
            public int TotalBattleWinCount { get; init; }
        }

        public event Action<ExploreProgressChangedEvent>? ProgressChanged;
        public event Action<BattleStartedEvent>? BattleStarted;
        public event Action<BattleEndedEvent>? BattleEnded;
        public event Action<LevelCompletedEvent>? LevelCompleted;

        public string CurrentZoneName { get; set; } = "Unknown Zone";
        public float ExploreProgress { get; set; }
        public int MoveFrameCounter { get; set; }
        public int QueueMoveInputPending { get; set; }
        public bool InBattle { get; set; }
        public int BattleRoundCounter { get; set; }
        public int PendingBattleInputEvents { get; set; }
        public int BattleMonsterIndex { get; set; } = -1;
        public string BattleMonsterId { get; set; } = string.Empty;
        public string BattleMonsterName { get; set; } = "Enemy";
        public int EnemyHp { get; set; } = 24;
        public int EnemyMaxHp { get; set; } = 24;
        public int PlayerHp { get; set; } = 36;
        public int PlayerMaxHp { get; set; } = 36;
        public double RecoveryRemainder { get; set; }

        public bool CanStartBattle => !InBattle && BattleRecoveryRules.CanFight(PlayerHp, PlayerMaxHp);

        public bool AdvanceRecovery(double seconds)
        {
            var next = BattleRecoveryRules.Advance(PlayerHp, PlayerMaxHp, seconds, RecoveryRemainder, InBattle);
            bool changed = PlayerHp != next.Hp;
            PlayerHp = next.Hp;
            RecoveryRemainder = next.Remainder;
            return changed;
        }
        public int EnemyAttackPower { get; set; } = 4;
        public int InputsPerBattleRoundRuntime { get; set; } = GameBalanceConstants.Explore.InputsPerBattleRound;
        public int PlayerAttackPerRoundRuntime { get; set; } = 4;
        public int EnemyDamageDividerRuntime { get; set; } = 4;
        public int EnemyMinDamageRuntime { get; set; } = 1;
        public bool LastBattleEndedByBossTimeout { get; set; }
        public bool BossWeaknessInsightApplied { get; set; }
        public int TotalBattleCount { get; set; }
        public int TotalBattleWinCount { get; set; }

        public void ApplyLevelConfig(
            string zoneName,
            int playerMaxHp,
            int playerAttackPerRound,
            int enemyDamageDivider,
            int enemyMinDamage)
        {
            if (!string.IsNullOrEmpty(zoneName))
            {
                CurrentZoneName = zoneName;
            }

            PlayerMaxHp = Math.Max(1, playerMaxHp);
            PlayerAttackPerRoundRuntime = Math.Max(1, playerAttackPerRound);
            EnemyDamageDividerRuntime = Math.Max(1, enemyDamageDivider);
            EnemyMinDamageRuntime = Math.Max(1, enemyMinDamage);
            PlayerHp = Math.Clamp(PlayerHp, 0, PlayerMaxHp);
        }

        public void RegisterTrackMovement(int movedFrames, int queueMoveInputPending)
        {
            MoveFrameCounter = Math.Max(0, MoveFrameCounter + Math.Max(0, movedFrames));
            QueueMoveInputPending = Math.Max(0, queueMoveInputPending);
        }

        public ExploreAdvanceResult AdvanceExploreByInput(int inputEvents, float progressPerInput, float maxProgress)
        {
            (float nextProgress, bool completedLevel) = ExploreProgressRules.AdvanceProgress(
                ExploreProgress,
                inputEvents,
                progressPerInput,
                maxProgress);

            ExploreProgress = DungeonLoopRules.ResolveProgressAfterExploreCompletion(nextProgress, completedLevel, maxProgress);
            ProgressChanged?.Invoke(new ExploreProgressChangedEvent(ExploreProgress, completedLevel));
            if (completedLevel)
            {
                LevelCompleted?.Invoke(new LevelCompletedEvent(ExploreProgress));
            }

            return new ExploreAdvanceResult(ExploreProgress, completedLevel);
        }

        public bool TryStartEncounter(
            int candidateIndex,
            float candidateX,
            float battleTriggerX,
            string monsterId,
            MonsterStatProfile? profile,
            int defaultInputsPerBattleRound,
            double baseEncounterRate = 1.0,
            int playerRealmLevel = 1,
            int zoneDangerLevel = 1,
            double randomRoll = 0.0)
        {
            if (!CanStartBattle)
            {
                return false;
            }

            BattleEncounterDecision encounter = BattleStartRules.DetermineEncounterStart(candidateIndex, candidateX, battleTriggerX, monsterId, baseEncounterRate, playerRealmLevel, zoneDangerLevel, randomRoll);
            if (!encounter.ShouldStart)
            {
                return false;
            }

            StartBattle(encounter, profile, defaultInputsPerBattleRound, isBossBattle: false);
            return true;
        }

        public bool TryStartBossChallenge(
            string bossMonsterId,
            MonsterStatProfile? profile,
            int defaultInputsPerBattleRound,
            float maxProgress)
        {
            if (!CanStartBattle || ExploreProgress < maxProgress)
            {
                return false;
            }

            if (!DungeonLoopRules.ShouldEnterBossChallenge(ExploreProgress >= maxProgress, bossMonsterId))
            {
                return false;
            }

            BattleEncounterDecision encounter = BattleStartRules.BuildBossEncounter(bossMonsterId);
            if (!encounter.ShouldStart)
            {
                return false;
            }

            StartBattle(encounter, profile, defaultInputsPerBattleRound, isBossBattle: true);
            return true;
        }

        public BattleAdvanceResult AdvanceBattleByInput(
            int inputEvents,
            CharacterStatBlock playerBaseStats,
            IReadOnlyList<EquipmentStatProfile> equippedProfiles,
            bool isBossBattle,
            int maxBossBattleRounds,
            Action? afterRoundResolved = null,
            MonsterStatProfile? monsterProfile = null,
            Func<double>? criticalRoll = null,
            int suppressMonsterHealingRounds = 0,
            int healPerRound = 0,
            double incomingDamageReduction = 0,
            IReadOnlyList<CharacterStatModifier>? extraModifiers = null)
        {
            BattleInputProgress progress = BattleRules.ConsumeBattleInputs(PendingBattleInputEvents, inputEvents, InputsPerBattleRoundRuntime);
            PendingBattleInputEvents = progress.RemainingInputs;
            if (progress.RoundsToResolve <= 0)
            {
                return new BattleAdvanceResult(progress.Threshold, progress.PendingInputs, 0, false, BattleOutcome.Ongoing, false);
            }

            for (int i = 0; i < progress.RoundsToResolve; i++)
            {
                BattleRoundCounter++;
                CharacterStatBlock playerStats = CharacterStatRules.BuildFinalStats(playerBaseStats,
                    equippedProfiles ?? Array.Empty<EquipmentStatProfile>(), extraModifiers ?? Array.Empty<CharacterStatModifier>());
                PlayerMaxHp = playerStats.MaxHp;
                CharacterBattleSnapshot playerSnapshot = CharacterStatRules.CreateBattleSnapshot(playerStats, PlayerHp);
                CharacterStatBlock enemyStats = monsterProfile?.BaseStats ?? new CharacterStatBlock(EnemyMaxHp, EnemyAttackPower, 0, 1, 0, 1.5);
                CharacterBattleSnapshot monsterSnapshot = CharacterStatRules.CreateBattleSnapshot(
                    enemyStats with { MaxHp = EnemyMaxHp, Attack = Math.Max(1, (int)Math.Round(EnemyAttackPower * (1 - incomingDamageReduction))) }, EnemyHp);
                BattleRoundResult roundResult = BattleRules.ResolvePlayerVsMonsterRound(
                    playerSnapshot,
                    monsterSnapshot,
                    EnemyDamageDividerRuntime,
                    EnemyMinDamageRuntime, criticalRoll?.Invoke() ?? 1.0, criticalRoll?.Invoke() ?? 1.0);

                EnemyHp = roundResult.Monster.CurrentHp;
                PlayerHp = roundResult.Player.CurrentHp;
                if (roundResult.Outcome == BattleOutcome.Ongoing)
                {
                    if (BattleRoundCounter > Math.Max(0, suppressMonsterHealingRounds)) EnemyHp = Math.Min(EnemyMaxHp, EnemyHp + Math.Max(0, monsterProfile?.HealPerRound ?? 0));
                    PlayerHp = Math.Min(roundResult.Player.MaxHp, PlayerHp + Math.Max(0, healPerRound));
                }
                if (roundResult.Outcome == BattleOutcome.Ongoing
                    && !(isBossBattle && BattleRoundCounter >= maxBossBattleRounds)) afterRoundResolved?.Invoke();

                BattleFlowDecision flowDecision = BattleRules.DetermineBattleFlow(roundResult.Outcome);
                if (flowDecision.EndBattle)
                {
                    return new BattleAdvanceResult(progress.Threshold, PendingBattleInputEvents, i + 1, true, roundResult.Outcome, false);
                }

                if (isBossBattle && BossEncounterRules.ResolveBossTimeout(BattleRoundCounter, maxBossBattleRounds) == BattleOutcome.MonsterWon)
                {
                    LastBattleEndedByBossTimeout = true;
                    return new BattleAdvanceResult(progress.Threshold, PendingBattleInputEvents, i + 1, true, BattleOutcome.MonsterWon, true);
                }
            }

            return new BattleAdvanceResult(progress.Threshold, PendingBattleInputEvents, progress.RoundsToResolve, false, BattleOutcome.Ongoing, false);
        }

        public BattleDefeatDecision HandleBattleDefeat(string activeLevelId, bool isBossBattle)
        {
            BattleDefeatDecision defeat = BattleLifecycleRules.DetermineDefeatReset(activeLevelId, isBossBattle);
            InBattle = false;
            BattleRoundCounter = 0;
            PendingBattleInputEvents = 0;
            if (defeat.ShouldResetExploreProgress)
            {
                ExploreProgress = DungeonLoopRules.ResolveProgressAfterBossBattle(isBossBattle, ExploreProgress);
            }

            PlayerHp = 0;
            RecoveryRemainder = 0;
            BattleMonsterIndex = -1;
            BattleMonsterId = string.Empty;
            TotalBattleCount++;
            BattleEnded?.Invoke(new BattleEndedEvent(BattleOutcome.MonsterWon, isBossBattle, LastBattleEndedByBossTimeout));
            return defeat;
        }

        public BattleVictoryDecision CompleteBattle(string activeLevelId, bool isBossBattle)
        {
            BattleVictoryDecision victory = BattleLifecycleRules.DetermineVictorySettlement(activeLevelId, BattleMonsterId, isBossBattle);
            InBattle = false;
            TotalBattleCount++;
            TotalBattleWinCount++;
            if (victory.ShouldResetExploreProgress)
            {
                ExploreProgress = DungeonLoopRules.ResolveProgressAfterBossBattle(isBossBattle, ExploreProgress);
            }

            PendingBattleInputEvents = 0;
            BattleMonsterIndex = -1;
            BattleMonsterId = string.Empty;
            BattleEnded?.Invoke(new BattleEndedEvent(BattleOutcome.PlayerWon, isBossBattle, false));
            return victory;
        }

        public void ResetBattleTrackState(int defaultInputsPerBattleRound)
        {
            InBattle = false;
            BattleMonsterIndex = -1;
            BattleMonsterId = string.Empty;
            BattleMonsterName = "Enemy";
            PendingBattleInputEvents = 0;
            BattleRoundCounter = 0;
            QueueMoveInputPending = 0;
            EnemyMaxHp = 24;
            EnemyAttackPower = 4;
            InputsPerBattleRoundRuntime = Math.Max(1, defaultInputsPerBattleRound);
            EnemyHp = EnemyMaxHp;
            // Changing region or restarting the track is not a healing action.
            LastBattleEndedByBossTimeout = false;
            BossWeaknessInsightApplied = false;
        }

        public bool CanApplyBossWeaknessInsight(bool isBossBattle, int dungeonMasteryLevel)
        {
            return BossEncounterRules.CanApplyWeaknessInsight(isBossBattle, BossWeaknessInsightApplied, dungeonMasteryLevel);
        }

        public bool TryApplyBossWeaknessInsight(bool isBossBattle)
        {
            if (!isBossBattle || BossWeaknessInsightApplied)
            {
                return false;
            }

            double reduction = SubsystemMasteryRules.GetEffectValue(
                PlayerActionState.ModeDungeon,
                4,
                SubsystemMasteryRules.DungeonBossWeaknessEffectId);
            BossWeaknessInsightApplied = true;
            EnemyMaxHp = Math.Max(1, (int)Math.Round(EnemyMaxHp * (1.0 - reduction)));
            EnemyHp = Math.Min(EnemyHp, EnemyMaxHp);
            EnemyAttackPower = Math.Max(1, (int)Math.Round(EnemyAttackPower * (1.0 - reduction)));
            return true;
        }

        public RuntimeState CaptureRuntimeState()
        {
            return new RuntimeState
            {
                ZoneName = CurrentZoneName,
                ExploreProgress = ExploreProgress,
                MoveFrameCounter = MoveFrameCounter,
                QueueMoveInputPending = QueueMoveInputPending,
                InBattle = InBattle,
                PlayerHp = PlayerHp,
                PlayerMaxHp = PlayerMaxHp,
                RecoveryRemainder = RecoveryRemainder,
                EnemyHp = EnemyHp,
                EnemyMaxHp = EnemyMaxHp,
                EnemyAttackPower = EnemyAttackPower,
                InputsPerBattleRoundRuntime = InputsPerBattleRoundRuntime,
                PlayerAttackPerRoundRuntime = PlayerAttackPerRoundRuntime,
                EnemyDamageDividerRuntime = EnemyDamageDividerRuntime,
                EnemyMinDamageRuntime = EnemyMinDamageRuntime,
                BattleRoundCounter = BattleRoundCounter,
                PendingBattleInputEvents = PendingBattleInputEvents,
                BattleMonsterIndex = BattleMonsterIndex,
                BattleMonsterId = BattleMonsterId,
                BattleMonsterName = BattleMonsterName,
                LastBattleEndedByBossTimeout = LastBattleEndedByBossTimeout,
                BossWeaknessInsightApplied = BossWeaknessInsightApplied,
                TotalBattleCount = TotalBattleCount,
                TotalBattleWinCount = TotalBattleWinCount,
            };
        }

        public void RestoreRuntimeState(RuntimeState state)
        {
            CurrentZoneName = string.IsNullOrEmpty(state.ZoneName) ? "Unknown Zone" : state.ZoneName;
            ExploreProgress = state.ExploreProgress;
            MoveFrameCounter = Math.Max(0, state.MoveFrameCounter);
            QueueMoveInputPending = Math.Max(0, state.QueueMoveInputPending);
            InBattle = state.InBattle;
            PlayerHp = Math.Max(0, state.PlayerHp);
            RecoveryRemainder = double.IsFinite(state.RecoveryRemainder) ? Math.Clamp(state.RecoveryRemainder, 0, .999999999) : 0;
            PlayerMaxHp = Math.Max(1, state.PlayerMaxHp);
            EnemyHp = Math.Max(0, state.EnemyHp);
            EnemyMaxHp = Math.Max(1, state.EnemyMaxHp);
            EnemyAttackPower = Math.Max(1, state.EnemyAttackPower);
            InputsPerBattleRoundRuntime = Math.Max(1, state.InputsPerBattleRoundRuntime);
            PlayerAttackPerRoundRuntime = Math.Max(1, state.PlayerAttackPerRoundRuntime);
            EnemyDamageDividerRuntime = Math.Max(1, state.EnemyDamageDividerRuntime);
            EnemyMinDamageRuntime = Math.Max(1, state.EnemyMinDamageRuntime);
            BattleRoundCounter = Math.Max(0, state.BattleRoundCounter);
            PendingBattleInputEvents = Math.Max(0, state.PendingBattleInputEvents);
            BattleMonsterIndex = state.BattleMonsterIndex;
            BattleMonsterId = state.BattleMonsterId ?? string.Empty;
            BattleMonsterName = string.IsNullOrEmpty(state.BattleMonsterName) ? "Enemy" : state.BattleMonsterName;
            LastBattleEndedByBossTimeout = state.LastBattleEndedByBossTimeout;
            BossWeaknessInsightApplied = state.BossWeaknessInsightApplied;
            TotalBattleCount = Math.Max(0, state.TotalBattleCount);
            TotalBattleWinCount = Math.Max(0, state.TotalBattleWinCount);
        }

        private void StartBattle(BattleEncounterDecision encounter, MonsterStatProfile? profile, int defaultInputsPerBattleRound, bool isBossBattle)
        {
            BattleStartSetup setup = BattleStartRules.BuildStartSetup(encounter.MonsterId, profile, defaultInputsPerBattleRound);
            InBattle = true;
            LastBattleEndedByBossTimeout = false;
            BossWeaknessInsightApplied = false;
            BattleMonsterIndex = encounter.MonsterIndex;
            BattleMonsterId = setup.MonsterId;
            BattleMonsterName = setup.MonsterName;
            EnemyMaxHp = setup.EnemyMaxHp;
            EnemyAttackPower = setup.EnemyAttack;
            InputsPerBattleRoundRuntime = setup.InputsPerRound;
            BattleRoundCounter = setup.BattleRoundCounter;
            PendingBattleInputEvents = setup.PendingBattleInputEvents;
            EnemyHp = EnemyMaxHp;
            BattleStarted?.Invoke(new BattleStartedEvent(BattleMonsterIndex, BattleMonsterId, BattleMonsterName, isBossBattle));
        }
    }
}
