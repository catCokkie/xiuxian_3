using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public sealed class BattleSimulationService
    {
        public delegate string RollSpawnMonsterIdDelegate();
        public delegate Dictionary<string, int> RollMonsterDropsDelegate(string monsterId);
        public delegate bool TryRollMonsterSettlementRewardDelegate(string monsterId, out double lingqi, out double insight);

        private readonly RollSpawnMonsterIdDelegate _rollSpawnMonsterId;
        private readonly RollMonsterDropsDelegate _rollMonsterDrops;
        private readonly TryRollMonsterSettlementRewardDelegate _tryRollMonsterSettlementReward;

        public BattleSimulationService(
            RollSpawnMonsterIdDelegate rollSpawnMonsterId,
            RollMonsterDropsDelegate rollMonsterDrops,
            TryRollMonsterSettlementRewardDelegate tryRollMonsterSettlementReward)
        {
            _rollSpawnMonsterId = rollSpawnMonsterId;
            _rollMonsterDrops = rollMonsterDrops;
            _tryRollMonsterSettlementReward = tryRollMonsterSettlementReward;
        }

        public string RunSimulation(
            int battleCount,
            string forcedMonsterId,
            Action<Dictionary<string, int>, string, int> addDrop,
            Action restoreState,
            Func<bool> wasPityTriggered,
            Func<bool> wasDailyBlocked,
            Func<bool> wasSoftCapSkipped)
        {
            int count = Math.Max(1, battleCount);
            var itemTotals = new Dictionary<string, int>();
            double totalLingqi = 0.0;
            double totalInsight = 0.0;
            int pityTriggeredCount = 0;
            int dailyBlockedCount = 0;
            int softSkipCount = 0;

            for (int i = 0; i < count; i++)
            {
                string monsterId = forcedMonsterId;
                if (string.IsNullOrEmpty(monsterId))
                {
                    monsterId = _rollSpawnMonsterId();
                }

                if (string.IsNullOrEmpty(monsterId))
                {
                    continue;
                }

                Dictionary<string, int> drops = _rollMonsterDrops(monsterId);
                foreach ((string itemId, int qty) in drops)
                {
                    addDrop(itemTotals, itemId, qty);
                }

                if (_tryRollMonsterSettlementReward(monsterId, out double lingqi, out double insight))
                {
                    totalLingqi += lingqi;
                    totalInsight += insight;
                }

                if (wasPityTriggered())
                {
                    pityTriggeredCount++;
                }

                if (wasDailyBlocked())
                {
                    dailyBlockedCount++;
                }

                if (wasSoftCapSkipped())
                {
                    softSkipCount++;
                }
            }

            restoreState();

            double avgLingqi = totalLingqi / count;
            double avgInsight = totalInsight / count;
            string topDrops = BuildTopDropsSummary(itemTotals, 3);
            return $"n={count}, avg_lq={avgLingqi:0.0}, avg_in={avgInsight:0.0}, pity={pityTriggeredCount}, softSkip={softSkipCount}, dailyBlock={dailyBlockedCount}, top={topDrops}";
        }

        private static string BuildTopDropsSummary(Dictionary<string, int> itemTotals, int topN)
        {
            if (itemTotals.Count == 0)
            {
                return "none";
            }

            var list = new List<KeyValuePair<string, int>>(itemTotals);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            int n = Math.Min(Math.Max(1, topN), list.Count);
            return string.Join(",", list.GetRange(0, n).ConvertAll(kv => $"{kv.Key}x{kv.Value}"));
        }
    }
}
