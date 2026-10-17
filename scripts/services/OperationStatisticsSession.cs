using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Xiuxian.Scripts.Services;

// Aggregated diagnostics only: no key codes, cursor positions or application information.
public readonly record struct OperationInputObservation(double Seconds,
    InputActivityRules.DiscreteInputBatch Raw, InputActivityRules.DiscreteInputBatch Accepted,
    double MouseDistance, double RawAp, double AcceptedAp, double ActiveSeconds);

public sealed record OperationStatisticsContext(string Scenario, string Action, string Region, string Battle,
    int Realm, bool BookOpen, string Page, bool WorkMode, string Capture);

public sealed record OperationStatisticsCounts(long Keys = 0, long Clicks = 0, long Scroll = 0,
    long JoyButtons = 0, long JoyAxes = 0, long AcceptedKeys = 0, long AcceptedClicks = 0,
    long AcceptedScroll = 0, long AcceptedJoyButtons = 0, long AcceptedJoyAxes = 0,
    double MouseDistance = 0, double RawAp = 0, double AcceptedAp = 0, double ActiveSeconds = 0)
{
    public long RawTotal => Keys + Clicks + Scroll + JoyButtons + JoyAxes;
    public long AcceptedTotal => AcceptedKeys + AcceptedClicks + AcceptedScroll + AcceptedJoyButtons + AcceptedJoyAxes;
    public long LimitedTotal => RawTotal - AcceptedTotal;
    public OperationStatisticsCounts Add(OperationInputObservation input) => new(
        Keys + input.Raw.KeyDownCount, Clicks + input.Raw.MouseClickCount, Scroll + input.Raw.MouseScrollSteps,
        JoyButtons + input.Raw.JoypadButtonCount, JoyAxes + input.Raw.JoypadAxisInputCount,
        AcceptedKeys + input.Accepted.KeyDownCount, AcceptedClicks + input.Accepted.MouseClickCount,
        AcceptedScroll + input.Accepted.MouseScrollSteps, AcceptedJoyButtons + input.Accepted.JoypadButtonCount,
        AcceptedJoyAxes + input.Accepted.JoypadAxisInputCount, MouseDistance + input.MouseDistance,
        RawAp + input.RawAp, AcceptedAp + input.AcceptedAp, ActiveSeconds + input.ActiveSeconds);
}

public sealed record OperationStatisticsRow(int Schema, string Session, string Kind, double StartSeconds,
    double EndSeconds, OperationStatisticsContext Context, OperationStatisticsCounts Counts,
    string Event = "", Dictionary<string, string>? Metadata = null);

public sealed class OperationStatisticsSession
{
    private readonly Action<OperationStatisticsRow> _write;
    private OperationStatisticsCounts _bucket = new();
    private double _bucketStart;
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public OperationStatisticsContext Context { get; private set; }
    public OperationStatisticsCounts Totals { get; private set; } = new();
    public double ElapsedSeconds { get; private set; }
    public bool Stopped { get; private set; }

    public OperationStatisticsSession(Action<OperationStatisticsRow> write, OperationStatisticsContext context,
        Dictionary<string, string> metadata)
    {
        _write = write; Context = context;
        Emit("session_start", new(), metadata: metadata);
    }

    public void SetContext(OperationStatisticsContext context)
    {
        if (Stopped || context == Context) return;
        Flush(); Context = context; Emit("context", new());
    }

    public void Observe(OperationInputObservation observation)
    {
        if (Stopped) return;
        if (!double.IsFinite(observation.Seconds) || observation.Seconds < 0) throw new ArgumentOutOfRangeException(nameof(observation));
        ElapsedSeconds += observation.Seconds;
        _bucket = _bucket.Add(observation); Totals = Totals.Add(observation);
        if (ElapsedSeconds - _bucketStart >= 60) Flush();
    }

    // These are named game events, never generic text input or physical click counts.
    public void RecordEvent(string eventId)
    {
        if (!Stopped) Emit("game_event", new(), eventId);
    }

    public void Flush()
    {
        if (Stopped || (ElapsedSeconds == _bucketStart && _bucket.RawTotal == 0 && _bucket.MouseDistance == 0)) return;
        _write(new(1, Id, "sample", _bucketStart, ElapsedSeconds, Context, _bucket));
        _bucketStart = ElapsedSeconds; _bucket = new();
    }

    public void Stop()
    {
        if (Stopped) return;
        Flush(); Emit("session_end", Totals); Stopped = true;
    }

    private void Emit(string kind, OperationStatisticsCounts counts, string eventId = "", Dictionary<string, string>? metadata = null)
        => _write(new(1, Id, kind, ElapsedSeconds, ElapsedSeconds, Context, counts, eventId, metadata));
}

public sealed class OperationStatisticsFile : IDisposable
{
    public const int MaxRows = 10000;
    private readonly StreamWriter _writer;
    private int _rows;
    public string Path { get; }
    public OperationStatisticsFile(string directory)
    {
        Directory.CreateDirectory(directory);
        Path = System.IO.Path.Combine(directory, $"operations_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.jsonl");
        _writer = new(new FileStream(Path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
    }
    public void Write(OperationStatisticsRow row)
    {
        if (_rows >= MaxRows) throw new IOException("本次统计达到10000条上限，请开始新的统计。");
        _writer.WriteLine(JsonSerializer.Serialize(row)); _rows++;
    }
    public void Dispose() => _writer.Dispose();

    // CSV contains all row kinds; only sample rows are additive input/time measurements.
    public static string ExportCsv(string path)
    {
        string output = System.IO.Path.ChangeExtension(path, ".csv");
        using var writer = new StreamWriter(output, false, new UTF8Encoding(true));
        writer.WriteLine("schema,session,kind,start_seconds,end_seconds,duration_seconds,scenario,action,region,battle,realm,book_open,page,work_mode,capture,event,raw_keys,raw_clicks,raw_scroll,raw_joy_buttons,raw_joy_axes,raw_total,accepted_keys,accepted_clicks,accepted_scroll,accepted_joy_buttons,accepted_joy_axes,accepted_total,limited_total,mouse_distance_px,raw_ap,accepted_ap,active_seconds");
        using var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var r = JsonSerializer.Deserialize<OperationStatisticsRow>(line) ?? throw new InvalidDataException("Invalid statistics row");
            var c = r.Context; var n = r.Counts;
            object[] values = { r.Schema, r.Session, r.Kind, r.StartSeconds, r.EndSeconds, r.EndSeconds - r.StartSeconds,
                c.Scenario, c.Action, c.Region, c.Battle, c.Realm, c.BookOpen, c.Page, c.WorkMode, c.Capture, r.Event,
                n.Keys, n.Clicks, n.Scroll, n.JoyButtons, n.JoyAxes, n.RawTotal, n.AcceptedKeys, n.AcceptedClicks,
                n.AcceptedScroll, n.AcceptedJoyButtons, n.AcceptedJoyAxes, n.AcceptedTotal, n.LimitedTotal,
                n.MouseDistance, n.RawAp, n.AcceptedAp, n.ActiveSeconds };
            writer.WriteLine(string.Join(",", values.Select(value => "\"" + Convert.ToString(value, CultureInfo.InvariantCulture)!.Replace("\"", "\"\"") + "\"")));
        }
        return output;
    }
}
