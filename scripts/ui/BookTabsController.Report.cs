using Godot;
using System.Linq;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private Button? _bossReviewToggle, _bossReviewEquipment, _bossReviewPreparation;
    private VBoxContainer? _bossReviewDetails;
    private Label? _bossReviewSummary, _bossReviewHeadline, _bossReviewComparison;
    private void BuildBossReviewUi(VBoxContainer parent)
    {
        _bossReviewHeadline = new() { Name = "BossReviewHeadline", Visible = false, AutowrapMode = TextServer.AutowrapMode.WordSmart }; parent.AddChild(_bossReviewHeadline);
        _bossReviewToggle = new() { Name = "BossReviewToggle", Text = "查看挑战回看", ToggleMode = true, Visible = false }; parent.AddChild(_bossReviewToggle);
        _bossReviewDetails = new() { Name = "BossReviewDetails", Visible = false }; parent.AddChild(_bossReviewDetails);
        _bossReviewComparison = new() { Name = "BossReviewComparison", Visible = false, AutowrapMode = TextServer.AutowrapMode.WordSmart }; _bossReviewDetails.AddChild(_bossReviewComparison);
        _bossReviewSummary = new() { Name = "BossReviewSummary", AutowrapMode = TextServer.AutowrapMode.WordSmart }; _bossReviewDetails.AddChild(_bossReviewSummary);
        HFlowContainer actions = new() { Name = "BossReviewActions" }; actions.AddThemeConstantOverride("h_separation", 8); actions.AddThemeConstantOverride("v_separation", 8); _bossReviewDetails.AddChild(actions);
        _bossReviewEquipment = new() { Name = "BossReviewEquipment", Text = "比较装备与强化" }; actions.AddChild(_bossReviewEquipment);
        _bossReviewPreparation = new() { Name = "BossReviewPreparation", Text = "查看可选准备" }; actions.AddChild(_bossReviewPreparation);
        _bossReviewToggle.Toggled += shown => { _bossReviewDetails.Visible = shown && _bossReviewToggle.Visible; _bossReviewToggle.Text = shown ? "收起挑战回看" : "查看挑战回看"; };
        _bossReviewEquipment.Pressed += () => SetActiveLeftTab("EquipmentTab");
        _bossReviewPreparation.Pressed += () =>
        {
            if (IsBossReportRelevant(_challengeGoalState?.LastBossReport) && _cultivationRoot.FindChild("PreparationToggle", true, false) is Button toggle)
            {
                toggle.ButtonPressed = true;
                if (_cultivationRoot.GetParent() is ScrollContainer scroll) Callable.From(() => scroll.EnsureControlVisible(_preparationDetails!)).CallDeferred();
            }
        };
    }
    private bool IsBossReportRelevant(BossChallengeReport? report)
    {
        if (report == null) return false;
        var goal = _challengeGoalState?.Goal; var loader = ServiceLocator.Instance?.LevelConfigLoader;
        return goal?.HasGoal == true ? goal.LevelId == report.LevelId && goal.BossId == report.BossId
            : loader?.ActiveLevelId == report.LevelId && loader.GetBossMonsterId() == report.BossId;
    }
    private void RefreshBossReviewUi()
    {
        if (_bossReviewToggle == null) return;
        var report = _challengeGoalState?.LastBossReport;
        _bossReviewToggle.Visible = report != null;
        _bossReviewHeadline!.Visible = report != null;
        _bossReviewDetails!.Visible = report != null && _bossReviewToggle.ButtonPressed;
        if (report == null) return;
        var loader = ServiceLocator.Instance?.LevelConfigLoader;
        string name = loader?.TryGetMonsterStatProfile(report.BossId, out var profile) == true ? UiText.LocalizeGameNames(profile.DisplayName) : "历史Boss";
        _bossReviewHeadline.Text = $"上次Boss挑战：{name} · {BossChallengeReportRules.ResultName(report.Result)} · {report.Rounds}回合";
        bool relevant = IsBossReportRelevant(report);
        string advice = relevant ? string.Join("\n", BossChallengeReportRules.Advice(report).Select((text, i) => $"调整{i + 1}：{text}"))
            : "记录来自其他目标，仅供回看；不会用它判断当前目标的准备。";
        if (relevant && report.Result != "victory" && report.StatisticsComplete && report.EnemyHealing > 0)
            advice += "\n" + (IsPreparationUnlocked(ActivityRegistry.GetRecipe(RecipeUnlockRules.Zhenling)!)
                ? "镇灵符配方已开放；是否制作由你选择，装备输出路线仍可继续。"
                : "镇灵符" + RecipeUnlockRules.Requirements(ActivityRegistry.GetRecipe(RecipeUnlockRules.Zhenling)!));
        _bossReviewSummary!.Text = BossChallengeReportRules.Details(report) + (advice != "" ? "\n" + advice : "")
            + (relevant && report.Result != "victory" && _playerActionState?.ConsumablePolicy == "none" ? "\n自动消耗已关闭，可继续走装备路线。" : "");
        _bossReviewComparison!.Text = BossChallengeReportRules.Comparison(report, _challengeGoalState?.PreviousBossReport);
        _bossReviewComparison.Visible = _bossReviewComparison.Text != "";
        _bossReviewEquipment!.Visible = relevant && report.Result != "victory";
        _bossReviewPreparation!.Visible = relevant && report.Result != "victory";
    }
}
