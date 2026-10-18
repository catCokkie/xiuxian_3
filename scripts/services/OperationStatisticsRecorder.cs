using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Xiuxian.Scripts.Services;

// Not persisted in player saves. Recording is explicitly started for each test session.
public sealed class OperationStatisticsRecorder : IDisposable
{
    private readonly InputActivityState _input;
    private readonly Func<OperationStatisticsContext> _context;
    private readonly Func<Dictionary<string, string>> _metadata;
    private OperationStatisticsFile? _file;
    private readonly Stopwatch _clock = new();
    private double _lastObservedSeconds;
    public OperationStatisticsSession? Session { get; private set; }
    public bool IsRecording => _file != null;
    public string LastPath { get; private set; } = "";
    public string Status { get; private set; } = "尚未开始统计。";

    public OperationStatisticsRecorder(InputActivityState input, Func<OperationStatisticsContext> context,
        Func<Dictionary<string, string>> metadata)
    { _input = input; _context = context; _metadata = metadata; }

    public bool Start(string directory)
    {
        if (IsRecording) return true;
        // Exclude data queued before the user started this recording.
        _input.FlushPendingInput();
        try
        {
            _file = new(directory); LastPath = _file.Path;
            var metadata = _metadata(); metadata["started_utc"] = DateTime.UtcNow.ToString("O");
            metadata["sampling"] = "60s or game-context change; no per-input timestamps";
            Session = new(_file.Write, _context(), metadata);
            _lastObservedSeconds = 0; _clock.Restart();
            _input.StatisticsObserved += Observe;
            Status = "统计中；每60秒或情境变化时保存。";
            return true;
        }
        catch (Exception ex) when (IsFileError(ex)) { Fail(ex); return false; }
    }
    public void ContextChanged()
    {
        if (!IsRecording) return;
        try { Session!.SetContext(_context()); }
        catch (Exception ex) when (IsFileError(ex)) { Fail(ex); }
    }
    public void RecordEvent(string eventId)
    {
        if (!IsRecording) return;
        try { Session!.SetContext(_context()); Session.RecordEvent(eventId); }
        catch (Exception ex) when (IsFileError(ex)) { Fail(ex); }
    }
    private void Observe(OperationInputObservation observation)
    {
        if (!IsRecording) return;
        try
        {
            Session!.SetContext(_context());
            double now = _clock.Elapsed.TotalSeconds;
            double duration = Math.Max(0, now - _lastObservedSeconds); _lastObservedSeconds = now;
            Session.Observe(observation with { Seconds = duration, ActiveSeconds = Math.Min(duration, observation.ActiveSeconds) });
        }
        catch (Exception ex) when (IsFileError(ex)) { Fail(ex); }
    }
    public void Stop()
    {
        if (!IsRecording) return;
        try
        {
            _input.FlushPendingInput();
            if (!IsRecording) return; // A write failure during the boundary flush already closed the file.
            Session!.Observe(new(Math.Max(0, _clock.Elapsed.TotalSeconds - _lastObservedSeconds), default, default, 0, 0, 0, 0));
            Session.Stop(); Status = "统计已停止，日志已保留。";
        }
        catch (Exception ex) when (IsFileError(ex)) { Status = "统计保存失败：" + ex.Message; }
        finally { Close(); }
    }
    public string Export()
    {
        try
        {
            if (IsRecording) { _input.FlushPendingInput(); if (IsRecording) Session!.Flush(); }
            if (LastPath.Length == 0) { Status = "请先开始一次统计。"; return ""; }
            string path = OperationStatisticsFile.ExportCsv(LastPath);
            Status = "CSV已导出：" + path; return path;
        }
        catch (Exception ex) when (IsFileError(ex) || ex is System.Text.Json.JsonException)
        { if (IsRecording) Fail(ex); else Status = "导出失败：" + ex.Message; return ""; }
    }
    private static bool IsFileError(Exception ex) => ex is IOException or UnauthorizedAccessException or System.Security.SecurityException;
    private void Fail(Exception ex) { Status = "统计已停止：" + ex.Message; Close(); }
    private void Close()
    {
        _input.StatisticsObserved -= Observe;
        try { _file?.Dispose(); }
        catch (Exception ex) when (IsFileError(ex)) { Status = "统计文件关闭失败：" + ex.Message; }
        _file = null;
    }
    public void Dispose() => Stop();
}
