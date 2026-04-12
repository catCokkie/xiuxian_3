using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class EventLogBadgeTests
{
    [Theory]
    [InlineData(0, "")]
    [InlineData(1, "1")]
    [InlineData(9, "9")]
    [InlineData(10, "9+")]
    [InlineData(42, "9+")]
    public void BuildUnreadBadgeText_ClampsToNinePlus(int unreadCount, string expected)
    {
        string text = EventLogPresentationRules.BuildUnreadBadgeText(unreadCount);

        Assert.Equal(expected, text);
    }
}
