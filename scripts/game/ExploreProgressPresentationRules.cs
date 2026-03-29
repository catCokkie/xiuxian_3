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
                1 => "修炼｜灵气悟性",
                2 => "炼丹｜战斗丹药",
                3 => "炼器｜装备强化",
                4 => "灵田｜草花果",
                5 => "矿脉｜矿玉银",
                6 => "灵渔｜鱼珠涎",
                7 => "符箓｜战斗消耗",
                8 => "烹饪｜战前增益",
                9 => "阵法｜常驻阵盘",
                10 => "悟道｜悟性增益",
                11 => "体修｜永久属性",
                _ => "副本｜材料装备",
            };
        }

        public static string GetActionModeDisplayName(string actionId)
        {
            return actionId switch
            {
                PlayerActionState.ActionCultivation => "修炼",
                PlayerActionState.ActionAlchemy => "炼丹",
                PlayerActionState.ActionSmithing => "炼器",
                PlayerActionState.ActionGarden => "灵田",
                PlayerActionState.ActionMining => "矿脉",
                PlayerActionState.ActionFishing => "灵渔",
                PlayerActionState.ActionTalisman => "符箓",
                PlayerActionState.ActionCooking => "烹饪",
                PlayerActionState.ActionFormation => "阵法",
                PlayerActionState.ActionEnlightenment => "悟道",
                PlayerActionState.ActionBodyCultivation => "体修",
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

        public static string BuildGardenProgressText(string cropName, float currentProgress, float requiredProgress)
        {
            if (string.IsNullOrEmpty(cropName))
            {
                return "灵田待命（请先选择作物）";
            }

            float percent = requiredProgress > 0.0f ? currentProgress / requiredProgress * 100.0f : 0.0f;
            return $"种植：{cropName} {percent:0}%";
        }

        public static string BuildMiningProgressText(string nodeName, float currentProgress, float requiredProgress, int durability)
        {
            if (string.IsNullOrEmpty(nodeName))
            {
                return "矿脉待命（请先选择矿点）";
            }

            float percent = requiredProgress > 0.0f ? currentProgress / requiredProgress * 100.0f : 0.0f;
            return $"开采：{nodeName} {percent:0}% | 耐久 {durability}";
        }

        public static string BuildFishingProgressText(string pondName, float currentProgress, float requiredProgress)
        {
            if (string.IsNullOrEmpty(pondName))
            {
                return "灵渔待命（请先选择鱼塘）";
            }

            float percent = requiredProgress > 0.0f ? currentProgress / requiredProgress * 100.0f : 0.0f;
            return $"垂钓：{pondName} {percent:0}%";
        }

        public static string GetPausedModeRoundLabel(string actionId, string alchemyText, string smithingText, string gardenText, string miningText, string fishingText)
        {
            return actionId switch
            {
                PlayerActionState.ActionAlchemy => alchemyText,
                PlayerActionState.ActionSmithing => smithingText,
                PlayerActionState.ActionGarden => gardenText,
                PlayerActionState.ActionMining => miningText,
                PlayerActionState.ActionFishing => fishingText,
                _ => $"副本暂停（{GetActionModeDisplayName(actionId)}模式）",
            };
        }

        public static string BuildRecentBattleLogText(IReadOnlyList<(string TimeLabel, string ZoneName, string MonsterName, string MonsterType, int RoundCount, string BattleResult, string RewardSummary)> entries)
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
                string typeBadge = entry.MonsterType switch
                {
                    "elite" => " [color=#c8a050](精英)[/color]",
                    "boss" => " [color=#c85050](Boss)[/color]",
                    _ => ""
                };
                sb.Append($"\n[{entry.TimeLabel}] 遭遇 {entry.MonsterName}{typeBadge}");

                bool isWin = entry.BattleResult == "胜利";
                string resultColor = isWin ? "#6aaf6a" : "#c85050";
                string roundText = entry.RoundCount > 0 ? $" — {entry.RoundCount} 回合" : "";
                sb.Append($"\n[color={resultColor}]战斗{entry.BattleResult}{roundText}[/color]");

                if (isWin && entry.RewardSummary != "无掉落")
                {
                    sb.Append($"\n掉落：{entry.RewardSummary}");
                }

                sb.Append("\n[color=#b8a080]────────────────────[/color]");
            }

            return sb.ToString();
        }
    }
}
