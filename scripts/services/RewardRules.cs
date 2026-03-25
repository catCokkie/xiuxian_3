using System;

namespace Xiuxian.Scripts.Services
{
    public static class RewardRules
    {
        public static BattleRewardDecision DetermineBattleRewardDecision(int dropCount, double lingqi, double insight, int spiritStones, string itemPart)
        {
            bool hasConfiguredRewards = dropCount > 0 || lingqi > 0.0 || insight > 0.0 || spiritStones > 0;
            return new BattleRewardDecision(
                hasConfiguredRewards,
                !hasConfiguredRewards,
                BuildBattleRewardSummary(lingqi, insight, spiritStones, itemPart));
        }

        public static string BuildBattleRewardSummary(double lingqi, double insight, int spiritStones, string itemPart)
        {
            return $"灵气+{lingqi:0} 悟性+{insight:0} 灵石+{spiritStones} 掉落:{itemPart}";
        }

        public static int CalculateBattleSpiritStoneReward(string moveCategory, bool isBoss, int zoneIndex = 0)
        {
            int dangerLevel = Math.Max(1, zoneIndex + 1);
            if (isBoss)
            {
                return 15;
            }

            return moveCategory switch
            {
                "elite" => 3 + dangerLevel * 2 + 2,
                _ => 3 + dangerLevel * 2,
            };
        }

        public static string BuildLevelCompletionSourceTag(string levelId, bool firstClear)
        {
            return firstClear ? $"level_first_clear:{levelId}" : $"level_repeat_clear:{levelId}";
        }
    }
}
