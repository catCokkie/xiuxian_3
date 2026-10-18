using Xiuxian.Scripts.Services;
using Xiuxian.Scripts.Game;

namespace Xiuxian.Tests;

[Collection("ActivityRegistry")]
public sealed class SuppressionChoiceTests
{
    [Theory]
    [InlineData(3, 2, true, false)]
    [InlineData(4, 1, true, false)]
    [InlineData(4, 2, false, false)]
    [InlineData(4, 2, true, true)]
    public void PotionRequiresAllThreeUnlockConditions(int realm, int mastery, bool cleared, bool available)
    {
        var recipe = ActivityRegistry.GetRecipe(RecipeUnlockRules.Yangyuan)!;
        Assert.Equal(available, RecipeUnlockRules.Available(recipe, realm, mastery, cleared ? new[] { "lv_qi_003" } : Array.Empty<string>()));
        Assert.Equal(260, recipe.RequiredInputEvents); Assert.Equal(70, recipe.LingqiCost);
        Assert.Equal(new[] { new MaterialCost("spirit_herb", 3), new MaterialCost("spirit_pearl", 1) }, recipe.Inputs);
        Assert.Equal(recipe.Inputs, AlchemyRules.CompleteRecipe(recipe.RecipeId, 2).Inputs);
        Assert.Equal(new[] { new MaterialOutput(SuppressionRules.PotionId, 1) }, recipe.Outputs);
    }

    [Fact]
    public void SecondIngredientAndItsReserveBlockTheWholeBatch()
    {
        var items = new Dictionary<string, int> { ["spirit_herb"] = 9 };
        var missing = ProductionBlockRules.Alchemy(RecipeUnlockRules.Yangyuan, 1000, items, null, 2);
        Assert.Equal("spirit_pearl", missing.Primary!.ItemId); Assert.Equal(ProductionBlockKind.Material, missing.Primary.Kind);
        items["spirit_pearl"] = 1;
        var reserve = ProductionBlockRules.Alchemy(RecipeUnlockRules.Yangyuan, 1000, items, new Dictionary<string, int> { ["spirit_pearl"] = 1 }, 2);
        Assert.Equal(ProductionBlockKind.Reserve, reserve.Primary!.Kind);
        Assert.True(AlchemyRules.CanStartRecipe(RecipeUnlockRules.Yangyuan, 70, items, 2));
        Assert.False(AlchemyRules.CanStartRecipe(RecipeUnlockRules.Yangyuan, 69.9, items, 2));
    }

    [Theory]
    [InlineData("potion", true, 4, false, 1, true)]
    [InlineData("talisman", true, 4, false, 1, false)]
    [InlineData("none", true, 4, false, 1, false)]
    [InlineData("potion", false, 4, false, 1, false)]
    [InlineData("potion", true, 0, false, 1, false)]
    [InlineData("potion", true, 4, true, 1, false)]
    [InlineData("potion", true, 4, false, 0, false)]
    public void SelectedPotionIsUsedOnlyOnceAtHealingBattleStart(string choice, bool start, int healing, bool suppressed, int stock, bool use)
        => Assert.Equal(use, SuppressionRules.ShouldUsePotion(choice, start, healing, suppressed, stock));

    [Fact]
    public void LegacyChoiceAndSimultaneousSourcesNeverExtendWindow()
    {
        Assert.Equal("talisman", SuppressionRules.Normalize(""));
        Assert.Equal("", SuppressionRules.Recipe("none"));
        Assert.Equal(5, SuppressionRules.Window(new[] { RecipeUnlockRules.Zhenling }, true));
        Assert.Equal(0, SuppressionRules.Window(Array.Empty<string>(), false));
        var goal = new ChallengeGoalPlan(); goal.Restore(new Dictionary<string, object> { ["level_id"] = "lv_qi_004", ["boss_id"] = "boss" });
        Assert.Equal(0, goal.GetTarget(RecipeUnlockRules.Yangyuan));
    }

    [Fact]
    public void MigrationPreservesBattleAndStockWithoutGrantingPotion()
    {
        var store = new SaveMigrationRules.MigrationStore();
        var action = new Dictionary<string, object> { ["action_id"] = "alchemy" };
        var runtime = new Dictionary<string, object> { ["battle_round_counter"] = 3, ["player_hp"] = 7 };
        var stock = new Dictionary<string, object> { ["potion_huiqi_dan"] = 2 };
        store.SetValue("action", "mode", action); store.SetValue("explore", "runtime", runtime); store.SetValue("potions", "inventory", stock);
        SaveMigrationRules.MigrateToLatest(store, 25);
        Assert.Equal("talisman", action["suppression_choice"]); Assert.Equal(false, runtime["used_suppression_potion"]);
        Assert.Equal(3, runtime["battle_round_counter"]); Assert.Equal(7, runtime["player_hp"]); Assert.Single(stock);
        action["suppression_choice"] = "potion"; runtime["used_suppression_potion"] = true;
        SaveMigrationRules.MigrateToLatest(store, 25);
        Assert.Equal("potion", action["suppression_choice"]); Assert.Equal(true, runtime["used_suppression_potion"]);
    }

    [Fact]
    public void PreparationUsesBothMaterialsAndExistingMasteryYield()
    {
        var prep = ChallengePreparationRules.Build(RecipeUnlockRules.Yangyuan, 3, 0, 3, 1, 0, new Dictionary<string, int>())!;
        Assert.Equal(2, prep.PerBatch); Assert.Equal(2, prep.Rounds); Assert.Equal(140, prep.LingqiCost);
        Assert.Equal(new[] { 6, 2 }, prep.Materials.Select(x => x.Required));
    }

    [Fact]
    public void PotionAdviceNamesActualSourceAndNeverPromisesExtension()
    {
        var attempt = new BossChallengeAttempt(); attempt.Begin("zone", "boss"); attempt.RecordPotion(SuppressionRules.PotionId, 0);
        attempt.Observe(new(20, 10, 0)); var report = attempt.Finish("timeout", 10, 90, 100, 1);
        var advice = BossChallengeReportRules.Advice(report);
        Assert.Contains("抑生丹", advice[1]); Assert.Contains("不会延长", advice[1]);
        Assert.Equal(0, report.PlayerHealing);
    }
}
