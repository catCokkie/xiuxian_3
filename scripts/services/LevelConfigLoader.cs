using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Loads level/monster/drop configuration from JSON and exposes indexed lookup.
    /// </summary>
    public partial class LevelConfigLoader : Node
    {
        [Signal]
        public delegate void ConfigLoadedEventHandler(string levelId, string levelName);

        [Export] public string ConfigPath = "res://docs/design/09_level_monster_drop_sample.json";

        public string ActiveLevelId => _activeLevelManager.ActiveLevelId;
        public string ActiveLevelName => _activeLevelManager.ActiveLevelName;
        public double ProgressPer100Inputs => _activeLevelManager.ProgressPer100Inputs;
        public double EncounterCheckIntervalProgress => _activeLevelManager.EncounterCheckIntervalProgress;
        public double BaseEncounterRate => _activeLevelManager.BaseEncounterRate;
        public double BattlePauseFactor => _activeLevelManager.BattlePauseFactor;
        public int PlayerBaseHp => _activeLevelManager.PlayerBaseHp;
        public int PlayerAttackPerRound => _activeLevelManager.PlayerAttackPerRound;
        public int EnemyDamageDivider => _activeLevelManager.EnemyDamageDivider;
        public int EnemyMinDamagePerRound => _activeLevelManager.EnemyMinDamagePerRound;

        private readonly LevelConfigProvider _provider = new();
        private ActiveLevelManager _activeLevelManager;
        private readonly OfflineProjectionService _offlineProjectionService;
        private readonly BattleSimulationService _battleSimulationService;
        private readonly DropEconomyService _dropEconomyService = new();

        private Godot.Collections.Dictionary<string, Variant> _rootData
        {
            get => _provider.RootData;
        }

        private List<Godot.Collections.Dictionary<string, Variant>> _levels
        {
            get => _provider.Levels;
        }

        private int _activeLevelIndex
        {
            get => _activeLevelManager.ActiveLevelIndex;
            set => _activeLevelManager.ActiveLevelIndex = value;
        }
        private Dictionary<string, Godot.Collections.Dictionary<string, Variant>> _monsterById => _provider.MonsterById;
        private Dictionary<string, Godot.Collections.Dictionary<string, Variant>> _dropTableById => _provider.DropTableById;
        private Dictionary<string, Godot.Collections.Dictionary<string, Variant>> _equipmentSeriesById => _provider.EquipmentSeriesById;
        private Dictionary<string, Godot.Collections.Dictionary<string, Variant>> _equipmentTemplateById => _provider.EquipmentTemplateById;
        private Dictionary<string, List<Godot.Collections.Dictionary<string, Variant>>> _equipmentExchangeRecipesByLevelId => _provider.EquipmentExchangeRecipesByLevelId;
        private readonly Dictionary<string, int> _levelClearCountById = new();
        private readonly Dictionary<string, int> _pityCounterByKey = new();
        private readonly Dictionary<string, int> _dailyRollCountByTable = new();
        private readonly Dictionary<string, long> _dailyRollDayByTable = new();
        private readonly Dictionary<string, int> _hourlyRollCountByTable = new();
        private readonly Dictionary<string, long> _hourlyRollHourByTable = new();
        private HashSet<string> _unlockedLevelIds => _activeLevelManager.UnlockedLevelIds;
        private HashSet<string> _bossClearedLevelIds => _activeLevelManager.BossClearedLevelIds;
        private IReadOnlyList<string> _activeLevelMonsterWave => _activeLevelManager.ActiveLevelMonsterWave;
        private Dictionary<string, int> _activeMoveInputsByCategory => _activeLevelManager.ActiveMoveInputsByCategory;
        private int _activeLevelWaveIndex
        {
            get => _activeLevelManager.ActiveLevelWaveIndex;
            set => _activeLevelManager.ActiveLevelWaveIndex = value;
        }
        private readonly RandomNumberGenerator _rng = new();
        private readonly List<string> _lastEquipmentDropTemplateIds = new();
        private readonly List<EquipmentInstanceData> _lastGeneratedEquipmentDrops = new();
        private readonly List<EquipmentInstanceData> _lastGeneratedFirstClearEquipmentRewards = new();
        private string _lastDropTableResolved = "";
        private string _lastLoadedConfigText => _provider.LastLoadedConfigText;
        private bool _lastDailyCapBlocked;
        private bool _lastSoftCapSkipped;
        private bool _lastPityTriggered;
        private string _lastPityCounterKey = "";
        private int _lastPityCounterValue;
        private string _lastSimulationReport = "no simulation yet";
        private readonly ConfigValidationService _configValidationService;
        public int ValidationIssueCount => _configValidationService.ValidationIssueCount;

        public LevelConfigLoader()
        {
            _activeLevelManager = new ActiveLevelManager(_provider);
            _configValidationService = new ConfigValidationService(
                _provider,
                getActiveLevelId: () => ActiveLevelId,
                getLevelName: GetLevelName,
                isLevelUnlocked: IsLevelUnlocked);
            _offlineProjectionService = new OfflineProjectionService(
                _provider,
                getActiveLevelIndex: () => _activeLevelIndex,
                tryFindLevelIndex: TryFindLevelIndex,
                tryGetMonsterStatProfile: TryGetMonsterStatProfile,
                tryGetMonster: TryGetMonster,
                tryGetDropTable: TryGetDropTable,
                resolveDropTableForActiveLevel: (monsterId, configuredDropTableId) => DropEconomyService.ResolveDropTableForActiveLevel(ActiveLevelId, monsterId, configuredDropTableId, _dropTableById, TryGetDropTable),
                buildDropEntrySpecs: DropEconomyService.BuildDropEntrySpecs);
            _battleSimulationService = new BattleSimulationService(
                rollSpawnMonsterId: RollSpawnMonsterId,
                rollMonsterDrops: RollMonsterDrops,
                tryRollMonsterSettlementReward: TryRollMonsterSettlementReward);
        }

        public override void _Ready()
        {
            _rng.Randomize();
            LoadConfig();
        }

        public bool LoadConfig()
        {
            _monsterById.Clear();
            _dropTableById.Clear();
            _equipmentSeriesById.Clear();
            _equipmentTemplateById.Clear();
            _equipmentExchangeRecipesByLevelId.Clear();
            _levelClearCountById.Clear();
            _pityCounterByKey.Clear();
            _dailyRollCountByTable.Clear();
            _dailyRollDayByTable.Clear();
            _hourlyRollCountByTable.Clear();
            _hourlyRollHourByTable.Clear();
            _bossClearedLevelIds.Clear();

            using FileAccess? file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PushWarning($"LevelConfigLoader: failed to open config at {ConfigPath}");
                return false;
            }

            return LoadConfigFromText(file.GetAsText());
        }

        public bool LoadConfigFromText(string text)
        {
            _levelClearCountById.Clear();
            _pityCounterByKey.Clear();
            _dailyRollCountByTable.Clear();
            _dailyRollDayByTable.Clear();
            _hourlyRollCountByTable.Clear();
            _hourlyRollHourByTable.Clear();
            _bossClearedLevelIds.Clear();

            if (!_provider.LoadConfigFromText(text))
            {
                GD.PushWarning("LevelConfigLoader: config is not a valid dictionary JSON.");
                return false;
            }

            _activeLevelIndex = 0;
            EnsureLevelUnlockBootstrap();
            ApplyActiveLevelData();
            _configValidationService.ValidateConfiguration();

            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
            GD.Print($"LevelConfigLoader: loaded level '{ActiveLevelId}' ({ActiveLevelName})");
            return true;
        }

        public bool AdvanceToNextLevel()
        {
            if (!_activeLevelManager.AdvanceToNextLevel())
            {
                return false;
            }

            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
            return true;
        }

        public bool TryAdvanceToNextUnlockedLevel()
        {
            if (!_activeLevelManager.TryAdvanceToNextUnlockedLevel())
            {
                return false;
            }

            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
            return true;
        }

        public bool TrySetActiveLevel(string levelId)
        {
            if (!_activeLevelManager.TrySetActiveLevel(levelId))
            {
                return false;
            }

            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
            return true;
        }

        public bool TrySetActiveLevelIfUnlocked(string levelId)
        {
            if (!_activeLevelManager.TrySetActiveLevelIfUnlocked(levelId))
            {
                return false;
            }

            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
            return true;
        }

        public bool TrySetNextUnlockedLevelAsActive()
        {
            if (!_activeLevelManager.TrySetNextUnlockedLevelAsActive())
            {
                return false;
            }

            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
            return true;
        }

        public bool TryGetMonster(string monsterId, out Godot.Collections.Dictionary<string, Variant> monsterData)
        {
            return _provider.TryGetMonster(monsterId, out monsterData);
        }

        public bool TryGetDropTable(string dropTableId, out Godot.Collections.Dictionary<string, Variant> dropTableData)
        {
            return _provider.TryGetDropTable(dropTableId, out dropTableData);
        }

        public string RollSpawnMonsterId()
        {
            return _activeLevelManager.RollSpawnMonsterId(_rng);
        }

        public bool TryGetMonsterCombatParams(
            string monsterId,
            out string monsterName,
            out int hp,
            out int inputsPerRound,
            out int attack)
        {
            monsterName = "Enemy";
            hp = 24;
            inputsPerRound = 18;
            attack = 4;

            if (!TryGetMonsterStatProfile(monsterId, out MonsterStatProfile profile))
            {
                return false;
            }

            monsterName = profile.DisplayName;
            hp = profile.BaseStats.MaxHp;
            inputsPerRound = profile.InputsPerRound;
            attack = profile.BaseStats.Attack;
            return true;
        }

        public bool TryGetMonsterStatProfile(string monsterId, out MonsterStatProfile profile)
        {
            profile = new MonsterStatProfile(
                monsterId,
                "Enemy",
                new CharacterStatBlock(24, 4, 0, 100, 0.0, 1.5),
                InputsPerRound: 18,
                MoveCategory: "normal",
                IsBoss: false);

            if (!TryGetMonster(monsterId, out var monster))
            {
                if (TryBuildFallbackBossProfile(monsterId, out profile))
                {
                    return true;
                }

                return false;
            }

            string monsterName = GetString(monster, "monster_name", profile.DisplayName);
            string moveCategory = GetString(monster, "move_category", "");
            if (string.IsNullOrEmpty(moveCategory))
            {
                moveCategory = GetString(monster, "rarity", "normal");
            }

            int hp = profile.BaseStats.MaxHp;
            int attack = profile.BaseStats.Attack;
            int defense = profile.BaseStats.Defense;
            double speedFactor = 1.0;
            int inputsPerRound = profile.InputsPerRound;
            if (TryGetChildDictionary(monster, "combat", out var combat))
            {
                hp = Math.Max(1, combat.ContainsKey("hp") ? combat["hp"].AsInt32() : hp);
                attack = Math.Max(1, combat.ContainsKey("attack") ? combat["attack"].AsInt32() : attack);
                defense = Math.Max(0, combat.ContainsKey("defense") ? combat["defense"].AsInt32() : defense);
                speedFactor = combat.ContainsKey("speed_factor") ? combat["speed_factor"].AsDouble() : speedFactor;
                inputsPerRound = Math.Max(1, combat.ContainsKey("inputs_per_round") ? combat["inputs_per_round"].AsInt32() : inputsPerRound);
            }

            string activeLevelId = ActiveLevelId;
            bool isBoss = !string.IsNullOrEmpty(activeLevelId) && IsBossMonsterForLevel(activeLevelId, monsterId);
            profile = MonsterStatRules.BuildProfile(monsterId, monsterName, hp, attack, defense, speedFactor, inputsPerRound, moveCategory, isBoss);
            return true;
        }

        public Godot.Collections.Array<string> GetBossDefeatedLevelIds()
        {
            return _activeLevelManager.GetBossClearedLevelIds();
        }

        public bool TryGetMonsterVisualConfig(
            string monsterId,
            out string portraitPath,
            out string animationType,
            out double animationSpeed,
            out double animationAmplitude,
            out Color tint)
        {
            portraitPath = "";
            animationType = "none";
            animationSpeed = 0.0;
            animationAmplitude = 0.0;
            tint = Colors.White;

            if (!TryGetMonster(monsterId, out var monster))
            {
                return false;
            }

            if (!TryGetChildDictionary(monster, "visual", out var visual))
            {
                return true;
            }

            portraitPath = GetString(visual, "portrait", "");
            animationType = GetString(visual, "animation", "none");
            animationSpeed = GetDouble(visual, "anim_speed", 0.0);
            animationAmplitude = GetDouble(visual, "anim_amplitude", 0.0);

            if (visual.ContainsKey("tint"))
            {
                tint = ParseColorVariant(visual["tint"], tint);
            }

            return true;
        }

        public bool TryGetMonsterMoveRule(
            string monsterId,
            out string moveCategory,
            out int inputsPerMove)
        {
            moveCategory = "normal";
            inputsPerMove = 4;

            if (!TryGetMonster(monsterId, out var monster))
            {
                return false;
            }

            moveCategory = GetString(monster, "move_category", "");
            if (string.IsNullOrEmpty(moveCategory))
            {
                moveCategory = GetString(monster, "rarity", "normal");
            }

            if (_activeMoveInputsByCategory.TryGetValue(moveCategory, out int configured))
            {
                inputsPerMove = Math.Max(1, configured);
            }
            else if (_activeMoveInputsByCategory.TryGetValue("default", out int fallback))
            {
                inputsPerMove = Math.Max(1, fallback);
            }

            return true;
        }

        public bool TryGetActiveWaveProgress(
            out int nextSpawnIndex,
            out int waveCount,
            out string nextMonsterId)
        {
            return _activeLevelManager.TryGetActiveWaveProgress(out nextSpawnIndex, out waveCount, out nextMonsterId);
        }

        public Dictionary<string, int> RollMonsterDrops(string monsterId)
        {
            var result = new Dictionary<string, int>();
            ResetLastDropDebug();
            _lastEquipmentDropTemplateIds.Clear();
            _lastGeneratedEquipmentDrops.Clear();
            if (!TryGetMonster(monsterId, out var monster))
            {
                return result;
            }
            if (!TryGetChildDictionary(monster, "drops", out var drops))
            {
                return result;
            }

            string configuredDropTableId = GetString(drops, "drop_table_id", "");
            string dropTableId = DropEconomyService.ResolveDropTableForActiveLevel(ActiveLevelId, monsterId, configuredDropTableId, _dropTableById, TryGetDropTable);
            _lastDropTableResolved = dropTableId;
            int dropRollCount = Math.Max(0, drops.ContainsKey("drop_roll_count") ? drops["drop_roll_count"].AsInt32() : 1);
            string pityCounterKey = "";
            string pityItemId = "";
            int pityThreshold = 0;
            int pityQty = 0;

            if (!string.IsNullOrEmpty(dropTableId) && dropRollCount > 0)
            {
                if (TryGetDropTable(dropTableId, out var table))
                {
                    DropEconomyService.ReadPityConfig(table, out pityCounterKey, out pityThreshold, out pityItemId, out pityQty);
                }
                AddDropRollResults(dropTableId, dropRollCount, result);
            }

            if (drops.ContainsKey("guaranteed_drop"))
            {
                Variant guaranteedVariant = drops["guaranteed_drop"];
                if (guaranteedVariant.VariantType == Variant.Type.Array)
                {
                    var guaranteed = (Godot.Collections.Array<Variant>)guaranteedVariant;
                    foreach (Variant item in guaranteed)
                    {
                        if (item.VariantType != Variant.Type.Dictionary)
                        {
                            continue;
                        }

                        var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                        string itemId = GetString(dict, "item_id", "");
                        int qty = Math.Max(0, dict.ContainsKey("qty") ? dict["qty"].AsInt32() : 0);
                        AddDrop(result, itemId, qty);
                    }
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

            if (!TryGetMonster(monsterId, out var monster))
            {
                return false;
            }
            if (!TryGetChildDictionary(monster, "settlement_reward", out var settlement))
            {
                return false;
            }

            int lingqiMin = settlement.ContainsKey("lingqi_min") ? settlement["lingqi_min"].AsInt32() : 0;
            int lingqiMax = settlement.ContainsKey("lingqi_max") ? settlement["lingqi_max"].AsInt32() : lingqiMin;
            int insightMin = settlement.ContainsKey("insight_min") ? settlement["insight_min"].AsInt32() : 0;
            int insightMax = settlement.ContainsKey("insight_max") ? settlement["insight_max"].AsInt32() : insightMin;

            (int rolledLingqi, int rolledInsight) = BattleSettlementRules.RollReward(
                lingqiMin,
                lingqiMax,
                insightMin,
                insightMax,
                (min, max) => _rng.RandiRange(min, max));
            lingqi = rolledLingqi;
            insight = rolledInsight;
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
            levelId = ActiveLevelId;
            firstClear = false;
            lingqi = 0.0;
            insight = 0.0;
            spiritStones = 0;
            items = new Dictionary<string, int>();
            _lastGeneratedFirstClearEquipmentRewards.Clear();

            if (!TryGetActiveLevel(out var level))
            {
                return false;
            }
            if (!TryGetChildDictionary(level, "rewards", out var rewards))
            {
                return false;
            }

            int clearCount = _levelClearCountById.TryGetValue(ActiveLevelId, out int c) ? c : 0;
            firstClear = clearCount <= 0;

            if (firstClear)
            {
                if (!TryGetChildDictionary(rewards, "first_clear", out var first))
                {
                    return false;
                }

                lingqi = first.ContainsKey("lingqi") ? first["lingqi"].AsDouble() : 0.0;
                insight = first.ContainsKey("insight") ? first["insight"].AsDouble() : 0.0;
                spiritStones = first.ContainsKey("spirit_stones") ? first["spirit_stones"].AsInt32() : 0;
                if (first.ContainsKey("items") && first["items"].VariantType == Variant.Type.Array)
                {
                    var itemArray = (Godot.Collections.Array<Variant>)first["items"];
                    foreach (Variant v in itemArray)
                    {
                        if (v.VariantType != Variant.Type.Dictionary)
                        {
                            continue;
                        }

                        var dict = (Godot.Collections.Dictionary<string, Variant>)v;
                        string itemId = GetString(dict, "item_id", "");
                        int qty = dict.ContainsKey("qty") ? dict["qty"].AsInt32() : 0;
                        AddDrop(items, itemId, qty);
                    }
                }

                GenerateFirstClearEquipmentRewards(first);
            }
            else
            {
                if (!TryGetChildDictionary(rewards, "repeat_clear", out var repeat))
                {
                    return false;
                }

                int lingqiMin = repeat.ContainsKey("lingqi_min") ? repeat["lingqi_min"].AsInt32() : 0;
                int lingqiMax = repeat.ContainsKey("lingqi_max") ? repeat["lingqi_max"].AsInt32() : lingqiMin;
                int insightMin = repeat.ContainsKey("insight_min") ? repeat["insight_min"].AsInt32() : 0;
                int insightMax = repeat.ContainsKey("insight_max") ? repeat["insight_max"].AsInt32() : insightMin;

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
            }

            _levelClearCountById[ActiveLevelId] = clearCount + 1;
            return true;
        }

        public EquipmentInstanceData[] GetLastGeneratedFirstClearEquipmentRewards()
        {
            return _lastGeneratedFirstClearEquipmentRewards.ToArray();
        }

        public string BuildDebugSummary()
        {
            var sb = new StringBuilder();
            sb.Append($"Level {ActiveLevelId} | dropTable {_lastDropTableResolved}");
            sb.Append($" | dailyCapBlocked={_lastDailyCapBlocked}");
            sb.Append($" | softCapSkip={_lastSoftCapSkipped}");
            sb.Append($" | pityTriggered={_lastPityTriggered}");
            sb.Append($" | clearCount={GetLevelClearCount(ActiveLevelId)}");

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

            Godot.Collections.Array<string> validationIssues = _configValidationService.GetValidationIssues();
            sb.Append($"\nValidation issues={validationIssues.Count}");
            if (validationIssues.Count > 0)
            {
                int max = Math.Min(3, validationIssues.Count);
                for (int i = 0; i < max; i++)
                {
                    sb.Append($"\n! {validationIssues[i]}");
                }
            }

            sb.Append($"\nSim: {_configValidationService.GetLastSimulationReport()}");

            return sb.ToString();
        }

        public string BuildValidationSummary(int maxLines = 12)
        {
            return _configValidationService.BuildValidationSummary(maxLines);
        }

        public string BuildLevelPreviewSummary(int maxLines = 12)
        {
            return _configValidationService.BuildLevelPreviewSummary(maxLines, GetLevelIds());
        }

        public Godot.Collections.Array<string> GetValidationIssues()
        {
            return _configValidationService.GetValidationIssues();
        }

        public Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> GetValidationEntries()
        {
            return _configValidationService.GetValidationEntries();
        }

        public string RunBattleSimulation(int battleCount, string forcedMonsterId = "")
        {
            return RunBattleSimulationFiltered(battleCount, "", forcedMonsterId);
        }

        public string RunBattleSimulationFiltered(int battleCount, string levelId = "", string forcedMonsterId = "")
        {
            int originalLevelIndex = _activeLevelIndex;
            bool switchedLevel = false;

            if (!string.IsNullOrEmpty(levelId) && TryFindLevelIndex(levelId, out int levelIndex))
            {
                _activeLevelIndex = levelIndex;
                _activeLevelManager.RefreshActiveLevelData();
                switchedLevel = true;
            }

            string report = RunBattleSimulationCore(battleCount, forcedMonsterId);

            if (switchedLevel)
            {
                _activeLevelIndex = originalLevelIndex;
                _activeLevelManager.RefreshActiveLevelData();
            }

            _configValidationService.SetLastSimulationReport(report);
            return report;
        }

        public Godot.Collections.Array<string> GetLevelIds()
        {
            return _provider.GetLevelIds();
        }

        public Godot.Collections.Array<string> GetEquipmentSeriesIds()
        {
            return _provider.GetEquipmentSeriesIds();
        }

        public Godot.Collections.Array<string> GetEquipmentTemplateIds()
        {
            return _provider.GetEquipmentTemplateIds();
        }

        public Godot.Collections.Array<string> GetEquipmentExchangeLevelIds()
        {
            return _provider.GetEquipmentExchangeLevelIds();
        }

        public bool TryGetEquipmentSeries(string seriesId, out Godot.Collections.Dictionary<string, Variant> seriesData)
        {
            return _provider.TryGetEquipmentSeries(seriesId, out seriesData);
        }

        public bool TryGetEquipmentTemplate(string templateId, out Godot.Collections.Dictionary<string, Variant> templateData)
        {
            return _provider.TryGetEquipmentTemplate(templateId, out templateData);
        }

        public Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> GetEquipmentExchangeRecipes(string levelId = "")
        {
            string resolvedLevelId = string.IsNullOrEmpty(levelId) ? ActiveLevelId : levelId;
            return _provider.GetEquipmentExchangeRecipes(resolvedLevelId);
        }

        public string GetLevelName(string levelId)
        {
            return _provider.GetLevelName(levelId);
        }

        public Godot.Collections.Array<string> GetUnlockedLevelIds()
        {
            return _activeLevelManager.GetUnlockedLevelIds();
        }

        public bool IsLevelUnlocked(string levelId)
        {
            return _activeLevelManager.IsLevelUnlocked(levelId);
        }

        public bool IsBossMonsterForLevel(string levelId, string monsterId)
        {
            return _activeLevelManager.IsBossMonsterForLevel(levelId, monsterId);
        }

        public string GetBossMonsterId(string levelId = "")
        {
            return _activeLevelManager.GetBossMonsterId(levelId);
        }

        public bool TryMarkBossDefeatedAndUnlockNext(string levelId, string monsterId, out string unlockedLevelId)
        {
            return _activeLevelManager.TryMarkBossDefeatedAndUnlockNext(levelId, monsterId, out unlockedLevelId);
        }

        public Godot.Collections.Array<string> GetSpawnMonsterIds(string levelId = "")
        {
            return _activeLevelManager.GetSpawnMonsterIds(levelId);
        }

        public DungeonOfflineSettlementRules.WeightedMonsterProfile[] GetOfflineWeightedMonsters(string levelId = "")
        {
            List<DungeonOfflineProjectionRules.MonsterSettlementSpec> specs = _offlineProjectionService.BuildOfflineMonsterSettlementSpecs(levelId);
            return DungeonOfflineProjectionRules.BuildWeightedMonsters(specs);
        }

        public double GetOfflineAverageLingqiPerVictory(string levelId = "")
        {
            return DungeonOfflineProjectionRules.CalculateAverageLingqiPerVictory(_offlineProjectionService.BuildOfflineMonsterSettlementSpecs(levelId));
        }

        public double GetOfflineAverageInsightPerVictory(string levelId = "")
        {
            return DungeonOfflineProjectionRules.CalculateAverageInsightPerVictory(_offlineProjectionService.BuildOfflineMonsterSettlementSpecs(levelId));
        }

        public Dictionary<string, double> GetOfflineAverageItemDropsPerVictory(string levelId = "")
        {
            return DungeonOfflineProjectionRules.CalculateAverageItemDropsPerVictory(_offlineProjectionService.BuildOfflineMonsterSettlementSpecs(levelId));
        }

        public double GetOfflineAverageEquipmentDropsPerVictory(string levelId = "")
        {
            return DungeonOfflineProjectionRules.CalculateAverageEquipmentDropsPerVictory(_offlineProjectionService.BuildOfflineMonsterSettlementSpecs(levelId));
        }

        private string RunBattleSimulationCore(int battleCount, string forcedMonsterId = "")
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

        private bool TryFindLevelIndex(string levelId, out int levelIndex)
        {
            levelIndex = -1;
            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            for (int i = 0; i < _levels.Count; i++)
            {
                string id = GetString(_levels[i], "level_id", "");
                if (id == levelId)
                {
                    levelIndex = i;
                    return true;
                }
            }

            return false;
        }

        public Godot.Collections.Dictionary<string, Variant> ToRuntimeDictionary()
        {
            Godot.Collections.Dictionary<string, Variant> activeLevelRuntime = _activeLevelManager.ToRuntimeDictionary();

            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["active_level_id"] = activeLevelRuntime["active_level_id"],
                ["active_wave_index"] = activeLevelRuntime["active_wave_index"],
                ["unlocked_level_ids"] = activeLevelRuntime["unlocked_level_ids"],
                ["boss_cleared_level_ids"] = activeLevelRuntime["boss_cleared_level_ids"],
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
            _pityCounterByKey.Clear();
            _levelClearCountById.Clear();
            _dailyRollCountByTable.Clear();
            _dailyRollDayByTable.Clear();
            _hourlyRollCountByTable.Clear();
            _hourlyRollHourByTable.Clear();

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

            _activeLevelManager.FromRuntimeDictionary(data);
        }

        private void EnsureLevelUnlockBootstrap()
        {
            _activeLevelManager.EnsureLevelUnlockBootstrap();
        }

        private string GetNextLevelId(string levelId)
        {
            if (!TryFindLevelIndex(levelId, out int index))
            {
                return "";
            }

            int next = index + 1;
            if (next < 0 || next >= _levels.Count)
            {
                return "";
            }

            return GetString(_levels[next], "level_id", "");
        }

        private string GetConfiguredNextLevelId(string levelId)
        {
            if (!TryFindLevelIndex(levelId, out int index))
            {
                return "";
            }

            return GetString(_levels[index], "unlock_next_level_id", "");
        }

        private string GetNextUnlockedLevelId(string levelId)
        {
            var unlocked = GetUnlockedLevelIds();
            return LevelUnlockRules.GetNextUnlockedLevelId(unlocked, levelId);
        }

        private static string GetLevelBossMonsterId(Godot.Collections.Dictionary<string, Variant> level)
        {
            string configured = GetString(level, "boss_monster_id", "");
            if (!string.IsNullOrEmpty(configured))
            {
                return configured;
            }

            if (level.ContainsKey("monster_wave") && level["monster_wave"].VariantType == Variant.Type.Array)
            {
                var wave = (Godot.Collections.Array<Variant>)level["monster_wave"];
                for (int i = wave.Count - 1; i >= 0; i--)
                {
                    string id = wave[i].AsString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        return id;
                    }
                }
            }

            return "";
        }

        private bool TryBuildFallbackBossProfile(string monsterId, out MonsterStatProfile profile)
        {
            profile = default;
            if (_levels.Count == 0 || string.IsNullOrEmpty(monsterId))
            {
                return false;
            }

            Godot.Collections.Dictionary<string, Variant> level = _levels[Math.Clamp(_activeLevelIndex, 0, _levels.Count - 1)];
            string bossId = GetLevelBossMonsterId(level);
            if (bossId != monsterId)
            {
                return false;
            }

            if (!level.ContainsKey("monster_wave") || level["monster_wave"].VariantType != Variant.Type.Array)
            {
                return false;
            }

            var wave = (Godot.Collections.Array<Variant>)level["monster_wave"];
            for (int i = wave.Count - 1; i >= 0; i--)
            {
                string eliteAnchorId = wave[i].AsString();
                if (string.IsNullOrEmpty(eliteAnchorId) || eliteAnchorId == monsterId)
                {
                    continue;
                }

                if (!TryGetMonsterStatProfile(eliteAnchorId, out MonsterStatProfile eliteProfile))
                {
                    continue;
                }

                profile = BossEncounterRules.BuildBossProfile(monsterId, $"{eliteProfile.DisplayName} Boss", eliteProfile, 2.5);
                return true;
            }

            return false;
        }

        private void AddDropRollResults(string dropTableId, int rollCount, Dictionary<string, int> result)
        {
            if (!TryGetDropTable(dropTableId, out var table))
            {
                return;
            }
            if (!table.ContainsKey("entries"))
            {
                return;
            }

            Variant entriesVariant = table["entries"];
            if (entriesVariant.VariantType != Variant.Type.Array)
            {
                return;
            }

            var entries = (Godot.Collections.Array<Variant>)entriesVariant;
            List<EquipmentDropResolutionRules.DropEntrySpec> specs = DropEconomyService.BuildDropEntrySpecs(entries);
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
                _equipmentTemplateById,
                _lastEquipmentDropTemplateIds,
                ActiveLevelId,
                totalWeight => _rng.RandiRange(1, totalWeight),
                (min, max) => min == max ? min : _rng.RandfRange((float)min, (float)max),
                () => (long)Time.GetUnixTimeFromSystem());

            _lastGeneratedEquipmentDrops.AddRange(generated);
        }

        private void GenerateFirstClearEquipmentRewards(Godot.Collections.Dictionary<string, Variant> firstClearRewards)
        {
            _lastGeneratedFirstClearEquipmentRewards.Clear();

            EquipmentInstanceData[] generated = EquipmentRewardGenerationService.GenerateFirstClearRewards(
                _equipmentTemplateById,
                firstClearRewards,
                ActiveLevelId,
                totalWeight => _rng.RandiRange(1, totalWeight),
                (min, max) => min == max ? min : _rng.RandfRange((float)min, (float)max),
                () => (long)Time.GetUnixTimeFromSystem());

            _lastGeneratedFirstClearEquipmentRewards.AddRange(generated);
        }

        private static void AddDrop(Dictionary<string, int> result, string itemId, int qty)
        {
            if (string.IsNullOrEmpty(itemId) || qty <= 0)
            {
                return;
            }

            if (!result.ContainsKey(itemId))
            {
                result[itemId] = 0;
            }
            result[itemId] += qty;
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

        private static void VariantDictionaryToIntDictionary(
            Godot.Collections.Dictionary<string, Variant> source,
            Dictionary<string, int> destination)
        {
            foreach (string key in source.Keys)
            {
                destination[key] = source[key].AsInt32();
            }
        }

        private static void VariantDictionaryToLongDictionary(
            Godot.Collections.Dictionary<string, Variant> source,
            Dictionary<string, long> destination)
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
            if (string.IsNullOrEmpty(levelId))
            {
                return 0;
            }

            return _levelClearCountById.TryGetValue(levelId, out int count) ? count : 0;
        }

        private void ParseLevelsSection()
        {
            _levels.Clear();
            _activeLevelIndex = 0;

            if (_rootData.ContainsKey("levels"))
            {
                Variant levelsVariant = _rootData["levels"];
                if (levelsVariant.VariantType == Variant.Type.Array)
                {
                    var levels = (Godot.Collections.Array<Variant>)levelsVariant;
                    foreach (Variant item in levels)
                    {
                        if (item.VariantType != Variant.Type.Dictionary)
                        {
                            continue;
                        }

                        _levels.Add((Godot.Collections.Dictionary<string, Variant>)item);
                    }
                }
            }

            if (_levels.Count == 0 && TryGetChildDictionary(_rootData, "level", out var singleLevel))
            {
                _levels.Add(singleLevel);
            }

            ApplyActiveLevelData();
        }

        private void IndexMonsters()
        {
            if (!_rootData.ContainsKey("monsters"))
            {
                return;
            }

            Variant monstersVariant = _rootData["monsters"];
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
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                _monsterById[id] = dict;
            }
        }

        private void IndexDropTables()
        {
            if (!_rootData.ContainsKey("drop_tables"))
            {
                return;
            }

            Variant dropTablesVariant = _rootData["drop_tables"];
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
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                _dropTableById[id] = dict;
            }
        }

        private void IndexEquipmentSeries()
        {
            _equipmentSeriesById.Clear();
            var indexes = EquipmentConfigTextParser.ParseIndexes(_lastLoadedConfigText);
            foreach (var kv in indexes.SeriesJsonById)
            {
                Variant parsed = Json.ParseString(kv.Value);
                if (parsed.VariantType == Variant.Type.Dictionary)
                {
                    _equipmentSeriesById[kv.Key] = (Godot.Collections.Dictionary<string, Variant>)parsed;
                }
            }
        }

        private void IndexEquipmentTemplates()
        {
            _equipmentTemplateById.Clear();
            var indexes = EquipmentConfigTextParser.ParseIndexes(_lastLoadedConfigText);
            foreach (var kv in indexes.TemplateJsonById)
            {
                Variant parsed = Json.ParseString(kv.Value);
                if (parsed.VariantType == Variant.Type.Dictionary)
                {
                    _equipmentTemplateById[kv.Key] = (Godot.Collections.Dictionary<string, Variant>)parsed;
                }
            }
        }

        private void IndexEquipmentExchangeRecipes()
        {
            _equipmentExchangeRecipesByLevelId.Clear();
            var indexes = EquipmentConfigTextParser.ParseIndexes(_lastLoadedConfigText);
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

                _equipmentExchangeRecipesByLevelId[kv.Key] = list;
            }
        }

        private static bool TryGetChildDictionary(
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

        private static string GetString(Godot.Collections.Dictionary<string, Variant> dict, string key, string fallback)
        {
            return dict.ContainsKey(key) ? dict[key].AsString() : fallback;
        }

        private static double GetDouble(Godot.Collections.Dictionary<string, Variant> dict, string key, double fallback)
        {
            return dict.ContainsKey(key) ? dict[key].AsDouble() : fallback;
        }

        private static Color ParseColorVariant(Variant value, Color fallback)
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

        private bool TryGetActiveLevel(out Godot.Collections.Dictionary<string, Variant> level)
        {
            level = new Godot.Collections.Dictionary<string, Variant>();
            if (_levels.Count == 0)
            {
                return false;
            }

            _activeLevelIndex = Math.Clamp(_activeLevelIndex, 0, _levels.Count - 1);
            level = _levels[_activeLevelIndex];
            return true;
        }

        private void ApplyActiveLevelData()
        {
            _activeLevelManager.RefreshActiveLevelData();
        }
    }
}
