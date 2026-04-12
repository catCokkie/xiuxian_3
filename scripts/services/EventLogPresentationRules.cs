using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Xiuxian.Scripts.Services
{
    public static class EventLogPresentationRules
    {
        private static readonly string[] CategoryOrder =
        {
            EventLogState.CategoryBattle,
            EventLogState.CategoryCraft,
            EventLogState.CategoryBreakthrough,
            EventLogState.CategoryMastery,
            EventLogState.CategoryEquipment,
            EventLogState.CategorySystem,
            EventLogState.CategoryCycle,
        };

        private static readonly Regex LingqiRewardRegex = new(@"灵气\+([0-9]+(?:\.[0-9]+)?)", RegexOptions.Compiled);
        private static readonly Regex InsightRewardRegex = new(@"悟性\+([0-9]+(?:\.[0-9]+)?)", RegexOptions.Compiled);
        private static readonly Regex SpiritStoneRewardRegex = new(@"灵石\+([0-9]+)", RegexOptions.Compiled);

        public static string BuildFeedText(IReadOnlyList<EventLogEntryData> entries, int totalLoggedCount, int capacity)
        {
            if (entries.Count == 0)
            {
                return BuildEmptyText();
            }

            StringBuilder sb = new();
            sb.AppendLine("动态");

            string battleSummary = BuildBattleSummary(entries);
            if (!string.IsNullOrEmpty(battleSummary))
            {
                sb.AppendLine($"[color=#8d7763]{battleSummary}[/color]");
            }

            sb.AppendLine(BuildCategorySummary(entries));

            for (int i = 0; i < entries.Count; i++)
            {
                EventLogEntryData entry = entries[i];
                if (entry.CategoryId == EventLogState.CategoryBattle)
                {
                    AppendBattleEntry(sb, entry);
                }
                else
                {
                    AppendGeneralEntry(sb, entry);
                }

                if (i < entries.Count - 1)
                {
                    sb.AppendLine("[color=#b8a080]--------------------[/color]");
                }
            }

            sb.AppendLine();
            sb.Append($"[color=#8d7763]共 {Math.Max(totalLoggedCount, entries.Count)} 条，显示最近 {capacity} 条[/color]");
            return sb.ToString().TrimEnd();
        }

        public static string BuildEmptyText()
        {
            return "动态\n暂无动态。开始操作后，所有重要事件都会记录在此处。";
        }

        public static string BuildUnreadBadgeText(int unreadCount)
        {
            if (unreadCount <= 0)
            {
                return string.Empty;
            }

            return unreadCount > 9 ? "9+" : unreadCount.ToString();
        }

        public static string GetCategoryLabel(string categoryId)
        {
            return categoryId switch
            {
                EventLogState.CategoryBattle => "战斗",
                EventLogState.CategoryCraft => "制作",
                EventLogState.CategoryBreakthrough => "突破",
                EventLogState.CategoryMastery => "精通",
                EventLogState.CategoryEquipment => "装备",
                EventLogState.CategoryCycle => "周天",
                _ => "系统",
            };
        }

        private static string BuildBattleSummary(IReadOnlyList<EventLogEntryData> entries)
        {
            int winCount = 0;
            int lossCount = 0;
            double lingqiReward = 0.0;
            double insightReward = 0.0;
            int spiritStoneReward = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                EventLogEntryData entry = entries[i];
                if (entry.CategoryId != EventLogState.CategoryBattle)
                {
                    continue;
                }

                if (entry.Outcome == "胜利")
                {
                    winCount++;
                }
                else if (!string.IsNullOrEmpty(entry.Outcome))
                {
                    lossCount++;
                }

                (double entryLingqi, double entryInsight, int entrySpiritStone) = ResolveBattleRewards(entry);
                lingqiReward += entryLingqi;
                insightReward += entryInsight;
                spiritStoneReward += entrySpiritStone;
            }

            if (winCount + lossCount <= 0)
            {
                return string.Empty;
            }

            return $"最近战况：胜 {winCount} / 负 {lossCount} ｜ 灵气 +{lingqiReward:0} ｜ 悟性 +{insightReward:0} ｜ 灵石 +{spiritStoneReward}";
        }

        private static string BuildCategorySummary(IReadOnlyList<EventLogEntryData> entries)
        {
            Dictionary<string, int> counts = new(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                string categoryId = entries[i].CategoryId;
                counts[categoryId] = counts.TryGetValue(categoryId, out int count) ? count + 1 : 1;
            }

            StringBuilder sb = new();
            sb.Append("[color=#8d7763]本页动态：");
            bool first = true;
            for (int i = 0; i < CategoryOrder.Length; i++)
            {
                string categoryId = CategoryOrder[i];
                if (!counts.TryGetValue(categoryId, out int count) || count <= 0)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append("｜");
                }

                first = false;
                sb.Append($"{GetCategoryLabel(categoryId)} {count}");
            }

            if (first)
            {
                sb.Append("暂无分类");
            }

            sb.Append("[/color]");
            return sb.ToString();
        }

        private static void AppendBattleEntry(StringBuilder sb, EventLogEntryData entry)
        {
            string typeBadge = entry.SubjectType switch
            {
                "elite" => " [color=#c8a050](精英)[/color]",
                "boss" => " [color=#c85050](Boss)[/color]",
                _ => string.Empty,
            };
            sb.Append($"[color={GetAccentColor(entry.AccentId)}][{FormatTime(entry.TimestampUnix)}][/color] ");
            sb.Append($"[color={GetCategoryColor(entry.CategoryId)}][{GetCategoryLabel(entry.CategoryId)}][/color] ");
            sb.AppendLine($"{ApplyAccent(entry.Message, entry.AccentId)}{typeBadge}");

            StringBuilder statusLine = new();
            if (!string.IsNullOrEmpty(entry.Outcome))
            {
                string resultColor = entry.Outcome == "胜利" ? "#6aaf6a" : "#c85050";
                statusLine.Append($"[color={resultColor}]战斗{entry.Outcome}[/color]");
            }

            if (entry.RoundCount > 0)
            {
                if (statusLine.Length > 0)
                {
                    statusLine.Append(" · ");
                }

                statusLine.Append($"{entry.RoundCount} 回合");
            }

            string contextName = ResolveContextName(entry);
            if (!string.IsNullOrEmpty(contextName))
            {
                if (statusLine.Length > 0)
                {
                    statusLine.Append(" · ");
                }

                statusLine.Append(contextName);
            }

            if (statusLine.Length > 0)
            {
                sb.AppendLine($"  {statusLine}");
            }

            string rewardSummary = ResolveRewardSummary(entry);
            if (!string.IsNullOrEmpty(rewardSummary))
            {
                sb.AppendLine($"  奖励：{rewardSummary}");
            }
        }

        private static void AppendGeneralEntry(StringBuilder sb, EventLogEntryData entry)
        {
            sb.Append($"[color={GetAccentColor(entry.AccentId)}][{FormatTime(entry.TimestampUnix)}][/color] ");
            sb.Append($"[color={GetCategoryColor(entry.CategoryId)}][{GetCategoryLabel(entry.CategoryId)}][/color] ");
            sb.AppendLine(ApplyAccent(entry.Message, entry.AccentId));

            if (!string.IsNullOrEmpty(entry.Detail))
            {
                sb.AppendLine($"  {entry.Detail}");
            }
        }

        private static string ApplyAccent(string message, string accentId)
        {
            string color = accentId switch
            {
                EventLogState.AccentHighlight => "#c8a050",
                EventLogState.AccentWarning => "#b85450",
                _ => "#4b3622",
            };

            return $"[color={color}]{message}[/color]";
        }

        private static string FormatTime(long timestampUnix)
        {
            if (timestampUnix <= 0)
            {
                return "--:--";
            }

            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(timestampUnix).ToLocalTime().ToString("HH:mm");
            }
            catch (ArgumentOutOfRangeException)
            {
                return "--:--";
            }
        }

        private static (double LingqiReward, double InsightReward, int SpiritStoneReward) ResolveBattleRewards(EventLogEntryData entry)
        {
            if (entry.LingqiReward > 0.0 || entry.InsightReward > 0.0 || entry.SpiritStoneReward > 0)
            {
                return (entry.LingqiReward, entry.InsightReward, entry.SpiritStoneReward);
            }

            string rewardSummary = ResolveRewardSummary(entry);
            if (string.IsNullOrEmpty(rewardSummary))
            {
                return (0.0, 0.0, 0);
            }

            return (
                ParseDoubleReward(LingqiRewardRegex, rewardSummary),
                ParseDoubleReward(InsightRewardRegex, rewardSummary),
                ParseIntReward(SpiritStoneRewardRegex, rewardSummary));
        }

        private static string ResolveContextName(EventLogEntryData entry)
        {
            if (!string.IsNullOrEmpty(entry.ContextName))
            {
                return entry.ContextName;
            }

            if (entry.CategoryId != EventLogState.CategoryBattle || string.IsNullOrEmpty(entry.Detail))
            {
                return string.Empty;
            }

            int separatorIndex = entry.Detail.IndexOf('｜');
            if (separatorIndex <= 0)
            {
                return string.Empty;
            }

            return entry.Detail.Substring(0, separatorIndex).Trim();
        }

        private static string ResolveRewardSummary(EventLogEntryData entry)
        {
            if (!string.IsNullOrEmpty(entry.RewardSummary))
            {
                return entry.RewardSummary;
            }

            if (entry.CategoryId != EventLogState.CategoryBattle || string.IsNullOrEmpty(entry.Detail))
            {
                return string.Empty;
            }

            int separatorIndex = entry.Detail.IndexOf('｜');
            if (separatorIndex < 0 || separatorIndex >= entry.Detail.Length - 1)
            {
                return entry.Detail.Contains("灵气+", StringComparison.Ordinal) || entry.Detail.Contains("灵石+", StringComparison.Ordinal)
                    ? entry.Detail.Trim()
                    : string.Empty;
            }

            return entry.Detail.Substring(separatorIndex + 1).Trim();
        }

        private static double ParseDoubleReward(Regex pattern, string text)
        {
            Match match = pattern.Match(text);
            return match.Success && double.TryParse(match.Groups[1].Value, out double value) ? value : 0.0;
        }

        private static int ParseIntReward(Regex pattern, string text)
        {
            Match match = pattern.Match(text);
            return match.Success && int.TryParse(match.Groups[1].Value, out int value) ? value : 0;
        }

        private static string GetCategoryColor(string categoryId)
        {
            return categoryId switch
            {
                EventLogState.CategoryBattle => "#8f5d3b",
                EventLogState.CategoryCraft => "#6d8a55",
                EventLogState.CategoryBreakthrough => "#c8a050",
                EventLogState.CategoryMastery => "#c8a050",
                EventLogState.CategoryEquipment => "#6689b3",
                EventLogState.CategoryCycle => "#5f8a74",
                _ => "#8d7763",
            };
        }

        private static string GetAccentColor(string accentId)
        {
            return accentId switch
            {
                EventLogState.AccentHighlight => "#c8a050",
                EventLogState.AccentWarning => "#b85450",
                _ => "#8d7763",
            };
        }
    }
}
