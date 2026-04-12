using Godot;
using System;

namespace Xiuxian.Scripts.Services
{
    public partial class EventLogState : Node, IDictionaryPersistable
    {
        public const string CategoryBattle = "battle";
        public const string CategoryCraft = "craft";
        public const string CategoryBreakthrough = "breakthrough";
        public const string CategoryMastery = "mastery";
        public const string CategoryEquipment = "equipment";
        public const string CategorySystem = "system";
        public const string CategoryCycle = "cycle";

        public const string AccentNeutral = "neutral";
        public const string AccentHighlight = "highlight";
        public const string AccentWarning = "warning";

        [Signal]
        public delegate void EntriesChangedEventHandler();

        private readonly System.Collections.Generic.List<EventLogEntryData> _entries = new();

        public int TotalLoggedCount { get; private set; }
        public int UnreadCount { get; private set; }

        public EventLogEntryData[] GetEntries()
        {
            return _entries.ToArray();
        }

        public string BuildFeedText()
        {
            return EventLogPresentationRules.BuildFeedText(_entries, TotalLoggedCount, EventLogPersistenceRules.MaxEntries);
        }

        public void AddEvent(string categoryId, string message, string detail = "", string accentId = AccentNeutral)
        {
            AddEntry(new EventLogEntryData(
                TimestampUnix: (long)Time.GetUnixTimeFromSystem(),
                CategoryId: NormalizeCategory(categoryId),
                Message: message ?? string.Empty,
                Detail: detail ?? string.Empty,
                AccentId: NormalizeAccent(accentId),
                SubjectName: string.Empty,
                SubjectType: string.Empty,
                Outcome: string.Empty,
                RoundCount: 0));
        }

        public void AddBattleEvent(
            string zoneName,
            string monsterName,
            string monsterType,
            int roundCount,
            string battleResult,
            string rewardSummary,
            double lingqiReward = 0.0,
            double insightReward = 0.0,
            int spiritStoneReward = 0)
        {
            string normalizedMonsterName = string.IsNullOrWhiteSpace(monsterName) ? UiText.DefaultMonsterName : monsterName.Trim();
            string normalizedMonsterType = monsterType switch
            {
                "elite" => "elite",
                "boss" => "boss",
                _ => "normal",
            };
            string normalizedOutcome = string.IsNullOrWhiteSpace(battleResult) ? "胜利" : battleResult.Trim();
            string message = normalizedOutcome == "胜利"
                ? $"击败 {normalizedMonsterName}"
                : $"败于 {normalizedMonsterName}";

            AddEntry(new EventLogEntryData(
                TimestampUnix: (long)Time.GetUnixTimeFromSystem(),
                CategoryId: CategoryBattle,
                Message: message,
                Detail: string.Empty,
                AccentId: normalizedOutcome == "胜利" ? AccentNeutral : AccentWarning,
                SubjectName: normalizedMonsterName,
                SubjectType: normalizedMonsterType,
                Outcome: normalizedOutcome,
                RoundCount: Math.Max(0, roundCount),
                ContextName: zoneName?.Trim() ?? string.Empty,
                RewardSummary: rewardSummary?.Trim() ?? string.Empty,
                LingqiReward: Math.Max(0.0, lingqiReward),
                InsightReward: Math.Max(0.0, insightReward),
                SpiritStoneReward: Math.Max(0, spiritStoneReward)));
        }

        public void MarkAllRead()
        {
            if (UnreadCount <= 0)
            {
                return;
            }

            UnreadCount = 0;
            EmitSignal(SignalName.EntriesChanged);
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            EventLogPersistenceRules.EventLogSnapshot snapshot = new(TotalLoggedCount, UnreadCount, _entries);
            return SaveValueConversionRules.ToVariantDictionary(EventLogPersistenceRules.ToPlainDictionary(snapshot));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            EventLogPersistenceRules.EventLogSnapshot snapshot = EventLogPersistenceRules.FromPlainDictionary(
                SaveValueConversionRules.ToPlainDictionary(data));

            _entries.Clear();
            for (int i = 0; i < snapshot.Entries.Count; i++)
            {
                _entries.Add(NormalizeEntry(snapshot.Entries[i]));
            }

            TotalLoggedCount = Math.Max(snapshot.TotalLoggedCount, _entries.Count);
            UnreadCount = Math.Clamp(snapshot.UnreadCount, 0, TotalLoggedCount);
            EmitSignal(SignalName.EntriesChanged);
        }

        private void AddEntry(EventLogEntryData entry)
        {
            _entries.Insert(0, NormalizeEntry(entry));
            if (_entries.Count > EventLogPersistenceRules.MaxEntries)
            {
                _entries.RemoveAt(_entries.Count - 1);
            }

            TotalLoggedCount++;
            UnreadCount = Math.Clamp(UnreadCount + 1, 0, TotalLoggedCount);
            EmitSignal(SignalName.EntriesChanged);
        }

        private static EventLogEntryData NormalizeEntry(EventLogEntryData entry)
        {
            long timestampUnix = entry.TimestampUnix > 0 ? entry.TimestampUnix : (long)Time.GetUnixTimeFromSystem();
            return new EventLogEntryData(
                TimestampUnix: timestampUnix,
                CategoryId: NormalizeCategory(entry.CategoryId),
                Message: entry.Message ?? string.Empty,
                Detail: entry.Detail ?? string.Empty,
                AccentId: NormalizeAccent(entry.AccentId),
                SubjectName: entry.SubjectName ?? string.Empty,
                SubjectType: entry.SubjectType ?? string.Empty,
                Outcome: entry.Outcome ?? string.Empty,
                RoundCount: Math.Max(0, entry.RoundCount),
                ContextName: entry.ContextName ?? string.Empty,
                RewardSummary: entry.RewardSummary ?? string.Empty,
                LingqiReward: Math.Max(0.0, entry.LingqiReward),
                InsightReward: Math.Max(0.0, entry.InsightReward),
                SpiritStoneReward: Math.Max(0, entry.SpiritStoneReward));
        }

        private static string NormalizeCategory(string categoryId)
        {
            return categoryId switch
            {
                CategoryBattle => CategoryBattle,
                CategoryCraft => CategoryCraft,
                CategoryBreakthrough => CategoryBreakthrough,
                CategoryMastery => CategoryMastery,
                CategoryEquipment => CategoryEquipment,
                CategoryCycle => CategoryCycle,
                _ => CategorySystem,
            };
        }

        private static string NormalizeAccent(string accentId)
        {
            return accentId switch
            {
                AccentHighlight => AccentHighlight,
                AccentWarning => AccentWarning,
                _ => AccentNeutral,
            };
        }
    }
}
