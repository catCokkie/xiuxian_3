using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public sealed class DropEconomyService
    {
        public delegate bool TryGetDropTableDelegate(string dropTableId, out Godot.Collections.Dictionary<string, Variant> dropTable);

        public static List<EquipmentDropResolutionRules.DropEntrySpec> BuildDropEntrySpecs(Godot.Collections.Array<Variant> entries)
        {
            var result = new List<EquipmentDropResolutionRules.DropEntrySpec>();
            foreach (Variant item in entries)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string entryType = LevelConfigProvider.GetString(dict, "entry_type", "item");
                string itemId = LevelConfigProvider.GetString(dict, "item_id", "");
                string equipmentTemplateId = LevelConfigProvider.GetString(dict, "equipment_template_id", "");
                int weight = Math.Max(0, dict.ContainsKey("weight") ? dict["weight"].AsInt32() : 0);
                int minQty = Math.Max(0, dict.ContainsKey("min_qty") ? dict["min_qty"].AsInt32() : 1);
                int maxQty = Math.Max(minQty, dict.ContainsKey("max_qty") ? dict["max_qty"].AsInt32() : minQty);
                result.Add(new EquipmentDropResolutionRules.DropEntrySpec(entryType, itemId, equipmentTemplateId, weight, minQty, maxQty));
            }

            return result;
        }

        public static void ReadPityConfig(
            Godot.Collections.Dictionary<string, Variant> table,
            out string pityCounterKey,
            out int pityThreshold,
            out string pityItemId,
            out int pityQty)
        {
            pityCounterKey = "";
            pityThreshold = 0;
            pityItemId = "";
            pityQty = 0;
            if (!LevelConfigProvider.TryGetChildDictionary(table, "pity", out var pity))
            {
                return;
            }

            pityCounterKey = LevelConfigProvider.GetString(pity, "counter_key", "");
            pityThreshold = pity.ContainsKey("threshold") ? pity["threshold"].AsInt32() : 0;
            pityItemId = LevelConfigProvider.GetString(pity, "item_id", "");
            pityQty = pity.ContainsKey("qty") ? pity["qty"].AsInt32() : 1;
        }

        public static int ReadDailyCap(Godot.Collections.Dictionary<string, Variant> table)
        {
            if (!LevelConfigProvider.TryGetChildDictionary(table, "economy", out var economy))
            {
                return 0;
            }

            return economy.ContainsKey("daily_cap_rolls") ? Math.Max(0, economy["daily_cap_rolls"].AsInt32()) : 0;
        }

        public static int ReadHourlySoftCap(Godot.Collections.Dictionary<string, Variant> table)
        {
            if (!LevelConfigProvider.TryGetChildDictionary(table, "economy", out var economy))
            {
                return 0;
            }

            return economy.ContainsKey("hourly_soft_cap_rolls") ? Math.Max(0, economy["hourly_soft_cap_rolls"].AsInt32()) : 0;
        }

        public static double ReadRepeatDecay(Godot.Collections.Dictionary<string, Variant> table)
        {
            if (!LevelConfigProvider.TryGetChildDictionary(table, "economy", out var economy) || !economy.ContainsKey("repeat_decay_factor"))
            {
                return 1.0;
            }

            return Math.Max(0.0, economy["repeat_decay_factor"].AsDouble());
        }

        public static void ApplyPity(
            string dropTableId,
            string pityCounterKey,
            int pityThreshold,
            string pityItemId,
            int pityQty,
            Dictionary<string, int> result,
            Dictionary<string, int> pityCounterByKey,
            Action<Dictionary<string, int>, string, int> addDrop,
            Action<string, int, bool> setPityDebug)
        {
            if (string.IsNullOrEmpty(dropTableId) || string.IsNullOrEmpty(pityCounterKey) || pityThreshold <= 0 || string.IsNullOrEmpty(pityItemId))
            {
                return;
            }

            bool hasPityItem = result.ContainsKey(pityItemId) && result[pityItemId] > 0;
            int current = pityCounterByKey.TryGetValue(pityCounterKey, out int saved) ? saved : 0;
            (int nextCounter, bool triggered, int addedQty) = LevelDropEconomyRules.ApplyPity(current, pityThreshold, hasPityItem, pityQty);
            if (triggered)
            {
                addDrop(result, pityItemId, addedQty);
            }

            pityCounterByKey[pityCounterKey] = nextCounter;
            setPityDebug(pityCounterKey, nextCounter, triggered);
        }

        public static bool TryConsumeDropRoll(
            Godot.Collections.Dictionary<string, Variant> table,
            string dropTableId,
            Dictionary<string, int> dailyRollCountByTable,
            Dictionary<string, long> dailyRollDayByTable,
            Dictionary<string, int> hourlyRollCountByTable,
            Dictionary<string, long> hourlyRollHourByTable,
            out int hourlyCountAfterConsume,
            out bool dailyCapBlocked)
        {
            hourlyCountAfterConsume = 0;
            dailyCapBlocked = false;
            long unix = (long)Time.GetUnixTimeFromSystem();
            int dailyCap = ReadDailyCap(table);
            bool hasSavedDay = dailyRollDayByTable.TryGetValue(dropTableId, out long savedDay);
            bool hasSavedHour = hourlyRollHourByTable.TryGetValue(dropTableId, out long savedHour);
            int dailyCount = dailyRollCountByTable.TryGetValue(dropTableId, out int d) ? d : 0;
            int hourlyCount = hourlyRollCountByTable.TryGetValue(dropTableId, out int h) ? h : 0;

            var result = LevelDropEconomyRules.ConsumeDropRoll(dailyCount, savedDay, dailyCap, hourlyCount, savedHour, unix, hasSavedDay, hasSavedHour);
            dailyRollCountByTable[dropTableId] = result.DailyCount;
            dailyRollDayByTable[dropTableId] = result.DayIndex;
            hourlyRollCountByTable[dropTableId] = result.HourlyCount;
            hourlyRollHourByTable[dropTableId] = result.HourIndex;
            hourlyCountAfterConsume = result.HourlyCountAfterConsume;
            dailyCapBlocked = result.DailyCapBlocked;
            return result.Allowed;
        }

        public static bool ShouldSkipDropBySoftCap(Godot.Collections.Dictionary<string, Variant> table, int hourlyCountAfterConsume, float randomValue)
        {
            int softCap = ReadHourlySoftCap(table);
            if (softCap <= 0 || hourlyCountAfterConsume <= softCap)
            {
                return false;
            }

            double decay = ReadRepeatDecay(table);
            return LevelDropEconomyRules.ShouldSkipDropBySoftCap(softCap, decay, hourlyCountAfterConsume, randomValue);
        }

        public static string ResolveDropTableForActiveLevel(
            string levelId,
            string monsterId,
            string configuredDropTableId,
            Dictionary<string, Godot.Collections.Dictionary<string, Variant>> dropTableById,
            TryGetDropTableDelegate tryGetDropTable)
        {
            bool configuredTableValidForLevel = !string.IsNullOrEmpty(configuredDropTableId)
                && tryGetDropTable(configuredDropTableId, out var configuredTable)
                && IsTableBoundToLevel(configuredTable, levelId);

            var candidates = new List<(string DropTableId, bool LevelMatch, bool MonsterMatch)>();
            foreach ((string dropTableId, Godot.Collections.Dictionary<string, Variant> table) in dropTableById)
            {
                candidates.Add((dropTableId, IsTableBoundToLevel(table, levelId), IsTableBoundToMonster(table, monsterId)));
            }

            return DropTableBindingRules.ResolveDropTableForActiveLevel(levelId, monsterId, configuredDropTableId, configuredTableValidForLevel, candidates);
        }

        private static bool IsTableBoundToLevel(Godot.Collections.Dictionary<string, Variant> table, string levelId)
        {
            string boundLevelId = LevelConfigProvider.GetString(table, "bind_level_id", "");
            return DropTableBindingRules.IsTableBoundToLevel(boundLevelId, levelId);
        }

        private static bool IsTableBoundToMonster(Godot.Collections.Dictionary<string, Variant> table, string monsterId)
        {
            if (!table.ContainsKey("bind_monster_ids") || table["bind_monster_ids"].VariantType != Variant.Type.Array)
            {
                return DropTableBindingRules.IsTableBoundToMonster(Array.Empty<string>(), monsterId);
            }

            var bindArray = (Godot.Collections.Array<Variant>)table["bind_monster_ids"];
            var boundMonsterIds = new List<string>();
            foreach (Variant item in bindArray)
            {
                string id = item.AsString();
                if (!string.IsNullOrEmpty(id))
                {
                    boundMonsterIds.Add(id);
                }
            }

            return DropTableBindingRules.IsTableBoundToMonster(boundMonsterIds, monsterId);
        }
    }
}
