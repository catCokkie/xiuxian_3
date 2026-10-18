using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests;

public partial class GameplaySmokeTest
{
    private void RunMousePerformanceChecks(PrototypeRootController root, ServiceLocator s)
    {
        var tabs = Descendants(root).OfType<BookTabsController>().Single();
        var book = Descendants(root).OfType<SubmenuWindowController>().Single();
        tabs.RestoreActivePage("CultivationTab");
        foreach (bool visible in new[] { false, true })
        {
            book.Visible = visible;
            long beforeRefreshes = tabs.DynamicContentRefreshCount;
            double beforeDistance = s.InputActivityState!.TotalMouseMoveDistancePx;
            long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
            var clock = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 120; i++)
            {
                s.InputActivityState!.RegisterMouseMove(10); s.InputActivityState._Process(1.0 / 60);
                tabs._Process(1.0 / 60);
            }
            GD.Print($"MOUSE PERF: visible={visible} frames=120 refreshes={tabs.DynamicContentRefreshCount - beforeRefreshes} allocated_bytes={GC.GetAllocatedBytesForCurrentThread() - beforeBytes} elapsed_ms={clock.Elapsed.TotalMilliseconds:0.###}");
            long refreshes = tabs.DynamicContentRefreshCount - beforeRefreshes;
            Check(visible ? refreshes is > 0 and <= 8 : refreshes == 0,
                "mouse performance: " + (visible ? "visible status updates are coalesced to at most four per second" : "hidden book performs no dynamic page rebuilds"));
            Check(Math.Abs(s.InputActivityState.TotalMouseMoveDistancePx - beforeDistance - 1200) < .001,
                "mouse performance: UI throttling preserves all sampled movement distance");
        }
        book.Visible = false; s.ResourceWalletState!.AddLingqi(1);
        long beforeOpen = tabs.DynamicContentRefreshCount; book.SetVisibleImmediate(true);
        Check(tabs.DynamicContentRefreshCount > beforeOpen, "mouse performance: opening the book immediately refreshes current state");
    }

    private void RunOperationStatisticsChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
    {
        var recorder = root.OperationStatistics!; var input = s.InputActivityState!;
        var tabs = Descendants(root).OfType<BookTabsController>().Single();
        var start = Descendants(tabs).OfType<Button>().Single(x => x.Name == "OperationStartStop");
        var export = Descendants(tabs).OfType<Button>().Single(x => x.Name == "OperationExport");
        var scenario = Descendants(tabs).OfType<OptionButton>().Single(x => x.Name == "OperationScenario");
        Check(!recorder.IsRecording && recorder.Session == null, "statistics: disabled by default and no session created");
        Check(BookTabsController.OperationStatisticsDirectory().Replace('\\', '/').Contains("/.godot/"), "statistics: smoke output is isolated inside project cache");
        var saved = input.ToDictionary(); int originalLimit = input.MaxInputPerMinute;
        input._Process(61); input.ResetCurrentTick(); input.MaxInputPerMinute = 20;
        try
        {
            scenario.Select(1); start.EmitSignal(Button.SignalName.Pressed);
            Check(recorder.IsRecording && recorder.Session!.Context.Scenario == "typing", "statistics: actual UI starts a labeled session");
            Check(recorder.Session!.Context.Capture == "isolated_test", "statistics: synthetic input never labeled real global capture");
            for (int i = 0; i < 100; i++) input.RegisterKeyDown();
            for (int i = 0; i < 30; i++) input.RegisterMouseClick();
            input.RegisterMouseScroll(20); input.RegisterMouseMove(600);
            for (int i = 0; i < 7; i++) input.RegisterJoypadButton();
            for (int i = 0; i < 3; i++) input.RegisterJoypadAxisInput();
            input._Process(1);
            var totals = recorder.Session.Totals;
            Check(totals.RawTotal == 160 && totals.AcceptedTotal == 20 && totals.LimitedTotal == 140, "statistics: raw effective and limited inputs conserve totals across the real cap");
            Check(totals.Keys == 100 && totals.Clicks == 30 && totals.Scroll == 20 && totals.JoyButtons == 7 && totals.JoyAxes == 3 && totals.MouseDistance == 600, "statistics: all input types stay separate and movement is distance only");
            Check(input.InputEventsThisSecond == totals.AcceptedTotal && Math.Abs(input.ApFinal - totals.AcceptedAp) < .00001, "statistics: recorder observes actual game settlement without replacing it");
            scenario.Select(2); scenario.EmitSignal(OptionButton.SignalName.ItemSelected, 2L);
            Check(recorder.Session.Context.Scenario == "reading", "statistics: scenario changes segment the active session");
            input.RegisterMouseMove(1200); input._Process(61);
            Check(recorder.Session.Totals.RawTotal == 160 && recorder.Session.Totals.MouseDistance == 1800, "statistics: movement-only frame never fabricates a discrete input");
            long runtimeCount = input.SessionRawInputCount;
            input.FromDictionary(new());
            Check(input.SessionRawInputCount == runtimeCount, "statistics: process session total is not replaced by saved lifetime counts");
            input.RegisterKeyDown(); input._Process(1);
            Check(recorder.Session.Totals.RawTotal == 161, "statistics: loading historical counters cannot rewind or duplicate a session");
            s.PlayerActionState!.SetAction("cultivation");
            Check(recorder.Session.Context.Action == "cultivation", "statistics: real activity changes update context immediately");
            s.EventLogState!.AddEvent(EventLogState.CategoryCraft, "Do not copy this free-form message");
            s.EventLogState.FromDictionary(s.EventLogState.ToDictionary());
            export.EmitSignal(Button.SignalName.Pressed);
            Check(recorder.IsRecording && File.Exists(Path.ChangeExtension(recorder.LastPath, ".csv")), "statistics: live CSV export succeeds and recording continues");
            start.EmitSignal(Button.SignalName.Pressed);
            string path = recorder.LastPath;
            var rows = File.ReadLines(path).Select(line => JsonSerializer.Deserialize<OperationStatisticsRow>(line)!).ToArray();
            Check(rows.Where(r => r.Kind == "sample").Sum(r => r.Counts.RawTotal) == 161 && rows.Last().Counts.RawTotal == 161, "statistics: persisted sample sum matches final total exactly once");
            Check(rows.Count(r => r.Kind == "game_event" && r.Event == "result_craft") == 1, "statistics: loading an event history does not manufacture new results");
            Check(!File.ReadAllText(path).Contains("Do not copy"), "statistics: free-form game log messages are never copied");
            Check(rows[0].Metadata!["input_limit"] == "20" && rows[0].Metadata!.ContainsKey("started_utc")
                && rows[0].Metadata!["input_counting_revision"] == "2", "statistics: recording identifies actual cap, start time and counting revision");
            input.RegisterKeyDown(); input._Process(1);
            Check(recorder.Session.Totals.RawTotal == 161 && !recorder.IsRecording, "statistics: stop detaches input observer");
            string oldId = recorder.Session.Id; start.EmitSignal(Button.SignalName.Pressed);
            Check(recorder.Session!.Id != oldId && recorder.Session.Totals.RawTotal == 0 && recorder.LastPath != path, "statistics: restart creates a new independent session file");
            start.EmitSignal(Button.SignalName.Pressed);
            Check(!recorder.Start(recorder.LastPath) && !recorder.IsRecording, "statistics: invalid directory reports an error without leaving recording enabled");
            input.RegisterKeyDown(); input._Process(1);
            Check(input.InputEventsThisSecond == 1 && s.InputHookService!.IsPaused, "statistics: file failure leaves gameplay input operational and real hooks disabled");
        }
        finally { recorder.Stop(); input.MaxInputPerMinute = originalLimit; input.FromDictionary(saved); input.ResetCurrentTick(); }
        RunInputCaptureBoundaryChecks(root, s);
    }

    private void RunInputCaptureBoundaryChecks(PrototypeRootController root, ServiceLocator s)
    {
        var input = s.InputActivityState!; var hook = s.InputHookService!; var recorder = root.OperationStatistics!;
        var activityField = typeof(InputHookService).GetField("_activityState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var oldActivity = activityField.GetValue(hook);
        var saved = input.ToDictionary(); int oldLimit = input.MaxInputPerMinute;
        bool oldPaused = hook.IsPaused;
        try
        {
            activityField.SetValue(hook, input); input.ResetCurrentTick(); input.MaxInputPerMinute = 100;
            for (int i = 0; i < 5; i++) input.RegisterKeyDown();
            recorder.Start(BookTabsController.OperationStatisticsDirectory());
            Check(recorder.Session!.Totals.RawTotal == 0, "capture: queued inputs from before recording are excluded");
            for (int i = 0; i < 4; i++) input.RegisterKeyDown();
            input.RegisterMouseClick(); input.RegisterMouseClick(); hook.SetPaused(true); input._Process(1);
            Check(recorder.Session.Totals.RawTotal == 6, "capture: pause preserves all already captured pending inputs");
            long before = input.SessionRawInputCount;
            hook._Input(new InputEventKey { Pressed = true });
            Check(input.SessionRawInputCount == before && hook.CaptureStatus.Contains("暂停"), "capture: paused fallback rejects new inputs and reports paused state");
            hook.SetPaused(false);
            hook._Input(new InputEventKey { Pressed = true });
            hook._Input(new InputEventKey { Pressed = true, Echo = true });
            hook._Input(new InputEventKey { Pressed = false });
            for (int i = 0; i < 2; i++) hook._Input(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left });
            for (int i = 0; i < 4; i++) hook._Input(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.WheelUp, Factor = .25f });
            for (int i = 0; i < 4; i++) hook._Input(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.WheelLeft, Factor = .25f });
            input._Process(1);
            Check(recorder.Session.Totals.Keys == 6, "capture: fallback counts repeated keydown consistently with global capture and ignores release");
            Check(recorder.Session.Totals.Clicks == 4 && recorder.Session.Totals.Scroll == 2, "capture: fractional vertical and horizontal wheels accumulate as scroll rather than clicks");
            Check(hook.CaptureStatus.Contains("仅游戏窗口"), "capture: disconnected hook honestly identifies in-app-only fallback");
            for (int i = 0; i < 4; i++) input.RegisterKeyDown();
            string csv = recorder.Export();
            Check(recorder.Session.Totals.RawTotal == 16 && File.Exists(csv) && recorder.IsRecording, "capture: export includes queued input without stopping recording");
            input._Process(1);
            Check(recorder.Session.Totals.RawTotal == 16, "capture: exported pending input is not counted again next frame");
            for (int i = 0; i < 3; i++) input.RegisterKeyDown();
            recorder.Stop();
            var rows = File.ReadLines(recorder.LastPath).Select(x => JsonSerializer.Deserialize<OperationStatisticsRow>(x)!).ToArray();
            Check(recorder.Session.Totals.RawTotal == 19 && rows.Last().Counts.RawTotal == 19
                && rows.Where(x => x.Kind == "sample").Sum(x => x.Counts.RawTotal) == 19, "capture: stopping before next frame preserves exact disk and session totals");
            input._Process(1);
            Check(recorder.Session.Totals.RawTotal == 19, "capture: stopped recording never duplicates a flushed tail");
            var buffer = (GlobalInputCaptureBuffer)typeof(InputHookService).GetField("_captureBuffer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(hook)!;
            recorder.Start(BookTabsController.OperationStatisticsDirectory());
            long beforeBuffered = input.SessionRawInputCount;
            buffer.ObserveKeyDown(); buffer.ObserveKeyDown(); buffer.ObserveClick();
            for (int i = 0; i < 4; i++) buffer.ObserveScroll(30, true);
            buffer.ObserveMouseMove(0, 0); buffer.ObserveMouseMove(3, 4);
            Check(input.SessionRawInputCount == beforeBuffered, "capture: hook-side accumulation never directly mutates game state");
            recorder.Export();
            Check(recorder.Session!.Totals.RawTotal == 4 && recorder.Session.Totals.MouseDistance == 5
                && input.SessionRawInputCount == beforeBuffered + 4, "capture: export drains asynchronous counts and exact screen distance before settlement");
            input._Process(1);
            Check(recorder.Session.Totals.RawTotal == 4, "capture: drained hook batch is not duplicated by the next game frame");
            buffer.ObserveKeyDown(); buffer.ObserveKeyDown(); hook.SetPaused(true); buffer.ObserveKeyDown(); recorder.Stop();
            Check(recorder.Session.Totals.RawTotal == 6, "capture: pause and stop preserve buffered tail while rejecting new hook input");
            hook.SetPaused(false);
            input.ResetCurrentTick(); input.MaxInputPerMinute = 3;
            for (int i = 0; i < 3; i++) input.RegisterKeyDown(); input._Process(1);
            hook.SetPaused(true); hook.SetPaused(false); input.RegisterKeyDown(); input._Process(1);
            Check(input.InputEventsThisSecond == 0, "capture: pause cannot reset or bypass the shared input cap");
            hook.StartHook();
            Check(!hook.IsHookActive, "capture: smoke mode refuses to start real system hooks even on an explicit reconnect");
        }
        finally
        {
            recorder.Stop(); hook.SetPaused(oldPaused); activityField.SetValue(hook, oldActivity);
            input.MaxInputPerMinute = oldLimit; input.FromDictionary(saved); input.ResetCurrentTick();
        }
    }
}
