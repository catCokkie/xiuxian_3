using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class AfkDetectionRulesTests
{
    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(59.9, 1.0)]
    [InlineData(60.0, 0.5)]
    [InlineData(119.9, 0.5)]
    [InlineData(120.0, 0.0)]
    public void GetProgressMultiplier_UsesConfiguredBands(double idleSeconds, double expected)
    {
        double result = AfkDetectionRules.GetProgressMultiplier(idleSeconds);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsAfk_ReturnsTrueAtAndAboveThreshold()
    {
        Assert.False(AfkDetectionRules.IsAfk(119.9));
        Assert.True(AfkDetectionRules.IsAfk(120.0));
        Assert.True(AfkDetectionRules.IsAfk(180.0));
    }
}
