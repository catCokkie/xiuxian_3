using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class SaveRetryRulesTests
{
    [Fact]
    public void ResolveAutosaveAttempt_ClearsDirtyFlagAfterSuccessfulSave()
    {
        SaveRetryRules.SaveRetryDecision decision = SaveRetryRules.ResolveAutosaveAttempt(saveSucceeded: true, retryIntervalSeconds: 0.5);

        Assert.False(decision.KeepDirty);
        Assert.Equal(0.5, decision.NextCooldownSeconds, 6);
    }

    [Fact]
    public void ResolveAutosaveAttempt_KeepsDirtyFlagAndRearmsCooldownAfterFailure()
    {
        SaveRetryRules.SaveRetryDecision decision = SaveRetryRules.ResolveAutosaveAttempt(saveSucceeded: false, retryIntervalSeconds: 0.5);

        Assert.True(decision.KeepDirty);
        Assert.Equal(0.5, decision.NextCooldownSeconds, 6);
    }

    [Fact]
    public void ResolveAutosaveAttempt_ClampsNegativeRetryIntervalToZero()
    {
        SaveRetryRules.SaveRetryDecision decision = SaveRetryRules.ResolveAutosaveAttempt(saveSucceeded: false, retryIntervalSeconds: -3.0);

        Assert.True(decision.KeepDirty);
        Assert.Equal(0.0, decision.NextCooldownSeconds, 6);
    }
}
