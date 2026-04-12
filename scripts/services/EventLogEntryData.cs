namespace Xiuxian.Scripts.Services
{
    public readonly record struct EventLogEntryData(
        long TimestampUnix,
        string CategoryId,
        string Message,
        string Detail,
        string AccentId,
        string SubjectName,
        string SubjectType,
        string Outcome,
        int RoundCount,
        string ContextName = "",
        string RewardSummary = "",
        double LingqiReward = 0.0,
        double InsightReward = 0.0,
        int SpiritStoneReward = 0);
}
