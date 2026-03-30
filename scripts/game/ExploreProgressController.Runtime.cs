using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using Xiuxian.Scripts.Services;
using Xiuxian.Scripts.UI;

namespace Xiuxian.Scripts.Game
{
    /// <summary>
    /// Bottom track controller:
    /// player fixed on the left, monsters advance from the right,
    /// and HP is shown under player + current battle target.
    /// </summary>
    public partial class ExploreProgressController : Node
    {
        [Export] public NodePath ProgressBarPath = "../MainBarWindow/Chrome/ExploreProgressBar";
        [Export] public NodePath CultivationProgressBarPath = "../MainBarWindow/Chrome/CultivationProgressBar";
        [Export] public NodePath BreakthroughButtonPath = "../MainBarWindow/Chrome/BreakthroughButton";
        [Export] public NodePath CultivationLabelPath = "../MainBarWindow/Chrome/CultivationLabel";
        [Export] public NodePath ZoneLabelPath = "../MainBarWindow/Chrome/ZoneLabel";
        [Export] public NodePath ActivityRateLabelPath = "../MainBarWindow/Chrome/ActivityRateLabel";
        [Export] public NodePath MoveDebugLabelPath = "../MainBarWindow/Chrome/MoveDebugLabel";
        [Export] public NodePath RealmStageLabelPath = "../MainBarWindow/Chrome/RealmStageLabel";

        [Export] public NodePath BattleInfoLabelPath = "../MainBarWindow/Chrome/BattleTrack/BattleInfoLabel";
        [Export] public NodePath RoundInfoLabelPath = "../MainBarWindow/Chrome/BattleTrack/RoundInfoLabel";
        [Export] public NodePath PlayerMarkerPath = "../MainBarWindow/Chrome/BattleTrack/PlayerMarker";
        [Export] public NodePath PlayerHpLabelPath = "../MainBarWindow/Chrome/BattleTrack/PlayerHpLabel";
        [Export] public NodePath EnemyHpLabelPath = "../MainBarWindow/Chrome/BattleTrack/EnemyHpLabel";
        [Export] public NodePath ValidationPanelPath = "../MainBarWindow/Chrome/ConfigValidationPanel";
        [Export] public NodePath ValidationTitleLabelPath = "../MainBarWindow/Chrome/ConfigValidationPanel/TitleLabel";
        [Export] public NodePath ValidationBodyLabelPath = "../MainBarWindow/Chrome/ConfigValidationPanel/BodyLabel";
        [Export] public NodePath ActionModeOptionButtonPath = "../MainBarWindow/Chrome/ActionModeOptionButton";
        [Export] public NodePath LevelOptionButtonPath = "../MainBarWindow/Chrome/LevelOptionButton";
        [Export] public NodePath PlayerSlotTexturePath = "../MainBarWindow/Chrome/BattleTrack/PlayerSlotTexture";
        [Export] public NodePath PlayerSlotLabelPath = "../MainBarWindow/Chrome/BattleTrack/PlayerSlotLabel";
        [Export] public NodePath EnemySlotTexturePath = "../MainBarWindow/Chrome/BattleTrack/EnemySlotTexture";
        [Export] public NodePath EnemySlotLabelPath = "../MainBarWindow/Chrome/BattleTrack/EnemySlotLabel";

        [Export] public NodePath ActivityStatePath = "/root/InputActivityState";
        [Export] public NodePath BackpackStatePath = "/root/BackpackState";
        [Export] public NodePath AlchemyStatePath = "/root/AlchemyState";
        [Export] public NodePath PotionInventoryStatePath = "/root/PotionInventoryState";
        [Export] public NodePath SmithingStatePath = "/root/SmithingState";
        [Export] public NodePath GardenStatePath = "/root/GardenState";
        [Export] public NodePath MiningStatePath = "/root/MiningState";
        [Export] public NodePath FishingStatePath = "/root/FishingState";
        [Export] public NodePath TalismanStatePath = "/root/TalismanState";
        [Export] public NodePath CookingStatePath = "/root/CookingState";
        [Export] public NodePath FormationStatePath = "/root/FormationState";
        [Export] public NodePath EnlightenmentStatePath = "/root/EnlightenmentState";
        [Export] public NodePath BodyCultivationStatePath = "/root/BodyCultivationState";
        [Export] public NodePath PlayerProgressPath = "/root/PlayerProgressState";
        [Export] public NodePath EquippedItemsStatePath = "/root/EquippedItemsState";
        [Export] public NodePath ResourceWalletPath = "/root/ResourceWalletState";
        [Export] public NodePath LevelConfigLoaderPath = "/root/LevelConfigLoader";
        [Export] public NodePath ActionStatePath = "/root/PlayerActionState";
        [Export] public NodePath SubsystemMasteryStatePath = "/root/SubsystemMasteryState";

        // Explore progress is input-event driven (percent per input event), not AP-driven.
        [Export] public float ProgressPerInput = GameBalanceConstants.Explore.ProgressPerInput;
        [Export] public int InputsPerMoveFrame = GameBalanceConstants.Explore.InputsPerMoveFrame;
        [Export] public int InputsPerBattleRound = GameBalanceConstants.Explore.InputsPerBattleRound;
        [Export] public int MaxBossBattleRounds = GameBalanceConstants.Explore.MaxBossBattleRounds;
        [Export] public float MaxProgress = 100.0f;

        [Export] public float MonsterMovePxPerFrame = GameBalanceConstants.Explore.MonsterMovePxPerFrame;
        [Export] public float MonsterRespawnSpacing = GameBalanceConstants.Explore.MonsterRespawnSpacing;
        [Export] public float BattleTriggerX = 220.0f;

        private ProgressBar _progressBar = null!;
        private ProgressBar? _cultivationProgressBar;
        private Button? _breakthroughButton;
        private Label? _cultivationLabel;
        private Label _zoneLabel = null!;
        private Label _activityRateLabel = null!;
        private Label? _moveDebugLabel;
        private Label _realmStageLabel = null!;
        private Label _battleInfoLabel = null!;
        private Label _roundInfoLabel = null!;
        private Label _debugPanelLabel = null!;
        private Panel? _validationPanel;
        private Label? _validationTitleLabel;
        private Label? _validationBodyLabel;
        private OptionButton? _actionModeOptionButton;
        private OptionButton? _levelOptionButton;
        private readonly ExploreGameLogic _logic = new();
        private BattleTrackVisualizer _battleTrackVisualizer = null!;

        private InputActivityState? _activityState;
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
        private RecipeProgressState? _enlightenmentState;
        private RecipeProgressState? _bodyCultivationState;
        private PlayerProgressState? _playerProgressState;
        private EquippedItemsState? _equippedItemsState;
        private SubsystemMasteryState? _subsystemMasteryState;
        private ResourceWalletState? _resourceWalletState;
        private LevelConfigLoader? _levelConfigLoader;
        private PlayerActionState? _actionState;

        private string _currentZone = UiText.DefaultZoneName;
        private float _exploreProgress;
        private int _moveFrameCounter;
        private int _queueMoveInputPending;
        private int _battleRoundCounter;
        private int _pendingBattleInputEvents;
        private bool _inBattle;
        private int _battleMonsterIndex = -1;
        private string _battleMonsterId = "";
        private string _battleMonsterName = UiText.DefaultMonsterName;
        private int _enemyHp = 24;
        private int _enemyMaxHp = 24;
        private int _playerHp = 36;
        private int _playerMaxHp = 36;
        private int _enemyAttackPower = 4;
        private int _inputsPerBattleRoundRuntime = 18;
        private int _playerAttackPerRoundRuntime = 4;
        private int _enemyDamageDividerRuntime = 4;
        private int _enemyMinDamageRuntime = 1;
        private bool _debugPanelVisible;
        private bool _globalDebugOverlayEnabled;
        private bool _validationPanelEnabled = false;
        private int _validationScopeFilterIndex;
        private bool _validationOnlyActiveLevel;
        private bool _syncingActionModeOption;
        private bool _syncingLevelOption;
        private bool _actionModeOptionConnected;
        private bool _levelOptionConnected;
        private CharacterStatModifier _battleConsumableModifier;
        private static readonly string[] ActionModeIds =
        {
            PlayerActionState.ModeDungeon,
            PlayerActionState.ModeCultivation,
            PlayerActionState.ModeAlchemy,
            PlayerActionState.ModeSmithing,
            PlayerActionState.ModeGarden,
            PlayerActionState.ModeMining,
            PlayerActionState.ModeFishing,
            PlayerActionState.ModeTalisman,
            PlayerActionState.ModeCooking,
            PlayerActionState.ModeFormation,
            PlayerActionState.ModeEnlightenment,
            PlayerActionState.ModeBodyCultivation,
        };
        private bool _offlineSummaryVisible;
        private string _lastDropSummary = "none";
        private string _lastSimulationSummary = "no simulation";
        private bool _lastBattleEndedByBossTimeout;
        private bool _bossWeaknessInsightApplied;
        private bool _usedHealPotionThisBattle;
        private bool _hasLingqiDropBuffThisBattle;
        private readonly List<string> _consumedPotionsThisBattle = new();
        private int _totalBattleCount;
        private int _totalBattleWinCount;
        private string _simulationLevelFilterId = "";
        private string _simulationMonsterFilterId = "";
        private readonly List<BattleLogEntry> _recentBattleLogs = new();
        private static readonly string[] ValidationScopeFilters = { "all", "level", "monster", "drop_table", "config" };

        private sealed class BattleLogEntry
        {
            public long TimestampUnix { get; init; }
            public string ZoneName { get; init; } = "";
            public string MonsterName { get; init; } = "";
            public string MonsterType { get; init; } = "normal";
            public int RoundCount { get; init; }
            public string BattleResult { get; init; } = "胜利";
            public string RewardSummary { get; init; } = "无掉落";

            public Godot.Collections.Dictionary<string, Variant> ToDictionary()
            {
                return new Godot.Collections.Dictionary<string, Variant>
                {
                    ["timestamp_unix"] = TimestampUnix,
                    ["zone_name"] = ZoneName,
                    ["monster_name"] = MonsterName,
                    ["monster_type"] = MonsterType,
                    ["round_count"] = RoundCount,
                    ["battle_result"] = BattleResult,
                    ["reward_summary"] = RewardSummary,
                };
            }

            public static BattleLogEntry FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
            {
                return new BattleLogEntry
                {
                    TimestampUnix = data.ContainsKey("timestamp_unix") ? data["timestamp_unix"].AsInt64() : 0,
                    ZoneName = data.ContainsKey("zone_name") ? data["zone_name"].AsString() : "",
                    MonsterName = data.ContainsKey("monster_name") ? data["monster_name"].AsString() : "",
                    MonsterType = data.ContainsKey("monster_type") ? data["monster_type"].AsString() : "normal",
                    RoundCount = data.ContainsKey("round_count") ? data["round_count"].AsInt32() : 0,
                    BattleResult = data.ContainsKey("battle_result") ? data["battle_result"].AsString() : "胜利",
                    RewardSummary = data.ContainsKey("reward_summary") ? data["reward_summary"].AsString() : "无掉落",
                };
            }
        }

        public override void _Ready()
        {
            _progressBar = GetNode<ProgressBar>(ProgressBarPath);
            _cultivationProgressBar = GetNodeOrNull<ProgressBar>(CultivationProgressBarPath);
            _breakthroughButton = GetNodeOrNull<Button>(BreakthroughButtonPath);
            _cultivationLabel = GetNodeOrNull<Label>(CultivationLabelPath);
            _zoneLabel = GetNode<Label>(ZoneLabelPath);
            _activityRateLabel = GetNode<Label>(ActivityRateLabelPath);
            _moveDebugLabel = GetNodeOrNull<Label>(MoveDebugLabelPath);
            _realmStageLabel = GetNode<Label>(RealmStageLabelPath);
            _battleInfoLabel = GetNode<Label>(BattleInfoLabelPath);
            _roundInfoLabel = GetNode<Label>(RoundInfoLabelPath);
            _validationPanel = GetNodeOrNull<Panel>(ValidationPanelPath);
            _validationTitleLabel = GetNodeOrNull<Label>(ValidationTitleLabelPath);
            _validationBodyLabel = GetNodeOrNull<Label>(ValidationBodyLabelPath);
            _actionModeOptionButton = GetNodeOrNull<OptionButton>(ActionModeOptionButtonPath);
            _levelOptionButton = GetNodeOrNull<OptionButton>(LevelOptionButtonPath);

            _battleTrackVisualizer = new BattleTrackVisualizer();
            _battleTrackVisualizer.Name = nameof(BattleTrackVisualizer);
            AddChild(_battleTrackVisualizer);
            _battleTrackVisualizer.Bind(this, PlayerMarkerPath, PlayerHpLabelPath, EnemyHpLabelPath, PlayerSlotTexturePath, PlayerSlotLabelPath, EnemySlotTexturePath, EnemySlotLabelPath, InputsPerMoveFrame, MonsterMovePxPerFrame, MonsterRespawnSpacing);
            EnsureDebugPanel();

            _activityState = GetNodeOrNull<InputActivityState>(ActivityStatePath);
            _backpackState = GetNodeOrNull<BackpackState>(BackpackStatePath);
            _alchemyState = GetNodeOrNull<AlchemyState>(AlchemyStatePath);
            _potionInventoryState = GetNodeOrNull<PotionInventoryState>(PotionInventoryStatePath);
            _smithingState = GetNodeOrNull<SmithingState>(SmithingStatePath);
            _gardenState = GetNodeOrNull<GardenState>(GardenStatePath);
            _miningState = GetNodeOrNull<MiningState>(MiningStatePath);
            _fishingState = GetNodeOrNull<FishingState>(FishingStatePath);
            _talismanState = GetNodeOrNull<RecipeProgressState>(TalismanStatePath);
            _cookingState = GetNodeOrNull<RecipeProgressState>(CookingStatePath);
            _formationState = GetNodeOrNull<FormationState>(FormationStatePath);
            _enlightenmentState = GetNodeOrNull<RecipeProgressState>(EnlightenmentStatePath);
            _bodyCultivationState = GetNodeOrNull<RecipeProgressState>(BodyCultivationStatePath);
            _playerProgressState = GetNodeOrNull<PlayerProgressState>(PlayerProgressPath);
            _equippedItemsState = GetNodeOrNull<EquippedItemsState>(EquippedItemsStatePath);
            _subsystemMasteryState = GetNodeOrNull<SubsystemMasteryState>(SubsystemMasteryStatePath);
            _resourceWalletState = GetNodeOrNull<ResourceWalletState>(ResourceWalletPath);
            _levelConfigLoader = GetNodeOrNull<LevelConfigLoader>(LevelConfigLoaderPath);
            _actionState = GetNodeOrNull<PlayerActionState>(ActionStatePath);

            if (_activityState == null || !_battleTrackVisualizer.HasMarkers)
            {
                GD.PushError("ExploreProgressController: missing InputActivityState or monster markers.");
                return;
            }

            _activityState.InputBatchTick += OnInputBatchTick;
            if (_levelConfigLoader != null)
            {
                _levelConfigLoader.ConfigLoaded += OnLevelConfigLoaded;
            }

            ApplyLevelConfig();
            _zoneLabel.Text = _currentZone;
            _progressBar.MaxValue = MaxProgress;
            _progressBar.Value = _exploreProgress;
            _activityRateLabel.Visible = false;
            if (_validationPanel != null)
            {
                _validationPanel.Visible = false;
            }
            if (_cultivationProgressBar != null)
            {
                _cultivationProgressBar.MaxValue = 100.0;
            }
            if (_breakthroughButton != null)
            {
                _breakthroughButton.Pressed += OnBreakthroughPressed;
            }
            ConfigureActionModeOptionButton();
            ConfigureLevelOptionButton();
            _simulationLevelFilterId = _levelConfigLoader?.ActiveLevelId ?? "";
            UpdateRealmStageLabel();
            RefreshCultivationPanel();
            ResetTrackVisual();
            RefreshDebugPanel();
            RefreshValidationPanel();
            RefreshMoveDebugLabel();
            ApplyGlobalDebugOverlayVisibility();
            RefreshActionModeOptionButton();
            RefreshLevelOptionButton();

            if (_playerProgressState != null)
            {
                _playerProgressState.RealmProgressChanged += OnRealmProgressChanged;
            }
            if (_actionState != null)
            {
                _actionState.ActionChanged += OnActionChanged;
                SyncActionTargetToActiveLevel();
            }
        }

        public override void _ExitTree()
        {
            if (_activityState != null)
            {
                _activityState.InputBatchTick -= OnInputBatchTick;
            }
            if (_levelConfigLoader != null)
            {
                _levelConfigLoader.ConfigLoaded -= OnLevelConfigLoaded;
            }
            if (_playerProgressState != null)
            {
                _playerProgressState.RealmProgressChanged -= OnRealmProgressChanged;
            }
            if (_actionState != null)
            {
                _actionState.ActionChanged -= OnActionChanged;
            }
            if (_breakthroughButton != null)
            {
                _breakthroughButton.Pressed -= OnBreakthroughPressed;
            }
            if (_actionModeOptionButton != null)
            {
                _actionModeOptionButton.ItemSelected -= OnActionModeOptionSelected;
            }
            if (_levelOptionButton != null)
            {
                _levelOptionButton.ItemSelected -= OnLevelOptionSelected;
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
            {
                if (keyEvent.Keycode == Key.F8)
                {
                    _debugPanelVisible = !_debugPanelVisible;
                    _debugPanelLabel.Visible = _debugPanelVisible;
                    RefreshDebugPanel();
                    RefreshValidationPanel();
                }
                else if (keyEvent.Keycode == Key.F9)
                {
                    if (_levelConfigLoader != null)
                    {
                        _lastSimulationSummary = RunSimulationWithFilters(200);
                    }
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F10)
                {
                    if (_levelConfigLoader != null)
                    {
                        _lastSimulationSummary = RunSimulationWithFilters(1000);
                    }
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F6)
                {
                    CycleSimulationLevelFilter();
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F5)
                {
                    if (_levelConfigLoader != null && _levelConfigLoader.TrySetNextUnlockedLevelAsActive())
                    {
                        ApplyLevelConfig();
                        _zoneLabel.Text = _currentZone;
                        _exploreProgress = 0.0f;
                        SyncLogicFromControllerState();
                        _progressBar.Value = 0.0f;
                        ResetTrackVisual();
                        RefreshLevelOptionButton();
                    }
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F4)
                {
                    _actionState?.ToggleMode();
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F7)
                {
                    CycleSimulationMonsterFilter();
                    RefreshDebugPanel();
                }
                else if (keyEvent.Keycode == Key.F11)
                {
                    CycleValidationScopeFilter();
                    RefreshValidationPanel();
                }
                else if (keyEvent.Keycode == Key.F12)
                {
                    _validationOnlyActiveLevel = !_validationOnlyActiveLevel;
                    RefreshValidationPanel();
                }
            }
        }

        public override void _Process(double delta)
        {
            _battleTrackVisualizer.AdvanceEnemyVisual(delta);
        }

        private string RunSimulationWithFilters(int battleCount)
        {
            if (_levelConfigLoader == null)
            {
                return "loader unavailable";
            }

            string levelId = string.IsNullOrEmpty(_simulationLevelFilterId)
                ? _levelConfigLoader.ActiveLevelId
                : _simulationLevelFilterId;

            return _levelConfigLoader.RunBattleSimulationFiltered(
                battleCount,
                levelId,
                _simulationMonsterFilterId);
        }

        private void CycleSimulationLevelFilter()
        {
            if (_levelConfigLoader == null)
            {
                return;
            }

            var levels = _levelConfigLoader.GetLevelIds();
            if (levels.Count == 0)
            {
                return;
            }

            int currentIndex = -1;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] == _simulationLevelFilterId)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                _simulationLevelFilterId = levels[0];
            }
            else
            {
                int next = (currentIndex + 1) % (levels.Count + 1);
                _simulationLevelFilterId = next >= levels.Count ? "" : levels[next];
            }

            _simulationMonsterFilterId = "";
        }

        private void CycleSimulationMonsterFilter()
        {
            if (_levelConfigLoader == null)
            {
                return;
            }

            string levelId = string.IsNullOrEmpty(_simulationLevelFilterId)
                ? _levelConfigLoader.ActiveLevelId
                : _simulationLevelFilterId;

            var monsters = _levelConfigLoader.GetSpawnMonsterIds(levelId);
            if (monsters.Count == 0)
            {
                _simulationMonsterFilterId = "";
                return;
            }

            int currentIndex = -1;
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i] == _simulationMonsterFilterId)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                _simulationMonsterFilterId = monsters[0];
            }
            else
            {
                int next = (currentIndex + 1) % (monsters.Count + 1);
                _simulationMonsterFilterId = next >= monsters.Count ? "" : monsters[next];
            }
        }

        private void OnRealmProgressChanged(int realmLevel, double realmExp, double realmExpRequired)
        {
            UpdateRealmStageLabel();
            RefreshCultivationPanel();
        }

        private void OnActionChanged(string actionId, string actionTargetId, string actionVariant)
        {
            _offlineSummaryVisible = false;
            if (HasDungeonCapability())
            {
                _battleInfoLabel.Visible = false;
                _progressBar.Value = _exploreProgress;
                _roundInfoLabel.Text = $"{UiText.ExploreProgress(_exploreProgress)} | {BuildFrontMoveStatus()}";
            }
            else
            {
                _battleInfoLabel.Text = GetPausedModeLabel();
                _battleInfoLabel.Visible = true;
                _progressBar.Value = IsAlchemyMode() && _alchemyState != null && _alchemyState.RequiredProgress > 0.0f
                    ? _alchemyState.CurrentProgress / _alchemyState.RequiredProgress * 100.0f
                    : 0.0f;
                _roundInfoLabel.Text = GetPausedModeRoundLabel();
            }

            RefreshActionModeOptionButton();
            RefreshDebugPanel();
        }

        private void ConfigureActionModeOptionButton()
        {
            if (_actionModeOptionButton == null)
            {
                return;
            }

            _actionModeOptionButton.Clear();
            for (int i = 0; i < ActionModeIds.Length; i++)
            {
                _actionModeOptionButton.AddItem(ExploreProgressPresentationRules.GetActionModeOptionText(i), i);
            }
            _actionModeOptionButton.TooltipText = "切换主行为（等同 F4）";
            if (_actionModeOptionConnected)
            {
                _actionModeOptionButton.ItemSelected -= OnActionModeOptionSelected;
            }
            _actionModeOptionButton.ItemSelected += OnActionModeOptionSelected;
            _actionModeOptionConnected = true;
        }

        private void ConfigureLevelOptionButton()
        {
            if (_levelOptionButton == null)
            {
                return;
            }

            if (_levelOptionConnected)
            {
                _levelOptionButton.ItemSelected -= OnLevelOptionSelected;
            }
            _levelOptionButton.ItemSelected += OnLevelOptionSelected;
            _levelOptionConnected = true;
            _levelOptionButton.TooltipText = "切换已解锁副本";
        }

        private void RefreshActionModeOptionButton()
        {
            if (_actionModeOptionButton == null)
            {
                return;
            }

            _syncingActionModeOption = true;
            int selected = GetActionModeSelectedIndex();
            _actionModeOptionButton.Select(selected);
            _actionModeOptionButton.Text = GetActionModeOptionText(selected);
            _syncingActionModeOption = false;
        }

        private void RefreshLevelOptionButton()
        {
            if (_levelOptionButton == null || _levelConfigLoader == null)
            {
                return;
            }

            _syncingLevelOption = true;
            _levelOptionButton.Clear();
            var unlocked = _levelConfigLoader.GetUnlockedLevelIds();
            int selectedIndex = -1;
            for (int i = 0; i < unlocked.Count; i++)
            {
                string levelId = unlocked[i];
                string levelName = _levelConfigLoader.GetLevelName(levelId);
                string text = string.IsNullOrEmpty(levelName) ? levelId : levelName;
                _levelOptionButton.AddItem(text, i);
                _levelOptionButton.SetItemMetadata(i, levelId);
                if (levelId == _levelConfigLoader.ActiveLevelId)
                {
                    selectedIndex = i;
                }
            }

            if (_levelOptionButton.ItemCount > 0)
            {
                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }
                _levelOptionButton.Select(selectedIndex);
                string selectedLevelId = _levelOptionButton.GetItemMetadata(selectedIndex).AsString();
                _levelOptionButton.TooltipText = string.IsNullOrEmpty(selectedLevelId)
                    ? "切换已解锁副本"
                    : $"当前副本: {selectedLevelId}";
            }
            _syncingLevelOption = false;
        }

        private void OnActionModeOptionSelected(long index)
        {
            if (_syncingActionModeOption || _actionState == null)
            {
                return;
            }

            int selectedIndex = Mathf.Clamp((int)index, 0, ActionModeIds.Length - 1);
            string modeId = ActionModeIds[selectedIndex];
            string targetLevelId = modeId == PlayerActionState.ModeDungeon ? _levelConfigLoader?.ActiveLevelId ?? string.Empty : string.Empty;
            _actionState.SetAction(modeId, targetLevelId);
        }

        private void OnLevelOptionSelected(long index)
        {
            if (_syncingLevelOption || _levelOptionButton == null || _levelConfigLoader == null)
            {
                return;
            }

            int selectedIndex = (int)index;
            if (selectedIndex < 0 || selectedIndex >= _levelOptionButton.ItemCount)
            {
                return;
            }

            string levelId = _levelOptionButton.GetItemMetadata(selectedIndex).AsString();
            if (string.IsNullOrEmpty(levelId))
            {
                return;
            }

            if (_levelConfigLoader.TrySetActiveLevelIfUnlocked(levelId))
            {
                if (_actionState != null && _actionState.IsDungeonAction)
                {
                    _actionState.SetAction(_actionState.ActionId, levelId, _actionState.ActionVariant);
                }
                ApplyLevelConfig();
                _zoneLabel.Text = _currentZone;
                _exploreProgress = 0.0f;
                SyncLogicFromControllerState();
                _progressBar.Value = 0.0f;
                ResetTrackVisual();
                RefreshDebugPanel();
            }
        }

        private void OnLevelConfigLoaded(string levelId, string levelName)
        {
            SyncActionTargetToActiveLevel();
            ApplyLevelConfig();
            _zoneLabel.Text = _currentZone;
            RefreshLevelOptionButton();
            RefreshValidationPanel();
        }

        private void OnInputBatchTick(int inputEvents, double apFinal)
        {
            // AP is displayed for resource settlement transparency; progress uses inputEvents only.
            _activityRateLabel.Text = UiText.BatchInputAndAp(inputEvents, apFinal);
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
            if (inputEvents <= 0)
            {
                return;
            }

            if (!HasDungeonCapability())
            {
                _battleInfoLabel.Text = GetPausedModeLabel();
                _battleInfoLabel.Visible = true;
                _roundInfoLabel.Text = GetPausedModeRoundLabel();

                if (IsAlchemyMode())
                {
                    AdvanceAlchemyByInput(inputEvents);
                }
                else if (IsSmithingMode())
                {
                    AdvanceSmithingByInput(inputEvents);
                }
                else if (IsGardenMode())
                {
                    AdvanceGardenByInput(inputEvents);
                }
                else if (IsMiningMode())
                {
                    AdvanceMiningByInput(inputEvents);
                }
                else if (IsFishingMode())
                {
                    AdvanceFishingByInput(inputEvents);
                }
                else if (IsGenericRecipeMode())
                {
                    AdvanceGenericRecipeByInput(inputEvents);
                }
                else
                {
                    _progressBar.Value = 0.0f;
                }
                return;
            }

            if (_inBattle)
            {
                AdvanceBattleByInput(inputEvents);
                return;
            }

            if (!AdvanceExploreByInput(inputEvents))
            {
                return;
            }

            TryStartBattle();
        }

        private bool AdvanceExploreByInput(int inputEvents)
        {
            double afkSeconds = _activityState?.SecondsSinceLastInputBeforeLatestBatch ?? 0.0;
            double progressMultiplier = AfkDetectionRules.GetProgressMultiplier(afkSeconds);
            if (progressMultiplier <= 0.0)
            {
                _battleInfoLabel.Text = "主行为：副本（AFK 暂停）";
                _battleInfoLabel.Visible = true;
                _roundInfoLabel.Text = $"空闲 {afkSeconds:0}s，探索暂停";
                _progressBar.Value = _exploreProgress;
                RefreshMoveDebugLabel();
                RefreshDebugPanel();
                return false;
            }

            SyncLogicFromControllerState();
            ExploreGameLogic.ExploreAdvanceResult result = _logic.AdvanceExploreByInput(inputEvents, (float)(ProgressPerInput * progressMultiplier), MaxProgress);
            _progressBar.Value = _exploreProgress;
            int frames = MoveMonsterQueueByInputs(inputEvents);
            _logic.RegisterTrackMovement(frames, _battleTrackVisualizer.QueueMoveInputPending);
            SyncControllerStateFromLogic();
            _progressBar.Value = _exploreProgress;

            _battleInfoLabel.Text = UiText.ExploreFrame(_moveFrameCounter);
            _battleInfoLabel.Visible = false;
            _roundInfoLabel.Text = progressMultiplier < 1.0
                ? $"{UiText.ExploreProgress(_exploreProgress)} | {BuildFrontMoveStatus()} | 反挂机 {progressMultiplier:0.0}x"
                : $"{UiText.ExploreProgress(_exploreProgress)} | {BuildFrontMoveStatus()}";
            RefreshMoveDebugLabel();

            if (result.CompletedLevel)
            {
                if (TryStartBossChallenge())
                {
                    return true;
                }

                _battleInfoLabel.Text = UiText.ZoneComplete;
                _battleInfoLabel.Visible = true;
            }

            UpdateHpLabels();
            RefreshActorSlots();
            RefreshDebugPanel();
            return true;
        }

        private int MoveMonsterQueueByInputs(int inputEvents)
        {
            return _battleTrackVisualizer.MoveMonsterQueueByInputs(inputEvents, BuildMonsterAssignment);
        }

        private string BuildFrontMoveStatus()
        {
            return _battleTrackVisualizer.BuildFrontMoveStatus();
        }

        private void RefreshMoveDebugLabel()
        {
            if (_moveDebugLabel == null)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.Append(BuildMoveDebugStatus());
            sb.Append('\n');
            sb.Append(BuildBattleDebugStatus());
            sb.Append('\n');
            sb.Append(BuildInputSourceDebugStatus());
            sb.Append('\n');
            sb.Append(BuildWaveDebugStatus());
            _moveDebugLabel.Text = sb.ToString();
        }

        private void ApplyGlobalDebugOverlayVisibility()
        {
            if (_moveDebugLabel != null)
            {
                _moveDebugLabel.Visible = _globalDebugOverlayEnabled;
            }
        }

        private float GetRightMostMonsterX()
        {
            BattleTrackVisualizer.FrontMonsterInfo front = _battleTrackVisualizer.GetFrontMonsterInfo();
            return front.Index >= 0 ? front.X : 0.0f;
        }

        private string BuildMoveDebugStatus()
        {
            return _battleTrackVisualizer.BuildMoveDebugStatus();
        }

        private string BuildBattleDebugStatus()
        {
            return ExploreProgressDebugRules.BuildBattleDebugStatus(_inBattle, _battleRoundCounter, _inputsPerBattleRoundRuntime, _pendingBattleInputEvents, _enemyHp, _playerAttackPerRoundRuntime);
        }

        private string BuildInputSourceDebugStatus()
        {
            if (_activityState == null)
            {
                return "调试-输入：InputActivityState 不可用";
            }

            return $"调试-输入：batch={_activityState.InputEventsThisSecond} ap={_activityState.ApFinal:0.00} | 键={_activityState.KeyDownCount} 鼠键={_activityState.MouseClickCount} 滚轮={_activityState.MouseScrollSteps} 移动px={_activityState.MouseMoveDistancePx:0} 手柄键={_activityState.JoypadButtonCount} 轴={_activityState.JoypadAxisInputCount}";
        }

        private string BuildWaveDebugStatus()
        {
            SyncLogicFromControllerState();
            return _battleTrackVisualizer.BuildWaveDebugStatus(_levelConfigLoader, _logic.InBattle, _logic.BattleMonsterIndex);
        }

        private void TryStartBattle()
        {
            if (TryStartBossChallenge())
            {
                return;
            }

            BattleTrackVisualizer.FrontMonsterInfo front = _battleTrackVisualizer.GetFrontMonsterInfo();
            MonsterStatProfile? profile = null;
            if (_levelConfigLoader != null &&
                !string.IsNullOrEmpty(front.MonsterId) &&
                _levelConfigLoader.TryGetMonsterStatProfile(front.MonsterId, out MonsterStatProfile loadedProfile))
            {
                profile = loadedProfile;
            }

            SyncLogicFromControllerState();
            if (!_logic.TryStartEncounter(
                front.Index,
                front.X,
                BattleTriggerX,
                front.MonsterId,
                profile,
                InputsPerBattleRound,
                _levelConfigLoader?.BaseEncounterRate ?? 0.18,
                _playerProgressState?.RealmLevel ?? 1,
                _levelConfigLoader?.ActiveLevelDangerLevel ?? 1,
                Random.Shared.NextDouble()))
            {
                return;
            }

            SyncControllerStateFromLogic();
            _battleMonsterIndex = _battleTrackVisualizer.ResolveBattleMonsterIndex(_battleMonsterIndex);
            _usedHealPotionThisBattle = false;
            _hasLingqiDropBuffThisBattle = false;
            _battleConsumableModifier = default;
            _consumedPotionsThisBattle.Clear();
            ApplyAutoConsumables(isBattleStart: true);
            SyncLogicFromControllerState();
            _battleInfoLabel.Text = UiText.Encounter(_battleMonsterName);
            _battleInfoLabel.Visible = true;
            _roundInfoLabel.Text = UiText.BattleRound(0, _battleMonsterName, _enemyMaxHp);
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
        }

        private bool TryStartBossChallenge()
        {
            if (_inBattle || _levelConfigLoader == null || _exploreProgress < MaxProgress)
            {
                return false;
            }

            string bossMonsterId = _levelConfigLoader.GetBossMonsterId();
            if (!DungeonLoopRules.ShouldEnterBossChallenge(_exploreProgress >= MaxProgress, bossMonsterId))
            {
                return false;
            }

            MonsterStatProfile? profile = null;
            if (_levelConfigLoader.TryGetMonsterStatProfile(bossMonsterId, out MonsterStatProfile loadedProfile))
            {
                profile = loadedProfile;
            }

            SyncLogicFromControllerState();
            if (!_logic.TryStartBossChallenge(bossMonsterId, profile, InputsPerBattleRound, MaxProgress))
            {
                return false;
            }

            SyncControllerStateFromLogic();
            _battleMonsterIndex = _battleTrackVisualizer.ResolveBattleMonsterIndex(_battleMonsterIndex);
            _usedHealPotionThisBattle = false;
            _hasLingqiDropBuffThisBattle = false;
            _consumedPotionsThisBattle.Clear();
            ApplyAutoConsumables(isBattleStart: true);
            SyncLogicFromControllerState();
            _battleInfoLabel.Text = $"{UiText.ZoneComplete} | Boss 挑战";
            _battleInfoLabel.Visible = true;
            _roundInfoLabel.Text = UiText.BattleRound(0, _battleMonsterName, _enemyMaxHp);
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
            return true;
        }

        private int FindFrontMonsterIndex()
        {
            return _battleTrackVisualizer.GetFrontMonsterInfo().Index;
        }

        private void AdvanceBattleByInput(int inputEvents)
        {
            SyncLogicFromControllerState();
            CharacterStatBlock playerBaseStats = PlayerBaseStatRules.BuildBaseStats(
                _playerProgressState?.RealmLevel ?? 1,
                _levelConfigLoader?.PlayerBaseHp ?? _playerMaxHp,
                _levelConfigLoader?.PlayerAttackPerRound ?? _playerAttackPerRoundRuntime);
            playerBaseStats = CharacterStatRules.BuildFinalStats(playerBaseStats, BuildNonEquipmentModifiers());
            ExploreGameLogic.BattleAdvanceResult progress = _logic.AdvanceBattleByInput(
                inputEvents,
                playerBaseStats,
                _equippedItemsState?.GetEquippedProfiles() ?? System.Array.Empty<EquipmentStatProfile>(),
                IsBossChallengeBattle(),
                MaxBossBattleRounds,
                afterRoundResolved: () =>
                {
                    SyncControllerStateFromLogic();
                    ApplyAutoConsumables(isBattleStart: false);
                    SyncLogicFromControllerState();
                });
            SyncControllerStateFromLogic();
            if (progress.RoundsResolved <= 0)
            {
                _battleInfoLabel.Text = UiText.BattleInProgress(_battleMonsterName);
                _battleInfoLabel.Visible = true;
                _roundInfoLabel.Text = $"蓄力 {progress.PendingInputs}/{progress.Threshold} | {UiText.BattleRound(_battleRoundCounter, _battleMonsterName, _enemyHp)}";
                UpdateHpLabels();
                RefreshActorSlots();
                RefreshMoveDebugLabel();
                RefreshDebugPanel();
                return;
            }

            if (progress.BattleEnded)
            {
                if (progress.Outcome == BattleOutcome.PlayerWon)
                {
                    CompleteBattle();
                }
                else
                {
                    HandleBattleDefeat();
                }
                return;
            }

            _battleInfoLabel.Text = UiText.BattleInProgress(_battleMonsterName);
            _battleInfoLabel.Visible = true;
            _roundInfoLabel.Text = $"{UiText.BattleRound(_battleRoundCounter, _battleMonsterName, _enemyHp)} | next {_pendingBattleInputEvents}/{progress.Threshold}";
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
        }

        private void HandleBattleDefeat()
        {
            bool isBossBattle = IsBossChallengeBattle();
            int defeatedMonsterIndex = _battleMonsterIndex;

            string defeatResult = _lastBattleEndedByBossTimeout ? "超时" : "失败";
            AppendBattleLog(0, 0, 0, "none", defeatResult);

            SyncLogicFromControllerState();
            BattleDefeatDecision defeat = _logic.HandleBattleDefeat(_levelConfigLoader?.ActiveLevelId ?? string.Empty, isBossBattle);
            SyncControllerStateFromLogic();
            _progressBar.Value = _exploreProgress;

            if (_levelConfigLoader != null && defeat.ShouldResetLevel)
            {
                _levelConfigLoader.TrySetActiveLevel(defeat.ActiveLevelId);
            }

            ApplyLevelConfig();
            _zoneLabel.Text = _currentZone;
            if (isBossBattle)
            {
                _battleMonsterIndex = -1;
                _battleMonsterId = string.Empty;
                ResetTrackVisual();
                _battleInfoLabel.Text = _lastBattleEndedByBossTimeout
                    ? "Boss 超时败退，本轮副本重新开始"
                    : "Boss 战败，本轮副本重新开始";
            }
            else
            {
                _battleTrackVisualizer.RecycleMonsterAt(defeatedMonsterIndex, BuildMonsterAssignment);
                _battleInfoLabel.Text = $"战败，未获得 {_battleMonsterName} 的掉落";
                _battleMonsterIndex = -1;
                _battleMonsterId = string.Empty;
            }
            _battleInfoLabel.Visible = true;
            _roundInfoLabel.Text = UiText.WaitingInput;
            RefreshLevelOptionButton();
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
            _lastBattleEndedByBossTimeout = false;
            _bossWeaknessInsightApplied = false;
            _usedHealPotionThisBattle = false;
            _hasLingqiDropBuffThisBattle = false;
            _consumedPotionsThisBattle.Clear();
            SyncLogicFromControllerState();
        }

        public bool CanApplyBossWeaknessInsight(int dungeonMasteryLevel)
        {
            SyncLogicFromControllerState();
            return _logic.CanApplyBossWeaknessInsight(IsBossChallengeBattle(), dungeonMasteryLevel);
        }

        public bool TryApplyBossWeaknessInsight()
        {
            SyncLogicFromControllerState();
            if (!_logic.TryApplyBossWeaknessInsight(IsBossChallengeBattle()))
            {
                return false;
            }

            SyncControllerStateFromLogic();
            _battleInfoLabel.Text = $"{_battleMonsterName} 弱点已看破，Boss 属性下降 10%";
            _battleInfoLabel.Visible = true;
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshDebugPanel();
            return true;
        }

        private void CompleteBattle()
        {
            bool isBossBattle = IsBossChallengeBattle();
            int defeatedMonsterIndex = _battleMonsterIndex;
            string rewardMonsterId = _battleMonsterId;
            string rewardMonsterName = _battleMonsterName;
            SyncLogicFromControllerState();
            BattleVictoryDecision victory = _logic.CompleteBattle(_levelConfigLoader?.ActiveLevelId ?? string.Empty, isBossBattle);
            SyncControllerStateFromLogic();
            _roundInfoLabel.Text = UiText.BattleRound(_battleRoundCounter, _battleMonsterName, 0);
            _battleInfoLabel.Text = UiText.BattleVictory(_battleMonsterName);
            _battleInfoLabel.Visible = true;

            if (_levelConfigLoader != null &&
                victory.ShouldTryBossUnlock &&
                _levelConfigLoader.TryMarkBossDefeatedAndUnlockNext(victory.ActiveLevelId, victory.MonsterId, out string unlockedLevelId) &&
                !string.IsNullOrEmpty(unlockedLevelId))
            {
                _battleInfoLabel.Text = $"{UiText.BattleVictory(_battleMonsterName)} | 已解锁 {unlockedLevelId}";
            }

            if (victory.ShouldApplyBattleRewards)
            {
                _battleMonsterId = rewardMonsterId;
                _battleMonsterName = rewardMonsterName;
                ApplyBattleRewards();
                _battleMonsterId = string.Empty;
                _battleMonsterName = rewardMonsterName;
            }

            if (victory.ShouldApplyLevelCompletionRewards)
            {
                ApplyLevelCompletionRewards();
            }

            _progressBar.Value = _exploreProgress;

            if (isBossBattle)
            {
                ResetTrackVisual();
            }
            else if (defeatedMonsterIndex >= 0)
            {
                _battleTrackVisualizer.RecycleMonsterAt(defeatedMonsterIndex, BuildMonsterAssignment);
            }

            _battleMonsterIndex = -1;
            _battleMonsterId = string.Empty;
            _pendingBattleInputEvents = 0;
            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
            SyncLogicFromControllerState();
        }

        private bool IsBossChallengeBattle()
        {
            return _levelConfigLoader != null
                && _exploreProgress >= MaxProgress
                && !string.IsNullOrEmpty(_battleMonsterId)
                && _levelConfigLoader.IsBossMonsterForLevel(_levelConfigLoader.ActiveLevelId, _battleMonsterId);
        }

        private void ResetTrackVisual()
        {
            SyncLogicFromControllerState();
            _logic.ResetBattleTrackState(InputsPerBattleRound);
            SyncControllerStateFromLogic();
            _battleInfoLabel.Text = "";
            _battleInfoLabel.Visible = false;
            _roundInfoLabel.Text = UiText.WaitingInput;
            _battleTrackVisualizer.ResetTrackVisual(BuildMonsterAssignment);
            _battleTrackVisualizer.ApplyGameState(_logic, _levelConfigLoader);
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
        }

        private void UpdateHpLabels()
        {
            SyncLogicFromControllerState();
            _battleTrackVisualizer.ApplyGameState(_logic, _levelConfigLoader);
        }

        private void RefreshActorSlots()
        {
            SyncLogicFromControllerState();
            _battleTrackVisualizer.ApplyGameState(_logic, _levelConfigLoader);
        }

        private void UpdateRealmStageLabel()
        {
            if (_playerProgressState == null)
            {
                _realmStageLabel.Text = UiText.RealmFallback;
                return;
            }

            double required = Mathf.Max(1.0f, (float)_playerProgressState.RealmExpRequired);
            double percent = _playerProgressState.RealmExp / required * 100.0;
            _realmStageLabel.Text = UiText.RealmStage(_playerProgressState.RealmLevel, percent);
        }

        private void SyncLogicFromControllerState()
        {
            _logic.CurrentZoneName = _currentZone;
            _logic.ExploreProgress = _exploreProgress;
            _logic.MoveFrameCounter = _moveFrameCounter;
            _logic.QueueMoveInputPending = _queueMoveInputPending;
            _logic.InBattle = _inBattle;
            _logic.BattleRoundCounter = _battleRoundCounter;
            _logic.PendingBattleInputEvents = _pendingBattleInputEvents;
            _logic.BattleMonsterIndex = _battleMonsterIndex;
            _logic.BattleMonsterId = _battleMonsterId;
            _logic.BattleMonsterName = _battleMonsterName;
            _logic.EnemyHp = _enemyHp;
            _logic.EnemyMaxHp = _enemyMaxHp;
            _logic.PlayerHp = _playerHp;
            _logic.PlayerMaxHp = _playerMaxHp;
            _logic.EnemyAttackPower = _enemyAttackPower;
            _logic.InputsPerBattleRoundRuntime = _inputsPerBattleRoundRuntime;
            _logic.PlayerAttackPerRoundRuntime = _playerAttackPerRoundRuntime;
            _logic.EnemyDamageDividerRuntime = _enemyDamageDividerRuntime;
            _logic.EnemyMinDamageRuntime = _enemyMinDamageRuntime;
            _logic.LastBattleEndedByBossTimeout = _lastBattleEndedByBossTimeout;
            _logic.BossWeaknessInsightApplied = _bossWeaknessInsightApplied;
            _logic.TotalBattleCount = _totalBattleCount;
            _logic.TotalBattleWinCount = _totalBattleWinCount;
        }

        private void SyncControllerStateFromLogic()
        {
            _currentZone = _logic.CurrentZoneName;
            _exploreProgress = _logic.ExploreProgress;
            _moveFrameCounter = _logic.MoveFrameCounter;
            _queueMoveInputPending = _logic.QueueMoveInputPending;
            _inBattle = _logic.InBattle;
            _battleRoundCounter = _logic.BattleRoundCounter;
            _pendingBattleInputEvents = _logic.PendingBattleInputEvents;
            _battleMonsterIndex = _logic.BattleMonsterIndex;
            _battleMonsterId = _logic.BattleMonsterId;
            _battleMonsterName = _logic.BattleMonsterName;
            _enemyHp = _logic.EnemyHp;
            _enemyMaxHp = _logic.EnemyMaxHp;
            _playerHp = _logic.PlayerHp;
            _playerMaxHp = _logic.PlayerMaxHp;
            _enemyAttackPower = _logic.EnemyAttackPower;
            _inputsPerBattleRoundRuntime = _logic.InputsPerBattleRoundRuntime;
            _playerAttackPerRoundRuntime = _logic.PlayerAttackPerRoundRuntime;
            _enemyDamageDividerRuntime = _logic.EnemyDamageDividerRuntime;
            _enemyMinDamageRuntime = _logic.EnemyMinDamageRuntime;
            _lastBattleEndedByBossTimeout = _logic.LastBattleEndedByBossTimeout;
            _bossWeaknessInsightApplied = _logic.BossWeaknessInsightApplied;
            _totalBattleCount = _logic.TotalBattleCount;
            _totalBattleWinCount = _logic.TotalBattleWinCount;
        }

        private BattleTrackVisualizer.MonsterAssignment BuildMonsterAssignment()
        {
            string monsterId = _levelConfigLoader?.RollSpawnMonsterId() ?? string.Empty;
            int threshold = Mathf.Max(1, InputsPerMoveFrame);
            if (_levelConfigLoader != null &&
                !string.IsNullOrEmpty(monsterId) &&
                _levelConfigLoader.TryGetMonsterMoveRule(monsterId, out _, out int configured))
            {
                threshold = Mathf.Max(1, configured);
            }

            return new BattleTrackVisualizer.MonsterAssignment(monsterId, threshold);
        }

        private void ApplyLevelConfig()
        {
            if (_offlineSummaryVisible)
            {
                return;
            }

            if (_levelConfigLoader == null)
            {
                return;
            }

            _currentZone = _levelConfigLoader.ActiveLevelName;
            ProgressPerInput = (float)(_levelConfigLoader.ProgressPer100Inputs / 100.0);
            CharacterStatBlock playerBaseStats = PlayerBaseStatRules.BuildBaseStats(
                _playerProgressState?.RealmLevel ?? 1,
                _levelConfigLoader.PlayerBaseHp,
                _levelConfigLoader.PlayerAttackPerRound);
            playerBaseStats = CharacterStatRules.BuildFinalStats(playerBaseStats, BuildNonEquipmentModifiers());
            SyncLogicFromControllerState();
            _logic.ApplyLevelConfig(
                _currentZone,
                playerBaseStats.MaxHp,
                playerBaseStats.Attack,
                Mathf.Max(1, _levelConfigLoader.EnemyDamageDivider),
                Mathf.Max(1, _levelConfigLoader.EnemyMinDamagePerRound));
            SyncControllerStateFromLogic();
        }

        private void ConfigureBattleMonster()
        {
            MonsterStatProfile? profile = null;
            if (_levelConfigLoader != null && !string.IsNullOrEmpty(_battleMonsterId) && _levelConfigLoader.TryGetMonsterStatProfile(_battleMonsterId, out MonsterStatProfile loadedProfile))
            {
                profile = loadedProfile;
            }

            BattleStartSetup setup = BattleStartRules.BuildStartSetup(_battleMonsterId, profile, InputsPerBattleRound);
            _battleMonsterId = setup.MonsterId;
            _battleMonsterName = setup.MonsterName;
            _enemyMaxHp = setup.EnemyMaxHp;
            _enemyAttackPower = setup.EnemyAttack;
            _inputsPerBattleRoundRuntime = setup.InputsPerRound;
            _battleRoundCounter = setup.BattleRoundCounter;
            _pendingBattleInputEvents = setup.PendingBattleInputEvents;
            _enemyHp = _enemyMaxHp;
        }

        private void ApplyBattleRewards()
        {
            double lingqi = 0.0;
            double insight = 0.0;
            int spiritStones = 0;
            string itemPart = "none";
            int dropCount = 0;

            if (_levelConfigLoader != null && !string.IsNullOrEmpty(_battleMonsterId))
            {
                bool isBossBattle = IsBossChallengeBattle();
                var drops = _levelConfigLoader.RollMonsterDrops(_battleMonsterId);
                dropCount = drops.Count;
                itemPart = drops.Count > 0 ? BuildDropSummary(drops) : "none";
                ApplyResourceAndItemRewards(0.0, 0.0, drops, "battle_drop");
                if (_backpackState != null)
                {
                    foreach (EquipmentInstanceData equipmentDrop in _levelConfigLoader.GetLastGeneratedEquipmentDrops())
                    {
                        if (!_backpackState.HasEquipment(equipmentDrop.EquipmentId))
                        {
                            _backpackState.AddEquipmentInstance(equipmentDrop);
                        }
                    }
                }

                if (_levelConfigLoader.TryRollMonsterSettlementReward(_battleMonsterId, out lingqi, out insight))
                {
                    if (_hasLingqiDropBuffThisBattle)
                    {
                        lingqi *= 1.0 + ConsumableUsageRules.JulingSanLingqiBuffPercent;
                    }
                    ApplyResourceAndItemRewards(lingqi, insight, new Dictionary<string, int>(), "battle_settle");
                }

                if (_levelConfigLoader.TryGetMonsterStatProfile(_battleMonsterId, out MonsterStatProfile profile))
                {
                    spiritStones = RewardRules.CalculateBattleSpiritStoneReward(profile.MoveCategory, isBossBattle || profile.IsBoss, _levelConfigLoader?.ActiveLevelIndex ?? 0);
                    if (spiritStones > 0)
                    {
                        _resourceWalletState?.AddSpiritStones(spiritStones);
                    }
                }
            }

            BattleRewardDecision rewardDecision = RewardRules.DetermineBattleRewardDecision(dropCount, lingqi, insight, spiritStones, itemPart);
            if (rewardDecision.ShouldUseFallback)
            {
                var fallbackItems = new Dictionary<string, int>
                {
                    ["spirit_herb"] = 1,
                    ["lingqi_shard"] = 3
                };
                itemPart = BuildDropSummary(fallbackItems);
                ApplyResourceAndItemRewards(0.0, 0.0, fallbackItems, "battle_fallback");
            }

            AppendBattleLog(lingqi, insight, spiritStones, itemPart);
        }

        private void ApplyLevelCompletionRewards()
        {
            if (_levelConfigLoader == null)
            {
                return;
            }

            if (_levelConfigLoader.TryBuildLevelCompletionReward(
                out string levelId,
                out bool firstClear,
                out double lingqi,
                out double insight,
                out int spiritStones,
                out Dictionary<string, int> items))
            {
                ApplyResourceAndItemRewards(lingqi, insight, items, RewardRules.BuildLevelCompletionSourceTag(levelId, firstClear));
                if (spiritStones > 0)
                {
                    _resourceWalletState?.AddSpiritStones(spiritStones);
                }

                if (firstClear && _backpackState != null)
                {
                    EquipmentInstanceData[] generatedRewards = _levelConfigLoader.GetLastGeneratedFirstClearEquipmentRewards();
                    if (generatedRewards.Length > 0)
                    {
                        foreach (EquipmentInstanceData equipmentReward in generatedRewards)
                        {
                            if (!_backpackState.HasEquipment(equipmentReward.EquipmentId))
                            {
                                _backpackState.AddEquipmentInstance(equipmentReward);
                            }
                        }
                    }
                    else if (EquipmentRewardRules.TryBuildFirstClearReward(levelId, out EquipmentStatProfile equipmentReward))
                    {
                        if (!_backpackState.HasEquipment(equipmentReward.EquipmentId))
                        {
                            _backpackState.AddEquipment(equipmentReward);
                        }
                    }
                }
            }
        }

        public Godot.Collections.Dictionary<string, Variant> ToRuntimeDictionary()
        {
            string zoneId = _levelConfigLoader?.ActiveLevelId ?? "";
            SyncLogicFromControllerState();
            ExploreGameLogic.RuntimeState logicState = _logic.CaptureRuntimeState();
            BattleTrackVisualizer.RuntimeState trackState = _battleTrackVisualizer.CaptureRuntimeState();
            string battleState = logicState.InBattle ? "in_battle" : "exploring";
            var markerStates = new Godot.Collections.Array<Variant>();
            var recentBattleLogs = new Godot.Collections.Array<Variant>();
            for (int i = 0; i < trackState.MarkerStates.Count; i++)
            {
                BattleTrackVisualizer.MarkerState marker = trackState.MarkerStates[i];
                var item = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["x"] = marker.X,
                    ["y"] = marker.Y,
                    ["monster_id"] = marker.MonsterId,
                    ["move_pending"] = marker.MovePending,
                    ["move_threshold"] = marker.MoveThreshold
                };
                markerStates.Add(item);
            }

            foreach (BattleLogEntry entry in _recentBattleLogs)
            {
                recentBattleLogs.Add(entry.ToDictionary());
            }

            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["zone_id"] = zoneId,
                ["zone_name"] = logicState.ZoneName,
                ["explore_progress"] = logicState.ExploreProgress,
                ["battle_state"] = battleState,
                ["move_frame_counter"] = logicState.MoveFrameCounter,
                ["queue_move_input_pending"] = trackState.QueueMoveInputPending,
                ["player_hp"] = logicState.PlayerHp,
                ["player_max_hp"] = logicState.PlayerMaxHp,
                ["enemy_hp"] = logicState.EnemyHp,
                ["enemy_max_hp"] = logicState.EnemyMaxHp,
                ["enemy_attack_power"] = logicState.EnemyAttackPower,
                ["inputs_per_battle_round_runtime"] = logicState.InputsPerBattleRoundRuntime,
                ["player_attack_per_round_runtime"] = logicState.PlayerAttackPerRoundRuntime,
                ["enemy_damage_divider_runtime"] = logicState.EnemyDamageDividerRuntime,
                ["enemy_min_damage_runtime"] = logicState.EnemyMinDamageRuntime,
                ["battle_round_counter"] = logicState.BattleRoundCounter,
                ["pending_battle_input_events"] = logicState.PendingBattleInputEvents,
                ["battle_monster_index"] = logicState.BattleMonsterIndex,
                ["battle_monster_id"] = logicState.BattleMonsterId,
                ["battle_monster_name"] = logicState.BattleMonsterName,
                ["total_battle_count"] = logicState.TotalBattleCount,
                ["total_battle_win_count"] = logicState.TotalBattleWinCount,
                ["monster_marker_states"] = markerStates,
                ["recent_battle_logs"] = recentBattleLogs
            };
        }

        public void FromRuntimeDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            ExploreGameLogic.RuntimeState logicState = new()
            {
                ZoneName = data.ContainsKey("zone_name") ? data["zone_name"].AsString() : _currentZone,
                ExploreProgress = data.ContainsKey("explore_progress") ? Mathf.Clamp((float)data["explore_progress"].AsDouble(), 0.0f, MaxProgress) : _exploreProgress,
                MoveFrameCounter = data.ContainsKey("move_frame_counter") ? Mathf.Max(0, data["move_frame_counter"].AsInt32()) : _moveFrameCounter,
                QueueMoveInputPending = data.ContainsKey("queue_move_input_pending") ? Mathf.Max(0, data["queue_move_input_pending"].AsInt32()) : _queueMoveInputPending,
                InBattle = data.ContainsKey("battle_state") && data["battle_state"].AsString() == "in_battle",
                PlayerHp = data.ContainsKey("player_hp") ? Mathf.Max(0, data["player_hp"].AsInt32()) : _playerHp,
                PlayerMaxHp = data.ContainsKey("player_max_hp") ? Mathf.Max(1, data["player_max_hp"].AsInt32()) : _playerMaxHp,
                EnemyHp = data.ContainsKey("enemy_hp") ? Mathf.Max(0, data["enemy_hp"].AsInt32()) : _enemyHp,
                EnemyMaxHp = data.ContainsKey("enemy_max_hp") ? Mathf.Max(1, data["enemy_max_hp"].AsInt32()) : _enemyMaxHp,
                EnemyAttackPower = data.ContainsKey("enemy_attack_power") ? Mathf.Max(1, data["enemy_attack_power"].AsInt32()) : _enemyAttackPower,
                InputsPerBattleRoundRuntime = data.ContainsKey("inputs_per_battle_round_runtime") ? Mathf.Max(1, data["inputs_per_battle_round_runtime"].AsInt32()) : _inputsPerBattleRoundRuntime,
                PlayerAttackPerRoundRuntime = data.ContainsKey("player_attack_per_round_runtime") ? Mathf.Max(1, data["player_attack_per_round_runtime"].AsInt32()) : _playerAttackPerRoundRuntime,
                EnemyDamageDividerRuntime = data.ContainsKey("enemy_damage_divider_runtime") ? Mathf.Max(1, data["enemy_damage_divider_runtime"].AsInt32()) : _enemyDamageDividerRuntime,
                EnemyMinDamageRuntime = data.ContainsKey("enemy_min_damage_runtime") ? Mathf.Max(1, data["enemy_min_damage_runtime"].AsInt32()) : _enemyMinDamageRuntime,
                BattleRoundCounter = data.ContainsKey("battle_round_counter") ? Mathf.Max(0, data["battle_round_counter"].AsInt32()) : _battleRoundCounter,
                PendingBattleInputEvents = data.ContainsKey("pending_battle_input_events") ? Mathf.Max(0, data["pending_battle_input_events"].AsInt32()) : _pendingBattleInputEvents,
                BattleMonsterIndex = data.ContainsKey("battle_monster_index") ? data["battle_monster_index"].AsInt32() : _battleMonsterIndex,
                BattleMonsterId = data.ContainsKey("battle_monster_id") ? data["battle_monster_id"].AsString() : _battleMonsterId,
                BattleMonsterName = data.ContainsKey("battle_monster_name") ? data["battle_monster_name"].AsString() : _battleMonsterName,
                TotalBattleCount = data.ContainsKey("total_battle_count") ? Mathf.Max(0, data["total_battle_count"].AsInt32()) : _totalBattleCount,
                TotalBattleWinCount = data.ContainsKey("total_battle_win_count") ? Mathf.Max(0, data["total_battle_win_count"].AsInt32()) : _totalBattleWinCount,
            };
            _logic.RestoreRuntimeState(logicState);
            SyncControllerStateFromLogic();

            _recentBattleLogs.Clear();
            if (data.ContainsKey("recent_battle_logs") && data["recent_battle_logs"].VariantType == Variant.Type.Array)
            {
                var logs = (Godot.Collections.Array<Variant>)data["recent_battle_logs"];
                foreach (Variant item in logs)
                {
                    if (item.VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    _recentBattleLogs.Add(BattleLogEntry.FromDictionary((Godot.Collections.Dictionary<string, Variant>)item));
                    if (_recentBattleLogs.Count >= 10)
                    {
                        break;
                    }
                }
            }

            var trackState = new BattleTrackVisualizer.RuntimeState
            {
                QueueMoveInputPending = data.ContainsKey("queue_move_input_pending") ? Mathf.Max(0, data["queue_move_input_pending"].AsInt32()) : _queueMoveInputPending,
            };
            if (data.ContainsKey("monster_marker_states") && data["monster_marker_states"].VariantType == Variant.Type.Array)
            {
                var markerStates = (Godot.Collections.Array<Variant>)data["monster_marker_states"];
                for (int i = 0; i < markerStates.Count; i++)
                {
                    if (markerStates[i].VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var item = (Godot.Collections.Dictionary<string, Variant>)markerStates[i];
                    trackState.MarkerStates.Add(new BattleTrackVisualizer.MarkerState
                    {
                        X = item.ContainsKey("x") ? (float)item["x"].AsDouble() : 0.0f,
                        Y = item.ContainsKey("y") ? (float)item["y"].AsDouble() : 0.0f,
                        MonsterId = item.ContainsKey("monster_id") ? item["monster_id"].AsString() : string.Empty,
                        MovePending = item.ContainsKey("move_pending") ? Mathf.Max(0, item["move_pending"].AsInt32()) : 0,
                        MoveThreshold = item.ContainsKey("move_threshold") ? Mathf.Max(1, item["move_threshold"].AsInt32()) : Mathf.Max(1, InputsPerMoveFrame),
                    });
                }
            }
            _battleTrackVisualizer.RestoreRuntimeState(trackState);

            if (_inBattle)
            {
                _battleMonsterIndex = _battleTrackVisualizer.ResolveBattleMonsterIndex(_battleMonsterIndex);
            }

            if (string.IsNullOrEmpty(_battleMonsterId) &&
                _battleMonsterIndex >= 0 &&
                !string.IsNullOrEmpty(_battleTrackVisualizer.GetMonsterId(_battleMonsterIndex)))
            {
                _battleMonsterId = _battleTrackVisualizer.GetMonsterId(_battleMonsterIndex);
            }
            SyncLogicFromControllerState();

            _zoneLabel.Text = _currentZone;
            _progressBar.Value = _exploreProgress;
            _playerHp = Mathf.Clamp(_playerHp, 0, _playerMaxHp);
            _enemyHp = Mathf.Clamp(_enemyHp, 0, _enemyMaxHp);

            if (_inBattle)
            {
                _battleInfoLabel.Text = UiText.BattleInProgress(_battleMonsterName) + BuildActiveFormationSuffix();
                _battleInfoLabel.Visible = true;
                int threshold = Mathf.Max(1, _inputsPerBattleRoundRuntime);
                _roundInfoLabel.Text = $"{UiText.BattleRound(_battleRoundCounter, _battleMonsterName, _enemyHp)} | next {_pendingBattleInputEvents}/{threshold}";
            }
            else
            {
                _battleInfoLabel.Text = "";
                _battleInfoLabel.Visible = false;
                _roundInfoLabel.Text = $"{UiText.ExploreProgress(_exploreProgress)} | {BuildFrontMoveStatus()}{BuildActiveFormationSuffix()}";
            }

            UpdateHpLabels();
            RefreshActorSlots();
            RefreshMoveDebugLabel();
            RefreshDebugPanel();
        }

        private void RefreshCultivationPanel()
        {
            if (_playerProgressState == null || _cultivationProgressBar == null || _breakthroughButton == null)
            {
                return;
            }

            double required = Mathf.Max(1.0f, (float)_playerProgressState.RealmExpRequired);
            double percentRaw = _playerProgressState.RealmExp / required * 100.0;
            double percent = Mathf.Clamp((float)percentRaw, 0.0f, 100.0f);
            _cultivationProgressBar.Value = percent;
            _cultivationProgressBar.TooltipText = $"修炼进度 {_playerProgressState.RealmExp:0.0}/{required:0.0}";
            _breakthroughButton.Disabled = !_playerProgressState.CanBreakthrough;
            _breakthroughButton.Text = _playerProgressState.CanBreakthrough ? "突破!" : "突破";
            _breakthroughButton.TooltipText = _playerProgressState.CanBreakthrough
                ? "可突破，点击提升境界"
                : $"进度未满，还需 {Mathf.Max(0.0f, (float)(required - _playerProgressState.RealmExp)):0.0}";
            if (_cultivationLabel != null)
            {
                _cultivationLabel.Text = $"修炼 {_playerProgressState.RealmExp:0}/{required:0}";
            }
        }

        private void OnBreakthroughPressed()
        {
            if (_playerProgressState == null)
            {
                return;
            }

            _playerProgressState.TryBreakthrough();
            RefreshCultivationPanel();
        }

        private void EnsureDebugPanel()
        {
            _debugPanelLabel = new Label();
            _debugPanelLabel.Name = "DebugPanelLabel";
            _debugPanelLabel.Position = new Vector2(360.0f, 4.0f);
            _debugPanelLabel.Size = new Vector2(620.0f, 130.0f);
            _debugPanelLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _debugPanelLabel.Modulate = new Color(0.95f, 0.95f, 0.75f, 0.95f);
            _debugPanelLabel.Visible = false;
            _debugPanelLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            _battleInfoLabel.GetParent().AddChild(_debugPanelLabel);
        }

        private void RefreshDebugPanel()
        {
            if (_debugPanelLabel == null || !_debugPanelVisible)
            {
                return;
            }

            string actionMode = _actionState?.ActionId ?? (HasDungeonCapability() ? PlayerActionState.ActionDungeon : PlayerActionState.ActionCultivation);
            _debugPanelLabel.Text = ExploreProgressDebugRules.BuildDebugPanelText(
                _currentZone,
                actionMode,
                _actionState?.ActionTargetId ?? "",
                _exploreProgress,
                _battleMonsterName,
                _battleMonsterId,
                _simulationLevelFilterId,
                _simulationMonsterFilterId,
                _levelConfigLoader?.BuildDebugSummary() ?? string.Empty,
                _levelConfigLoader?.BuildValidationSummary(6) ?? string.Empty,
                _levelConfigLoader?.BuildLevelPreviewSummary(8) ?? string.Empty,
                _lastSimulationSummary);
        }

        private bool HasDungeonCapability()
        {
            return PlayerActionCapabilityRules.HasCapability(_actionState, PlayerActionCapability.AdvancesDungeon);
        }

        private bool IsAlchemyMode()
        {
            return _actionState?.ActionId == PlayerActionState.ActionAlchemy;
        }

        private bool IsSmithingMode()
        {
            return _actionState?.ActionId == PlayerActionState.ActionSmithing;
        }

        private bool IsGardenMode()
        {
            return _actionState?.ActionId == PlayerActionState.ActionGarden;
        }

        private bool IsMiningMode()
        {
            return _actionState?.ActionId == PlayerActionState.ActionMining;
        }

        private bool IsFishingMode()
        {
            return _actionState?.ActionId == PlayerActionState.ActionFishing;
        }

        private bool IsGenericRecipeMode()
        {
            string actionId = _actionState?.ActionId ?? string.Empty;
            return actionId == PlayerActionState.ActionTalisman
                || actionId == PlayerActionState.ActionCooking
                || actionId == PlayerActionState.ActionFormation
                || actionId == PlayerActionState.ActionEnlightenment
                || actionId == PlayerActionState.ActionBodyCultivation;
        }

        private IRecipeProgressState? GetActiveGenericRecipeState()
        {
            return (_actionState?.ActionId ?? string.Empty) switch
            {
                PlayerActionState.ActionTalisman => _talismanState,
                PlayerActionState.ActionCooking => _cookingState,
                PlayerActionState.ActionFormation => _formationState,
                PlayerActionState.ActionEnlightenment => _enlightenmentState,
                PlayerActionState.ActionBodyCultivation => _bodyCultivationState,
                _ => null,
            };
        }

        private string GetActiveGenericSystemId()
        {
            return (_actionState?.ActionId ?? string.Empty) switch
            {
                PlayerActionState.ActionTalisman => PlayerActionState.ModeTalisman,
                PlayerActionState.ActionCooking => PlayerActionState.ModeCooking,
                PlayerActionState.ActionFormation => PlayerActionState.ModeFormation,
                PlayerActionState.ActionEnlightenment => PlayerActionState.ModeEnlightenment,
                PlayerActionState.ActionBodyCultivation => PlayerActionState.ModeBodyCultivation,
                _ => string.Empty,
            };
        }

        private void AdvanceAlchemyByInput(int inputEvents)
        {
            if (_alchemyState == null || _backpackState == null || _resourceWalletState == null || inputEvents <= 0)
            {
                return;
            }

            inputEvents = ApplyFormationCraftSpeed(inputEvents);

            if (!_alchemyState.HasSelectedRecipe)
            {
                _roundInfoLabel.Text = "炼丹待命（请先选择丹方）";
                _progressBar.Value = 0.0f;
                return;
            }

            bool completedBatch = CraftingProgressionService.AdvanceAlchemy(_alchemyState, inputEvents, out float percent);
            _progressBar.Value = percent;
            _roundInfoLabel.Text = BuildAlchemyProgressText();

            if (!completedBatch)
            {
                return;
            }

            if (!TryCompleteAlchemyBatch())
            {
                _battleInfoLabel.Text = "材料或灵气不足，炼丹已暂停";
                _battleInfoLabel.Visible = true;
            }
        }

        private bool TryCompleteAlchemyBatch()
        {
            bool completed = CraftingProgressionService.TryCompleteAlchemyBatch(
                _alchemyState,
                _backpackState,
                _resourceWalletState,
                _subsystemMasteryState?.GetLevel(PlayerActionState.ModeAlchemy) ?? 1,
                _potionInventoryState,
                out string rewardText);
            if (completed)
            {
                _battleInfoLabel.Text = rewardText;
                _battleInfoLabel.Visible = true;
            }

            return completed;
        }

        private void AdvanceSmithingByInput(int inputEvents)
        {
            if (_smithingState == null || _equippedItemsState == null || _backpackState == null || _resourceWalletState == null || inputEvents <= 0)
            {
                return;
            }

            inputEvents = ApplyFormationCraftSpeed(inputEvents);

            if (!_smithingState.HasTarget || !_equippedItemsState.TryGetEquippedProfileById(_smithingState.TargetEquipmentId, out EquipmentStatProfile targetProfile))
            {
                _roundInfoLabel.Text = "炼器待命（请先选择装备）";
                _progressBar.Value = 0.0f;
                return;
            }

            bool completed = CraftingProgressionService.AdvanceSmithing(_smithingState, inputEvents, out float percent);
            _progressBar.Value = percent;
            _roundInfoLabel.Text = BuildSmithingProgressText(targetProfile);
            if (!completed)
            {
                return;
            }

            if (!TryCompleteSmithingBatch(targetProfile))
            {
                _battleInfoLabel.Text = "材料不足、灵气不足或强化已达上限";
                _battleInfoLabel.Visible = true;
            }
        }

        private bool TryCompleteSmithingBatch(EquipmentStatProfile targetProfile)
        {
            bool completed = CraftingProgressionService.TryCompleteSmithingBatch(
                _smithingState,
                _equippedItemsState,
                _backpackState,
                _resourceWalletState,
                _subsystemMasteryState?.GetLevel(PlayerActionState.ModeSmithing) ?? 1,
                targetProfile,
                out EquipmentStatProfile enhanced,
                out string rewardText);
            if (completed)
            {
                _battleInfoLabel.Text = rewardText;
                _battleInfoLabel.Visible = true;
                ApplyLevelConfig();
                UpdateHpLabels();
            }

            return completed;
        }

        private void AdvanceGardenByInput(int inputEvents)
        {
            if (_gardenState == null || _backpackState == null || inputEvents <= 0)
            {
                return;
            }

            inputEvents = ApplyFormationGatherSpeed(inputEvents);

            if (!_gardenState.HasSelectedCrop)
            {
                _roundInfoLabel.Text = "灵田待命（请先选择作物）";
                _progressBar.Value = 0.0f;
                return;
            }

            bool completedBatch = _gardenState.AdvanceProgress(inputEvents);
            _progressBar.Value = _gardenState.RequiredProgress > 0.0f
                ? _gardenState.CurrentProgress / _gardenState.RequiredProgress * 100.0f
                : 0.0f;
            _roundInfoLabel.Text = BuildGardenProgressText();
            if (!completedBatch)
            {
                return;
            }

            if (TryCompleteGardenBatch())
            {
                _progressBar.Value = 0.0f;
                _roundInfoLabel.Text = BuildGardenProgressText();
            }
        }

        private bool TryCompleteGardenBatch()
        {
            if (_gardenState == null || _backpackState == null)
            {
                return false;
            }

            GardenRules.GatherResult result = GardenRules.HarvestCrop(
                _gardenState.SelectedRecipeId,
                _subsystemMasteryState?.GetLevel(PlayerActionState.ModeGarden) ?? 1);
            if (string.IsNullOrEmpty(result.ItemId) || result.ItemCount <= 0)
            {
                return false;
            }

            _backpackState.AddItem(result.ItemId, result.ItemCount);
            _battleInfoLabel.Text = $"灵田收获：{result.ItemId} x{result.ItemCount}";
            _battleInfoLabel.Visible = true;
            return true;
        }

        private void AdvanceMiningByInput(int inputEvents)
        {
            if (_miningState == null || _backpackState == null || inputEvents <= 0)
            {
                return;
            }

            inputEvents = ApplyFormationGatherSpeed(inputEvents);

            if (!_miningState.HasSelectedNode)
            {
                _roundInfoLabel.Text = "矿脉待命（请先选择矿点）";
                _progressBar.Value = 0.0f;
                return;
            }

            bool completedBatch = _miningState.AdvanceProgress(inputEvents);
            _progressBar.Value = _miningState.RequiredProgress > 0.0f
                ? _miningState.CurrentProgress / _miningState.RequiredProgress * 100.0f
                : 0.0f;
            _roundInfoLabel.Text = BuildMiningProgressText();
            if (!completedBatch)
            {
                return;
            }

            if (TryCompleteMiningBatch())
            {
                _progressBar.Value = 0.0f;
                _roundInfoLabel.Text = BuildMiningProgressText();
            }
        }

        private bool TryCompleteMiningBatch()
        {
            if (_miningState == null || _backpackState == null)
            {
                return false;
            }

            MiningRules.GatherResult result = MiningRules.GatherOre(
                _miningState.SelectedRecipeId,
                _subsystemMasteryState?.GetLevel(PlayerActionState.ModeMining) ?? 1);
            if (string.IsNullOrEmpty(result.ItemId) || result.ItemCount <= 0)
            {
                return false;
            }

            _backpackState.AddItem(result.ItemId, result.ItemCount);
            _battleInfoLabel.Text = $"矿脉收获：{result.ItemId} x{result.ItemCount}";
            _battleInfoLabel.Visible = true;
            return true;
        }

        private void AdvanceFishingByInput(int inputEvents)
        {
            if (_fishingState == null || _backpackState == null || inputEvents <= 0)
            {
                return;
            }

            inputEvents = ApplyFormationGatherSpeed(inputEvents);

            if (!_fishingState.HasSelectedPond)
            {
                _roundInfoLabel.Text = "灵渔待命（请先选择鱼塘）";
                _progressBar.Value = 0.0f;
                return;
            }

            bool completedBatch = _fishingState.AdvanceProgress(inputEvents);
            _progressBar.Value = _fishingState.RequiredProgress > 0.0f
                ? _fishingState.CurrentProgress / _fishingState.RequiredProgress * 100.0f
                : 0.0f;
            _roundInfoLabel.Text = BuildFishingProgressText();
            if (!completedBatch)
            {
                return;
            }

            if (TryCompleteFishingBatch())
            {
                _progressBar.Value = 0.0f;
                _roundInfoLabel.Text = BuildFishingProgressText();
            }
        }

        private bool TryCompleteFishingBatch()
        {
            if (_fishingState == null || _backpackState == null)
            {
                return false;
            }

            FishingRules.CatchResult result = FishingRules.CatchFish(
                _fishingState.SelectedRecipeId,
                _subsystemMasteryState?.GetLevel(PlayerActionState.ModeFishing) ?? 1);
            if (string.IsNullOrEmpty(result.ItemId) || result.ItemCount <= 0)
            {
                return false;
            }

            _backpackState.AddItem(result.ItemId, result.ItemCount);
            _battleInfoLabel.Text = $"灵渔收获：{result.ItemId} x{result.ItemCount}";
            _battleInfoLabel.Visible = true;
            return true;
        }

        private void AdvanceGenericRecipeByInput(int inputEvents)
        {
            IRecipeProgressState? state = GetActiveGenericRecipeState();
            if (state == null || _backpackState == null || _resourceWalletState == null || inputEvents <= 0)
            {
                return;
            }

            inputEvents = ApplyFormationCraftSpeed(inputEvents);

            EnsureGenericRecipeSelected(state, GetActiveGenericSystemId());
            if (!state.HasSelectedRecipe)
            {
                _roundInfoLabel.Text = $"{GetActionModeDisplayName()}待命（当前无可用项目）";
                _progressBar.Value = 0.0f;
                return;
            }

            bool completedBatch = state.AdvanceProgress(inputEvents);
            _progressBar.Value = state.RequiredProgress > 0.0f ? state.CurrentProgress / state.RequiredProgress * 100.0f : 0.0f;
            _roundInfoLabel.Text = BuildGenericRecipeProgressText();
            if (!completedBatch)
            {
                return;
            }

            if (TryCompleteGenericRecipeBatch(state.SelectedRecipeId))
            {
                _progressBar.Value = 0.0f;
                _roundInfoLabel.Text = BuildGenericRecipeProgressText();
            }
        }

        private void EnsureGenericRecipeSelected(IRecipeProgressState state, string systemId)
        {
            if (state.HasSelectedRecipe)
            {
                return;
            }

            IActivityDefinition? activity = ActivityRegistry.GetBySystem(systemId);
            if (activity == null || activity.GetRecipes().Count == 0)
            {
                return;
            }

            state.SelectRecipe(activity.GetRecipes()[0].RecipeId);
        }

        private bool TryCompleteGenericRecipeBatch(string recipeId)
        {
            if (_backpackState == null || _resourceWalletState == null)
            {
                return false;
            }

            if (!Xiuxian.Scripts.Game.CraftingProgressionService.TryCompleteGenericBatch(recipeId, _backpackState, _resourceWalletState, out string rewardText))
            {
                _battleInfoLabel.Text = "材料不足或灵气不足，当前流程已暂停";
                _battleInfoLabel.Visible = true;
                return false;
            }

            if (_formationState != null && recipeId.StartsWith("formation_", StringComparison.Ordinal))
            {
                _formationState.AddCraftedFormation(recipeId, 1);
                if (string.IsNullOrEmpty(_formationState.ActivePrimaryId))
                {
                    _formationState.TryActivatePrimary(recipeId);
                }
            }

            if (_playerProgressState != null)
            {
                if (recipeId == "enlightenment_meditation" || recipeId == "enlightenment_contemplation")
                {
                    _playerProgressState.ApplyEnlightenmentReward(recipeId);
                    rewardText = recipeId == "enlightenment_meditation"
                        ? "悟道完成，悟性收益提升 +5%"
                        : "悟道完成，灵气收益提升 +8%";
                }
                else if (recipeId == "body_cultivation_temper" || recipeId == "body_cultivation_boneforge")
                {
                    _playerProgressState.ApplyBodyCultivationReward(recipeId);
                    rewardText = recipeId == "body_cultivation_temper"
                        ? "淬体完成，永久气血 +10、防御 +1"
                        : "炼骨完成，永久攻击 +4、防御 +2";
                }
            }

            _battleInfoLabel.Text = rewardText;
            _battleInfoLabel.Visible = true;
            return true;
        }

        private int ApplyFormationGatherSpeed(int inputEvents)
        {
            double rate = ActivityEffectRules.CollectFormationGatherSpeedRate(
                _formationState?.ActivePrimaryId ?? string.Empty,
                _formationState?.ActiveSecondaryId ?? string.Empty,
                _backpackState?.GetItemEntries() ?? new Dictionary<string, int>(),
                _subsystemMasteryState?.GetLevel(PlayerActionState.ModeFormation) ?? 1);
            return ApplyProgressRate(inputEvents, rate);
        }

        private int ApplyFormationCraftSpeed(int inputEvents)
        {
            double rate = ActivityEffectRules.CollectFormationCraftSpeedRate(
                _formationState?.ActivePrimaryId ?? string.Empty,
                _formationState?.ActiveSecondaryId ?? string.Empty,
                _backpackState?.GetItemEntries() ?? new Dictionary<string, int>(),
                _subsystemMasteryState?.GetLevel(PlayerActionState.ModeFormation) ?? 1);
            return ApplyProgressRate(inputEvents, rate);
        }

        private string GetCurrentFormationId()
        {
            return _formationState?.ActivePrimaryId ?? string.Empty;
        }

        private string BuildActiveFormationSuffix()
        {
            string primaryId = _formationState?.ActivePrimaryId ?? string.Empty;
            string secondaryId = _formationState?.ActiveSecondaryId ?? string.Empty;
            if (string.IsNullOrEmpty(primaryId))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(secondaryId))
            {
                return $" | 阵法:{UiText.BackpackItemName(primaryId)}";
            }

            return $" | 阵法:{UiText.BackpackItemName(primaryId)}+{UiText.BackpackItemName(secondaryId)}";
        }

        private static int ApplyProgressRate(int inputEvents, double rate)
        {
            return Mathf.Max(1, Mathf.RoundToInt((float)(inputEvents * (1.0 + System.Math.Max(0.0, rate)))));
        }

        private int GetActionModeSelectedIndex()
        {
            string currentActionId = _actionState?.ActionId ?? PlayerActionState.ActionDungeon;
            int index = Array.IndexOf(ActionModeIds, currentActionId);
            return index >= 0 ? index : 0;
        }

        private string GetActionModeOptionText(int selected)
        {
            return ExploreProgressPresentationRules.GetActionModeOptionText(selected);
        }

        private string GetActionModeDisplayName()
        {
            return ExploreProgressPresentationRules.GetActionModeDisplayName(_actionState?.ActionId ?? PlayerActionState.ActionDungeon);
        }

        private string GetPausedModeLabel()
        {
            return ExploreProgressPresentationRules.GetPausedModeLabel(_actionState?.ActionId ?? PlayerActionState.ActionDungeon);
        }

        private string GetPausedModeRoundLabel()
        {
            string actionId = _actionState?.ActionId ?? PlayerActionState.ActionDungeon;
            if (actionId == PlayerActionState.ActionTalisman
                || actionId == PlayerActionState.ActionCooking
                || actionId == PlayerActionState.ActionFormation
                || actionId == PlayerActionState.ActionEnlightenment
                || actionId == PlayerActionState.ActionBodyCultivation)
            {
                return BuildGenericRecipeProgressText();
            }

            return ExploreProgressPresentationRules.GetPausedModeRoundLabel(
                actionId,
                BuildAlchemyProgressText(),
                BuildSmithingProgressText(),
                BuildGardenProgressText(),
                BuildMiningProgressText(),
                BuildFishingProgressText());
        }

        private string BuildAlchemyProgressText()
        {
            if (_alchemyState == null || !_alchemyState.HasSelectedRecipe || !AlchemyRules.TryGetRecipe(_alchemyState.SelectedRecipeId, out AlchemyRules.RecipeSpec recipe))
            {
                return ExploreProgressPresentationRules.BuildAlchemyProgressText(string.Empty, 0.0f, 0.0f);
            }

            return ExploreProgressPresentationRules.BuildAlchemyProgressText(recipe.DisplayName, _alchemyState.CurrentProgress, _alchemyState.RequiredProgress);
        }

        private string BuildSmithingProgressText(EquipmentStatProfile? targetProfile = null)
        {
            if (_smithingState == null || !_smithingState.HasTarget)
            {
                return ExploreProgressPresentationRules.BuildSmithingProgressText(string.Empty, 0, 0.0f, 0.0f);
            }

            EquipmentStatProfile resolved = targetProfile ?? (_equippedItemsState != null && _equippedItemsState.TryGetEquippedProfileById(_smithingState.TargetEquipmentId, out EquipmentStatProfile profile) ? profile : default);
            if (string.IsNullOrEmpty(resolved.EquipmentId))
            {
                return ExploreProgressPresentationRules.BuildSmithingProgressText(string.Empty, 0, 0.0f, 0.0f);
            }

            return ExploreProgressPresentationRules.BuildSmithingProgressText(resolved.DisplayName, resolved.EnhanceLevel, _smithingState.CurrentProgress, _smithingState.RequiredProgress);
        }

        private string BuildGardenProgressText()
        {
            if (_gardenState == null || !_gardenState.HasSelectedCrop || !GardenRules.TryGetCrop(_gardenState.SelectedRecipeId, out GardenRules.CropSpec crop))
            {
                return ExploreProgressPresentationRules.BuildGardenProgressText(string.Empty, 0.0f, 0.0f);
            }

            return ExploreProgressPresentationRules.BuildGardenProgressText(crop.DisplayName.Replace("种植", string.Empty), _gardenState.CurrentProgress, _gardenState.RequiredProgress);
        }

        private string BuildMiningProgressText()
        {
            if (_miningState == null || !_miningState.HasSelectedNode || !MiningRules.TryGetNode(_miningState.SelectedRecipeId, out MiningRules.NodeSpec node))
            {
                return ExploreProgressPresentationRules.BuildMiningProgressText(string.Empty, 0.0f, 0.0f, MiningRules.DefaultNodeDurability);
            }

            return ExploreProgressPresentationRules.BuildMiningProgressText(node.DisplayName.Replace("开采", string.Empty), _miningState.CurrentProgress, _miningState.RequiredProgress, _miningState.CurrentDurability);
        }

        private string BuildFishingProgressText()
        {
            if (_fishingState == null || !_fishingState.HasSelectedPond || !FishingRules.TryGetPond(_fishingState.SelectedRecipeId, out FishingRules.PondSpec pond))
            {
                return ExploreProgressPresentationRules.BuildFishingProgressText(string.Empty, 0.0f, 0.0f);
            }

            return ExploreProgressPresentationRules.BuildFishingProgressText(pond.DisplayName, _fishingState.CurrentProgress, _fishingState.RequiredProgress);
        }

        private string BuildGenericRecipeProgressText()
        {
            IRecipeProgressState? state = GetActiveGenericRecipeState();
            if (state == null || !state.HasSelectedRecipe)
            {
                return $"{GetActionModeDisplayName()}待命（当前无可用项目）";
            }

            IRecipeDefinition? recipe = ActivityRegistry.GetRecipe(state.SelectedRecipeId);
            if (recipe == null)
            {
                return $"{GetActionModeDisplayName()}待命（当前无可用项目）";
            }

            float percent = state.RequiredProgress > 0.0f ? state.CurrentProgress / state.RequiredProgress * 100.0f : 0.0f;
            return $"{recipe.DisplayName} {percent:0}%";
        }

        private CharacterStatModifier[] BuildNonEquipmentModifiers()
        {
            var result = new List<CharacterStatModifier>();
            Dictionary<string, int> items = _backpackState?.GetItemEntries() ?? new Dictionary<string, int>();
            CharacterStatModifier formationModifier = ActivityEffectRules.CollectFormationModifier(
                _formationState?.ActivePrimaryId ?? string.Empty,
                _formationState?.ActiveSecondaryId ?? string.Empty,
                items,
                _subsystemMasteryState?.GetLevel(PlayerActionState.ModeFormation) ?? 1);
            if (!formationModifier.Equals(default(CharacterStatModifier)))
            {
                result.Add(formationModifier);
            }

            if (_playerProgressState != null)
            {
                CharacterStatModifier permanentModifier = ActivityEffectRules.CollectPermanentProgressModifier(
                    new PlayerProgressPersistenceRules.PlayerProgressSnapshot(
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
                        _playerProgressState.BoneforgeCount));
                if (!permanentModifier.Equals(default(CharacterStatModifier)))
                {
                    result.Add(permanentModifier);
                }
            }

            if (!_battleConsumableModifier.Equals(default(CharacterStatModifier)))
            {
                result.Add(_battleConsumableModifier);
            }

            return result.ToArray();
        }

        private void SyncActionTargetToActiveLevel()
        {
            if (_actionState == null || _levelConfigLoader == null || !_actionState.IsDungeonAction)
            {
                return;
            }

            string activeLevelId = _levelConfigLoader.ActiveLevelId;
            if (!string.IsNullOrEmpty(activeLevelId) && _actionState.ActionTargetId != activeLevelId)
            {
                _actionState.SetAction(_actionState.ActionId, activeLevelId, _actionState.ActionVariant);
            }
        }

        public void ShowOfflineSummary(string title, string body)
        {
            _offlineSummaryVisible = true;
            _battleInfoLabel.Text = title;
            _battleInfoLabel.Visible = true;
            _roundInfoLabel.Text = body;
        }

        private void RefreshValidationPanel()
        {
            if (_validationPanel == null || _validationTitleLabel == null || _validationBodyLabel == null)
            {
                return;
            }

            _validationPanel.Visible = _validationPanelEnabled;
            if (!_validationPanelEnabled)
            {
                return;
            }

            if (_levelConfigLoader == null)
            {
                _validationPanel.SelfModulate = new Color(0.82f, 0.82f, 0.82f, 0.95f);
                _validationTitleLabel.Text = "配置校验：不可用";
                _validationBodyLabel.Text = "LevelConfigLoader 未加载。\n[F11] scope  [F12] 当前关卡";
                return;
            }

            var entries = _levelConfigLoader.GetValidationEntries();
            var filtered = FilterValidationEntries(entries);
            int issueCount = filtered.Count;
            int totalCount = entries.Count;
            if (issueCount <= 0)
            {
                _validationPanel.SelfModulate = new Color(0.70f, 0.90f, 0.74f, 0.95f);
                _validationTitleLabel.Text = $"配置校验：通过 ({BuildValidationFilterSummary()})";
                _validationBodyLabel.Text = "当前过滤条件下未发现配置错误。\n[F11] scope  [F12] 当前关卡";
                return;
            }

            _validationPanel.SelfModulate = new Color(0.98f, 0.72f, 0.72f, 0.96f);
            _validationTitleLabel.Text = $"配置校验：{issueCount}/{totalCount} 项 ({BuildValidationFilterSummary()})";

            int maxLines = 2;
            var sb = new StringBuilder();
            int shown = Mathf.Min(maxLines, issueCount);
            for (int i = 0; i < shown; i++)
            {
                var entry = filtered[i];
                string scope = entry.ContainsKey("scope") ? entry["scope"].AsString() : "config";
                string id = entry.ContainsKey("id") ? entry["id"].AsString() : "(unknown)";
                string field = entry.ContainsKey("field") ? entry["field"].AsString() : "(unknown)";
                string message = entry.ContainsKey("message") ? entry["message"].AsString() : "validation failed";
                string levelId = entry.ContainsKey("level_id") ? entry["level_id"].AsString() : "";
                string monsterId = entry.ContainsKey("monster_id") ? entry["monster_id"].AsString() : "";
                string dropTableId = entry.ContainsKey("drop_table_id") ? entry["drop_table_id"].AsString() : "";

                if (i > 0)
                {
                    sb.Append('\n');
                }

                sb.Append($"• {scope}/{id} {field} {message}");

                if (!string.IsNullOrEmpty(levelId) || !string.IsNullOrEmpty(monsterId) || !string.IsNullOrEmpty(dropTableId))
                {
                    sb.Append(" (");
                    bool first = true;
                    if (!string.IsNullOrEmpty(levelId))
                    {
                        sb.Append($"level_id={levelId}");
                        first = false;
                    }
                    if (!string.IsNullOrEmpty(monsterId))
                    {
                        if (!first)
                        {
                            sb.Append(", ");
                        }
                        sb.Append($"monster_id={monsterId}");
                        first = false;
                    }
                    if (!string.IsNullOrEmpty(dropTableId))
                    {
                        if (!first)
                        {
                            sb.Append(", ");
                        }
                        sb.Append($"drop_table_id={dropTableId}");
                    }
                    sb.Append(')');
                }
            }

            if (issueCount > shown)
            {
                sb.Append($"\n… 还有 {issueCount - shown} 项");
            }

            sb.Append("\n[F11] scope  [F12] 当前关卡");
            _validationBodyLabel.Text = sb.ToString();
        }

        public void SetValidationPanelEnabled(bool enabled)
        {
            _validationPanelEnabled = enabled;
            RefreshValidationPanel();
        }

        public void SetGlobalDebugOverlayEnabled(bool enabled)
        {
            _globalDebugOverlayEnabled = enabled;
            ApplyGlobalDebugOverlayVisibility();
            RefreshMoveDebugLabel();
        }

        private void CycleValidationScopeFilter()
        {
            _validationScopeFilterIndex = (_validationScopeFilterIndex + 1) % ValidationScopeFilters.Length;
        }

        private string BuildValidationFilterSummary()
        {
            string scope = ValidationScopeFilters[Mathf.Clamp(_validationScopeFilterIndex, 0, ValidationScopeFilters.Length - 1)];
            return ExploreProgressDebugRules.BuildValidationFilterSummary(scope, _validationOnlyActiveLevel);
        }

        private Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> FilterValidationEntries(
            Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> entries)
        {
            string scopeFilter = ValidationScopeFilters[Mathf.Clamp(_validationScopeFilterIndex, 0, ValidationScopeFilters.Length - 1)];
            string activeLevelId = _levelConfigLoader?.ActiveLevelId ?? "";
            var filtered = ExploreProgressDebugRules.FilterValidationEntries(entries, scopeFilter, _validationOnlyActiveLevel, activeLevelId);
            var result = new Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>>();
            foreach (Godot.Collections.Dictionary<string, Variant> entry in filtered)
            {
                result.Add(entry);
            }

            return result;
        }

        private static string BuildDropSummary(Dictionary<string, int> drops)
        {
            if (drops.Count == 0)
            {
                return "none";
            }

            var sb = new StringBuilder();
            bool first = true;
            foreach (var kv in drops)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;
                sb.Append($"{kv.Key} x{kv.Value}");
            }
            return sb.ToString();
        }

        public string BuildRecentBattleLogText()
        {
            if (_recentBattleLogs.Count == 0)
            {
                return UiText.BattleLogEmpty;
            }

            var entries = new List<(string TimeLabel, string ZoneName, string MonsterName, string MonsterType, int RoundCount, string BattleResult, string RewardSummary)>();
            foreach (BattleLogEntry entry in _recentBattleLogs)
            {
                string timeLabel = entry.TimestampUnix > 0
                    ? Time.GetDatetimeStringFromUnixTime(entry.TimestampUnix, true).Substring(11, 5)
                    : "--:--";
                entries.Add((timeLabel, entry.ZoneName, entry.MonsterName, entry.MonsterType, entry.RoundCount, entry.BattleResult, entry.RewardSummary));
            }

            return ExploreProgressPresentationRules.BuildRecentBattleLogText(entries);
        }

        public int TotalBattleCount => _totalBattleCount;
        public int TotalBattleWinCount => _totalBattleWinCount;

        private void AppendBattleLog(double lingqi, double insight, int spiritStones, string itemPart, string result = "胜利")
        {
            string potionSummary = _consumedPotionsThisBattle.Count > 0
                ? $" | 丹药:{string.Join(',', _consumedPotionsThisBattle)}"
                : string.Empty;
            string monsterType = "normal";
            if (IsBossChallengeBattle())
            {
                monsterType = "boss";
            }
            else if (_levelConfigLoader != null && !string.IsNullOrEmpty(_battleMonsterId)
                && _levelConfigLoader.TryGetMonsterStatProfile(_battleMonsterId, out MonsterStatProfile mProfile)
                && mProfile.MoveCategory == "elite")
            {
                monsterType = "elite";
            }

            _recentBattleLogs.Insert(0, new BattleLogEntry
            {
                TimestampUnix = (long)Time.GetUnixTimeFromSystem(),
                ZoneName = _currentZone,
                MonsterName = string.IsNullOrEmpty(_battleMonsterName) ? UiText.DefaultMonsterName : _battleMonsterName,
                MonsterType = monsterType,
                RoundCount = _battleRoundCounter,
                BattleResult = result,
                RewardSummary = RewardRules.BuildBattleRewardSummary(lingqi, insight, spiritStones, itemPart) + potionSummary,
            });

            if (_recentBattleLogs.Count > 10)
            {
                _recentBattleLogs.RemoveAt(_recentBattleLogs.Count - 1);
            }
        }

        private void ApplyAutoConsumables(bool isBattleStart)
        {
            if (!_inBattle)
            {
                return;
            }

            BattleConsumableState state = new(
                PlayerCurrentHp: _playerHp,
                PlayerMaxHp: _playerMaxHp,
                IsBattleStart: isBattleStart,
                HasLingqiDropBuff: _hasLingqiDropBuffThisBattle,
                UsedHealPotionThisBattle: _usedHealPotionThisBattle);

            List<PotionUsage> usages = _potionInventoryState != null
                ? ConsumableUsageRules.DetermineAutoConsume(state, _potionInventoryState.GetPotionEntries())
                : new List<PotionUsage>();

            for (int i = 0; i < usages.Count; i++)
            {
                PotionUsage usage = usages[i];
                if (_potionInventoryState == null || !_potionInventoryState.ConsumePotion(usage.PotionId))
                {
                    continue;
                }

                _consumedPotionsThisBattle.Add(UiText.BackpackItemName(usage.PotionId));
                if (usage.PotionId == "potion_huiqi_dan")
                {
                    _usedHealPotionThisBattle = true;
                    int heal = Mathf.Max(1, Mathf.RoundToInt((float)(_playerMaxHp * usage.EffectValue)));
                    _playerHp = Mathf.Clamp(_playerHp + heal, 0, _playerMaxHp);
                }
                else if (usage.PotionId == "potion_juling_san")
                {
                    _hasLingqiDropBuffThisBattle = true;
                }
            }

            if (_backpackState == null)
            {
                return;
            }

            List<BackpackConsumableUsage> backpackUsages = ActivityEffectRules.DetermineAutoUseBackpackConsumables(state, _backpackState.GetItemEntries());
            for (int i = 0; i < backpackUsages.Count; i++)
            {
                BackpackConsumableUsage usage = backpackUsages[i];
                if (!_backpackState.RemoveItem(usage.ItemId, 1))
                {
                    continue;
                }

                CharacterStatModifier modifier = ActivityEffectRules.GetBackpackConsumableModifier(usage.ItemId);
                _battleConsumableModifier = new CharacterStatModifier(
                    MaxHpFlat: _battleConsumableModifier.MaxHpFlat + modifier.MaxHpFlat,
                    AttackFlat: _battleConsumableModifier.AttackFlat + modifier.AttackFlat,
                    DefenseFlat: _battleConsumableModifier.DefenseFlat + modifier.DefenseFlat,
                    SpeedFlat: _battleConsumableModifier.SpeedFlat + modifier.SpeedFlat,
                    MaxHpRate: _battleConsumableModifier.MaxHpRate + modifier.MaxHpRate,
                    AttackRate: _battleConsumableModifier.AttackRate + modifier.AttackRate,
                    DefenseRate: _battleConsumableModifier.DefenseRate + modifier.DefenseRate,
                    SpeedRate: _battleConsumableModifier.SpeedRate + modifier.SpeedRate,
                    CritChanceDelta: _battleConsumableModifier.CritChanceDelta + modifier.CritChanceDelta,
                    CritDamageDelta: _battleConsumableModifier.CritDamageDelta + modifier.CritDamageDelta);
                _consumedPotionsThisBattle.Add(UiText.BackpackItemName(usage.ItemId));
            }
        }

        private void ApplyResourceAndItemRewards(double lingqi, double insight, Dictionary<string, int> items, string source)
        {
            if (_playerProgressState != null)
            {
                lingqi *= 1.0 + ActivityEffectRules.CollectFormationLingqiRate(
                    _formationState?.ActivePrimaryId ?? string.Empty,
                    _formationState?.ActiveSecondaryId ?? string.Empty,
                    _backpackState?.GetItemEntries() ?? new Dictionary<string, int>(),
                    _subsystemMasteryState?.GetLevel(PlayerActionState.ModeFormation) ?? 1);
                lingqi *= 1.0 + _playerProgressState.EnlightenmentLingqiBonusRate;
                insight *= 1.0 + _playerProgressState.EnlightenmentInsightBonusRate;
            }

            if (lingqi > 0.0)
            {
                _resourceWalletState?.AddLingqi(lingqi);
            }
            if (insight > 0.0)
            {
                _resourceWalletState?.AddInsight(insight);
            }

            foreach (var kv in items)
            {
                _backpackState?.AddItem(kv.Key, kv.Value);
            }

            string itemPart = items.Count > 0 ? BuildDropSummary(items) : "none";
            _lastDropSummary = $"{source} | lq={lingqi:0} in={insight:0} ss={_resourceWalletState?.SpiritStones ?? 0} | items={itemPart}";
            RefreshDebugPanel();
        }
    }
}
