using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    public static class EquipmentExchangeService
    {
        public static EquipmentInheritanceCheck Preview(ServiceLocator s, ExploreProgressController controller, string sourceId, string targetId)
        {
            s.EquippedItemsState!.TryGetEquippedProfileById(sourceId, out var source);
            s.BackpackState!.TryGetEquipmentById(targetId, out var target, out _);
            var check = EquipmentInheritanceRules.Evaluate(source, target, s.PlayerProgressState!.RealmLevel,
                s.SubsystemMasteryState!.GetLevel("smithing"), s.LevelConfigLoader!.GetBossDefeatedLevelIds().Contains("lv_qi_001"),
                s.ResourceWalletState!.Lingqi, controller.IsInBattle);
            return s.BackpackState.HasEquipment(sourceId) || s.EquippedItemsState.TryGetEquippedProfileById(targetId, out _)
                ? check with { BlockReason = "装备位置已变化，请重新选择。" } : check;
        }

        public static bool TryInherit(ServiceLocator s, ExploreProgressController controller, EquipmentInheritanceCheck reviewed, out string message)
        {
            var current = Preview(s, controller, reviewed.Source.EquipmentId ?? "", reviewed.Target.EquipmentId ?? "");
            message = current.BlockReason;
            if (!current.CanCommit) return false;
            if (!reviewed.CanCommit || current != reviewed)
            {
                message = "装备或继承上限已变化，请查看新预览后再确认。"; return false;
            }
            // Single-threaded: no signals run between validation and writing both gear stores.
            Swap(s, current.Target, current.Result, current.Source with { EnhanceLevel = 0 }, false);
            s.ResourceWalletState!.SpendLingqi(current.LingqiCost);
            s.PlayerStatsState?.RecordLingqiSpend(current.LingqiCost);
            s.SmithingState!.ResetAfterInheritance(current.Source.EquipmentId, current.Target.EquipmentId, current.ResultLevel);
            Publish(s);
            message = $"已继承并换装：{current.Target.DisplayName} +{current.ResultLevel}；旧装备 +0 已放回背包。消耗 {current.LingqiCost:0} 灵气。";
            s.EventLogState?.AddEvent(EventLogState.CategoryEquipment, message);
            return true;
        }

        public static bool TryEquipById(ServiceLocator s, string targetId)
        {
            if (!s.BackpackState!.TryGetEquipmentById(targetId, out var next, out _)
                || s.EquippedItemsState!.TryGetEquippedProfileById(targetId, out _)) return false;
            EquipmentStatProfile? old = s.EquippedItemsState.TryGetEquippedProfile(next.Slot, out var profile) ? profile : null;
            if (old.HasValue && s.BackpackState.HasEquipment(old.Value.EquipmentId)) return false;
            Swap(s, next, next, old, true);
            return true;
        }

        private static void Swap(ServiceLocator s, EquipmentStatProfile target, EquipmentStatProfile equipped,
            EquipmentStatProfile? returned, bool publish)
        {
            s.BackpackState!.TryGetEquipmentById(target.EquipmentId, out _, out var targetInstance);
            EquipmentInstanceData? oldInstance = null;
            if (returned.HasValue) s.EquippedItemsState!.TryGetEquippedInstanceById(returned.Value.EquipmentId, out oldInstance);
            s.BackpackState.CommitEquipmentSwap(target.EquipmentId, returned, oldInstance);
            s.EquippedItemsState!.CommitEquipmentSwap(equipped, targetInstance);
            if (publish) Publish(s);
        }

        private static void Publish(ServiceLocator s)
        {
            s.EquippedItemsState!.NotifyEquipmentChanged();
            s.BackpackState!.NotifyEquipmentChanged();
        }
    }
}
