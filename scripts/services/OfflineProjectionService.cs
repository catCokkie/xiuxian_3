using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public sealed class OfflineProjectionService
    {
        public delegate bool TryGetMonsterStatProfileDelegate(string monsterId, out MonsterStatProfile profile);
        public delegate bool TryGetMonsterDelegate(string monsterId, out Godot.Collections.Dictionary<string, Variant> monster);
        public delegate bool TryGetDropTableDelegate(string dropTableId, out Godot.Collections.Dictionary<string, Variant> dropTable);
        public delegate string ResolveDropTableDelegate(string monsterId, string configuredDropTableId);
        public delegate List<EquipmentDropResolutionRules.DropEntrySpec> BuildDropEntrySpecsDelegate(Godot.Collections.Array<Variant> entries);
        public delegate bool TryFindLevelIndexDelegate(string levelId, out int levelIndex);

        private readonly LevelConfigProvider _provider;
        private readonly Func<int> _getActiveLevelIndex;
        private readonly TryFindLevelIndexDelegate _tryFindLevelIndex;
        private readonly TryGetMonsterStatProfileDelegate _tryGetMonsterStatProfile;
        private readonly TryGetMonsterDelegate _tryGetMonster;
        private readonly TryGetDropTableDelegate _tryGetDropTable;
        private readonly ResolveDropTableDelegate _resolveDropTableForActiveLevel;
        private readonly BuildDropEntrySpecsDelegate _buildDropEntrySpecs;

        public OfflineProjectionService(
            LevelConfigProvider provider,
            Func<int> getActiveLevelIndex,
            TryFindLevelIndexDelegate tryFindLevelIndex,
            TryGetMonsterStatProfileDelegate tryGetMonsterStatProfile,
            TryGetMonsterDelegate tryGetMonster,
            TryGetDropTableDelegate tryGetDropTable,
            ResolveDropTableDelegate resolveDropTableForActiveLevel,
            BuildDropEntrySpecsDelegate buildDropEntrySpecs)
        {
            _provider = provider;
            _getActiveLevelIndex = getActiveLevelIndex;
            _tryFindLevelIndex = tryFindLevelIndex;
            _tryGetMonsterStatProfile = tryGetMonsterStatProfile;
            _tryGetMonster = tryGetMonster;
            _tryGetDropTable = tryGetDropTable;
            _resolveDropTableForActiveLevel = resolveDropTableForActiveLevel;
            _buildDropEntrySpecs = buildDropEntrySpecs;
        }

        public List<DungeonOfflineProjectionRules.MonsterSettlementSpec> BuildOfflineMonsterSettlementSpecs(string levelId = "")
        {
            var result = new List<DungeonOfflineProjectionRules.MonsterSettlementSpec>();
            if (_provider.Levels.Count == 0)
            {
                return result;
            }

            int levelIndex = _getActiveLevelIndex();
            if (!string.IsNullOrEmpty(levelId) && _tryFindLevelIndex(levelId, out int found))
            {
                levelIndex = found;
            }

            var level = _provider.Levels[Math.Clamp(levelIndex, 0, _provider.Levels.Count - 1)];
            if (!level.ContainsKey("spawn_table") || level["spawn_table"].VariantType != Variant.Type.Array)
            {
                return result;
            }

            var spawnTable = (Godot.Collections.Array<Variant>)level["spawn_table"];
            foreach (Variant item in spawnTable)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string monsterId = LevelConfigProvider.GetString(dict, "monster_id", "");
                int weight = Math.Max(0, dict.ContainsKey("weight") ? dict["weight"].AsInt32() : 0);
                if (string.IsNullOrEmpty(monsterId) || weight <= 0 || !_tryGetMonsterStatProfile(monsterId, out MonsterStatProfile monsterProfile))
                {
                    continue;
                }

                double averageLingqi = 0.0;
                double averageInsight = 0.0;
                if (_tryGetMonster(monsterId, out var monster) && LevelConfigProvider.TryGetChildDictionary(monster, "settlement_reward", out var settlement))
                {
                    int lingqiMin = settlement.ContainsKey("lingqi_min") ? settlement["lingqi_min"].AsInt32() : 0;
                    int lingqiMax = settlement.ContainsKey("lingqi_max") ? settlement["lingqi_max"].AsInt32() : lingqiMin;
                    int insightMin = settlement.ContainsKey("insight_min") ? settlement["insight_min"].AsInt32() : 0;
                    int insightMax = settlement.ContainsKey("insight_max") ? settlement["insight_max"].AsInt32() : insightMin;
                    averageLingqi = (lingqiMin + lingqiMax) / 2.0;
                    averageInsight = (insightMin + insightMax) / 2.0;
                }

                result.Add(new DungeonOfflineProjectionRules.MonsterSettlementSpec(
                    weight,
                    monsterProfile,
                    averageLingqi,
                    averageInsight,
                    BuildOfflineAverageItemDrops(monsterId),
                    BuildOfflineAverageEquipmentDrops(monsterId)));
            }

            return result;
        }

        private Dictionary<string, double> BuildOfflineAverageItemDrops(string monsterId)
        {
            var result = new Dictionary<string, double>();
            if (!_tryGetMonster(monsterId, out var monster) || !LevelConfigProvider.TryGetChildDictionary(monster, "drops", out var drops))
            {
                return result;
            }

            string configuredDropTableId = LevelConfigProvider.GetString(drops, "drop_table_id", "");
            string dropTableId = _resolveDropTableForActiveLevel(monsterId, configuredDropTableId);
            int dropRollCount = Math.Max(0, drops.ContainsKey("drop_roll_count") ? drops["drop_roll_count"].AsInt32() : 1);
            if (!string.IsNullOrEmpty(dropTableId) && _tryGetDropTable(dropTableId, out var table) && table.ContainsKey("entries") && table["entries"].VariantType == Variant.Type.Array)
            {
                var entries = (Godot.Collections.Array<Variant>)table["entries"];
                List<EquipmentDropResolutionRules.DropEntrySpec> specs = _buildDropEntrySpecs(entries);
                int totalWeight = 0;
                for (int i = 0; i < specs.Count; i++)
                {
                    totalWeight += Math.Max(0, specs[i].Weight);
                }

                if (totalWeight > 0)
                {
                    for (int i = 0; i < specs.Count; i++)
                    {
                        EquipmentDropResolutionRules.DropEntrySpec spec = specs[i];
                        if (spec.EntryType != "item" || string.IsNullOrEmpty(spec.ItemId) || spec.Weight <= 0)
                        {
                            continue;
                        }

                        double avgQty = (spec.MinQty + spec.MaxQty) / 2.0;
                        double expected = dropRollCount * (spec.Weight / (double)totalWeight) * avgQty;
                        result[spec.ItemId] = result.TryGetValue(spec.ItemId, out double current) ? current + expected : expected;
                    }
                }
            }

            if (drops.ContainsKey("guaranteed_drop") && drops["guaranteed_drop"].VariantType == Variant.Type.Array)
            {
                foreach (Variant item in (Godot.Collections.Array<Variant>)drops["guaranteed_drop"])
                {
                    if (item.VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                    string itemId = LevelConfigProvider.GetString(dict, "item_id", "");
                    int qty = Math.Max(0, dict.ContainsKey("qty") ? dict["qty"].AsInt32() : 0);
                    if (!string.IsNullOrEmpty(itemId) && qty > 0)
                    {
                        result[itemId] = result.TryGetValue(itemId, out double current) ? current + qty : qty;
                    }
                }
            }

            return result;
        }

        private double BuildOfflineAverageEquipmentDrops(string monsterId)
        {
            if (!_tryGetMonster(monsterId, out var monster) || !LevelConfigProvider.TryGetChildDictionary(monster, "drops", out var drops))
            {
                return 0.0;
            }

            string configuredDropTableId = LevelConfigProvider.GetString(drops, "drop_table_id", "");
            string dropTableId = _resolveDropTableForActiveLevel(monsterId, configuredDropTableId);
            int dropRollCount = Math.Max(0, drops.ContainsKey("drop_roll_count") ? drops["drop_roll_count"].AsInt32() : 1);
            if (string.IsNullOrEmpty(dropTableId) || !_tryGetDropTable(dropTableId, out var table) || !table.ContainsKey("entries") || table["entries"].VariantType != Variant.Type.Array)
            {
                return 0.0;
            }

            var entries = (Godot.Collections.Array<Variant>)table["entries"];
            List<EquipmentDropResolutionRules.DropEntrySpec> specs = _buildDropEntrySpecs(entries);
            int totalWeight = 0;
            int equipmentWeight = 0;
            for (int i = 0; i < specs.Count; i++)
            {
                totalWeight += Math.Max(0, specs[i].Weight);
                if (specs[i].EntryType == "equipment_template")
                {
                    equipmentWeight += Math.Max(0, specs[i].Weight);
                }
            }

            if (totalWeight <= 0 || equipmentWeight <= 0)
            {
                return 0.0;
            }

            return dropRollCount * (equipmentWeight / (double)totalWeight);
        }
    }
}
