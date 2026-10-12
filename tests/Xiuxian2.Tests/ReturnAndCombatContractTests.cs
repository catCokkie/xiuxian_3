using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ReturnAndCombatContractTests
{
    [Theory]
    [InlineData("cultivation")]
    [InlineData("dungeon")]
    [InlineData("alchemy")]
    [InlineData("smithing")]
    [InlineData("talisman")]
    [InlineData("cooking")]
    [InlineData("formation")]
    [InlineData("garden")]
    [InlineData("mining")]
    [InlineData("fishing")]
    [InlineData("body_cultivation")]
    public void EveryExitModePaysTheSameBasicOfflineGrowth(string mode)
    {
        var result = OfflineActionSettlementRules.BuildShared(86400, 1, mode);
        Assert.Equal(6360 * .28, result.RealmExpGain, 6);
        Assert.Equal(6360 * .9, result.LingqiGain, 6);
        Assert.Equal(6360 * .08, result.InsightGain, 6);
        var longAbsence = OfflineActionSettlementRules.BuildShared(7 * 86400, 1, mode);
        Assert.Equal(result.RealmExpGain, longAbsence.RealmExpGain);
        Assert.Equal(result.LingqiGain, longAbsence.LingqiGain);
        Assert.Equal(result.ApConsumed, longAbsence.ApConsumed);
        Assert.Equal(0, OfflineActionSettlementRules.BuildShared(double.NaN, 1, mode).RealmExpGain);
    }

    [Fact]
    public void DungeonBonusCannotDoubleExperienceAndBossReadyStillPaysBasicGrowth()
    {
        var shared = OfflineActionSettlementRules.BuildShared(600, 1, "dungeon");
        var empty = shared with { ApConsumed = 0, LingqiGain = 0, InsightGain = 0, RealmExpGain = 0 };
        Assert.Equal(shared, OfflineActionSettlementRules.WithDungeonRewards(shared, empty));
        var dungeon = empty with { ActionTargetId = "lv_qi_001", LingqiGain = 10, RealmExpGain = 100, ExploreProgressGain = 1 };
        var combined = OfflineActionSettlementRules.WithDungeonRewards(shared, dungeon);
        Assert.Equal(shared.RealmExpGain, combined.RealmExpGain);
        Assert.Equal(shared.LingqiGain + 10, combined.LingqiGain);
        Assert.Equal("lv_qi_001", combined.ActionTargetId); // Quota consumption uses the resolved default region.
    }

    private static ExploreGameLogic HealingFight() => new()
    {
        InBattle = true, PlayerHp = 1000, PlayerMaxHp = 1000,
        EnemyHp = 100, EnemyMaxHp = 100, EnemyAttackPower = 1, InputsPerBattleRoundRuntime = 1
    };
    private static readonly CharacterStatBlock Player = new(1000, 10, 0, 100, 0, 1.5);
    private static readonly MonsterStatProfile HealingEnemy = new("healer", "healer", new(100, 1, 0, 1, 0, 1.5), 1, HealPerRound: 2);

    [Fact]
    public void SuppressionExpiresAfterFiveRoundsRegardlessOfBatchingOrReload()
    {
        var batched = HealingFight();
        batched.AdvanceBattleByInput(6, Player, Array.Empty<EquipmentStatProfile>(), false, 20,
            monsterProfile: HealingEnemy, suppressMonsterHealingRounds: 5);
        var resumed = HealingFight();
        resumed.AdvanceBattleByInput(3, Player, Array.Empty<EquipmentStatProfile>(), false, 20,
            monsterProfile: HealingEnemy, suppressMonsterHealingRounds: 5);
        var loaded = new ExploreGameLogic(); loaded.RestoreRuntimeState(resumed.CaptureRuntimeState());
        loaded.AdvanceBattleByInput(3, Player, Array.Empty<EquipmentStatProfile>(), false, 20,
            monsterProfile: HealingEnemy, suppressMonsterHealingRounds: 5);
        Assert.Equal(42, batched.EnemyHp);
        Assert.Equal(batched.EnemyHp, loaded.EnemyHp);
        var rewardOnly = HealingFight();
        rewardOnly.AdvanceBattleByInput(6, Player, Array.Empty<EquipmentStatProfile>(), false, 20, monsterProfile: HealingEnemy);
        Assert.Equal(52, rewardOnly.EnemyHp);
    }

    [Theory]
    [InlineData(0)] // lethal defeat
    [InlineData(1)] // victory
    [InlineData(2)] // timeout
    public void TerminalRoundsNeverTriggerAutomaticPotionConsumption(int ending)
    {
        var logic = HealingFight();
        var stats = Player;
        if (ending == 0) { logic.PlayerHp = 1; logic.EnemyAttackPower = 1000; stats = stats with { Attack = 1 }; }
        if (ending == 1) logic.EnemyHp = 1;
        if (ending == 2) logic.BattleRoundCounter = 19;
        int callbacks = 0;
        var result = logic.AdvanceBattleByInput(1, stats, Array.Empty<EquipmentStatProfile>(), ending == 2, 20,
            afterRoundResolved: () => callbacks++, monsterProfile: HealingEnemy);
        Assert.True(result.BattleEnded);
        Assert.Equal(0, callbacks);
        Assert.Empty(ConsumableUsageRules.DetermineAutoConsume(new(0, 100, false, false, false), new Dictionary<string, int> { ["potion_huiqi_dan"] = 1 }));
    }

    [Fact]
    public void StatsAreIndependentOfModifierOrderAndStarterDefenseActuallyImproves()
    {
        var equipment = EquipmentStarterLoadout.CreateDefaultProfiles();
        var basis = PlayerBaseStatRules.BuildBaseStats(5, 36, 4);
        var guard = FormationRules.GetModifier("formation_guard_flag", 2);
        var skin = ActivityEffectRules.CollectPermanentProgressModifier(new(5, 0, false, 0, BoneforgeCount: 1, BodyCultivationDefenseRate: .02));
        var plain = CharacterStatRules.BuildFinalStats(basis, equipment);
        Assert.True(CharacterStatRules.BuildFinalStats(basis, equipment, new[] { guard }).Defense > plain.Defense);
        Assert.True(CharacterStatRules.BuildFinalStats(basis, equipment, new[] { skin }).Defense > plain.Defense);
        var list = CharacterStatRules.CollectEquipmentModifiers(equipment).Concat(new[] { guard, skin }).ToArray();
        Assert.Equal(CharacterStatRules.BuildFinalStats(basis, list), CharacterStatRules.BuildFinalStats(basis, list.Reverse().ToArray()));
    }

    [Fact]
    public void RecoveryIsContinuousAcrossVictoryRegionChangeSaveAndDefeat()
    {
        var logic = new ExploreGameLogic { PlayerHp = 31, PlayerMaxHp = 100, InBattle = true };
        Assert.False(logic.AdvanceRecovery(20));
        logic.CompleteBattle("lv_qi_001", false);
        Assert.Equal(31, logic.PlayerHp);
        logic.ResetBattleTrackState(12);
        Assert.Equal(31, logic.PlayerHp);
        logic.AdvanceRecovery(.1);
        var loaded = new ExploreGameLogic(); loaded.RestoreRuntimeState(logic.CaptureRuntimeState());
        loaded.AdvanceRecovery(.1);
        Assert.Equal(32, loaded.PlayerHp);
        loaded.HandleBattleDefeat("lv_qi_001", false);
        Assert.Equal(0, loaded.PlayerHp);
        Assert.False(loaded.TryStartBossChallenge("boss", HealingEnemy, 1, 0));
        loaded.AdvanceRecovery(14);
        Assert.Equal(70, loaded.PlayerHp);
        Assert.True(loaded.CanStartBattle);
        loaded.AdvanceRecovery(6);
        Assert.Equal(100, loaded.PlayerHp);
    }

    [Fact]
    public void RecoveryIsFrameRateIndependentAndNeverNeedsInputOrOvershoots()
    {
        var fine = new ExploreGameLogic { PlayerHp = 0, PlayerMaxHp = 36 };
        var coarse = new ExploreGameLogic { PlayerHp = 0, PlayerMaxHp = 36 };
        for (int i = 0; i < 1400; i++) fine.AdvanceRecovery(.01);
        coarse.AdvanceRecovery(14);
        Assert.Equal(coarse.PlayerHp, fine.PlayerHp);
        fine.AdvanceRecovery(86400);
        Assert.Equal(36, fine.PlayerHp);
        Assert.Equal(0, fine.RecoveryRemainder);
    }
}
