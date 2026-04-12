using Godot;
using Xiuxian.Scripts.Services;

public partial class MainBarLayoutController : Control
{
    private const float DefaultMinWidth = 800.0f;
    private const float DefaultMaxWidth = 1500.0f;
    private const float RightMargin = 12.0f;
    private const float LeftPadding = 8.0f;
    private const float ControlGap = 8.0f;
    private const float ControlRowHeight = 26.0f;
    private const float ControlRowY = 172.0f;
    private const float BattleTrackTopY = 6.0f;
    private const float BattleTrackBottomY = 164.0f;
    private const float MinBattleTrackWidth = 220.0f;
    private const float MinOptionButtonWidth = 92.0f;

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
        _breakthroughButton = GetNode<Button>("Chrome/BreakthroughButton");
        _battleTrack = GetNode<Panel>("Chrome/BattleTrack");
        _validationPanel = GetNode<Panel>("Chrome/ConfigValidationPanel");
        _actionModeOptionButton = GetNodeOrNull<OptionButton>("Chrome/ActionModeOptionButton");
        _levelOptionButton = GetNodeOrNull<OptionButton>("Chrome/LevelOptionButton");
        EnsureBookUnreadBadge();

        _dragHandle.GuiInput += OnDragHandleGuiInput;
        _resizeHandle.GuiInput += OnResizeHandleGuiInput;
        _bookButton.Pressed += () => EmitSignal(SignalName.BookButtonPressed);
        _dragHandle.Text = UiText.DragHandle;
        _resizeHandle.Text = UiText.ResizeHandle;
        _bookButton.Text = UiText.BookButton;
        _zoneLabel.Visible = false;
        SetBookUnreadCount(0);

        _bottomMargin = Mathf.Max(MinBottomMargin, GetViewportRect().Size.Y - (Position.Y + Size.Y));
        _fixedBottomY = GetBottomLockedY();
        Position = new Vector2(Position.X, _fixedBottomY);
        UpdateRightAnchoredLayout();
    }

    public override void _Process(double delta)
    {
        if (!LockToBottom)
        {
            return;
        }

        float nextY = GetBottomLockedY();
        _fixedBottomY = nextY;
        if (!Mathf.IsEqualApprox(Position.Y, nextY))
        {
            Position = new Vector2(Position.X, nextY);
        }
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
            float nextWidth = Mathf.Clamp(Size.X + mouseMotion.Relative.X, MinWidth, MaxWidth);
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
        float clampedWidth = Mathf.Clamp(width, MinWidth, MaxWidth);
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

    private void UpdateRightAnchoredLayout()
    {
        float panelWidth = Size.X;
        float dragW = 44.0f;

        float trackLeft = dragW + ControlGap;
        float trackRight = panelWidth - RightMargin;
        float trackWidth = Mathf.Max(MinBattleTrackWidth, trackRight - trackLeft);
        _battleTrack.Position = new Vector2(trackLeft, BattleTrackTopY);
        _battleTrack.Size = new Vector2(trackWidth, BattleTrackBottomY - BattleTrackTopY);

        float controlX = LeftPadding;
        _realmStageLabel.Position = new Vector2(controlX, ControlRowY);
        controlX += _realmStageLabel.Size.X + ControlGap;

        float bookW = 42.0f;
        float resizeW = 36.0f;
        float rightButtonsWidth = bookW + ControlGap + resizeW + RightMargin;
        float rightButtonsStartX = panelWidth - rightButtonsWidth;

        float optionBudget = Mathf.Max(0.0f, rightButtonsStartX - controlX - ControlGap);
        float actionButtonWidth = Mathf.Min(130.0f, optionBudget * 0.35f);
        float levelButtonWidth = Mathf.Max(MinOptionButtonWidth, optionBudget - actionButtonWidth - ControlGap);
        actionButtonWidth = Mathf.Max(MinOptionButtonWidth, actionButtonWidth);

        if (_actionModeOptionButton != null)
        {
            _actionModeOptionButton.Position = new Vector2(controlX, ControlRowY);
            _actionModeOptionButton.Size = new Vector2(actionButtonWidth, ControlRowHeight);
            controlX += actionButtonWidth + ControlGap;
        }

        if (_levelOptionButton != null)
        {
            _levelOptionButton.Position = new Vector2(controlX, ControlRowY);
            _levelOptionButton.Size = new Vector2(levelButtonWidth, ControlRowHeight);
        }

        _activityRateLabel.Visible = false;
        if (_validationPanel != null)
        {
            _validationPanel.Visible = false;
        }

        _bookButton.Position = new Vector2(panelWidth - RightMargin - resizeW - ControlGap - bookW, ControlRowY);
        _bookButton.Size = new Vector2(bookW, ControlRowHeight);
        _resizeHandle.Position = new Vector2(panelWidth - RightMargin - resizeW, ControlRowY);
        _resizeHandle.Size = new Vector2(resizeW, ControlRowHeight);
        UpdateBookUnreadBadgeLayout();

        float barAreaRight = rightButtonsStartX - ControlGap;
        float breakthroughWidth = _breakthroughButton?.Size.X > 0.0f ? _breakthroughButton.Size.X : 78.0f;
        float cultivationWidth = Mathf.Clamp(barAreaRight * 0.22f, 120.0f, 240.0f);
        float exploreWidth = Mathf.Clamp(barAreaRight * 0.22f, 120.0f, 228.0f);

        float exploreX = barAreaRight - exploreWidth;
        float breakthroughX = exploreX - ControlGap - breakthroughWidth;
        float cultivationX = breakthroughX - ControlGap - cultivationWidth;

        float barRowY = ControlRowY;

        _exploreProgressBar.Position = new Vector2(exploreX, barRowY);
        _exploreProgressBar.Size = new Vector2(exploreWidth, _exploreProgressBar.Size.Y);

        if (_breakthroughButton != null)
        {
            _breakthroughButton.Position = new Vector2(breakthroughX, barRowY - 2.0f);
        }

        _cultivationProgressBar.Position = new Vector2(cultivationX, barRowY);
        _cultivationProgressBar.Size = new Vector2(cultivationWidth, _cultivationProgressBar.Size.Y);

        _cultivationLabel.Position = new Vector2(cultivationX, barRowY - 18.0f);
        _cultivationLabel.Size = new Vector2(cultivationWidth + breakthroughWidth + ControlGap + exploreWidth, _cultivationLabel.Size.Y);

        _zoneLabel.Position = new Vector2(exploreX, barRowY - 18.0f);
        _zoneLabel.Size = new Vector2(exploreWidth, _zoneLabel.Size.Y);
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
