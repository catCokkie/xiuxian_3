using Xiuxian.Scripts.Game;

namespace Xiuxian.Tests;

public sealed class ExploreProgressDebugRulesTests
{
    [Fact]
    public void BuildValidationFilterSummary_FormatsScopeAndLevelMode()
    {
        Assert.Equal("all, all-levels", ExploreProgressDebugRules.BuildValidationFilterSummary("all", false));
        Assert.Equal("monster, active-level", ExploreProgressDebugRules.BuildValidationFilterSummary("monster", true));
    }

    [Fact]
    public void BuildMoveDebugStatus_FormatsRemainingSteps()
    {
        string text = ExploreProgressDebugRules.BuildMoveDebugStatus(0, 4, 1, "monster_slime");

        Assert.Contains("前排#1", text);
        Assert.Contains("还需 3 步", text);
    }
}
