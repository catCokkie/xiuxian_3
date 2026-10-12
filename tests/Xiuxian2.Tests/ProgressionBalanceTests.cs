using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ProgressionBalanceTests
{
    [Theory]
    [InlineData(1, 1.0)]
    [InlineData(9, 1.0)]
    [InlineData(9, 1.2)]
    [InlineData(9, 0.4)]
    public void CultivationHasTheSameExperienceAdvantageForEveryInputType(int realm, double apPerInput)
    {
        double multiplier = 1 + (realm - 1) * .06;
        var cultivation = ResourceConversionRules.Calculate("cultivation", 100 * apPerInput, 100, 300, multiplier, 4, .5);
        foreach (string action in new[] { "dungeon", "garden", "mining", "fishing", "alchemy", "smithing", "talisman", "cooking", "formation", "body_cultivation" })
        {
            var other = ResourceConversionRules.Calculate(action, 100 * apPerInput, 100, 300, multiplier, 4, .5, 1.5);
            Assert.Equal(28, other.RealmExp, 6);
            Assert.Equal(1.25, cultivation.RealmExp / other.RealmExp, 6);
        }
    }

    [Fact]
    public void PassiveRecoveryAndMouseMovementDoNotCreateInputExperience()
    {
        foreach (string action in new[] { "cultivation", "dungeon", "body_cultivation" })
        {
            var passive = ResourceConversionRules.Calculate(action, 0, 0, 300, 1.48, 4, .5, 1.5);
            var movement = ResourceConversionRules.Calculate(action, 100, 0, 300, 1.48, 4, .5);
            Assert.Equal(0, passive.RealmExp);
            Assert.Equal(0, movement.RealmExp);
            Assert.True(movement.Lingqi > passive.Lingqi);
        }
    }

    [Fact]
    public void DisablingCultivationBonusKeepsTheCommonRateAndDoesNotFallbackToLingqi()
    {
        var result = ResourceConversionRules.Calculate("cultivation", 120, 100, 300, 1.48, 4, .5, inputExpEnabled: false);
        Assert.Equal(28, result.RealmExp, 6);
    }

    [Fact]
    public void BasicSelfSupplyIsCheaperEvenWithMinimumYieldAndNoReturnedSeed()
    {
        var seed = ShopRules.GetItems().Single(x => x.ItemId == ShopRules.ItemSpiritHerbSeed);
        var bundle = ShopRules.GetItems().Single(x => x.ItemId == ShopRules.ItemHuiqiDanBundle);
        Assert.True(GardenRules.TryGetCrop("garden_spirit_herb", out var crop));
        Assert.True(AlchemyRules.TryGetRecipe("recipe_huiqi_dan", out var recipe));
        double worstGoldPerPotion = (seed.Price + GardenRules.PlantingSpiritStones)
            / (double)crop.MinOutputCount * recipe.MaterialCount / recipe.OutputCount;
        Assert.Equal(4, worstGoldPerPotion);
        Assert.True(worstGoldPerPotion < bundle.Price / (double)bundle.GrantAmount);
        Assert.Equal(30, bundle.Price); // The affordable convenience route stays available.
    }
}
