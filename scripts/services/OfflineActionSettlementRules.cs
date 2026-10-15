namespace Xiuxian.Scripts.Services
{
    // Shared time growth pays once. Offline activity progress reuses the same budget;
    // a paused recipe never cancels basic character growth. Real input remains separate.
    public static class OfflineActionSettlementRules
    {
        public static ActionSettlementResult BuildSharedFromBudget(double inputs, double realmMultiplier, string actionId, string targetId = "")
        {
            inputs = double.IsFinite(inputs) ? System.Math.Max(0, inputs) : 0;
            double ap = inputs * GameBalanceConstants.Offline.ApPerInput;
            return ActionSettlementRules.BuildCultivationSettlement(targetId, ap,
                ap * GameBalanceConstants.ResourceConversion.LingqiFactor * System.Math.Max(0, realmMultiplier),
                ap * GameBalanceConstants.ResourceConversion.InsightFactor,
                inputs * GameBalanceConstants.ResourceConversion.BaseExpPerInput) with { ActionId = actionId, SourceTag = "shared_basic_growth" };
        }
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
