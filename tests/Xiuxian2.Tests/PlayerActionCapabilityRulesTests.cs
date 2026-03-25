using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class PlayerActionCapabilityRulesTests
{
    [Fact]
    public void CultivationAction_HasSettlementAndInputExpCapabilities()
    {
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionCultivation, PlayerActionCapability.ConsumesApSettlement));
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionCultivation, PlayerActionCapability.GrantsCultivationInputExp));
        Assert.False(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionCultivation, PlayerActionCapability.AdvancesDungeon));
    }

    [Fact]
    public void DungeonAction_HasDungeonBattleAndLootCapabilities()
    {
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionDungeon, PlayerActionCapability.AdvancesDungeon));
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionDungeon, PlayerActionCapability.RunsBattle));
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionDungeon, PlayerActionCapability.GeneratesLoot));
        Assert.False(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionDungeon, PlayerActionCapability.ConsumesApSettlement));
    }

    [Fact]
    public void AlchemyAndSmithingActions_KeepApSettlementButPauseDungeonLoop()
    {
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionAlchemy, PlayerActionCapability.ConsumesApSettlement));
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionAlchemy, PlayerActionCapability.SupportsOfflineSettlement));
        Assert.False(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionAlchemy, PlayerActionCapability.AdvancesDungeon));

        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionSmithing, PlayerActionCapability.ConsumesApSettlement));
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionSmithing, PlayerActionCapability.SupportsOfflineSettlement));
        Assert.False(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionSmithing, PlayerActionCapability.RunsBattle));
    }
}
