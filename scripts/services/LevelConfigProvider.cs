using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public sealed class LevelConfigProvider
    {
        public Godot.Collections.Dictionary<string, Variant> RootData { get; private set; } = new();
        public List<Godot.Collections.Dictionary<string, Variant>> Levels { get; } = new();
        public Dictionary<string, Godot.Collections.Dictionary<string, Variant>> MonsterById { get; } = new();
        public Dictionary<string, Godot.Collections.Dictionary<string, Variant>> DropTableById { get; } = new();
        public Dictionary<string, Godot.Collections.Dictionary<string, Variant>> EquipmentSeriesById { get; } = new();
        public Dictionary<string, Godot.Collections.Dictionary<string, Variant>> EquipmentTemplateById { get; } = new();
        public Dictionary<string, List<Godot.Collections.Dictionary<string, Variant>>> EquipmentExchangeRecipesByLevelId { get; } = new();
        public string LastLoadedConfigText { get; private set; } = string.Empty;

        public bool LoadConfigFromText(string text)
        {
            MonsterById.Clear();
            DropTableById.Clear();
            EquipmentSeriesById.Clear();
            EquipmentTemplateById.Clear();
            EquipmentExchangeRecipesByLevelId.Clear();

            Variant parsed = Json.ParseString(text);
            if (parsed.VariantType != Variant.Type.Dictionary)
            {
                return false;
            }

            RootData = (Godot.Collections.Dictionary<string, Variant>)parsed;
            LastLoadedConfigText = text;
            ParseLevelsSection();
            IndexMonsters();
            IndexDropTables();
            IndexEquipmentSeries();
            IndexEquipmentTemplates();
            IndexEquipmentExchangeRecipes();
            return true;
        }

        public bool TryGetMonster(string monsterId, out Godot.Collections.Dictionary<string, Variant> monsterData)
        {
            if (MonsterById.TryGetValue(monsterId, out monsterData))
            {
                return true;
            }

            monsterData = new Godot.Collections.Dictionary<string, Variant>();
            return false;
        }

        public bool TryGetDropTable(string dropTableId, out Godot.Collections.Dictionary<string, Variant> dropTableData)
        {
            if (DropTableById.TryGetValue(dropTableId, out dropTableData))
            {
                return true;
            }

            dropTableData = new Godot.Collections.Dictionary<string, Variant>();
            return false;
        }

        public Godot.Collections.Array<string> GetLevelIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (var level in Levels)
            {
                string levelId = GetString(level, "level_id", "");
                if (!string.IsNullOrEmpty(levelId))
                {
                    result.Add(levelId);
                }
            }

            return result;
        }

        public string GetLevelName(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return string.Empty;
            }

            foreach (var level in Levels)
            {
                string id = GetString(level, "level_id", "");
                if (id == levelId)
                {
                    return GetString(level, "level_name", levelId);
                }
            }

            return string.Empty;
        }

        public Godot.Collections.Array<string> GetEquipmentSeriesIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (string id in EquipmentSeriesById.Keys)
            {
                result.Add(id);
            }

            return result;
        }

        public Godot.Collections.Array<string> GetEquipmentTemplateIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (string id in EquipmentTemplateById.Keys)
            {
                result.Add(id);
            }

            return result;
        }

        public Godot.Collections.Array<string> GetEquipmentExchangeLevelIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (string id in EquipmentExchangeRecipesByLevelId.Keys)
            {
                result.Add(id);
            }

            return result;
        }

        public bool TryGetEquipmentSeries(string seriesId, out Godot.Collections.Dictionary<string, Variant> seriesData)
        {
            if (EquipmentSeriesById.TryGetValue(seriesId, out seriesData))
            {
                seriesData = new Godot.Collections.Dictionary<string, Variant>(seriesData);
                return true;
            }

            seriesData = new Godot.Collections.Dictionary<string, Variant>();
            return false;
        }

        public bool TryGetEquipmentTemplate(string templateId, out Godot.Collections.Dictionary<string, Variant> templateData)
        {
            if (EquipmentTemplateById.TryGetValue(templateId, out templateData))
            {
                templateData = new Godot.Collections.Dictionary<string, Variant>(templateData);
                return true;
            }

            templateData = new Godot.Collections.Dictionary<string, Variant>();
            return false;
        }

        public Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> GetEquipmentExchangeRecipes(string levelId)
        {
            var result = new Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>>();
            if (string.IsNullOrEmpty(levelId) || !EquipmentExchangeRecipesByLevelId.TryGetValue(levelId, out List<Godot.Collections.Dictionary<string, Variant>> recipes))
            {
                return result;
            }

            foreach (var recipe in recipes)
            {
                result.Add(new Godot.Collections.Dictionary<string, Variant>(recipe));
            }

            return result;
        }

        private void ParseLevelsSection()
        {
            Levels.Clear();
            if (RootData.ContainsKey("levels"))
            {
                Variant levelsVariant = RootData["levels"];
                if (levelsVariant.VariantType == Variant.Type.Array)
                {
                    var levels = (Godot.Collections.Array<Variant>)levelsVariant;
                    foreach (Variant item in levels)
                    {
                        if (item.VariantType == Variant.Type.Dictionary)
                        {
                            Levels.Add((Godot.Collections.Dictionary<string, Variant>)item);
                        }
                    }
                }
            }

            if (Levels.Count == 0 && TryGetChildDictionary(RootData, "level", out var singleLevel))
            {
                Levels.Add(singleLevel);
            }
        }

        private void IndexMonsters()
        {
            if (!RootData.ContainsKey("monsters"))
            {
                return;
            }

            Variant monstersVariant = RootData["monsters"];
            if (monstersVariant.VariantType != Variant.Type.Array)
            {
                return;
            }

            var monsters = (Godot.Collections.Array<Variant>)monstersVariant;
            foreach (Variant item in monsters)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string id = GetString(dict, "monster_id", "");
                if (!string.IsNullOrEmpty(id))
                {
                    MonsterById[id] = dict;
                }
            }
        }

        private void IndexDropTables()
        {
            if (!RootData.ContainsKey("drop_tables"))
            {
                return;
            }

            Variant dropTablesVariant = RootData["drop_tables"];
            if (dropTablesVariant.VariantType != Variant.Type.Array)
            {
                return;
            }

            var tables = (Godot.Collections.Array<Variant>)dropTablesVariant;
            foreach (Variant item in tables)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string id = GetString(dict, "drop_table_id", "");
                if (!string.IsNullOrEmpty(id))
                {
                    DropTableById[id] = dict;
                }
            }
        }

        private void IndexEquipmentSeries()
        {
            EquipmentSeriesById.Clear();
            var indexes = EquipmentConfigTextParser.ParseIndexes(LastLoadedConfigText);
            foreach (var kv in indexes.SeriesJsonById)
            {
                Variant parsed = Json.ParseString(kv.Value);
                if (parsed.VariantType == Variant.Type.Dictionary)
                {
                    EquipmentSeriesById[kv.Key] = (Godot.Collections.Dictionary<string, Variant>)parsed;
                }
            }
        }

        private void IndexEquipmentTemplates()
        {
            EquipmentTemplateById.Clear();
            var indexes = EquipmentConfigTextParser.ParseIndexes(LastLoadedConfigText);
            foreach (var kv in indexes.TemplateJsonById)
            {
                Variant parsed = Json.ParseString(kv.Value);
                if (parsed.VariantType == Variant.Type.Dictionary)
                {
                    EquipmentTemplateById[kv.Key] = (Godot.Collections.Dictionary<string, Variant>)parsed;
                }
            }
        }

        private void IndexEquipmentExchangeRecipes()
        {
            EquipmentExchangeRecipesByLevelId.Clear();
            var indexes = EquipmentConfigTextParser.ParseIndexes(LastLoadedConfigText);
            foreach (var kv in indexes.ExchangeRecipeJsonByLevelId)
            {
                var list = new List<Godot.Collections.Dictionary<string, Variant>>();
                foreach (string recipeJson in kv.Value)
                {
                    Variant parsed = Json.ParseString(recipeJson);
                    if (parsed.VariantType == Variant.Type.Dictionary)
                    {
                        list.Add((Godot.Collections.Dictionary<string, Variant>)parsed);
                    }
                }

                EquipmentExchangeRecipesByLevelId[kv.Key] = list;
            }
        }

        public static bool TryGetChildDictionary(
            Godot.Collections.Dictionary<string, Variant> parent,
            string key,
            out Godot.Collections.Dictionary<string, Variant> child)
        {
            child = new Godot.Collections.Dictionary<string, Variant>();
            if (!parent.ContainsKey(key))
            {
                return false;
            }

            Variant value = parent[key];
            if (value.VariantType != Variant.Type.Dictionary)
            {
                return false;
            }

            child = (Godot.Collections.Dictionary<string, Variant>)value;
            return true;
        }

        public static string GetString(Godot.Collections.Dictionary<string, Variant> dict, string key, string fallback)
        {
            return dict.ContainsKey(key) ? dict[key].AsString() : fallback;
        }

        public static double GetDouble(Godot.Collections.Dictionary<string, Variant> dict, string key, double fallback)
        {
            return dict.ContainsKey(key) ? dict[key].AsDouble() : fallback;
        }

        public static Color ParseColorVariant(Variant value, Color fallback)
        {
            if (value.VariantType == Variant.Type.Array)
            {
                var arr = (Godot.Collections.Array<Variant>)value;
                if (arr.Count >= 3)
                {
                    float r = (float)arr[0].AsDouble();
                    float g = (float)arr[1].AsDouble();
                    float b = (float)arr[2].AsDouble();
                    float a = arr.Count >= 4 ? (float)arr[3].AsDouble() : 1.0f;
                    return new Color(r, g, b, a);
                }
            }

            if (value.VariantType == Variant.Type.String)
            {
                string html = value.AsString();
                if (!string.IsNullOrEmpty(html))
                {
                    try
                    {
                        return Color.FromHtml(html);
                    }
                    catch (Exception)
                    {
                        return fallback;
                    }
                }
            }

            return fallback;
        }
    }
}
