using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public partial class LevelConfigLoader : Node
    {
        [Signal] public delegate void ConfigLoadedEventHandler(string levelId, string levelName);
        [Export] public string ConfigPath = "res://docs/design/09_level_monster_drop_sample.json";

        private readonly LevelConfigProvider _provider = new();
        private readonly RandomNumberGenerator _rng = new();
        private readonly ActiveLevelManager _activeLevelManager;
        private readonly MonsterConfigService _monsterConfigService;
        private readonly LevelRuntimeStateService _runtimeStateService;
        private readonly ConfigValidationService _configValidationService;
        private readonly OfflineProjectionService _offlineProjectionService;

        public LevelConfigLoader()
        {
            _activeLevelManager = new ActiveLevelManager(_provider);
            _monsterConfigService = new MonsterConfigService(_provider, _activeLevelManager);
            _runtimeStateService = new LevelRuntimeStateService(_provider, _activeLevelManager, _rng, () => _activeLevelManager.RollSpawnMonsterId(_rng));
            _configValidationService = new ConfigValidationService(_provider, () => ActiveLevelId, GetLevelName, IsLevelUnlocked);
            _offlineProjectionService = new OfflineProjectionService(
                _provider,
                () => ActiveLevelIndex,
                _provider.TryFindLevelIndex,
                _monsterConfigService.TryGetMonsterStatProfile,
                _provider.TryGetMonster,
                _provider.TryGetDropTable,
                _runtimeStateService.ResolveDropTableForLevel,
                DropEconomyService.BuildDropEntrySpecs);
        }

        public string ActiveLevelId => _activeLevelManager.ActiveLevelId;
        public string ActiveLevelName => _activeLevelManager.ActiveLevelName;
        public double ProgressPer100Inputs => _activeLevelManager.ProgressPer100Inputs;
        public double EncounterCheckIntervalProgress => _activeLevelManager.EncounterCheckIntervalProgress;
        public double BaseEncounterRate => _activeLevelManager.BaseEncounterRate;
        public int ActiveLevelDangerLevel => _activeLevelManager.DangerLevel;
        public double BattlePauseFactor => _activeLevelManager.BattlePauseFactor;
        public int PlayerBaseHp => _activeLevelManager.PlayerBaseHp;
        public int PlayerAttackPerRound => _activeLevelManager.PlayerAttackPerRound;
        public int EnemyDamageDivider => _activeLevelManager.EnemyDamageDivider;
        public int EnemyMinDamagePerRound => _activeLevelManager.EnemyMinDamagePerRound;
        public int ActiveLevelIndex => _activeLevelManager.ActiveLevelIndex;
        public int ValidationIssueCount => _configValidationService.ValidationIssueCount;

        public override void _Ready()
        {
            _rng.Randomize();
            LoadConfig();
        }

        public bool LoadConfig()
        {
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
            if (!_provider.LoadConfigFromText(text))
            {
                GD.PushWarning("LevelConfigLoader: config is not a valid dictionary JSON.");
                return false;
            }

            _activeLevelManager.ResetForReload();
            _activeLevelManager.EnsureLevelUnlockBootstrap();
            _runtimeStateService.ResetForReload();
            _configValidationService.ValidateConfiguration();
            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
            GD.Print($"LevelConfigLoader: loaded level '{ActiveLevelId}' ({ActiveLevelName})");
            return true;
        }

        public bool AdvanceToNextLevel() => SwitchLevel(_activeLevelManager.AdvanceToNextLevel());
        public bool TryAdvanceToNextUnlockedLevel() => SwitchLevel(_activeLevelManager.TryAdvanceToNextUnlockedLevel());
        public bool TrySetActiveLevel(string levelId) => SwitchLevel(_activeLevelManager.TrySetActiveLevel(levelId));
        public bool TrySetActiveLevelIfUnlocked(string levelId) => SwitchLevel(_activeLevelManager.TrySetActiveLevelIfUnlocked(levelId));
        public bool TrySetNextUnlockedLevelAsActive() => SwitchLevel(_activeLevelManager.TrySetNextUnlockedLevelAsActive());
        public bool TryGetMonster(string monsterId, out Godot.Collections.Dictionary<string, Variant> monsterData) => _provider.TryGetMonster(monsterId, out monsterData);
        public bool TryGetDropTable(string dropTableId, out Godot.Collections.Dictionary<string, Variant> dropTableData) => _provider.TryGetDropTable(dropTableId, out dropTableData);
        public string RollSpawnMonsterId() => _activeLevelManager.RollSpawnMonsterId(_rng);
        public bool TryGetMonsterCombatParams(string monsterId, out string monsterName, out int hp, out int inputsPerRound, out int attack) => _monsterConfigService.TryGetMonsterCombatParams(monsterId, out monsterName, out hp, out inputsPerRound, out attack);
        public bool TryGetMonsterStatProfile(string monsterId, out MonsterStatProfile profile) => _monsterConfigService.TryGetMonsterStatProfile(monsterId, out profile);
        public Godot.Collections.Array<string> GetBossDefeatedLevelIds() => _activeLevelManager.GetBossClearedLevelIds();
        public bool TryGetMonsterVisualConfig(string monsterId, out string portraitPath, out string animationType, out double animationSpeed, out double animationAmplitude, out Color tint) => _monsterConfigService.TryGetMonsterVisualConfig(monsterId, out portraitPath, out animationType, out animationSpeed, out animationAmplitude, out tint);
        public bool TryGetMonsterMoveRule(string monsterId, out string moveCategory, out int inputsPerMove) => _monsterConfigService.TryGetMonsterMoveRule(monsterId, out moveCategory, out inputsPerMove);
        public bool TryGetActiveWaveProgress(out int nextSpawnIndex, out int waveCount, out string nextMonsterId) => _activeLevelManager.TryGetActiveWaveProgress(out nextSpawnIndex, out waveCount, out nextMonsterId);
        public Dictionary<string, int> RollMonsterDrops(string monsterId) => _runtimeStateService.RollMonsterDrops(monsterId);
        public Godot.Collections.Array<string> GetLastEquipmentDropTemplateIds() => _runtimeStateService.GetLastEquipmentDropTemplateIds();
        public EquipmentInstanceData[] GetLastGeneratedEquipmentDrops() => _runtimeStateService.GetLastGeneratedEquipmentDrops();
        public bool TryRollMonsterSettlementReward(string monsterId, out double lingqi, out double insight) => _runtimeStateService.TryRollMonsterSettlementReward(monsterId, out lingqi, out insight);
        public bool TryBuildLevelCompletionReward(out string levelId, out bool firstClear, out double lingqi, out double insight, out int spiritStones, out Dictionary<string, int> items) => _runtimeStateService.TryBuildLevelCompletionReward(out levelId, out firstClear, out lingqi, out insight, out spiritStones, out items);
        public EquipmentInstanceData[] GetLastGeneratedFirstClearEquipmentRewards() => _runtimeStateService.GetLastGeneratedFirstClearEquipmentRewards();
        public string BuildDebugSummary() => _runtimeStateService.BuildDebugSummary(ActiveLevelId, _configValidationService.GetValidationIssues());
        public string BuildValidationSummary(int maxLines = 12) => _configValidationService.BuildValidationSummary(maxLines);
        public string BuildLevelPreviewSummary(int maxLines = 12) => _configValidationService.BuildLevelPreviewSummary(maxLines, GetLevelIds());
        public Godot.Collections.Array<string> GetValidationIssues() => _configValidationService.GetValidationIssues();
        public Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> GetValidationEntries() => _configValidationService.GetValidationEntries();
        public string RunBattleSimulation(int battleCount, string forcedMonsterId = "") => _runtimeStateService.RunBattleSimulation(battleCount, forcedMonsterId);
        public string RunBattleSimulationFiltered(int battleCount, string levelId = "", string forcedMonsterId = "") => _runtimeStateService.RunBattleSimulationFiltered(battleCount, levelId, forcedMonsterId);
        public Godot.Collections.Array<string> GetLevelIds() => _provider.GetLevelIds();
        public Godot.Collections.Array<string> GetEquipmentSeriesIds() => _provider.GetEquipmentSeriesIds();
        public Godot.Collections.Array<string> GetEquipmentTemplateIds() => _provider.GetEquipmentTemplateIds();
        public Godot.Collections.Array<string> GetEquipmentExchangeLevelIds() => _provider.GetEquipmentExchangeLevelIds();
        public bool TryGetEquipmentSeries(string seriesId, out Godot.Collections.Dictionary<string, Variant> seriesData) => _provider.TryGetEquipmentSeries(seriesId, out seriesData);
        public bool TryGetEquipmentTemplate(string templateId, out Godot.Collections.Dictionary<string, Variant> templateData) => _provider.TryGetEquipmentTemplate(templateId, out templateData);
        public Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> GetEquipmentExchangeRecipes(string levelId = "") => _provider.GetEquipmentExchangeRecipes(string.IsNullOrEmpty(levelId) ? ActiveLevelId : levelId);
        public string GetLevelName(string levelId) => _provider.GetLevelName(levelId);
        public Godot.Collections.Array<string> GetUnlockedLevelIds() => _activeLevelManager.GetUnlockedLevelIds();
        public bool IsLevelUnlocked(string levelId) => _activeLevelManager.IsLevelUnlocked(levelId);
        public bool IsBossMonsterForLevel(string levelId, string monsterId) => _activeLevelManager.IsBossMonsterForLevel(levelId, monsterId);
        public string GetBossMonsterId(string levelId = "") => _activeLevelManager.GetBossMonsterId(levelId);
        public bool TryMarkBossDefeatedAndUnlockNext(string levelId, string monsterId, out string unlockedLevelId) => _activeLevelManager.TryMarkBossDefeatedAndUnlockNext(levelId, monsterId, out unlockedLevelId);
        public Godot.Collections.Array<string> GetSpawnMonsterIds(string levelId = "") => _activeLevelManager.GetSpawnMonsterIds(levelId);
        public DungeonOfflineSettlementRules.WeightedMonsterProfile[] GetOfflineWeightedMonsters(string levelId = "") => DungeonOfflineProjectionRules.BuildWeightedMonsters(_offlineProjectionService.BuildOfflineMonsterSettlementSpecs(levelId));
        public double GetOfflineAverageLingqiPerVictory(string levelId = "") => DungeonOfflineProjectionRules.CalculateAverageLingqiPerVictory(_offlineProjectionService.BuildOfflineMonsterSettlementSpecs(levelId));
        public double GetOfflineAverageInsightPerVictory(string levelId = "") => DungeonOfflineProjectionRules.CalculateAverageInsightPerVictory(_offlineProjectionService.BuildOfflineMonsterSettlementSpecs(levelId));
        public Dictionary<string, double> GetOfflineAverageItemDropsPerVictory(string levelId = "") => DungeonOfflineProjectionRules.CalculateAverageItemDropsPerVictory(_offlineProjectionService.BuildOfflineMonsterSettlementSpecs(levelId));
        public double GetOfflineAverageDropRollsPerVictory(string levelId = "") => _offlineProjectionService.CalculateAverageDropRollsPerVictory(levelId);
        public double GetOfflineAverageEquipmentDropsPerVictory(string levelId = "") => DungeonOfflineProjectionRules.CalculateAverageEquipmentDropsPerVictory(_offlineProjectionService.BuildOfflineMonsterSettlementSpecs(levelId));
        public int GetOfflineRemainingDailyRolls(string levelId = "") => _runtimeStateService.GetRemainingDailyRollsForLevel(string.IsNullOrEmpty(levelId) ? ActiveLevelId : levelId);

        public Godot.Collections.Dictionary<string, Variant> ToRuntimeDictionary()
        {
            Godot.Collections.Dictionary<string, Variant> result = _runtimeStateService.ToRuntimeDictionary();
            Godot.Collections.Dictionary<string, Variant> activeLevelRuntime = _activeLevelManager.ToRuntimeDictionary();
            result["active_level_id"] = activeLevelRuntime["active_level_id"];
            result["active_wave_index"] = activeLevelRuntime["active_wave_index"];
            result["unlocked_level_ids"] = activeLevelRuntime["unlocked_level_ids"];
            result["boss_cleared_level_ids"] = activeLevelRuntime["boss_cleared_level_ids"];
            return result;
        }

        public void FromRuntimeDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            _runtimeStateService.FromRuntimeDictionary(data);
            _activeLevelManager.FromRuntimeDictionary(data);
        }

        private bool SwitchLevel(bool changed)
        {
            if (!changed)
            {
                return false;
            }

            EmitSignal(SignalName.ConfigLoaded, ActiveLevelId, ActiveLevelName);
            return true;
        }
    }
}
