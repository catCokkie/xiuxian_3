using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests;

// Dedicated native UI regression scene; --smoke-test isolates saves and system input hooks.
public partial class LayoutSmokeTest : Node
{
    private int _checks;
    private PrototypeRootController _root = null!;
    private BookTabsController _tabs = null!;
    private SubmenuWindowController _book = null!;
    private MainBarLayoutController _bar = null!;
    private static readonly string[] Pages = { "CultivationTab", "BattleLogTab", "EquipmentTab", "BackpackTab", "ShopTab", "StatsTab", "ValidationTab" };
    private static readonly string[] PageRoots = { "CultivationScroll", "EquipmentRoot", "BackpackRoot", "ShopRoot", "ValidationRoot", "BugFeedbackRoot", "SettingsRoot", "LeftContentLabel" };

    public override void _Ready()
    {
        if (!OS.GetCmdlineUserArgs().Contains("--smoke-test")) { GetTree().Quit(2); return; }
        _root = GD.Load<PackedScene>("res://scenes/PrototypeRoot.tscn").Instantiate<PrototypeRootController>();
        AddChild(_root);
        _book = _root.GetNode<SubmenuWindowController>("SubmenuBookWindow");
        _tabs = _book.GetNode<BookTabsController>("BookFrame");
        _bar = _root.GetNode<MainBarLayoutController>("MainBarWindow");
        CallDeferred(nameof(RunChecks));
    }

    private void Check(bool success, string label)
    {
        if (!success) throw new InvalidOperationException(label);
        _checks++;
        GD.Print($"LAYOUT PASS: {label}");
    }

    private async Task Settle()
    {
        for (int i = 0; i < 20; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private Control PageRoot(string name) => _tabs.GetNode<Control>("SpreadBody/LeftPage/" + name);

    private async Task Select(string page)
    {
        string group = Pages.Contains(page) ? "LeftTabs" : "RightTabs";
        _tabs.GetNode<Button>($"TopStrip/{group}/{page}").EmitSignal(Button.SignalName.Pressed);
        await Settle();
        Check(_tabs.ActivePageName == page && PageRoots.Count(x => PageRoot(x).IsVisibleInTree()) == 1, $"{page}: exactly one content page");
        var navigation = _tabs.GetNode<Control>("TopStrip");
        var buttons = navigation.FindChildren("*", "Button", true, false).Cast<Button>();
        Check(buttons.Count(x => x.IsVisibleInTree() && x.ButtonPressed) == 1, $"{page}: exactly one selected navigation item");
    }

    private static bool Inside(Rect2 outer, Rect2 inner) => inner.Position.X >= outer.Position.X - 1 && inner.Position.Y >= outer.Position.Y - 1 && inner.End.X <= outer.End.X + 1 && inner.End.Y <= outer.End.Y + 1;

    private void CheckHud(string scenario)
    {
        var chrome = _bar.GetNode<Control>("Chrome");
        string[] names = { "RealmStageLabel", "ActionModeOptionButton", "LevelOptionButton", "WorkModeButton", "BookButton", "ResizeHandleButton", "CultivationProgressBar", "BreakthroughButton", "ExploreProgressBar" };
        var controls = names.Select(name => chrome.GetNode<Control>(name)).ToArray();
        Check(Inside(GetViewport().GetVisibleRect(), _bar.GetGlobalRect()), scenario + ": HUD fits viewport");
        Check(controls.All(x => Inside(_bar.GetGlobalRect(), x.GetGlobalRect())), scenario + ": HUD controls fit panel");
        Check(!controls.SelectMany((a, i) => controls.Skip(i + 1).Select(b => a.GetGlobalRect().Intersects(b.GetGlobalRect()))).Any(x => x), scenario + ": HUD controls do not overlap");
    }

    private async Task Capture(string label)
    {
        if (!OS.GetCmdlineUserArgs().Contains("--render-smoke")) return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var img = GetViewport().GetTexture().GetImage();
        string directory = "res://.godot/layout_redesign/";
        DirAccess.MakeDirRecursiveAbsolute(directory);
        Check(img.SavePng(directory + label + ".png") == Error.Ok, label + ": rendered screenshot saved");
    }

    private async void RunChecks()
    {
        try
        {
            await Settle();
            _book.SetVisibleImmediate(true);
            await Settle();
            if (OS.GetCmdlineUserArgs().Contains("--feedback-layout"))
            {
                var feedbackServices = ServiceLocator.Instance!;
                feedbackServices.PlayerProgressState!.FromDictionary(new() { ["realm_level"] = 9, ["realm_exp"] = 700.0 });
                feedbackServices.EventLogState!.AddEvent(EventLogState.CategoryCraft, "回气丹炼制完成 ×1；这一条很长的成果记录应当只显示一行摘要并保留全文查看入口。", accentId: EventLogState.AccentHighlight);
                foreach (var size in new[] { new Vector2I(1600, 900), new Vector2I(1280, 720), new Vector2I(1024, 768), new Vector2I(800, 600) })
                { GetWindow().Size = size; await Settle(); await CheckGainPresentation(size); }
                GD.Print($"FEEDBACK LAYOUT COMPLETE: {_checks} checks"); GetTree().Quit(0); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--hunt-retry-layout"))
            {
                ServiceLocator.Instance!.PlayerProgressState!.FromDictionary(new() { ["realm_level"] = 5 });
                foreach (var size in new[] { new Vector2I(1600, 900), new Vector2I(1280, 720), new Vector2I(1024, 768), new Vector2I(800, 600) })
                { GetWindow().Size = size; await Settle(); await CheckHuntRetryPresentation(size); }
                GD.Print($"HUNT RETRY LAYOUT COMPLETE: {_checks} checks"); GetTree().Quit(0); return;
            }
            Check(!PageRoot("CultivationScroll").FindChild("MasteryDetails", true, false).Get("visible").AsBool(), "new player: detailed systems collapsed");
            Check(((OptionButton)_tabs.FindChild("HomeActivityPicker", true, false)).ItemCount == ProgressiveUnlockRules.GetUnlockedActionModes(1).Count, "new player: activity picker respects unlocks");
            await Capture("home_new");
            var s = ServiceLocator.Instance!;
            s.PlayerProgressState!.FromDictionary(new() { ["realm_level"] = 9, ["realm_exp"] = 700.0 });
            s.ResourceWalletState!.AddLingqi(1200); s.ResourceWalletState.AddSpiritStones(320);
            foreach (string id in new[] { "spirit_herb", "spirit_flower", "spirit_fruit", "cold_iron_ore", "seed_spirit_herb", "spirit_jade", "mithril", "spirit_fish", "spirit_pearl", "dragon_saliva", "spirit_ink", "beast_bone", "page_fragment" }) s.BackpackState!.AddItem(id, 15);
            s.BackpackState!.AddEquipmentInstance(s.LevelConfigLoader!.BuildOfflineEquipmentFactory("lv_qi_001")!.Invoke(1));
            _tabs.FromSystemSettingsDictionary(new() { ["unlocked_equipment_tab"] = true, ["unlocked_backpack_tab"] = true, ["show_validation_panel"] = true });
            foreach (string page in Pages)
            {
                await Select(page);
                await Select("SettingsTab");
                Check(!PageRoot("CultivationScroll").IsVisibleInTree(), page + ": settings hides cultivation");
                await Select("BugTab");
            }
            await Select("BackpackTab");
            var grid = PageRoot("BackpackRoot").FindChildren("*", "GridContainer", true, false).Cast<GridContainer>().First();
            Check(grid.Columns > 1 && grid.GetGlobalRect().Position.Y < _book.GetGlobalRect().End.Y, "first filled backpack: visible multi-column grid");
            int columns = grid.Columns;
            var firstCell = grid.GetChild<Control>(0);
            Check(firstCell.GetNode<TextureRect>("ItemIcon").Texture?.ResourcePath.StartsWith("res://assets/items/") == true, "backpack: real item icon displayed");
            var itemIcon = firstCell.GetNode<TextureRect>("ItemIcon");
            Check(itemIcon.Size == new Vector2(28, 28), $"backpack: icon bounds stay 28px ({itemIcon.Size}, minimum {itemIcon.GetCombinedMinimumSize()})");
            firstCell.EmitSignal(Control.SignalName.MouseEntered); await Settle();
            var popup = (Control)_tabs.FindChild("InventoryDetailPopup", true, false);
            Check(popup.Visible && Inside(GetViewport().GetVisibleRect(), popup.GetGlobalRect()), "backpack hover: tooltip stays in viewport after layout");
            await Capture("backpack_tooltip");
            firstCell.EmitSignal(Control.SignalName.MouseExited);
            await Capture("backpack");
            await Select("CultivationTab"); await Select("BackpackTab");
            grid = PageRoot("BackpackRoot").FindChildren("*", "GridContainer", true, false).Cast<GridContainer>().First();
            Check(grid.Columns == columns, "backpack: first open and reopen use same columns");
            await Select("CultivationTab");
            foreach (string mode in ProgressiveUnlockRules.GetAllActionModesInUnlockOrder())
            {
                s.PlayerActionState!.SetAction(mode);
                await Settle();
                var rows = _tabs.FindChildren("HomeAction_*", "HFlowContainer", true, false).Cast<Control>().ToArray();
                Check(rows.Count(x => x.IsVisibleInTree()) == 1 && rows.Single(x => x.IsVisibleInTree()).Name == "HomeAction_" + mode, mode + ": only relevant actions shown");
                Check(!_root.GetNode<Control>("MainBarWindow/Chrome/BattleTrack/EnemySlotTexture").Visible, mode + ": compact stage hides enemies");
                var production = (Control)_tabs.FindChild("HomeProduction", true, false);
                Check(production.IsVisibleInTree() == CraftingProductionPlan.Supports(mode), mode + ": production controls appear only for processing activities");
                await Capture("activity_" + mode);
            }
            s.PlayerActionState!.SetAction("alchemy"); s.AlchemyState!.SelectRecipe("recipe_huiqi_dan");
            await Settle();
            var productionPanel = (Control)_tabs.FindChild("HomeProduction", true, false);
            var productionMode = productionPanel.GetNode<OptionButton>("ProductionActions/ProductionMode");
            var productionRounds = productionPanel.GetNode<SpinBox>("ProductionActions/ProductionRounds");
            productionRounds.GetLineEdit().Text = "5";
            productionPanel.GetNode<Button>("ProductionActions/ProductionStart").EmitSignal(Button.SignalName.Pressed);
            await Settle();
            Check(s.AlchemyState.Production.RoundLimit == 5 && productionPanel.GetNode<Label>("ProductionStatus").Text.Contains("0/5"), "production: custom round count and status agree");
            await Capture("production_rounds");
            productionMode.Select(1); productionMode.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
            productionPanel.GetNode<Button>("ProductionActions/ProductionStart").EmitSignal(Button.SignalName.Pressed);
            await Settle();
            Check(s.AlchemyState.Production.Continuous && !productionRounds.Visible, "production: continuous mode hides finite round input");
            await Capture("production_continuous");
            foreach (Vector2I dimensions in new[] { new Vector2I(1600, 900), new Vector2I(1280, 720), new Vector2I(1024, 768), new Vector2I(800, 600) })
            {
                GetWindow().Size = dimensions;
                await Settle();
                string scenario = dimensions.ToString();
                Check(Inside(GetViewport().GetVisibleRect(), _book.GetGlobalRect()), scenario + ": book fits viewport");
                await CheckGainPresentation(dimensions);
                await CheckHuntRetryPresentation(dimensions);
                var basicGrowth = (Label)_tabs.FindChild("HomeBasicGrowth", true, false);
                Check(basicGrowth.Text.Contains("线上线下共用") && basicGrowth.Size.X <= basicGrowth.GetParent<Control>().Size.X,
                    scenario + ": quiet growth explanation wraps within the existing home card");
                s.PlayerActionState!.SetAction("mining"); s.MiningState!.SetRecycling(true); await Select("CultivationTab");
                Check(productionPanel.IsVisibleInTree() && productionPanel.GetNode<Label>("ProductionStatus").Text.Contains("每轮固定1"), scenario + ": recycling shows fixed yield and shared production controls");
                foreach (Control control in ((Control)_tabs.FindChild("HomeAction_mining", true, false)).GetChildren())
                    if (control.Visible) Check(Inside(_book.GetGlobalRect(), control.GetGlobalRect()), scenario + ": mining operation picker fits book");
                if (dimensions.X == 800) await Capture("small_recycling");
                s.MiningState.SetRecycling(false);
                s.PlayerActionState!.SetAction("alchemy"); await Select("CultivationTab");
                foreach (Control control in productionPanel.GetNode<Control>("ProductionActions").GetChildren())
                    if (control.Visible) Check(Inside(productionPanel.GetGlobalRect(), control.GetGlobalRect()), scenario + ": production action fits its wrapping row");
                if (dimensions.X == 800)
                {
                    PageRoot("CultivationScroll").GetNode<Control>("CultivationRoot").GetParent<ScrollContainer>().ScrollVertical = 220;
                    await Settle(); await Capture("small_production");
                    ((ScrollContainer)PageRoot("CultivationScroll")).ScrollVertical = 0;
                }
                var inventoryBefore = s.BackpackState!.ToDictionary(); var walletBefore = s.ResourceWalletState!.ToDictionary();
                s.BackpackState.FromDictionary(new()); s.BackpackState.AddItem("spirit_herb", 1); s.BackpackState.SetReservedCount("spirit_herb", 1);
                s.ResourceWalletState.FromDictionary(new() { ["lingqi"] = 0.0 }); await Select("CultivationTab");
                var blockToggle = productionPanel.GetNode<Button>("ProductionRecovery/ProductionDetailsToggle");
                blockToggle.ButtonPressed = true; await Settle();
                var blockDetails = productionPanel.GetNode<Label>("ProductionDetails");
                Check(productionPanel.GetNode<Label>("ProductionStatus").Text.Contains("材料不足") && blockDetails.Text.Contains("灵气不足") && blockDetails.Text.Contains("保留1 / 可用0"),
                    scenario + ": exact primary and secondary blockers render with owned reserved and spendable counts");
                Check(blockDetails.IsVisibleInTree() && blockDetails.Size.X <= productionPanel.Size.X, scenario + ": multiline production conditions wrap inside the panel");
                foreach (Control control in productionPanel.GetNode<Control>("ProductionRecovery").GetChildren())
                    if (control.Visible) Check(Inside(productionPanel.GetGlobalRect(), control.GetGlobalRect()), scenario + ": production recovery button fits wrapping row");
                if (dimensions.X == 800)
                {
                    var scroll = (ScrollContainer)PageRoot("CultivationScroll");
                    scroll.ScrollVertical = (int)(productionPanel.GetGlobalRect().Position.Y - scroll.GetGlobalRect().Position.Y + scroll.ScrollVertical - 20);
                    await Settle(); await Capture("small_production_blockers"); scroll.ScrollVertical = 0;
                }
                blockToggle.ButtonPressed = false;
                s.BackpackState.FromDictionary(inventoryBefore); s.ResourceWalletState.FromDictionary(walletBefore); await Select("CultivationTab");
                var equipmentBefore = s.EquippedItemsState!.ToDictionary(); var smithingBefore = s.SmithingState!.ToDictionary();
                var weapon = s.EquippedItemsState.GetEquippedProfiles().First();
                s.EquippedItemsState.Equip(weapon with { EnhanceLevel = 3, Rarity = 1 }); s.SmithingState.SelectTarget(weapon.EquipmentId, 3); s.SmithingState.Production.Start(true, 1);
                await Select("EquipmentTab");
                var forge = (Control)_tabs.FindChild("EquipmentProduction", true, false);
                var forgeToggle = forge.GetNode<Button>("ProductionRecovery/ProductionDetailsToggle"); forgeToggle.ButtonPressed = true; await Settle();
                Check(forge.GetNode<Label>("ProductionStatus").Text.Contains("强化已达当前上限") && forge.GetNode<Button>("ProductionRecovery/ProductionHelp").Text == "比较其他装备",
                    scenario + ": equipment rarity cap and its real recovery option are visible");
                var equipmentScroll = PageRoot("EquipmentRoot").GetNode<ScrollContainer>("EquipmentScroll");
                Check(Inside(PageRoot("EquipmentRoot").GetGlobalRect(), equipmentScroll.GetGlobalRect()), scenario + ": expanded equipment production stays inside scrollable page");
                foreach (Control control in forge.GetNode<Control>("ProductionRecovery").GetChildren())
                    if (control.Visible) Check(Inside(forge.GetGlobalRect(), control.GetGlobalRect()), scenario + ": equipment recovery control fits wrapping row");
                if (dimensions.X == 800) { equipmentScroll.EnsureControlVisible(forge); await Settle(); await Capture("small_production_cap"); }
                s.EquippedItemsState.Equip(weapon with { EnhanceLevel = 0, Rarity = 3 }); s.SmithingState.SelectTarget(weapon.EquipmentId, 0);
                s.BackpackState.FromDictionary(new()); s.ResourceWalletState.FromDictionary(new() { ["lingqi"] = 0.0 }); await Select("EquipmentTab");
                Check(forge.GetNode<Label>("ProductionDetails").Text.Contains("灵气不足") && forge.GetNode<Label>("ProductionDetails").Text.Contains("碎片"), scenario + ": expanded smithing conditions show all material and qi blockers");
                Check(forge.GetNode<Label>("ProductionDetails").Size.X <= forge.Size.X, scenario + ": detailed smithing shortages wrap inside scroll content");
                if (dimensions.X == 800) { equipmentScroll.EnsureControlVisible(forge); await Settle(); await Capture("small_production_smithing"); }
                forgeToggle.ButtonPressed = false; equipmentScroll.ScrollVertical = 0;
                s.EquippedItemsState.Equip(weapon with { EnhanceLevel = 6, Rarity = 3 });
                var inheritTarget = weapon with { EquipmentId = "layout_inherit", DisplayName = "用于检查窄窗口长装备名显示的寒铁长枪", EnhanceLevel = 1, Rarity = 1, IsEquipped = false };
                s.BackpackState.AddEquipment(inheritTarget); await Select("EquipmentTab");
                var inheritToggle = (Button)_tabs.FindChild("InheritanceToggle", true, false);
                var inheritPanel = (Control)_tabs.FindChild("InheritancePanel", true, false);
                Check(!inheritPanel.Visible && !inheritToggle.ButtonPressed, scenario + ": inheritance stays collapsed until requested");
                inheritToggle.ButtonPressed = true;
                foreach (var pair in new[] { ("InheritanceSource", weapon.EquipmentId), ("InheritanceTarget", inheritTarget.EquipmentId) })
                {
                    var picker = (OptionButton)_tabs.FindChild(pair.Item1, true, false);
                    for (int i = 0; i < picker.ItemCount; i++)
                        if (picker.GetItemMetadata(i).AsString() == pair.Item2) { picker.Select(i); picker.EmitSignal(OptionButton.SignalName.ItemSelected, (long)i); break; }
                }
                await Settle();
                var inheritPreview = (Label)_tabs.FindChild("InheritancePreview", true, false);
                Check(inheritPreview.Text.Contains("83 灵气") && inheritPreview.Text.Contains("将损失") && inheritPreview.Text.Contains("气血"), scenario + ": inheritance preview includes exact capped result cost and total attributes");
                foreach (Control control in inheritPanel.GetChildren())
                    if (control.Visible) Check(control.Size.X <= inheritPanel.Size.X + 1, scenario + ": long inheritance controls wrap within page width");
                Check(equipmentScroll.GetHScrollBar().MaxValue <= equipmentScroll.Size.X + 1, scenario + ": expanded inheritance needs no horizontal scrolling");
                if (dimensions.X == 800)
                {
                    equipmentScroll.EnsureControlVisible(inheritPreview); await Settle(); await Capture("small_inheritance_preview");
                    equipmentScroll.EnsureControlVisible((Control)_tabs.FindChild("InheritanceConfirm", true, false)); await Settle(); await Capture("small_inheritance_confirm");
                }
                inheritToggle.ButtonPressed = false; equipmentScroll.ScrollVertical = 0;
                s.EquippedItemsState.FromDictionary(equipmentBefore); s.SmithingState.FromDictionary(smithingBefore);
                s.BackpackState.FromDictionary(inventoryBefore); s.ResourceWalletState.FromDictionary(walletBefore);
                s.PlayerActionState!.SetAction("cooking"); await Select("CultivationTab");
                var mealUsage = (Control)_tabs.FindChild("MealUsage", true, false);
                var mealMode = (OptionButton)_tabs.FindChild("MealMode", true, false);
                mealMode.Select(1); mealMode.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
                ((SpinBox)_tabs.FindChild("MealPortions", true, false)).Value = 5;
                await Settle();
                Check(mealUsage.IsVisibleInTree(), scenario + ": cooking exposes the separate meal usage budget");
                foreach (Control control in mealUsage.GetNode<Control>("MealUsageActions").GetChildren())
                    if (control.Visible) Check(Inside(mealUsage.GetGlobalRect(), control.GetGlobalRect()), scenario + ": meal control fits wrapping row");
                if (dimensions.X == 800)
                {
                    var scroll = (ScrollContainer)PageRoot("CultivationScroll");
                    scroll.ScrollVertical = (int)(mealUsage.GetGlobalRect().Position.Y - scroll.GetGlobalRect().Position.Y + scroll.ScrollVertical - 36);
                    await Settle(); await Capture("small_meal_budget"); scroll.ScrollVertical = 0;
                }
                await Select("BackpackTab");
                var reserveToggle = (Button)_tabs.FindChild("ReserveToggle", true, false);
                Check(!reserveToggle.ButtonPressed, scenario + ": material reserve starts collapsed");
                reserveToggle.ButtonPressed = true; await Settle();
                var reserveRoot = (Control)_tabs.FindChild("MaterialReserve", true, false);
                Check(reserveRoot.IsVisibleInTree() && Inside(PageRoot("BackpackRoot").GetGlobalRect(), reserveRoot.GetGlobalRect()), scenario + ": expanded reserve fits backpack page");
                foreach (Control control in reserveRoot.GetNode<Control>("ReserveActions").GetChildren())
                    Check(Inside(reserveRoot.GetGlobalRect(), control.GetGlobalRect()), scenario + ": reserve action fits wrapping row");
                var backpackGrid = (Control)_tabs.FindChild("BackpackGrid", true, false);
                Check(backpackGrid.GetGlobalRect().Position.Y >= reserveRoot.GetGlobalRect().End.Y && backpackGrid.Size.Y >= 80,
                    scenario + ": reserve editor leaves visible space for backpack grid");
                if (dimensions.X == 800) await Capture("small_material_reserve");
                reserveToggle.ButtonPressed = false; await Settle();
                s.SubsystemMasteryState!.FromDictionary(new() { ["alchemy"] = 3, ["formation"] = 3 });
                s.ResourceWalletState!.AddInsight(1000); await Select("CultivationTab");
                var masteryToggle = (Button)_tabs.FindChild("MasteryToggle", true, false); masteryToggle.ButtonPressed = true; await Settle();
                var alchemyUpgrade = (Button)_tabs.FindChild("MasteryUpgrade_alchemy", true, false);
                var formationUpgrade = (Button)_tabs.FindChild("MasteryUpgrade_formation", true, false);
                var masteryDetails = (Control)_tabs.FindChild("MasteryDetails", true, false);
                Check(alchemyUpgrade.Disabled && alchemyUpgrade.Text.Contains("尚未开放"), scenario + ": alchemy reserved upgrade visibly disabled");
                Check(formationUpgrade.TooltipText.Contains("工巧阵盘") && !formationUpgrade.TooltipText.Contains("自修复"), scenario + ": formation explains its available recipe");
                Check(Inside(masteryDetails.GetGlobalRect(), alchemyUpgrade.GetGlobalRect()) && Inside(masteryDetails.GetGlobalRect(), formationUpgrade.GetGlobalRect()),
                    scenario + ": mastery availability labels fit the wrapping panel");
                if (dimensions.X == 800)
                {
                    var scroll = (ScrollContainer)PageRoot("CultivationScroll");
                    scroll.ScrollVertical = (int)(alchemyUpgrade.GetGlobalRect().Position.Y - scroll.GetGlobalRect().Position.Y + scroll.ScrollVertical - 40);
                    await Settle(); await Capture("small_mastery_availability");
                    s.SubsystemMasteryState.FromDictionary(new() { ["alchemy"] = 4, ["formation"] = 4 }); _tabs.RestoreActiveTabs("CultivationTab", "BugTab"); await Settle();
                    Check(alchemyUpgrade.Text.Contains("旧档保留") && alchemyUpgrade.Disabled, "legacy mastery: retained level has no purchase action");
                    scroll.ScrollVertical = (int)(alchemyUpgrade.GetGlobalRect().Position.Y - scroll.GetGlobalRect().Position.Y + scroll.ScrollVertical - 40);
                    await Settle(); await Capture("small_mastery_legacy"); scroll.ScrollVertical = 0;
                }
                masteryToggle.ButtonPressed = false; await Settle();
                var goalState = s.ChallengeGoalState!;
                var loader = s.LevelConfigLoader!;
                goalState.TryStart(loader.ActiveLevelId, loader.GetBossMonsterId(), new System.Collections.Generic.Dictionary<string, int> { ["recipe_huiqi_dan"] = 99, ["talisman_fire_charm"] = 99, ["talisman_shield_charm"] = 99 });
                await Settle();
                var goalCard = (Control)_tabs.FindChild("ChallengeGoalCard", true, false);
                var goalSummary = (Label)_tabs.FindChild("ChallengeGoalSummary", true, false);
                Check(goalState.Goal.HasGoal && goalSummary.Text.Contains("追踪目标"), scenario + ": pinned goal exposes live preparation purpose");
                foreach (Control control in goalCard.GetNode<Control>("ChallengeGoalActions").GetChildren())
                    if (control.Visible) Check(Inside(goalCard.GetGlobalRect(), control.GetGlobalRect()), scenario + ": goal action fits wrapping card");
                Check(goalSummary.Size.X <= goalCard.Size.X && goalSummary.Size.Y > 0, scenario + ": long goal summary wraps inside its card");
                if (dimensions.X == 800)
                {
                    var scroll = (ScrollContainer)PageRoot("CultivationScroll");
                    scroll.ScrollVertical = (int)(goalCard.GetGlobalRect().Position.Y - scroll.GetGlobalRect().Position.Y + scroll.ScrollVertical - 20);
                    await Settle(); await Capture("small_goal_pending");
                    ((Button)_tabs.FindChild("ChallengeGoalSkip", true, false)).EmitSignal(Button.SignalName.Pressed); await Settle();
                    Check(goalSummary.Text.Contains("跳过"), "goal: skip preparation keeps challenge optional");
                    await Capture("small_goal_skipped");
                    goalState.RecordBossVictory(loader.ActiveLevelId, loader.GetBossMonsterId()); await Settle();
                    Check(goalSummary.Text.Contains("目标已完成"), "goal: completion record renders a distinct state");
                    await Capture("small_goal_complete"); scroll.ScrollVertical = 0;
                }
                goalState.Cancel(); await Settle();
                var reviewBefore = goalState.ToDictionary();
                var reviewAttempt = new BossChallengeAttempt(); reviewAttempt.Begin(loader.ActiveLevelId, loader.GetBossMonsterId());
                reviewAttempt.Observe(new(120, 45, 3)); reviewAttempt.RecordPotion("potion_huiqi_dan", 12); reviewAttempt.RecordTalisman("talisman_fire_charm", 10);
                var report = reviewAttempt.Finish("death", 12, 88, 240, 1); goalState.RecordBossReport(report); await Settle();
                var reviewToggle = (Button)_tabs.FindChild("BossReviewToggle", true, false);
                Check(reviewToggle.Visible && !reviewToggle.ButtonPressed, scenario + ": Boss review starts collapsed and remains optional");
                reviewToggle.ButtonPressed = true; await Settle();
                var reviewDetails = (Control)_tabs.FindChild("BossReviewDetails", true, false);
                var reviewSummary = (Label)_tabs.FindChild("BossReviewSummary", true, false);
                Check(reviewSummary.Text.Contains("敌方实际回血45") && reviewSummary.Text.Contains("调整2") && reviewSummary.Text.Contains("镇灵符阻断前5回合"),
                    scenario + ": Boss review shows actual recovery and at most two honest adjustment directions");
                Check(reviewSummary.Size.X <= reviewDetails.Size.X && reviewSummary.IsVisibleInTree(), scenario + ": long Boss review wraps inside the scrollable home");
                foreach (Control control in reviewDetails.GetNode<Control>("BossReviewActions").GetChildren())
                    if (control.Visible) Check(Inside(reviewDetails.GetGlobalRect(), control.GetGlobalRect()), scenario + ": Boss review recovery action fits wrapping row");
                var reviewHeadline = (Label)_tabs.FindChild("BossReviewHeadline", true, false);
                Check(reviewHeadline.Size.X <= goalCard.Size.X, scenario + ": Boss review headline wraps independently of its toggle button");
                if (dimensions.X == 800)
                {
                    var scroll = (ScrollContainer)PageRoot("CultivationScroll");
                    scroll.ScrollVertical = (int)(reviewHeadline.GetGlobalRect().Position.Y - scroll.GetGlobalRect().Position.Y + scroll.ScrollVertical - 10);
                    await Settle(); await Capture("small_boss_review_death");
                    goalState.RecordBossReport(report with { Result = "timeout", Rounds = 100, StatisticsComplete = false }); await Settle();
                    Check(reviewSummary.Text.Contains("统计不完整") && reviewSummary.Text.Contains("优先比较武器") && !reviewSummary.Text.Contains("调整2"), "legacy Boss review does not turn partial counters into full-fight advice");
                    await Capture("small_boss_review_legacy");
                    goalState.RecordBossReport(report with { Result = "victory", Rounds = 3, EnemyHp = 0 }); await Settle();
                    Check(reviewSummary.Text.Contains("胜利") && !reviewSummary.Text.Contains("调整1"), "victory review does not display defeat advice");
                    await Capture("small_boss_review_victory"); scroll.ScrollVertical = 0;
                }
                goalState.RecordBossReport(report with { LevelId = "historical_region", BossId = "historical_boss" }); await Settle();
                Check(reviewSummary.Text.Contains("其他目标") && !reviewSummary.Text.Contains("调整1"), scenario + ": unrelated archived Boss never supplies current-goal advice");
                reviewToggle.ButtonPressed = false; goalState.FromDictionary(reviewBefore); _tabs.RestoreActiveTabs("CultivationTab", "BugTab"); await Settle();
                foreach (string page in Pages.Append("SettingsTab").Append("BugTab"))
                {
                    await Select(page);
                    var visible = PageRoots.Select(PageRoot).Single(x => x.IsVisibleInTree());
                    Check(Inside(PageRoot("LeftContentLabel").GetParent<Control>().GetGlobalRect(), visible.GetGlobalRect()), scenario + " " + page + ": page fits paper");
                    if (dimensions.X == 800) await Capture("small_" + page);
                    if (page == "SettingsTab")
                    {
                        var settings = PageRoot("SettingsRoot");
                        var nav = settings.GetNode<Control>("SettingsNavRoot");
                        var scroll = settings.GetNode<Control>("SettingsScroll");
                        var actions = settings.GetNode<Control>("SettingsActionRoot");
                        foreach (Button section in nav.GetChildren().OfType<Button>())
                        {
                            section.EmitSignal(Button.SignalName.Pressed); await Settle();
                            var roots = settings.FindChildren("Settings*Root", "VBoxContainer", true, false).Cast<Control>().Where(x => x.Name != "SettingsActionRoot");
                            Check(roots.Count(x => x.IsVisibleInTree()) == 1, scenario + " settings " + section.Text + ": exactly one section");
                            Check(nav.GetGlobalRect().End.Y <= scroll.GetGlobalRect().Position.Y && scroll.GetGlobalRect().End.Y <= actions.GetGlobalRect().Position.Y, scenario + " settings " + section.Text + ": scroll avoids fixed actions");
                            var operationRoot = settings.FindChild("OperationStatistics", true, false) as Control;
                            if (operationRoot?.IsVisibleInTree() == true)
                            {
                                var statisticsButtons = operationRoot.FindChildren("Operation*", "Button", true, false).Cast<Control>().ToArray();
                                Check(statisticsButtons.All(x => x.Size.X <= scroll.Size.X), scenario + ": statistics actions wrap within settings scroll width");
                                Check(statisticsButtons.All(x => x.GlobalPosition.X >= scroll.GlobalPosition.X - 1 && x.GetGlobalRect().End.X <= scroll.GetGlobalRect().End.X + 1), scenario + ": statistics actions do not overflow paper horizontally");
                                if (dimensions.X == 800)
                                {
                                    ((ScrollContainer)scroll).ScrollVertical = 10000; await Settle();
                                    await Capture("small_operation_statistics");
                                    ((ScrollContainer)scroll).ScrollVertical = 0;
                                }
                            }
                            if (dimensions.X == 800) await Capture("small_settings_" + section.Text);
                        }
                    }
                }
                await Select("CultivationTab");
                s.PlayerActionState!.SetWorkMode(false); await Settle(); CheckHud(scenario);
                s.PlayerActionState.SetWorkMode(true); await Settle(); CheckHud(scenario + " work");
                Check(_bar.Size.Y <= 96, scenario + ": work mode compact");
                await Capture("window_" + dimensions.X + "x" + dimensions.Y);
            }
            GetWindow().Size = new Vector2I(1600, 900);
            foreach (float scale in new[] { 1.25f, 1.5f })
            {
                GetWindow().ContentScaleFactor = scale; await Settle();
                Check(Inside(GetViewport().GetVisibleRect(), _book.GetGlobalRect()), $"scale {scale}: book fits logical viewport");
                CheckHud($"scale {scale}");
                await Capture("scale_" + scale);
            }
            GetWindow().ContentScaleFactor = 1;
            _tabs.RestoreActiveTabs("CultivationTab", "SettingsTab");
            _tabs.RestoreActivePage("BugTab"); await Settle();
            Check(_tabs.ActivePageName == "BugTab" && PageRoot("BugFeedbackRoot").IsVisibleInTree() && !PageRoot("SettingsRoot").IsVisibleInTree(), "restore: explicit current page overrides legacy groups");
            s.PlayerActionState!.SetWorkMode(false); s.PlayerActionState.SetAction("dungeon");
            var controller = _root.GetNode<ExploreProgressController>("ExploreProgressController");
            controller.FromRuntimeDictionary(new() { ["explore_progress"] = 100f, ["battle_state"] = "boss_ready", ["player_hp"] = 0 });
            await Select("CultivationTab");
            var boss = (Button)_tabs.FindChild("HomeBossChallenge", true, false);
            Check(boss.Disabled && boss.Text == "休整中…", "home: injured BossReady exposes recovery instead of starting battle");
            Check(Inside(_book.GetGlobalRect(), boss.GetGlobalRect()), "home: recovery action fits book");
            await Capture("home_rest");
            controller.AdvanceHealthRecovery(20);
            await Select("CultivationTab");
            Check(!boss.Disabled, "home: BossReady has accessible challenge action");
            boss.EmitSignal(Button.SignalName.Pressed);
            Check(controller.ToRuntimeDictionary()["battle_state"].AsString() == "in_battle", "home challenge starts real battle");
            _book.SetVisibleImmediate(false); await Settle();
            Check(_bar.Size.Y > 96 && _root.GetNode<Control>("MainBarWindow/Chrome/BattleTrack/EnemySlotTexture").Visible, "battle: stage expands and shows target");
            var stage = _root.GetNode<Control>("MainBarWindow/Chrome/BattleTrack");
            Check(stage.GetNode<TextureRect>("PlayerSlotTexture").Texture?.ResourcePath.EndsWith("cultivator_idle_plain_v2.svg") == true, "battle: plain cultivator portrait");
            Check(stage.GetNode<TextureRect>("EnemySlotTexture").Texture?.ResourcePath.StartsWith("res://assets/characters/monsters/") == true, "battle: target uses species portrait");
            Check(Enumerable.Range(1, 5).All(i => stage.GetNode<TextureRect>($"MonsterSlot{i:00}").Texture?.ResourcePath.StartsWith("res://assets/characters/monsters/") == true), "battle: entire queue uses species portraits");
            Check(stage.GetNode<TextureRect>("PlayerSlotTexture").Size == new Vector2(80, 80), "battle: source image cannot enlarge sprite bounds");
            CheckHud("expanded battle"); await Capture("battle");
            var regionalState = s.LevelConfigLoader!.ToRuntimeDictionary();
            regionalState["unlocked_level_ids"] = new Godot.Collections.Array<string> { "lv_qi_001", "lv_qi_002", "lv_qi_003", "lv_qi_004", "lv_qi_005" };
            s.LevelConfigLoader.FromRuntimeDictionary(regionalState);
            s.LevelConfigLoader.TrySetActiveLevel("lv_qi_001");
            var regionPicker = _root.GetNode<OptionButton>("MainBarWindow/Chrome/LevelOptionButton");
            foreach (string levelId in new[] { "lv_qi_003", "lv_qi_004", "lv_qi_005" })
            {
                // Switch through the real UI handler so the track is repopulated for this region.
                int choice = Enumerable.Range(0, regionPicker.ItemCount).First(i => regionPicker.GetItemMetadata(i).AsString() == levelId);
                regionPicker.Select(choice); regionPicker.EmitSignal(OptionButton.SignalName.ItemSelected, choice);
                Check(s.LevelConfigLoader!.ActiveLevelId == levelId, levelId + ": region switches in memory via UI");
                controller.FromRuntimeDictionary(new() { ["explore_progress"] = 100f, ["battle_state"] = "boss_ready" });
                controller.AdvanceHealthRecovery(20);
                Check(controller.StartBossChallengeFromUi(), levelId + ": boss challenge starts");
                await Settle();
                string bossId = s.LevelConfigLoader.GetBossMonsterId(levelId);
                Check(stage.GetNode<TextureRect>("EnemySlotTexture").Texture?.ResourcePath.EndsWith(bossId + "_plain_v2.svg") == true, levelId + ": correct boss artwork");
                var markers = (Godot.Collections.Array<Variant>)controller.ToRuntimeDictionary()["monster_marker_states"];
                Check(Enumerable.Range(1, 5).All(i =>
                {
                    string id = ((Godot.Collections.Dictionary<string, Variant>)markers[i - 1])["monster_id"].AsString();
                    return stage.GetNode<TextureRect>($"MonsterSlot{i:00}").Texture?.ResourcePath.EndsWith(id + "_plain_v2.svg") == true;
                }), levelId + ": queue images match actual regional monster IDs");
                await Capture("battle_" + levelId);
            }
            _book.SetVisibleImmediate(true); await Settle();
            Check(_bar.Size.Y <= 96, "book: battle stage collapses to avoid covering content");
            await Capture("home_final");
            _tabs.FromSystemSettingsDictionary(new() { ["ui_scale"] = 1.0, ["unlocked_equipment_tab"] = true, ["unlocked_backpack_tab"] = true });
            GetWindow().Size = new Vector2I(1600, 900);
            s.PlayerActionState!.SetAction("dungeon"); s.PlayerActionState.SetConsumablePolicy("boss_only");
            s.SubsystemMasteryState!.FromDictionary(new() { ["alchemy"] = 3, ["talisman"] = 2 });
            s.PotionInventoryState!.FromDictionary(new()); s.BackpackState!.FromDictionary(new());
            s.LevelConfigLoader!.TrySetActiveLevel("lv_qi_005"); controller.FromRuntimeDictionary(new() { ["battle_state"] = "exploring", ["explore_progress"] = 0f });
            _tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            ((Button)_tabs.FindChild("PreparationToggle", true, false)).ButtonPressed = true;
            var preparation = (Control)_tabs.FindChild("PreparationDetails", true, false);
            ((ScrollContainer)PageRoot("CultivationScroll")).ScrollVertical = 350;
            await Settle();
            ((ScrollContainer)PageRoot("CultivationScroll")).ScrollVertical = 500; await Settle();
            Check(preparation.IsVisibleInTree() && ((Label)preparation.FindChild("PreparationSummary", true, false)).Text.Contains("当前目标"), "preparation: expanded drawer shows current Boss");
            Check(preparation.FindChildren("Preparation_*", "VBoxContainer", true, false).Count(x => ((Control)x).Visible) == 2, "preparation: armor Boss offers recovery and relevant armor-break supplies");
            await Capture("preparation_armor");
            s.LevelConfigLoader.TrySetActiveLevel("lv_qi_004"); _tabs.RestoreActiveTabs("CultivationTab", "BugTab"); await Settle();
            ((ScrollContainer)PageRoot("CultivationScroll")).ScrollVertical = 500; await Settle();
            Check(((Label)preparation.FindChild("PreparationSummary", true, false)).Text.Contains("第6回合恢复"), "preparation: healing threat explains the exact suppression window and recipe requirements");
            var suppression = (Control)preparation.FindChild("Preparation_" + RecipeUnlockRules.Zhenling, true, false);
            ((SpinBox)suppression.FindChild("StockTarget", true, false)).Value = 1;
            Check(suppression.Visible && ((Button)suppression.FindChild("ArrangeProduction", true, false)).Disabled
                && ((Label)suppression.FindChild("PreparationDetail", true, false)).Text.Contains("首通第三地区"), "preparation: locked suppression explains the regional gate instead of inviting invalid production");
            await Capture("preparation_healing");
            var suppressionRegions = s.LevelConfigLoader.ToRuntimeDictionary();
            suppressionRegions["boss_cleared_level_ids"] = new Godot.Collections.Array<Variant> { "lv_qi_001", "lv_qi_002", "lv_qi_003" }; s.LevelConfigLoader.FromRuntimeDictionary(suppressionRegions);
            _tabs.RestoreActiveTabs("CultivationTab", "BugTab"); await Settle();
            Check(!((Button)suppression.FindChild("ArrangeProduction", true, false)).Disabled, "preparation: regional clear enables the optional suppression route");
            await Capture("preparation_healing_unlocked");
            GetWindow().Size = new Vector2I(800, 600); await Settle();
            ((ScrollContainer)PageRoot("CultivationScroll")).ScrollVertical = 500; await Settle();
            foreach (var flow in preparation.FindChildren("*", "HFlowContainer", true, false).OfType<HFlowContainer>().Where(x => x.IsVisibleInTree()))
                foreach (Control control in flow.GetChildren()) if (control.IsVisibleInTree()) Check(Inside(preparation.GetGlobalRect(), control.GetGlobalRect()), "small preparation: action fits wrapping drawer");
            await Capture("small_preparation");
            var completedRegions = s.LevelConfigLoader.ToRuntimeDictionary(); var clears = new Godot.Collections.Array<Variant>();
            foreach (string id in s.LevelConfigLoader.GetLevelIds()) clears.Add(id);
            completedRegions["boss_cleared_level_ids"] = clears; s.LevelConfigLoader.FromRuntimeDictionary(completedRegions);
            s.PlayerProgressState!.FromDictionary(new() { ["realm_level"] = 10, ["realm_exp"] = 9999.0 });
            _tabs.RestoreActiveTabs("CultivationTab", "BugTab"); ((ScrollContainer)PageRoot("CultivationScroll")).ScrollVertical = 0;
            await Settle();
            Check(((Control)_tabs.FindChild("ChapterPanel", true, false)).IsVisibleInTree(), "chapter: completion card visible after five-region breakthrough");
            Check(!((Control)_tabs.FindChild("HomeRealmProgress", true, false)).Visible && _root.GetNode<Button>("MainBarWindow/Chrome/BreakthroughButton").Disabled, "chapter: no progress toward unsupported next realm");
            await Capture("small_chapter_end");
            GetWindow().Size = new Vector2I(1600, 900); await Settle(); await Capture("chapter_end");
            completedRegions["boss_cleared_level_ids"] = new Godot.Collections.Array<Variant>(); s.LevelConfigLoader.FromRuntimeDictionary(completedRegions);
            s.PlayerProgressState.FromDictionary(new() { ["realm_level"] = 12, ["realm_exp"] = 9999.0 }); _tabs.RestoreActiveTabs("CultivationTab", "BugTab"); await Settle();
            Check(((Label)_tabs.FindChild("ChapterSummary", true, false)).Text.Contains("旧档境界保留"), "chapter: legacy high realm does not invent region completion");
            await Capture("legacy_chapter");
            GD.Print($"LAYOUT COMPLETE: {_checks} checks");
            GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"LAYOUT FAILURE: {error}"); GetTree().Quit(1); }
    }
}
