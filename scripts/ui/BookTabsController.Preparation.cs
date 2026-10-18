using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private VBoxContainer? _preparationDetails;
    private Label? _preparationSummary;
    private sealed record PreparationView(string Recipe, VBoxContainer Root, SpinBox Target, Label Detail, Button Arrange, Button Hunt, Button Source);
    private readonly List<PreparationView> _preparationViews = new();
    private VBoxContainer? _chapterPanel;
    private Label? _chapterSummary;

    private void BuildPreparationUi(VBoxContainer parent)
    {
        BuildChallengeGoalUi(parent);
        Button toggle = new() { Name = "PreparationToggle", Text = "查看挑战准备", ToggleMode = true }; parent.AddChild(toggle);
        _preparationDetails = new() { Name = "PreparationDetails", Visible = false }; parent.AddChild(_preparationDetails);
        _preparationSummary = new() { Name = "PreparationSummary", AutowrapMode = TextServer.AutowrapMode.WordSmart }; _preparationDetails.AddChild(_preparationSummary);
        BuildSuppressionChoice(_preparationDetails);
        toggle.Toggled += shown => { _preparationDetails.Visible = shown; toggle.Text = shown ? "收起挑战准备" : "查看挑战准备"; RefreshPreparationUi(); };
        foreach (string recipeId in ChallengeGoalPlan.Recipes)
        {
            VBoxContainer root = new() { Name = "Preparation_" + recipeId }; _preparationDetails.AddChild(root);
            HFlowContainer row = new(); row.AddThemeConstantOverride("h_separation", 8); root.AddChild(row);
            Label name = new() { Text = ActivityRegistry.GetRecipe(recipeId)!.DisplayName + "目标库存" }; row.AddChild(name);
            SpinBox target = new() { Name = "StockTarget", MinValue = 0, MaxValue = 99, Value = 1, Step = 1, Suffix = ActivityRegistry.GetRecipe(recipeId)!.SystemId == "alchemy" ? "颗" : "张", TooltipText = "0表示跳过本项制作准备；数量不作为挑战或实际消耗门槛。", CustomMinimumSize = new Vector2(105, 0) }; row.AddChild(target);
            Button arrange = new() { Name = "ArrangeProduction", Text = "安排制作" }; row.AddChild(arrange);
            Button hunt = new() { Name = "PreparationHunt", Text = "本区讨伐补料", Visible = false }; row.AddChild(hunt);
            Button source = new() { Name = "PreparationSource", Text = "补首项材料", Visible = false }; row.AddChild(source);
            Label detail = new() { Name = "PreparationDetail", AutowrapMode = TextServer.AutowrapMode.WordSmart }; root.AddChild(detail);
            var view = new PreparationView(recipeId, root, target, detail, arrange, hunt, source); _preparationViews.Add(view);
            source.Pressed += () =>
            {
                var missing = BuildRecommendation(view)?.Materials.FirstOrDefault(x => x.Missing > 0);
                if (missing?.Missing > 0) NavigateProductionMaterial(missing.Value.ItemId);
            };
            hunt.Pressed += () =>
            {
                var missing = BuildRecommendation(view)?.Materials.FirstOrDefault(x => x.Missing > 0);
                if (missing.HasValue) OpenHuntForMaterial(missing.Value.ItemId);
            };
            target.ValueChanged += value => { if (_refreshingGoalTargets) return; _challengeGoalState?.SetTarget(recipeId, (int)value); RefreshPreparationUi(); };
            arrange.Pressed += () => ArrangePreparation(view);
        }
        HFlowContainer sources = new(); _preparationDetails.AddChild(sources);
        Button dungeon = new() { Text = "前往副本找材料" }; sources.AddChild(dungeon);
        dungeon.Pressed += () => { _playerActionState?.SetAction("dungeon"); RefreshDynamicTabContent(); };
        Button garden = new() { Name = "PreparationGarden", Text = "前往灵田种灵草" }; sources.AddChild(garden);
        garden.Pressed += () => { if (ProgressiveUnlockRules.IsActionModeUnlocked("garden", _playerProgressState?.RealmLevel ?? 1)) { ServiceLocator.Instance?.GardenState?.SelectCrop("garden_spirit_herb"); _playerActionState?.SetAction("garden"); RefreshDynamicTabContent(); } };
        Button equipment = new() { Name = "PreparationEquipment", Text = "比较装备与强化" }; sources.AddChild(equipment);
        equipment.Pressed += () => SetActiveLeftTab("EquipmentTab");
    }
    private PreparationRecommendation? BuildRecommendation(PreparationView view)
    {
        var recipe = ActivityRegistry.GetRecipe(view.Recipe)!;
        int owned = recipe.SystemId == "alchemy" ? ServiceLocator.Instance?.PotionInventoryState?.GetPotionCount(recipe.Outputs[0].ItemId) ?? 0
            : _backpackState?.GetItemCount(recipe.Outputs[0].ItemId) ?? 0;
        return ChallengePreparationRules.Build(view.Recipe, (int)view.Target.Value, owned, GetMasteryLevel(recipe.SystemId),
            ServiceLocator.Instance?.ShopState?.DoubleYieldMultiplier >= 2 ? 2 : 1, ServiceLocator.Instance?.CookingState?.ActiveMeal.ExtraOutput ?? 0,
            _backpackState?.GetItemEntries() ?? new Dictionary<string, int>(), _backpackState?.GetReservedEntries());
    }
    private string MaterialSource(string itemId)
    {
        var loader = ServiceLocator.Instance?.LevelConfigLoader;
        var regions = loader?.GetUnlockedLevelIds().Where(id => loader.GetOfflineAverageItemDropsPerVictory(id).GetValueOrDefault(itemId) > 0)
            .Select(id => UiText.ZoneName(id, loader.GetLevelName(id))).ToArray() ?? Array.Empty<string>();
        string drops = regions.Length > 0 ? "副本：" + string.Join("、", regions.Take(2)) + (regions.Length > 2 ? "等" : "") : "当前已开放副本暂无该材料来源";
        if (itemId == "spirit_pearl") return "灵渔的灵泉浅滩也可产出灵珠（概率产出），无需高阶鱼塘；" + drops;
        return itemId == "spirit_herb" ? (ProgressiveUnlockRules.IsActionModeUnlocked("garden", _playerProgressState?.RealmLevel ?? 1) ? "灵田种植；" : "灵田在炼气2层开放；") + drops : drops;
    }
    private void RefreshPreparationUi()
    {
        if (_preparationSummary == null || _preparationDetails == null) return;
        var loader = ServiceLocator.Instance?.LevelConfigLoader;
        bool hasBoss = TryGetPreparationContext(out string levelId, out _, out MonsterStatProfile boss);
        RefreshChallengeGoalUi(hasBoss, levelId, boss);
        RefreshSuppressionChoice(hasBoss, boss);
        var stats = _exploreProgressController?.GetPreparationStats() ?? default;
        string threat = boss.HealPerRound > 0 ? $"每回合恢复{boss.HealPerRound}气血，可提高持续伤害，或选镇灵符/抑生丹阻断前5回合回血，第6回合恢复。两者只消耗选中的一种，无库存不替换；改变选择不改当前战斗。\n{SuppressionCostSummary(RecipeUnlockRules.Zhenling)}；{SuppressionCostSummary(RecipeUnlockRules.Yangyuan)}。制作均需炼气4层、各自精通Lv2及首通第三地区。"
            : boss.StrategyTag == "armor" ? "护甲型，可用破甲符削减防御。"
            : boss.StrategyTag == "fast" ? "迅捷型，疾风符可提供先手和回合加速。" : "按当前装备与气血准备即可。";
        bool consumes = _playerActionState?.ConsumablePolicy != "none";
        int roundLimit = _exploreProgressController?.MaxBossBattleRounds ?? GameBalanceConstants.Explore.MaxBossBattleRounds;
        int? estimate = hasBoss ? ChallengePreparationRules.EstimateBaselineRounds(stats.Attack, boss) : null;
        string outputHint = estimate.HasValue ? $"基础输出估算约{estimate}回合（不计暴击、弱点参悟和战术效果）。"
            + (estimate > roundLimit ? " 输出偏紧，可优先比较武器、强化或战术符箓。" : "") : "基础输出难以压过回血，可优先比较装备攻击。";
        _preparationSummary.Text = hasBoss ? $"当前目标：{UiText.LocalizeGameNames(boss.DisplayName)} · 攻击{boss.BaseStats.Attack} / 防御{boss.BaseStats.Defense}\n{threat}\n当前攻击{stats.Attack} / 防御{stats.Defense}，Boss上限{roundLimit}回合。可按自己的路线选择准备。\n{outputHint}"
            + (consumes ? "\n回气丹仅在低血量时使用，单战最多一颗；目标库存可修改。" : "\n自动消耗已关闭，当前不推荐消耗品备货；可先比较装备与气血。") : "当前没有可用的Boss目标。";
        string counter = PreparationCounter(boss);
        foreach (var view in _preparationViews)
        {
            var recipe = ActivityRegistry.GetRecipe(view.Recipe)!;
            bool unlocked = IsPreparationUnlocked(recipe);
            view.Hunt.Visible = false;
            view.Source.Visible = false;
            view.Root.Visible = hasBoss && consumes && (view.Recipe == "recipe_huiqi_dan" || view.Recipe == counter)
                && (unlocked || _challengeGoalState?.Goal.HasGoal == true || view.Recipe is RecipeUnlockRules.Zhenling or RecipeUnlockRules.Yangyuan);
            if (!view.Root.Visible) continue;
            if (!unlocked) { view.Arrange.Disabled = true; view.Detail.Text = RecipeUnlockRules.Requirements(recipe) + "可将数量设为0跳过准备。"; continue; }
            var recommendation = BuildRecommendation(view)!;
            var missingMaterial = recommendation.Materials.FirstOrDefault(x => x.Missing > 0);
            var source = FindProductionMaterialSource(missingMaterial.ItemId ?? "");
            view.Source.Visible = missingMaterial.Missing > 0 && source.System != "";
            view.Source.TooltipText = source.Hint;
            view.Hunt.Visible = missingMaterial.Missing > 0 && _levelConfigLoader?.HuntUnlocked == true
                && _levelConfigLoader.BestHuntForMaterial(missingMaterial.ItemId) != "";
            view.Hunt.TooltipText = "针对当前首项缺料选择本区来源；保持挑战目标，不切区、不自动制作。讨伐探索速度80%，基础目标权重×2，掉落仍受原规则限制。";
            view.Arrange.Disabled = recommendation.Rounds == 0 || _challengeGoalState?.Goal.Completed == true;
            string unit = recipe.SystemId == "alchemy" ? "颗" : "张";
            view.Detail.Text = $"持有{recommendation.Owned}{unit}，保留{recommendation.Reserved}{unit}，可用{recommendation.Spendable}{unit}；目标{recommendation.Target}{unit}，缺{recommendation.Missing}{unit}。当前每轮{recommendation.PerBatch}{unit}，建议{recommendation.Rounds}轮。"
                + (recommendation.Target == 0 ? "\n本项准备已跳过，不限制挑战。" : recommendation.Rounds == 0 ? "\n库存已满足，不必重复制作。" : $"\n需要灵气{recommendation.LingqiCost:0}（缺{Math.Max(0, recommendation.LingqiCost - (_resourceWalletState?.Lingqi ?? 0)):0}）。\n"
                    + string.Join("\n", recommendation.Materials.Where(x => x.Required > 0).Select(x => $"{UiText.BackpackItemName(x.ItemId)}：需{x.Required} / 持有{x.Owned} / 保留{x.Reserved} / 可用{x.Spendable} / 缺{x.Missing}；{MaterialSource(x.ItemId)}")))
                + (ServiceLocator.Instance?.CookingState?.ActiveMeal.ExtraOutput > 0 ? "\n产量按当前灵膳估算，效果结束后可重新计算。" : "");
        }
        _preparationDetails.FindChild("PreparationGarden", true, false).Set("disabled", !ProgressiveUnlockRules.IsActionModeUnlocked("garden", _playerProgressState?.RealmLevel ?? 1));
        _preparationDetails.FindChild("PreparationEquipment", true, false).Set("disabled", !IsLeftTabUnlocked("EquipmentTab"));
    }
    private bool IsPreparationUnlocked(IRecipeDefinition recipe) => RecipeUnlockRules.Available(recipe, _playerProgressState?.RealmLevel ?? 1,
        GetMasteryLevel(recipe.SystemId), ServiceLocator.Instance?.LevelConfigLoader?.GetBossDefeatedLevelIds());
    private void ArrangePreparation(PreparationView view)
    {
        view.Target.Apply();
        if (_challengeGoalState?.Goal.Completed == true || !TryGetPreparationContext(out _, out _, out _) || _playerActionState?.ConsumablePolicy == "none") return;
        if (!IsPreparationUnlocked(ActivityRegistry.GetRecipe(view.Recipe)!)) return;
        var recommendation = BuildRecommendation(view);
        if (recommendation == null || recommendation.Rounds == 0) return;
        string system = ActivityRegistry.GetRecipe(view.Recipe)!.SystemId;
        if (system == "alchemy") _alchemyState?.SelectRecipe(view.Recipe); else _talismanState?.SelectRecipe(view.Recipe);
        var plan = GetProductionPlan(system)!;
        plan.Configure(false, recommendation.Rounds); plan.Stop();
        NotifyProductionChanged(system);
        _playerActionState?.SetAction(system);
        RefreshDynamicTabContent();
        _cultivationScroll!.ScrollVertical = 0;
    }
    private void BuildChapterUi()
    {
        _chapterPanel = HomeSection("炼气篇回顾"); _chapterPanel.GetParent<Control>().Name = "ChapterPanel";
        _chapterSummary = new() { Name = "ChapterSummary", AutowrapMode = TextServer.AutowrapMode.WordSmart }; _chapterPanel.AddChild(_chapterSummary);
        Button equipment = new() { Text = "查看收藏装备" }; _chapterPanel.AddChild(equipment); equipment.Pressed += () => SetActiveLeftTab("BackpackTab");
    }
    private void RefreshChapterUi()
    {
        if (_chapterSummary == null || _chapterPanel == null || _playerProgressState == null) return;
        var loader = ServiceLocator.Instance?.LevelConfigLoader;
        var cleared = loader?.GetBossDefeatedLevelIds().ToHashSet() ?? new HashSet<string>();
        bool regionComplete = cleared.Contains(RealmBreakthroughRules.FinalRegionId);
        bool capped = _playerProgressState.IsAtContentLimit;
        _chapterPanel.GetParent<Control>().Visible = regionComplete || capped;
        if (!regionComplete && !capped) return;
        _chapterSummary.Text = regionComplete && capped ? "筑基初成 · 炼气篇完成\n" : regionComplete ? "炼气五区已通关；经验足够后手动突破，即可筑基初成。\n" : "旧档境界保留，可继续补齐五区首通记录。\n";
        _chapterSummary.Text += string.Join("\n", (loader?.GetLevelIds() ?? new Godot.Collections.Array<string>()).Select(id => $"{(cleared.Contains(id) ? "✓" : "○")} {UiText.ZoneName(id, loader!.GetLevelName(id))}"));
        if (capped) _chapterSummary.Text += $"\n本篇境界上限为筑基初成；后续境界尚未开放。溢出经验{_playerProgressState.RealmExp:0}保留为修行记录，后续承接规则在内容开放时说明。";
        _chapterSummary.Text += "\n可选目标：收集已有装备、探索鱼塘稀有渔获、尝试其他准备路线。区域与收藏记录持续保留。";
    }
}
