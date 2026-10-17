using System.Text.Json;
using Xiuxian.Scripts.Services;

namespace Xiuxian2.Tests;

public class OperationStatisticsTests
{
    private static OperationStatisticsContext Context(string scenario = "typing") => new(scenario, "dungeon", "lv_qi_001", "exploring", 1, false, "closed", false, "global");
    private static OperationInputObservation Input(double seconds = 1) => new(seconds, new(10, 2, 3, 4, 1), new(5, 1, 1, 2, 1), 600, 21.4, 11.6, Math.Min(seconds, 1));

    [Fact]
    public void SeparatesOriginalEffectiveLimitedAndMovement()
    {
        var rows = new List<OperationStatisticsRow>(); var session = new OperationStatisticsSession(rows.Add, Context(), new());
        session.Observe(Input(60)); session.Stop();
        var sample = Assert.Single(rows.Where(r => r.Kind == "sample"));
        Assert.Equal(20, sample.Counts.RawTotal); Assert.Equal(10, sample.Counts.AcceptedTotal);
        Assert.Equal(10, sample.Counts.LimitedTotal); Assert.Equal(600, sample.Counts.MouseDistance);
        Assert.Equal(4, sample.Counts.JoyButtons); Assert.Equal(2, sample.Counts.AcceptedJoyButtons);
        Assert.Equal(sample.Counts, rows.Last().Counts);
    }
    [Fact]
    public void ContextSwitchAndStopPreservePartialBucketsExactlyOnce()
    {
        var rows = new List<OperationStatisticsRow>(); var session = new OperationStatisticsSession(rows.Add, Context(), new());
        session.Observe(Input(13)); session.SetContext(Context("reading")); session.Observe(Input(7));
        session.Stop(); session.Stop(); session.Observe(Input(100)); session.Flush();
        var samples = rows.Where(r => r.Kind == "sample").ToArray();
        Assert.Equal(2, samples.Length); Assert.Equal("typing", samples[0].Context.Scenario); Assert.Equal("reading", samples[1].Context.Scenario);
        Assert.Equal(13, samples[0].EndSeconds); Assert.Equal(13, samples[1].StartSeconds); Assert.Equal(20, session.ElapsedSeconds);
        Assert.Equal(40, samples.Sum(r => r.Counts.RawTotal)); Assert.Single(rows.Where(r => r.Kind == "session_end"));
    }
    [Fact]
    public void IdleAndMovementOnlyHaveDurationWithoutDiscreteInputs()
    {
        var rows = new List<OperationStatisticsRow>(); var session = new OperationStatisticsSession(rows.Add, Context("reading"), new());
        session.Observe(new(60, default, default, 0, 0, 0, 0));
        session.Observe(new(60, default, default, 1200, 2, 2, 5)); session.Stop();
        Assert.Equal(120, rows.Where(r => r.Kind == "sample").Sum(r => r.EndSeconds - r.StartSeconds));
        Assert.Equal(0, session.Totals.RawTotal); Assert.Equal(2, session.Totals.AcceptedAp); Assert.Equal(5, session.Totals.ActiveSeconds);
    }
    [Fact]
    public void ExportFlushDoesNotDuplicateTotalsAndEventsAreNotInput()
    {
        var rows = new List<OperationStatisticsRow>(); var session = new OperationStatisticsSession(rows.Add, Context(), new());
        session.Observe(Input()); session.Flush(); session.Flush(); session.RecordEvent("book_open");
        session.Observe(Input()); session.Stop();
        Assert.Equal(40, rows.Where(r => r.Kind == "sample").Sum(r => r.Counts.RawTotal));
        Assert.Equal(0, Assert.Single(rows.Where(r => r.Kind == "game_event")).Counts.RawTotal);
    }
    [Fact]
    public void LongCountsRemainExactBeyondInt32AndRoundtrip()
    {
        var rows = new List<OperationStatisticsRow>(); var session = new OperationStatisticsSession(rows.Add, Context(), new());
        for (int i = 0; i < 3; i++) session.Observe(new(60, new(int.MaxValue, 0, 0, 0, 0), default, 0, 0, 0, 0));
        session.Stop(); var restored = JsonSerializer.Deserialize<OperationStatisticsRow>(JsonSerializer.Serialize(rows.Last()))!;
        Assert.Equal(3L * int.MaxValue, restored.Counts.RawTotal);
    }
    [Fact]
    public void ChangingIntervalsAndContextsConservesAllTypesAndTime()
    {
        var rows = new List<OperationStatisticsRow>(); var session = new OperationStatisticsSession(rows.Add, Context(), new());
        var expected = new OperationStatisticsCounts(); double duration = 0;
        for (int i = 1; i <= 500; i++)
        {
            var observation = Input(i % 11 + .25); expected = expected.Add(observation); duration += observation.Seconds;
            session.Observe(observation); if (i % 17 == 0) session.SetContext(Context(i % 2 == 0 ? "reading" : "typing"));
            if (i % 29 == 0) session.Flush();
        }
        session.Stop(); var samples = rows.Where(r => r.Kind == "sample").ToArray();
        Assert.Equal(expected, session.Totals); Assert.Equal(expected.RawTotal, samples.Sum(r => r.Counts.RawTotal));
        Assert.Equal(expected.AcceptedJoyAxes, samples.Sum(r => r.Counts.AcceptedJoyAxes));
        Assert.Equal(duration, samples.Sum(r => r.EndSeconds - r.StartSeconds), 7);
    }
    [Theory]
    [InlineData(-1)] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    public void RejectsInvalidDuration(double seconds)
    {
        var session = new OperationStatisticsSession(_ => { }, Context(), new());
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Observe(Input(seconds)));
    }
    [Fact]
    public void JsonlAndCsvAreLocalReadableAndEscapeContext()
    {
        string dir = Path.Combine(Path.GetTempPath(), "xiuxian-operation-test-" + Guid.NewGuid().ToString("N"));
        string logPath = "", csv = "";
        try
        {
            using (var file = new OperationStatisticsFile(dir))
            {
                logPath = file.Path;
                var session = new OperationStatisticsSession(file.Write, Context("quoted,\"value"), new() { ["started_utc"] = "2026-10-17T00:00:00Z" });
                session.Observe(Input(60)); session.Stop();
                csv = Path.ChangeExtension(logPath, ".csv");
                csv = OperationStatisticsFile.ExportCsv(logPath);
            }
            Assert.Equal(3, File.ReadLines(logPath).Count()); Assert.Equal(4, File.ReadLines(csv).Count());
            Assert.Contains("\"quoted,\"\"value\"", File.ReadAllText(csv));
            Assert.Contains("accepted_total", File.ReadLines(csv).First());
            Assert.DoesNotContain("key_code", File.ReadAllText(logPath));
        }
        finally { if (File.Exists(logPath)) File.Delete(logPath); if (File.Exists(csv)) File.Delete(csv); if (Directory.Exists(dir)) Directory.Delete(dir); }
    }
}
