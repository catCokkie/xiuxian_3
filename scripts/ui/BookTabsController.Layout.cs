using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private VBoxContainer? _settingsRoot;
    private ScrollContainer? _settingsScroll;
    private Label? _homeRealm;
    private Label? _homeResources;
    private Label? _homeActivity;
    private Label? _homeNextStep;
    private ProgressBar? _homeRealmProgress;
    private OptionButton? _homeMode;
    private VBoxContainer? _masteryDetails;
    private Button? _masteryToggle;
    private Button? _homeBossChallenge;
    private readonly Dictionary<string, HFlowContainer> _homeActions = new();
    private bool _refreshingHomeMode;

    private void OnHomeActionChanged(string action, string target, string variant) => RefreshDynamicTabContent();

    private void BuildResponsiveLayout()
    {
        ApplyBookTheme();
        Resized += ResizeBookContents;
        ResizeBookContents();
        BuildSettingsLayout();
        BuildHomeLayout();
        SetButtonText("TopStrip/LeftTabs/CultivationTab", "修炼");
        SetButtonText("TopStrip/LeftTabs/EquipmentTab", "装备");
        SetButtonText("TopStrip/LeftTabs/StatsTab", "统计");
        SetButtonText("TopStrip/LeftTabs/ValidationTab", "校验");
    }

    private static StyleBoxFlat PaperStyle(string color = "F3E8CF") => new()
    {
        BgColor = new Color(color), BorderColor = new Color("B59665"),
        BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
        CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
        ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 10, ContentMarginBottom = 10
    };

    private void ApplyBookTheme()
    {
        Theme bookTheme = new();
        bookTheme.DefaultFontSize = 14;
        bookTheme.SetColor("font_color", "Label", new Color("4A3728"));
        bookTheme.SetColor("default_color", "RichTextLabel", new Color("4A3728"));
        bookTheme.SetColor("font_color", "CheckButton", new Color("4A3728"));
        foreach (string state in new[] { "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color" })
            bookTheme.SetColor(state, "CheckButton", new Color("4A3728"));
        foreach (string type in new[] { "Button", "OptionButton" })
        {
            bookTheme.SetColor("font_color", type, new Color("FFF6E2"));
            bookTheme.SetColor("font_hover_color", type, new Color("FFF6E2"));
            bookTheme.SetColor("font_pressed_color", type, new Color("FFF6E2"));
            bookTheme.SetColor("font_disabled_color", type, new Color("786A58"));
            foreach (var state in new[] { ("normal", "5B4838"), ("hover", "73583D"), ("pressed", "80603A"), ("disabled", "D8CCB5") })
            {
                StyleBoxFlat style = PaperStyle(state.Item2);
                style.BorderWidthLeft = style.BorderWidthTop = style.BorderWidthRight = style.BorderWidthBottom = 0;
                style.ContentMarginLeft = style.ContentMarginRight = 10;
                style.ContentMarginTop = style.ContentMarginBottom = 6;
                bookTheme.SetStylebox(state.Item1, type, style);
            }
        }
        bookTheme.SetColor("font_color", "TextEdit", new Color("4A3728"));
        bookTheme.SetColor("font_placeholder_color", "TextEdit", new Color("70604B"));
        bookTheme.SetStylebox("normal", "TextEdit", PaperStyle());
        foreach (var state in new[] { ("background", "E3D5B8"), ("fill", "997342") })
        {
            StyleBoxFlat progressStyle = PaperStyle(state.Item2);
            progressStyle.ContentMarginLeft = progressStyle.ContentMarginRight = progressStyle.ContentMarginTop = progressStyle.ContentMarginBottom = 0;
            bookTheme.SetStylebox(state.Item1, "ProgressBar", progressStyle);
        }
        Theme = bookTheme;
        _leftTitleLabel.AddThemeFontSizeOverride("font_size", 18);
        _leftContentLabel.AddThemeFontSizeOverride("normal_font_size", 14);
        _coinLabel.AddThemeColorOverride("font_color", new Color("FFF6E2"));
        _coinLabel.AddThemeFontSizeOverride("font_size", 14);
    }

    private void ResizeBookContents()
    {
        if (_leftPage == null) return;
        Rect(GetNode<Control>("TopStrip"), 64, 12, -16, 48, false);
        GetNode<Control>("TopStrip/RightTabs").SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        Rect(GetNode<Control>("SpreadBody"), 16, 58, -16, -58);
        Rect(_leftPage, 12, 12, -12, -12);
        Rect(_leftTitleLabel, 16, 10, -16, 36, false);
        var bottom = GetNode<Control>("BottomBar");
        bottom.SetAnchorsPreset(LayoutPreset.BottomWide);
        bottom.OffsetLeft = 16; bottom.OffsetTop = -48; bottom.OffsetRight = -16; bottom.OffsetBottom = -12;
        var close = GetNode<Control>("CloseButton");
        close.Position = new Vector2(16, 12); close.Size = new Vector2(36, 36);
    }

    private static void Rect(Control node, float left, float top, float right, float bottom, bool full = true)
    {
        node.SetAnchorsPreset(full ? LayoutPreset.FullRect : LayoutPreset.TopWide);
        node.OffsetLeft = left; node.OffsetTop = top; node.OffsetRight = right; node.OffsetBottom = bottom;
    }

    private void BuildSettingsLayout()
    {
        _settingsRoot = new VBoxContainer { Name = "SettingsRoot", Visible = false };
        _leftPage.AddChild(_settingsRoot);
        Rect(_settingsRoot, 16, 44, -16, -16);
        _settingsRoot.AddThemeConstantOverride("separation", 12);
        _settingsNavRoot.Reparent(_settingsRoot);
        _settingsNavRoot.SetAnchorsPreset(LayoutPreset.TopLeft);
        _settingsNavRoot.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _settingsScroll = new ScrollContainer { Name = "SettingsScroll", SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _settingsRoot.AddChild(_settingsScroll);
        VBoxContainer sections = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _settingsScroll.AddChild(sections);
        foreach (var section in new[] { _settingsSystemRoot, _settingsDisplayRoot, _settingsProgressRoot, _settingsPrivacyRoot })
        {
            section.Reparent(sections);
            section.SetAnchorsPreset(LayoutPreset.TopLeft);
            section.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            section.AddThemeConstantOverride("separation", 12);
        }
        _settingsActionRoot.Reparent(_settingsRoot);
        _settingsActionRoot.SetAnchorsPreset(LayoutPreset.TopLeft);
        HFlowContainer actions = new();
        foreach (var child in _settingsActionRoot.GetChildren().OfType<Control>().ToArray()) child.Reparent(actions);
        _settingsActionRoot.AddChild(actions);
        ShowSettingsSection(_activeSettingsSection);
    }

    private VBoxContainer HomeSection(string title)
    {
        PanelContainer panel = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", PaperStyle());
        _cultivationRoot.AddChild(panel);
        VBoxContainer body = new(); body.AddThemeConstantOverride("separation", 8); panel.AddChild(body);
        Label heading = new() { Text = title }; heading.AddThemeFontSizeOverride("font_size", 16); body.AddChild(heading);
        return body;
    }

    private void BuildHomeLayout()
    {
        var oldChildren = _cultivationRoot.GetChildren().ToArray();
        var oldActionRow = (Control)_cultivationBreakthroughButton.GetParent();
        var oldGatheringRow = (Control)_cultivationGardenOption.GetParent();
        var oldMealRow = _cultivationRoot.GetChildren().OfType<HFlowContainer>().First(x => x.GetChildren().OfType<Button>().Any(b => b.Text == "用餐"));
        var oldMasteryRow = (Control)_cultivationMasteryButtons.Values.First().GetParent().GetParent();
        foreach (var child in oldChildren) _cultivationRoot.RemoveChild(child);
        _cultivationScroll!.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        Rect(_cultivationScroll, 16, 44, -16, -16);
        _cultivationRoot.SetAnchorsPreset(LayoutPreset.TopLeft);
        _cultivationRoot.AddThemeConstantOverride("separation", 12);

        VBoxContainer summary = HomeSection("境界与资源");
        HFlowContainer realmRow = new(); summary.AddChild(realmRow);
        _homeRealm = new Label(); _homeRealm.AddThemeFontSizeOverride("font_size", 18); realmRow.AddChild(_homeRealm);
        _cultivationBreakthroughButton.Reparent(realmRow);
        _homeRealmProgress = new ProgressBar { Name = "HomeRealmProgress", CustomMinimumSize = new Vector2(0, 12), ShowPercentage = false };
        summary.AddChild(_homeRealmProgress);
        _homeResources = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; summary.AddChild(_homeResources);
        summary.AddChild(_cultivationStatusLabel);

        VBoxContainer activity = HomeSection("当前活动");
        _homeMode = new OptionButton { Name = "HomeActivityPicker" }; activity.AddChild(_homeMode);
        _homeMode.ItemSelected += index => { if (!_refreshingHomeMode) { _playerActionState?.SetAction(_homeMode.GetItemMetadata((int)index).AsString()); RefreshHomeLayout(); } };
        _homeActivity = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; activity.AddChild(_homeActivity);
        foreach (string mode in ProgressiveUnlockRules.GetAllActionModesInUnlockOrder())
        {
            HFlowContainer row = new() { Name = "HomeAction_" + mode };
            row.AddThemeConstantOverride("h_separation", 8); row.AddThemeConstantOverride("v_separation", 8);
            activity.AddChild(row); _homeActions[mode] = row;
        }
        void Move(string mode, params Control[] controls) { foreach (var control in controls) control.Reparent(_homeActions[mode]); }
        Move("dungeon", _cultivationBossInsightButton);
        _homeBossChallenge = new Button { Name = "HomeBossChallenge", Text = "挑战 Boss" };
        _homeBossChallenge.Pressed += () => { _exploreProgressController?.StartBossChallengeFromUi(); RefreshCultivationPanelContent(); };
        _homeActions["dungeon"].AddChild(_homeBossChallenge);
        foreach (var control in oldActionRow.GetChildren().OfType<OptionButton>().Where(x => x != _cultivationAlchemyRecipeOption && x != _cultivationTalismanOption).ToArray()) Move("dungeon", control);
        Move("alchemy", _cultivationAlchemyRecipeOption, _cultivationAlchemyStartButton);
        Move("talisman", _cultivationTalismanOption, _cultivationTalismanStartButton);
        Move("garden", _cultivationGardenOption, _cultivationGardenPlotOption, _cultivationGardenStartButton, _cultivationGardenBoostButton);
        foreach (var breeding in oldGatheringRow.GetChildren().OfType<Button>().Where(b => b.Text == "育种研究").ToArray()) Move("garden", breeding);
        Move("mining", _cultivationMiningOption, _cultivationMiningStartButton, _cultivationMiningRefreshButton);
        Move("fishing", _cultivationFishingOption, _cultivationFishingStartButton);
        Move("formation", _cultivationFormationOption, _cultivationFormationActivateButton, _cultivationFormationSecondaryOption, _cultivationFormationSecondaryActivateButton);
        Move("body_cultivation", _cultivationBodyCultivationOption, _cultivationBodyCultivationStartButton);
        foreach (var child in oldMealRow.GetChildren().OfType<Control>().ToArray()) Move("cooking", child);
        Button equipment = new() { Text = "前往装备强化" }; equipment.Pressed += () => SetActiveLeftTab("EquipmentTab"); _homeActions["smithing"].AddChild(equipment);
        _homeActions["cultivation"].AddChild(new Label { Text = "正常键鼠操作即可积累修炼收益。", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        VBoxContainer next = HomeSection("下一步");
        _homeNextStep = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; next.AddChild(_homeNextStep);
        Button journal = new() { Text = "查看本轮动态" }; journal.Pressed += () => SetActiveLeftTab("BattleLogTab"); next.AddChild(journal);
        _masteryToggle = new Button { Name = "MasteryToggle", Text = "展开精通与详细状态", ToggleMode = true };
        _cultivationRoot.AddChild(_masteryToggle);
        _masteryDetails = new VBoxContainer { Name = "MasteryDetails", Visible = false }; _masteryDetails.AddThemeConstantOverride("separation", 12);
        _cultivationRoot.AddChild(_masteryDetails);
        _masteryToggle.Toggled += expanded => { _masteryDetails.Visible = expanded; _masteryToggle.Text = expanded ? "收起精通与详细状态" : "展开精通与详细状态"; };
        _masteryDetails.AddChild(_cultivationMasteryLabel);
        _masteryDetails.AddChild(oldMasteryRow);
        _masteryDetails.AddChild(_cultivationContentLabel);
        foreach (var child in oldChildren) if (child.GetParent() == null) child.QueueFree();
        RefreshHomeLayout();
    }

    private void RefreshHomeLayout()
    {
        if (_homeRealm == null || _playerProgressState == null) return;
        double percent = 100 * _playerProgressState.RealmExp / Math.Max(1, _playerProgressState.RealmExpRequired);
        _homeRealm.Text = UiText.RealmStage(_playerProgressState.RealmLevel, percent);
        _homeRealmProgress!.Value = Math.Clamp(percent, 0, 100);
        _homeResources!.Text = $"灵气 {_resourceWalletState?.Lingqi ?? 0:N0}    悟性 {_resourceWalletState?.Insight ?? 0:N0}    灵石 {_resourceWalletState?.SpiritStones ?? 0:N0}";
        _cultivationStatusLabel.AddThemeColorOverride("font_color", new Color("705334"));
        if (!_playerProgressState.HasRequiredBossClear) _cultivationStatusLabel.Text = "突破筑基前，需要击败当前阶段的最终 Boss。";
        string action = _playerActionState?.ActionId ?? "dungeon";
        _homeActivity!.Text = action == "dungeon" ? $"{BuildCultivationActionSummary()} · 探索 {_exploreProgressController?.ExploreProgressValue ?? 0:0}%" : BuildCultivationFocusSummary();
        _homeBossChallenge!.Disabled = _exploreProgressController?.CanStartBossChallenge != true;
        _homeBossChallenge.TooltipText = _homeBossChallenge.Disabled ? "探索达到 100% 且当前没有战斗时，可以挑战 Boss。" : "挑战当前副本的 Boss。";
        _homeNextStep!.Text = BuildHomeNextStep(action);
        _refreshingHomeMode = true;
        _homeMode!.Clear();
        foreach (string mode in ProgressiveUnlockRules.GetUnlockedActionModes(_playerProgressState.RealmLevel))
        {
            int index = _homeMode.ItemCount; _homeMode.AddItem(UiText.MasterySystemName(mode)); _homeMode.SetItemMetadata(index, mode);
            _homeMode.SetItemIcon(index, Xiuxian.Scripts.UI.GameArtCatalog.Get("ui:" + mode));
            _homeMode.AddThemeConstantOverride("icon_max_width", 20);
            if (mode == action) _homeMode.Select(index);
        }
        _refreshingHomeMode = false;
        foreach (var row in _homeActions) row.Value.Visible = row.Key == action;
    }

    private string BuildHomeNextStep(string action)
    {
        if (_playerProgressState?.CanBreakthrough == true) return "经验与通关条件已满足，点击上方“突破”提升境界。";
        if (action == "dungeon" && _exploreProgressController?.CanStartBossChallenge == true) return "探索已满，点击“挑战 Boss”完成本区；也可以先准备丹药与装备。";
        return action switch
        {
            "garden" => "选择地块与灵植，再点击“播种”；成熟后收益会记入动态。",
            "mining" => "选择矿脉后开始开采；矿脉耗尽时可以刷新。",
            "fishing" => "选择鱼塘后开始垂钓；渔获可用于灵膳或兑换。",
            "alchemy" => "选择丹方后开始炼制；材料不足时可前往背包查看库存。",
            "talisman" => "选择符箓后开始制作；产物用于战斗辅助。",
            "formation" => "选择并激活阵法，持续收益会记录在动态中。",
            "body_cultivation" => "选择淬体方案后开始；完成后永久提升对应属性。",
            "cooking" => "选择灵膳后开始烹饪，成品可在这里用餐。",
            "smithing" => "前往装备页选择已穿戴装备并开始强化。",
            "cultivation" => "保持日常键鼠操作，积累经验与灵气；满经验后回到这里突破。",
            _ => "继续推进当前副本，探索满后挑战 Boss；战斗与掉落记录保存在动态。"
        };
    }
}
