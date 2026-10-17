using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game;

public partial class PrototypeRootController
{
    public OperationStatisticsRecorder? OperationStatistics { get; private set; }
    private void SetupOperationStatistics()
    {
        if (_activityState == null) return;
        OperationStatistics = new(_activityState, StatisticsContext, StatisticsMetadata);
        _bookTabs.BindOperationStatistics(OperationStatistics);
        if (_playerActionState != null) _playerActionState.ActionChanged += OnStatisticsActionChanged;
        if (_eventLogState != null) _eventLogState.EventAdded += OnStatisticsGameEvent;
    }
    private void CloseOperationStatistics()
    {
        OperationStatistics?.Dispose();
        if (_playerActionState != null) _playerActionState.ActionChanged -= OnStatisticsActionChanged;
        if (_eventLogState != null) _eventLogState.EventAdded -= OnStatisticsGameEvent;
    }
    private void OnStatisticsActionChanged(string action, string target, string variant)
    {
        if (OperationStatistics?.Session is not { } session || !OperationStatistics.IsRecording) return;
        var context = StatisticsContext();
        if (session.Context.Action != context.Action) OperationStatistics.RecordEvent("action_changed");
        else OperationStatistics.ContextChanged();
    }
    private void OnStatisticsGameEvent(EventLogEntryData entry)
    {
        // Outcome categories are separate from explicit UI actions; never copy free-form log text.
        string name = entry.CategoryId switch
        {
            EventLogState.CategoryBattle => "result_battle_" + entry.SubjectType + (entry.Outcome == "胜利" ? "_win" : "_loss"),
            EventLogState.CategoryCraft => "result_craft",
            EventLogState.CategoryBreakthrough => "result_breakthrough",
            EventLogState.CategoryEquipment => "result_equipment",
            EventLogState.CategoryMastery => "result_mastery",
            _ => "",
        };
        if (name.Length > 0) OperationStatistics?.RecordEvent(name);
    }
    private OperationStatisticsContext StatisticsContext()
    {
        string capture = _hookService?.IsPaused == true ? "paused" : _hookService?.IsHookActive == true ? "global"
            : _hookService?.EnableInAppFallback == true ? "in_app_fallback" : "unavailable";
        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--smoke-test") >= 0) capture = "isolated_test";
        string battle = _exploreProgressController?.IsInBossBattle == true ? "boss_battle"
            : _exploreProgressController?.IsInBattle == true ? "normal_battle"
            : _exploreProgressController?.IsRecoveringForBattle == true ? "recovering"
            : _exploreProgressController?.IsBossChallengeReady == true ? "boss_ready" : "exploring";
        if (_exploreProgressController?.IsInBattle == true && _playerActionState?.IsDungeonAction != true) battle = "paused_" + battle;
        bool open = _submenu.Visible;
        return new(_bookTabs.OperationStatisticsScenario, _playerActionState?.ActionId ?? "", _levelConfigLoader?.ActiveLevelId ?? "",
            battle, _playerProgressState?.RealmLevel ?? 1, open, open ? _bookTabs.ActivePageName : "closed",
            _playerActionState?.WorkMode ?? false, capture);
    }
    private Dictionary<string, string> StatisticsMetadata()
    {
        string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        return new()
        {
            ["save_schema"] = SaveSchemaVersion.ToString(), ["game_version"] = ProjectSettings.GetSetting("application/config/version", "unspecified").AsString(),
            ["input_limit"] = Number(_activityState!.MaxInputPerMinute), ["input_window_seconds"] = Number(_activityState.InputWindowSeconds),
            ["window_at_start"] = "existing sliding input/AP window is preserved; no reset on recording or scenario change",
            ["key_weight"] = Number(_activityState.KeyDownWeight), ["click_weight"] = Number(_activityState.MouseClickWeight),
            ["scroll_weight"] = Number(_activityState.ScrollStepWeight), ["move_divider"] = Number(_activityState.MovePxDivider),
            ["joy_button_weight"] = Number(_activityState.JoypadButtonWeight), ["joy_axis_weight"] = Number(_activityState.JoypadAxisWeight),
            ["joy_axis_enabled"] = (_hookService?.EnableJoyAxisCounting ?? false).ToString(),
            ["explore_progress_per_input_at_start"] = Number(_exploreProgressController?.ProgressPerInput ?? 0),
            ["boss_round_limit"] = Number(_exploreProgressController?.MaxBossBattleRounds ?? 0),
            ["privacy"] = "counts and game state only; scenario is self-reported; no key values, positions, app names or automatic upload",
        };
    }
}
