using Godot;

public partial class BookTabsController
{
    private Label? _gainHeadline, _gainDetail, _gainGoal, _gainMilestone;
    private CheckButton? _gainMotion;
    private Label? _homeExactExperience, _homeStoredExperience;
    public bool GainMotionEnabled => !_settings.ContainsKey("gain_feedback_motion") || _settings["gain_feedback_motion"].AsBool();

    private void BuildGainFeedback(VBoxContainer parent)
    {
        PanelContainer panel = new() { Name = "HomeGainFeedback", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("E5D6B7"),
            BorderColor = new Color("AE8C54"), BorderWidthLeft = 3, ContentMarginLeft = 12,
            ContentMarginRight = 12, ContentMarginTop = 3, ContentMarginBottom = 3 });
        parent.AddChild(panel);
        VBoxContainer body = new(); body.AddThemeConstantOverride("separation", 3); panel.AddChild(body);
        HFlowContainer header = new(); body.AddChild(header);
        _gainHeadline = new Label { Name = "GainHeadline", Text = "日常输入，积少成多" };
        _gainHeadline.AddThemeFontSizeOverride("font_size", 20); _gainHeadline.AddThemeColorOverride("font_color", new Color("6F4C24"));
        header.AddChild(_gainHeadline);
        _gainMotion = new CheckButton { Name = "GainMotion", Text = "收益微光", ButtonPressed = GainMotionEnabled,
            TooltipText = "关闭后保留收益数字与成果。工作模式也会关闭微光，不改变收益。" };
        header.AddChild(_gainMotion);
        _gainMotion.AddThemeFontSizeOverride("font_size", 12);
        foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color" })
            _gainMotion.AddThemeColorOverride(state, new Color("FFF6E2"));
        _gainMotion.Toggled += value => { if (!_isApplyingSettingsUi) OnSettingChanged("gain_feedback_motion", value); };
        _gainDetail = new Label { Name = "GainDetail", Text = "实际到账的经验、灵气与悟性会在这里汇总。", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _gainGoal = new Label { Name = "GainGoal", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _gainMilestone = new Label { Name = "GainMilestone", Visible = false, ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
        body.AddChild(_gainDetail); body.AddChild(_gainGoal); body.AddChild(_gainMilestone);
        Button review = new() { Name = "GainReview", Text = "成果回看", TooltipText = "查看动态中保留的制作、突破、战斗与装备成果。" };
        review.AddThemeFontSizeOverride("font_size", 12);
        review.Pressed += () => SetActiveLeftTab("BattleLogTab"); header.AddChild(review);
    }

    public void PaintGainFeedback(string headline, string detail, string goal, string milestone, bool ready)
    {
        // Fixed labels only: do not rebuild the page or steal focus from working applications.
        if (_gainHeadline == null || !IsVisibleInTree()) return;
        _gainHeadline.Text = headline; _gainDetail!.Text = detail; _gainGoal!.Text = goal;
        _gainGoal.AddThemeColorOverride("font_color", new Color(ready ? "7F5421" : "5B4838"));
        _gainMilestone!.Visible = milestone.Length > 0;
        _gainMilestone.Text = "最近成果 · " + milestone;
        _gainMilestone.TooltipText = milestone;
    }

    public void PaintGainPulse(float pulse)
    {
        if (_gainHeadline != null && IsVisibleInTree())
            _gainHeadline.Modulate = Colors.White.Lerp(new Color("C59139"), pulse * .25f);
    }
}
