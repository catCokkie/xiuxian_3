namespace Xiuxian.Scripts.Services
{
    public static class PlayerActionCapabilityRules
    {
        public static bool HasCapability(string actionId, PlayerActionCapability capability)
        {
            if (capability == PlayerActionCapability.SupportsOfflineSettlement)
                return NormalizeActionId(actionId) != PlayerActionState.ActionMining
                    && NormalizeActionId(actionId) != PlayerActionState.ActionFishing
                    && NormalizeActionId(actionId) != PlayerActionState.ActionBodyCultivation;
            return NormalizeActionId(actionId) switch
            {
                PlayerActionState.ActionCultivation => capability switch
                {
                    PlayerActionCapability.ConsumesApSettlement => true,
                    PlayerActionCapability.GrantsCultivationInputExp => true,
                    PlayerActionCapability.SupportsOfflineSettlement => true,
                    _ => false,
                },
                PlayerActionState.ActionAlchemy => capability switch
                {
                    PlayerActionCapability.ConsumesApSettlement => true,
                    PlayerActionCapability.SupportsOfflineSettlement => true,
                    _ => false,
                },
                PlayerActionState.ActionSmithing => capability switch
                {
                    PlayerActionCapability.ConsumesApSettlement => true,
                    PlayerActionCapability.SupportsOfflineSettlement => true,
                    _ => false,
                },
                PlayerActionState.ActionGarden or
                PlayerActionState.ActionMining or
                PlayerActionState.ActionFishing => capability switch
                {
                    PlayerActionCapability.ConsumesApSettlement => true,
                    PlayerActionCapability.GeneratesLoot => true,
                    PlayerActionCapability.SupportsOfflineSettlement => true,
                    _ => false,
                },
                PlayerActionState.ActionTalisman or
                PlayerActionState.ActionCooking or
                PlayerActionState.ActionFormation => capability switch
                {
                    PlayerActionCapability.ConsumesApSettlement => true,
                    PlayerActionCapability.SupportsOfflineSettlement => true,
                    _ => false,
                },
                PlayerActionState.ActionBodyCultivation => capability switch
                {
                    PlayerActionCapability.ConsumesApSettlement => true,
                    PlayerActionCapability.GrantsCultivationInputExp => false,
                    PlayerActionCapability.SupportsOfflineSettlement => true,
                    _ => false,
                },
                _ => capability switch
                {
                    PlayerActionCapability.ConsumesApSettlement => true,
                    PlayerActionCapability.AdvancesDungeon => true,
                    PlayerActionCapability.RunsBattle => true,
                    PlayerActionCapability.GeneratesLoot => true,
                    PlayerActionCapability.SupportsOfflineSettlement => true,
                    _ => false,
                },
            };
        }

        public static bool HasCapability(PlayerActionState? actionState, PlayerActionCapability capability)
        {
            string actionId = actionState?.ActionId ?? PlayerActionState.ActionDungeon;
            return HasCapability(actionId, capability);
        }

        public static string NormalizeActionId(string actionId)
        {
            return actionId switch
            {
                PlayerActionState.ActionCultivation => PlayerActionState.ActionCultivation,
                PlayerActionState.ActionAlchemy => PlayerActionState.ActionAlchemy,
                PlayerActionState.ActionSmithing => PlayerActionState.ActionSmithing,
                PlayerActionState.ActionGarden => PlayerActionState.ActionGarden,
                PlayerActionState.ActionMining => PlayerActionState.ActionMining,
                PlayerActionState.ActionFishing => PlayerActionState.ActionFishing,
                PlayerActionState.ActionTalisman => PlayerActionState.ActionTalisman,
                PlayerActionState.ActionCooking => PlayerActionState.ActionCooking,
                PlayerActionState.ActionFormation => PlayerActionState.ActionFormation,
                PlayerActionState.ActionBodyCultivation => PlayerActionState.ActionBodyCultivation,
                _ => PlayerActionState.ActionDungeon,
            };
        }
    }
}
