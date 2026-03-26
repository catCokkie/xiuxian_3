using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Central catalogue of the 16 core material types and their source / consumer systems.
    /// </summary>
    public static class MaterialRegistry
    {
        public readonly record struct MaterialDef(
            string ItemId,
            string DisplayName,
            string[] SourceSystems,
            string[] ConsumerSystems);

        private static readonly MaterialDef[] Materials =
        {
            // --- 采集类: 灵田 ---
            new("spirit_herb",    "灵草", new[] { "dungeon", "garden" }, new[] { "alchemy" }),
            new("spirit_flower",  "灵花", new[] { "garden" },           new[] { "alchemy", "cooking" }),
            new("spirit_fruit",   "灵果", new[] { "garden" },           new[] { "cooking" }),
            new("seed",           "种子", new[] { "garden" },           new[] { "garden" }),

            // --- 采集类: 矿脉 ---
            new("cold_iron_ore",  "寒铁矿", new[] { "mining" },         new[] { "smithing" }),
            new("spirit_jade",    "灵玉",   new[] { "mining" },         new[] { "smithing", "formation" }),
            new("mithril",        "秘银",   new[] { "mining" },         new[] { "smithing" }),
            new("fossil",         "化石",   new[] { "mining" },         new[] { "alchemy" }),

            // --- 采集类: 灵渔 ---
            new("spirit_fish",    "灵鱼",   new[] { "fishing" },        new[] { "cooking" }),
            new("spirit_pearl",   "灵珠",   new[] { "fishing" },        new[] { "formation", "alchemy" }),
            new("dragon_saliva",  "龙涎",   new[] { "fishing" },        new[] { "alchemy" }),

            // --- 战斗掉落 ---
            new("lingqi_shard",      "碎片",   new[] { "dungeon" }, new[] { "smithing" }),
            new("broken_talisman",   "碎符",   new[] { "dungeon" }, new[] { "smithing", "talisman" }),
            new("spirit_ink",        "灵墨",   new[] { "dungeon" }, new[] { "talisman" }),
            new("beast_bone",        "兽骨",   new[] { "dungeon" }, new[] { "alchemy", "cooking" }),
            new("toxic_gland",       "毒腺",   new[] { "dungeon" }, new[] { "alchemy" }),
        };

        private static readonly Dictionary<string, MaterialDef> Index;

        static MaterialRegistry()
        {
            Index = new Dictionary<string, MaterialDef>(Materials.Length, StringComparer.Ordinal);
            for (int i = 0; i < Materials.Length; i++)
            {
                Index[Materials[i].ItemId] = Materials[i];
            }
        }

        public static IReadOnlyList<MaterialDef> GetAll() => Materials;

        public static bool TryGet(string itemId, out MaterialDef def)
        {
            return Index.TryGetValue(itemId, out def);
        }

        public static int Count => Materials.Length;
    }
}
