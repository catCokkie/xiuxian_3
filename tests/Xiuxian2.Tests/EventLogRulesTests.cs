using System;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class EventLogRulesTests
{
    [Fact]
    public void EventLogPersistenceRules_RoundTripsEntriesUnreadAndTotals()
    {
        EventLogPersistenceRules.EventLogSnapshot expected = new(
            TotalLoggedCount: 12,
            UnreadCount: 3,
            Entries: new[]
            {
                new EventLogEntryData(
                    1712570400,
                    EventLogState.CategoryBattle,
                    "击败 阴潮蛇",
                    string.Empty,
                    EventLogState.AccentNeutral,
                    "阴潮蛇",
                    "elite",
                    "胜利",
                    3,
                    ContextName: "干泉洞窟",
                    RewardSummary: "灵气+12 悟性+3 灵石+8 掉落:灵草x1",
                    LingqiReward: 12,
                    InsightReward: 3,
                    SpiritStoneReward: 8),
                new EventLogEntryData(
                    1712570500,
                    EventLogState.CategoryBreakthrough,
                    "突破成功 → 炼气3层",
                    string.Empty,
                    EventLogState.AccentHighlight,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    0),
            });

        Dictionary<string, object> data = EventLogPersistenceRules.ToPlainDictionary(expected);
        EventLogPersistenceRules.EventLogSnapshot restored = EventLogPersistenceRules.FromPlainDictionary(data);

        Assert.Equal(12, restored.TotalLoggedCount);
        Assert.Equal(3, restored.UnreadCount);
        Assert.Equal(2, restored.Entries.Count);
        Assert.Equal(EventLogState.CategoryBattle, restored.Entries[0].CategoryId);
        Assert.Equal("击败 阴潮蛇", restored.Entries[0].Message);
        Assert.Equal("elite", restored.Entries[0].SubjectType);
        Assert.Equal("胜利", restored.Entries[0].Outcome);
        Assert.Equal("干泉洞窟", restored.Entries[0].ContextName);
        Assert.Equal("灵气+12 悟性+3 灵石+8 掉落:灵草x1", restored.Entries[0].RewardSummary);
        Assert.Equal(12, restored.Entries[0].LingqiReward, 6);
        Assert.Equal(3, restored.Entries[0].InsightReward, 6);
        Assert.Equal(8, restored.Entries[0].SpiritStoneReward);
        Assert.Equal(EventLogState.AccentHighlight, restored.Entries[1].AccentId);
    }

    [Fact]
    public void EventLogPersistenceRules_FromPlainDictionary_TrimsEntriesToMax()
    {
        List<object> entries = new();
        for (int i = 0; i < EventLogPersistenceRules.MaxEntries + 5; i++)
        {
            entries.Add(new Dictionary<string, object>
            {
                ["timestamp_unix"] = 1712570400L + i,
                ["category_id"] = EventLogState.CategorySystem,
                ["message"] = $"事件 {i}",
                ["detail"] = string.Empty,
                ["accent_id"] = EventLogState.AccentNeutral,
                ["subject_name"] = string.Empty,
                ["subject_type"] = string.Empty,
                ["outcome"] = string.Empty,
                ["round_count"] = 0,
            });
        }

        Dictionary<string, object> data = new()
        {
            ["total_logged_count"] = EventLogPersistenceRules.MaxEntries + 5,
            ["unread_count"] = EventLogPersistenceRules.MaxEntries + 5,
            ["entries"] = entries,
        };

        EventLogPersistenceRules.EventLogSnapshot restored = EventLogPersistenceRules.FromPlainDictionary(data);

        Assert.Equal(EventLogPersistenceRules.MaxEntries, restored.Entries.Count);
        Assert.Equal(EventLogPersistenceRules.MaxEntries + 5, restored.TotalLoggedCount);
        Assert.Equal(EventLogPersistenceRules.MaxEntries + 5, restored.UnreadCount);
        Assert.Equal("事件 0", restored.Entries[0].Message);
    }

    [Fact]
    public void EventLogPresentationRules_BuildFeedText_FormatsBattleSummaryAndExpandedBattleEntry()
    {
        string text = EventLogPresentationRules.BuildFeedText(
            new[]
            {
                new EventLogEntryData(
                    1712570400,
                    EventLogState.CategoryBattle,
                    "击败 阴潮蛇",
                    string.Empty,
                    EventLogState.AccentNeutral,
                    "阴潮蛇",
                    "elite",
                    "胜利",
                    3,
                    ContextName: "干泉洞窟",
                    RewardSummary: "灵气+12 悟性+3 灵石+8 掉落:灵草x1",
                    LingqiReward: 12,
                    InsightReward: 3,
                    SpiritStoneReward: 8),
                new EventLogEntryData(
                    1712570500,
                    EventLogState.CategoryBattle,
                    "败于 暗窟蛛王",
                    string.Empty,
                    EventLogState.AccentWarning,
                    "暗窟蛛王",
                    "boss",
                    "失败",
                    5,
                    ContextName: "暗窟深层",
                    RewardSummary: "灵气+0 悟性+0 灵石+0 掉落:none"),
                new EventLogEntryData(
                    1712570600,
                    EventLogState.CategoryBreakthrough,
                    "突破成功 → 炼气3层",
                    string.Empty,
                    EventLogState.AccentHighlight,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    0),
            },
            totalLoggedCount: 12,
            capacity: EventLogPersistenceRules.MaxEntries);

        Assert.Contains("动态", text);
        Assert.Contains("最近战况：胜 1 / 负 1 ｜ 灵气 +12 ｜ 悟性 +3 ｜ 灵石 +8", text);
        Assert.Contains("本页动态：战斗 2｜突破 1", text);
        Assert.Contains("[战斗]", text);
        Assert.Contains("(精英)", text);
        Assert.Contains("(Boss)", text);
        Assert.Contains("战斗胜利", text);
        Assert.Contains("3 回合 · 干泉洞窟", text);
        Assert.Contains("奖励：灵气+12 悟性+3 灵石+8 掉落:灵草x1", text);
        Assert.Contains("突破成功", text);
        Assert.Contains("共 12 条", text);
    }

    [Fact]
    public void EventLogPresentationRules_BuildFeedText_ParsesLegacyBattleDetailForSummary()
    {
        string text = EventLogPresentationRules.BuildFeedText(
            new[]
            {
                new EventLogEntryData(
                    1712570400,
                    EventLogState.CategoryBattle,
                    "击败 阴潮蛇",
                    "干泉洞窟｜灵气+12 悟性+3 灵石+8 掉落:灵草x1",
                    EventLogState.AccentNeutral,
                    "阴潮蛇",
                    "elite",
                    "胜利",
                    3),
            },
            totalLoggedCount: 1,
            capacity: EventLogPersistenceRules.MaxEntries);

        Assert.Contains("最近战况：胜 1 / 负 0 ｜ 灵气 +12 ｜ 悟性 +3 ｜ 灵石 +8", text);
        Assert.Contains("3 回合 · 干泉洞窟", text);
        Assert.Contains("奖励：灵气+12 悟性+3 灵石+8 掉落:灵草x1", text);
    }

    [Fact]
    public void EventLogPresentationRules_BuildFeedText_UsesEmptyPlaceholderWhenNoEntries()
    {
        string text = EventLogPresentationRules.BuildFeedText(Array.Empty<EventLogEntryData>(), 0, EventLogPersistenceRules.MaxEntries);

        Assert.Equal("动态\n暂无动态。开始操作后，所有重要事件都会记录在此处。", text);
    }
}
