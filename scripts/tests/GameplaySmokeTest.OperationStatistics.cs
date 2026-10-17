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
            Check(rows[0].Metadata!["input_limit"] == "20" && rows[0].Metadata!.ContainsKey("started_utc"), "statistics: recording identifies actual cap and start time");
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
    }
}
