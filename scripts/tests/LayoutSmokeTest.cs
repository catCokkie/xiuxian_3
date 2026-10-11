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
                await Capture("activity_" + mode);
            }
            foreach (Vector2I dimensions in new[] { new Vector2I(1600, 900), new Vector2I(1280, 720), new Vector2I(1024, 768), new Vector2I(800, 600) })
            {
                GetWindow().Size = dimensions;
                await Settle();
                string scenario = dimensions.ToString();
                Check(Inside(GetViewport().GetVisibleRect(), _book.GetGlobalRect()), scenario + ": book fits viewport");
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
            controller.FromRuntimeDictionary(new() { ["explore_progress"] = 100f, ["battle_state"] = "boss_ready" });
            await Select("CultivationTab");
            var boss = (Button)_tabs.FindChild("HomeBossChallenge", true, false);
            Check(!boss.Disabled, "home: BossReady has accessible challenge action");
            boss.EmitSignal(Button.SignalName.Pressed);
            Check(controller.ToRuntimeDictionary()["battle_state"].AsString() == "in_battle", "home challenge starts real battle");
            _book.SetVisibleImmediate(false); await Settle();
            Check(_bar.Size.Y > 96 && _root.GetNode<Control>("MainBarWindow/Chrome/BattleTrack/EnemySlotTexture").Visible, "battle: stage expands and shows target");
            var stage = _root.GetNode<Control>("MainBarWindow/Chrome/BattleTrack");
            Check(stage.GetNode<TextureRect>("PlayerSlotTexture").Texture?.ResourcePath.EndsWith("cultivator_idle_v1.png") == true, "battle: new cultivator portrait");
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
                Check(controller.StartBossChallengeFromUi(), levelId + ": boss challenge starts");
                await Settle();
                string bossId = s.LevelConfigLoader.GetBossMonsterId(levelId);
                Check(stage.GetNode<TextureRect>("EnemySlotTexture").Texture?.ResourcePath.EndsWith(bossId + "_idle_v1.png") == true, levelId + ": correct boss artwork");
                var markers = (Godot.Collections.Array<Variant>)controller.ToRuntimeDictionary()["monster_marker_states"];
                Check(Enumerable.Range(1, 5).All(i =>
                {
                    string id = ((Godot.Collections.Dictionary<string, Variant>)markers[i - 1])["monster_id"].AsString();
                    return stage.GetNode<TextureRect>($"MonsterSlot{i:00}").Texture?.ResourcePath.EndsWith(id + "_idle_v1.png") == true;
                }), levelId + ": queue images match actual regional monster IDs");
                await Capture("battle_" + levelId);
            }
            _book.SetVisibleImmediate(true); await Settle();
            Check(_bar.Size.Y <= 96, "book: battle stage collapses to avoid covering content");
            await Capture("home_final");
            GD.Print($"LAYOUT COMPLETE: {_checks} checks");
            GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"LAYOUT FAILURE: {error}"); GetTree().Quit(1); }
    }
}
