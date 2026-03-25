using System.Collections.Generic;
using System.Text;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    public static class ExploreProgressPresentationRules
    {
        public static string GetActionModeOptionText(int selected)
        {
            return selected switch
            {
                1 => UiText.ActionModeCultivation,
                2 => UiText.ActionModeAlchemy,
                3 => UiText.ActionModeSmithing,
                _ => UiText.ActionModeDungeon,
            };
        }

        public static string GetActionModeDisplayName(string actionId)
        {
            return actionId switch
            {
                PlayerActionState.ActionCultivation => "修炼",
                PlayerActionState.ActionAlchemy => "炼丹",
                PlayerActionState.ActionSmithing => "炼器",
                _ => "副本",
            };
        }

        public static string GetPausedModeLabel(string actionId)
        {
            return $"主行为：{GetActionModeDisplayName(actionId)}";
        }

        public static string BuildAlchemyProgressText(string recipeName, float currentProgress, float requiredProgress)
        {
            if (string.IsNullOrEmpty(recipeName))
            {
                return "炼丹待命（请先选择丹方）";
            }

            float percent = requiredProgress > 0.0f ? currentProgress / requiredProgress * 100.0f : 0.0f;
            return $"炼制：{recipeName} {percent:0}%";
        }

        public static string BuildSmithingProgressText(string equipmentName, int enhanceLevel, float currentProgress, float requiredProgress)
        {
            if (string.IsNullOrEmpty(equipmentName))
            {
                return "强化待命（请先选择装备）";
            }

            float percent = requiredProgress > 0.0f ? currentProgress / requiredProgress * 100.0f : 0.0f;
            return $"强化：{equipmentName} +{enhanceLevel}->{enhanceLevel + 1} {percent:0}%";
        }

        public static string GetPausedModeRoundLabel(string actionId, string alchemyText, string smithingText)
        {
            return actionId switch
            {
                PlayerActionState.ActionAlchemy => alchemyText,
                PlayerActionState.ActionSmithing => smithingText,
                _ => $"副本暂停（{GetActionModeDisplayName(actionId)}模式）",
            };
        }

        public static string BuildRecentBattleLogText(IReadOnlyList<(string TimeLabel, string ZoneName, string MonsterName, string BattleResult, string RewardSummary)> entries)
        {
            if (entries.Count == 0)
            {
                return UiText.BattleLogEmpty;
            }

            var sb = new StringBuilder();
            sb.Append("最近战斗日志");
            sb.Append($"\n共 {entries.Count} 条，最新在上\n");

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                sb.Append($"\n[{entry.TimeLabel}] {entry.ZoneName} | {entry.MonsterName} | {entry.BattleResult}");
                sb.Append($"\n{entry.RewardSummary}");
            }

            return sb.ToString();
        }
    }
}
