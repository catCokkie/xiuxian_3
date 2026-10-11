using System;
namespace Xiuxian.Scripts.Services
{
    public static class OfflineCraftingRules
    {
        public static int InputBudget(double offlineSeconds, string actionId)
        {
            var activity = ActivityRegistry.GetBySystem(actionId);
            return activity?.SupportsOffline == true && activity.Category == ActivityCategory.Processing
                ? (int)Math.Floor(OfflineSettlementRules.CalculateOfflineInputBudget(offlineSeconds) * activity.OfflineEfficiency) : 0;
        }
    }
}
