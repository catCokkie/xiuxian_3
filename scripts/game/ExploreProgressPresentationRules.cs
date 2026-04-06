using System.Collections.Generic;
using System.Text;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    public static class ExploreProgressPresentationRules
    {
        public static string GetActionModeOptionText(int selected)
        {
            IReadOnlyList<string> allModes = ProgressiveUnlockRules.GetAllActionModesInUnlockOrder();
            if (allModes.Count == 0)
            {
                return GetActionModeOptionText(PlayerActionState.ModeDungeon);
            }

            int index = System.Math.Clamp(selected, 0, allModes.Count - 1);
            return GetActionModeOptionText(allModes[index]);
        }

        public static string GetActionModeOptionText(string modeId)
        {
            return modeId switch
            {
                PlayerActionState.ModeCultivation => "修炼｜灵气悟性",
                PlayerActionState.ModeAlchemy => "炼丹｜战斗丹药",
                PlayerActionState.ModeGarden => "灵田｜草花果",
                PlayerActionState.ModeMining => "矿脉｜矿玉银",
                PlayerActionState.ModeSmithing => "炼器｜装备强化",
                PlayerActionState.ModeFishing => "灵渔｜鱼珠涎",
                PlayerActionState.ModeCooking => "烹饪｜战前增益",
                PlayerActionState.ModeTalisman => "符箓｜战斗消耗",
                PlayerActionState.ModeFormation => "阵法｜常驻阵盘",
                PlayerActionState.ModeBodyCultivation => "体修｜永久属性",
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

            if (requiredProgress > 0.0f && currentProgress >= requiredProgress)
            {
                return $"种植：{cropName} 已成熟，可收获";
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

        public static string BuildFormationStatusText(string formationName, string summary, bool isActive)
        {
            if (string.IsNullOrEmpty(formationName))
            {
                return "阵法：未激活";
            }

            string suffix = isActive ? "（当前生效）" : string.Empty;
            return $"阵法：{formationName}{suffix}｜{summary}";
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

            StringBuilder sb = new();
            sb.Append("最近战斗日志");
            sb.Append($"\n共 {entries.Count} 条，最新在上\n");

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                string typeBadge = entry.MonsterType switch
                {
                    "elite" => " [color=#c8a050](精英)[/color]",
                    "boss" => " [color=#c85050](Boss)[/color]",
                    _ => string.Empty
                };
                sb.Append($"\n[{entry.TimeLabel}] 遭遇 {entry.MonsterName}{typeBadge}");

                bool isWin = entry.BattleResult == "胜利";
                string resultColor = isWin ? "#6aaf6a" : "#c85050";
                string roundText = entry.RoundCount > 0 ? $" - {entry.RoundCount} 回合" : string.Empty;
                sb.Append($"\n[color={resultColor}]战斗{entry.BattleResult}{roundText}[/color]");

                if (isWin && entry.RewardSummary != "无掉落")
                {
                    sb.Append($"\n掉落：{entry.RewardSummary}");
                }

                sb.Append("\n[color=#b8a080]--------------------[/color]");
            }

            return sb.ToString();
        }
    }
}
