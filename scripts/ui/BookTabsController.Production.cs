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
        public string FixedSystem = "";
        public string LastTarget = "";
        public CraftingProductionPlan? LastPlan;
        public int LastLimit = -1;
    }
    private readonly List<ProductionView> _productionViews = new();
    private OptionButton? _productionCookingRecipeOption;

    private CraftingProductionPlan? GetProductionPlan(string system) => system switch
    {
        "alchemy" => _alchemyState?.Production, "smithing" => _smithingState?.Production,
        "talisman" => _talismanState?.Production, "formation" => _formationState?.Production,
        "cooking" => ServiceLocator.Instance?.CookingState?.Production, _ => null,
    };
    private string GetProductionTarget(string system) => system switch
    {
        "alchemy" => _alchemyState?.SelectedRecipeId ?? "", "smithing" => _smithingState?.TargetEquipmentId ?? "",
        "talisman" => _talismanState?.SelectedRecipeId ?? "", "formation" => _formationState?.SelectedRecipeId ?? "",
        "cooking" => ServiceLocator.Instance?.CookingState?.SelectedRecipeId ?? "", _ => "",
    };
    private void NotifyProductionChanged(string system)
    {
        switch (system)
        {
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
        HFlowContainer row = new() { Name = "ProductionActions" }; row.AddThemeConstantOverride("h_separation", 8); row.AddThemeConstantOverride("v_separation", 8); root.AddChild(row);
        OptionButton mode = new() { Name = "ProductionMode" }; mode.AddItem("指定轮数"); mode.AddItem("一直生产"); row.AddChild(mode);
        SpinBox rounds = new() { Name = "ProductionRounds", MinValue = 1, MaxValue = CraftingProductionPlan.MaxRounds, Step = 1, Value = 1, Suffix = "轮", CustomMinimumSize = new Vector2(105, 0) }; row.AddChild(rounds);
        mode.ItemSelected += index => rounds.Visible = index == 0;
        Button start = new() { Name = "ProductionStart", Text = "开始 / 继续" }; row.AddChild(start);
        Button stop = new() { Name = "ProductionStop", Text = "停止" }; row.AddChild(stop);
        Label status = new() { Name = "ProductionStatus", AutowrapMode = TextServer.AutowrapMode.WordSmart }; root.AddChild(status);
        ProductionView view = new() { Root = root, Mode = mode, Rounds = rounds, Start = start, Stop = stop, Status = status, FixedSystem = fixedSystem };
        _productionViews.Add(view);
        start.Pressed += () =>
        {
            string system = fixedSystem == "" ? _playerActionState?.ActionId ?? "" : fixedSystem;
            if (GetProductionPlan(system) is not { } plan || GetProductionTarget(system) == "") return;
            rounds.Apply();
            plan.Start(mode.Selected == 1, (int)rounds.Value);
            NotifyProductionChanged(system);
            _playerActionState?.SetAction(system);
            RefreshDynamicTabContent(); RefreshProductionControls();
        };
        stop.Pressed += () =>
        {
            string system = fixedSystem == "" ? _playerActionState?.ActionId ?? "" : fixedSystem;
            GetProductionPlan(system)?.Stop(); NotifyProductionChanged(system);
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
            bool funded = false;
            string output = "";
            if (system == "smithing")
            {
                output = "每轮强化一次；达到强化上限时暂停。";
                funded = _equippedItemsState?.TryGetEquippedProfileById(target, out EquipmentStatProfile profile) == true
                    && SmithingRules.CanEnhance(profile, _backpackState!, _resourceWalletState!, GetSmithingMasteryLevel());
            }
            else if (ActivityRegistry.GetRecipe(target) is { } recipe)
            {
                output = "每轮基础产出：" + string.Join("、", recipe.Outputs.Select(x => $"{UiText.BackpackItemName(x.ItemId)} ×{x.Count}")) + "（额外产量加成另计）。";
                funded = system == "alchemy"
                    ? _resourceWalletState != null && _backpackState != null && AlchemyRules.CanStartRecipe(target, _resourceWalletState.Lingqi, _backpackState.GetItemEntries(), GetAlchemyMasteryLevel())
                    : CraftingProgressionService.CanAffordGenericRecipe(target, _backpackState, _resourceWalletState, GetMasteryLevel(system));
            }
            view.Status.Text = target == "" ? "先选择制作项目。" : plan.Summary + (plan.CanProduce && !funded ? " · 缺料或达到上限，保留进度" : "") + "\n" + output
                + (system == "formation" ? " 同名阵法重复持有不叠加效果。" : "");
            view.Start.TooltipText = "相同设置继续剩余轮数；修改设置或完成后开始新计划。停止保留当前批次进度；离线共用轮数。";
        }
    }
}
