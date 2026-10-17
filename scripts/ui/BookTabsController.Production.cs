using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private sealed class ProductionView
    {
        public required VBoxContainer Root;
        public required OptionButton Mode;
        public required SpinBox Rounds;
        public required Label Status;
        public required Button Start;
        public required Button Stop;
        public required Button DetailsToggle;
        public required Label Details;
        public required Button Help;
        public required Button Reserve;
        public ProductionCheck? Check;
        public string FixedSystem = "";
        public string LastTarget = "";
        public CraftingProductionPlan? LastPlan;
        public int LastLimit = -1;
    }
    private readonly List<ProductionView> _productionViews = new();
    private OptionButton? _productionCookingRecipeOption;

    private CraftingProductionPlan? GetProductionPlan(string system) => system switch
    {
        "mining" when _miningState?.Recycling == true => _miningState.RecyclingProduction,
        "alchemy" => _alchemyState?.Production, "smithing" => _smithingState?.Production,
        "talisman" => _talismanState?.Production, "formation" => _formationState?.Production,
        "cooking" => ServiceLocator.Instance?.CookingState?.Production, _ => null,
    };
    private string GetProductionTarget(string system) => system switch
    {
        "mining" when _miningState?.Recycling == true => RecipeUnlockRules.Recycle,
        "alchemy" => _alchemyState?.SelectedRecipeId ?? "", "smithing" => _smithingState?.TargetEquipmentId ?? "",
        "talisman" => _talismanState?.SelectedRecipeId ?? "", "formation" => _formationState?.SelectedRecipeId ?? "",
        "cooking" => ServiceLocator.Instance?.CookingState?.SelectedRecipeId ?? "", _ => "",
    };
    private void NotifyProductionChanged(string system)
    {
        switch (system)
        {
            case "mining": _miningState?.NotifyProductionChanged(); break;
            case "alchemy": _alchemyState?.NotifyProductionChanged(); break;
            case "smithing": _smithingState?.NotifyProductionChanged(); break;
            case "talisman": _talismanState?.NotifyProductionChanged(); break;
            case "formation": _formationState?.NotifyProductionChanged(); break;
            case "cooking": ServiceLocator.Instance?.CookingState?.NotifyProductionChanged(); break;
        }
    }
    private void BuildProductionControls(VBoxContainer parent, string fixedSystem = "")
    {
        VBoxContainer root = new() { Name = fixedSystem == "" ? "HomeProduction" : "EquipmentProduction", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        parent.AddChild(root);
        AddReserveLink(root);
        HFlowContainer row = new() { Name = "ProductionActions" }; row.AddThemeConstantOverride("h_separation", 8); row.AddThemeConstantOverride("v_separation", 8); root.AddChild(row);
        OptionButton mode = new() { Name = "ProductionMode" }; mode.AddItem("指定轮数"); mode.AddItem("一直生产"); row.AddChild(mode);
        SpinBox rounds = new() { Name = "ProductionRounds", MinValue = 1, MaxValue = CraftingProductionPlan.MaxRounds, Step = 1, Value = 1, Suffix = "轮", CustomMinimumSize = new Vector2(105, 0) }; row.AddChild(rounds);
        mode.ItemSelected += index => rounds.Visible = index == 0;
        Button start = new() { Name = "ProductionStart", Text = "开始 / 继续" }; row.AddChild(start);
        Button stop = new() { Name = "ProductionStop", Text = "停止" }; row.AddChild(stop);
        Label status = new() { Name = "ProductionStatus", AutowrapMode = TextServer.AutowrapMode.WordSmart }; root.AddChild(status);
        HFlowContainer helpRow = new() { Name = "ProductionRecovery" }; helpRow.AddThemeConstantOverride("h_separation", 8); helpRow.AddThemeConstantOverride("v_separation", 8); root.AddChild(helpRow);
        Button toggle = new() { Name = "ProductionDetailsToggle", Text = "查看生产条件", ToggleMode = true }; helpRow.AddChild(toggle);
        Button help = new() { Name = "ProductionHelp" }; helpRow.AddChild(help);
        Button reserve = new() { Name = "ProductionReserve", Text = "修改此项保留" }; helpRow.AddChild(reserve);
        Label details = new() { Name = "ProductionDetails", Visible = false, AutowrapMode = TextServer.AutowrapMode.WordSmart }; root.AddChild(details);
        toggle.Toggled += shown => { details.Visible = shown; toggle.Text = shown ? "收起生产条件" : "查看生产条件"; };
        ProductionView view = new() { Root = root, Mode = mode, Rounds = rounds, Start = start, Stop = stop, Status = status, FixedSystem = fixedSystem,
            DetailsToggle = toggle, Details = details, Help = help, Reserve = reserve };
        _productionViews.Add(view);
        help.Pressed += () => ResolveProductionBlock(view);
        reserve.Pressed += () =>
        {
            string system = view.FixedSystem == "" ? _playerActionState?.ActionId ?? "" : view.FixedSystem;
            if (RecoveryBlock(_exploreProgressController?.GetProductionCheck(system))?.ItemId is { Length: > 0 } id) OpenMaterialReserve(id);
        };
        start.Pressed += () =>
        {
            string system = fixedSystem == "" ? _playerActionState?.ActionId ?? "" : fixedSystem;
            if (GetProductionPlan(system) is not { } plan || GetProductionTarget(system) == "") return;
            rounds.Apply();
            plan.Start(mode.Selected == 1, (int)rounds.Value);
            NotifyProductionChanged(system);
            _playerActionState?.SetAction(system);
            _operationStatistics?.RecordEvent("production_start_" + system);
            RefreshDynamicTabContent(); RefreshProductionControls();
        };
        stop.Pressed += () =>
        {
            string system = fixedSystem == "" ? _playerActionState?.ActionId ?? "" : fixedSystem;
            GetProductionPlan(system)?.Stop(); NotifyProductionChanged(system);
            _operationStatistics?.RecordEvent("production_stop_" + system);
            RefreshProductionControls();
        };
    }
    private void RefreshProductionControls()
    {
        if (_productionCookingRecipeOption is { } cookingOption)
        {
            string selected = GetProductionTarget("cooking");
            var recipes = ActivityRegistry.GetBySystem("cooking")!.GetRecipes();
            for (int i = 0; i < recipes.Count; i++)
                if (recipes[i].RecipeId == selected && cookingOption.Selected != i) cookingOption.Select(i);
        }
        foreach (var view in _productionViews)
        {
            string system = view.FixedSystem == "" ? _playerActionState?.ActionId ?? "" : view.FixedSystem;
            CraftingProductionPlan? plan = GetProductionPlan(system);
            view.Root.Visible = plan != null;
            if (plan == null) continue;
            string target = GetProductionTarget(system);
            OptionButton? picker = system switch
            {
                "alchemy" => _cultivationAlchemyRecipeOption, "smithing" => _equipmentSmithingTargetOption,
                "talisman" => _cultivationTalismanOption, "formation" => _cultivationFormationOption,
                "cooking" => _productionCookingRecipeOption, _ => null,
            };
            if (picker != null) picker.TooltipText = "切换项目会冻结原进度与轮数；切回来后点击继续。只有当前项目会推进。强化等级在其他途径发生变化时，旧批次进度会重新校验。";
            if (view.LastPlan != plan || view.LastTarget != target || view.LastLimit != plan.RoundLimit)
            {
                view.Mode.Select(plan.Continuous ? 1 : 0); view.Rounds.Value = Math.Max(1, plan.RoundLimit);
                view.LastPlan = plan; view.LastTarget = target; view.LastLimit = plan.RoundLimit;
            }
            view.Rounds.Visible = view.Mode.Selected == 0;
            view.Start.Disabled = target == "";
            view.Stop.Disabled = target == "" || !plan.CanProduce;
            view.Start.Text = plan.Finished ? "开始新计划" : plan.Paused ? "继续生产" : "开始 / 继续";
            view.Check = _exploreProgressController?.GetProductionCheck(system) ?? ProductionBlockRules.Unavailable(target);
            string output = "";
            if (system == "smithing")
            {
                output = "每轮强化一次；达到强化上限时暂停。";
            }
            else if (ActivityRegistry.GetRecipe(target) is { } recipe)
            {
                output = target == RecipeUnlockRules.Recycle ? "每轮固定1灵气碎片；不受双倍产量或灵膳增产影响，仅在线推进。" : "每轮基础产出：" + string.Join("、", recipe.Outputs.Select(x => $"{UiText.BackpackItemName(x.ItemId)} ×{x.Count}")) + "（额外产量加成另计）。";
            }
            view.Status.Text = (target == "" ? "" : plan.Summary + "\n") + ProductionBlockRules.Summary(view.Check) + "\n" + output
                + (system == "formation" ? " 同名阵法重复持有不叠加效果。" : "");
            if (view.Check.CanProduce && system != _playerActionState?.ActionId)
                view.Status.Text += "\n当前活动不推进此项目；开始/继续会切换到对应加工活动。";
            view.Details.Text = ProductionBlockRules.Details(view.Check);
            RefreshProductionRecovery(view);
            view.Start.TooltipText = "相同设置继续剩余轮数；修改设置或完成后开始新计划。停止保留当前批次进度；离线共用轮数。";
        }
    }

    private static ProductionBlock? RecoveryBlock(ProductionCheck? check) => check?.Primary?.Kind == ProductionBlockKind.Finished ? null
        : check?.Blocks.FirstOrDefault(x => x.Kind != ProductionBlockKind.Paused);
    private void RefreshProductionRecovery(ProductionView view)
    {
        var block = RecoveryBlock(view.Check);
        view.Help.Visible = block != null;
        view.Help.Disabled = false;
        view.Reserve.Visible = block?.Kind == ProductionBlockKind.Material && (_backpackState?.GetReservedCount(block.ItemId) ?? 0) > 0;
        if (block == null) return;
        view.Help.Text = block.Kind switch
        {
            ProductionBlockKind.Reserve => "修改此项保留",
            ProductionBlockKind.Material => "找该材料",
            ProductionBlockKind.Lingqi => "切到修炼补充灵气",
            ProductionBlockKind.ActivityLocked => "查看开放条件",
            ProductionBlockKind.RegionClear => "查看首通条件",
            ProductionBlockKind.Mastery => "查看研习",
            ProductionBlockKind.EnhanceCap => block.RequiredLevel > 0 ? "查看研习" : "比较其他装备",
            _ => "选择制作项目"
        };
        view.Help.TooltipText = ProductionBlockRules.Describe(view.Check!, block);
        if (block.Kind == ProductionBlockKind.Material)
        {
            var source = FindProductionMaterialSource(block.ItemId);
            view.Help.Disabled = source.System == "";
            view.Help.TooltipText = source.Hint;
            view.Details.Text += "\n" + source.Hint;
        }
    }
    private void ResolveProductionBlock(ProductionView view)
    {
        string system = view.FixedSystem == "" ? _playerActionState?.ActionId ?? "" : view.FixedSystem;
        // Recheck after inventory, mastery or target edits; a stale button must not act on the old blocker.
        view.Check = _exploreProgressController?.GetProductionCheck(system);
        var block = RecoveryBlock(view.Check); if (block == null) return;
        if (block.Kind == ProductionBlockKind.Reserve) { OpenMaterialReserve(block.ItemId); return; }
        if (block.Kind == ProductionBlockKind.Material) { NavigateProductionMaterial(block.ItemId); return; }
        if (block.Kind == ProductionBlockKind.RegionClear) { _playerActionState?.SetAction("dungeon"); SetActiveLeftTab("CultivationTab"); _cultivationStatusLabel.Text = "需首通第三地区；从底栏查看区域，不会自动切区或挑战。"; return; }
        if (block.Kind == ProductionBlockKind.Lingqi) { _playerActionState?.SetAction("cultivation"); return; }
        if (block.Kind is ProductionBlockKind.Mastery or ProductionBlockKind.ActivityLocked || block.Kind == ProductionBlockKind.EnhanceCap && block.RequiredLevel > 0)
        {
            SetActiveLeftTab("CultivationTab"); _masteryToggle!.ButtonPressed = true;
            if (_cultivationRoot.GetParent() is ScrollContainer scroll)
            {
                Control target = _cultivationMasteryButtons.GetValueOrDefault(system) ?? (Control)_masteryDetails!;
                Callable.From(() => scroll.EnsureControlVisible(target)).CallDeferred();
            }
            return;
        }
        if (system == "smithing") SetActiveLeftTab("EquipmentTab");
        else (system switch { "alchemy" => _cultivationAlchemyRecipeOption, "talisman" => _cultivationTalismanOption,
            "cooking" => _productionCookingRecipeOption, "formation" => _cultivationFormationOption, _ => null })?.GrabFocus();
    }
    private (string System, string Target, string Hint) FindProductionMaterialSource(string itemId)
    {
        int realm = _playerProgressState?.RealmLevel ?? 1;
        if (itemId == "lingqi_shard" && realm >= 3)
            return ("mining", RecipeUnlockRules.Recycle, "稳定来源：回收寒铁，每轮寒铁矿×3、灵气20、100输入→灵气碎片×1；选择后手动开始。副本也可掉落碎片。");
        if (ProgressiveUnlockRules.IsActionModeUnlocked("garden", realm))
            foreach (var crop in GardenRules.GetCrops())
                if (crop.OutputItemId == itemId && GardenRules.CanPlantCrop(crop.RecipeId, GetMasteryLevel("garden")))
                    return ("garden", crop.RecipeId, $"来源：灵田{crop.DisplayName}；选择后仍需手动播种并准备种子与灵石。");
        if (ProgressiveUnlockRules.IsActionModeUnlocked("mining", realm))
            foreach (var node in MiningRules.GetNodes())
                if (node.OutputItemId == itemId && MiningRules.CanMineNode(node.RecipeId, GetMasteryLevel("mining")))
                    return ("mining", node.RecipeId, $"来源：矿脉{node.DisplayName}；正常输入推进采集。");
        if (ProgressiveUnlockRules.IsActionModeUnlocked("fishing", realm))
            foreach (var pond in FishingRules.GetPonds())
                if (ServiceLocator.Instance?.FishingState?.CanSelectPond(pond.RecipeId) == true
                    && Enumerable.Range(0, 100).Any(roll => FishingRules.CatchFish(pond.RecipeId, GetMasteryLevel("fishing"), roll).ItemId == itemId))
                    return ("fishing", pond.RecipeId, $"来源：灵渔{pond.DisplayName}，产物随机；仍受鱼群容量限制。");
        var loader = ServiceLocator.Instance?.LevelConfigLoader;
        var regions = loader?.GetUnlockedLevelIds().Where(id => loader.GetOfflineAverageItemDropsPerVictory(id).GetValueOrDefault(itemId) > 0).ToArray() ?? Array.Empty<string>();
        if (regions.Length > 0)
            return ("dungeon", "", "副本来源：" + string.Join("、", regions.Select(id => UiText.ZoneName(id, loader!.GetLevelName(id))))
                + "；进入副本后从底栏选择来源区域，切区会重置当前探索。");
        return ("", "", "当前没有已开放且可用的来源；请查看采集精通、鱼塘许可或后续区域，不会自动采购材料。");
    }
    private void NavigateProductionMaterial(string itemId)
    {
        var source = FindProductionMaterialSource(itemId); if (source.System == "") return;
        var services = ServiceLocator.Instance;
        if (source.System == "garden") services?.GardenState?.SelectCrop(source.Target);
        else if (source.System == "mining")
        {
            if (source.Target == RecipeUnlockRules.Recycle) services?.MiningState?.SetRecycling(true);
            else services?.MiningState?.SelectNode(source.Target);
        }
        else if (source.System == "fishing" && services?.FishingState?.SelectedRecipeId != source.Target) services?.FishingState?.SelectPond(source.Target);
        _playerActionState?.SetAction(source.System); SetActiveLeftTab("CultivationTab");
        _cultivationStatusLabel.Text = source.Hint;
    }
}
