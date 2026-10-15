using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private Label? _challengeGoalSummary;
    private Button? _challengeGoalPin, _challengeGoalCancel, _challengeGoalSkip;
    private bool _refreshingGoalTargets;
    private readonly Dictionary<string, int> _syncedGoalTargets = new();
    private (string Level, string Boss) _boundGoal;
    private int _goalLoadRevision = -1;
    private ChallengeGoalState? _challengeGoalState;
    private void OnChallengeGoalChanged() => RefreshDynamicTabContent();
    private void BuildChallengeGoalUi(VBoxContainer parent)
    {
        VBoxContainer card = new() { Name = "ChallengeGoalCard", SizeFlagsHorizontal = SizeFlags.ExpandFill }; parent.AddChild(card);
        _challengeGoalSummary = new() { Name = "ChallengeGoalSummary", AutowrapMode = TextServer.AutowrapMode.WordSmart }; card.AddChild(_challengeGoalSummary);
        HFlowContainer row = new() { Name = "ChallengeGoalActions" }; row.AddThemeConstantOverride("h_separation", 8); row.AddThemeConstantOverride("v_separation", 8); card.AddChild(row);
        _challengeGoalPin = new() { Name = "ChallengeGoalPin", Text = "追踪当前Boss" }; row.AddChild(_challengeGoalPin);
        _challengeGoalCancel = new() { Name = "ChallengeGoalCancel", Text = "取消追踪", TooltipText = "仅取消目标追踪，已有生产计划与库存保持。" }; row.AddChild(_challengeGoalCancel);
        _challengeGoalSkip = new() { Name = "ChallengeGoalSkip", Text = "跳过准备", TooltipText = "将本次准备数量设为0，仍可自行挑战；已有制作计划可单独停止。" }; row.AddChild(_challengeGoalSkip);
        _challengeGoalPin.Pressed += () =>
        {
            var loader = ServiceLocator.Instance?.LevelConfigLoader; if (loader == null) return;
            var targets = new Dictionary<string, int>();
            foreach (var view in _preparationViews) { view.Target.Apply(); targets[view.Recipe] = (int)view.Target.Value; }
            _challengeGoalState?.TryStart(loader.ActiveLevelId, loader.GetBossMonsterId(), targets);
        };
        _challengeGoalCancel.Pressed += () => _challengeGoalState?.Cancel();
        _challengeGoalSkip.Pressed += () => _challengeGoalState?.SkipPreparation();
        BuildBossReviewUi(card);
    }
    private bool TryGetPreparationContext(out string levelId, out string bossId, out MonsterStatProfile boss)
    {
        var loader = ServiceLocator.Instance?.LevelConfigLoader; var goal = _challengeGoalState?.Goal;
        levelId = goal?.HasGoal == true ? goal.LevelId : loader?.ActiveLevelId ?? "";
        bossId = goal?.HasGoal == true ? goal.BossId : loader?.GetBossMonsterId(levelId) ?? "";
        boss = default;
        return loader != null && (goal?.HasGoal == true ? loader.GetUnlockedLevelIds() : loader.GetLevelIds()).Contains(levelId) && loader.GetBossMonsterId(levelId) == bossId
            && loader.TryGetMonsterStatProfile(bossId, out boss);
    }
    private IEnumerable<string> RelevantPreparationRecipes(MonsterStatProfile boss)
    {
        yield return "recipe_huiqi_dan";
        string counter = ChallengePreparationRules.ChooseTalisman(boss.StrategyTag, 4, boss.HealPerRound);
        if (counter != "") yield return counter;
    }
    private void RefreshChallengeGoalUi(bool hasBoss, string levelId, MonsterStatProfile boss)
    {
        if (_challengeGoalSummary == null) return;
        RefreshBossReviewUi();
        var loader = ServiceLocator.Instance?.LevelConfigLoader; var goal = _challengeGoalState?.Goal;
        bool tracking = goal?.HasGoal == true;
        bool completed = tracking && goal!.Completed;
        if (_homeNextStep != null) _homeNextStep.Visible = !tracking;
        _challengeGoalCancel!.Visible = tracking; _challengeGoalSkip!.Visible = tracking && !completed;
        _challengeGoalPin!.Disabled = loader == null || !loader.GetUnlockedLevelIds().Contains(loader.ActiveLevelId) || !loader.TryGetMonsterStatProfile(loader.GetBossMonsterId(), out _)
            || (tracking && !completed && goal!.LevelId == loader.ActiveLevelId && goal.BossId == loader.GetBossMonsterId());
        _challengeGoalPin.Text = !tracking ? "追踪当前Boss" : completed && goal!.LevelId == loader?.ActiveLevelId ? "再次追踪当前Boss" : "改为当前Boss";
        bool bindChanged = tracking && (_boundGoal != (goal!.LevelId, goal.BossId) || _goalLoadRevision != _challengeGoalState!.LoadRevision);
        _boundGoal = tracking ? (goal!.LevelId, goal.BossId) : ("", "");
        _goalLoadRevision = _challengeGoalState?.LoadRevision ?? -1;
        if (!tracking) _syncedGoalTargets.Clear();
        _refreshingGoalTargets = true;
        foreach (var view in _preparationViews)
        {
            if (tracking)
            {
                int count = goal!.GetTarget(view.Recipe);
                if (bindChanged || !_syncedGoalTargets.TryGetValue(view.Recipe, out int last) || last != count)
                {
                    view.Target.Value = count;
                    view.Target.GetLineEdit().Text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    _syncedGoalTargets[view.Recipe] = count;
                }
            }
            view.Target.Editable = !completed;
        }
        _refreshingGoalTargets = false;
        if (!tracking) { _challengeGoalSummary.Text = "可追踪一次Boss挑战，切活动后继续查看缺口；准备清单可跳过。"; return; }
        if (!hasBoss) { _challengeGoalSummary.Text = "已保存的目标当前不可用，可替换为当前Boss或取消追踪。"; return; }
        string name = $"{UiText.ZoneName(levelId, loader!.GetLevelName(levelId))} · {UiText.LocalizeGameNames(boss.DisplayName)}";
        if (completed) { _challengeGoalSummary.Text = $"目标已完成：{name}。已有生产计划可单独停止；可替换或再次追踪。"; return; }
        var lines = new List<string>();
        string next = "准备库存已满足或已跳过；是否挑战由你决定。";
        bool suggested = false;
        if (_playerActionState?.ConsumablePolicy == "none") next = "自动消耗已关闭，可走装备路线，或调整消耗策略。";
        else foreach (string recipeId in RelevantPreparationRecipes(boss))
        {
            var view = _preparationViews.First(x => x.Recipe == recipeId); int target = goal!.GetTarget(recipeId);
            if (target == 0) continue;
            var recipe = ActivityRegistry.GetRecipe(recipeId)!;
            int owned = recipe.SystemId == "alchemy" ? _potionInventoryState?.GetPotionCount(recipe.Outputs[0].ItemId) ?? 0 : _backpackState?.GetSpendableItemCount(recipe.Outputs[0].ItemId) ?? 0;
            lines.Add($"{recipe.DisplayName}可用{owned}/{target}");
            if (owned >= target || suggested) continue;
            suggested = true;
            if (!IsPreparationUnlocked(recipe))
                next = $"{recipe.DisplayName}尚未解锁，可查看真实门槛或跳过本项。";
            else
            {
                var recommendation = BuildRecommendation(view)!;
                var missing = recommendation.Materials.FirstOrDefault(x => x.Missing > 0);
                next = missing.Missing > 0 ? $"下一步：{UiText.BackpackItemName(missing.ItemId)}缺{missing.Missing}，展开准备查看来源。"
                    : recommendation.LingqiCost > (_resourceWalletState?.Lingqi ?? 0) ? $"下一步：制作准备品尚缺灵气{recommendation.LingqiCost - (_resourceWalletState?.Lingqi ?? 0):0}。"
                    : $"下一步：可安排{recipe.DisplayName}{recommendation.Rounds}轮，确认后再开始。";
            }
        }
        string condition = levelId != loader.ActiveLevelId ? "当前在其他区域，可从底栏切回目标区域查看真实挑战条件。"
            : _exploreProgressController?.CanStartBossChallenge == true ? "当前可主动挑战；准备数量不限制挑战。"
            : _exploreProgressController?.IsRecoveringForBattle == true ? _exploreProgressController.HealthRecoverySummary : "探索/活动尚未满足出战条件；准备库存达标不代表可立即挑战。";
        _challengeGoalSummary.Text = $"追踪目标：{name}\n{string.Join("；", lines)}{(lines.Count > 0 ? "。\n" : "")}{next}\n{condition}";
    }
}
