using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ExploreGameLogicTests
{
    [Fact]
    public void AdvanceExploreByInput_CompletesLevelAndFiresEvent()
    {
        ExploreGameLogic logic = new()
        {
            ExploreProgress = 98.0f
        };
        bool completedRaised = false;
        logic.LevelCompleted += _ => completedRaised = true;

        ExploreGameLogic.ExploreAdvanceResult result = logic.AdvanceExploreByInput(1, 2.0f, 100.0f);

        Assert.True(result.CompletedLevel);
        Assert.Equal(100.0f, logic.ExploreProgress);
        Assert.True(completedRaised);
    }

    [Fact]
    public void TryStartEncounter_StartsBattleWithConfiguredMonster()
    {
        ExploreGameLogic logic = new();
        bool startedRaised = false;
        logic.BattleStarted += evt => startedRaised = evt.MonsterId == "monster_alpha";
        MonsterStatProfile profile = new(
            "monster_alpha",
            "Forest Wolf",
            new CharacterStatBlock(32, 6, 1, 100, 0.0, 1.5),
            InputsPerRound: 14);

        bool started = logic.TryStartEncounter(0, 200.0f, 220.0f, "monster_alpha", profile, 18);

        Assert.True(started);
        Assert.True(logic.InBattle);
        Assert.Equal(0, logic.BattleMonsterIndex);
        Assert.Equal("monster_alpha", logic.BattleMonsterId);
        Assert.Equal("Forest Wolf", logic.BattleMonsterName);
        Assert.Equal(32, logic.EnemyMaxHp);
        Assert.True(startedRaised);
    }

    [Fact]
    public void HandleBattleDefeat_BossBattleResetsProgressAndCountsBattle()
    {
        ExploreGameLogic logic = new()
        {
            ExploreProgress = 100.0f,
            InBattle = true,
            BattleMonsterId = "monster_boss",
            BattleMonsterIndex = -1,
            PlayerMaxHp = 36,
            PlayerHp = 8
        };

        BattleDefeatDecision defeat = logic.HandleBattleDefeat("lv_qi_001", isBossBattle: true);

        Assert.True(defeat.ShouldResetExploreProgress);
        Assert.Equal(0.0f, logic.ExploreProgress);
        Assert.False(logic.InBattle);
        Assert.Equal(string.Empty, logic.BattleMonsterId);
        Assert.Equal(36, logic.PlayerHp);
        Assert.Equal(1, logic.TotalBattleCount);
    }
}
