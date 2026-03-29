using Godot;
using System;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    /// <summary>
    /// Prototype root controller: coordinates UI/input persistence and global services.
    /// </summary>
    public partial class PrototypeRootController : Control
    {
        private const string UnifiedStatePath = "user://save_state.cfg";
        private const string LegacyUiStatePath = "user://ui_state.cfg";
        private const string LegacyGameStatePath = "user://game_state.cfg";
        private const int SaveSchemaVersion = SaveMigrationRules.LatestVersion;
        private const double SaveIntervalSeconds = 0.5;
        private const double DefaultActivitySaveMarkIntervalSeconds = 10.0;

        private MainBarLayoutController _mainBar = null!;
        private SubmenuWindowController _submenu = null!;
        private BookTabsController _bookTabs = null!;
        private InputActivityState? _activityState;
        private InputHookService? _hookService;
        private BackpackState? _backpackState;
        private AlchemyState? _alchemyState;
        private PotionInventoryState? _potionInventoryState;
        private SmithingState? _smithingState;
        private GardenState? _gardenState;
        private MiningState? _miningState;
        private FishingState? _fishingState;
        private RecipeProgressState? _talismanState;
        private RecipeProgressState? _cookingState;
        private RecipeProgressState? _formationState;
        private RecipeProgressState? _enlightenmentState;
        private RecipeProgressState? _bodyCultivationState;
        private ResourceWalletState? _resourceWalletState;
        private PlayerProgressState? _playerProgressState;
        private PlayerActionState? _playerActionState;
        private EquippedItemsState? _equippedItemsState;
        private SubsystemMasteryState? _subsystemMasteryState;
        private LevelConfigLoader? _levelConfigLoader;
        private ExploreProgressController? _exploreProgressController;
        private CloudSaveSyncService? _cloudSaveSyncService;
        private bool _cloudSyncEnabled;
        private long _lastLoadedSavedUnix;

        private bool _saveDirty;
        private double _saveCooldown;
        private double _activitySaveMarkTimer;
        private double _activitySaveMarkIntervalSeconds = DefaultActivitySaveMarkIntervalSeconds;

        public override void _Ready()
        {
            ServiceLocator.Instance?.Refresh();

            _mainBar = GetNode<MainBarLayoutController>("MainBarWindow");
            _submenu = GetNode<SubmenuWindowController>("SubmenuBookWindow");
            _bookTabs = GetNode<BookTabsController>("SubmenuBookWindow/BookFrame");

            ServiceLocator? services = ServiceLocator.Instance;
            _activityState = services?.InputActivityState;
            _hookService = services?.InputHookService;
            _backpackState = services?.BackpackState;
            _alchemyState = services?.AlchemyState;
            _potionInventoryState = services?.PotionInventoryState;
            _smithingState = services?.SmithingState;
            _gardenState = services?.GardenState;
            _miningState = services?.MiningState;
            _fishingState = services?.FishingState;
            _talismanState = services?.TalismanState;
            _cookingState = services?.CookingState;
            _formationState = services?.FormationState;
            _enlightenmentState = services?.EnlightenmentState;
            _bodyCultivationState = services?.BodyCultivationState;
            _resourceWalletState = services?.ResourceWalletState;
            _playerProgressState = services?.PlayerProgressState;
            _playerActionState = services?.PlayerActionState;
            _equippedItemsState = services?.EquippedItemsState;
            _subsystemMasteryState = services?.SubsystemMasteryState;
            _levelConfigLoader = services?.LevelConfigLoader;
            _exploreProgressController = GetNodeOrNull<ExploreProgressController>("ExploreProgressController");
            _cloudSaveSyncService = services?.CloudSaveSyncService;

            _mainBar.BookButtonPressed += _submenu.ToggleVisible;
            _mainBar.LayoutChanged += (_, _) => MarkDirty();
            _submenu.VisibilityChanged += _ => MarkDirty();
            _bookTabs.ActiveTabsChanged += (_, _) =>
            {
                RefreshRuntimeSettingsFromBookTabs();
                MarkDirty();
            };

            if (_activityState != null)
            {
                _activityState.ActivityTick += OnActivityTick;
                _activityState.InputBatchTick += OnInputBatchTick;
            }
            if (_resourceWalletState != null)
            {
                _resourceWalletState.WalletChanged += OnEconomyStateChanged;
            }
            if (_alchemyState != null)
            {
                _alchemyState.AlchemyChanged += OnAlchemyChanged;
            }
            if (_potionInventoryState != null)
            {
                _potionInventoryState.PotionInventoryChanged += OnPotionInventoryChanged;
            }
            if (_smithingState != null)
            {
                _smithingState.SmithingChanged += OnSmithingChanged;
            }
            if (_playerProgressState != null)
            {
                _playerProgressState.RealmProgressChanged += OnRealmProgressChanged;
            }
            if (_gardenState != null)
            {
                _gardenState.GardenChanged += OnGardenChanged;
            }
            if (_miningState != null)
            {
                _miningState.MiningChanged += OnMiningChanged;
            }
            if (_fishingState != null)
            {
                _fishingState.FishingChanged += OnFishingChanged;
            }
            if (_talismanState != null)
            {
                _talismanState.RecipeProgressChanged += OnGenericRecipeProgressChanged;
            }
            if (_cookingState != null)
            {
                _cookingState.RecipeProgressChanged += OnGenericRecipeProgressChanged;
            }
            if (_formationState != null)
            {
                _formationState.RecipeProgressChanged += OnGenericRecipeProgressChanged;
            }
            if (_enlightenmentState != null)
            {
                _enlightenmentState.RecipeProgressChanged += OnGenericRecipeProgressChanged;
            }
            if (_bodyCultivationState != null)
            {
                _bodyCultivationState.RecipeProgressChanged += OnGenericRecipeProgressChanged;
            }

            if (_hookService == null)
            {
                GD.PushWarning("PrototypeRootController: InputHookService not found at /root/InputHookService");
            }
            if (_backpackState == null)
            {
                GD.PushWarning("PrototypeRootController: BackpackState not found at /root/BackpackState");
            }
            if (_resourceWalletState == null)
            {
                GD.PushWarning("PrototypeRootController: ResourceWalletState not found at /root/ResourceWalletState");
            }
            if (_playerProgressState == null)
            {
                GD.PushWarning("PrototypeRootController: PlayerProgressState not found at /root/PlayerProgressState");
            }
            if (_gardenState == null)
            {
                GD.PushWarning("PrototypeRootController: GardenState not found at /root/GardenState");
            }
            if (_miningState == null)
            {
                GD.PushWarning("PrototypeRootController: MiningState not found at /root/MiningState");
            }
            if (_fishingState == null)
            {
                GD.PushWarning("PrototypeRootController: FishingState not found at /root/FishingState");
            }
            if (_talismanState == null)
            {
                GD.PushWarning("PrototypeRootController: TalismanState not found at /root/TalismanState");
            }
            if (_cloudSaveSyncService == null)
            {
                GD.PushWarning("PrototypeRootController: CloudSaveSyncService not found at /root/CloudSaveSyncService");
            }
            if (_levelConfigLoader == null)
            {
                GD.PushWarning("PrototypeRootController: LevelConfigLoader not found at /root/LevelConfigLoader");
            }
            if (_exploreProgressController == null)
            {
                GD.PushWarning("PrototypeRootController: ExploreProgressController not found under PrototypeRoot");
            }

            CallDeferred(nameof(LoadAllState));
        }

        public override void _ExitTree()
        {
            if (_activityState != null)
            {
                _activityState.ActivityTick -= OnActivityTick;
                _activityState.InputBatchTick -= OnInputBatchTick;
            }
            if (_resourceWalletState != null)
            {
                _resourceWalletState.WalletChanged -= OnEconomyStateChanged;
            }
            if (_alchemyState != null)
            {
                _alchemyState.AlchemyChanged -= OnAlchemyChanged;
            }
            if (_potionInventoryState != null)
            {
                _potionInventoryState.PotionInventoryChanged -= OnPotionInventoryChanged;
            }
            if (_smithingState != null)
            {
                _smithingState.SmithingChanged -= OnSmithingChanged;
            }
            if (_playerProgressState != null)
            {
                _playerProgressState.RealmProgressChanged -= OnRealmProgressChanged;
            }
            if (_gardenState != null)
            {
                _gardenState.GardenChanged -= OnGardenChanged;
            }
            if (_miningState != null)
            {
                _miningState.MiningChanged -= OnMiningChanged;
            }
            if (_fishingState != null)
            {
                _fishingState.FishingChanged -= OnFishingChanged;
            }
            if (_talismanState != null)
            {
                _talismanState.RecipeProgressChanged -= OnGenericRecipeProgressChanged;
            }
            if (_cookingState != null)
            {
                _cookingState.RecipeProgressChanged -= OnGenericRecipeProgressChanged;
            }
            if (_formationState != null)
            {
                _formationState.RecipeProgressChanged -= OnGenericRecipeProgressChanged;
            }
            if (_enlightenmentState != null)
            {
                _enlightenmentState.RecipeProgressChanged -= OnGenericRecipeProgressChanged;
            }
            if (_bodyCultivationState != null)
            {
                _bodyCultivationState.RecipeProgressChanged -= OnGenericRecipeProgressChanged;
            }
        }

        public override void _Process(double delta)
        {
            if (!_saveDirty)
            {
                return;
            }

            _saveCooldown -= delta;
            if (_saveCooldown > 0.0)
            {
                return;
            }

            SaveAllState();
            _saveDirty = false;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationWMCloseRequest)
            {
                SaveAllState();
            }
        }

        private void OnActivityTick(double apThisSecond, double apFinal)
        {
            _activitySaveMarkTimer += 1.0;
            if (_activitySaveMarkTimer >= _activitySaveMarkIntervalSeconds)
            {
                _activitySaveMarkTimer = 0.0;
                MarkDirty();
            }
        }

        private void OnInputBatchTick(int inputEventsThisBatch, double apFinal)
        {
            if (inputEventsThisBatch > 0)
            {
                MarkDirty();
            }
        }

        private void OnEconomyStateChanged(double lingqi, double insight, double petAffinity, int spiritStones)
        {
            MarkDirty();
        }

        private void OnRealmProgressChanged(int realmLevel, double realmExp, double realmExpRequired)
        {
            MarkDirty();
        }

        private void OnAlchemyChanged(string selectedRecipeId, float currentProgress, float requiredProgress)
        {
            MarkDirty();
        }

        private void OnPotionInventoryChanged(string potionId, int amount, int newTotal)
        {
            MarkDirty();
        }

        private void OnSmithingChanged(string targetEquipmentId, float currentProgress, float requiredProgress)
        {
            MarkDirty();
        }

        private void OnGardenChanged(string selectedRecipeId, float currentProgress, float requiredProgress)
        {
            MarkDirty();
        }

        private void OnMiningChanged(string selectedRecipeId, float currentProgress, float requiredProgress, int currentDurability)
        {
            MarkDirty();
        }

        private void OnFishingChanged(string selectedRecipeId, float currentProgress, float requiredProgress)
        {
            MarkDirty();
        }

        private void OnGenericRecipeProgressChanged(string selectedRecipeId, float currentProgress, float requiredProgress)
        {
            MarkDirty();
        }

        private void LoadAllState()
        {
            bool loaded = LoadUnifiedState(out bool migrated);
            if (!loaded)
            {
                migrated = false;
                LoadLegacyState();
            }

            EnsureStarterEquipmentLoadout();

            bool appliedOfflineSettlement = loaded && ApplyOfflineSettlementIfNeeded();

            if (!loaded || migrated || appliedOfflineSettlement)
            {
                SaveAllState();
            }

            _saveDirty = false;
            _saveCooldown = SaveIntervalSeconds;
            _activitySaveMarkTimer = 0.0;
            RefreshRuntimeSettingsFromBookTabs();
        }

        private void EnsureStarterEquipmentLoadout()
        {
            if (_equippedItemsState == null)
            {
                return;
            }

            int beforeCount = _equippedItemsState.GetEquippedProfiles().Length;
            _equippedItemsState.SeedIfEmpty(EquipmentStarterLoadout.CreateDefaultProfiles());
            if (beforeCount == 0 && _equippedItemsState.GetEquippedProfiles().Length > 0)
            {
                MarkDirty();
            }
        }

        private void SaveAllState()
        {
            ConfigFile config = new();

            config.SetValue("meta", "version", SaveSchemaVersion);
            config.SetValue("meta", "last_saved_unix", Time.GetUnixTimeFromSystem());

            WriteUiState(config);
            WriteInputState(config);
            WriteBackpackState(config);
            WriteAlchemyState(config);
            WriteSmithingState(config);
            WriteGatheringState(config);
            WriteGenericRecipeState(config);
            WriteResourceState(config);
            WritePlayerProgressState(config);
            WriteMasteryState(config);
            WriteActionModeState(config);
            WriteEquippedItemsState(config);
            WriteExploreRuntimeState(config);
            WriteLevelRuntimeState(config);
            WriteSystemSettings(config);

            Error err = config.Save(UnifiedStatePath);
            if (err != Error.Ok)
            {
                GD.PushWarning($"PrototypeRootController: failed to save unified state ({err})");
                return;
            }

            _cloudSaveSyncService?.TryUploadLocal(_cloudSyncEnabled);
        }

        private bool LoadUnifiedState(out bool migrated)
        {
            migrated = false;
            ConfigFile config = new();
            if (config.Load(UnifiedStatePath) != Error.Ok)
            {
                return false;
            }

            PrepareLoadedState(config, ref migrated);
            ReadUnifiedState(config);

            if (_cloudSyncEnabled && _cloudSaveSyncService != null && _cloudSaveSyncService.TryDownloadToLocal(true))
            {
                ConfigFile refreshed = new();
                if (refreshed.Load(UnifiedStatePath) == Error.Ok)
                {
                    PrepareLoadedState(refreshed, ref migrated);
                    ReadUnifiedState(refreshed);
                }
            }

            return true;
        }

        private void PrepareLoadedState(ConfigFile config, ref bool migrated)
        {
            int version = config.GetValue("meta", "version", 1).AsInt32();
            if (!SaveMigrationRules.NeedsMigration(version))
            {
                return;
            }

            SaveMigrationRules.MigrateToLatest(config, version);
            migrated = true;
        }

        private void ReadUnifiedState(ConfigFile config)
        {
            _lastLoadedSavedUnix = config.GetValue("meta", "last_saved_unix", 0L).AsInt64();
            int version = config.GetValue("meta", "version", SaveSchemaVersion).AsInt32();

            ReadUiState(config, version);
            ReadInputState(config);
            ReadBackpackState(config);
            ReadAlchemyState(config);
            ReadSmithingState(config);
            ReadGatheringState(config);
            ReadGenericRecipeState(config);
            ReadResourceState(config);
            ReadPlayerProgressState(config);
            ReadMasteryState(config);
            ReadActionModeState(config);
            ReadEquippedItemsState(config);
            ReadLevelRuntimeState(config);
            ReadExploreRuntimeState(config);
            ReadSystemSettings(config);
        }

        private bool ApplyOfflineSettlementIfNeeded()
        {
            if (_playerActionState == null || _resourceWalletState == null || _playerProgressState == null)
            {
                return false;
            }

            bool isCultivation = PlayerActionCapabilityRules.HasCapability(_playerActionState, PlayerActionCapability.ConsumesApSettlement);
            bool isDungeon = PlayerActionCapabilityRules.HasCapability(_playerActionState, PlayerActionCapability.AdvancesDungeon);
            if (!isCultivation && !isDungeon)
            {
                return false;
            }

            if (_lastLoadedSavedUnix <= 0)
            {
                return false;
            }

            long nowUnix = (long)Time.GetUnixTimeFromSystem();
            double offlineSeconds = nowUnix - _lastLoadedSavedUnix;
            OfflineSettlementRules.OfflineTimeEvaluation evaluation = OfflineSettlementRules.EvaluateOfflineSeconds(offlineSeconds);
            if (evaluation.GuardMode == OfflineSettlementRules.OfflineTimeGuardMode.Invalid || evaluation.EffectiveOfflineSeconds <= 1.0)
            {
                return false;
            }

            ActionSettlementResult result = isCultivation
                ? OfflineSettlementRules.BuildCultivationOfflineSettlement(
                    evaluation.EffectiveOfflineSeconds,
                    apPerInput: GameBalanceConstants.Offline.ApPerInput,
                    lingqiFactor: GameBalanceConstants.ResourceConversion.LingqiFactor,
                    insightFactor: GameBalanceConstants.ResourceConversion.InsightFactor,
                    petAffinityFactor: GameBalanceConstants.ResourceConversion.PetAffinityFactor,
                    realmExpFromLingqiRate: GameBalanceConstants.ResourceConversion.RealmExpFromLingqiRate,
                    moodMultiplier: _playerProgressState.GetMoodMultiplier(),
                    realmMultiplier: _playerProgressState.GetRealmMultiplier(),
                    inputExpActive: false,
                    actionTargetId: _playerActionState.ActionTargetId)
                : BuildOfflineDungeonSettlement(evaluation.EffectiveOfflineSeconds);

            if (!result.HasAnyReward)
            {
                return false;
            }

            _resourceWalletState.AddLingqi(result.LingqiGain);
            _resourceWalletState.AddInsight(result.InsightGain);
            _resourceWalletState.AddPetAffinity(result.PetAffinityGain);
            _playerProgressState.AddRealmExp(result.RealmExpGain);
            if (_backpackState != null)
            {
                foreach (KeyValuePair<string, int> drop in result.ItemDrops)
                {
                    _backpackState.AddItem(drop.Key, drop.Value);
                }

                foreach (EquipmentInstanceData equipmentDrop in result.EquipmentDrops)
                {
                    if (!_backpackState.HasEquipment(equipmentDrop.EquipmentId))
                    {
                        _backpackState.AddEquipmentInstance(equipmentDrop);
                    }
                }
            }
            _exploreProgressController?.ShowOfflineSummary(
                OfflineSummaryPresentationRules.BuildTitle(result),
                OfflineSummaryPresentationRules.BuildBody(result));
            MarkDirty();
            return true;
        }

        private ActionSettlementResult BuildOfflineDungeonSettlement(double offlineSeconds)
        {
            if (_levelConfigLoader == null || _equippedItemsState == null || _playerProgressState == null)
            {
                return new ActionSettlementResult(PlayerActionState.ActionDungeon, _playerActionState?.ActionTargetId ?? string.Empty, "offline_dungeon", 0, 0, 0, 0, 0, 0, 0, new Dictionary<string, int>(), Array.Empty<EquipmentInstanceData>());
            }

            double offlineInputBudget = OfflineSettlementRules.CalculateOfflineInputBudget(offlineSeconds);
            CharacterStatBlock baseStats = PlayerBaseStatRules.BuildBaseStats(
                _playerProgressState.RealmLevel,
                _levelConfigLoader.PlayerBaseHp,
                _levelConfigLoader.PlayerAttackPerRound);
            var extraModifiers = new List<CharacterStatModifier>
            {
                ActivityEffectRules.CollectFormationModifier(_formationState?.SelectedRecipeId ?? string.Empty, _backpackState?.GetItemEntries() ?? new Dictionary<string, int>()),
                ActivityEffectRules.CollectPermanentProgressModifier(new PlayerProgressPersistenceRules.PlayerProgressSnapshot(
                    _playerProgressState.RealmLevel,
                    _playerProgressState.RealmExp,
                    _playerProgressState.PetMood,
                    _playerProgressState.HasUnlockedAdvancedAlchemyStudy,
                    _playerProgressState.CurrentRealmActiveSeconds,
                    _playerProgressState.EnlightenmentInsightBonusRate,
                    _playerProgressState.EnlightenmentLingqiBonusRate,
                    _playerProgressState.BodyCultivationMaxHpFlat,
                    _playerProgressState.BodyCultivationAttackFlat,
                    _playerProgressState.BodyCultivationDefenseFlat,
                    _playerProgressState.MeditationCount,
                    _playerProgressState.ContemplationCount,
                    _playerProgressState.TemperCount,
                    _playerProgressState.BoneforgeCount))
            };
            CharacterStatBlock playerStats = CharacterStatRules.BuildFinalStats(
                CharacterStatRules.BuildFinalStats(baseStats, extraModifiers),
                _equippedItemsState.GetEquippedProfiles());
            string targetLevelId = _playerActionState?.ActionTargetId ?? _levelConfigLoader.ActiveLevelId;

            return DungeonOfflineSettlementRules.BuildDungeonOfflineSettlement(
                actionTargetId: targetLevelId,
                offlineInputBudget: offlineInputBudget,
                dungeonProgressPerInput: _levelConfigLoader.ProgressPer100Inputs / 100.0,
                encounterProgressThreshold: _levelConfigLoader.EncounterCheckIntervalProgress,
                playerStats: playerStats,
                weightedMonsters: _levelConfigLoader.GetOfflineWeightedMonsters(targetLevelId),
                averageLingqiPerVictory: _levelConfigLoader.GetOfflineAverageLingqiPerVictory(targetLevelId),
                averageInsightPerVictory: _levelConfigLoader.GetOfflineAverageInsightPerVictory(targetLevelId),
                averageItemDropsPerVictory: _levelConfigLoader.GetOfflineAverageItemDropsPerVictory(targetLevelId),
                remainingDailyRolls: _levelConfigLoader.GetOfflineRemainingDailyRolls(targetLevelId),
                averageDropRollsPerVictory: _levelConfigLoader.GetOfflineAverageDropRollsPerVictory(targetLevelId),
                equipmentDropCap: 2,
                estimatedEquipmentDropsPerVictory: _levelConfigLoader.GetOfflineAverageEquipmentDropsPerVictory(targetLevelId));
        }

        private void LoadLegacyState()
        {
            ConfigFile uiConfig = new();
            if (uiConfig.Load(LegacyUiStatePath) == Error.Ok)
            {
                ReadUiState(uiConfig, 1);
            }

            ConfigFile gameConfig = new();
            if (gameConfig.Load(LegacyGameStatePath) == Error.Ok)
            {
                ReadInputState(gameConfig);
            }
        }

        private void ReadUiState(ConfigFile config, int version)
        {
            float mainBarX = config.GetValue("ui", "main_bar_x", _mainBar.Position.X).AsSingle();
            float mainBarWidth = config.GetValue("ui", "main_bar_width", _mainBar.Size.X).AsSingle();
            _mainBar.ApplyLayout(mainBarX, mainBarWidth);

            string activeLeftTab = config.GetValue("ui", "submenu_active_left_tab", "CultivationTab").AsString();
            string activeRightTab = config.GetValue("ui", "submenu_active_right_tab", "BugTab").AsString();

            if (version < 2 || !config.HasSectionKey("ui", "submenu_active_left_tab"))
            {
                // Legacy single-tab key: migrate into left-page tab selection.
                activeLeftTab = config.GetValue("ui", "submenu_active_tab", "CultivationTab").AsString();
            }

            _bookTabs.RestoreActiveTabs(activeLeftTab, activeRightTab);

            bool submenuVisible = config.GetValue("ui", "submenu_visible", false).AsBool();
            _submenu.SetVisibleImmediate(submenuVisible);
        }

        private void WriteUiState(ConfigFile config)
        {
            config.SetValue("ui", "main_bar_x", _mainBar.Position.X);
            config.SetValue("ui", "main_bar_width", _mainBar.Size.X);
            config.SetValue("ui", "submenu_visible", _submenu.Visible);
            config.SetValue("ui", "submenu_active_left_tab", _bookTabs.ActiveLeftTabName);
            config.SetValue("ui", "submenu_active_right_tab", _bookTabs.ActiveRightTabName);
        }

        private void ReadInputState(ConfigFile config)
        {
            if (_activityState == null)
            {
                return;
            }

            Variant inputData = config.GetValue("input", "stats", new Godot.Collections.Dictionary<string, Variant>());
            if (inputData.VariantType == Variant.Type.Dictionary)
            {
                _activityState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)inputData);
            }

            bool hookPaused = config.GetValue("input", "hook_paused", false).AsBool();
            if (hookPaused)
            {
                GD.PushWarning("PrototypeRootController: saved hook_paused=true detected, auto-resuming input capture.");
            }
            _hookService?.SetPaused(false);
        }

        private void WriteInputState(ConfigFile config)
        {
            if (_activityState == null)
            {
                return;
            }

            config.SetValue("input", "stats", _activityState.ToDictionary());
            config.SetValue("input", "hook_paused", _hookService?.IsPaused ?? false);
        }

        private void ReadBackpackState(ConfigFile config)
        {
            if (_backpackState == null)
            {
                return;
            }

            Variant backpackData = config.GetValue("backpack", "items", new Godot.Collections.Dictionary<string, Variant>());
            if (backpackData.VariantType == Variant.Type.Dictionary)
            {
                _backpackState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)backpackData);
            }

            if (_potionInventoryState != null)
            {
                Variant potionData = config.GetValue("backpack", "potions", new Godot.Collections.Dictionary<string, Variant>());
                if (potionData.VariantType == Variant.Type.Dictionary)
                {
                    _potionInventoryState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)potionData);
                }
            }
        }

        private void WriteBackpackState(ConfigFile config)
        {
            if (_backpackState == null)
            {
                return;
            }

            config.SetValue("backpack", "items", _backpackState.ToDictionary());
            if (_potionInventoryState != null)
            {
                config.SetValue("backpack", "potions", _potionInventoryState.ToDictionary());
            }
        }

        private void ReadAlchemyState(ConfigFile config)
        {
            if (_alchemyState == null)
            {
                return;
            }

            Variant alchemyData = config.GetValue("alchemy", "state", new Godot.Collections.Dictionary<string, Variant>());
            if (alchemyData.VariantType == Variant.Type.Dictionary)
            {
                _alchemyState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)alchemyData);
            }
        }

        private void WriteAlchemyState(ConfigFile config)
        {
            if (_alchemyState == null)
            {
                return;
            }

            config.SetValue("alchemy", "state", _alchemyState.ToDictionary());
        }

        private void ReadSmithingState(ConfigFile config)
        {
            if (_smithingState == null)
            {
                return;
            }

            Variant smithingData = config.GetValue("smithing", "state", new Godot.Collections.Dictionary<string, Variant>());
            if (smithingData.VariantType == Variant.Type.Dictionary)
            {
                _smithingState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)smithingData);
            }
        }

        private void WriteSmithingState(ConfigFile config)
        {
            if (_smithingState == null)
            {
                return;
            }

            config.SetValue("smithing", "state", _smithingState.ToDictionary());
        }

        private void ReadGatheringState(ConfigFile config)
        {
            if (_gardenState != null)
            {
                Variant gardenData = config.GetValue("garden", "state", new Godot.Collections.Dictionary<string, Variant>());
                if (gardenData.VariantType == Variant.Type.Dictionary)
                {
                    _gardenState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)gardenData);
                }
            }

            if (_miningState != null)
            {
                Variant miningData = config.GetValue("mining", "state", new Godot.Collections.Dictionary<string, Variant>());
                if (miningData.VariantType == Variant.Type.Dictionary)
                {
                    _miningState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)miningData);
                }
            }

            if (_fishingState != null)
            {
                Variant fishingData = config.GetValue("fishing", "state", new Godot.Collections.Dictionary<string, Variant>());
                if (fishingData.VariantType == Variant.Type.Dictionary)
                {
                    _fishingState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)fishingData);
                }
            }
        }

        private void WriteGatheringState(ConfigFile config)
        {
            if (_gardenState != null)
            {
                config.SetValue("garden", "state", _gardenState.ToDictionary());
            }

            if (_miningState != null)
            {
                config.SetValue("mining", "state", _miningState.ToDictionary());
            }

            if (_fishingState != null)
            {
                config.SetValue("fishing", "state", _fishingState.ToDictionary());
            }
        }

        private void ReadGenericRecipeState(ConfigFile config)
        {
            ReadGenericRecipeStateSection(config, "talisman", _talismanState);
            ReadGenericRecipeStateSection(config, "cooking", _cookingState);
            ReadGenericRecipeStateSection(config, "formation", _formationState);
            ReadGenericRecipeStateSection(config, "enlightenment", _enlightenmentState);
            ReadGenericRecipeStateSection(config, "body_cultivation", _bodyCultivationState);
        }

        private void WriteGenericRecipeState(ConfigFile config)
        {
            WriteGenericRecipeStateSection(config, "talisman", _talismanState);
            WriteGenericRecipeStateSection(config, "cooking", _cookingState);
            WriteGenericRecipeStateSection(config, "formation", _formationState);
            WriteGenericRecipeStateSection(config, "enlightenment", _enlightenmentState);
            WriteGenericRecipeStateSection(config, "body_cultivation", _bodyCultivationState);
        }

        private static void ReadGenericRecipeStateSection(ConfigFile config, string section, RecipeProgressState? state)
        {
            if (state == null)
            {
                return;
            }

            Variant data = config.GetValue(section, "state", new Godot.Collections.Dictionary<string, Variant>());
            if (data.VariantType == Variant.Type.Dictionary)
            {
                state.FromDictionary((Godot.Collections.Dictionary<string, Variant>)data);
            }
        }

        private static void WriteGenericRecipeStateSection(ConfigFile config, string section, RecipeProgressState? state)
        {
            if (state != null)
            {
                config.SetValue(section, "state", state.ToDictionary());
            }
        }

        private void ReadResourceState(ConfigFile config)
        {
            if (_resourceWalletState == null)
            {
                return;
            }

            Variant walletData = config.GetValue("resource", "wallet", new Godot.Collections.Dictionary<string, Variant>());
            if (walletData.VariantType == Variant.Type.Dictionary)
            {
                _resourceWalletState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)walletData);
            }
        }

        private void WriteResourceState(ConfigFile config)
        {
            if (_resourceWalletState == null)
            {
                return;
            }

            config.SetValue("resource", "wallet", _resourceWalletState.ToDictionary());
        }

        private void ReadPlayerProgressState(ConfigFile config)
        {
            if (_playerProgressState == null)
            {
                return;
            }

            Variant progressData = config.GetValue("progress", "player", new Godot.Collections.Dictionary<string, Variant>());
            if (progressData.VariantType == Variant.Type.Dictionary)
            {
                _playerProgressState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)progressData);
            }
        }

        private void WritePlayerProgressState(ConfigFile config)
        {
            if (_playerProgressState == null)
            {
                return;
            }

            config.SetValue("progress", "player", _playerProgressState.ToDictionary());
        }

        private void ReadMasteryState(ConfigFile config)
        {
            if (_subsystemMasteryState == null)
            {
                return;
            }

            Variant masteryData = config.GetValue("mastery", "levels", new Godot.Collections.Dictionary<string, Variant>());
            if (masteryData.VariantType == Variant.Type.Dictionary)
            {
                _subsystemMasteryState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)masteryData);
            }
        }

        private void WriteMasteryState(ConfigFile config)
        {
            if (_subsystemMasteryState == null)
            {
                return;
            }

            config.SetValue("mastery", "levels", _subsystemMasteryState.ToDictionary());
        }

        private void ReadActionModeState(ConfigFile config)
        {
            if (_playerActionState == null)
            {
                return;
            }

            Variant modeData = config.GetValue("action", "mode", new Godot.Collections.Dictionary<string, Variant>());
            if (modeData.VariantType == Variant.Type.Dictionary)
            {
                _playerActionState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)modeData);
            }
        }

        private void WriteActionModeState(ConfigFile config)
        {
            if (_playerActionState == null)
            {
                return;
            }

            config.SetValue("action", "mode", _playerActionState.ToDictionary());
        }

        private void ReadEquippedItemsState(ConfigFile config)
        {
            if (_equippedItemsState == null)
            {
                return;
            }

            Variant data = config.GetValue("equipment", "equipped", new Godot.Collections.Dictionary<string, Variant>());
            if (data.VariantType == Variant.Type.Dictionary)
            {
                _equippedItemsState.FromDictionary((Godot.Collections.Dictionary<string, Variant>)data);
            }
        }

        private void WriteEquippedItemsState(ConfigFile config)
        {
            if (_equippedItemsState == null)
            {
                return;
            }

            config.SetValue("equipment", "equipped", _equippedItemsState.ToDictionary());
        }

        private void ReadExploreRuntimeState(ConfigFile config)
        {
            if (_exploreProgressController == null)
            {
                return;
            }

            Variant data = config.GetValue("explore", "runtime", new Godot.Collections.Dictionary<string, Variant>());
            if (data.VariantType == Variant.Type.Dictionary)
            {
                _exploreProgressController.FromRuntimeDictionary((Godot.Collections.Dictionary<string, Variant>)data);
            }
        }

        private void WriteExploreRuntimeState(ConfigFile config)
        {
            if (_exploreProgressController == null)
            {
                return;
            }

            config.SetValue("explore", "runtime", _exploreProgressController.ToRuntimeDictionary());
        }

        private void ReadLevelRuntimeState(ConfigFile config)
        {
            if (_levelConfigLoader == null)
            {
                return;
            }

            Variant data = config.GetValue("level", "runtime", new Godot.Collections.Dictionary<string, Variant>());
            if (data.VariantType == Variant.Type.Dictionary)
            {
                _levelConfigLoader.FromRuntimeDictionary((Godot.Collections.Dictionary<string, Variant>)data);
            }
        }

        private void WriteLevelRuntimeState(ConfigFile config)
        {
            if (_levelConfigLoader == null)
            {
                return;
            }

            config.SetValue("level", "runtime", _levelConfigLoader.ToRuntimeDictionary());
        }

        private void ReadSystemSettings(ConfigFile config)
        {
            Variant systemData = config.GetValue("settings", "system", new Godot.Collections.Dictionary<string, Variant>());
            if (systemData.VariantType == Variant.Type.Dictionary)
            {
                var dict = (Godot.Collections.Dictionary<string, Variant>)systemData;
                _bookTabs.FromSystemSettingsDictionary(dict);
            }

            RefreshRuntimeSettingsFromBookTabs();
        }

        private void WriteSystemSettings(ConfigFile config)
        {
            var dict = _bookTabs.ToSystemSettingsDictionary();
            config.SetValue("settings", "system", dict);
            _cloudSyncEnabled = dict.ContainsKey("cloud_sync") && dict["cloud_sync"].AsBool();
            _activitySaveMarkIntervalSeconds = ReadActivitySaveInterval(dict);
        }

        private void RefreshRuntimeSettingsFromBookTabs()
        {
            var dict = _bookTabs.ToSystemSettingsDictionary();
            _cloudSyncEnabled = dict.ContainsKey("cloud_sync") && dict["cloud_sync"].AsBool();
            _activitySaveMarkIntervalSeconds = ReadActivitySaveInterval(dict);
            bool showValidationPanel = !dict.ContainsKey("show_validation_panel") || dict["show_validation_panel"].AsBool();
            _exploreProgressController?.SetValidationPanelEnabled(showValidationPanel);
            bool globalDebugOverlay = dict.ContainsKey("global_debug_overlay") && dict["global_debug_overlay"].AsBool();
            _exploreProgressController?.SetGlobalDebugOverlayEnabled(globalDebugOverlay);
        }

        private static double ReadActivitySaveInterval(Godot.Collections.Dictionary<string, Variant> dict)
        {
            if (!dict.ContainsKey("auto_save_interval_sec"))
            {
                return DefaultActivitySaveMarkIntervalSeconds;
            }

            int value = dict["auto_save_interval_sec"].AsInt32();
            return Math.Max(1, value);
        }

        private void MarkDirty()
        {
            _saveDirty = true;
            _saveCooldown = SaveIntervalSeconds;
        }
    }
}
