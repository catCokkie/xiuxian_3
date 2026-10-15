using Xiuxian.Scripts.Services;
using Xiuxian.Scripts.Game;

namespace Xiuxian.Tests;

[Collection("ActivityRegistry")]
public sealed class SupplyRulesTests
{
    [Theory]
    [InlineData(3, 2, true, false)]
    [InlineData(4, 1, true, false)]
    [InlineData(4, 2, false, false)]
    [InlineData(4, 2, true, true)]
    public void ZhenlingRequiresRealmMasteryAndThirdRegion(int realm, int mastery, bool cleared, bool available)
    {
        var recipe = ActivityRegistry.GetRecipe(RecipeUnlockRules.Zhenling)!;
        Assert.Equal(available, RecipeUnlockRules.Available(recipe, realm, mastery, cleared ? new[] { "lv_qi_003" } : Array.Empty<string>()));
        Assert.Equal(70, recipe.LingqiCost); Assert.Equal(280, recipe.RequiredInputEvents);
        Assert.Equal(new[] { new MaterialCost("broken_talisman", 3), new MaterialCost("spirit_ink", 2) }, recipe.Inputs);
    }
    [Fact]
    public void RecyclingHasOneWayFixedBaseCostsAndRemainsOutsideOfflineProcessing()
    {
        var recipe = ActivityRegistry.GetRecipe(RecipeUnlockRules.Recycle)!;
        Assert.Equal(new[] { new MaterialCost("cold_iron_ore", 3) }, recipe.Inputs);
        Assert.Equal(20, recipe.LingqiCost); Assert.Equal(100, recipe.RequiredInputEvents);
        Assert.Equal(new[] { new MaterialOutput("lingqi_shard", 1) }, recipe.Outputs);
        Assert.False(RecipeUnlockRules.Available(recipe, 2, 4, null)); Assert.True(RecipeUnlockRules.Available(recipe, 3, 1, null));
        Assert.Equal(ActivityCategory.Gathering, ActivityRegistry.GetBySystem("mining")!.Category);
    }
    [Theory]
    [InlineData(0, false, true)]
    [InlineData(3, true, true)]
    [InlineData(3, false, false)]
    public void SuppressionIsChosenOnlyAgainstHealingAndAtBattleStart(int healing, bool selected, bool start)
    {
        var usages = ActivityEffectRules.DetermineAutoUseBackpackConsumables(new(100, 100, start, false, false),
            new Dictionary<string, int> { [RecipeUnlockRules.Zhenling] = 8, ["talisman_burst_charm"] = 8 }, 1, "", healing);
        Assert.Equal(selected, usages.Any(x => x.ItemId == RecipeUnlockRules.Zhenling));
        Assert.True(usages.Count <= 1);
    }
    [Fact]
    public void SuppressionCannotStackAndAddsNoPermanentCombatStats()
    {
        Assert.Equal(5, TalismanRules.GetSuppressionRounds(new[] { RecipeUnlockRules.Zhenling, RecipeUnlockRules.Zhenling }));
        Assert.Equal(default, TalismanRules.GetModifier(RecipeUnlockRules.Zhenling));
        Assert.Equal(0, TalismanRules.GetSuppressionRounds(new[] { "talisman_fire_charm" }));
    }
    [Fact]
    public void FailedSuppressionRouteDoesNotRecommendStackingTheSamePreparation()
    {
        var attempt = new BossChallengeAttempt(); attempt.Begin("zone", "boss"); attempt.RecordTalisman(RecipeUnlockRules.Zhenling, 0);
        attempt.Observe(new(20, 10, 0)); var report = attempt.Finish("timeout", 10, 90, 100, 1);
        var advice = BossChallengeReportRules.Advice(report);
        Assert.Equal(2, advice.Count); Assert.Contains("本次已使用镇灵符", advice[1]); Assert.Contains("不会延长", advice[1]);
        Assert.DoesNotContain("选择镇灵符", advice[1]);
    }
    [Fact]
    public void LegacyGoalDoesNotGainMandatoryNewPreparation()
    {
        var goal = new ChallengeGoalPlan(); goal.Restore(new Dictionary<string, object> { ["level_id"] = "lv_qi_004", ["boss_id"] = "boss", ["stock_targets"] = new Dictionary<string, object> { ["recipe_huiqi_dan"] = 2 } });
        Assert.Equal(0, goal.GetTarget(RecipeUnlockRules.Zhenling)); Assert.Equal(2, goal.GetTarget("recipe_huiqi_dan"));
    }
}
