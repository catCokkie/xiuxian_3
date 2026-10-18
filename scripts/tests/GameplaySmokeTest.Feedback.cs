using Godot;
using System;
using System.Linq;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests;

public partial class GameplaySmokeTest
{
    private void RunGainFeedbackChecks(PrototypeRootController root, ServiceLocator s)
    {
        var feedback = root.ProgressFeedback; var input = s.InputActivityState!; var player = s.PlayerProgressState!;
        var hook = s.InputHookService!; var tabs = Descendants(root).OfType<BookTabsController>().Single();
        var book = Descendants(root).OfType<SubmenuWindowController>().Single();
        var savedPlayer = player.ToDictionary(); var savedInput = input.ToDictionary(); var savedAction = s.PlayerActionState!.ToDictionary();
        var savedGrowth = s.ActivityConversionService!.ToDictionary(); var savedSettings = tabs.ToSystemSettingsDictionary();
        int oldLimit = input.MaxInputPerMinute; bool oldPaused = hook.IsPaused, oldAuto = player.AutoBreakthrough;
        try
        {
            hook.SetPaused(false); input.ResetCurrentTick(); input.MaxInputPerMinute = 600;
            player.AutoBreakthrough = false; player.FromDictionary(new() { ["realm_level"] = 1, ["realm_exp"] = 10.0 });
            s.PlayerActionState.SetAction("cultivation"); s.PlayerActionState.SetWorkMode(false);
            tabs.FromSystemSettingsDictionary(new() { ["gain_feedback_motion"] = true });
            book.SetVisibleImmediate(true); tabs.RestoreActivePage("CultivationTab"); feedback._Process(5);
            var headline = (Label)tabs.FindChild("GainHeadline", true, false);
            var hud = root.GetNode<Label>("MainBarWindow/Chrome/GainFeedbackHud/GainHudText");
            double before = player.RealmExp; input.RegisterKeyDown(); input._Process(1.0 / 60); feedback._Process(.1);
            Check(headline.Text.Contains("+0.35") && hud.Text.Contains("+0.35"), "feedback: one real effective input shows its actual experience in both home and HUD");
            Check(Math.Abs(player.RealmExp - before - .35) < .000001, "feedback: presentation never grants an extra experience reward");
            Check(root.GetNode<Label>("MainBarWindow/Chrome/CultivationProgressBar/ExactExperience").Text.Contains("10.35"), "feedback: exact experience stays visible on the cultivation bar");
            for (int i = 0; i < 9; i++) input.RegisterKeyDown(); input._Process(1.0 / 60); feedback._Process(.1);
            Check(feedback.Feedback.Inputs == 10 && headline.Text.Contains("+3.5"), "feedback: rapid inputs merge their actual gains in a single fixed label");
            var bar = root.GetNode<Control>("MainBarWindow/Chrome/CultivationProgressBar");
            s.PlayerActionState.SetWorkMode(true); feedback._Process(.1);
            Check(bar.Modulate == Colors.White && hud.Text.Contains("3.5"), "feedback: work mode removes the pulse while retaining numerical input feedback");
            s.PlayerActionState.SetWorkMode(false);
            ((CheckButton)tabs.FindChild("GainMotion", true, false)).ButtonPressed = false; feedback._Process(.1);
            Check(!tabs.GainMotionEnabled && !tabs.ToSystemSettingsDictionary()["gain_feedback_motion"].AsBool() && bar.Modulate == Colors.White,
                "feedback: motion preference persists in existing settings without changing rewards");
            input.ResetCurrentTick(); input.MaxInputPerMinute = 1;
            input.RegisterKeyDown(); input._Process(.01);
            double capped = player.RealmExp;
            for (int i = 0; i < 20; i++) input.RegisterKeyDown(); input._Process(.01); feedback._Process(.1);
            Check(hud.Text.Contains("收益已满") && player.RealmExp == capped, "feedback: capped inputs show an honest status instead of fabricated rewards");
            input.ResetCurrentTick(); input.MaxInputPerMinute = 600; feedback._Process(3);
            input.RegisterMouseMove(600); input._Process(.01); feedback._Process(.1);
            Check(feedback.Feedback.Experience == 0 && hud.Text.Contains("灵气"), "feedback: movement feedback uses actual AP resources rather than key experience");
            feedback._Process(3);
            if (s.ActivityConversionService.GrowthClock.LastSettledUnix <= 0) s.ActivityConversionService.GrowthClock.Claim(Time.GetUnixTimeFromSystem());
            s.ActivityConversionService.ApplyOnlineBasicGrowth(s.ActivityConversionService.GrowthClock.LastSettledUnix + 10); feedback._Process(.1);
            Check(hud.Text.Contains("静修到账"), "feedback: time growth is visually identified as a separate source");
            s.EventLogState!.AddEvent(EventLogState.CategoryCraft, "回气丹炼制完成 ×1", accentId: EventLogState.AccentHighlight); feedback._Process(.1);
            Check(feedback.LastMilestone.Contains("回气丹") && ((Label)tabs.FindChild("GainMilestone", true, false)).Visible,
                "feedback: a new completed result remains visible in the home card");
            string milestone = feedback.LastMilestone; s.EventLogState.FromDictionary(s.EventLogState.ToDictionary());
            s.EventLogState.AddBattleEvent("test", "monster", "boss", 1, "失败", ""); feedback._Process(.1);
            Check(feedback.LastMilestone == milestone, "feedback: loading history and battle failures never manufacture positive milestones");
            player.FromDictionary(new() { ["realm_level"] = 1, ["realm_exp"] = 400.0 }); feedback._Process(.1);
            Check(((Label)tabs.FindChild("GainGoal", true, false)).Text.Contains("已可突破"), "feedback: real manual-breakthrough readiness replaces the remaining-experience goal");
            player.FromDictionary(new() { ["realm_level"] = 10, ["realm_exp"] = 400.0 }); feedback._Process(.1);
            Check(((Label)tabs.FindChild("GainGoal", true, false)).Text.Contains("继续保留"), "feedback: chapter cap never implies an unimplemented next realm");
            ((Button)tabs.FindChild("GainReview", true, false)).EmitSignal(Button.SignalName.Pressed);
            Check(tabs.ActivePageName == "BattleLogTab", "feedback: results review opens the actual existing event history");
            int nodes = Descendants(root).Count(); long paints = feedback.PaintCount;
            for (int i = 0; i < 1000; i++) feedback._Process(.01);
            Check(Descendants(root).Count() == nodes && feedback.PaintCount - paints <= 101,
                "feedback: repeated updates keep a fixed node count and bounded text refresh rate");
        }
        finally
        {
            input.MaxInputPerMinute = oldLimit; player.AutoBreakthrough = oldAuto; hook.SetPaused(oldPaused);
            input.FromDictionary(savedInput); input.ResetCurrentTick(); player.FromDictionary(savedPlayer);
            s.PlayerActionState.FromDictionary(savedAction); s.ActivityConversionService.FromDictionary(savedGrowth);
            tabs.FromSystemSettingsDictionary(savedSettings);
        }
    }
}
