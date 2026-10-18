using Godot;
using System;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.UI;

public partial class ProgressFeedbackController : Node
{
    public GainFeedbackState Feedback { get; } = new();
    private ServiceLocator _services = null!;
    private MainBarLayoutController _bar = null!;
    private BookTabsController _book = null!;
    private double _paintAge, _lastBasic, _basicRecent, _basicAge = 100, _milestoneAge = 100;
    private string _milestone = "";
    public string LastMilestone => _milestone;
    public long PaintCount { get; private set; }

    public override void _Ready()
    {
        _services = ServiceLocator.Instance!;
        _bar = GetParent().GetNode<MainBarLayoutController>("MainBarWindow");
        _book = GetParent().GetNode<BookTabsController>("SubmenuBookWindow/BookFrame");
        _services.InputActivityState!.StatisticsObserved += OnObserved;
        _services.ActivityConversionService!.SettlementApplied += OnSettlement;
        _services.EventLogState!.EventAdded += OnResult;
        _lastBasic = _services.ActivityConversionService.SessionOnlineBasicGrowthExperience;
        Paint();
    }

    public override void _ExitTree()
    {
        if (_services == null) return;
        _services.InputActivityState!.StatisticsObserved -= OnObserved;
        _services.ActivityConversionService!.SettlementApplied -= OnSettlement;
        _services.EventLogState!.EventAdded -= OnResult;
    }

    private void OnObserved(OperationInputObservation batch) => Feedback.ObserveLimit(batch.Raw.TotalCount, batch.Accepted.TotalCount);
    private void OnSettlement(double ap, double lingqi, double insight, double experience, double active)
        => Feedback.ObserveReward(_services.InputActivityState!.InputEventsThisSecond, ap, experience, lingqi, insight);

    private void OnResult(EventLogEntryData entry)
    {
        bool positive = entry.CategoryId is EventLogState.CategoryCraft or EventLogState.CategoryBreakthrough
            or EventLogState.CategoryMastery or EventLogState.CategoryEquipment
            || (entry.CategoryId == EventLogState.CategoryBattle && entry.Outcome == "胜利");
        if (!positive || entry.AccentId == EventLogState.AccentWarning) return;
        _milestone = UiText.LocalizeGameNames(entry.Message + (entry.RewardSummary.Length > 0 ? " · " + entry.RewardSummary : ""));
        _milestoneAge = 0;
    }

    public override void _Process(double delta)
    {
        Feedback.Tick(delta); _paintAge += delta; _basicAge += delta; _milestoneAge += delta;
        double basic = _services.ActivityConversionService!.SessionOnlineBasicGrowthExperience;
        if (basic > _lastBasic) { _basicRecent = basic - _lastBasic; _basicAge = 0; }
        _lastBasic = basic;
        bool motion = _book.GainMotionEnabled && _services.PlayerActionState?.WorkMode != true;
        _bar.PaintGainPulse(motion ? (float)Feedback.Pulse : 0);
        _book.PaintGainPulse(motion ? (float)Feedback.Pulse : 0);
        // This updates a handful of fixed labels; full book rebuilding stays at the existing 4 Hz cap.
        if (_paintAge >= .1) { _paintAge = 0; Paint(); }
    }

    private void Paint()
    {
        PaintCount++;
        var player = _services.PlayerProgressState!; var conversion = _services.ActivityConversionService!;
        string gain;
        if (_services.InputHookService?.IsPaused == true) gain = "采集暂停 · 静修继续";
        else if (Feedback.Limited) gain = "输入已记录 · 窗口收益已满";
        else if (Feedback.Age < GainFeedbackState.BurstGapSeconds)
            gain = Feedback.Experience > 0 ? $"输入实得 +{Feedback.Experience:0.##} 经验" : $"活动实得 +{Feedback.Lingqi:0.##} 灵气";
        else if (_basicAge < 3) gain = $"静修到账 +{_basicRecent:0.##} 经验";
        else gain = $"本次输入已积累 {conversion.SessionInputExperience:0.##} 经验";
        string detail = Feedback.Age < GainFeedbackState.BurstGapSeconds
            ? $"这段有效输入 {Feedback.Inputs} 次 · 灵气 +{Feedback.Lingqi:0.##} · 悟性 +{Feedback.Insight:0.##}"
            : $"本次启动 · 输入经验 +{conversion.SessionInputExperience:0.##} · 在线静修 +{conversion.SessionOnlineBasicGrowthExperience:0.##}";
        string goal = GainFeedbackState.NextRealmGoal(player.RealmLevel, player.RealmExp, player.RealmExpRequired,
            player.CanBreakthrough, player.HasRequiredBossClear);
        string hud = _milestoneAge < 4 && _services.PlayerActionState?.WorkMode != true ? "成果 · " + _milestone : gain;
        _bar.PaintGainFeedback(hud, player.RealmExp, player.RealmExpRequired, player.IsAtContentLimit);
        _book.PaintGainFeedback(gain, detail, goal, _milestone, player.CanBreakthrough);
    }
}
