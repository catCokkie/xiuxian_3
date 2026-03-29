using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class BossEncounterRulesTests
{
    [Fact]
    public void IsBossDefeated_MatchesZoneMembership()
    {
        Assert.True(BossEncounterRules.IsBossDefeated("lv_qi_001", new[] { "lv_qi_001" }));
        Assert.False(BossEncounterRules.IsBossDefeated("lv_qi_002", new[] { "lv_qi_001" }));
    }

    [Fact]
    public void ResolveBossTimeout_ReturnsMonsterWinAtRoundCap()
    {
        Assert.Equal(BattleOutcome.Ongoing, BossEncounterRules.ResolveBossTimeout(19, 20));
        Assert.Equal(BattleOutcome.MonsterWon, BossEncounterRules.ResolveBossTimeout(20, 20));
    }

    [Fact]
    public void CanApplyWeaknessInsight_RequiresBossChallengeAndDungeonMasteryLevel4()
    {
        Assert.True(BossEncounterRules.CanApplyWeaknessInsight(true, false, 4));
        Assert.False(BossEncounterRules.CanApplyWeaknessInsight(false, false, 4));
        Assert.False(BossEncounterRules.CanApplyWeaknessInsight(true, true, 4));
        Assert.False(BossEncounterRules.CanApplyWeaknessInsight(true, false, 3));
    }

    [Fact]
    public void BuildBossProfile_ScalesEliteIntoBossRange()
    {
        MonsterStatProfile elite = new(
            "monster_spider_cave",
            "Cave Brood Spider",
            new CharacterStatBlock(42, 8, 4, 95, 0.0, 1.5),
            InputsPerRound: 20,
            MoveCategory: "elite",
            IsBoss: false);

        MonsterStatProfile boss = BossEncounterRules.BuildBossProfile("monster_spider_cave_queen", "Cave Spider Queen", elite, 2.5);

        Assert.True(boss.IsBoss);
        Assert.Equal("boss", boss.MoveCategory);
        Assert.Equal(105, boss.BaseStats.MaxHp);
        Assert.True(boss.BaseStats.Attack >= 16);
    }
}
