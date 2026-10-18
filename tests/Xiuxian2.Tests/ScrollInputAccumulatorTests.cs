using Xiuxian.Scripts.Services;

namespace Xiuxian2.Tests;

public class ScrollInputAccumulatorTests
{
    [Fact]
    public void SmallWindowsDeltasAccumulateInsteadOfDisappearing()
    {
        var wheel = new ScrollInputAccumulator();
        Assert.Equal(0, wheel.AddWindowsDelta(30));
        Assert.Equal(0, wheel.AddWindowsDelta(30));
        Assert.Equal(0, wheel.AddWindowsDelta(30));
        Assert.Equal(1, wheel.AddWindowsDelta(30));
        Assert.Equal(2, wheel.AddWindowsDelta(-240));
    }

    [Fact]
    public void AxesAreIndependentAndOppositeTravelDoesNotCancel()
    {
        var wheel = new ScrollInputAccumulator();
        Assert.Equal(0, wheel.AddWindowsDelta(60));
        Assert.Equal(0, wheel.AddWindowsDelta(60, true));
        Assert.Equal(1, wheel.AddWindowsDelta(-60));
        Assert.Equal(1, wheel.AddWindowsDelta(-60, true));
    }

    [Fact]
    public void FallbackFactorsPreserveFractionalAndMultiStepTravel()
    {
        var wheel = new ScrollInputAccumulator();
        int total = 0;
        for (int i = 0; i < 100; i++) total += wheel.Add(.1);
        Assert.Equal(10, total);
        Assert.Equal(3, wheel.Add(3.5));
        Assert.Equal(1, wheel.Add(.5));
    }

    [Fact]
    public void PauseOrSourceChangeResetsRemaindersAndInvalidValuesAreIgnored()
    {
        var wheel = new ScrollInputAccumulator();
        wheel.Add(.75); wheel.Add(.75, true); wheel.Reset();
        Assert.Equal(0, wheel.Add(.25)); Assert.Equal(0, wheel.Add(.25, true));
        Assert.Equal(0, wheel.Add(double.NaN)); Assert.Equal(0, wheel.Add(double.PositiveInfinity));
        Assert.Equal(0, wheel.Add(-1)); Assert.Equal(0, wheel.Add(0));
    }
}
