using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class EventLogPersistenceRules
    {
        public const int MaxEntries = 200;

        public readonly record struct EventLogSnapshot(
            int TotalLoggedCount,
            int UnreadCount,
            IReadOnlyList<EventLogEntryData> Entries);

        public static Dictionary<string, object> ToPlainDictionary(EventLogSnapshot snapshot)
        {
            List<object> entries = new();
            int entryCount = Math.Min(snapshot.Entries.Count, MaxEntries);
            for (int i = 0; i < entryCount; i++)
            {
                entries.Add(ToPlainEntry(snapshot.Entries[i]));
            }

            int totalLoggedCount = Math.Max(snapshot.TotalLoggedCount, entryCount);
            int unreadCount = Math.Clamp(snapshot.UnreadCount, 0, totalLoggedCount);

            return new Dictionary<string, object>
            {
                ["total_logged_count"] = totalLoggedCount,
                ["unread_count"] = unreadCount,
                ["entries"] = entries,
            };
        }

        public static EventLogSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            List<object> rawEntries = SaveValueConversionRules.GetList(data, "entries");
            List<EventLogEntryData> entries = new();

            for (int i = 0; i < rawEntries.Count && entries.Count < MaxEntries; i++)
            {
                if (rawEntries[i] is IDictionary<string, object> entryData)
                {
                    entries.Add(FromPlainEntry(entryData));
                }
            }

            int totalLoggedCount = Math.Max(SaveValueConversionRules.GetInt(data, "total_logged_count", entries.Count), entries.Count);
            int unreadCount = Math.Clamp(SaveValueConversionRules.GetInt(data, "unread_count"), 0, totalLoggedCount);
            return new EventLogSnapshot(totalLoggedCount, unreadCount, entries);
        }

        private static Dictionary<string, object> ToPlainEntry(EventLogEntryData entry)
        {
            return new Dictionary<string, object>
            {
                ["timestamp_unix"] = entry.TimestampUnix,
                ["category_id"] = entry.CategoryId,
                ["message"] = entry.Message,
                ["detail"] = entry.Detail,
                ["accent_id"] = entry.AccentId,
                ["subject_name"] = entry.SubjectName,
                ["subject_type"] = entry.SubjectType,
                ["outcome"] = entry.Outcome,
                ["round_count"] = entry.RoundCount,
                ["context_name"] = entry.ContextName,
                ["reward_summary"] = entry.RewardSummary,
                ["lingqi_reward"] = entry.LingqiReward,
                ["insight_reward"] = entry.InsightReward,
                ["spirit_stone_reward"] = entry.SpiritStoneReward,
            };
        }

        private static EventLogEntryData FromPlainEntry(IDictionary<string, object> data)
        {
            return new EventLogEntryData(
                TimestampUnix: SaveValueConversionRules.GetLong(data, "timestamp_unix"),
                CategoryId: SaveValueConversionRules.GetString(data, "category_id", EventLogState.CategorySystem),
                Message: SaveValueConversionRules.GetString(data, "message"),
                Detail: SaveValueConversionRules.GetString(data, "detail"),
                AccentId: SaveValueConversionRules.GetString(data, "accent_id", EventLogState.AccentNeutral),
                SubjectName: SaveValueConversionRules.GetString(data, "subject_name"),
                SubjectType: SaveValueConversionRules.GetString(data, "subject_type"),
                Outcome: SaveValueConversionRules.GetString(data, "outcome"),
                RoundCount: SaveValueConversionRules.GetInt(data, "round_count"),
                ContextName: SaveValueConversionRules.GetString(data, "context_name"),
                RewardSummary: SaveValueConversionRules.GetString(data, "reward_summary"),
                LingqiReward: SaveValueConversionRules.GetDouble(data, "lingqi_reward"),
                InsightReward: SaveValueConversionRules.GetDouble(data, "insight_reward"),
                SpiritStoneReward: SaveValueConversionRules.GetInt(data, "spirit_stone_reward"));
        }
    }
}
