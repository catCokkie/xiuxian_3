using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public partial class LevelConfigLoader
    {
        private Dictionary<string, Godot.Collections.Dictionary<string, Variant>> GetOfflineDropTables(string levelId)
        {
            var tables = new Dictionary<string, Godot.Collections.Dictionary<string, Variant>>();
            foreach (string monsterId in GetSpawnMonsterIds(levelId))
            {
                if (!TryGetMonster(monsterId, out var monster) || !LevelConfigProvider.TryGetChildDictionary(monster, "drops", out var drops)) continue;
                string tableId = _runtimeStateService.ResolveDropTableForLevel(levelId, monsterId, LevelConfigProvider.GetString(drops, "drop_table_id", ""));
                if (TryGetDropTable(tableId, out var table)) tables[tableId] = table;
            }
            return tables;
        }

        public Func<int, EquipmentInstanceData>? BuildOfflineEquipmentFactory(string levelId)
        {
            var pool = new List<(EquipmentGenerationRules.EquipmentTemplateGenerationSpec spec, int weight)>();
            int total = 0;
            foreach (var table in GetOfflineDropTables(levelId).Values)
            {
                if (!table.ContainsKey("entries")) continue;
                foreach (var entry in DropEconomyService.BuildDropEntrySpecs((Godot.Collections.Array<Variant>)table["entries"]))
                {
                    if (entry.EntryType != "equipment_template" || entry.Weight <= 0 || !TryGetEquipmentTemplate(entry.EquipmentTemplateId, out var template)) continue;
                    pool.Add((EquipmentGenerationRules.FromTemplateDictionary(template), entry.Weight));
                    total += entry.Weight;
                }
            }
            if (total <= 0) return null;
            return index =>
            {
                int roll = Random.Shared.Next(total);
                var selected = pool[0].spec;
                foreach (var item in pool) { if (roll < item.weight) { selected = item.spec; break; } roll -= item.weight; }
                return EquipmentGenerationRules.GenerateFromSpec(selected, levelId, EquipmentSourceStage.Normal,
                    uniqueSuffix: $"offline_{Guid.NewGuid():N}", pickIndex: max => Random.Shared.Next(1, max + 1),
                    rollValue: (min, max) => min + Random.Shared.NextDouble() * (max - min));
            };
        }

        public void ConsumeOfflineDropRolls(string levelId, int count)
            => _runtimeStateService.ConsumeOfflineDropRolls(GetOfflineDropTables(levelId), count);

        public EquipmentInstanceData? RollSunkenEquipment()
        {
            var templates = new List<EquipmentGenerationRules.EquipmentTemplateGenerationSpec>();
            foreach (string id in GetEquipmentTemplateIds())
                if (TryGetEquipmentTemplate(id, out var template) && LevelConfigProvider.GetString(template, "series_id", "") == "series_sunken_pearl")
                    templates.Add(EquipmentGenerationRules.FromTemplateDictionary(template));
            if (templates.Count == 0) return null;
            return EquipmentGenerationRules.GenerateFromSpec(templates[Random.Shared.Next(templates.Count)], ActiveLevelId, EquipmentSourceStage.Fishing,
                uniqueSuffix: $"fishing_{Guid.NewGuid():N}", pickIndex: max => Random.Shared.Next(1, max + 1),
                rollValue: (min, max) => min + Random.Shared.NextDouble() * (max - min));
        }
    }
}
