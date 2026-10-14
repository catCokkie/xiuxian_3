using Godot;
using System;
using System.Linq;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private VBoxContainer? _mealUsageRoot;
    private OptionButton? _mealChoice, _mealMode, _reserveChoice;
    private SpinBox? _mealPortions, _reserveCount;
    private Label? _mealUsageStatus, _reserveStatus;
    private Button? _mealUse, _mealStop;
    private string _lastPlannedMeal = "";
    private int _lastMealLimit = -1;

    private void BuildMealUsageControls(VBoxContainer parent)
    {
        _mealUsageRoot = new() { Name = "MealUsage", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        parent.AddChild(_mealUsageRoot);
        _mealUsageRoot.AddChild(new Label { Text = "灵膳使用", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        HFlowContainer row = new() { Name = "MealUsageActions" };
        row.AddThemeConstantOverride("h_separation", 8); row.AddThemeConstantOverride("v_separation", 8); _mealUsageRoot.AddChild(row);
        _mealChoice = new() { Name = "MealChoice" }; row.AddChild(_mealChoice);
        foreach (var meal in MealRules.Meals) _mealChoice.AddItem(meal.Name);
        _mealMode = new() { Name = "MealMode" }; row.AddChild(_mealMode);
        foreach (string mode in new[] { "单份", "指定份数", "一直续用" }) _mealMode.AddItem(mode);
        _mealPortions = new() { Name = "MealPortions", MinValue = 1, MaxValue = MealUsagePlan.MaxPortions, Value = 1, Step = 1,
            Suffix = "份", CustomMinimumSize = new Vector2(105, 0), Visible = false }; row.AddChild(_mealPortions);
        _mealUse = new() { Name = "MealUse", Text = "用餐" }; row.AddChild(_mealUse);
        _mealStop = new() { Name = "MealStop", Text = "停止续用", TooltipText = "仅停止后续扣物，当前一份的剩余效果继续有效。" }; row.AddChild(_mealStop);
        _mealUsageStatus = new() { Name = "MealUsageStatus", AutowrapMode = TextServer.AutowrapMode.WordSmart }; _mealUsageRoot.AddChild(_mealUsageStatus);
        _mealMode.ItemSelected += _ => _mealPortions.Visible = _mealMode.Selected == 1;
        _mealChoice.ItemSelected += _ => RefreshMealUsageControls();
        _mealUse.Pressed += () =>
        {
            _mealPortions.Apply();
            ServiceLocator.Instance?.CookingState?.TryActivateMeal(MealRules.Meals[Math.Max(0, _mealChoice.Selected)].Id,
                _mealMode.Selected == 2, _mealMode.Selected == 1 ? (int)_mealPortions.Value : 1);
            RefreshMealUsageControls();
        };
        _mealStop.Pressed += () => { ServiceLocator.Instance?.CookingState?.StopMealRenewal(); RefreshMealUsageControls(); };
    }
    private void RefreshMealUsageControls()
    {
        if (_mealUsageRoot == null || ServiceLocator.Instance?.CookingState is not { } cooking) return;
        _mealUsageRoot.Visible = _playerActionState?.ActionId == "cooking";
        if (_lastPlannedMeal != cooking.PlannedMealId)
        {
            int index = Array.FindIndex(MealRules.Meals, x => x.Id == cooking.PlannedMealId);
            if (index >= 0) _mealChoice!.Select(index);
            _lastPlannedMeal = cooking.PlannedMealId;
        }
        if (_lastMealLimit != cooking.MealUsage.PortionLimit)
        {
            int limit = cooking.MealUsage.PortionLimit;
            _mealMode!.Select(limit == 0 ? 2 : limit == 1 ? 0 : 1);
            _mealPortions!.Value = Math.Max(1, limit); _lastMealLimit = limit;
        }
        _mealPortions!.Visible = _mealMode!.Selected == 1;
        int mastery = GetMasteryLevel("cooking");
        for (int i = 0; i < MealRules.Meals.Length; i++) _mealChoice!.SetItemDisabled(i, mastery < MealRules.Meals[i].RequiredMastery);
        var selected = MealRules.Meals[Math.Max(0, _mealChoice!.Selected)];
        bool current = cooking.ActiveMealId == selected.Id && cooking.ActiveMealRemainingSeconds > 0;
        _mealUse!.Text = current ? "应用份数" : "用餐";
        _mealUse.Disabled = mastery < selected.RequiredMastery || (!current && (_backpackState?.GetSpendableItemCount(selected.Id) ?? 0) == 0);
        _mealUse.TooltipText = "份数包含当前一份。应用到当前灵膳不再扣物、不刷新时长；换餐返还旧餐的剩余时长。到期缺货后需再次用餐。仅活跃输入计时，离线不续用。";
        _mealStop!.Disabled = cooking.ActiveMealRemainingSeconds <= 0 || cooking.MealUsage.RenewalStopped;
        string active = cooking.ActiveMealRemainingSeconds > 0 ? $"{cooking.ActiveMeal.Name} · 剩余{cooking.ActiveMealRemainingSeconds / 60:0.0}活跃分钟"
            : "当前无灵膳效果，再次用餐才会扣物";
        _mealUsageStatus!.Text = $"{active}；{cooking.MealUsage.Summary}。\n所选库存{_backpackState?.GetItemCount(selected.Id) ?? 0}，保留{_backpackState?.GetReservedCount(selected.Id) ?? 0}，可用{_backpackState?.GetSpendableItemCount(selected.Id) ?? 0}。份数含当前一份，点击用餐后生效；离线不扣餐。";
    }

    private void BuildMaterialReserveControls(VBoxContainer parent)
    {
        Button toggle = new() { Name = "ReserveToggle", Text = "展开材料保留（高级）", ToggleMode = true }; parent.AddChild(toggle);
        VBoxContainer root = new() { Name = "MaterialReserve", Visible = false, SizeFlagsHorizontal = SizeFlags.ExpandFill }; parent.AddChild(root);
        toggle.Toggled += expanded => { root.Visible = expanded; toggle.Text = expanded ? "收起材料保留（高级）" : "展开材料保留（高级）"; RefreshMaterialReserveControls(); };
        HFlowContainer row = new() { Name = "ReserveActions" }; row.AddThemeConstantOverride("h_separation", 8); row.AddThemeConstantOverride("v_separation", 8); root.AddChild(row);
        _reserveChoice = new() { Name = "ReserveChoice" }; row.AddChild(_reserveChoice);
        _reserveCount = new() { Name = "ReserveCount", MinValue = 0, MaxValue = InventoryReserveRules.MaxReserve, Step = 1, Value = 0,
            CustomMinimumSize = new Vector2(110, 0) }; row.AddChild(_reserveCount);
        Button apply = new() { Name = "ReserveApply", Text = "保存保留量" }; row.AddChild(apply);
        Button clear = new() { Name = "ReserveClear", Text = "取消此项保留" }; row.AddChild(clear);
        _reserveStatus = new() { Name = "ReserveStatus", AutowrapMode = TextServer.AutowrapMode.WordSmart }; root.AddChild(_reserveStatus);
        _reserveChoice.ItemSelected += _ => { SyncReserveCount(); RefreshMaterialReserveControls(); };
        apply.Pressed += () => { _reserveCount.Apply(); _backpackState?.SetReservedCount(SelectedReserveId(), (int)_reserveCount.Value); RefreshMaterialReserveControls(); };
        clear.Pressed += () => { _backpackState?.SetReservedCount(SelectedReserveId(), 0); SyncReserveCount(); RefreshMaterialReserveControls(); };
        RefreshMaterialReserveControls(); SyncReserveCount();
    }
    private string SelectedReserveId() => _reserveChoice?.Selected >= 0 ? _reserveChoice.GetItemMetadata(_reserveChoice.Selected).AsString() : "";
    private void SyncReserveCount() { if (_reserveCount != null) _reserveCount.Value = _backpackState?.GetReservedCount(SelectedReserveId()) ?? 0; }
    private void RefreshMaterialReserveControls()
    {
        if (_reserveChoice == null || _backpackState == null) return;
        string selected = SelectedReserveId();
        // Recipe inputs and owned items automatically expose future content without maintaining another material list.
        string[] ids = MaterialRegistry.GetAll().Select(x => x.ItemId)
            .Concat(ActivityRegistry.GetAll().Values.SelectMany(x => x.GetRecipes()).SelectMany(x => x.Inputs).Select(x => x.ItemId))
            .Concat(MealRules.Meals.Select(x => x.Id)).Concat(_backpackState.GetItemEntries().Keys).Concat(_backpackState.GetReservedEntries().Keys)
            .Where(x => !x.StartsWith("__", StringComparison.Ordinal)).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
        string[] old = Enumerable.Range(0, _reserveChoice.ItemCount).Select(i => _reserveChoice.GetItemMetadata(i).AsString()).ToArray();
        if (!old.SequenceEqual(ids))
        {
            _reserveChoice.Clear();
            foreach (string id in ids) { _reserveChoice.AddItem(UiText.BackpackItemName(id)); _reserveChoice.SetItemMetadata(_reserveChoice.ItemCount - 1, id); }
            int index = Array.IndexOf(ids, selected); _reserveChoice.Select(Math.Max(0, index)); SyncReserveCount();
        }
        string idSelected = SelectedReserveId();
        _reserveStatus!.Text = $"{UiText.BackpackItemName(idSelected)}：持有{_backpackState.GetItemCount(idSelected)} / 保留{_backpackState.GetReservedCount(idSelected)} / 可用{_backpackState.GetSpendableItemCount(idSelected)}。\n默认0；所有扣物共用此下限，在线与离线相同。可设高于库存的目标；需要使用储备时先降低保留量。";
    }
    private void AddReserveLink(VBoxContainer parent)
    {
        Button link = new() { Name = "MaterialReserveLink", Text = "设置材料保留", TooltipText = "在背包的高级设置中保护共享库存。" };
        parent.AddChild(link); link.Pressed += () =>
        {
            SetActiveLeftTab("BackpackTab");
            if (_backpackRoot?.FindChild("ReserveToggle", true, false) is Button toggle) { toggle.ButtonPressed = true; }
        };
    }
}
