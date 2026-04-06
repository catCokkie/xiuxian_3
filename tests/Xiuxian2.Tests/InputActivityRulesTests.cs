using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class InputActivityRulesTests
{
    [Fact]
    public void CalculateRemainingWindowAllowance_BelowCap_ReturnsRemaining()
    {
        int result = InputActivityRules.CalculateRemainingWindowAllowance(
            currentWindowCount: 420,
            maxInputPerMinute: 600);

        Assert.Equal(180, result);
    }

    [Fact]
    public void CalculateRemainingWindowAllowance_AtCap_ReturnsZero()
    {
        int result = InputActivityRules.CalculateRemainingWindowAllowance(
            currentWindowCount: 600,
            maxInputPerMinute: 600);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateRemainingWindowAllowance_ZeroCap_DisablesLimit()
    {
        int result = InputActivityRules.CalculateRemainingWindowAllowance(
            currentWindowCount: 999,
            maxInputPerMinute: 0);

        Assert.Equal(int.MaxValue, result);
    }

    [Fact]
    public void ClampDiscreteInputBatch_WithinAllowance_ReturnsOriginalBatch()
    {
        InputActivityRules.DiscreteInputBatch batch = new(
            KeyDownCount: 4,
            MouseClickCount: 3,
            MouseScrollSteps: 2,
            JoypadButtonCount: 1,
            JoypadAxisInputCount: 1);

        InputActivityRules.DiscreteInputBatch accepted = InputActivityRules.ClampDiscreteInputBatch(batch, allowedCount: 20);

        Assert.Equal(batch, accepted);
    }

    [Fact]
    public void ClampDiscreteInputBatch_NoAllowance_ReturnsEmptyBatch()
    {
        InputActivityRules.DiscreteInputBatch batch = new(
            KeyDownCount: 4,
            MouseClickCount: 3,
            MouseScrollSteps: 2,
            JoypadButtonCount: 1,
            JoypadAxisInputCount: 1);

        InputActivityRules.DiscreteInputBatch accepted = InputActivityRules.ClampDiscreteInputBatch(batch, allowedCount: 0);

        Assert.Equal(0, accepted.TotalCount);
        Assert.Equal(default, accepted);
    }

    [Fact]
    public void ClampDiscreteInputBatch_OverAllowance_PreservesTotalAndRelativeShape()
    {
        InputActivityRules.DiscreteInputBatch batch = new(
            KeyDownCount: 6,
            MouseClickCount: 3,
            MouseScrollSteps: 1,
            JoypadButtonCount: 0,
            JoypadAxisInputCount: 0);

        InputActivityRules.DiscreteInputBatch accepted = InputActivityRules.ClampDiscreteInputBatch(batch, allowedCount: 5);

        Assert.Equal(5, accepted.TotalCount);
        Assert.True(accepted.KeyDownCount <= batch.KeyDownCount);
        Assert.True(accepted.MouseClickCount <= batch.MouseClickCount);
        Assert.True(accepted.MouseScrollSteps <= batch.MouseScrollSteps);
        Assert.Equal(3, accepted.KeyDownCount);
        Assert.Equal(2, accepted.MouseClickCount);
        Assert.Equal(0, accepted.MouseScrollSteps);
    }

    [Fact]
    public void CalculateRawAp_UsesAcceptedDiscreteInputsPlusMouseMove()
    {
        InputActivityRules.DiscreteInputBatch batch = new(
            KeyDownCount: 3,
            MouseClickCount: 2,
            MouseScrollSteps: 1,
            JoypadButtonCount: 1,
            JoypadAxisInputCount: 2);

        double result = InputActivityRules.CalculateRawAp(
            batch,
            mouseMoveDistancePx: 300.0,
            keyDownWeight: 1.0,
            mouseClickWeight: 1.2,
            scrollStepWeight: 0.4,
            movePxDivider: 600.0,
            joypadButtonWeight: 1.0,
            joypadAxisWeight: 0.8);

        Assert.Equal(8.9, result, precision: 6);
    }

    [Fact]
    public void CalculateAccumulator_AddsApAndDrains()
    {
        double result = InputActivityRules.CalculateAccumulator(
            currentAccumulator: 10.0,
            apFinal: 5.0,
            drainPerSecond: 0.6,
            delta: 1.0);

        Assert.Equal(14.4, result, precision: 6);
    }

    [Fact]
    public void CalculateAccumulator_DrainExceedsBalance_ClampsToZero()
    {
        double result = InputActivityRules.CalculateAccumulator(
            currentAccumulator: 0.1,
            apFinal: 0.0,
            drainPerSecond: 0.6,
            delta: 1.0);

        Assert.Equal(0.0, result);
    }
}
