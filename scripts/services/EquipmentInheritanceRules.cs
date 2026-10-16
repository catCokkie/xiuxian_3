using System;

namespace Xiuxian.Scripts.Services
{
    public readonly record struct EquipmentInheritanceCheck(
        EquipmentStatProfile Source, EquipmentStatProfile Target, int TargetCap,
        int ResultLevel, double LingqiCost, string BlockReason)
    {
        public bool CanCommit => string.IsNullOrEmpty(BlockReason);
        public EquipmentStatProfile Result => Target with { EnhanceLevel = ResultLevel, IsEquipped = true };
    }

    public static class EquipmentInheritanceRules
    {
        public static double GetCost(int sourceLevel)
        {
            double level = Math.Max(0, sourceLevel);
            return Math.Ceiling((Math.Min(level, 3) * 30 + Math.Clamp(level - 3, 0, 3) * 80 + Math.Max(0, level - 6) * 150) * .25);
        }

        public static EquipmentInheritanceCheck Evaluate(EquipmentStatProfile source, EquipmentStatProfile target,
            int realm, int mastery, bool clearedFirstRegion, double lingqi, bool inBattle = false)
        {
            int cap = SmithingRules.GetMaxEnhanceLevel(mastery, target.Rarity);
            int result = Math.Min(Math.Max(0, source.EnhanceLevel), cap);
            string reason = realm < 3 ? "需达到炼气三层。"
                : mastery < 2 ? "需炼器精通 Lv2。"
                : !clearedFirstRegion ? "需首通第一区。"
                : inBattle ? "战斗尚未结束，请结束战斗后继承。"
                : string.IsNullOrEmpty(source.EquipmentId) || !source.IsEquipped ? "请选择当前穿戴的源装备。"
                : string.IsNullOrEmpty(target.EquipmentId) || target.IsEquipped ? "请选择背包中的目标装备。"
                : source.EquipmentId == target.EquipmentId ? "源与目标必须是不同装备。"
                : source.Slot != target.Slot ? "只能向同槽装备继承。"
                : source.EnhanceLevel <= 0 ? "源装备尚未强化。"
                : target.EnhanceLevel >= result ? "目标现有强化不低于可继承等级。"
                : !double.IsFinite(lingqi) || lingqi < GetCost(source.EnhanceLevel) ? "灵气不足。" : "";
            return new(source, target, cap, result, GetCost(source.EnhanceLevel), reason);
        }
    }
}
