using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class MiningRulesTests
{
    [Fact]
    public void CanMineNode_RespectsMasteryUnlocks()
    {
        Assert.True(MiningRules.CanMineNode("mining_cold_iron_ore", masteryLevel: 1));
        Assert.False(MiningRules.CanMineNode("mining_spirit_jade", masteryLevel: 1));
        Assert.True(MiningRules.CanMineNode("mining_spirit_jade", masteryLevel: 2));
        Assert.False(MiningRules.CanMineNode("mining_mithril", masteryLevel: 3));
        Assert.True(MiningRules.CanMineNode("mining_mithril", masteryLevel: 4));
    }

    [Fact]
    public void AdvanceProgress_CompletesBatchAndConsumesDurability()
    {
        MiningRules.MiningProgressDecision decision = MiningRules.AdvanceProgress(80.0f, 40, 100, currentDurability: 60);

        Assert.True(decision.CompletedBatch);
        Assert.Equal(0.0f, decision.NextProgress);
        Assert.Equal(59, decision.NextDurability);
        Assert.False(decision.RefreshedNode);
    }

    [Fact]
    public void AdvanceProgress_RefreshesNodeAfterLastDurabilityUse()
    {
        MiningRules.MiningProgressDecision decision = MiningRules.AdvanceProgress(90.0f, 20, 100, currentDurability: 1);

        Assert.True(decision.CompletedBatch);
        Assert.Equal(MiningRules.DefaultNodeDurability, decision.NextDurability);
        Assert.True(decision.RefreshedNode);
    }
}
