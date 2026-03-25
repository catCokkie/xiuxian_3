using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xiuxian.Scripts.Services
{
    public sealed class LevelRuntimeStateService
    {
        private readonly LevelConfigProvider _provider;
        private readonly ActiveLevelManager _activeLevelManager;
        private readonly RandomNumberGenerator _rng;
        private readonly BattleSimulationService _battleSimulationService;

        private readonly Dictionary<string, int> _levelClearCountById = new();
        private readonly Dictionary<string, int> _pityCounterByKey = new();
        private readonly Dictionary<string, int> _dailyRollCountByTable = new();
        private readonly Dictionary<string, long> _dailyRollDayByTable = new();
        private readonly Dictionary<string, int> _hourlyRollCountByTable = new();
        private readonly Dictionary<string, long> _hourlyRollHourByTable = new();
        private readonly List<string> _lastEquipmentDropTemplateIds = new();
        private readonly List<EquipmentInstanceData> _lastGeneratedEquipmentDrops = new();
        private readonly List<EquipmentInstanceData> _lastGeneratedFirstClearEquipmentRewards = new();

        private string _lastDropTableResolved = "";
        private bool _lastDailyCapBlocked;
        private bool _lastSoftCapSkipped;
        private bool _lastPityTriggered;
        private string _lastPityCounterKey = "";
        private int _lastPityCounterValue;
        private string _lastSimulationReport = "no simulation yet";

        public LevelRuntimeStateService(
            LevelConfigProvider provider,
            ActiveLevelManager activeLevelManager,
            RandomNumberGenerator rng,
            BattleSimulationService.RollSpawnMonsterIdDelegate rollSpawnMonsterId)
        {
            _provider = provider;
            _activeLevelManager = activeLevelManager;
            _rng = rng;
            _battleSimulationService = new BattleSimulationService(
                rollSpawnMonsterId,
                RollMonsterDrops,
                TryRollMonsterSettlementReward);
        }

        public void ResetForReload()
        {
            _levelClearCountById.Clear();
            _pityCounterByKey.Clear();
            _dailyRollCountByTable.Clear();
            _dailyRollDayByTable.Clear();
            _hourlyRollCountByTable.Clear();
            _hourlyRollHourByTable.Clear();
            _lastEquipmentDropTemplateIds.Clear();
            _lastGeneratedEquipmentDrops.Clear();
            _lastGeneratedFirstClearEquipmentRewards.Clear();
            _lastSimulationReport = "no simulation yet";
            ResetLastDropDebug();
        }

        public string ResolveDropTableForLevel(string levelId, string monsterId, string configuredDropTableId)
        {
            return DropEconomyService.ResolveDropTableForActiveLevel(
                levelId,
                monsterId,
                configuredDropTableId,
                _provider.DropTableById,
                _provider.TryGetDropTable);
        }

        public Dictionary<string, int> RollMonsterDrops(string monsterId)
        {
            var result = new Dictionary<string, int>();
            ResetLastDropDebug();
            _lastEquipmentDropTemplateIds.Clear();
            _lastGeneratedEquipmentDrops.Clear();

            if (!_provider.TryGetMonster(monsterId, out var monster) || !LevelConfigProvider.TryGetChildDictionary(monster, "drops", out var drops))
            {
                return result;
            }

            string configuredDropTableId = LevelConfigProvider.GetString(drops, "drop_table_id", "");
            string dropTableId = ResolveDropTableForLevel(_activeLevelManager.ActiveLevelId, monsterId, configuredDropTableId);
            _lastDropTableResolved = dropTableId;
            int dropRollCount = Math.Max(0, drops.ContainsKey("drop_roll_count") ? drops["drop_roll_count"].AsInt32() : 1);
            string pityCounterKey = "";
            string pityItemId = "";
            int pityThreshold = 0;
            int pityQty = 0;

            if (!string.IsNullOrEmpty(dropTableId) && dropRollCount > 0)
            {
                if (_provider.TryGetDropTable(dropTableId, out var table))
                {
                    DropEconomyService.ReadPityConfig(table, out pityCounterKey, out pityThreshold, out pityItemId, out pityQty);
                }

                AddDropRollResults(dropTableId, dropRollCount, result);
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
                    AddDrop(result, LevelConfigProvider.GetString(dict, "item_id", ""), dict.ContainsKey("qty") ? dict["qty"].AsInt32() : 0);
                }
            }

            DropEconomyService.ApplyPity(dropTableId, pityCounterKey, pityThreshold, pityItemId, pityQty, result, _pityCounterByKey, AddDrop, (key, value, triggered) =>
            {
                _lastPityCounterKey = key;
                _lastPityCounterValue = value;
                if (triggered)
                {
                    _lastPityTriggered = true;
                }
            });

            GenerateLastEquipmentDropInstances();
            return result;
        }

        public Godot.Collections.Array<string> GetLastEquipmentDropTemplateIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (string templateId in _lastEquipmentDropTemplateIds)
            {
                result.Add(templateId);
            }

            return result;
        }

        public EquipmentInstanceData[] GetLastGeneratedEquipmentDrops()
        {
            return _lastGeneratedEquipmentDrops.ToArray();
        }

        public bool TryRollMonsterSettlementReward(string monsterId, out double lingqi, out double insight)
        {
            lingqi = 0.0;
            insight = 0.0;

            if (!_provider.TryGetMonster(monsterId, out var monster) || !LevelConfigProvider.TryGetChildDictionary(monster, "settlement_reward", out var settlement))
            {
                return false;
            }

            int lingqiMin = settlement.ContainsKey("lingqi_min") ? settlement["lingqi_min"].AsInt32() : 0;
            int lingqiMax = settlement.ContainsKey("lingqi_max") ? settlement["lingqi_max"].AsInt32() : lingqiMin;
            int insightMin = settlement.ContainsKey("insight_min") ? settlement["insight_min"].AsInt32() : 0;
            int insightMax = settlement.ContainsKey("insight_max") ? settlement["insight_max"].AsInt32() : insightMin;

            if (lingqiMax < lingqiMin)
            {
                lingqiMax = lingqiMin;
            }

            if (insightMax < insightMin)
            {
                insightMax = insightMin;
            }

            lingqi = _rng.RandiRange(lingqiMin, lingqiMax);
            insight = _rng.RandiRange(insightMin, insightMax);
            return true;
        }

        public bool TryBuildLevelCompletionReward(
            out string levelId,
            out bool firstClear,
            out double lingqi,
            out double insight,
            out int spiritStones,
            out Dictionary<string, int> items)
        {
            levelId = _activeLevelManager.ActiveLevelId;
            firstClear = false;
            lingqi = 0.0;
            insight = 0.0;
            spiritStones = 0;
            items = new Dictionary<string, int>();
            _lastGeneratedFirstClearEquipmentRewards.Clear();

            if (!_provider.TryGetLevelAtIndex(_activeLevelManager.ActiveLevelIndex, out var level) || !LevelConfigProvider.TryGetChildDictionary(level, "rewards", out var rewards))
            {
                return false;
            }

            int clearCount = _levelClearCountById.TryGetValue(levelId, out int savedCount) ? savedCount : 0;
            firstClear = clearCount <= 0;

            if (firstClear)
            {
                if (!LevelConfigProvider.TryGetChildDictionary(rewards, "first_clear", out var first))
                {
                    return false;
                }

                lingqi = first.ContainsKey("lingqi") ? first["lingqi"].AsDouble() : 0.0;
                insight = first.ContainsKey("insight") ? first["insight"].AsDouble() : 0.0;
                spiritStones = first.ContainsKey("spirit_stones") ? first["spirit_stones"].AsInt32() : 0;

                if (first.ContainsKey("items") && first["items"].VariantType == Variant.Type.Array)
                {
                    foreach (Variant item in (Godot.Collections.Array<Variant>)first["items"])
                    {
                        if (item.VariantType != Variant.Type.Dictionary)
                        {
                            continue;
                        }

                        var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                        AddDrop(items, LevelConfigProvider.GetString(dict, "item_id", ""), dict.ContainsKey("qty") ? dict["qty"].AsInt32() : 0);
                    }
                }

                GenerateFirstClearEquipmentRewards(first);
            }
            else
            {
                if (!LevelConfigProvider.TryGetChildDictionary(rewards, "repeat_clear", out var repeat))
                {
                    return false;
                }

                int lingqiMin = repeat.ContainsKey("lingqi_min") ? repeat["lingqi_min"].AsInt32() : 0;
                int lingqiMax = Math.Max(lingqiMin, repeat.ContainsKey("lingqi_max") ? repeat["lingqi_max"].AsInt32() : lingqiMin);
                int insightMin = repeat.ContainsKey("insight_min") ? repeat["insight_min"].AsInt32() : 0;
                int insightMax = Math.Max(insightMin, repeat.ContainsKey("insight_max") ? repeat["insight_max"].AsInt32() : insightMin);
                lingqi = _rng.RandiRange(lingqiMin, lingqiMax);
                insight = _rng.RandiRange(insightMin, insightMax);
            }

            _levelClearCountById[levelId] = clearCount + 1;
            return true;
        }

        public EquipmentInstanceData[] GetLastGeneratedFirstClearEquipmentRewards()
        {
            return _lastGeneratedFirstClearEquipmentRewards.ToArray();
        }

        public string BuildDebugSummary(string activeLevelId, Godot.Collections.Array<string> validationIssues)
        {
            var sb = new StringBuilder();
            sb.Append($"Level {activeLevelId} | dropTable {_lastDropTableResolved}");
            sb.Append($" | dailyCapBlocked={_lastDailyCapBlocked}");
            sb.Append($" | softCapSkip={_lastSoftCapSkipped}");
            sb.Append($" | pityTriggered={_lastPityTriggered}");
            sb.Append($" | clearCount={GetLevelClearCount(activeLevelId)}");

            if (!string.IsNullOrEmpty(_lastPityCounterKey))
            {
                sb.Append($"\nPity {_lastPityCounterKey}: {_lastPityCounterValue}");
            }

            sb.Append("\nHourly rolls:");
            foreach (var kv in _hourlyRollCountByTable)
            {
                sb.Append($" {kv.Key}={kv.Value}");
            }

            sb.Append("\nDaily rolls:");
            foreach (var kv in _dailyRollCountByTable)
            {
                sb.Append($" {kv.Key}={kv.Value}");
            }

            sb.Append($"\nValidation issues={validationIssues.Count}");
            for (int i = 0; i < validationIssues.Count && i < 3; i++)
            {
                sb.Append($"\n! {validationIssues[i]}");
            }

            sb.Append($"\nSim: {_lastSimulationReport}");
            return sb.ToString();
        }

        public string RunBattleSimulation(int battleCount, string forcedMonsterId = "")
        {
            return RunBattleSimulationFiltered(battleCount, "", forcedMonsterId);
        }

        public string RunBattleSimulationFiltered(int battleCount, string levelId = "", string forcedMonsterId = "")
        {
            int originalLevelIndex = _activeLevelManager.ActiveLevelIndex;
            bool switchedLevel = false;

            if (!string.IsNullOrEmpty(levelId) && _provider.TryFindLevelIndex(levelId, out int levelIndex))
            {
                _activeLevelManager.ActiveLevelIndex = levelIndex;
                _activeLevelManager.RefreshActiveLevelData();
                switchedLevel = true;
            }

            string report = RunBattleSimulationCore(battleCount, forcedMonsterId);

            if (switchedLevel)
            {
                _activeLevelManager.ActiveLevelIndex = originalLevelIndex;
                _activeLevelManager.RefreshActiveLevelData();
            }

            return report;
        }

        public Godot.Collections.Dictionary<string, Variant> ToRuntimeDictionary()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["level_clear_count_by_id"] = IntDictionaryToVariantDictionary(_levelClearCountById),
                ["pity_counter_by_key"] = IntDictionaryToVariantDictionary(_pityCounterByKey),
                ["daily_roll_count_by_table"] = IntDictionaryToVariantDictionary(_dailyRollCountByTable),
                ["daily_roll_day_by_table"] = LongDictionaryToVariantDictionary(_dailyRollDayByTable),
                ["hourly_roll_count_by_table"] = IntDictionaryToVariantDictionary(_hourlyRollCountByTable),
                ["hourly_roll_hour_by_table"] = LongDictionaryToVariantDictionary(_hourlyRollHourByTable)
            };
        }

        public void FromRuntimeDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            ResetForReload();

            if (data.ContainsKey("level_clear_count_by_id") && data["level_clear_count_by_id"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToIntDictionary((Godot.Collections.Dictionary<string, Variant>)data["level_clear_count_by_id"], _levelClearCountById);
            }

            if (data.ContainsKey("pity_counter_by_key") && data["pity_counter_by_key"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToIntDictionary((Godot.Collections.Dictionary<string, Variant>)data["pity_counter_by_key"], _pityCounterByKey);
            }

            if (data.ContainsKey("daily_roll_count_by_table") && data["daily_roll_count_by_table"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToIntDictionary((Godot.Collections.Dictionary<string, Variant>)data["daily_roll_count_by_table"], _dailyRollCountByTable);
            }

            if (data.ContainsKey("daily_roll_day_by_table") && data["daily_roll_day_by_table"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToLongDictionary((Godot.Collections.Dictionary<string, Variant>)data["daily_roll_day_by_table"], _dailyRollDayByTable);
            }

            if (data.ContainsKey("hourly_roll_count_by_table") && data["hourly_roll_count_by_table"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToIntDictionary((Godot.Collections.Dictionary<string, Variant>)data["hourly_roll_count_by_table"], _hourlyRollCountByTable);
            }

            if (data.ContainsKey("hourly_roll_hour_by_table") && data["hourly_roll_hour_by_table"].VariantType == Variant.Type.Dictionary)
            {
                VariantDictionaryToLongDictionary((Godot.Collections.Dictionary<string, Variant>)data["hourly_roll_hour_by_table"], _hourlyRollHourByTable);
            }
        }

        private string RunBattleSimulationCore(int battleCount, string forcedMonsterId)
        {
            var pityBackup = new Dictionary<string, int>(_pityCounterByKey);
            var dailyCountBackup = new Dictionary<string, int>(_dailyRollCountByTable);
            var dailyDayBackup = new Dictionary<string, long>(_dailyRollDayByTable);
            var hourlyCountBackup = new Dictionary<string, int>(_hourlyRollCountByTable);
            var hourlyHourBackup = new Dictionary<string, long>(_hourlyRollHourByTable);

            _lastSimulationReport = _battleSimulationService.RunSimulation(
                battleCount,
                forcedMonsterId,
                addDrop: AddDrop,
                restoreState: () =>
                {
                    _pityCounterByKey.Clear();
                    _dailyRollCountByTable.Clear();
                    _dailyRollDayByTable.Clear();
                    _hourlyRollCountByTable.Clear();
                    _hourlyRollHourByTable.Clear();
                    MergeDictionary(_pityCounterByKey, pityBackup);
                    MergeDictionary(_dailyRollCountByTable, dailyCountBackup);
                    MergeDictionary(_dailyRollDayByTable, dailyDayBackup);
                    MergeDictionary(_hourlyRollCountByTable, hourlyCountBackup);
                    MergeDictionary(_hourlyRollHourByTable, hourlyHourBackup);
                },
                wasPityTriggered: () => _lastPityTriggered,
                wasDailyBlocked: () => _lastDailyCapBlocked,
                wasSoftCapSkipped: () => _lastSoftCapSkipped);

            return _lastSimulationReport;
        }

        private void AddDropRollResults(string dropTableId, int rollCount, Dictionary<string, int> result)
        {
            if (!_provider.TryGetDropTable(dropTableId, out var table) || !table.ContainsKey("entries") || table["entries"].VariantType != Variant.Type.Array)
            {
                return;
            }

            List<EquipmentDropResolutionRules.DropEntrySpec> specs = DropEconomyService.BuildDropEntrySpecs((Godot.Collections.Array<Variant>)table["entries"]);
            for (int i = 0; i < rollCount; i++)
            {
                if (!DropEconomyService.TryConsumeDropRoll(table, dropTableId, _dailyRollCountByTable, _dailyRollDayByTable, _hourlyRollCountByTable, _hourlyRollHourByTable, out int hourlyCountAfterConsume, out bool dailyCapBlocked))
                {
                    _lastDailyCapBlocked = dailyCapBlocked;
                    break;
                }

                _lastDailyCapBlocked = dailyCapBlocked;
                if (DropEconomyService.ShouldSkipDropBySoftCap(table, hourlyCountAfterConsume, _rng.Randf()))
                {
                    _lastSoftCapSkipped = true;
                    continue;
                }

                EquipmentDropResolutionRules.DropEntrySpec picked = EquipmentDropResolutionRules.PickWeightedEntry(specs, totalWeight => _rng.RandiRange(1, totalWeight));
                if (picked.Weight <= 0)
                {
                    continue;
                }

                EquipmentDropResolutionRules.DropResolveResult resolved = EquipmentDropResolutionRules.ResolveEntry(
                    picked,
                    (minQty, maxQty) => _rng.RandiRange(minQty, Math.Max(minQty, maxQty)));

                if (!string.IsNullOrEmpty(resolved.ItemId))
                {
                    AddDrop(result, resolved.ItemId, resolved.Quantity);
                }
                else if (!string.IsNullOrEmpty(resolved.EquipmentTemplateId))
                {
                    _lastEquipmentDropTemplateIds.Add(resolved.EquipmentTemplateId);
                }
            }
        }

        private void GenerateLastEquipmentDropInstances()
        {
            _lastGeneratedEquipmentDrops.Clear();
            if (_lastEquipmentDropTemplateIds.Count == 0)
            {
                return;
            }

            EquipmentInstanceData[] generated = EquipmentRewardGenerationService.GenerateDropInstances(
                _provider.EquipmentTemplateById,
                _lastEquipmentDropTemplateIds,
                _activeLevelManager.ActiveLevelId,
                totalWeight => _rng.RandiRange(1, totalWeight),
                (min, max) => min == max ? min : _rng.RandfRange((float)min, (float)max),
                () => (long)Time.GetUnixTimeFromSystem());

            _lastGeneratedEquipmentDrops.AddRange(generated);
        }

        private void GenerateFirstClearEquipmentRewards(Godot.Collections.Dictionary<string, Variant> firstClearRewards)
        {
            _lastGeneratedFirstClearEquipmentRewards.Clear();

            EquipmentInstanceData[] generated = EquipmentRewardGenerationService.GenerateFirstClearRewards(
                _provider.EquipmentTemplateById,
                firstClearRewards,
                _activeLevelManager.ActiveLevelId,
                totalWeight => _rng.RandiRange(1, totalWeight),
                (min, max) => min == max ? min : _rng.RandfRange((float)min, (float)max),
                () => (long)Time.GetUnixTimeFromSystem());

            _lastGeneratedFirstClearEquipmentRewards.AddRange(generated);
        }

        private void ResetLastDropDebug()
        {
            _lastDropTableResolved = "";
            _lastDailyCapBlocked = false;
            _lastSoftCapSkipped = false;
            _lastPityTriggered = false;
            _lastPityCounterKey = "";
            _lastPityCounterValue = 0;
        }

        private int GetLevelClearCount(string levelId)
        {
            return !string.IsNullOrEmpty(levelId) && _levelClearCountById.TryGetValue(levelId, out int count) ? count : 0;
        }

        private static void AddDrop(Dictionary<string, int> result, string itemId, int qty)
        {
            if (string.IsNullOrEmpty(itemId) || qty <= 0)
            {
                return;
            }

            result[itemId] = result.TryGetValue(itemId, out int saved) ? saved + qty : qty;
        }

        private static Godot.Collections.Dictionary<string, Variant> IntDictionaryToVariantDictionary(Dictionary<string, int> source)
        {
            var result = new Godot.Collections.Dictionary<string, Variant>();
            foreach (var kv in source)
            {
                result[kv.Key] = kv.Value;
            }

            return result;
        }

        private static Godot.Collections.Dictionary<string, Variant> LongDictionaryToVariantDictionary(Dictionary<string, long> source)
        {
            var result = new Godot.Collections.Dictionary<string, Variant>();
            foreach (var kv in source)
            {
                result[kv.Key] = kv.Value;
            }

            return result;
        }

        private static void VariantDictionaryToIntDictionary(Godot.Collections.Dictionary<string, Variant> source, Dictionary<string, int> destination)
        {
            foreach (string key in source.Keys)
            {
                destination[key] = source[key].AsInt32();
            }
        }

        private static void VariantDictionaryToLongDictionary(Godot.Collections.Dictionary<string, Variant> source, Dictionary<string, long> destination)
        {
            foreach (string key in source.Keys)
            {
                destination[key] = source[key].AsInt64();
            }
        }

        private static void MergeDictionary<TKey, TValue>(Dictionary<TKey, TValue> destination, Dictionary<TKey, TValue> source)
            where TKey : notnull
        {
            foreach (var kv in source)
            {
                destination[kv.Key] = kv.Value;
            }
        }
    }
}
