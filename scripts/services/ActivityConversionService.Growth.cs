using Godot;
using System;

namespace Xiuxian.Scripts.Services
{
    public partial class ActivityConversionService : IDictionaryPersistable
    {
        public SharedGrowthClock GrowthClock { get; } = new();
        private bool _automaticGrowthEnabled;
        private const double GrowthSettlementInterval = 10;

        // Claiming updates only the time ledger; the caller commits the returned resource settlement.
        public ActionSettlementResult ClaimBasicGrowth(double nowUnix, double legacySavedUnix = 0)
            => OfflineActionSettlementRules.BuildSharedFromBudget(GrowthClock.Claim(nowUnix, legacySavedUnix),
                _progressState.GetRealmMultiplier(), _actionState?.ActionId ?? "dungeon", _actionState?.ActionTargetId ?? "");

        public ActionSettlementResult ApplyOnlineBasicGrowth(double nowUnix)
        {
            var result = ClaimBasicGrowth(nowUnix);
            _walletState.AddLingqi(result.LingqiGain); _walletState.AddInsight(result.InsightGain); _progressState.AddRealmExp(result.RealmExpGain);
            SessionOnlineBasicGrowthExperience += result.RealmExpGain;
            // No input events, active-time cycles, meals, production, battles or notifications are synthesized.
            return result;
        }
        private void AdvanceBasicGrowthClock()
        {
            if (!_automaticGrowthEnabled || _walletState == null || _progressState == null) return;
            double now = Time.GetUnixTimeFromSystem();
            if (GrowthClock.LastSettledUnix <= 0 || now - GrowthClock.LastSettledUnix >= GrowthSettlementInterval)
                ApplyOnlineBasicGrowth(now);
        }
        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
            => SaveValueConversionRules.ToVariantDictionary(GrowthClock.ToPlainDictionary());
        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
            => GrowthClock.Restore(SaveValueConversionRules.ToPlainDictionary(data));
    }
}
