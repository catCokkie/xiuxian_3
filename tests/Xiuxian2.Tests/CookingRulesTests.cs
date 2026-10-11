using Xiuxian.Scripts.Services;
namespace Xiuxian.Tests;
public sealed class CookingRulesTests
{
    [Fact] public void MealsHaveDistinctEffectsAndActiveTimeDurations()
    {
        Assert.Equal(.20, MealRules.Get("food_clear_fish_soup").InsightRate);
        Assert.Equal(.15, CookingRules.GetLingqiRewardRate("food_spirit_fish_slices"));
        Assert.Equal(1, MealRules.Get("food_focus_fish_stew").ExtraOutput);
        Assert.Equal(.10, CookingRules.GetModifier("food_iron_fish_meal").DefenseRate);
        Assert.Equal(5, MealRules.Get("food_iron_fish_meal").HealPerRound);
        Assert.Equal(1800, CookingRules.GetEffectiveDuration(1));
        Assert.Equal(2394, CookingRules.GetEffectiveDuration(3));
    }
    [Fact] public void IdleTimeDoesNotConsumeMealAndLongActivityRefillsExactlyOncePerPortion()
    {
        Assert.Equal(new MealRules.TickResult(60, 0), MealRules.Advance(60, 0, 3, 1800));
        Assert.Equal(new MealRules.TickResult(1760, 1), MealRules.Advance(60, 100, 3, 1800));
        Assert.Equal(new MealRules.TickResult(0, 2), MealRules.Advance(60, 4000, 2, 1800));
        Assert.Equal(new MealRules.TickResult(0, 0), MealRules.Advance(60, 100, 0, 1800));
    }
    [Fact] public void LegacyFoodMapsToCanonicalMealWithoutChangingInventoryQuantity()
    {
        Assert.Equal("food_clear_fish_soup", MealRules.NormalizeId("food_spirit_porridge"));
        Assert.Equal("food_spirit_fish_slices", MealRules.NormalizeId("food_fruit_jelly"));
    }
}
