using Godot;

public partial class MainBarLayoutController : Control
{
    private const float DefaultMinWidth = 800.0f;
    private const float DefaultMaxWidth = 1500.0f;
    private const float RightMargin = 12.0f;
    private const float OptionRowOffsetY = 10.0f;
    private const float TextRowOffsetY = 34.0f;
    private const float BarRowOffsetY = 22.0f;
    private const float LeftPanelPadding = 8.0f;
    private const float PanelGap = 20.0f;
    private const float ControlGap = 10.0f;
    private const float BreakthroughRowOffsetY = -2.0f;
    private const float MinBattleTrackWidth = 220.0f;
    private const float MaxBattleTrackWidth = 432.0f;
    private const float MinExploreBarWidth = 140.0f;
    private const float DefaultExploreBarWidth = 228.0f;
    private const float MinCultivationBarWidth = 150.0f;
    private const float DefaultCultivationBarWidth = 240.0f;
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

        _dragHandle.GuiInput += OnDragHandleGuiInput;
        _resizeHandle.GuiInput += OnResizeHandleGuiInput;
        _bookButton.Pressed += () => EmitSignal(SignalName.BookButtonPressed);
        _dragHandle.Text = UiText.DragHandle;
        _resizeHandle.Text = UiText.ResizeHandle;
        _bookButton.Text = UiText.BookButton;
        _zoneLabel.Visible = false;

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

    private void UpdateRightAnchoredLayout()
    {
        float optionRowY = _battleTrack.Position.Y + _battleTrack.Size.Y + OptionRowOffsetY;
        float textRowY = optionRowY + TextRowOffsetY;
        float barRowY = textRowY + BarRowOffsetY;
        float availableContentWidth = Mathf.Max(0.0f, Size.X - _battleTrack.Position.X - RightMargin);
        float breakthroughWidth = _breakthroughButton.Size.X > 0.0f ? _breakthroughButton.Size.X : (_breakthroughButton.CustomMinimumSize.X > 0.0f ? _breakthroughButton.CustomMinimumSize.X : 78.0f);
        float minBarsWidth = MinExploreBarWidth + MinCultivationBarWidth;
        float defaultBarsWidth = DefaultExploreBarWidth + DefaultCultivationBarWidth;
        float barsWidthBudget = Mathf.Clamp(
            availableContentWidth - MinBattleTrackWidth - PanelGap - breakthroughWidth - (ControlGap * 2.0f),
            minBarsWidth,
            defaultBarsWidth);
        float exploreWidth = Mathf.Clamp(barsWidthBudget * (DefaultExploreBarWidth / defaultBarsWidth), MinExploreBarWidth, DefaultExploreBarWidth);
        float cultivationWidth = Mathf.Max(MinCultivationBarWidth, barsWidthBudget - exploreWidth);
        float battleTrackWidth = Mathf.Clamp(
            availableContentWidth - PanelGap - breakthroughWidth - (ControlGap * 2.0f) - exploreWidth - cultivationWidth,
            MinBattleTrackWidth,
            MaxBattleTrackWidth);

        float exploreX = Size.X - RightMargin - exploreWidth;
        float breakthroughX = exploreX - ControlGap - breakthroughWidth;
        float cultivationX = breakthroughX - ControlGap - cultivationWidth;

        _battleTrack.Size = new Vector2(battleTrackWidth, _battleTrack.Size.Y);
        _zoneLabel.Size = new Vector2(exploreWidth, _zoneLabel.Size.Y);
        _zoneLabel.Position = new Vector2(exploreX, textRowY);
        _exploreProgressBar.Size = new Vector2(exploreWidth, _exploreProgressBar.Size.Y);
        _exploreProgressBar.Position = new Vector2(exploreX, barRowY);
        _realmStageLabel.Position = new Vector2(_battleTrack.Position.X + LeftPanelPadding, textRowY);
        _activityRateLabel.Visible = false;

        _breakthroughButton.Position = new Vector2(breakthroughX, barRowY + BreakthroughRowOffsetY);
        _cultivationProgressBar.Size = new Vector2(cultivationWidth, _cultivationProgressBar.Size.Y);
        _cultivationProgressBar.Position = new Vector2(cultivationX, barRowY);
        _cultivationLabel.Size = new Vector2(cultivationWidth, _cultivationLabel.Size.Y);
        _cultivationLabel.Position = new Vector2(cultivationX, textRowY);

        float optionStartX = _battleTrack.Position.X + LeftPanelPadding;
        float optionUsableWidth = Mathf.Max(0.0f, _battleTrack.Size.X - (LeftPanelPadding * 2.0f));
        float actionButtonWidth = _actionModeOptionButton?.Size.X ?? 130.0f;
        float levelButtonWidth = _levelOptionButton?.Size.X ?? 220.0f;
        float totalDefaultOptionWidth = actionButtonWidth + levelButtonWidth + ControlGap;
        if (totalDefaultOptionWidth > optionUsableWidth && optionUsableWidth > 0.0f)
        {
            float shrinkableWidth = Mathf.Max(0.0f, optionUsableWidth - ControlGap);
            actionButtonWidth = Mathf.Max(MinOptionButtonWidth, shrinkableWidth * 0.38f);
            levelButtonWidth = Mathf.Max(MinOptionButtonWidth, shrinkableWidth - actionButtonWidth);
        }

        if (_actionModeOptionButton != null)
        {
            _actionModeOptionButton.Size = new Vector2(actionButtonWidth, _actionModeOptionButton.Size.Y);
            _actionModeOptionButton.Position = new Vector2(optionStartX, optionRowY);
        }

        if (_levelOptionButton != null)
        {
            float leftX = optionStartX;
            if (_actionModeOptionButton != null)
            {
                leftX += _actionModeOptionButton.Size.X + ControlGap;
            }
            _levelOptionButton.Size = new Vector2(levelButtonWidth, _levelOptionButton.Size.Y);
            _levelOptionButton.Position = new Vector2(leftX, optionRowY);
        }

        if (_validationPanel != null)
        {
            _validationPanel.Size = new Vector2(Mathf.Max(220.0f, optionUsableWidth), _validationPanel.Size.Y);
            _validationPanel.Visible = false;
        }
    }

    private float GetBottomLockedY()
    {
        return GetViewportRect().Size.Y - Size.Y - _bottomMargin;
    }
}
