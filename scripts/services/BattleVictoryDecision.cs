namespace Xiuxian.Scripts.Services
{
    public readonly record struct BattleVictoryDecision(
        bool ShouldEndBattle,
        bool ShouldApplyBattleRewards,
        bool ShouldResetExploreProgress,
        bool ShouldApplyLevelCompletionRewards,
        bool ShouldTryBossUnlock,
        string ActiveLevelId,
        string MonsterId);
}
