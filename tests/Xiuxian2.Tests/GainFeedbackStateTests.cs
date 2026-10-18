using Xiuxian.Scripts.Services;

namespace Xiuxian2.Tests;

public class GainFeedbackStateTests
{
    [Fact]
    public void ShowsActualAmountsAndMergesContinuousInput()
    {
        var state = new GainFeedbackState();
        state.ObserveReward(1, 1, .35, .9, .08); state.Tick(.1);
        state.ObserveReward(2, 2, .7, 1.8, .16);
        Assert.Equal(3, state.Inputs); Assert.Equal(1.05, state.Experience, 6);
        Assert.Equal(2.7, state.Lingqi, 6); Assert.Equal(1, state.Pulse);
    }

    [Fact]
    public void NewBurstAfterIdleStartsFromTheNewReward()
    {
        var state = new GainFeedbackState(); state.ObserveReward(20, 20, 7, 18, 1.6);
        state.Tick(3); state.ObserveReward(1, 1, .35, .9, .08);
        Assert.Equal(1, state.Inputs); Assert.Equal(.35, state.Experience);
    }

    [Fact]
    public void PassiveRecoveryCannotPretendToBeAnInputReward()
    {
        var state = new GainFeedbackState(); state.ObserveReward(0, 0, 0, 5, 0);
        Assert.Equal(0, state.Lingqi); Assert.Equal(0, state.Experience); Assert.Equal(0, state.Pulse);
    }

    [Fact]
    public void LimitedInputHasNoRewardPulseAndNoticeExpiresWithoutMoreInput()
    {
        var state = new GainFeedbackState(); state.ObserveReward(1, 1, .35, .9, .08);
        state.ObserveLimit(20, 0); Assert.True(state.Limited); Assert.Equal(0, state.Pulse);
        state.Tick(3); Assert.False(state.Limited);
        Assert.Equal(.35, state.Experience);
    }

    [Fact]
    public void MovementRewardsHaveNoFabricatedInputExperience()
    {
        var state = new GainFeedbackState(); state.ObserveReward(0, 1, 0, .9, .08);
        Assert.Equal(0, state.Inputs); Assert.Equal(0, state.Experience); Assert.Equal(.9, state.Lingqi);
    }

    [Fact]
    public void GoalsRespectManualBreakthroughGateAndChapterCap()
    {
        Assert.Contains("290", GainFeedbackState.NextRealmGoal(1, 10, 300, false, true));
        Assert.Contains("已可突破", GainFeedbackState.NextRealmGoal(1, 310, 300, true, true));
        Assert.Contains("最终 Boss", GainFeedbackState.NextRealmGoal(9, 9999, 2000, false, false));
        Assert.Contains("继续保留", GainFeedbackState.NextRealmGoal(10, 9999, 3000, false, true));
    }
}
