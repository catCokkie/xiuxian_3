using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class OfflineSettlementRulesTests
{
    [Fact]
    public void CalculateOfflineInputBudget_UsesSegmentedInputRates()
    {
        double inputs = OfflineSettlementRules.CalculateOfflineInputBudget(2 * 60 * 60);

        Assert.Equal(1080, inputs, 6);
    }

    [Fact]
    public void CalculateOfflineInputBudget_CapsAtTwentyFourHours()
    {
        double inputsA = OfflineSettlementRules.CalculateOfflineInputBudget(24 * 60 * 60);
        double inputsB = OfflineSettlementRules.CalculateOfflineInputBudget(48 * 60 * 60);

        Assert.Equal(6360, inputsA);
        Assert.Equal(inputsA, inputsB);
    }

    [Fact]
    public void EvaluateOfflineSeconds_MarksNegativeDeltaAsInvalid()
    {
        OfflineSettlementRules.OfflineTimeEvaluation evaluation = OfflineSettlementRules.EvaluateOfflineSeconds(-10);

        Assert.Equal(OfflineSettlementRules.OfflineTimeGuardMode.Invalid, evaluation.GuardMode);
        Assert.Equal(0.0, evaluation.EffectiveOfflineSeconds);
    }

    [Fact]
    public void EvaluateOfflineSeconds_LongAbsenceIsNormalAndCapped()
    {
        OfflineSettlementRules.OfflineTimeEvaluation normal = OfflineSettlementRules.EvaluateOfflineSeconds(24 * 60 * 60);
        OfflineSettlementRules.OfflineTimeEvaluation guarded = OfflineSettlementRules.EvaluateOfflineSeconds(48 * 60 * 60);

        Assert.Equal(OfflineSettlementRules.OfflineTimeGuardMode.Normal, normal.GuardMode);
        Assert.Equal(OfflineSettlementRules.OfflineTimeGuardMode.Normal, guarded.GuardMode);
        Assert.Equal(normal.EffectiveOfflineSeconds, guarded.EffectiveOfflineSeconds);
    }

    [Fact]
    public void BuildCultivationOfflineSettlement_BuildsRewardResult()
    {
        ActionSettlementResult result = OfflineSettlementRules.BuildCultivationOfflineSettlement(
            offlineSeconds: 60 * 60,
            apPerInput: 1.0,
            lingqiFactor: 0.9,
            insightFactor: 0.08,
            baseExpPerInput: GameBalanceConstants.ResourceConversion.BaseExpPerInput,
            realmMultiplier: 1.0,
            actionTargetId: "");

        Assert.Equal(PlayerActionState.ActionCultivation, result.ActionId);
        Assert.True(result.ApConsumed > 0.0);
        Assert.True(result.LingqiGain > 0.0);
        Assert.True(result.InsightGain > 0.0);
        Assert.Equal(600 * GameBalanceConstants.ResourceConversion.BaseExpPerInput, result.RealmExpGain, 6);
    }

    [Theory]
    [InlineData(36 * 3600 - 1)]
    [InlineData(36 * 3600)]
    [InlineData(36 * 3600 + 1)]
    [InlineData(7 * 24 * 3600)]
    public void ReturnAfterWeekendOrWeekRetainsTheSameUpperBudget(double seconds)
        => Assert.Equal(6360, OfflineSettlementRules.CalculateOfflineInputBudget(seconds));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-1)]
    [InlineData(0)]
    public void InvalidClockDeltaCannotProduceResources(double seconds)
    {
        Assert.Equal(OfflineSettlementRules.OfflineTimeGuardMode.Invalid,
            OfflineSettlementRules.EvaluateOfflineSeconds(seconds).GuardMode);
        Assert.Equal(0, OfflineSettlementRules.CalculateOfflineInputBudget(seconds));
    }

    [Fact]
    public void OfflineExperienceIsIndependentOfRealmAndLingqiMultipliers()
    {
        var baseline = OfflineSettlementRules.BuildCultivationOfflineSettlement(3600, 1, .9, .08, .28, 1);
        var rich = OfflineSettlementRules.BuildCultivationOfflineSettlement(3600, 1.2, 1.8, .08, .28, 1.48);
        Assert.Equal(baseline.RealmExpGain, rich.RealmExpGain);
        Assert.True(rich.LingqiGain > baseline.LingqiGain);
        Assert.True(baseline.RealmExpGain < 600 * GameBalanceConstants.ResourceConversion.CultivationExpPerInput);
    }
}
