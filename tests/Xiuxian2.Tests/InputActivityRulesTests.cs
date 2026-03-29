using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class InputActivityRulesTests
{
    // --- CalculateDecayMultiplier ---

    [Fact]
    public void DecayMultiplier_BelowThreshold_ReturnsOne()
    {
        double result = InputActivityRules.CalculateDecayMultiplier(
            apPerSecond: 1.0, apBaseline: 5.0, decayThreshold: 1.5, decayRate: 0.3, minDecayMultiplier: 0.1);

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void DecayMultiplier_AboveThreshold_DecaysLinearly()
    {
        // ratio = 10/5 = 2.0, excess = 2.0 - 1.5 = 0.5, decay = 1.0 - 0.5*0.3 = 0.85
        double result = InputActivityRules.CalculateDecayMultiplier(
            apPerSecond: 10.0, apBaseline: 5.0, decayThreshold: 1.5, decayRate: 0.3, minDecayMultiplier: 0.1);

        Assert.Equal(0.85, result, precision: 6);
    }

    [Fact]
    public void DecayMultiplier_ExtremeRatio_ClampsToMin()
    {
        // ratio = 100/5 = 20, excess = 18.5, decay = 1.0 - 18.5*0.3 = -4.55 → clamped to min 0.1
        double result = InputActivityRules.CalculateDecayMultiplier(
            apPerSecond: 100.0, apBaseline: 5.0, decayThreshold: 1.5, decayRate: 0.3, minDecayMultiplier: 0.1);

        Assert.Equal(0.1, result);
    }

    [Fact]
    public void DecayMultiplier_ZeroBaseline_UsesRawApAsRatio()
    {
        double result = InputActivityRules.CalculateDecayMultiplier(
            apPerSecond: 3.0, apBaseline: 0.0, decayThreshold: 1.5, decayRate: 0.3, minDecayMultiplier: 0.1);

        // ratio = 3.0 (raw), excess = 1.5, decay = 1.0 - 1.5*0.3 = 0.55
        Assert.Equal(0.55, result, precision: 6);
    }

    [Fact]
    public void DecayMultiplier_ZeroInput_ReturnsOne()
    {
        double result = InputActivityRules.CalculateDecayMultiplier(
            apPerSecond: 0.0, apBaseline: 5.0, decayThreshold: 1.5, decayRate: 0.3, minDecayMultiplier: 0.1);

        Assert.Equal(1.0, result);
    }

    // --- CalculateCapMultiplier ---

    [Fact]
    public void CapMultiplier_BelowCap_ReturnsOne()
    {
        double result = InputActivityRules.CalculateCapMultiplier(
            apFinalThisMinute: 100.0, softCapPerMinute: 420.0, minCapMultiplier: 0.2);

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CapMultiplier_AtCap_ReturnsOne()
    {
        double result = InputActivityRules.CalculateCapMultiplier(
            apFinalThisMinute: 420.0, softCapPerMinute: 420.0, minCapMultiplier: 0.2);

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CapMultiplier_OverCap_ReturnsInverseRatio()
    {
        // ratio = 840/420 = 2.0, multiplier = 1/2 = 0.5
        double result = InputActivityRules.CalculateCapMultiplier(
            apFinalThisMinute: 840.0, softCapPerMinute: 420.0, minCapMultiplier: 0.2);

        Assert.Equal(0.5, result, precision: 6);
    }

    [Fact]
    public void CapMultiplier_FarOverCap_ClampsToMin()
    {
        // ratio = 4200/420 = 10, multiplier = 0.1 → clamped to min 0.2
        double result = InputActivityRules.CalculateCapMultiplier(
            apFinalThisMinute: 4200.0, softCapPerMinute: 420.0, minCapMultiplier: 0.2);

        Assert.Equal(0.2, result);
    }

    [Fact]
    public void CapMultiplier_ZeroCap_ReturnsOne()
    {
        double result = InputActivityRules.CalculateCapMultiplier(
            apFinalThisMinute: 999.0, softCapPerMinute: 0.0, minCapMultiplier: 0.2);

        Assert.Equal(1.0, result);
    }

    // --- CalculateAccumulator ---

    [Fact]
    public void Accumulator_AddsApAndDrains()
    {
        // current=10, +5 ap, -0.6*1.0 drain = 14.4
        double result = InputActivityRules.CalculateAccumulator(
            currentAccumulator: 10.0, apFinal: 5.0, drainPerSecond: 0.6, delta: 1.0);

        Assert.Equal(14.4, result, precision: 6);
    }

    [Fact]
    public void Accumulator_DrainExceedsBalance_ClampsToZero()
    {
        // current=0.1, +0 ap, -0.6*1.0 drain = -0.5 → clamped to 0
        double result = InputActivityRules.CalculateAccumulator(
            currentAccumulator: 0.1, apFinal: 0.0, drainPerSecond: 0.6, delta: 1.0);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void Accumulator_ZeroDelta_NoDrain()
    {
        double result = InputActivityRules.CalculateAccumulator(
            currentAccumulator: 10.0, apFinal: 2.0, drainPerSecond: 0.6, delta: 0.0);

        Assert.Equal(12.0, result, precision: 6);
    }
}
