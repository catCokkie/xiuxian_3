namespace Xiuxian.Scripts.Services
{
    // The common input channel pays once. Activity progress uses that same budget,
    // like online crafting; a paused recipe never cancels basic character growth.
    public static class OfflineActionSettlementRules
    {
        public static ActionSettlementResult BuildShared(double seconds, double realmMultiplier, string actionId, string targetId = "")
        {
            var result = OfflineSettlementRules.BuildCultivationOfflineSettlement(seconds,
                GameBalanceConstants.Offline.ApPerInput, GameBalanceConstants.ResourceConversion.LingqiFactor,
                GameBalanceConstants.ResourceConversion.InsightFactor, GameBalanceConstants.ResourceConversion.BaseExpPerInput,
                realmMultiplier, targetId);
            return result with { ActionId = actionId, SourceTag = "offline_shared" };
        }

        public static ActionSettlementResult WithDungeonRewards(ActionSettlementResult shared, ActionSettlementResult dungeon)
            => shared with
            {
                ActionTargetId = dungeon.ActionTargetId,
                LingqiGain = shared.LingqiGain + dungeon.LingqiGain,
                InsightGain = shared.InsightGain + dungeon.InsightGain,
                ExploreProgressGain = dungeon.ExploreProgressGain,
                BattleRoundsAdvanced = dungeon.BattleRoundsAdvanced,
                ItemDrops = dungeon.ItemDrops,
                EquipmentDrops = dungeon.EquipmentDrops
            };
    }
}
