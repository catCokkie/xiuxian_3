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

    [Fact]
    public void GetEffectiveMaxDurability_IncreasesAtMasteryLevel2()
    {
        int durLv1 = MiningRules.GetEffectiveMaxDurability(1);
        int durLv2 = MiningRules.GetEffectiveMaxDurability(2);

        Assert.Equal(100, durLv1);
        Assert.Equal(120, durLv2);
    }

    [Fact]
    public void GetDoubleOutputChance_ZeroAtLevel1_TenPercentAtLevel4()
    {
        Assert.Equal(0.0, MiningRules.GetDoubleOutputChance(1));
        Assert.Equal(0.10, MiningRules.GetDoubleOutputChance(4));
    }

    [Fact]
    public void TryGetNode_ReturnsFalseForUnknownRecipe()
    {
        bool found = MiningRules.TryGetNode("mining_nonexistent", out _);
        Assert.False(found);
    }

    [Fact]
    public void GatherOre_ReturnsDefaultForUnknownRecipe()
    {
        MiningRules.GatherResult result = MiningRules.GatherOre("mining_nonexistent");
        Assert.Null(result.ItemId);
        Assert.Equal(0, result.ItemCount);
    }

    [Fact]
    public void MiningPersistence_RoundTripsSnapshotViaPlainDictionary()
    {
        var original = new MiningPersistenceRules.MiningSnapshot("mining_cold_iron_ore", 45.0f, 180.0f, 73);
        var dict = MiningPersistenceRules.ToPlainDictionary(original);
        var restored = MiningPersistenceRules.FromPlainDictionary(dict);

        Assert.Equal(original.SelectedRecipeId, restored.SelectedRecipeId);
        Assert.Equal(original.Progress, restored.Progress, 2);
        Assert.Equal(original.RequiredProgress, restored.RequiredProgress, 2);
        Assert.Equal(original.CurrentDurability, restored.CurrentDurability);
    }
}
