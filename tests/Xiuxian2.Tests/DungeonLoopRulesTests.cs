using Xiuxian.Scripts.Game;

namespace Xiuxian.Tests;

public sealed class DungeonLoopRulesTests
{
    [Fact]
    public void ShouldEnterBossChallenge_RequiresLevelCompletionAndBossId()
    {
        Assert.True(DungeonLoopRules.ShouldEnterBossChallenge(true, "monster_boss"));
        Assert.False(DungeonLoopRules.ShouldEnterBossChallenge(false, "monster_boss"));
        Assert.False(DungeonLoopRules.ShouldEnterBossChallenge(true, string.Empty));
    }

    [Fact]
    public void ResolveProgressAfterExploreCompletion_PinsProgressAtMaxForBossChallenge()
    {
        float progress = DungeonLoopRules.ResolveProgressAfterExploreCompletion(0.0f, true, 100.0f);

        Assert.Equal(100.0f, progress);
    }

    [Fact]
    public void ResolveProgressAfterBossBattle_ResetsOnlyBossLoops()
    {
        Assert.Equal(0.0f, DungeonLoopRules.ResolveProgressAfterBossBattle(true, 100.0f));
        Assert.Equal(47.5f, DungeonLoopRules.ResolveProgressAfterBossBattle(false, 47.5f));
    }
}
