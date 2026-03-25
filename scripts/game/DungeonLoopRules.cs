namespace Xiuxian.Scripts.Game
{
    public static class DungeonLoopRules
    {
        public static bool ShouldEnterBossChallenge(bool completedLevel, string bossMonsterId)
        {
            return completedLevel && !string.IsNullOrEmpty(bossMonsterId);
        }

        public static float ResolveProgressAfterExploreCompletion(float nextProgress, bool completedLevel, float maxProgress)
        {
            if (!completedLevel)
            {
                return nextProgress;
            }

            return maxProgress > 0.0f ? maxProgress : nextProgress;
        }

        public static float ResolveProgressAfterBossBattle(bool isBossBattle, float currentProgress)
        {
            return isBossBattle ? 0.0f : currentProgress;
        }
    }
}
