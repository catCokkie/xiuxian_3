using Godot;
using Xiuxian.Scripts.Services;
using Xiuxian.Scripts.UI;

public partial class MainBarLayoutController : Control
{
    private const float DefaultMinWidth = 800.0f;
    private const float DefaultMaxWidth = 1500.0f;

    [Signal]
    public delegate void BookButtonPressedEventHandler();
    [Signal]
    public delegate void LayoutChangedEventHandler(float x, float width);

    [Export] public float MinWidth = DefaultMinWidth;
    [Export] public float MaxWidth = DefaultMaxWidth;
    [Export] public bool LockToBottom = true;
    [Export] public float MinBottomMargin = 8.0f;

    private Button _dragHandle = null!;
    private Button _resizeHandle = null!;
    private Button _bookButton = null!;
    private Label _zoneLabel = null!;
    private Label _activityRateLabel = null!;
    private Label _realmStageLabel = null!;
    private ProgressBar _exploreProgressBar = null!;
    private Label _cultivationLabel = null!;
    private ProgressBar _cultivationProgressBar = null!;
    private Button _breakthroughButton = null!;
    private Panel _battleTrack = null!;
    private Panel _validationPanel = null!;
    private OptionButton? _actionModeOptionButton;
    private OptionButton? _levelOptionButton;
    private PanelContainer _bookUnreadBadge = null!;
    private Label _bookUnreadBadgeLabel = null!;

    private bool _isDragging;
    private bool _isResizing;
    private Vector2 _lastMousePos;
    private float _fixedBottomY;
    private float _bottomMargin;
    private float _preferredWidth = 1152;
    private Vector2 _lastViewport;
    private float _lastBreakthroughWidth;
    private float _lastWorkWidth;
    private bool _expanded;
    private Button? _workButton;
    private Label _exploreCaption = null!;
    private Label _cultivationCaption = null!;
    private PanelContainer _gainPanel = null!;
    private Label _gainHud = null!, _exactExperience = null!;

    public override void _Ready()
    {
        _dragHandle = GetNode<Button>("Chrome/DragHandleButton");
        _resizeHandle = GetNode<Button>("Chrome/ResizeHandleButton");
        _bookButton = GetNode<Button>("Chrome/BookButton");
        _zoneLabel = GetNode<Label>("Chrome/ZoneLabel");
        _activityRateLabel = GetNode<Label>("Chrome/ActivityRateLabel");
        _realmStageLabel = GetNode<Label>("Chrome/RealmStageLabel");
        _exploreProgressBar = GetNode<ProgressBar>("Chrome/ExploreProgressBar");
        _cultivationLabel = GetNode<Label>("Chrome/CultivationLabel");
        _cultivationProgressBar = GetNode<ProgressBar>("Chrome/CultivationProgressBar");
        _cultivationProgressBar.ShowPercentage = false;
        _breakthroughButton = GetNode<Button>("Chrome/BreakthroughButton");
        _battleTrack = GetNode<Panel>("Chrome/BattleTrack");
        _validationPanel = GetNode<Panel>("Chrome/ConfigValidationPanel");
        _actionModeOptionButton = GetNodeOrNull<OptionButton>("Chrome/ActionModeOptionButton");
        _levelOptionButton = GetNodeOrNull<OptionButton>("Chrome/LevelOptionButton");
        ApplyCompactTheme();
        EnsureBookUnreadBadge();

        _dragHandle.GuiInput += OnDragHandleGuiInput;
        _resizeHandle.GuiInput += OnResizeHandleGuiInput;
        _bookButton.Pressed += () => EmitSignal(SignalName.BookButtonPressed);
        _dragHandle.Text = UiText.DragHandle;
        _resizeHandle.Text = UiText.ResizeHandle;
        _bookButton.Text = string.Empty;
        _bookButton.Icon = GameArtCatalog.Get("ui:book");
        _bookButton.AddThemeConstantOverride("icon_max_width", 24);
        _bookButton.ExpandIcon = true;
        _bookButton.TooltipText = "打开修炼书卷";
        _zoneLabel.Visible = false;
        SetBookUnreadCount(0);

        _bottomMargin = MinBottomMargin;
        _preferredWidth = Size.X;
        _exploreCaption = new Label { Text = "探索", MouseFilter = MouseFilterEnum.Ignore };
        _cultivationCaption = new Label { Text = "修炼", MouseFilter = MouseFilterEnum.Ignore };
        GetNode<Control>("Chrome").AddChild(_exploreCaption);
        GetNode<Control>("Chrome").AddChild(_cultivationCaption);
        _gainPanel = new PanelContainer { Name = "GainFeedbackHud", MouseFilter = MouseFilterEnum.Ignore };
        var gainStyle = new StyleBoxFlat { BgColor = new Color("5B4838"), BorderColor = new Color("9E8052"),
            BorderWidthLeft = 2, ContentMarginLeft = 8, ContentMarginRight = 8 };
        gainStyle.SetCornerRadiusAll(4); _gainPanel.AddThemeStyleboxOverride("panel", gainStyle);
        _gainHud = new Label { Name = "GainHudText", Text = "日常输入，积少成多", ClipText = true, MouseFilter = MouseFilterEnum.Ignore };
        _gainHud.AddThemeFontSizeOverride("font_size", 14); _gainHud.AddThemeColorOverride("font_color", new Color("FFE3A8"));
        _gainPanel.AddChild(_gainHud); GetNode<Control>("Chrome").AddChild(_gainPanel);
        _exactExperience = new Label { Name = "ExactExperience", HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, ClipText = true, MouseFilter = MouseFilterEnum.Ignore };
        _exactExperience.AddThemeFontSizeOverride("font_size", 12);
        _exactExperience.AddThemeColorOverride("font_color", new Color("FFF9EC"));
        _cultivationProgressBar.AddChild(_exactExperience);
        _exactExperience.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _realmStageLabel.ClipText = true;
        _realmStageLabel.AddThemeColorOverride("font_color", new Color("FFF6E2"));
        if (_levelOptionButton != null) { _levelOptionButton.ClipText = true; _levelOptionButton.FitToLongestItem = false; }
        if (_actionModeOptionButton != null) { _actionModeOptionButton.ClipText = true; _actionModeOptionButton.FitToLongestItem = false; }
        _cultivationLabel.ClipText = true;
        _cultivationLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _fixedBottomY = GetBottomLockedY();
        Position = new Vector2(Position.X, _fixedBottomY);
        UpdateRightAnchoredLayout();
    }

    public override void _Process(double delta)
    {
        Vector2 viewport = GetViewportRect().Size;
        if (_workButton == null)
        {
            _workButton = GetNodeOrNull<Button>("Chrome/BattleTrack/WorkModeButton");
            if (_workButton != null) { _workButton.Reparent(GetNode("Chrome")); UpdateRightAnchoredLayout(); }
        }
        float width = Mathf.Min(_preferredWidth, Mathf.Max(320, viewport.X - 16));
        float height = _expanded ? 210 : 96;
        float breakthroughWidth = _breakthroughButton.GetCombinedMinimumSize().X;
        float workWidth = _workButton?.GetCombinedMinimumSize().X ?? 0;
        if (Size != new Vector2(width, height) || _lastViewport != viewport || _lastBreakthroughWidth != breakthroughWidth || _lastWorkWidth != workWidth)
        {
            Size = new Vector2(width, height);
            _lastViewport = viewport;
            _lastBreakthroughWidth = breakthroughWidth;
            _lastWorkWidth = workWidth;
            UpdateRightAnchoredLayout();
        }
        float x = Mathf.Clamp(Position.X, 8, Mathf.Max(8, viewport.X - Size.X - 8));
        Position = new Vector2(x, LockToBottom ? GetBottomLockedY() : Position.Y);
        _validationPanel.Hide();
    }

    public void SetBattleExpanded(bool expanded)
    {
        if (_expanded == expanded) return;
        _expanded = expanded;
        Size = new Vector2(Size.X, expanded ? 210 : 96);
        UpdateRightAnchoredLayout();
    }

    public void PaintGainFeedback(string message, double experience, double required, bool capped)
    {
        _gainHud.Text = message;
        _gainHud.TooltipText = message;
        _exactExperience.Text = capped ? $"已保留 {experience:0.##} 经验" : $"{experience:0.##} / {required:0.##}";
        _cultivationProgressBar.TooltipText = _exactExperience.Text;
    }

    public void PaintGainPulse(float pulse)
    {
        _gainPanel.Modulate = Colors.White.Lerp(new Color("FFDC91"), pulse * .35f);
        _cultivationProgressBar.Modulate = Colors.White.Lerp(new Color("FFDC91"), pulse * .3f);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && !mouseButton.Pressed)
        {
            if (_isDragging || _isResizing)
            {
                EmitSignal(SignalName.LayoutChanged, Position.X, Size.X);
            }

            _isDragging = false;
            _isResizing = false;
        }

        if (@event is not InputEventMouseMotion mouseMotion)
        {
            return;
        }

        if (_isDragging)
        {
            Vector2 delta = mouseMotion.GlobalPosition - _lastMousePos;
            float targetX = Position.X + delta.X;
            float maxX = GetViewportRect().Size.X - Size.X;
            Position = new Vector2(Mathf.Clamp(targetX, 0.0f, Mathf.Max(maxX, 0.0f)), LockToBottom ? _fixedBottomY : Position.Y + delta.Y);
            _lastMousePos = mouseMotion.GlobalPosition;
            EmitSignal(SignalName.LayoutChanged, Position.X, Size.X);
        }

        if (_isResizing)
        {
            float nextWidth = Mathf.Clamp(Size.X + mouseMotion.Relative.X, Mathf.Min(MinWidth, GetViewportRect().Size.X - 16), Mathf.Min(MaxWidth, GetViewportRect().Size.X - 16));
            _preferredWidth = nextWidth;
            Size = new Vector2(nextWidth, Size.Y);
            UpdateRightAnchoredLayout();
            EmitSignal(SignalName.LayoutChanged, Position.X, Size.X);
        }
    }

    private void OnDragHandleGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            _isDragging = mouseButton.Pressed;
            _lastMousePos = mouseButton.GlobalPosition;
        }
    }

    private void OnResizeHandleGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            _isResizing = mouseButton.Pressed;
        }
    }

    public void ApplyLayout(float x, float width)
    {
        _preferredWidth = Mathf.Clamp(width, MinWidth, MaxWidth);
        float clampedWidth = Mathf.Min(_preferredWidth, Mathf.Max(320, GetViewportRect().Size.X - 16));
        Size = new Vector2(clampedWidth, Size.Y);

        float maxX = GetViewportRect().Size.X - Size.X;
        Position = new Vector2(Mathf.Clamp(x, 0.0f, Mathf.Max(maxX, 0.0f)), LockToBottom ? _fixedBottomY : Position.Y);
        UpdateRightAnchoredLayout();
    }

    public void SetBookUnreadCount(int unreadCount)
    {
        string badgeText = EventLogPresentationRules.BuildUnreadBadgeText(unreadCount);
        bool isVisible = !string.IsNullOrEmpty(badgeText);
        _bookUnreadBadge.Visible = isVisible;
        _bookUnreadBadgeLabel.Text = badgeText;
        UpdateBookUnreadBadgeLayout();
    }

    private static void Place(Control control, float x, float y, float width, float height)
    {
        control.SetAnchorsPreset(LayoutPreset.TopLeft);
        control.Position = new Vector2(x, y); control.Size = new Vector2(width, height);
    }

    private void UpdateRightAnchoredLayout()
    {
        float width = Size.X;
        float selectorY = Size.Y - 62;
        float progressY = Size.Y - 30;
        float bookX = width - 98;
        Place(_dragHandle, 8, 6, 32, 24);
        Place(_realmStageLabel, 8, selectorY, 116, 28);
        float modeWidth = Mathf.Max(160, _actionModeOptionButton?.GetCombinedMinimumSize().X ?? 160);
        if (_actionModeOptionButton != null) Place(_actionModeOptionButton, 132, selectorY, modeWidth, 28);
        float targetX = 140 + modeWidth;
        float workWidth = _workButton == null ? 0 : Mathf.Max(110, _workButton.GetCombinedMinimumSize().X);
        float targetWidth = Mathf.Max(100, bookX - workWidth - 24 - targetX);
        if (_levelOptionButton != null) Place(_levelOptionButton, targetX, selectorY, targetWidth, 28);
        if (_workButton != null) Place(_workButton, bookX - workWidth - 12, selectorY, workWidth, 28);
        Place(_bookButton, bookX, selectorY, 42, 28);
        Place(_resizeHandle, width - 48, selectorY, 36, 28);
        float breakthroughWidth = Mathf.Max(78, _breakthroughButton.GetCombinedMinimumSize().X);
        float cultivationWidth = Mathf.Max(120, (width - breakthroughWidth - 120) * .44f);
        Place(_cultivationCaption, 8, progressY, 36, 24);
        Place(_cultivationProgressBar, 48, progressY + 3, cultivationWidth, 18);
        float breakthroughX = 56 + cultivationWidth;
        Place(_breakthroughButton, breakthroughX, progressY - 1, breakthroughWidth, 26);
        float exploreX = breakthroughX + breakthroughWidth + 48;
        Place(_exploreCaption, exploreX - 36, progressY, 32, 24);
        Place(_exploreProgressBar, exploreX, progressY + 3, Mathf.Max(80, width - exploreX - 12), 18);
        Place(_battleTrack, 48, 6, _expanded ? width - 60 : Mathf.Max(200, width - 376), _expanded ? 138 : 26);
        _battleTrack.ClipContents = true;
        Place(_cultivationLabel, width - 312, 7, 300, 22);
        _cultivationLabel.Visible = false;
        Place(_gainPanel, width - 312, 6, 300, 24);
        _gainPanel.Visible = !_expanded;
        _activityRateLabel.Visible = false;
        _zoneLabel.Visible = false;
        _validationPanel.Visible = false;
        UpdateBookUnreadBadgeLayout();
    }

    private void ApplyCompactTheme()
    {
        Theme theme = new() { DefaultFontSize = 14 };
        foreach (string type in new[] { "Button", "OptionButton" })
        {
            foreach (var state in new[] { ("normal", "5B4838"), ("hover", "73583D"), ("pressed", "80603A"), ("disabled", "826B57") })
            {
                StyleBoxFlat style = new() { BgColor = new Color(state.Item2), ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 2, ContentMarginBottom = 2 };
                style.SetCornerRadiusAll(4);
                theme.SetStylebox(state.Item1, type, style);
            }
            theme.SetColor("font_color", type, new Color("FFF6E2"));
            theme.SetColor("font_disabled_color", type, new Color("DACAB2"));
        }
        foreach (var state in new[] { ("background", "674936"), ("fill", "BA9462") })
            theme.SetStylebox(state.Item1, "ProgressBar", new StyleBoxFlat { BgColor = new Color(state.Item2) });
        Theme = theme;
    }

    private float GetBottomLockedY()
    {
        return GetViewportRect().Size.Y - Size.Y - _bottomMargin;
    }

    private void EnsureBookUnreadBadge()
    {
        _bookUnreadBadge = new PanelContainer();
        _bookUnreadBadge.Name = "BookUnreadBadge";
        _bookUnreadBadge.Visible = false;
        _bookUnreadBadge.MouseFilter = MouseFilterEnum.Ignore;
        _bookUnreadBadge.ZIndex = 10;

        StyleBoxFlat badgeStyle = new();
        badgeStyle.BgColor = new Color("B85450");
        badgeStyle.BorderColor = new Color("F5E6D3");
        badgeStyle.SetBorderWidthAll(1);
        badgeStyle.SetCornerRadiusAll(10);
        badgeStyle.ContentMarginLeft = 4;
        badgeStyle.ContentMarginRight = 4;
        badgeStyle.ContentMarginTop = 1;
        badgeStyle.ContentMarginBottom = 1;
        _bookUnreadBadge.AddThemeStyleboxOverride("panel", badgeStyle);

        _bookUnreadBadgeLabel = new Label();
        _bookUnreadBadgeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _bookUnreadBadgeLabel.VerticalAlignment = VerticalAlignment.Center;
        _bookUnreadBadgeLabel.AddThemeColorOverride("font_color", Colors.White);
        _bookUnreadBadgeLabel.AddThemeFontSizeOverride("font_size", 11);
        _bookUnreadBadgeLabel.MouseFilter = MouseFilterEnum.Ignore;
        _bookUnreadBadge.AddChild(_bookUnreadBadgeLabel);

        GetNode<Control>("Chrome").AddChild(_bookUnreadBadge);
    }

    private void UpdateBookUnreadBadgeLayout()
    {
        if (_bookUnreadBadge == null)
        {
            return;
        }

        Vector2 badgeSize = _bookUnreadBadgeLabel.Text == "9+" ? new Vector2(24.0f, 18.0f) : new Vector2(18.0f, 18.0f);
        _bookUnreadBadge.CustomMinimumSize = badgeSize;
        _bookUnreadBadge.Size = badgeSize;
        _bookUnreadBadge.Position = new Vector2(
            _bookButton.Position.X + _bookButton.Size.X - badgeSize.X * 0.55f,
            _bookButton.Position.Y - badgeSize.Y * 0.35f);
    }
}
