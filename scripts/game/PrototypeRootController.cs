using Godot;
using System;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;
using Xiuxian.Scripts.Ui;

namespace Xiuxian.Scripts.Game
{
    /// <summary>
    /// Prototype root controller: coordinates UI/input persistence and global services.
    /// </summary>
    public partial class PrototypeRootController : Control
    {
        private const string UnifiedStatePath = "user://save_state.cfg";
        private const string UnifiedStateBackupPath = "user://save_state.cfg.backup";
        private const string UnifiedStateTempPath = "user://save_state.cfg.tmp";
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
        private FormationState? _formationState;
        private RecipeProgressState? _bodyCultivationState;
        private ResourceWalletState? _resourceWalletState;
        private PlayerStatsState? _playerStatsState;
        private PlayerProgressState? _playerProgressState;
        private CultivationRhythmState? _cultivationRhythmState;
        private ShopState? _shopState;
        private PlayerActionState? _playerActionState;
        private EquippedItemsState? _equippedItemsState;
        private SubsystemMasteryState? _subsystemMasteryState;
        private LevelConfigLoader? _levelConfigLoader;
        private ExploreProgressController? _exploreProgressController;
        private CloudSaveSyncService? _cloudSaveSyncService;
        private ToastController? _toastController;
        private StatePersistenceManager _persistenceManager = new();
        private bool _cloudSyncEnabled;
        private long _lastLoadedSavedUnix;
        private Control? _privacyNoticeOverlay;

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
            _bodyCultivationState = services?.BodyCultivationState;
            _resourceWalletState = services?.ResourceWalletState;
            _playerStatsState = services?.PlayerStatsState;
            _playerProgressState = services?.PlayerProgressState;
            _cultivationRhythmState = services?.CultivationRhythmState;
            _shopState = services?.ShopState;
            _playerActionState = services?.PlayerActionState;
            _equippedItemsState = services?.EquippedItemsState;
            _subsystemMasteryState = services?.SubsystemMasteryState;
            _levelConfigLoader = services?.LevelConfigLoader;
            _exploreProgressController = GetNodeOrNull<ExploreProgressController>("ExploreProgressController");
            _cloudSaveSyncService = services?.CloudSaveSyncService;
            _toastController = GetNodeOrNull<ToastController>("ToastController");

            _persistenceManager = new StatePersistenceManager();
            _persistenceManager.Register("input", "stats", _activityState);
            _persistenceManager.Register("backpack", "items", _backpackState);
            _persistenceManager.Register("backpack", "potions", _potionInventoryState);
            _persistenceManager.Register("alchemy", "state", _alchemyState);
            _persistenceManager.Register("smithing", "state", _smithingState);
            _persistenceManager.Register("garden", "state", _gardenState);
            _persistenceManager.Register("mining", "state", _miningState);
            _persistenceManager.Register("fishing", "state", _fishingState);
            _persistenceManager.Register("talisman", "state", _talismanState);
            _persistenceManager.Register("cooking", "state", _cookingState);
            _persistenceManager.Register("formation", "state", _formationState);
            _persistenceManager.Register("body_cultivation", "state", _bodyCultivationState);
            _persistenceManager.Register("resource", "wallet", _resourceWalletState);
            _persistenceManager.Register("stats", "player", _playerStatsState);
            _persistenceManager.Register("progress", "player", _playerProgressState);
            _persistenceManager.Register("rhythm", "state", _cultivationRhythmState);
            _persistenceManager.Register("shop", "state", _shopState);
            _persistenceManager.Register("mastery", "levels", _subsystemMasteryState);
            _persistenceManager.Register("action", "mode", _playerActionState);
            _persistenceManager.Register("equipment", "equipped", _equippedItemsState);

            _mainBar.BookButtonPressed += _submenu.ToggleVisible;
            _mainBar.LayoutChanged += OnMainBarLayoutChanged;
            _submenu.VisibilityChanged += OnSubmenuVisibilityChanged;
            _bookTabs.ActiveTabsChanged += OnBookTabsActiveTabsChanged;
            _bookTabs.SaveSlotSaveRequested += OnSaveSlotSaveRequested;
            _bookTabs.SaveSlotLoadRequested += OnSaveSlotLoadRequested;
            _bookTabs.SaveSlotDeleteRequested += OnSaveSlotDeleteRequested;

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
            if (_cultivationRhythmState != null)
            {
                _cultivationRhythmState.RhythmChanged += OnCultivationRhythmChanged;
            }
            if (_shopState != null)
            {
                _shopState.ShopChanged += OnShopChanged;
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
            if (_playerStatsState == null)
            {
                GD.PushWarning("PrototypeRootController: PlayerStatsState not found at /root/PlayerStatsState");
            }
            if (_cultivationRhythmState == null)
            {
                GD.PushWarning("PrototypeRootController: CultivationRhythmState not found at /root/CultivationRhythmState");
            }
            if (_shopState == null)
            {
                GD.PushWarning("PrototypeRootController: ShopState not found at /root/ShopState");
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
            _mainBar.BookButtonPressed -= _submenu.ToggleVisible;
            _mainBar.LayoutChanged -= OnMainBarLayoutChanged;
            _submenu.VisibilityChanged -= OnSubmenuVisibilityChanged;
            _bookTabs.ActiveTabsChanged -= OnBookTabsActiveTabsChanged;
            _bookTabs.SaveSlotSaveRequested -= OnSaveSlotSaveRequested;
            _bookTabs.SaveSlotLoadRequested -= OnSaveSlotLoadRequested;
            _bookTabs.SaveSlotDeleteRequested -= OnSaveSlotDeleteRequested;

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
            if (_cultivationRhythmState != null)
            {
                _cultivationRhythmState.RhythmChanged -= OnCultivationRhythmChanged;
            }
            if (_shopState != null)
            {
                _shopState.ShopChanged -= OnShopChanged;
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

        private void OnMainBarLayoutChanged(float x, float width) => MarkDirty();

        private void OnSubmenuVisibilityChanged(bool isVisible) => MarkDirty();

        private void OnBookTabsActiveTabsChanged(string leftTab, string rightTab)
        {
            RefreshRuntimeSettingsFromBookTabs();
            MarkDirty();
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

        private void OnEconomyStateChanged(double lingqi, double insight, int spiritStones)
        {
            MarkDirty();
        }

        private void OnRealmProgressChanged(int realmLevel, double realmExp, double realmExpRequired)
        {
            MarkDirty();
        }

        private void OnCultivationRhythmChanged()
        {
            MarkDirty();
        }

        private void OnShopChanged()
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
            bool appliedOfflineGarden = loaded && ApplyOfflineGardenIfNeeded();
            bool appliedOfflineRhythm = loaded && ApplyOfflineRhythmIfNeeded();

            if (!loaded || migrated || appliedOfflineSettlement || appliedOfflineGarden || appliedOfflineRhythm)
            {
                SaveAllState();
            }

            _saveDirty = false;
            _saveCooldown = SaveIntervalSeconds;
            _activitySaveMarkTimer = 0.0;
            RefreshRuntimeSettingsFromBookTabs();
            if (_bookTabs.ShouldShowPrivacyNotice())
            {
                ShowFirstLaunchPrivacyCard();
            }

            RefreshSaveSlotUi();
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

        private bool SaveAllState()
        {
            ConfigFile config = new();

            config.SetValue("meta", "version", SaveSchemaVersion);
            config.SetValue("meta", "last_saved_unix", Time.GetUnixTimeFromSystem());

            try
            {
                WriteUiState(config);
                WriteInputHookState(config);
                _persistenceManager.WriteAll(config);
                WriteExploreRuntimeState(config);
                WriteLevelRuntimeState(config);
                WriteSystemSettings(config);
            }
            catch (Exception ex)
            {
                GD.PushError($"PrototypeRootController: failed to serialize state — save aborted: {ex.Message}");
                return false;
            }

            Error err = config.Save(UnifiedStateTempPath);
            if (err != Error.Ok)
            {
                GD.PushWarning($"PrototypeRootController: failed to save unified state to temp ({err})");
                return false;
            }

            using var dir = DirAccess.Open("user://");
            if (dir != null)
            {
                if (dir.FileExists(UnifiedStatePath))
                {
                    dir.Remove(UnifiedStateBackupPath);
                    Error backupErr = dir.Rename(UnifiedStatePath, UnifiedStateBackupPath);
                    if (backupErr != Error.Ok)
                    {
                        GD.PushWarning($"PrototypeRootController: failed to create backup ({backupErr}) — save aborted");
                        return false;
                    }
                }

                Error renameErr = dir.Rename(UnifiedStateTempPath, UnifiedStatePath);
                if (renameErr != Error.Ok)
                {
                    GD.PushWarning($"PrototypeRootController: failed to promote temp save ({renameErr}) — restoring backup");
                    if (dir.FileExists(UnifiedStateBackupPath))
                    {
                        dir.Rename(UnifiedStateBackupPath, UnifiedStatePath);
                    }

                    return false;
                }
            }

            _cloudSaveSyncService?.TryUploadLocal(_cloudSyncEnabled);
            RefreshSaveSlotUi();
            return true;
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

            try
            {
                SaveMigrationRules.MigrateToLatest(config, version);
                migrated = true;
            }
            catch (SaveMigrationException ex)
            {
                GD.PushError($"PrototypeRootController: {ex.Message} — save version rolled back to v{ex.FromVersion}");
            }
        }

        private void ReadUnifiedState(ConfigFile config)
        {
            _lastLoadedSavedUnix = config.GetValue("meta", "last_saved_unix", 0L).AsInt64();
            int version = config.GetValue("meta", "version", SaveSchemaVersion).AsInt32();

            ReadStateSafe("UiState", () => ReadUiState(config, version));
            ReadStateSafe("InputHookState", () => ReadInputHookState(config));
            _persistenceManager.ReadAll(config);
            ReadStateSafe("LevelRuntimeState", () => ReadLevelRuntimeState(config));
            ReadStateSafe("ExploreRuntimeState", () => ReadExploreRuntimeState(config));
            ReadStateSafe("SystemSettings", () => ReadSystemSettings(config));
        }

        private void OnSaveSlotSaveRequested(int slotId)
        {
            if (!SaveAllState())
            {
                _bookTabs.SetSaveSlotStatus($"槽位 {slotId} 保存失败：当前自动存档刷新失败。");
                return;
            }

            if (SaveSlotService.SaveWorkingStateToSlot(slotId, out string error))
            {
                string message = $"已保存到槽位 {slotId}";
                _bookTabs.SetSaveSlotStatus(message);
                _toastController?.Enqueue(message, new Color("C8A050"));
                RefreshSaveSlotUi();
                return;
            }

            _bookTabs.SetSaveSlotStatus($"槽位 {slotId} 保存失败：{error}");
        }

        private void OnSaveSlotLoadRequested(int slotId)
        {
            if (!SaveSlotService.TryLoadSlotConfig(slotId, out ConfigFile config, out string error))
            {
                _bookTabs.SetSaveSlotStatus($"槽位 {slotId} 读取失败：{error}");
                return;
            }

            bool migrated = false;
            PrepareLoadedState(config, ref migrated);
            ReadUnifiedState(config);
            EnsureStarterEquipmentLoadout();

            ApplyOfflineSettlementIfNeeded();
            ApplyOfflineGardenIfNeeded();
            ApplyOfflineRhythmIfNeeded();
            if (!SaveAllState())
            {
                _bookTabs.SetSaveSlotStatus($"槽位 {slotId} 已读取，但写回当前自动存档失败。");
                return;
            }

            _saveDirty = false;
            _saveCooldown = SaveIntervalSeconds;
            _activitySaveMarkTimer = 0.0;
            RefreshRuntimeSettingsFromBookTabs();
            RefreshSaveSlotUi();

            string message = $"已读取槽位 {slotId}";
            _bookTabs.SetSaveSlotStatus(message);
            _toastController?.Enqueue(message, new Color("C8A050"));
        }

        private void OnSaveSlotDeleteRequested(int slotId)
        {
            if (SaveSlotService.DeleteSlot(slotId, out string error))
            {
                string message = $"已删除槽位 {slotId}";
                _bookTabs.SetSaveSlotStatus(message);
                _toastController?.Enqueue(message, new Color("B85450"));
                RefreshSaveSlotUi();
                return;
            }

            _bookTabs.SetSaveSlotStatus($"槽位 {slotId} 删除失败：{error}");
        }

        private void RefreshSaveSlotUi()
        {
            _bookTabs.SetSaveSlotSummaries(SaveSlotService.ReadAllSummaries());
        }

        private static void ReadStateSafe(string name, Action readAction)
        {
            try
            {
                readAction();
            }
            catch (Exception ex)
            {
                GD.PushError($"PrototypeRootController: failed to read {name} — section skipped: {ex.Message}");
            }
        }

        private bool ApplyOfflineSettlementIfNeeded()
        {
            if (_playerActionState == null || _resourceWalletState == null || _playerProgressState == null)
            {
                return false;
            }

            if (_playerActionState.ActionId == PlayerActionState.ActionGarden)
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
                    realmExpFromLingqiRate: GameBalanceConstants.ResourceConversion.RealmExpFromLingqiRate,
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
            _toastController?.Enqueue(OfflineSummaryPresentationRules.BuildTitle(result), new Color("C8A050"));
            MarkDirty();
            return true;
        }

        private bool ApplyOfflineGardenIfNeeded()
        {
            if (_gardenState == null || _lastLoadedSavedUnix <= 0)
            {
                return false;
            }

            bool changed = _gardenState.ApplyOfflineRealTime();
            if (changed)
            {
                MarkDirty();
            }

            return changed;
        }

        private bool ApplyOfflineRhythmIfNeeded()
        {
            if (_cultivationRhythmState == null || _lastLoadedSavedUnix <= 0)
            {
                return false;
            }

            long nowUnix = (long)Time.GetUnixTimeFromSystem();
            double offlineSeconds = Math.Max(0.0, nowUnix - _lastLoadedSavedUnix);
            if (offlineSeconds <= 0.0)
            {
                return false;
            }

            bool hadRest = _cultivationRhythmState.InRest;
            _cultivationRhythmState.ApplyOfflineRealTime(offlineSeconds);
            return hadRest;
        }

        private ActionSettlementResult BuildOfflineDungeonSettlement(double offlineSeconds)
        {
            if (_levelConfigLoader == null || _equippedItemsState == null || _playerProgressState == null)
            {
                return new ActionSettlementResult(PlayerActionState.ActionDungeon, _playerActionState?.ActionTargetId ?? string.Empty, "offline_dungeon", 0, 0, 0, 0, 0, 0, new Dictionary<string, int>(), Array.Empty<EquipmentInstanceData>());
            }

            double offlineInputBudget = OfflineSettlementRules.CalculateOfflineInputBudget(offlineSeconds);
            CharacterStatBlock baseStats = PlayerBaseStatRules.BuildBaseStats(
                _playerProgressState.RealmLevel,
                _levelConfigLoader.PlayerBaseHp,
                _levelConfigLoader.PlayerAttackPerRound);
            var extraModifiers = new List<CharacterStatModifier>
            {
                ActivityEffectRules.CollectFormationModifier(
                    _formationState?.ActivePrimaryId ?? string.Empty,
                    _formationState?.ActiveSecondaryId ?? string.Empty,
                    _backpackState?.GetItemEntries() ?? new Dictionary<string, int>(),
                    _subsystemMasteryState?.GetLevel(PlayerActionState.ModeFormation) ?? 1),
                ActivityEffectRules.CollectPermanentProgressModifier(new PlayerProgressPersistenceRules.PlayerProgressSnapshot(
                    _playerProgressState.RealmLevel,
                    _playerProgressState.RealmExp,
                    _playerProgressState.HasUnlockedAdvancedAlchemyStudy,
                    _playerProgressState.CurrentRealmActiveSeconds,
                    _playerProgressState.BodyCultivationMaxHpFlat,
                    _playerProgressState.BodyCultivationAttackFlat,
                    _playerProgressState.BodyCultivationDefenseFlat,
                    _playerProgressState.TemperCount,
                    _playerProgressState.BoneforgeCount,
                    _playerProgressState.BloodflowCount,
                    _playerProgressState.BodyCultivationPostBattleHealRate,
                    _playerProgressState.ZhouTianMaxHpRate,
                    _playerProgressState.ZhouTianAttackRate,
                    _playerProgressState.ZhouTianDefenseRate))
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
                _persistenceManager.ReadAll(gameConfig);
                ReadInputHookState(gameConfig);
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

        private void ReadInputHookState(ConfigFile config)
        {
            bool hookPaused = config.GetValue("input", "hook_paused", false).AsBool();
            if (hookPaused)
            {
                GD.PushWarning("PrototypeRootController: saved hook_paused=true detected, auto-resuming input capture.");
            }
            _hookService?.SetPaused(false);
        }

        private void WriteInputHookState(ConfigFile config)
        {
            config.SetValue("input", "hook_paused", _hookService?.IsPaused ?? false);
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
            bool shouldPauseInputCollection = !_bookTabs.IsPrivacyInputCollectionEnabled();
            if (_hookService != null && _hookService.IsPaused != shouldPauseInputCollection)
            {
                _hookService.SetPaused(shouldPauseInputCollection);
            }
            bool showValidationPanel = !dict.ContainsKey("show_validation_panel") || dict["show_validation_panel"].AsBool();
            _exploreProgressController?.SetValidationPanelEnabled(showValidationPanel);
            bool globalDebugOverlay = dict.ContainsKey("global_debug_overlay") && dict["global_debug_overlay"].AsBool();
            _exploreProgressController?.SetGlobalDebugOverlayEnabled(globalDebugOverlay);
        }

        private void ShowFirstLaunchPrivacyCard()
        {
            if (_privacyNoticeOverlay != null && IsInstanceValid(_privacyNoticeOverlay))
            {
                return;
            }

            ColorRect overlay = new();
            overlay.Name = "FirstLaunchPrivacyOverlay";
            overlay.SetAnchorsPreset(LayoutPreset.FullRect);
            overlay.Color = new Color(0.0f, 0.0f, 0.0f, 0.3f);
            overlay.MouseFilter = Control.MouseFilterEnum.Stop;
            overlay.ZIndex = 1000;
            overlay.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            AddChild(overlay);
            _privacyNoticeOverlay = overlay;

            CenterContainer center = new();
            center.SetAnchorsPreset(LayoutPreset.FullRect);
            overlay.AddChild(center);

            PanelContainer card = new();
            card.CustomMinimumSize = new Vector2(480.0f, 220.0f);
            card.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            card.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color("F5E6C8"),
                BorderColor = new Color("C8A050"),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                ShadowColor = new Color(0.0f, 0.0f, 0.0f, 0.18f),
                ShadowSize = 8,
            });
            center.AddChild(card);

            MarginContainer margin = new();
            margin.AddThemeConstantOverride("margin_left", 16);
            margin.AddThemeConstantOverride("margin_top", 16);
            margin.AddThemeConstantOverride("margin_right", 16);
            margin.AddThemeConstantOverride("margin_bottom", 16);
            card.AddChild(margin);

            VBoxContainer content = new();
            content.Alignment = BoxContainer.AlignmentMode.Center;
            content.AddThemeConstantOverride("separation", 12);
            margin.AddChild(content);

            Label iconLabel = new();
            iconLabel.Text = "🔒";
            iconLabel.HorizontalAlignment = HorizontalAlignment.Center;
            iconLabel.AddThemeFontSizeOverride("font_size", 28);
            content.AddChild(iconLabel);

            Label titleLabel = new();
            titleLabel.Text = UiText.PrivacyNoticeTitle;
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.AddThemeFontSizeOverride("font_size", 18);
            titleLabel.AddThemeColorOverride("font_color", new Color("4A3728"));
            content.AddChild(titleLabel);

            Label bodyLabel = new();
            bodyLabel.Text = UiText.PrivacyNoticeBody;
            bodyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            bodyLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            bodyLabel.AddThemeFontSizeOverride("font_size", 14);
            bodyLabel.AddThemeColorOverride("font_color", new Color("6B5B4E"));
            content.AddChild(bodyLabel);

            Control spacer = new();
            spacer.CustomMinimumSize = new Vector2(0.0f, 12.0f);
            content.AddChild(spacer);

            CenterContainer buttonCenter = new();
            content.AddChild(buttonCenter);

            Button confirmButton = new();
            confirmButton.Text = UiText.PrivacyNoticeConfirm;
            confirmButton.CustomMinimumSize = new Vector2(160.0f, 36.0f);
            confirmButton.AddThemeColorOverride("font_color", Colors.White);
            confirmButton.AddThemeStyleboxOverride("normal", new StyleBoxFlat
            {
                BgColor = new Color("C8A050"),
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
            });
            confirmButton.AddThemeStyleboxOverride("hover", new StyleBoxFlat
            {
                BgColor = new Color("D6AE59"),
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
            });
            confirmButton.AddThemeStyleboxOverride("pressed", new StyleBoxFlat
            {
                BgColor = new Color("B58F46"),
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
            });
            confirmButton.Pressed += OnFirstLaunchPrivacyCardConfirmed;
            buttonCenter.AddChild(confirmButton);

            overlay.TreeExited += () =>
            {
                if (_privacyNoticeOverlay == overlay)
                {
                    _privacyNoticeOverlay = null;
                }
            };

            CreateTween().TweenProperty(overlay, "modulate:a", 1.0f, 0.3f);
        }

        private void OnFirstLaunchPrivacyCardConfirmed()
        {
            _bookTabs.AcknowledgePrivacyNotice();
            MarkDirty();

            if (_privacyNoticeOverlay != null && IsInstanceValid(_privacyNoticeOverlay))
            {
                _privacyNoticeOverlay.QueueFree();
                _privacyNoticeOverlay = null;
            }
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
