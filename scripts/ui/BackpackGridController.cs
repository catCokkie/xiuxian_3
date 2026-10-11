using Godot;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;
using Xiuxian.Scripts.UI;

/// <summary>
/// Grid-based backpack display. Replaces the old RichTextLabel list.
/// Three sections: Materials, Potions, Equipment — each rendered as a grid of cells.
/// </summary>
public partial class BackpackGridController : VBoxContainer
{
    private const int CellSize = 64;
    private const int CellGap = 4;
    private const int SectionGap = 8;
    private const int DefaultColumns = 12;

    private static readonly Color MaterialColor = new("E3E5C9");
    private static readonly Color PotionColor = new("D5DFD9");
    private static readonly Color EquipCommonColor = new("DED3BD");
    private static readonly Color EquipArtifactColor = new("D2DDD4");
    private static readonly Color EquipSpiritColor = new("DED7E1");
    private static readonly Color EquipTreasureColor = new("EAD6AE");
    private static readonly Color BorderColor = new(0.45f, 0.32f, 0.20f, 1.0f);
    private static readonly Color HoverBorderColor = new(0.85f, 0.70f, 0.35f, 1.0f);
    private static readonly Color SectionLabelColor = new(0.35f, 0.22f, 0.12f, 1.0f);

    [Signal]
    public delegate void EquipmentCellClickedEventHandler(string equipmentId);

    private BackpackState? _backpackState;
    private PotionInventoryState? _potionInventoryState;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _innerContainer = null!;
    private Panel _detailPopup = null!;
    private Label _detailLabel = null!;
    private bool _detailVisible;

    // Keep references for cleanup
    private readonly List<Control> _materialCells = new();
    private readonly List<Control> _potionCells = new();
    private readonly List<Control> _equipmentCells = new();
    private GridContainer? _materialGrid;
    private GridContainer? _potionGrid;
    private GridContainer? _equipmentGrid;
    private Label? _materialHeader;
    private Label? _potionHeader;
    private Label? _equipmentHeader;
    private Label? _emptyLabel;
    private HBoxContainer? _actionRow;

    public void Initialize(BackpackState? backpackState, PotionInventoryState? potionInventoryState)
    {
        _backpackState = backpackState;
        _potionInventoryState = potionInventoryState;
    }

    public override void _Ready()
    {
        _scrollContainer = new ScrollContainer();
        _scrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scrollContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        AddChild(_scrollContainer);

        _innerContainer = new VBoxContainer();
        _innerContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _innerContainer.AddThemeConstantOverride("separation", SectionGap);
        _scrollContainer.AddChild(_innerContainer);
        Resized += UpdateGridColumns;
        _scrollContainer.Resized += UpdateGridColumns;
        VisibilityChanged += () => CallDeferred(nameof(UpdateGridColumns));

        // Detail popup (initially hidden)
        _detailPopup = new Panel();
        _detailPopup.Name = "InventoryDetailPopup";
        _detailPopup.TopLevel = true;
        _detailPopup.MouseFilter = MouseFilterEnum.Ignore;
        _detailPopup.Visible = false;
        _detailPopup.ZIndex = 10;
        StyleBoxFlat popupStyle = new();
        popupStyle.BgColor = new Color(0.12f, 0.10f, 0.08f, 0.94f);
        popupStyle.BorderWidthLeft = 1;
        popupStyle.BorderWidthTop = 1;
        popupStyle.BorderWidthRight = 1;
        popupStyle.BorderWidthBottom = 1;
        popupStyle.BorderColor = new Color(0.7f, 0.55f, 0.30f, 1.0f);
        popupStyle.CornerRadiusTopLeft = 4;
        popupStyle.CornerRadiusTopRight = 4;
        popupStyle.CornerRadiusBottomLeft = 4;
        popupStyle.CornerRadiusBottomRight = 4;
        popupStyle.ContentMarginLeft = 8;
        popupStyle.ContentMarginTop = 6;
        popupStyle.ContentMarginRight = 8;
        popupStyle.ContentMarginBottom = 6;
        _detailPopup.AddThemeStyleboxOverride("panel", popupStyle);
        AddChild(_detailPopup);

        _detailLabel = new Label();
        _detailLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.88f, 0.78f));
        _detailLabel.AddThemeFontSizeOverride("font_size", 12);
        _detailPopup.AddChild(_detailLabel);
    }

    public void Refresh()
    {
        ClearGrid();

        Dictionary<string, int> items = _backpackState?.GetItemEntries() ?? new Dictionary<string, int>();
        Dictionary<string, int> potions = _potionInventoryState?.GetPotionEntries() ?? new Dictionary<string, int>();
        EquipmentInstanceData[] equipInstances = _backpackState?.GetEquipmentInstances() ?? System.Array.Empty<EquipmentInstanceData>();
        EquipmentStatProfile[] equipProfiles = _backpackState?.GetEquipmentProfiles() ?? System.Array.Empty<EquipmentStatProfile>();

        bool hasAnything = items.Count > 0 || potions.Count > 0 || equipInstances.Length > 0 || equipProfiles.Length > 0;

        // --- Action Row ---
        _actionRow = new HBoxContainer();
        _actionRow.AddThemeConstantOverride("separation", 8);
        _innerContainer.AddChild(_actionRow);

        _actionRow.AddChild(new Label { Text = "点击装备穿戴，悬停查看属性与差值。", AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill });
        CallDeferred(nameof(UpdateGridColumns));

        if (!hasAnything)
        {
            _emptyLabel = new Label();
            _emptyLabel.Text = "背包空空如也";
            _emptyLabel.AddThemeColorOverride("font_color", SectionLabelColor);
            _innerContainer.AddChild(_emptyLabel);
            return;
        }

        // --- Materials ---
        if (items.Count > 0)
        {
            _materialHeader = CreateSectionHeader(UiText.BackpackSectionMaterials);
            _innerContainer.AddChild(_materialHeader);

            _materialGrid = CreateGridContainer();
            _innerContainer.AddChild(_materialGrid);

            foreach ((string itemId, int amount) in items)
            {
                Control cell = CreateItemCell(itemId, UiText.BackpackItemName(itemId), amount, MaterialColor);
                _materialGrid.AddChild(cell);
                _materialCells.Add(cell);
            }
        }

        // --- Potions ---
        if (potions.Count > 0)
        {
            _potionHeader = CreateSectionHeader("丹药库存");
            _innerContainer.AddChild(_potionHeader);

            _potionGrid = CreateGridContainer();
            _innerContainer.AddChild(_potionGrid);

            foreach ((string itemId, int amount) in potions)
            {
                Control cell = CreateItemCell(itemId, UiText.BackpackItemName(itemId), amount, PotionColor);
                _potionGrid.AddChild(cell);
                _potionCells.Add(cell);
            }
        }

        // --- Equipment ---
        if (equipInstances.Length > 0 || equipProfiles.Length > 0)
        {
            _equipmentHeader = CreateSectionHeader(UiText.BackpackSectionEquipment);
            _innerContainer.AddChild(_equipmentHeader);

            _equipmentGrid = CreateGridContainer();
            _innerContainer.AddChild(_equipmentGrid);

            for (int i = 0; i < equipInstances.Length; i++)
            {
                EquipmentInstanceData inst = equipInstances[i];
                Color bg = GetRarityColor(inst.RarityTier);
                Control cell = CreateEquipmentCell(GameArtCatalog.Equipment(inst.EquipmentTemplateId, inst.Slot), inst.DisplayName, bg, BuildInstanceTooltip(inst));
                string capturedId = inst.EquipmentId;
                ((Button)cell.GetChild(0)).Pressed += () => EmitSignal(SignalName.EquipmentCellClicked, capturedId);
                _equipmentGrid.AddChild(cell);
                _equipmentCells.Add(cell);
            }

            for (int i = 0; i < equipProfiles.Length; i++)
            {
                EquipmentStatProfile profile = equipProfiles[i];
                Control cell = CreateEquipmentCell(GameArtCatalog.Equipment(string.Empty, profile.Slot), profile.DisplayName, EquipCommonColor, BuildProfileTooltip(profile));
                string capturedId = profile.EquipmentId;
                ((Button)cell.GetChild(0)).Pressed += () => EmitSignal(SignalName.EquipmentCellClicked, capturedId);
                _equipmentGrid.AddChild(cell);
                _equipmentCells.Add(cell);
            }
        }
    }

    private void ClearGrid()
    {
        HideDetail();
        _materialCells.Clear();
        _potionCells.Clear();
        _equipmentCells.Clear();
        _materialGrid = null;
        _potionGrid = null;
        _equipmentGrid = null;
        _materialHeader = null;
        _potionHeader = null;
        _equipmentHeader = null;
        _emptyLabel = null;
        _actionRow = null;

        foreach (Node child in _innerContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private static GridContainer CreateGridContainer()
    {
        GridContainer grid = new();
        grid.Columns = DefaultColumns;
        grid.AddThemeConstantOverride("h_separation", CellGap);
        grid.AddThemeConstantOverride("v_separation", CellGap);
        grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return grid;
    }

    private static Label CreateSectionHeader(string text)
    {
        Label header = new();
        header.Text = text;
        header.AddThemeColorOverride("font_color", SectionLabelColor);
        header.AddThemeFontSizeOverride("font_size", 13);
        return header;
    }

    private Control CreateItemCell(string itemId, string displayName, int count, Color bgColor)
    {
        Panel cell = new();
        cell.CustomMinimumSize = new Vector2(CellSize, CellSize);

        StyleBoxFlat style = new();
        style.BgColor = bgColor;
        style.BorderWidthLeft = 1;
        style.BorderWidthTop = 1;
        style.BorderWidthRight = 1;
        style.BorderWidthBottom = 1;
        style.BorderColor = BorderColor;
        style.CornerRadiusTopLeft = 3;
        style.CornerRadiusTopRight = 3;
        style.CornerRadiusBottomLeft = 3;
        style.CornerRadiusBottomRight = 3;
        cell.AddThemeStyleboxOverride("panel", style);

        AddCellIcon(cell, GameArtCatalog.Item(itemId), 2);

        // Text and quantity stay readable even for unknown future items.
        Label nameLabel = new();
        nameLabel.Text = displayName.Length > 3 ? displayName[..3] : displayName;
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.VerticalAlignment = VerticalAlignment.Center;
        nameLabel.SetAnchorsPreset(LayoutPreset.FullRect);
        nameLabel.OffsetLeft = 2;
        nameLabel.OffsetTop = 30;
        nameLabel.OffsetRight = -2;
        nameLabel.OffsetBottom = -16;
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.AddThemeColorOverride("font_color", new Color("201B15"));
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        cell.AddChild(nameLabel);

        // Count label (bottom-right)
        Label countLabel = new();
        countLabel.Text = count > 999 ? "999+" : count.ToString();
        countLabel.HorizontalAlignment = HorizontalAlignment.Right;
        countLabel.SetAnchorsPreset(LayoutPreset.BottomRight);
        countLabel.OffsetLeft = -40;
        countLabel.OffsetTop = -18;
        countLabel.OffsetRight = -3;
        countLabel.OffsetBottom = -2;
        countLabel.AddThemeFontSizeOverride("font_size", 11);
        countLabel.AddThemeColorOverride("font_color", new Color("201B15"));
        countLabel.MouseFilter = MouseFilterEnum.Ignore;
        cell.AddChild(countLabel);

        // Hover handling for tooltip
        cell.MouseEntered += () => OnCellHover(cell, $"{displayName} x{count}");
        cell.MouseExited += HideDetail;

        return cell;
    }

    private Control CreateEquipmentCell(Texture2D? icon, string displayName, Color bgColor, string tooltip)
    {
        Panel wrapper = new();
        wrapper.CustomMinimumSize = new Vector2(CellSize, CellSize);

        // Use a Button as the clickable base to get press events
        Button btn = new();
        btn.Flat = true;
        btn.SetAnchorsPreset(LayoutPreset.FullRect);
        btn.MouseFilter = MouseFilterEnum.Pass;
        wrapper.AddChild(btn);

        StyleBoxFlat style = new();
        style.BgColor = bgColor;
        style.BorderWidthLeft = 1;
        style.BorderWidthTop = 1;
        style.BorderWidthRight = 1;
        style.BorderWidthBottom = 1;
        style.BorderColor = BorderColor;
        style.CornerRadiusTopLeft = 3;
        style.CornerRadiusTopRight = 3;
        style.CornerRadiusBottomLeft = 3;
        style.CornerRadiusBottomRight = 3;
        wrapper.AddThemeStyleboxOverride("panel", style);

        AddCellIcon(wrapper, icon, 7);

        // Equipment name (centered)
        Label nameLabel = new();
        string shortName = displayName.Length > 4 ? displayName[..4] : displayName;
        nameLabel.Text = shortName;
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.VerticalAlignment = VerticalAlignment.Center;
        nameLabel.SetAnchorsPreset(LayoutPreset.FullRect);
        nameLabel.OffsetLeft = 2;
        nameLabel.OffsetTop = 38;
        nameLabel.OffsetRight = -2;
        nameLabel.OffsetBottom = -4;
        nameLabel.AddThemeFontSizeOverride("font_size", 12);
        nameLabel.AddThemeColorOverride("font_color", new Color("201B15"));
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        wrapper.AddChild(nameLabel);

        // Hover for detail tooltip
        wrapper.MouseEntered += () => OnCellHover(wrapper, tooltip);
        wrapper.MouseExited += HideDetail;

        return wrapper;
    }

    private void OnCellHover(Control cell, string text)
    {
        _detailLabel.Text = text;
        Vector2 labelSize = _detailLabel.GetMinimumSize();
        _detailPopup.Size = labelSize + new Vector2(16, 12);

        // Position popup above the cell
        Vector2 cellGlobal = cell.GlobalPosition;
        Vector2 popupPos = new(cellGlobal.X, cellGlobal.Y - _detailPopup.Size.Y - 4);

        // Convert to local coordinates relative to this control
        Vector2 viewport = GetViewportRect().Size;
        if (popupPos.Y < 8) popupPos.Y = cellGlobal.Y + cell.Size.Y + 4;
        _detailPopup.GlobalPosition = new Vector2(Mathf.Clamp(popupPos.X, 8, Mathf.Max(8, viewport.X - _detailPopup.Size.X - 8)), Mathf.Clamp(popupPos.Y, 8, Mathf.Max(8, viewport.Y - _detailPopup.Size.Y - 8)));
        _detailPopup.Visible = true;
        _detailVisible = true;
    }

    private void HideDetail()
    {
        _detailPopup.Visible = false;
        _detailVisible = false;
    }

    /// <summary>
    /// Call from _Process or after resize to adapt columns.
    /// </summary>
    public void UpdateGridColumns()
    {
        if (_scrollContainer == null || !IsVisibleInTree()) return;
        float available = _scrollContainer.Size.X - _scrollContainer.GetVScrollBar().Size.X - 8;
        int cols = Mathf.Max(1, (int)((available + CellGap) / (CellSize + CellGap)));
        if (_materialGrid != null) _materialGrid.Columns = cols;
        if (_potionGrid != null) _potionGrid.Columns = cols;
        if (_equipmentGrid != null) _equipmentGrid.Columns = cols;
    }

    private void AddEquipButton(HBoxContainer row, string text, EquipmentSlotType slot)
    {
        Button btn = new();
        btn.Text = text;
        btn.AddThemeFontSizeOverride("font_size", 12);
        btn.Pressed += () =>
        {
            OnEquipSlotPressed(slot);
        };
        row.AddChild(btn);
    }

    private void OnEquipSlotPressed(EquipmentSlotType slot)
    {
        // Delegate to parent — the BookTabsController handles actual equip logic
        EmitSignal(SignalName.EquipmentCellClicked, $"__equip_slot__{slot}");
    }

    private static void AddCellIcon(Control cell, Texture2D? texture, float top)
    {
        var icon = new TextureRect
        {
            Name = "ItemIcon", Position = new Vector2(18, top),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
            Texture = texture,
        };
        cell.AddChild(icon);
        icon.Size = new Vector2(28, 28);
        // A texture change invalidates minimum size on the next layout pass.
        icon.SetDeferred(Control.PropertyName.Size, new Vector2(28, 28));
    }

    private static Color GetRarityColor(EquipmentRarityTier rarity)
    {
        return rarity switch
        {
            EquipmentRarityTier.CommonTool => EquipCommonColor,
            EquipmentRarityTier.Artifact => EquipArtifactColor,
            EquipmentRarityTier.Spirit => EquipSpiritColor,
            EquipmentRarityTier.Treasure => EquipTreasureColor,
            _ => EquipCommonColor
        };
    }

    private string BuildInstanceTooltip(EquipmentInstanceData inst)
    {
        string rarity = UiText.EquipmentRarityLabel(inst.RarityTier);
        string source = UiText.EquipmentSourceLabel(inst.SourceStage);
        string slot = UiText.SlotLabel(inst.Slot);
        string main = EquipmentPresentationRules.BuildSingleStatLine(inst.MainStatKey, inst.MainStatValue);
        string sub = EquipmentPresentationRules.BuildSubStatSummary(inst.SubStats);
        string comparison = EquipmentPresentationRules.BuildComparisonHint(inst, ServiceLocator.Instance?.EquippedItemsState?.GetEquippedProfiles() ?? System.Array.Empty<EquipmentStatProfile>());
        return $"[{slot}] {inst.DisplayName} +{inst.EnhanceLevel}\n{rarity} | {source}\n主属性: {main}\n副属性: {sub}\n对比当前装备: {comparison}";
    }

    private static string BuildProfileTooltip(EquipmentStatProfile profile)
    {
        string slot = UiText.SlotLabel(profile.Slot);
        string mod = EquipmentPresentationRules.BuildModifierSummary(profile.Modifier);
        return $"[{slot}] {profile.DisplayName}\n旧版装备\n属性: {mod}";
    }
}
