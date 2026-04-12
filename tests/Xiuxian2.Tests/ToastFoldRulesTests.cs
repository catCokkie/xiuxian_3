using Xiuxian.Scripts.Ui;

namespace Xiuxian.Tests;

public sealed class ToastFoldRulesTests
{
    [Theory]
    [InlineData("炼丹完成", 1, "炼丹完成")]
    [InlineData("炼丹完成", 2, "炼丹完成 ×2")]
    [InlineData("强化完成", 5, "强化完成 ×5")]
    public void BuildDisplayMessage_AppendsCountSuffixWhenNeeded(string message, int count, string expected)
    {
        string text = ToastFoldRules.BuildDisplayMessage(message, count);

        Assert.Equal(expected, text);
    }

    [Fact]
    public void CanFold_ReturnsTrueForSameKeyWithinThreeSecondWindow()
    {
        bool result = ToastFoldRules.CanFold("craft_alchemy_complete", "craft_alchemy_complete", existingLastSeenAt: 12.0, now: 14.9);

        Assert.True(result);
    }

    [Theory]
    [InlineData("craft_alchemy_complete", "craft_smithing_complete", 12.0, 13.0)]
    [InlineData("craft_alchemy_complete", "craft_alchemy_complete", 12.0, 15.1)]
    [InlineData("", "craft_alchemy_complete", 12.0, 13.0)]
    public void CanFold_ReturnsFalseForDifferentKeyExpiredWindowOrEmptyKey(string existingKey, string incomingKey, double existingLastSeenAt, double now)
    {
        bool result = ToastFoldRules.CanFold(existingKey, incomingKey, existingLastSeenAt, now);

        Assert.False(result);
    }
}
