using System;
namespace Xiuxian.Scripts.Services
{
    public static class OfflineCraftingRules
    {
        public static int InputBudgetFromShared(double inputs, string systemId)
        {
            var activity = ActivityRegistry.GetBySystem(systemId);
            return double.IsFinite(inputs) && activity?.Category == ActivityCategory.Processing && activity.SupportsOffline
                ? (int)Math.Floor(Math.Max(0, inputs) * activity.OfflineEfficiency) : 0;
        }
        public static int InputBudget(double offlineSeconds, string actionId)
        {
            var activity = ActivityRegistry.GetBySystem(actionId);
            return activity?.SupportsOffline == true && activity.Category == ActivityCategory.Processing
                ? (int)Math.Floor(OfflineSettlementRules.CalculateOfflineInputBudget(offlineSeconds) * activity.OfflineEfficiency) : 0;
        }
    }
}
