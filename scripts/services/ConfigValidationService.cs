using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xiuxian.Scripts.Services
{
    public sealed class ConfigValidationService
    {
        private readonly LevelConfigProvider _provider;
        private readonly Func<string> _getActiveLevelId;
        private readonly Func<string, string> _getLevelName;
        private readonly Func<string, bool> _isLevelUnlocked;

        private readonly List<string> _validationIssues = new();
        private readonly List<Godot.Collections.Dictionary<string, Variant>> _validationEntries = new();
        private string _lastSimulationReport = "no simulation yet";

        public ConfigValidationService(
            LevelConfigProvider provider,
            Func<string> getActiveLevelId,
            Func<string, string> getLevelName,
            Func<string, bool> isLevelUnlocked)
        {
            _provider = provider;
            _getActiveLevelId = getActiveLevelId;
            _getLevelName = getLevelName;
            _isLevelUnlocked = isLevelUnlocked;
        }

        public int ValidationIssueCount => _validationIssues.Count;

        public void ValidateConfiguration()
        {
            _validationIssues.Clear();
            _validationEntries.Clear();

            if (_provider.Levels.Count == 0)
            {
                AddValidationIssue("config", "root", "levels", "missing levels array");
                return;
            }

            var levelIds = new HashSet<string>();
            foreach (var level in _provider.Levels)
            {
                string levelId = LevelConfigProvider.GetString(level, "level_id", string.Empty);
                if (string.IsNullOrEmpty(levelId))
                {
                    AddValidationIssue("level", "(missing)", "level_id", "missing");
                    continue;
                }

                if (!levelIds.Add(levelId))
                {
                    AddValidationIssue("level", levelId, "level_id", "duplicate level id", levelId: levelId);
                }

                ValidateLevelSpawnTable(level, levelId);
            }

            ValidateMonsters();
            ValidateDropTables(levelIds);
            ValidateEquipmentConfiguration();
        }

        public string BuildValidationSummary(int maxLines = 12)
        {
            if (_validationIssues.Count == 0)
            {
                return "Validation: OK";
            }

            int lines = Math.Max(1, maxLines);
            var sb = new StringBuilder();
            sb.Append($"Validation: {_validationIssues.Count} issue(s)");
            for (int i = 0; i < _validationIssues.Count && i < lines; i++)
            {
                sb.Append($"\n- {_validationIssues[i]}");
            }

            if (_validationIssues.Count > lines)
            {
                sb.Append($"\n... {_validationIssues.Count - lines} more");
            }

            return sb.ToString();
        }

        public string BuildLevelPreviewSummary(int maxLines, Godot.Collections.Array<string> levelIds)
        {
            if (levelIds.Count == 0)
            {
                return "No levels loaded.";
            }

            int shown = Math.Max(1, maxLines);
            var sb = new StringBuilder();
            sb.Append("Loaded Levels");
            for (int i = 0; i < levelIds.Count && i < shown; i++)
            {
                string levelId = levelIds[i];
                string levelName = _getLevelName(levelId);
                bool active = levelId == _getActiveLevelId();
                bool unlocked = _isLevelUnlocked(levelId);
                string flag = active ? "*" : (unlocked ? "O" : "X");
                sb.Append($"\n{flag} {levelId} {levelName}");
            }

            if (levelIds.Count > shown)
            {
                sb.Append($"\n... {levelIds.Count - shown} more");
            }

            sb.Append("\nLegend: *=active, O=unlocked, X=locked");
            return sb.ToString();
        }

        public Godot.Collections.Array<string> GetValidationIssues()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (string issue in _validationIssues)
            {
                result.Add(issue);
            }

            return result;
        }

        public Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> GetValidationEntries()
        {
            var result = new Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>>();
            foreach (var entry in _validationEntries)
            {
                result.Add(new Godot.Collections.Dictionary<string, Variant>(entry));
            }

            return result;
        }

        public void SetLastSimulationReport(string report)
        {
            _lastSimulationReport = string.IsNullOrEmpty(report) ? "no simulation yet" : report;
        }

        public string GetLastSimulationReport()
        {
            return _lastSimulationReport;
        }

        private void ValidateEquipmentConfiguration()
        {
            if (_provider.EquipmentSeriesById.Count == 0)
            {
                AddValidationIssue("equipment", "series", "equipment_series", "no equipment series indexed", severity: "warning");
            }
        }

        private void ValidateLevelSpawnTable(Godot.Collections.Dictionary<string, Variant> level, string levelId)
        {
            if (!level.ContainsKey("spawn_table") || level["spawn_table"].VariantType != Variant.Type.Array)
            {
                AddValidationIssue("level", levelId, "spawn_table", "missing or not array", levelId: levelId);
                return;
            }

            var spawnTable = (Godot.Collections.Array<Variant>)level["spawn_table"];
            if (spawnTable.Count == 0)
            {
                AddValidationIssue("level", levelId, "spawn_table", "is empty", levelId: levelId);
            }

            int totalWeight = 0;
            for (int i = 0; i < spawnTable.Count; i++)
            {
                if (spawnTable[i].VariantType != Variant.Type.Dictionary)
                {
                    AddValidationIssue("level", levelId, $"spawn_table[{i}]", "is not dictionary", levelId: levelId);
                    continue;
                }

                var entry = (Godot.Collections.Dictionary<string, Variant>)spawnTable[i];
                string monsterId = LevelConfigProvider.GetString(entry, "monster_id", string.Empty);
                int weight = Math.Max(0, entry.ContainsKey("weight") ? entry["weight"].AsInt32() : 0);
                totalWeight += weight;

                if (string.IsNullOrEmpty(monsterId))
                {
                    AddValidationIssue("level", levelId, $"spawn_table[{i}].monster_id", "missing", levelId: levelId);
                }
                else if (!_provider.MonsterById.ContainsKey(monsterId))
                {
                    AddValidationIssue("level", levelId, $"spawn_table[{i}].monster_id", $"monster '{monsterId}' not found", levelId: levelId, monsterId: monsterId);
                }

                if (weight <= 0)
                {
                    AddValidationIssue("level", levelId, $"spawn_table[{i}].weight", "weight <= 0", levelId: levelId);
                }
            }

            if (totalWeight <= 0)
            {
                AddValidationIssue("level", levelId, "spawn_table.total_weight", "total weight <= 0", levelId: levelId);
            }
        }

        private void ValidateMonsters()
        {
            foreach ((string monsterId, Godot.Collections.Dictionary<string, Variant> monster) in _provider.MonsterById)
            {
                if (!monster.ContainsKey("monster_name"))
                {
                    AddValidationIssue("monster", monsterId, "monster_name", "missing", monsterId: monsterId);
                }

                if (!LevelConfigProvider.TryGetChildDictionary(monster, "combat", out _))
                {
                    AddValidationIssue("monster", monsterId, "combat", "missing or not dictionary", monsterId: monsterId);
                }
            }
        }

        private void ValidateDropTables(HashSet<string> levelIds)
        {
            foreach ((string tableId, Godot.Collections.Dictionary<string, Variant> table) in _provider.DropTableById)
            {
                if (!table.ContainsKey("entries") || table["entries"].VariantType != Variant.Type.Array)
                {
                    AddValidationIssue("drop_table", tableId, "entries", "missing or not array", dropTableId: tableId);
                    continue;
                }

                if (table.ContainsKey("bind_level_id"))
                {
                    string levelId = table["bind_level_id"].AsString();
                    if (!string.IsNullOrEmpty(levelId) && !levelIds.Contains(levelId))
                    {
                        AddValidationIssue("drop_table", tableId, "bind_level_id", $"level '{levelId}' not found", dropTableId: tableId, levelId: levelId);
                    }
                }

                var entries = (Godot.Collections.Array<Variant>)table["entries"];
                if (entries.Count == 0)
                {
                    AddValidationIssue("drop_table", tableId, "entries", "is empty", dropTableId: tableId);
                    continue;
                }

                int totalWeight = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].VariantType != Variant.Type.Dictionary)
                    {
                        AddValidationIssue("drop_table", tableId, $"entries[{i}]", "is not dictionary", dropTableId: tableId);
                        continue;
                    }

                    var entry = (Godot.Collections.Dictionary<string, Variant>)entries[i];
                    string entryType = LevelConfigProvider.GetString(entry, "entry_type", "item");
                    string itemId = LevelConfigProvider.GetString(entry, "item_id", string.Empty);
                    string equipmentTemplateId = LevelConfigProvider.GetString(entry, "equipment_template_id", string.Empty);
                    int weight = Math.Max(0, entry.ContainsKey("weight") ? entry["weight"].AsInt32() : 0);
                    int qtyMin = Math.Max(0, entry.ContainsKey("qty_min") ? entry["qty_min"].AsInt32() : 0);
                    int qtyMax = Math.Max(0, entry.ContainsKey("qty_max") ? entry["qty_max"].AsInt32() : 0);
                    totalWeight += weight;

                    if (entryType == "equipment_template")
                    {
                        if (string.IsNullOrEmpty(equipmentTemplateId))
                        {
                            AddValidationIssue("drop_table", tableId, $"entries[{i}].equipment_template_id", "missing", dropTableId: tableId);
                        }
                        else if (!_provider.EquipmentTemplateById.ContainsKey(equipmentTemplateId))
                        {
                            AddValidationIssue("drop_table", tableId, $"entries[{i}].equipment_template_id", $"equipment template '{equipmentTemplateId}' not found", dropTableId: tableId);
                        }
                    }
                    else if (string.IsNullOrEmpty(itemId))
                    {
                        AddValidationIssue("drop_table", tableId, $"entries[{i}].item_id", "missing", dropTableId: tableId);
                    }

                    if (weight <= 0)
                    {
                        AddValidationIssue("drop_table", tableId, $"entries[{i}].weight", "weight <= 0", dropTableId: tableId);
                    }

                    if (qtyMax < qtyMin)
                    {
                        AddValidationIssue("drop_table", tableId, $"entries[{i}].qty_max", "qty_max < qty_min", dropTableId: tableId);
                    }
                }

                if (totalWeight <= 0)
                {
                    AddValidationIssue("drop_table", tableId, "entries.total_weight", "total weight <= 0", dropTableId: tableId);
                }
            }
        }

        private void AddValidationIssue(
            string scope,
            string id,
            string field,
            string message,
            string severity = "error",
            string levelId = "",
            string monsterId = "",
            string dropTableId = "")
        {
            var entry = new Godot.Collections.Dictionary<string, Variant>
            {
                ["scope"] = string.IsNullOrEmpty(scope) ? "config" : scope,
                ["id"] = string.IsNullOrEmpty(id) ? "(unknown)" : id,
                ["field"] = string.IsNullOrEmpty(field) ? "(unknown)" : field,
                ["severity"] = string.IsNullOrEmpty(severity) ? "error" : severity,
                ["message"] = string.IsNullOrEmpty(message) ? "validation failed" : message,
                ["level_id"] = levelId,
                ["monster_id"] = monsterId,
                ["drop_table_id"] = dropTableId,
            };

            _validationEntries.Add(entry);
            _validationIssues.Add(BuildValidationIssueMessage(entry));
        }

        private static string BuildValidationIssueMessage(Godot.Collections.Dictionary<string, Variant> entry)
        {
            string scope = entry.ContainsKey("scope") ? entry["scope"].AsString() : "config";
            string id = entry.ContainsKey("id") ? entry["id"].AsString() : "(unknown)";
            string field = entry.ContainsKey("field") ? entry["field"].AsString() : "(unknown)";
            string message = entry.ContainsKey("message") ? entry["message"].AsString() : "validation failed";
            return $"{scope} {id}: {field} {message}.";
        }
    }
}
