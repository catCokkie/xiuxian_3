using System;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Keeps autosave retry behavior deterministic and testable.
    /// </summary>
    public static class SaveRetryRules
    {
        public readonly record struct SaveRetryDecision(
            bool KeepDirty,
            double NextCooldownSeconds);

        public static SaveRetryDecision ResolveAutosaveAttempt(bool saveSucceeded, double retryIntervalSeconds)
        {
            double normalizedRetryInterval = Math.Max(0.0, retryIntervalSeconds);
            return saveSucceeded
                ? new SaveRetryDecision(KeepDirty: false, NextCooldownSeconds: normalizedRetryInterval)
                : new SaveRetryDecision(KeepDirty: true, NextCooldownSeconds: normalizedRetryInterval);
        }
    }
}
