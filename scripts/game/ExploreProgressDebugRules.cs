using Godot;
using System.Collections.Generic;
using System.Text;

namespace Xiuxian.Scripts.Game
{
    public static class ExploreProgressDebugRules
    {
        public static string BuildMoveDebugStatus(int frontIndex, int threshold, int pending, string monsterId)
        {
            if (frontIndex < 0 || threshold <= 0)
            {
                return "调试-移动：前排怪 无目标";
            }

            int clampedPending = Mathf.Clamp(pending, 0, threshold);
            int remaining = Mathf.Max(0, threshold - clampedPending);
            return $"调试-移动：前排#{frontIndex + 1} [{monsterId}] 还需 {remaining} 步 ({clampedPending}/{threshold})";
        }

        public static string BuildBattleDebugStatus(bool inBattle, int roundCounter, int threshold, int pendingBattleInputEvents, int enemyHp, int playerAttackPerRound)
        {
            if (!inBattle)
            {
                return "调试-战斗：未接敌";
            }

            int safeThreshold = Mathf.Max(1, threshold);
            int pending = Mathf.Clamp(pendingBattleInputEvents, 0, safeThreshold);
            int remaining = Mathf.Max(0, safeThreshold - pending);
            int roundsToKill = Mathf.CeilToInt(Mathf.Max(0, enemyHp) / (float)Mathf.Max(1, playerAttackPerRound));
            return $"调试-战斗：回合 {roundCounter}，下回合剩 {remaining} 输入 ({pending}/{safeThreshold})，预计 {roundsToKill} 回合结束";
        }

        public static string BuildValidationFilterSummary(string scope, bool activeLevelOnly)
        {
            string levelScope = activeLevelOnly ? "active-level" : "all-levels";
            return $"{scope}, {levelScope}";
        }

        public static List<Godot.Collections.Dictionary<string, Variant>> FilterValidationEntries(
            IEnumerable<Godot.Collections.Dictionary<string, Variant>> entries,
            string scopeFilter,
            bool activeLevelOnly,
            string activeLevelId)
        {
            var result = new List<Godot.Collections.Dictionary<string, Variant>>();
            foreach (var entry in entries)
            {
                string scope = entry.ContainsKey("scope") ? entry["scope"].AsString() : "config";
                string levelId = entry.ContainsKey("level_id") ? entry["level_id"].AsString() : "";

                if (scopeFilter != "all" && scope != scopeFilter)
                {
                    continue;
                }

                if (activeLevelOnly && !string.IsNullOrEmpty(activeLevelId))
                {
                    if (string.IsNullOrEmpty(levelId) || levelId != activeLevelId)
                    {
                        continue;
                    }
                }

                result.Add(entry);
            }

            return result;
        }

        public static string BuildDebugPanelText(
            string zoneName,
            string actionMode,
            string actionTargetId,
            float exploreProgress,
            string battleMonsterName,
            string battleMonsterId,
            string simulationLevelFilterId,
            string simulationMonsterFilterId,
            string loaderDebugSummary,
            string loaderValidationSummary,
            string loaderLevelPreviewSummary,
            string lastSimulationSummary)
        {
            var sb = new StringBuilder();
            sb.Append($"[F8] debug | zone={zoneName}");
            sb.Append($" | mode={actionMode}");
            sb.Append($" | target={actionTargetId}");
            sb.Append($" | progress={exploreProgress:0.0}%");
            sb.Append($" | monster={battleMonsterName}({battleMonsterId})");
            sb.Append($"\nSimFilter level={(string.IsNullOrEmpty(simulationLevelFilterId) ? "active" : simulationLevelFilterId)}");
            sb.Append($" | monster={(string.IsNullOrEmpty(simulationMonsterFilterId) ? "auto" : simulationMonsterFilterId)}");

            if (!string.IsNullOrEmpty(loaderDebugSummary))
            {
                sb.Append('\n');
                sb.Append(loaderDebugSummary);
                sb.Append('\n');
                sb.Append(loaderValidationSummary);
                sb.Append('\n');
                sb.Append(loaderLevelPreviewSummary);
            }

            sb.Append("\n[F4] toggle main action  [F5] switch unlocked level  [F6] sim-level  [F7] sim-monster  [F9] sim200  [F10] sim1000  [F11] scope  [F12] active-level");
            sb.Append($"\nSim: {lastSimulationSummary}");
            return sb.ToString();
        }
    }
}
