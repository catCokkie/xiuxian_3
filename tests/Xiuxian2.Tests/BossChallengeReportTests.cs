using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class BossChallengeReportTests
{
    [Fact]
    public void ObservationCapsHealingAndDamageAtRealHpIncludingTimeoutRound()
    {
        var logic = new ExploreGameLogic { InBattle = true, PlayerHp = 10, EnemyHp = 20, EnemyMaxHp = 20, EnemyAttackPower = 1, EnemyDamageDividerRuntime = 1, InputsPerBattleRoundRuntime = 1 };
        var attempt = new BossChallengeAttempt(); attempt.Begin("zone", "boss");
        var result = logic.AdvanceBattleByInput(10, new CharacterStatBlock(100, 5, 0, 1, 0, 1.5), Array.Empty<EquipmentStatProfile>(), true, 4,
            monsterProfile: new MonsterStatProfile("boss", "boss", new CharacterStatBlock(20, 1, 0, 1, 0, 1.5), 1, HealPerRound: 1000),
            healPerRound: 1000, roundObserved: attempt.Observe);
        Assert.True(result.BossTimedOut); Assert.Equal(4, result.RoundsResolved);
        Assert.Equal(20, attempt.DamageToBoss); Assert.Equal(20, attempt.EnemyHealing); Assert.Equal(94, attempt.PlayerHealing);
    }
    [Fact]
    public void LethalOverkillIsNotCountedAsActualDamageOrHealing()
    {
        var logic = new ExploreGameLogic { InBattle = true, PlayerHp = 100, EnemyHp = 3, EnemyMaxHp = 20, EnemyAttackPower = 1, InputsPerBattleRoundRuntime = 1 };
        var rounds = new List<BattleRoundObservation>();
        var result = logic.AdvanceBattleByInput(10, new CharacterStatBlock(100, 500, 0, 100, 0, 1.5), Array.Empty<EquipmentStatProfile>(), true, 4,
            monsterProfile: new MonsterStatProfile("boss", "boss", new CharacterStatBlock(20, 1, 0, 1, 0, 1.5), 1, HealPerRound: 1000),
            healPerRound: 1000, roundObserved: rounds.Add);
        Assert.Equal(BattleOutcome.PlayerWon, result.Outcome);
        Assert.Equal(new BattleRoundObservation(3, 0, 0), Assert.Single(rounds));
    }
    [Fact]
    public void LethalRoundBeforePlayerAttackCannotInventDamageOrRecovery()
    {
        var logic = new ExploreGameLogic { InBattle = true, PlayerHp = 1, EnemyHp = 20, EnemyMaxHp = 20, EnemyAttackPower = 100, EnemyDamageDividerRuntime = 1, InputsPerBattleRoundRuntime = 1 };
        var rounds = new List<BattleRoundObservation>();
        var result = logic.AdvanceBattleByInput(1, new CharacterStatBlock(100, 5, 0, 1, 0, 1.5), Array.Empty<EquipmentStatProfile>(), true, 4,
            monsterProfile: new MonsterStatProfile("boss", "boss", new CharacterStatBlock(20, 100, 0, 100, 0, 1.5), 1, HealPerRound: 1000),
            healPerRound: 1000, roundObserved: rounds.Add);
        Assert.Equal(BattleOutcome.MonsterWon, result.Outcome); Assert.Equal(new BattleRoundObservation(0, 0, 0), Assert.Single(rounds));
    }
    [Fact]
    public void SuppressedAndClippedEnemyHealingUsesObservedValues()
    {
        var logic = new ExploreGameLogic { InBattle = true, PlayerHp = 100, EnemyHp = 20, EnemyMaxHp = 20, EnemyAttackPower = 1, InputsPerBattleRoundRuntime = 1 };
        var attempt = new BossChallengeAttempt(); attempt.Begin("zone", "boss");
        logic.AdvanceBattleByInput(3, new CharacterStatBlock(100, 5, 0, 1, 0, 1.5), Array.Empty<EquipmentStatProfile>(), true, 3,
            monsterProfile: new MonsterStatProfile("boss", "boss", new CharacterStatBlock(20, 1, 0, 1, 0, 1.5), 1, HealPerRound: 1000), suppressMonsterHealingRounds: 2, roundObserved: attempt.Observe);
        Assert.Equal(15, attempt.EnemyHealing); Assert.Equal(15, attempt.DamageToBoss);
    }
    [Fact]
    public void ReportSnapshotsRemainStableAfterNewAttemptsAndPreserveLargeTimestamp()
    {
        var attempt = new BossChallengeAttempt(); attempt.Begin("zone", "boss"); attempt.Observe(new(10, 4, 2));
        attempt.RecordPotion("potion_huiqi_dan", 5); attempt.RecordTalisman("talisman_fire_charm", 3);
        var report = attempt.Finish("death", 3, 14, 20, 5000000000L); attempt.Begin("other", "boss2");
        var saved = BossChallengeReport.Restore(report.ToPlainDictionary())!;
        Assert.Equal(13, saved.DamageToBoss); Assert.Equal(7, saved.PlayerHealing); Assert.Equal(4, saved.EnemyHealing);
        Assert.Equal(1, saved.Potions["potion_huiqi_dan"]); Assert.Equal(1, saved.Talismans["talisman_fire_charm"]);
        Assert.Equal(5000000000L, saved.TimestampUnix); Assert.True(saved.StatisticsComplete);
        Assert.Contains("破甲符×1", BossChallengeReportRules.Details(saved));
    }
    [Fact]
    public void MidBattleSaveRestoresCountersOnlyForTheSameBattleIdentity()
    {
        var attempt = new BossChallengeAttempt(); attempt.Begin("zone", "boss"); attempt.Observe(new(5, 2, 0)); attempt.RecordPotion("potion_huiqi_dan", 3);
        var restored = new BossChallengeAttempt(); restored.Restore(attempt.ToPlainDictionary(), "zone", "boss");
        Assert.True(restored.StatisticsComplete); Assert.Equal(5, restored.DamageToBoss); Assert.Equal(3, restored.PlayerHealing);
        restored.Restore(attempt.ToPlainDictionary(), "other", "boss"); Assert.False(restored.StatisticsComplete); Assert.Equal(0, restored.DamageToBoss);
    }
    [Fact]
    public void MissingLegacyCountersAreExplicitlyIncompleteAndNeverInferred()
    {
        var attempt = new BossChallengeAttempt(); attempt.Restore(new Dictionary<string, object>(), "zone", "boss"); attempt.Observe(new(1, 1, 0));
        var report = attempt.Finish("timeout", 80, 100, 100, 1);
        Assert.False(report.StatisticsComplete); Assert.Contains("仅含已有记录", BossChallengeReportRules.Details(report));
        Assert.Single(BossChallengeReportRules.Advice(report));
    }
    [Theory]
    [InlineData("death", "护具")]
    [InlineData("timeout", "武器")]
    [InlineData("double_knockout", "先手")]
    public void AdviceStartsFromTheActualFailureAndNeverPromisesVictory(string outcome, string direction)
    {
        var attempt = new BossChallengeAttempt(); attempt.Begin("zone", "boss"); attempt.Observe(new(100, 30, 0));
        var report = attempt.Finish(outcome, 10, 30, 100, 1);
        var advice = BossChallengeReportRules.Advice(report);
        Assert.Equal(2, advice.Count); Assert.Contains(direction, advice[0]); Assert.Contains("镇灵符阻断前5回合", advice[1]);
    }
    [Fact]
    public void VictoryHasNoFailureAdviceAndCorruptReportsAreConservative()
    {
        var attempt = new BossChallengeAttempt(); attempt.Begin("zone", "boss"); var report = attempt.Finish("victory", 1, 0, 100, 1);
        Assert.Empty(BossChallengeReportRules.Advice(report)); Assert.Null(BossChallengeReport.Restore(new Dictionary<string, object>()));
        var data = report.ToPlainDictionary(); data["enemy_healing"] = "invented";
        Assert.False(BossChallengeReport.Restore(data)!.StatisticsComplete);
        data["result"] = "unknown"; Assert.Null(BossChallengeReport.Restore(data));
    }
    [Fact]
    public void MigrationAddsEmptyHistoryAndIncompleteAttemptWithoutRewritingBattle()
    {
        var store = new SaveMigrationRules.MigrationStore(); var goal = new ChallengeGoalPlan(); goal.Start("zone", "boss", new Dictionary<string, int> { ["recipe_huiqi_dan"] = 2 });
        var state = goal.ToPlainDictionary(); var runtime = new Dictionary<string, object> { ["battle_state"] = "in_battle", ["player_hp"] = 7, ["enemy_hp"] = 30, ["battle_round_counter"] = 6 };
        store.SetValue("challenge_goal", "state", state); store.SetValue("explore", "runtime", runtime); SaveMigrationRules.MigrateToLatest(store, 20);
        Assert.Empty((Dictionary<string, object>)state["last_boss_report"]); Assert.Empty((Dictionary<string, object>)runtime["boss_attempt"]);
        Assert.Equal(7, runtime["player_hp"]); Assert.Equal(30, runtime["enemy_hp"]); Assert.Equal(6, runtime["battle_round_counter"]);
        Assert.Equal(2, ((Dictionary<string, object>)state["stock_targets"])["recipe_huiqi_dan"]);
        var attempt = new BossChallengeAttempt(); attempt.Begin("zone", "boss"); var report = attempt.Finish("death", 6, 30, 100, 1).ToPlainDictionary();
        state["last_boss_report"] = report; runtime["boss_attempt"] = attempt.ToPlainDictionary(); SaveMigrationRules.MigrateToLatest(store, 20);
        Assert.Same(report, state["last_boss_report"]); Assert.True(((Dictionary<string, object>)runtime["boss_attempt"])["statistics_complete"] is true);
    }
}
