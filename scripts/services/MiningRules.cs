using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class MiningRules
    {
        public const int DefaultNodeDurability = 100;

        public readonly record struct NodeSpec(
            string RecipeId,
            string DisplayName,
            string OutputItemId,
            int OutputCount,
            int RequiredInputs,
            int RequiredMasteryLevel = 1);

        public readonly record struct MiningProgressDecision(float NextProgress, bool CompletedBatch, int NextDurability, bool RefreshedNode);
        public readonly record struct GatherResult(string ItemId, int ItemCount);

        private static readonly NodeSpec[] Nodes =
        {
            new("mining_cold_iron_ore", "开采寒铁矿", "cold_iron_ore", 2, 180, 1),
            new("mining_spirit_jade", "开采灵玉", "spirit_jade", 2, 220, 2),
            new("mining_mithril", "开采秘银", "mithril", 2, 260, 4),
        };

        public static IReadOnlyList<NodeSpec> GetNodes() => Nodes;

        public static bool TryGetNode(string recipeId, out NodeSpec node)
        {
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].RecipeId == recipeId)
                {
                    node = Nodes[i];
                    return true;
                }
            }

            node = default;
            return false;
        }

        public static bool CanMineNode(string recipeId, int masteryLevel = 1)
        {
            return TryGetNode(recipeId, out NodeSpec node) && masteryLevel >= node.RequiredMasteryLevel;
        }

        public static MiningProgressDecision AdvanceProgress(float currentProgress, int inputEvents, int requiredInputs, int currentDurability)
        {
            float next = Math.Max(0.0f, currentProgress) + Math.Max(0, inputEvents);
            float threshold = Math.Max(1, requiredInputs);
            int durability = Math.Clamp(currentDurability, 1, DefaultNodeDurability);
            bool completed = next >= threshold;
            bool refreshed = false;

            if (!completed)
            {
                return new MiningProgressDecision(next, false, durability, false);
            }

            durability = Math.Max(0, durability - 1);
            if (durability == 0)
            {
                durability = DefaultNodeDurability;
                refreshed = true;
            }

            return new MiningProgressDecision(0.0f, true, durability, refreshed);
        }

        public static GatherResult GatherOre(string recipeId, int masteryLevel = 1)
        {
            if (!TryGetNode(recipeId, out NodeSpec node))
            {
                return default;
            }

            int bonusYield = masteryLevel >= 4 ? 1 : 0;
            return new GatherResult(node.OutputItemId, node.OutputCount + bonusYield);
        }
    }
}
