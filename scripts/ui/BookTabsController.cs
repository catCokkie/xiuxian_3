using Godot;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

public partial class BookTabsController : Control
{
    [Signal]
    public delegate void ActiveTabsChangedEventHandler(string leftTabName, string rightTabName);

    private readonly Dictionary<string, string> _leftTabContentMap = new()
    {
        { "CultivationTab", UiText.CultivationTemplate },
        { "BattleLogTab", UiText.BattleLogEmpty },
        { "EquipmentTab", UiText.EquipmentTemplate },
        { "BackpackTab", UiText.BackpackTemplate },
        { "StatsTab", UiText.StatsTemplate },
        { "ValidationTab", UiText.LeftTabValidation },
    };

    private readonly Dictionary<string, string> _rightTabContentMap = new()
    {
        { "BugTab", UiText.BugTemplate },
        { "SettingsTab", UiText.SettingsTitle },
    };

    private RichTextLabel _leftContentLabel = null!;
    private Label _leftTitleLabel = null!;
    private Label _coinLabel = null!;
    private Control _leftPage = null!;
    private Control _rightPage = null!;
    private Button _closeButton = null!;

    private HBoxContainer _settingsNavRoot = null!;
    private VBoxContainer _settingsSystemRoot = null!;
    private VBoxContainer _settingsDisplayRoot = null!;
    private VBoxContainer _settingsProgressRoot = null!;
    private VBoxContainer _settingsActionRoot = null!;
    private VBoxContainer _cultivationRoot = null!;
    private VBoxContainer _backpackRoot = null!;
    private VBoxContainer _bugFeedbackRoot = null!;
    private VBoxContainer _equipmentRoot = null!;
    private VBoxContainer _validationRoot = null!;
    private Control _validationFilterPanel = null!;
    private Control _validationResultPanel = null!;
    private Control _simulationResultPanel = null!;
    private Button _settingsSystemBtn = null!;
    private Button _settingsDisplayBtn = null!;
    private Button _settingsProgressBtn = null!;

    private OptionButton _languageOption = null!;
    private CheckButton _keepOnTopCheck = null!;
    private CheckButton _taskbarIconCheck = null!;
    private CheckButton _vsyncCheck = null!;
    private OptionButton _fpsOption = null!;

    private OptionButton _resolutionOption = null!;
    private CheckButton _showControlMarkerCheck = null!;
    private CheckButton _showValidationPanelCheck = null!;
    private Button _openLogFolderButton = null!;
    private OptionButton _gameScaleOption = null!;
    private OptionButton _uiScaleOption = null!;
    private OptionButton _autoSaveIntervalOption = null!;
    private CheckButton _cloudSyncCheck = null!;
    private CheckButton _milestoneTipsCheck = null!;
    private CheckButton _globalDebugOverlayCheck = null!;
    private Label _cultivationStatusLabel = null!;
    private Button _cultivationBreakthroughButton = null!;
    private Button _cultivationBossInsightButton = null!;
    private RichTextLabel _cultivationMasteryLabel = null!;
    private readonly Dictionary<string, Button> _cultivationMasteryButtons = new();
    private OptionButton _cultivationAlchemyRecipeOption = null!;
    private Button _cultivationAlchemyStartButton = null!;
    private OptionButton _cultivationGardenOption = null!;
    private Button _cultivationGardenStartButton = null!;
    private OptionButton _cultivationMiningOption = null!;
    private Button _cultivationMiningStartButton = null!;
    private OptionButton _cultivationFishingOption = null!;
    private Button _cultivationFishingStartButton = null!;
    private OptionButton _cultivationFormationOption = null!;
    private Button _cultivationFormationActivateButton = null!;
    private OptionButton _cultivationFormationSecondaryOption = null!;
    private Button _cultivationFormationSecondaryActivateButton = null!;
    private RichTextLabel _cultivationContentLabel = null!;
    private BackpackGridController _backpackGrid = null!;
    private TextEdit _bugFeedbackInput = null!;
    private Label _bugFeedbackStatusLabel = null!;
    private RichTextLabel _equipmentContentLabel = null!;
    private OptionButton _equipmentSmithingTargetOption = null!;
    private Button _equipmentSmithingStartButton = null!;
    private Label _validationStatusLabel = null!;
    private RichTextLabel _validationContentLabel = null!;
    private Label _validationFilterInfoLabel = null!;
    private Button _validationScopeButton = null!;
    private Button _validationLevelScopeButton = null!;
    private Button _simulationLevelButton = null!;
    private Button _simulationMonsterButton = null!;
    private Label _simulationStatusLabel = null!;
    private RichTextLabel _simulationContentLabel = null!;

    private Tween? _leftTween;
    private InputActivityState? _activityState;
    private BackpackState? _backpackState;
    private AlchemyState? _alchemyState;
    private PotionInventoryState? _potionInventoryState;
    private SmithingState? _smithingState;
    private GardenState? _gardenState;
    private MiningState? _miningState;
    private FishingState? _fishingState;
    private FormationState? _formationState;
    private ResourceWalletState? _resourceWalletState;
    private PlayerProgressState? _playerProgressState;
    private PlayerActionState? _playerActionState;
    private EquippedItemsState? _equippedItemsState;
    private SubsystemMasteryState? _subsystemMasteryState;
    private LevelConfigLoader? _levelConfigLoader;
    private ExploreProgressController? _exploreProgressController;

    public string ActiveLeftTabName { get; private set; } = "CultivationTab";
    public string ActiveRightTabName { get; private set; } = "BugTab";
    private bool _isShowingRightTab;

    private string _activeSettingsSection = "system";
    private bool _isApplyingSettingsUi;
    private int _validationScopeFilterIndex;
    private bool _validationOnlyActiveLevel;
    private string _simulationLevelFilterId = "";
    private string _simulationMonsterFilterId = "";
    private string _lastSimulationSummary = "";

    private static readonly string[] ValidationScopeFilters = { "all", "config", "level", "monster", "drop_table" };

    private readonly Godot.Collections.Dictionary<string, Variant> _settings = new()
    {
        ["language"] = "zh-CN",
        ["keep_on_top"] = false,
        ["taskbar_icon"] = true,
        ["startup_animation"] = true,
        ["admin_mode"] = false,
        ["handwriting_support"] = false,
        ["vsync"] = true,
        ["max_fps"] = 60,
        ["resolution"] = "1600x900",
        ["show_control_markers"] = true,
        ["show_validation_panel"] = false,
        ["game_scale"] = 1.33,
        ["ui_scale"] = 1.0,
        ["auto_save_interval_sec"] = 10,
        ["cloud_sync"] = false,
        ["milestone_tips"] = true,
        ["global_debug_overlay"] = false,
    };

    public override void _Ready()
    {
        ServiceLocator.Instance?.Refresh();

        _leftContentLabel = GetNode<RichTextLabel>("SpreadBody/LeftPage/LeftContentLabel");
        _leftTitleLabel = GetNode<Label>("SpreadBody/LeftPage/LeftTitle");
        _coinLabel = GetNode<Label>("BottomBar/CoinLabel");
        _leftPage = GetNode<Control>("SpreadBody/LeftPage");
        _rightPage = GetNode<Control>("SpreadBody/RightPage");
        _closeButton = GetNode<Button>("CloseButton");
        _closeButton.Pressed += CloseWindow;
        ServiceLocator? services = ServiceLocator.Instance;
        _activityState = services?.InputActivityState;
        _backpackState = services?.BackpackState;
        _alchemyState = services?.AlchemyState;
        _potionInventoryState = services?.PotionInventoryState;
        _smithingState = services?.SmithingState;
        _gardenState = services?.GardenState;
        _formationState = services?.FormationState;
        _miningState = services?.MiningState;
        _fishingState = services?.FishingState;
        _resourceWalletState = services?.ResourceWalletState;
        _playerProgressState = services?.PlayerProgressState;
        _playerActionState = services?.PlayerActionState;
        _equippedItemsState = services?.EquippedItemsState;
        _subsystemMasteryState = services?.SubsystemMasteryState;
        _levelConfigLoader = services?.LevelConfigLoader;
        _exploreProgressController = GetNodeOrNull<ExploreProgressController>("../../ExploreProgressController");
        _simulationLevelFilterId = _levelConfigLoader?.ActiveLevelId ?? "";

        if (_activityState != null)
        {
            _activityState.ActivityTick += OnActivityTick;
        }
        if (_backpackState != null)
        {
            _backpackState.InventoryChanged += OnInventoryChanged;
            _backpackState.EquipmentInventoryChanged += OnEquipmentInventoryChanged;
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
        if (_formationState != null)
        {
            _formationState.RecipeProgressChanged += OnFormationChanged;
        }
        if (_resourceWalletState != null)
        {
            _resourceWalletState.WalletChanged += OnWalletChanged;
        }
        if (_playerProgressState != null)
        {
            _playerProgressState.RealmProgressChanged += OnRealmProgressChanged;
        }
        if (_equippedItemsState != null)
        {
            _equippedItemsState.EquippedItemsChanged += OnEquippedItemsChanged;
        }
        if (_levelConfigLoader != null)
        {
            _levelConfigLoader.ConfigLoaded += OnLevelConfigLoaded;
        }

        ApplyStaticTexts();

        BuildSettingsUi();
        ApplySettingsRuntime();
        UpdateSettingsControlsFromState();
        UpdateSettingsUiVisibility();

        BindButtons(_leftTabContentMap.Keys, "TopStrip/LeftTabs", SetActiveLeftTab);
        BindButtons(_rightTabContentMap.Keys, "TopStrip/RightTabs", SetActiveRightTab);

        RestoreActiveTabs(ActiveLeftTabName, ActiveRightTabName);
        RefreshCoinLabel();
        RefreshDynamicTabContent();
    }

    public override void _ExitTree()
    {
        if (_activityState != null)
        {
            _activityState.ActivityTick -= OnActivityTick;
        }
        if (_backpackState != null)
        {
            _backpackState.InventoryChanged -= OnInventoryChanged;
            _backpackState.EquipmentInventoryChanged -= OnEquipmentInventoryChanged;
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
        if (_formationState != null)
        {
            _formationState.RecipeProgressChanged -= OnFormationChanged;
        }
        if (_resourceWalletState != null)
        {
            _resourceWalletState.WalletChanged -= OnWalletChanged;
        }
        if (_playerProgressState != null)
        {
            _playerProgressState.RealmProgressChanged -= OnRealmProgressChanged;
        }
        if (_equippedItemsState != null)
        {
            _equippedItemsState.EquippedItemsChanged -= OnEquippedItemsChanged;
        }
        if (_levelConfigLoader != null)
        {
            _levelConfigLoader.ConfigLoaded -= OnLevelConfigLoaded;
        }
    }

    public void SetSpiritStone(int amount)
    {
        _coinLabel.Text = UiText.SpiritStone(amount);
    }

    public void RestoreActiveTabs(string leftTabName, string rightTabName)
    {
        if (!_leftTabContentMap.ContainsKey(leftTabName))
        {
            leftTabName = "CultivationTab";
        }

        if (!_rightTabContentMap.ContainsKey(rightTabName))
        {
            rightTabName = "BugTab";
        }

        ActiveLeftTabName = leftTabName;
        ActiveRightTabName = rightTabName;
        _isShowingRightTab = false;

        SyncButtons("TopStrip/LeftTabs", _leftTabContentMap.Keys, ActiveLeftTabName);
        SyncButtons("TopStrip/RightTabs", _rightTabContentMap.Keys, ActiveRightTabName);
        RefreshCurrentPageContent();
    }

    public Godot.Collections.Dictionary<string, Variant> ToSystemSettingsDictionary()
    {
        return new Godot.Collections.Dictionary<string, Variant>(_settings);
    }

    public void FromSystemSettingsDictionary(Godot.Collections.Dictionary<string, Variant> data)
    {
        foreach (string key in _settings.Keys)
        {
            if (data.ContainsKey(key))
            {
                _settings[key] = data[key];
            }
        }

        ApplySettingsRuntime();
        UpdateSettingsControlsFromState();
    }

    private void SetActiveLeftTab(string tabName)
    {
        if (!_leftTabContentMap.ContainsKey(tabName))
        {
            return;
        }

        // Leaving settings/right-page mode should immediately return to left tabs.
        if (ActiveRightTabName == "SettingsTab")
        {
            ActiveRightTabName = "BugTab";
        }

        ActiveLeftTabName = tabName;
        _isShowingRightTab = false;
        SyncButtons("TopStrip/LeftTabs", _leftTabContentMap.Keys, ActiveLeftTabName);
        SyncButtons("TopStrip/RightTabs", _rightTabContentMap.Keys, ActiveRightTabName);
        RefreshCurrentPageContent();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnActivityTick(double apThisSecond, double apFinal)
    {
        RefreshDynamicTabContent();
    }

    private void OnInventoryChanged(string itemId, int amount, int newTotal)
    {
        RefreshDynamicTabContent();
    }

    private void OnEquipmentInventoryChanged()
    {
        RefreshDynamicTabContent();
    }

    private void OnWalletChanged(double lingqi, double insight, int spiritStones)
    {
        RefreshCoinLabel();
        RefreshDynamicTabContent();
    }

    private void OnAlchemyChanged(string selectedRecipeId, float currentProgress, float requiredProgress)
    {
        RefreshDynamicTabContent();
    }

    private void OnPotionInventoryChanged(string potionId, int amount, int newTotal)
    {
        RefreshDynamicTabContent();
    }

    private void OnSmithingChanged(string targetEquipmentId, float currentProgress, float requiredProgress)
    {
        RefreshDynamicTabContent();
    }

    private void OnGardenChanged(string selectedRecipeId, float currentProgress, float requiredProgress)
    {
        RefreshDynamicTabContent();
    }

    private void OnMiningChanged(string selectedRecipeId, float currentProgress, float requiredProgress, int currentDurability)
    {
        RefreshDynamicTabContent();
    }

    private void OnFishingChanged(string selectedRecipeId, float currentProgress, float requiredProgress)
    {
        RefreshDynamicTabContent();
    }

    private void OnFormationChanged(string selectedRecipeId, float currentProgress, float requiredProgress)
    {
        RefreshDynamicTabContent();
    }

    private void OnRealmProgressChanged(int realmLevel, double realmExp, double realmExpRequired)
    {
        RefreshDynamicTabContent();
    }

    private void OnEquippedItemsChanged()
    {
        RefreshDynamicTabContent();
    }

    private void OnLevelConfigLoaded(string levelId, string levelName)
    {
        RefreshDynamicTabContent();
    }

    private void RefreshDynamicTabContent()
    {
        if (ActiveRightTabName == "SettingsTab")
        {
            return;
        }

        if (!_isShowingRightTab && (ActiveLeftTabName == "CultivationTab" || ActiveLeftTabName == "StatsTab" || ActiveLeftTabName == "BattleLogTab" || ActiveLeftTabName == "EquipmentTab" || ActiveLeftTabName == "BackpackTab" || ActiveLeftTabName == "ValidationTab"))
        {
            string content = GetLeftTabContent(ActiveLeftTabName);
            if (ActiveLeftTabName == "CultivationTab")
            {
                RefreshCultivationPanelContent();
                _cultivationContentLabel.Text = content;
            }
            else if (ActiveLeftTabName == "EquipmentTab")
            {
                _equipmentContentLabel.Text = content;
            }
            else if (ActiveLeftTabName == "BackpackTab")
            {
                RefreshBackpackGrid();
            }
            else if (ActiveLeftTabName == "ValidationTab")
            {
                RefreshValidationPanelContent();
            }
            else
            {
                _leftContentLabel.Text = content;
            }
        }
    }

    private string GetLeftTabContent(string tabName)
    {
        return tabName switch
        {
            "CultivationTab" => BuildCultivationOverviewText(),
            "BattleLogTab" => BuildBattleLogText(),
            "EquipmentTab" => BuildEquipmentOverviewText(),
            "BackpackTab" => UiText.BackpackTemplate,
            "StatsTab" => BuildStatsOverviewText(),
            "ValidationTab" => BuildValidationOverviewText(),
            _ => _leftTabContentMap[tabName]
        };
    }

    private string BuildEquipmentOverviewText()
    {
        if (_playerProgressState == null || _levelConfigLoader == null || _equippedItemsState == null || _backpackState == null)
        {
            return _leftTabContentMap["EquipmentTab"];
        }

        EquipmentStatProfile[] equippedProfiles = _equippedItemsState.GetEquippedProfiles();
        if (equippedProfiles.Length == 0)
        {
            return UiText.EquipmentEmpty;
        }

        CharacterStatBlock baseStats = PlayerBaseStatRules.BuildBaseStats(
            _playerProgressState.RealmLevel,
            _levelConfigLoader.PlayerBaseHp,
            _levelConfigLoader.PlayerAttackPerRound);
        CharacterStatModifier[] bonusModifiers =
        {
            ActivityEffectRules.CollectFormationModifier(
                ServiceLocator.Instance?.FormationState?.ActivePrimaryId ?? string.Empty,
                ServiceLocator.Instance?.FormationState?.ActiveSecondaryId ?? string.Empty,
                _backpackState.GetItemEntries(),
                GetMasteryLevel(PlayerActionState.ModeFormation)),
            ActivityEffectRules.CollectPermanentProgressModifier(new PlayerProgressPersistenceRules.PlayerProgressSnapshot(
                _playerProgressState.RealmLevel,
                _playerProgressState.RealmExp,
                _playerProgressState.HasUnlockedAdvancedAlchemyStudy,
                _playerProgressState.CurrentRealmActiveSeconds,
                _playerProgressState.BodyCultivationMaxHpFlat,
                _playerProgressState.BodyCultivationAttackFlat,
                _playerProgressState.BodyCultivationDefenseFlat,
                _playerProgressState.TemperCount,
                _playerProgressState.BoneforgeCount))
        };
        CharacterStatBlock finalStats = CharacterStatRules.BuildFinalStats(CharacterStatRules.BuildFinalStats(baseStats, bonusModifiers), equippedProfiles);
        EquipmentInstanceData[] backpackInstances = _backpackState.GetEquipmentInstances();
        EquipmentStatProfile[] backpackProfiles = _backpackState.GetEquipmentProfiles();
        RefreshSmithingControls(equippedProfiles);
        string text = EquipmentPresentationRules.BuildEquipmentPageText(baseStats, finalStats, equippedProfiles, backpackInstances, backpackProfiles);
        if (_smithingState != null && _smithingState.HasTarget && _equippedItemsState.TryGetEquippedProfileById(_smithingState.TargetEquipmentId, out EquipmentStatProfile target))
        {
            double percent = _smithingState.RequiredProgress > 0.0f ? _smithingState.CurrentProgress / _smithingState.RequiredProgress * 100.0 : 0.0;
            text += $"\n\n强化目标\n- {target.DisplayName} +{target.EnhanceLevel} ({percent:0}%)";
        }

        if (_formationState != null)
        {
            string activeFormationId = _formationState.ActivePrimaryId;
            text += $"\n\n当前阵法\n- {ExploreProgressPresentationRules.BuildFormationStatusText(UiText.BackpackItemName(activeFormationId), UiText.FormationSummary(activeFormationId), !string.IsNullOrEmpty(activeFormationId))}";
            if (!string.IsNullOrEmpty(_formationState.ActiveSecondaryId))
            {
                text += $"\n- 副阵：{ExploreProgressPresentationRules.BuildFormationStatusText(UiText.BackpackItemName(_formationState.ActiveSecondaryId), UiText.FormationSummary(_formationState.ActiveSecondaryId) + "（50%）", true)}";
            }
        }

        text += $"\n\n体修加成\n- 气血 +{_playerProgressState.BodyCultivationMaxHpFlat}\n- 攻击 +{_playerProgressState.BodyCultivationAttackFlat}\n- 防御 +{_playerProgressState.BodyCultivationDefenseFlat}";

        return text;
    }

    private void EquipFromBackpack(EquipmentSlotType slot)
    {
        if (_equippedItemsState == null || _backpackState == null)
        {
            return;
        }

        if (!_backpackState.TryTakeEquipmentBySlot(slot, out EquipmentStatProfile nextProfile))
        {
            return;
        }

        if (_equippedItemsState.TryEquipReplacing(nextProfile, out EquipmentStatProfile replacedProfile))
        {
            _backpackState.AddEquipment(replacedProfile with { IsEquipped = false });
        }
    }

    private void RefreshBackpackGrid()
    {
        _backpackGrid.Refresh();
        _backpackGrid.UpdateGridColumns();
    }

    private void OnBackpackGridCellClicked(string equipmentId)
    {
        if (equipmentId.StartsWith("__equip_slot__"))
        {
            string slotName = equipmentId.Replace("__equip_slot__", "");
            if (System.Enum.TryParse<EquipmentSlotType>(slotName, out EquipmentSlotType slot))
            {
                EquipFromBackpack(slot);
                RefreshBackpackGrid();
            }
            return;
        }

        // Clicking an equipment cell — for now just refresh (future: show detail panel)
        RefreshBackpackGrid();
    }

    private string BuildBattleLogText()
    {
        if (_exploreProgressController == null)
        {
            return UiText.BattleLogEmpty;
        }

        return _exploreProgressController.BuildRecentBattleLogText();
    }

    private string BuildCultivationOverviewText()
    {
        if (_playerProgressState == null || _resourceWalletState == null)
        {
            return _leftTabContentMap["CultivationTab"];
        }

        double expRequired = _playerProgressState.RealmExpRequired;
        double expPercent = expRequired > 0.0 ? _playerProgressState.RealmExp / expRequired * 100.0 : 0.0;

        var sb = new StringBuilder();
        sb.AppendLine(UiText.LeftTabCultivation);
        sb.AppendLine("当前状态");
        sb.AppendLine($"- 主行为: {BuildCultivationActionSummary()}");
        sb.AppendLine($"- 当前重心: {BuildCultivationFocusSummary()}");
        sb.AppendLine();
        sb.AppendLine("成长状态");
        sb.AppendLine($"- 当前境界: 炼气{_playerProgressState.RealmLevel}层");
        sb.AppendLine($"- 境界经验: {_playerProgressState.RealmExp:0.0}/{expRequired:0.0} ({expPercent:0}%)");
        sb.AppendLine($"- 突破状态: {UiText.CultivationBreakthroughStatus(_playerProgressState.CanBreakthrough, Mathf.Max(0.0f, (float)(expRequired - _playerProgressState.RealmExp)))}");
        sb.AppendLine($"- 悟性储备: {_resourceWalletState.Insight:0.0}（{BuildInsightStatusSummary()}）");
        sb.AppendLine();
        sb.AppendLine("战斗准备");
        sb.AppendLine($"- 当前区域: {BuildZoneReadinessSummary()}");
        sb.AppendLine($"- 装备概况: {BuildEquipmentReadinessSummary()}");
        sb.AppendLine($"- 丹药储备: {BuildPotionReadinessSummary()}");
        sb.AppendLine();
        sb.AppendLine("资源判断");
        sb.AppendLine($"- 灵气: {_resourceWalletState.Lingqi:0.0}（{BuildLingqiStatusSummary()}）");
        sb.AppendLine($"- 灵石: {_resourceWalletState.SpiritStones}（{BuildSpiritStoneStatusSummary()}）");

        if (_alchemyState != null && _alchemyState.HasSelectedRecipe && AlchemyRules.TryGetRecipe(_alchemyState.SelectedRecipeId, out AlchemyRules.RecipeSpec recipe))
        {
            double percent = _alchemyState.RequiredProgress > 0.0f ? _alchemyState.CurrentProgress / _alchemyState.RequiredProgress * 100.0 : 0.0;
            sb.AppendLine($"- 当前丹方: {recipe.DisplayName} ({percent:0}%)");
        }

        if (_smithingState != null && _smithingState.HasTarget && _equippedItemsState != null && _equippedItemsState.TryGetEquippedProfileById(_smithingState.TargetEquipmentId, out EquipmentStatProfile target))
        {
            double percent = _smithingState.RequiredProgress > 0.0f ? _smithingState.CurrentProgress / _smithingState.RequiredProgress * 100.0 : 0.0;
            sb.AppendLine($"- 强化目标: {target.DisplayName} +{target.EnhanceLevel} ({percent:0}%)");
        }

        if (_formationState != null)
        {
            string activeFormationId = _formationState.ActivePrimaryId;
            sb.AppendLine($"- 当前阵法: {ExploreProgressPresentationRules.BuildFormationStatusText(UiText.BackpackItemName(activeFormationId), UiText.FormationSummary(activeFormationId), !string.IsNullOrEmpty(activeFormationId))}");
            if (!string.IsNullOrEmpty(_formationState.ActiveSecondaryId))
            {
                sb.AppendLine($"- 当前副阵: {ExploreProgressPresentationRules.BuildFormationStatusText(UiText.BackpackItemName(_formationState.ActiveSecondaryId), UiText.FormationSummary(_formationState.ActiveSecondaryId) + "（50%）", true)}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("当前判断");
        sb.AppendLine($"- 核心判断: {BuildPrimaryCultivationAssessment()}");

        string alternate = BuildSecondaryCultivationAssessment();
        if (!string.IsNullOrEmpty(alternate))
        {
            sb.AppendLine($"- 补充判断: {alternate}");
        }

        return sb.ToString().TrimEnd();
    }

    private string BuildMasteryOverviewText()
    {
        if (_playerProgressState == null)
        {
            return UiText.CultivationUnavailable;
        }

        StringBuilder sb = new();
        sb.AppendLine(UiText.MasterySectionTitle);
        sb.AppendLine($"当前悟性: {_resourceWalletState?.Insight ?? 0.0:0.0}");
        foreach (string systemId in GetTrackedMasterySystems())
        {
            int currentLevel = GetMasteryLevel(systemId);
            SubsystemMasteryRules.TryGetDefinition(systemId, currentLevel, out var currentDefinition);
            string effectDescription = UiText.MasteryEffectDescription(currentDefinition.EffectId, currentDefinition.EffectValue);
            string nextUnlock = BuildNextMasteryUnlockDescription(systemId, currentLevel);
            sb.AppendLine(UiText.MasteryStatusLine(systemId, currentLevel, effectDescription, nextUnlock));
        }

        return sb.ToString().TrimEnd();
    }

    private string BuildNextMasteryUnlockDescription(string systemId, int currentLevel)
    {
        int nextLevel = currentLevel + 1;
        if (!SubsystemMasteryRules.TryGetDefinition(systemId, nextLevel, out var nextDefinition))
        {
            return "已满级";
        }

        return $"Lv{nextDefinition.Level}（{nextDefinition.InsightCost:0}悟性，炼气{nextDefinition.RequiredRealmLevel}层）";
    }

    private string BuildCultivationActionSummary()
    {
        string actionId = _playerActionState?.ActionId ?? PlayerActionState.ActionDungeon;
        string actionName = ExploreProgressPresentationRules.GetActionModeDisplayName(actionId);
        return actionId == PlayerActionState.ActionDungeon
            ? $"{actionName}（目标：{BuildActiveZoneName()}）"
            : actionName;
    }

    private string BuildCultivationFocusSummary()
    {
        string actionId = _playerActionState?.ActionId ?? PlayerActionState.ActionDungeon;
        return actionId switch
        {
            PlayerActionState.ActionCultivation => "稳定积累灵气、悟性与境界经验",
            PlayerActionState.ActionAlchemy => BuildAlchemyFocusSummary(),
            PlayerActionState.ActionSmithing => BuildSmithingFocusSummary(),
            _ => $"刷取 {BuildActiveZoneName()} 的材料、装备与过区进度",
        };
    }

    private string BuildAlchemyFocusSummary()
    {
        if (_alchemyState != null && _alchemyState.HasSelectedRecipe && AlchemyRules.TryGetRecipe(_alchemyState.SelectedRecipeId, out AlchemyRules.RecipeSpec recipe))
        {
            return $"炼制{recipe.DisplayName}，为后续战斗补充消耗品";
        }

        return "准备战斗消耗品，当前尚未指定丹方";
    }

    private string BuildSmithingFocusSummary()
    {
        if (_smithingState != null && _smithingState.HasTarget && _equippedItemsState != null && _equippedItemsState.TryGetEquippedProfileById(_smithingState.TargetEquipmentId, out EquipmentStatProfile target))
        {
            return $"强化{target.DisplayName}，提升当前主力战斗强度";
        }

        return "强化当前装备，当前尚未指定强化目标";
    }

    private string BuildInsightStatusSummary()
    {
        if (_playerProgressState == null || _resourceWalletState == null)
        {
            return "暂不可用";
        }

        int dungeonMasteryLevel = _subsystemMasteryState?.GetLevel(PlayerActionState.ModeDungeon) ?? 1;
        if (_exploreProgressController != null && _exploreProgressController.CanApplyBossWeaknessInsight(dungeonMasteryLevel))
        {
            return "状态：副本精通已可看破当前 Boss 弱点";
        }

        if (CanUnlockMastery(PlayerActionState.ModeAlchemy))
        {
            return "状态：可用于参悟高阶丹方";
        }

        return _playerProgressState.CanBreakthrough ? "状态：可作为突破后的下阶段储备" : "状态：继续积累，兼顾突破与参悟";
    }

    private string BuildZoneReadinessSummary()
    {
        string zoneName = BuildActiveZoneName();
        int dangerLevel = _levelConfigLoader?.ActiveLevelDangerLevel ?? 1;
        int realmLevel = _playerProgressState?.RealmLevel ?? 1;
        int diff = realmLevel - dangerLevel;
        string readiness = diff >= 2
            ? "当前战力明显占优，可加快清图"
            : diff >= 0
                ? "可稳定推进，适合准备 Boss 挑战"
                : "区域压力偏高，更适合先补强后再推进";
        return $"{zoneName}（危险度 {dangerLevel}）— {readiness}";
    }

    private string BuildEquipmentReadinessSummary()
    {
        EquipmentStatProfile[] profiles = _equippedItemsState?.GetEquippedProfiles() ?? System.Array.Empty<EquipmentStatProfile>();
        if (profiles.Length == 0)
        {
            return "当前未装备物品，战斗强度会明显受限";
        }

        int maxEnhance = 0;
        for (int i = 0; i < profiles.Length; i++)
        {
            maxEnhance = Mathf.Max(maxEnhance, profiles[i].EnhanceLevel);
        }

        return maxEnhance > 0
            ? $"已装备 {profiles.Length} 件，当前最高强化 +{maxEnhance}"
            : $"已装备 {profiles.Length} 件，仍有明显强化空间";
    }

    private string BuildPotionReadinessSummary()
    {
        Dictionary<string, int> potions = _potionInventoryState?.GetPotionEntries() ?? new Dictionary<string, int>();
        int huiqi = potions.TryGetValue("potion_huiqi_dan", out int huiqiCount) ? huiqiCount : 0;
        int juling = potions.TryGetValue("potion_juling_san", out int julingCount) ? julingCount : 0;

        if (huiqi <= 0 && juling <= 0)
        {
            return "结果：当前无战斗丹药，续航与战斗容错偏弱";
        }

        if (huiqi < 2)
        {
            return $"回气丹 {huiqi}，聚灵散 {juling}，续航略紧";
        }

        return $"回气丹 {huiqi}，聚灵散 {juling}，可支撑连续战斗";
    }

    private string BuildLingqiStatusSummary()
    {
        double lingqi = _resourceWalletState?.Lingqi ?? 0.0;
        if (lingqi < 50.0)
        {
            return "偏紧，优先修炼或减少强化消耗";
        }

        if (lingqi < 150.0)
        {
            return "可用，够支撑一轮炼丹或低阶强化";
        }

        return "充足，可同时覆盖炼丹、强化与推进消耗";
    }

    private string BuildSpiritStoneStatusSummary()
    {
        int spiritStones = _resourceWalletState?.SpiritStones ?? 0;
        if (spiritStones < 30)
        {
            return "偏少，先以副本与出售材料补充";
        }

        if (spiritStones < 100)
        {
            return "状态：可用于少量便利消费或兑换";
        }

        return "储备充足，可作为应急便利资金";
    }

    private string BuildPrimaryCultivationAssessment()
    {
        if (_playerProgressState?.CanBreakthrough == true)
        {
            return "结果：当前已满足突破条件，进入下一境界的门槛已经打开。";
        }

        Dictionary<string, int> potions = _potionInventoryState?.GetPotionEntries() ?? new Dictionary<string, int>();
        int huiqi = potions.TryGetValue("potion_huiqi_dan", out int huiqiCount) ? huiqiCount : 0;
        if (huiqi < 2 && _resourceWalletState != null && _backpackState != null && AlchemyRules.CanStartRecipe("recipe_huiqi_dan", _resourceWalletState.Lingqi, _backpackState.GetItemEntries(), GetAlchemyMasteryLevel()))
        {
            return "结果：当前已经具备补充回气丹的条件，战斗容错仍有提升空间。";
        }

        if (_smithingState != null && _smithingState.HasTarget && _equippedItemsState != null && _equippedItemsState.TryGetEquippedProfileById(_smithingState.TargetEquipmentId, out EquipmentStatProfile target))
        {
            return $"结果：当前强化重心集中在 {target.DisplayName}，装备成长链路已经建立。";
        }

        if (_resourceWalletState != null && _resourceWalletState.Lingqi < 50.0)
        {
            return "结果：当前灵气偏紧，炼丹、强化和持续推进都会受到约束。";
        }

        return $"结果：当前主循环仍围绕 {BuildActiveZoneName()} 展开，资源与过区进度保持同步增长。";
    }

    private string BuildSecondaryCultivationAssessment()
    {
        if (CanUnlockMastery(PlayerActionState.ModeAlchemy))
        {
            return "状态：当前悟性已经足够支撑一次高阶丹方参悟。";
        }

        int dungeonMasteryLevel = _subsystemMasteryState?.GetLevel(PlayerActionState.ModeDungeon) ?? 1;
        if (_exploreProgressController != null && _exploreProgressController.CanApplyBossWeaknessInsight(dungeonMasteryLevel))
        {
            return "状态：当前已具备 Boss 弱点洞察条件，Boss 战准备度较高。";
        }

        if (_resourceWalletState != null && _resourceWalletState.SpiritStones >= 50)
        {
            return "状态：当前灵石储备已能覆盖一定便利消费和兑换操作。";
        }

        return string.Empty;
    }

    private string BuildActiveZoneName()
    {
        if (_levelConfigLoader == null)
        {
            return UiText.DefaultZoneName;
        }

        string actionTargetId = _playerActionState?.ActionTargetId ?? string.Empty;
        if (!string.IsNullOrEmpty(actionTargetId))
        {
            return _levelConfigLoader.GetLevelName(actionTargetId);
        }

        return _levelConfigLoader.ActiveLevelName;
    }

    private void RefreshCultivationPanelContent()
    {
        if (_cultivationStatusLabel == null || _cultivationBreakthroughButton == null)
        {
            return;
        }

        if (_playerProgressState == null)
        {
            _cultivationStatusLabel.Text = UiText.CultivationUnavailable;
            _cultivationBreakthroughButton.Disabled = true;
            _cultivationBreakthroughButton.Text = UiText.BreakthroughButtonLabel;
            _cultivationBreakthroughButton.TooltipText = UiText.CultivationUnavailableTooltip;
            _cultivationBossInsightButton.Disabled = true;
            _cultivationAlchemyRecipeOption.Disabled = true;
            _cultivationAlchemyStartButton.Disabled = true;
            _cultivationGardenOption.Disabled = true;
            _cultivationGardenStartButton.Disabled = true;
            _cultivationMiningOption.Disabled = true;
            _cultivationMiningStartButton.Disabled = true;
            _cultivationFishingOption.Disabled = true;
            _cultivationFishingStartButton.Disabled = true;
            _cultivationFormationOption.Disabled = true;
            _cultivationFormationActivateButton.Disabled = true;
            _cultivationFormationSecondaryOption.Disabled = true;
            _cultivationFormationSecondaryActivateButton.Disabled = true;
            if (_cultivationMasteryLabel != null)
            {
                _cultivationMasteryLabel.Text = UiText.CultivationUnavailable;
            }

            foreach (Button button in _cultivationMasteryButtons.Values)
            {
                button.Disabled = true;
                button.TooltipText = UiText.CultivationUnavailableTooltip;
            }
            return;
        }

        double remainingExp = Mathf.Max(0.0f, (float)(_playerProgressState.RealmExpRequired - _playerProgressState.RealmExp));
        bool canBreakthrough = _playerProgressState.CanBreakthrough;

        _cultivationStatusLabel.Text = UiText.CultivationBreakthroughStatus(canBreakthrough, remainingExp);
        _cultivationStatusLabel.AddThemeColorOverride("font_color", canBreakthrough
            ? new Color(0.3f, 0.7f, 0.3f)
            : new Color(0.8f, 0.6f, 0.2f));
        _cultivationBreakthroughButton.Disabled = !canBreakthrough;
        _cultivationBreakthroughButton.Text = canBreakthrough
            ? UiText.BreakthroughButtonReadyLabel
            : UiText.BreakthroughButtonLabel;
        _cultivationBreakthroughButton.TooltipText = UiText.CultivationBreakthroughTooltip(canBreakthrough, remainingExp);

        int dungeonMasteryLevel = _subsystemMasteryState?.GetLevel(PlayerActionState.ModeDungeon) ?? 1;
        bool canProbeBoss = _exploreProgressController != null
            && _exploreProgressController.CanApplyBossWeaknessInsight(dungeonMasteryLevel);
        _cultivationBossInsightButton.Disabled = !canProbeBoss;
        _cultivationBossInsightButton.TooltipText = canProbeBoss
            ? UiText.BossWeaknessInsightReadyTooltip
            : UiText.BossWeaknessInsightLockedTooltip;

        RefreshMasteryControls();

        RefreshAlchemyControls();
        RefreshGatheringControls();
        RefreshFormationControls();
    }

    private void RefreshMasteryControls()
    {
        if (_cultivationMasteryLabel == null)
        {
            return;
        }

        _cultivationMasteryLabel.Text = BuildMasteryOverviewText();
        foreach (string systemId in GetTrackedMasterySystems())
        {
            if (!_cultivationMasteryButtons.TryGetValue(systemId, out Button? button))
            {
                continue;
            }

            int currentLevel = GetMasteryLevel(systemId);
            int nextLevel = currentLevel + 1;
            if (!SubsystemMasteryRules.TryGetDefinition(systemId, nextLevel, out var nextDefinition))
            {
                button.Text = UiText.MasteryUnlockButton(systemId, currentLevel);
                button.Disabled = true;
                button.TooltipText = UiText.MasteryMaxLevelTooltip;
                continue;
            }

            bool canUnlock = _playerProgressState != null
                && _resourceWalletState != null
                && InsightSpendRules.CanUnlockMastery(systemId, currentLevel, _resourceWalletState.Insight, _playerProgressState.RealmLevel);
            button.Text = UiText.MasteryUnlockButton(systemId, nextDefinition.Level);
            button.Disabled = !canUnlock;
            button.TooltipText = canUnlock
                ? UiText.MasteryUnlockTooltip(systemId, currentLevel, nextDefinition.Level, nextDefinition.InsightCost, nextDefinition.RequiredRealmLevel)
                : UiText.MasteryUnavailableTooltip;
        }
    }

    private void OnCultivationBreakthroughPressed()
    {
        if (_playerProgressState == null)
        {
            return;
        }

        if (!_playerProgressState.TryBreakthrough())
        {
            RefreshCultivationPanelContent();
            return;
        }

        RefreshDynamicTabContent();
    }

    private void OnSmithingTargetSelected(long index)
    {
        if (_equipmentSmithingTargetOption == null || _equippedItemsState == null || _smithingState == null)
        {
            return;
        }

        int selected = (int)index;
        if (selected < 0 || selected >= _equipmentSmithingTargetOption.ItemCount)
        {
            return;
        }

        string equipmentId = _equipmentSmithingTargetOption.GetItemMetadata(selected).AsString();
        if (_equippedItemsState.TryGetEquippedProfileById(equipmentId, out EquipmentStatProfile profile))
        {
            _smithingState.SelectTarget(profile.EquipmentId, profile.EnhanceLevel);
        }

        RefreshDynamicTabContent();
    }

    private void OnSmithingStartPressed()
    {
        if (_smithingState == null || _equippedItemsState == null || _backpackState == null || _resourceWalletState == null || !_smithingState.HasTarget)
        {
            return;
        }

        if (!_equippedItemsState.TryGetEquippedProfileById(_smithingState.TargetEquipmentId, out EquipmentStatProfile profile))
        {
            return;
        }

        if (!SmithingRules.CanEnhance(profile, _backpackState, _resourceWalletState, GetSmithingMasteryLevel()))
        {
            RefreshDynamicTabContent();
            return;
        }

        RefreshDynamicTabContent();
    }

    private void RefreshSmithingControls(EquipmentStatProfile[] equippedProfiles)
    {
        if (_equipmentSmithingTargetOption == null || _equipmentSmithingStartButton == null || _smithingState == null || _backpackState == null || _resourceWalletState == null)
        {
            return;
        }

        _equipmentSmithingTargetOption.Clear();
        for (int i = 0; i < equippedProfiles.Length; i++)
        {
            EquipmentStatProfile profile = equippedProfiles[i];
            _equipmentSmithingTargetOption.AddItem($"{profile.DisplayName} +{profile.EnhanceLevel}", i);
            _equipmentSmithingTargetOption.SetItemMetadata(i, profile.EquipmentId);
            if (_smithingState.TargetEquipmentId == profile.EquipmentId)
            {
                _equipmentSmithingTargetOption.Select(i);
            }
        }

        bool canStart = _smithingState.HasTarget
            && _equippedItemsState != null
            && _equippedItemsState.TryGetEquippedProfileById(_smithingState.TargetEquipmentId, out EquipmentStatProfile target)
            && SmithingRules.CanEnhance(target, _backpackState, _resourceWalletState, GetSmithingMasteryLevel());
        _equipmentSmithingStartButton.Disabled = !canStart;
        _equipmentSmithingStartButton.TooltipText = canStart
            ? "切到炼器模式后输入会推进当前强化。"
            : "请选择可强化装备，并准备足够碎片、碎符与灵气。";
    }

    private void OnBossWeaknessInsightPressed()
    {
        if (_exploreProgressController == null)
        {
            return;
        }

        int dungeonMasteryLevel = _subsystemMasteryState?.GetLevel(PlayerActionState.ModeDungeon) ?? 1;
        if (!_exploreProgressController.CanApplyBossWeaknessInsight(dungeonMasteryLevel))
        {
            RefreshCultivationPanelContent();
            return;
        }

        _exploreProgressController.TryApplyBossWeaknessInsight();
        RefreshDynamicTabContent();
    }

    private void OnMasteryUnlockPressed(string systemId)
    {
        if (_resourceWalletState == null || _playerProgressState == null)
        {
            return;
        }

        int currentLevel = GetMasteryLevel(systemId);
        if (!InsightSpendRules.CanUnlockMastery(systemId, currentLevel, _resourceWalletState.Insight, _playerProgressState.RealmLevel))
        {
            RefreshCultivationPanelContent();
            return;
        }

        if (!InsightSpendRules.SpendInsightForMastery(systemId, currentLevel, _resourceWalletState.Insight, _playerProgressState.RealmLevel, out int nextLevel, out double remainingInsight))
        {
            RefreshCultivationPanelContent();
            return;
        }

        _resourceWalletState.SpendInsight(_resourceWalletState.Insight - remainingInsight);
        _subsystemMasteryState?.TrySetLevel(systemId, nextLevel);
        _cultivationStatusLabel.Text = UiText.MasteryUnlockSuccessPrefix + UiText.MasterySystemName(systemId);
        RefreshDynamicTabContent();
    }

    private void OnAlchemyRecipeSelected(long index)
    {
        if (_alchemyState == null || index < 0 || index >= _cultivationAlchemyRecipeOption.ItemCount)
        {
            return;
        }

        string recipeId = _cultivationAlchemyRecipeOption.GetItemMetadata((int)index).AsString();
        _alchemyState.SelectRecipe(recipeId);
        RefreshDynamicTabContent();
    }

    private void OnAlchemyStartPressed()
    {
        if (_alchemyState == null || _backpackState == null || _resourceWalletState == null || !_alchemyState.HasSelectedRecipe)
        {
            RefreshCultivationPanelContent();
            return;
        }

        if (!AlchemyRules.CanStartRecipe(_alchemyState.SelectedRecipeId, _resourceWalletState.Lingqi, _backpackState.GetItemEntries(), GetAlchemyMasteryLevel()))
        {
            RefreshCultivationPanelContent();
            _cultivationStatusLabel.Text = "材料不足或灵气不足，无法开始当前丹方。";
            return;
        }

        RefreshCultivationPanelContent();
        _cultivationStatusLabel.Text = "炼丹已开始，切到炼丹模式后输入会推进进度。";
    }

    private void OnGardenRecipeSelected(long index)
    {
        if (_gardenState == null || index < 0 || index >= _cultivationGardenOption.ItemCount)
        {
            return;
        }

        _gardenState.SelectCrop(_cultivationGardenOption.GetItemMetadata((int)index).AsString());
        RefreshDynamicTabContent();
    }

    private void OnMiningRecipeSelected(long index)
    {
        if (_miningState == null || index < 0 || index >= _cultivationMiningOption.ItemCount)
        {
            return;
        }

        _miningState.SelectNode(_cultivationMiningOption.GetItemMetadata((int)index).AsString());
        RefreshDynamicTabContent();
    }

    private void OnFishingRecipeSelected(long index)
    {
        if (_fishingState == null || index < 0 || index >= _cultivationFishingOption.ItemCount)
        {
            return;
        }

        _fishingState.SelectPond(_cultivationFishingOption.GetItemMetadata((int)index).AsString());
        RefreshDynamicTabContent();
    }

    private void OnGardenStartPressed()
    {
        if (_gardenState == null || !_gardenState.HasSelectedCrop)
        {
            RefreshCultivationPanelContent();
            return;
        }

        _cultivationStatusLabel.Text = "灵田已就绪，切到灵田模式后输入会推进种植。";
        RefreshCultivationPanelContent();
    }

    private void OnMiningStartPressed()
    {
        if (_miningState == null || !_miningState.HasSelectedNode)
        {
            RefreshCultivationPanelContent();
            return;
        }

        _cultivationStatusLabel.Text = "矿脉已就绪，切到矿脉模式后输入会推进开采。";
        RefreshCultivationPanelContent();
    }

    private void OnFishingStartPressed()
    {
        if (_fishingState == null || !_fishingState.HasSelectedPond)
        {
            RefreshCultivationPanelContent();
            return;
        }

        _cultivationStatusLabel.Text = "灵渔已就绪，切到灵渔模式后输入会推进垂钓。";
        RefreshCultivationPanelContent();
    }

    private void OnFormationSelected(long index)
    {
        if (_formationState == null || index < 0 || index >= _cultivationFormationOption.ItemCount)
        {
            return;
        }

        string formationId = _cultivationFormationOption.GetItemMetadata((int)index).AsString();
        _formationState.SelectRecipe(formationId);
        RefreshDynamicTabContent();
    }

    private void OnFormationActivatePressed()
    {
        if (_formationState == null || string.IsNullOrEmpty(_formationState.SelectedRecipeId))
        {
            RefreshCultivationPanelContent();
            return;
        }

        if (_formationState.TryActivatePrimary(_formationState.SelectedRecipeId))
        {
            _cultivationStatusLabel.Text = $"阵法已切换：{UiText.BackpackItemName(_formationState.ActivePrimaryId)}";
        }
        else
        {
            _cultivationStatusLabel.Text = "尚未拥有该阵法，需先完成一次制作。";
        }

        RefreshCultivationPanelContent();
        RefreshDynamicTabContent();
    }

    private void OnFormationSecondarySelected(long index)
    {
        if (_formationState == null || index < 0 || index >= _cultivationFormationSecondaryOption.ItemCount)
        {
            return;
        }

        RefreshDynamicTabContent();
    }

    private void OnFormationSecondaryActivatePressed()
    {
        if (_formationState == null)
        {
            RefreshCultivationPanelContent();
            return;
        }

        if (GetMasteryLevel(PlayerActionState.ModeFormation) < 3)
        {
            _cultivationStatusLabel.Text = "阵法精通 Lv3 后解锁副阵槽。";
            RefreshCultivationPanelContent();
            return;
        }

        string selectedFormationId = _cultivationFormationSecondaryOption.GetSelectedMetadata().AsString();
        if (_formationState.TryActivateSecondary(selectedFormationId))
        {
            _cultivationStatusLabel.Text = $"副阵已切换：{UiText.BackpackItemName(_formationState.ActiveSecondaryId)}";
        }
        else
        {
            _cultivationStatusLabel.Text = "尚未拥有该阵法，无法设置副阵。";
        }

        RefreshCultivationPanelContent();
        RefreshDynamicTabContent();
    }

    private void RefreshAlchemyControls()
    {
        if (_cultivationAlchemyRecipeOption == null || _cultivationAlchemyStartButton == null)
        {
            return;
        }

        bool canUseAlchemy = _alchemyState != null && _backpackState != null && _resourceWalletState != null;
        _cultivationAlchemyRecipeOption.Disabled = !canUseAlchemy;
        _cultivationAlchemyStartButton.Disabled = !canUseAlchemy;
        if (!canUseAlchemy)
        {
            return;
        }

        if (_cultivationAlchemyRecipeOption.ItemCount == 0)
        {
            IReadOnlyList<AlchemyRules.RecipeSpec> recipes = AlchemyRules.GetRecipes();
            for (int i = 0; i < recipes.Count; i++)
            {
                _cultivationAlchemyRecipeOption.AddItem(recipes[i].DisplayName, i);
                _cultivationAlchemyRecipeOption.SetItemMetadata(i, recipes[i].RecipeId);
            }
        }

        if (_alchemyState != null && !string.IsNullOrEmpty(_alchemyState.SelectedRecipeId))
        {
            for (int i = 0; i < _cultivationAlchemyRecipeOption.ItemCount; i++)
            {
                if (_cultivationAlchemyRecipeOption.GetItemMetadata(i).AsString() == _alchemyState.SelectedRecipeId)
                {
                    _cultivationAlchemyRecipeOption.Select(i);
                    break;
                }
            }
        }

        bool canStart = _alchemyState != null
            && _alchemyState.HasSelectedRecipe
            && AlchemyRules.CanStartRecipe(_alchemyState.SelectedRecipeId, _resourceWalletState!.Lingqi, _backpackState!.GetItemEntries(), GetAlchemyMasteryLevel());
        _cultivationAlchemyStartButton.Disabled = !canStart;
        _cultivationAlchemyStartButton.TooltipText = canStart
            ? "满足材料后，切到炼丹模式即可推进当前批次。"
            : "请选择可用丹方，并准备足够灵草与灵气。";
    }

    private void RefreshGatheringControls()
    {
        RefreshGardenControls();
        RefreshMiningControls();
        RefreshFishingControls();
    }

    private void RefreshGardenControls()
    {
        bool enabled = _cultivationGardenOption != null && _cultivationGardenStartButton != null && _gardenState != null;
        if (!enabled)
        {
            return;
        }

        if (_cultivationGardenOption.ItemCount == 0)
        {
            IReadOnlyList<GardenRules.CropSpec> crops = GardenRules.GetCrops();
            for (int i = 0; i < crops.Count; i++)
            {
                _cultivationGardenOption.AddItem(crops[i].DisplayName, i);
                _cultivationGardenOption.SetItemMetadata(i, crops[i].RecipeId);
            }
        }

        SelectCurrentOption(_cultivationGardenOption, _gardenState.SelectedRecipeId);
        bool canStart = _gardenState.HasSelectedCrop;
        _cultivationGardenOption.Disabled = false;
        _cultivationGardenStartButton.Disabled = !canStart;
    }

    private void RefreshMiningControls()
    {
        bool enabled = _cultivationMiningOption != null && _cultivationMiningStartButton != null && _miningState != null;
        if (!enabled)
        {
            return;
        }

        if (_cultivationMiningOption.ItemCount == 0)
        {
            IReadOnlyList<MiningRules.NodeSpec> nodes = MiningRules.GetNodes();
            for (int i = 0; i < nodes.Count; i++)
            {
                _cultivationMiningOption.AddItem(nodes[i].DisplayName, i);
                _cultivationMiningOption.SetItemMetadata(i, nodes[i].RecipeId);
            }
        }

        SelectCurrentOption(_cultivationMiningOption, _miningState.SelectedRecipeId);
        bool canStart = _miningState.HasSelectedNode;
        _cultivationMiningOption.Disabled = false;
        _cultivationMiningStartButton.Disabled = !canStart;
    }

    private void RefreshFishingControls()
    {
        bool enabled = _cultivationFishingOption != null && _cultivationFishingStartButton != null && _fishingState != null;
        if (!enabled)
        {
            return;
        }

        if (_cultivationFishingOption.ItemCount == 0)
        {
            IReadOnlyList<FishingRules.PondSpec> ponds = FishingRules.GetPonds();
            for (int i = 0; i < ponds.Count; i++)
            {
                _cultivationFishingOption.AddItem(ponds[i].DisplayName, i);
                _cultivationFishingOption.SetItemMetadata(i, ponds[i].RecipeId);
            }
        }

        SelectCurrentOption(_cultivationFishingOption, _fishingState.SelectedRecipeId);
        bool canStart = _fishingState.HasSelectedPond;
        _cultivationFishingOption.Disabled = false;
        _cultivationFishingStartButton.Disabled = !canStart;
    }

    private void RefreshFormationControls()
    {
        if (_cultivationFormationOption == null || _cultivationFormationActivateButton == null || _cultivationFormationSecondaryOption == null || _cultivationFormationSecondaryActivateButton == null || _formationState == null)
        {
            return;
        }

        if (_cultivationFormationOption.ItemCount == 0)
        {
            string[] formationIds =
            {
                "formation_spirit_plate",
                "formation_guard_flag",
                "formation_harvest_array",
                "formation_craft_array",
            };

            for (int i = 0; i < formationIds.Length; i++)
            {
                string formationId = formationIds[i];
                _cultivationFormationOption.AddItem(UiText.BackpackItemName(formationId), i);
                _cultivationFormationOption.SetItemMetadata(i, formationId);
            }
        }

        if (_cultivationFormationSecondaryOption.ItemCount == 0)
        {
            for (int i = 0; i < _cultivationFormationOption.ItemCount; i++)
            {
                _cultivationFormationSecondaryOption.AddItem(_cultivationFormationOption.GetItemText(i), i);
                _cultivationFormationSecondaryOption.SetItemMetadata(i, _cultivationFormationOption.GetItemMetadata(i));
            }
        }

        SelectCurrentOption(_cultivationFormationOption, _formationState.SelectedRecipeId);
        SelectCurrentOption(_cultivationFormationSecondaryOption, _formationState.ActiveSecondaryId);
        _cultivationFormationOption.Disabled = false;
        _cultivationFormationActivateButton.Disabled = string.IsNullOrEmpty(_formationState.SelectedRecipeId);
        _cultivationFormationActivateButton.TooltipText = string.IsNullOrEmpty(_formationState.ActivePrimaryId)
            ? "选择已拥有阵法并设为当前生效"
            : ExploreProgressPresentationRules.BuildFormationStatusText(
                UiText.BackpackItemName(_formationState.ActivePrimaryId),
                UiText.FormationSummary(_formationState.ActivePrimaryId),
                true);

        int formationMasteryLevel = GetMasteryLevel(PlayerActionState.ModeFormation);
        bool secondaryUnlocked = FormationRules.GetMaxSlotCount(formationMasteryLevel) >= 2;
        _cultivationFormationSecondaryOption.Disabled = !secondaryUnlocked;
        _cultivationFormationSecondaryActivateButton.Disabled = !secondaryUnlocked;
        _cultivationFormationSecondaryActivateButton.TooltipText = secondaryUnlocked
            ? "为当前策略挂载副阵，副阵按 50% 效果生效。"
            : "阵法精通 Lv3 后解锁副阵槽。";
    }

    private static void SelectCurrentOption(OptionButton option, string selectedId)
    {
        if (string.IsNullOrEmpty(selectedId))
        {
            return;
        }

        for (int i = 0; i < option.ItemCount; i++)
        {
            if (option.GetItemMetadata(i).AsString() == selectedId)
            {
                option.Select(i);
                break;
            }
        }
    }

    private int GetAlchemyMasteryLevel()
    {
        return GetMasteryLevel(PlayerActionState.ModeAlchemy);
    }

    private int GetSmithingMasteryLevel()
    {
        return GetMasteryLevel(PlayerActionState.ModeSmithing);
    }

    private int GetMasteryLevel(string systemId)
    {
        return _subsystemMasteryState?.GetLevel(systemId) ?? 1;
    }

    private bool CanUnlockMastery(string systemId)
    {
        if (_resourceWalletState == null || _playerProgressState == null)
        {
            return false;
        }

        return InsightSpendRules.CanUnlockMastery(systemId, GetMasteryLevel(systemId), _resourceWalletState.Insight, _playerProgressState.RealmLevel);
    }

    private static string[] GetTrackedMasterySystems()
    {
        return new[]
        {
            PlayerActionState.ModeDungeon,
            PlayerActionState.ModeCultivation,
            PlayerActionState.ModeAlchemy,
            PlayerActionState.ModeSmithing,
        };
    }

    private string BuildStatsOverviewText()
    {
        if (_activityState == null || _resourceWalletState == null || _playerProgressState == null)
        {
            return _leftTabContentMap["StatsTab"];
        }

        int battleCount = _exploreProgressController?.TotalBattleCount ?? 0;
        int battleWins = _exploreProgressController?.TotalBattleWinCount ?? 0;
        double winRate = battleCount > 0 ? (double)battleWins / battleCount : 0.0;
        double currentRealmDays = _playerProgressState.CurrentRealmActiveSeconds / 86400.0;

        return
            UiText.StatsOverview(
                _activityState.TotalKeyDownCount,
                _activityState.TotalMouseClickCount,
                _activityState.TotalMouseScrollSteps,
                _activityState.TotalMouseMoveDistancePx,
                _activityState.TotalActiveSeconds,
                _playerProgressState.RealmLevel,
                currentRealmDays,
                battleCount,
                winRate,
                _resourceWalletState.TotalEarnedLingqi,
                _resourceWalletState.TotalEarnedInsight,
                _resourceWalletState.TotalEarnedSpiritStones);
    }

    private string BuildValidationOverviewText()
    {
        if (_levelConfigLoader == null)
        {
            return "配置校验当前不可用：LevelConfigLoader 未加载。";
        }

        var filtered = GetFilteredValidationItems();
        string body = ConfigValidationViewFormatter.BuildBody(filtered, 8);
        return body.TrimEnd();
    }

    private string BuildValidationFilterInfoText()
    {
        if (_levelConfigLoader == null)
        {
            return "当前关卡：未加载\n过滤：不可用\n模拟筛选：不可用";
        }

        string activeLevelName = string.IsNullOrEmpty(_levelConfigLoader.ActiveLevelName) ? "未选择" : _levelConfigLoader.ActiveLevelName;
        string activeLevelId = string.IsNullOrEmpty(_levelConfigLoader.ActiveLevelId) ? "-" : _levelConfigLoader.ActiveLevelId;

        var sb = new StringBuilder();
        sb.AppendLine($"当前关卡：{activeLevelName} ({activeLevelId})");
        sb.AppendLine($"校验过滤：{BuildValidationFilterSummary()}");
        sb.AppendLine(BuildSimulationLevelButtonText());
        sb.Append(BuildSimulationMonsterButtonText());
        return sb.ToString().TrimEnd();
    }

    private string BuildSimulationDetailText()
    {
        var sb = new StringBuilder();
        sb.AppendLine(BuildSimulationLevelButtonText());
        sb.AppendLine(BuildSimulationMonsterButtonText());
        sb.AppendLine();
        sb.Append(ConfigValidationViewFormatter.BuildSimulationStatus(_lastSimulationSummary));
        return sb.ToString().TrimEnd();
    }

    private void RefreshValidationPanelContent()
    {
        if (_validationStatusLabel == null || _validationContentLabel == null || _validationFilterInfoLabel == null || _validationScopeButton == null || _validationLevelScopeButton == null || _simulationLevelButton == null || _simulationMonsterButton == null || _simulationStatusLabel == null || _simulationContentLabel == null)
        {
            return;
        }

        _validationScopeButton.Text = $"范围：{BuildValidationScopeButtonLabel()}";
        _validationLevelScopeButton.Text = _validationOnlyActiveLevel ? "关卡：当前关卡" : "关卡：全部关卡";
        _simulationLevelButton.Text = BuildSimulationLevelButtonText();
        _simulationMonsterButton.Text = BuildSimulationMonsterButtonText();
        _simulationStatusLabel.Text = ConfigValidationViewFormatter.BuildSimulationStatus(_lastSimulationSummary);
        _validationFilterInfoLabel.Text = BuildValidationFilterInfoText();
        _simulationContentLabel.Text = BuildSimulationDetailText();

        if (_levelConfigLoader == null)
        {
            _validationStatusLabel.Text = "LevelConfigLoader 未加载，无法显示配置校验结果。";
            _validationContentLabel.Text = "请先确认配置已成功加载。";
            _simulationContentLabel.Text = "请先确认配置已成功加载。";
            return;
        }

        var filtered = GetFilteredValidationItems();
        int totalCount = _levelConfigLoader.GetValidationEntries().Count;
        string filterSummary = BuildValidationFilterSummary();

        _validationStatusLabel.Text = ConfigValidationViewFormatter.BuildTitle(filtered.Count, totalCount, filterSummary);
        _validationContentLabel.Text = BuildValidationOverviewText();
    }

    private void CycleValidationScope()
    {
        _validationScopeFilterIndex = (_validationScopeFilterIndex + 1) % ValidationScopeFilters.Length;
        RefreshValidationPanelContent();
    }

    private void ToggleValidationLevelScope()
    {
        _validationOnlyActiveLevel = !_validationOnlyActiveLevel;
        RefreshValidationPanelContent();
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
        RefreshValidationPanelContent();
    }

    private void CycleSimulationMonsterFilter()
    {
        if (_levelConfigLoader == null)
        {
            return;
        }

        string levelId = GetSimulationLevelId();
        var monsters = _levelConfigLoader.GetSpawnMonsterIds(levelId);
        if (monsters.Count == 0)
        {
            _simulationMonsterFilterId = "";
            RefreshValidationPanelContent();
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

        RefreshValidationPanelContent();
    }

    private void RunSimulationFromValidationPanel(int battleCount)
    {
        if (_levelConfigLoader == null)
        {
            _lastSimulationSummary = "loader unavailable";
            RefreshValidationPanelContent();
            return;
        }

        _lastSimulationSummary = _levelConfigLoader.RunBattleSimulationFiltered(
            battleCount,
            GetSimulationLevelId(),
            _simulationMonsterFilterId);
        RefreshValidationPanelContent();
    }

    private string BuildValidationFilterSummary()
    {
        string scope = BuildValidationScopeButtonLabel();
        string levelScope = _validationOnlyActiveLevel ? "active-level" : "all-levels";
        return $"{scope}, {levelScope}";
    }

    private string BuildValidationScopeButtonLabel()
    {
        string scope = ValidationScopeFilters[Mathf.Clamp(_validationScopeFilterIndex, 0, ValidationScopeFilters.Length - 1)];
        return scope switch
        {
            "all" => "全部",
            "config" => "全局",
            "level" => "关卡",
            "monster" => "怪物",
            "drop_table" => "掉落",
            _ => scope,
        };
    }

    private string BuildSimulationLevelButtonText()
    {
        if (_levelConfigLoader == null)
        {
            return "模拟关卡：不可用";
        }

        bool useActiveLevel = string.IsNullOrEmpty(_simulationLevelFilterId);
        string levelId = GetSimulationLevelId();
        string levelName = _levelConfigLoader.GetLevelName(levelId);
        return ConfigValidationViewFormatter.BuildSimulationLevelLabel(levelId, levelName, useActiveLevel);
    }

    private string BuildSimulationMonsterButtonText()
    {
        return ConfigValidationViewFormatter.BuildSimulationMonsterLabel(_simulationMonsterFilterId, string.IsNullOrEmpty(_simulationMonsterFilterId));
    }

    private string GetSimulationLevelId()
    {
        if (_levelConfigLoader == null)
        {
            return "";
        }

        return string.IsNullOrEmpty(_simulationLevelFilterId)
            ? _levelConfigLoader.ActiveLevelId
            : _simulationLevelFilterId;
    }

    private List<ConfigValidationViewFormatter.ConfigValidationItem> GetFilteredValidationItems()
    {
        var entries = _levelConfigLoader?.GetValidationEntries();
        if (entries == null)
        {
            return new List<ConfigValidationViewFormatter.ConfigValidationItem>();
        }

        List<ConfigValidationViewFormatter.ConfigValidationItem> items = new(entries.Count);
        foreach (var entry in entries)
        {
            items.Add(ConfigValidationViewFormatter.FromDictionary(entry));
        }

        string activeLevelId = _levelConfigLoader?.ActiveLevelId ?? "";
        string scope = ValidationScopeFilters[Mathf.Clamp(_validationScopeFilterIndex, 0, ValidationScopeFilters.Length - 1)];
        return ConfigValidationViewFormatter.FilterItems(items, scope, _validationOnlyActiveLevel, activeLevelId);
    }

    private void RefreshCoinLabel()
    {
        if (_resourceWalletState == null)
        {
            return;
        }

        SetSpiritStone(_resourceWalletState.SpiritStones);
    }

    private void SetActiveRightTab(string tabName)
    {
        if (!_rightTabContentMap.ContainsKey(tabName))
        {
            return;
        }

        ActiveRightTabName = tabName;
        _isShowingRightTab = tabName != "SettingsTab";
        SyncButtons("TopStrip/RightTabs", _rightTabContentMap.Keys, ActiveRightTabName);
        RefreshCurrentPageContent();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void BuildSettingsUi()
    {
        BuildCultivationUi();
        BuildEquipmentUi();
        BuildBackpackUi();
        BuildValidationUi();
        BuildBugFeedbackUi();

        _settingsNavRoot = new HBoxContainer();
        _settingsNavRoot.Name = "SettingsNavRoot";
        _settingsNavRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _settingsNavRoot.OffsetLeft = 20.0f;
        _settingsNavRoot.OffsetTop = 36.0f;
        _settingsNavRoot.OffsetRight = -20.0f;
        _settingsNavRoot.OffsetBottom = -330.0f;
        _settingsNavRoot.AddThemeConstantOverride("separation", 8);
        _leftPage.AddChild(_settingsNavRoot);

        _settingsSystemBtn = CreateSettingsSectionButton(UiText.SystemSection, "system");
        _settingsDisplayBtn = CreateSettingsSectionButton(UiText.DisplaySection, "display");
        _settingsProgressBtn = CreateSettingsSectionButton(UiText.ProgressSection, "progress");

        _settingsActionRoot = new VBoxContainer();
        _settingsActionRoot.Name = "SettingsActionRoot";
        _settingsActionRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _settingsActionRoot.OffsetLeft = 20.0f;
        _settingsActionRoot.OffsetTop = 286.0f;
        _settingsActionRoot.OffsetRight = -20.0f;
        _settingsActionRoot.OffsetBottom = -12.0f;
        _settingsActionRoot.AddThemeConstantOverride("separation", 8);
        _leftPage.AddChild(_settingsActionRoot);

        Button resetButton = new();
        resetButton.Text = UiText.ResetAndApply;
        resetButton.Pressed += ResetSettings;
        _settingsActionRoot.AddChild(resetButton);

        Button quitButton = new();
        quitButton.Text = UiText.Quit;
        quitButton.AddThemeColorOverride("font_color", new Color(0.78f, 0.31f, 0.31f));
        quitButton.Pressed += () => GetTree().Quit();
        _settingsActionRoot.AddChild(quitButton);

        _settingsSystemRoot = CreateSettingsSectionRoot("SettingsSystemRoot");
        _settingsDisplayRoot = CreateSettingsSectionRoot("SettingsDisplayRoot");
        _settingsProgressRoot = CreateSettingsSectionRoot("SettingsProgressRoot");

        BuildSystemSection(_settingsSystemRoot);
        BuildDisplaySection(_settingsDisplayRoot);
        BuildProgressSection(_settingsProgressRoot);
    }

    private void BuildCultivationUi()
    {
        _cultivationRoot = new VBoxContainer();
        _cultivationRoot.Name = "CultivationRoot";
        _cultivationRoot.Visible = false;
        _cultivationRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _cultivationRoot.OffsetLeft = 20.0f;
        _cultivationRoot.OffsetTop = 42.0f;
        _cultivationRoot.OffsetRight = -20.0f;
        _cultivationRoot.OffsetBottom = -18.0f;
        _cultivationRoot.AddThemeConstantOverride("separation", 10);
        _leftPage.AddChild(_cultivationRoot);

        _cultivationStatusLabel = new Label();
        _cultivationStatusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _cultivationRoot.AddChild(_cultivationStatusLabel);

        HBoxContainer actionRow = new();
        actionRow.AddThemeConstantOverride("separation", 8);
        _cultivationRoot.AddChild(actionRow);

        _cultivationBreakthroughButton = new Button();
        _cultivationBreakthroughButton.Text = UiText.BreakthroughButtonLabel;
        _cultivationBreakthroughButton.Pressed += OnCultivationBreakthroughPressed;
        actionRow.AddChild(_cultivationBreakthroughButton);

        _cultivationBossInsightButton = new Button();
        _cultivationBossInsightButton.Text = UiText.BossWeaknessInsightButton;
        _cultivationBossInsightButton.Pressed += OnBossWeaknessInsightPressed;
        actionRow.AddChild(_cultivationBossInsightButton);

        _cultivationAlchemyRecipeOption = new OptionButton();
        _cultivationAlchemyRecipeOption.ItemSelected += OnAlchemyRecipeSelected;
        actionRow.AddChild(_cultivationAlchemyRecipeOption);

        _cultivationAlchemyStartButton = new Button();
        _cultivationAlchemyStartButton.Text = "开始炼丹";
        _cultivationAlchemyStartButton.Pressed += OnAlchemyStartPressed;
        actionRow.AddChild(_cultivationAlchemyStartButton);

        HBoxContainer gatheringRow = new();
        gatheringRow.AddThemeConstantOverride("separation", 8);
        _cultivationRoot.AddChild(gatheringRow);

        _cultivationGardenOption = new OptionButton();
        _cultivationGardenOption.ItemSelected += OnGardenRecipeSelected;
        gatheringRow.AddChild(_cultivationGardenOption);

        _cultivationGardenStartButton = new Button();
        _cultivationGardenStartButton.Text = "开始灵田";
        _cultivationGardenStartButton.Pressed += OnGardenStartPressed;
        gatheringRow.AddChild(_cultivationGardenStartButton);

        _cultivationMiningOption = new OptionButton();
        _cultivationMiningOption.ItemSelected += OnMiningRecipeSelected;
        gatheringRow.AddChild(_cultivationMiningOption);

        _cultivationMiningStartButton = new Button();
        _cultivationMiningStartButton.Text = "开始矿脉";
        _cultivationMiningStartButton.Pressed += OnMiningStartPressed;
        gatheringRow.AddChild(_cultivationMiningStartButton);

        _cultivationFishingOption = new OptionButton();
        _cultivationFishingOption.ItemSelected += OnFishingRecipeSelected;
        gatheringRow.AddChild(_cultivationFishingOption);

        _cultivationFishingStartButton = new Button();
        _cultivationFishingStartButton.Text = "开始灵渔";
        _cultivationFishingStartButton.Pressed += OnFishingStartPressed;
        gatheringRow.AddChild(_cultivationFishingStartButton);

        HBoxContainer formationRow = new();
        formationRow.AddThemeConstantOverride("separation", 8);
        _cultivationRoot.AddChild(formationRow);

        _cultivationFormationOption = new OptionButton();
        _cultivationFormationOption.ItemSelected += OnFormationSelected;
        formationRow.AddChild(_cultivationFormationOption);

        _cultivationFormationActivateButton = new Button();
        _cultivationFormationActivateButton.Text = "激活阵法";
        _cultivationFormationActivateButton.Pressed += OnFormationActivatePressed;
        formationRow.AddChild(_cultivationFormationActivateButton);

        _cultivationFormationSecondaryOption = new OptionButton();
        _cultivationFormationSecondaryOption.ItemSelected += OnFormationSecondarySelected;
        formationRow.AddChild(_cultivationFormationSecondaryOption);

        _cultivationFormationSecondaryActivateButton = new Button();
        _cultivationFormationSecondaryActivateButton.Text = "激活副阵";
        _cultivationFormationSecondaryActivateButton.Pressed += OnFormationSecondaryActivatePressed;
        formationRow.AddChild(_cultivationFormationSecondaryActivateButton);

        _cultivationMasteryLabel = new RichTextLabel();
        _cultivationMasteryLabel.FitContent = true;
        _cultivationMasteryLabel.ScrollActive = false;
        _cultivationRoot.AddChild(_cultivationMasteryLabel);

        HBoxContainer masteryRow = new();
        masteryRow.AddThemeConstantOverride("separation", 8);
        _cultivationRoot.AddChild(masteryRow);
        foreach (string systemId in GetTrackedMasterySystems())
        {
            Button button = new();
            button.Pressed += () => OnMasteryUnlockPressed(systemId);
            masteryRow.AddChild(button);
            _cultivationMasteryButtons[systemId] = button;
        }

        _cultivationContentLabel = new RichTextLabel();
        _cultivationContentLabel.FitContent = false;
        _cultivationContentLabel.ScrollActive = true;
        _cultivationContentLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _cultivationRoot.AddChild(_cultivationContentLabel);
    }

    private void BuildEquipmentUi()
    {
        _equipmentRoot = new VBoxContainer();
        _equipmentRoot.Name = "EquipmentRoot";
        _equipmentRoot.Visible = false;
        _equipmentRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _equipmentRoot.OffsetLeft = 20.0f;
        _equipmentRoot.OffsetTop = 42.0f;
        _equipmentRoot.OffsetRight = -20.0f;
        _equipmentRoot.OffsetBottom = -18.0f;
        _equipmentRoot.AddThemeConstantOverride("separation", 10);
        _leftPage.AddChild(_equipmentRoot);

        HBoxContainer actionRow = new();
        actionRow.AddThemeConstantOverride("separation", 8);
        _equipmentRoot.AddChild(actionRow);

        Button equipWeaponButton = new();
        equipWeaponButton.Pressed += () => EquipFromBackpack(EquipmentSlotType.Weapon);
        equipWeaponButton.Text = UiText.BackpackEquipWeapon;
        actionRow.AddChild(equipWeaponButton);

        Button equipArmorButton = new();
        equipArmorButton.Pressed += () => EquipFromBackpack(EquipmentSlotType.Armor);
        equipArmorButton.Text = UiText.BackpackEquipArmor;
        actionRow.AddChild(equipArmorButton);

        Button equipAccessoryButton = new();
        equipAccessoryButton.Text = UiText.BackpackEquipAccessory;
        equipAccessoryButton.Pressed += () => EquipFromBackpack(EquipmentSlotType.Accessory);
        actionRow.AddChild(equipAccessoryButton);

        _equipmentSmithingTargetOption = new OptionButton();
        _equipmentSmithingTargetOption.ItemSelected += OnSmithingTargetSelected;
        actionRow.AddChild(_equipmentSmithingTargetOption);

        _equipmentSmithingStartButton = new Button();
        _equipmentSmithingStartButton.Text = "开始强化";
        _equipmentSmithingStartButton.Pressed += OnSmithingStartPressed;
        actionRow.AddChild(_equipmentSmithingStartButton);

        Label hint = new();
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        hint.Text = UiText.EquipmentPageHint;
        _equipmentRoot.AddChild(hint);

        _equipmentContentLabel = new RichTextLabel();
        _equipmentContentLabel.BbcodeEnabled = true;
        _equipmentContentLabel.FitContent = false;
        _equipmentContentLabel.ScrollActive = true;
        _equipmentContentLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _equipmentRoot.AddChild(_equipmentContentLabel);
    }

    private void BuildBackpackUi()
    {
        _backpackRoot = new VBoxContainer();
        _backpackRoot.Name = "BackpackRoot";
        _backpackRoot.Visible = false;
        _backpackRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _backpackRoot.OffsetLeft = 20.0f;
        _backpackRoot.OffsetTop = 42.0f;
        _backpackRoot.OffsetRight = -20.0f;
        _backpackRoot.OffsetBottom = -18.0f;
        _backpackRoot.AddThemeConstantOverride("separation", 10);
        _leftPage.AddChild(_backpackRoot);

        _backpackGrid = new BackpackGridController();
        _backpackGrid.Name = "BackpackGrid";
        _backpackGrid.SizeFlagsVertical = SizeFlags.ExpandFill;
        _backpackGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _backpackGrid.Initialize(_backpackState, _potionInventoryState);
        _backpackGrid.EquipmentCellClicked += OnBackpackGridCellClicked;
        _backpackRoot.AddChild(_backpackGrid);
    }

    private void BuildValidationUi()
    {
        _validationRoot = new VBoxContainer();
        _validationRoot.Name = "ValidationRoot";
        _validationRoot.Visible = false;
        _validationRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _validationRoot.OffsetLeft = 20.0f;
        _validationRoot.OffsetTop = 42.0f;
        _validationRoot.OffsetRight = -20.0f;
        _validationRoot.OffsetBottom = -18.0f;
        _validationRoot.AddThemeConstantOverride("separation", 8);
        _leftPage.AddChild(_validationRoot);

        ScrollContainer scroll = new();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _validationRoot.AddChild(scroll);

        VBoxContainer scrollInner = new();
        scrollInner.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scrollInner.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(scrollInner);

        _validationFilterPanel = CreateValidationSectionPanel(scrollInner, "筛选信息", out VBoxContainer filterContent);

        HBoxContainer filterRow = new();
        filterRow.AddThemeConstantOverride("separation", 8);
        filterContent.AddChild(filterRow);

        _validationScopeButton = new Button();
        _validationScopeButton.Pressed += CycleValidationScope;
        filterRow.AddChild(_validationScopeButton);

        _validationLevelScopeButton = new Button();
        _validationLevelScopeButton.Pressed += ToggleValidationLevelScope;
        filterRow.AddChild(_validationLevelScopeButton);

        _validationFilterInfoLabel = new Label();
        _validationFilterInfoLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _validationFilterInfoLabel.AddThemeFontSizeOverride("font_size", 12);
        filterContent.AddChild(_validationFilterInfoLabel);

        _validationResultPanel = CreateValidationSectionPanel(scrollInner, "校验结果", out VBoxContainer validationContent);

        _validationStatusLabel = new Label();
        _validationStatusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _validationStatusLabel.AddThemeFontSizeOverride("font_size", 12);
        validationContent.AddChild(_validationStatusLabel);

        _validationContentLabel = new RichTextLabel();
        _validationContentLabel.BbcodeEnabled = true;
        _validationContentLabel.FitContent = true;
        _validationContentLabel.ScrollActive = false;
        _validationContentLabel.AddThemeFontSizeOverride("normal_font_size", 12);
        _validationContentLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        validationContent.AddChild(_validationContentLabel);

        _simulationResultPanel = CreateValidationSectionPanel(scrollInner, "模拟结果", out VBoxContainer simulationContent);

        HBoxContainer simulationFilterRow = new();
        simulationFilterRow.AddThemeConstantOverride("separation", 8);
        simulationContent.AddChild(simulationFilterRow);

        _simulationLevelButton = new Button();
        _simulationLevelButton.Pressed += CycleSimulationLevelFilter;
        simulationFilterRow.AddChild(_simulationLevelButton);

        _simulationMonsterButton = new Button();
        _simulationMonsterButton.Pressed += CycleSimulationMonsterFilter;
        simulationFilterRow.AddChild(_simulationMonsterButton);

        HBoxContainer simulationRunRow = new();
        simulationRunRow.AddThemeConstantOverride("separation", 8);
        simulationContent.AddChild(simulationRunRow);

        Button sim200Button = new();
        sim200Button.Text = "模拟 200 次";
        sim200Button.Pressed += () => RunSimulationFromValidationPanel(200);
        simulationRunRow.AddChild(sim200Button);

        Button sim1000Button = new();
        sim1000Button.Text = "模拟 1000 次";
        sim1000Button.Pressed += () => RunSimulationFromValidationPanel(1000);
        simulationRunRow.AddChild(sim1000Button);

        _simulationStatusLabel = new Label();
        _simulationStatusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _simulationStatusLabel.AddThemeFontSizeOverride("font_size", 12);
        simulationContent.AddChild(_simulationStatusLabel);

        _simulationContentLabel = new RichTextLabel();
        _simulationContentLabel.BbcodeEnabled = true;
        _simulationContentLabel.FitContent = true;
        _simulationContentLabel.ScrollActive = false;
        _simulationContentLabel.AddThemeFontSizeOverride("normal_font_size", 12);
        _simulationContentLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        simulationContent.AddChild(_simulationContentLabel);
    }

    private Control CreateValidationSectionPanel(VBoxContainer parent, string title, out VBoxContainer wrapper)
    {
        PanelContainer panelContainer = new();
        panelContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panelContainer.AddThemeStyleboxOverride("panel", CreateValidationSectionStyle());
        parent.AddChild(panelContainer);

        wrapper = new VBoxContainer();
        wrapper.AddThemeConstantOverride("separation", 6);
        wrapper.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panelContainer.AddChild(wrapper);

        Label heading = new();
        heading.Text = title;
        heading.AddThemeColorOverride("font_color", new Color(0.36f, 0.24f, 0.16f, 1.0f));
        heading.AddThemeFontSizeOverride("font_size", 14);
        wrapper.AddChild(heading);

        return panelContainer;
    }

    private static StyleBoxFlat CreateValidationSectionStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.94f, 0.89f, 0.78f, 0.98f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.66f, 0.50f, 0.30f, 1.0f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
        };
    }

    private void BuildBugFeedbackUi()
    {
        _bugFeedbackRoot = new VBoxContainer();
        _bugFeedbackRoot.Name = "BugFeedbackRoot";
        _bugFeedbackRoot.Visible = false;
        _bugFeedbackRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _bugFeedbackRoot.OffsetLeft = 20.0f;
        _bugFeedbackRoot.OffsetTop = 42.0f;
        _bugFeedbackRoot.OffsetRight = -20.0f;
        _bugFeedbackRoot.OffsetBottom = -18.0f;
        _bugFeedbackRoot.AddThemeConstantOverride("separation", 10);
        _leftPage.AddChild(_bugFeedbackRoot);

        RichTextLabel hint = new();
        hint.FitContent = true;
        hint.ScrollActive = false;
        hint.Text = UiText.BugFeedbackHint;
        _bugFeedbackRoot.AddChild(hint);

        _bugFeedbackInput = new TextEdit();
        _bugFeedbackInput.CustomMinimumSize = new Vector2(0.0f, 180.0f);
        _bugFeedbackInput.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _bugFeedbackInput.PlaceholderText = UiText.BugFeedbackInputPlaceholder;
        _bugFeedbackRoot.AddChild(_bugFeedbackInput);

        HBoxContainer actionRow = new();
        actionRow.AddThemeConstantOverride("separation", 8);
        _bugFeedbackRoot.AddChild(actionRow);

        Button copyButton = new();
        copyButton.Text = UiText.CopyLogPath;
        copyButton.Pressed += CopyLogFolderPath;
        actionRow.AddChild(copyButton);

        Button exportButton = new();
        exportButton.Text = UiText.ExportFeedbackPack;
        exportButton.Pressed += ExportFeedbackPack;
        actionRow.AddChild(exportButton);

        Button openButton = new();
        openButton.Text = UiText.OpenDataFolder;
        openButton.Pressed += OpenLogFolder;
        actionRow.AddChild(openButton);

        _bugFeedbackStatusLabel = new Label();
        _bugFeedbackStatusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _bugFeedbackStatusLabel.Text = ProjectSettings.GlobalizePath("user://");
        _bugFeedbackRoot.AddChild(_bugFeedbackStatusLabel);
    }

    private Button CreateSettingsSectionButton(string title, string sectionId)
    {
        Button button = new();
        button.Text = title;
        button.ToggleMode = true;
        button.Pressed += () => ShowSettingsSection(sectionId);
        _settingsNavRoot.AddChild(button);
        return button;
    }

    private VBoxContainer CreateSettingsSectionRoot(string name)
    {
        VBoxContainer root = new();
        root.Name = name;
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 20.0f;
        root.OffsetTop = 74.0f;
        root.OffsetRight = -20.0f;
        root.OffsetBottom = -128.0f;
        root.AddThemeConstantOverride("separation", 6);
        _leftPage.AddChild(root);
        return root;
    }

    private void BuildSystemSection(VBoxContainer root)
    {
        _languageOption = AddOptionRow(root, UiText.Language, new[] { "简体中文", "English" });
        _keepOnTopCheck = AddCheckRow(root, UiText.KeepOnTop);
        _taskbarIconCheck = AddCheckRow(root, UiText.ReservedLabel(UiText.TaskbarIcon));
        HideSettingRow(_taskbarIconCheck);
        _vsyncCheck = AddCheckRow(root, UiText.Vsync);
        _fpsOption = AddOptionRow(root, UiText.MaxFps, new[] { "30", "60", "120", "不限" });

        _languageOption.ItemSelected += _ => OnLanguageChanged();
        _keepOnTopCheck.Toggled += value => OnSettingChanged("keep_on_top", value, applyNow: true);
        _taskbarIconCheck.Toggled += value => OnSettingChanged("taskbar_icon", value);
        _vsyncCheck.Toggled += value => OnSettingChanged("vsync", value, applyNow: true);
        _fpsOption.ItemSelected += _ => OnFpsChanged();
    }

    private void BuildDisplaySection(VBoxContainer root)
    {
        _resolutionOption = AddOptionRow(root, UiText.Resolution, new[] { "1280x720", "1600x900", "1920x1080", "2560x1440" });
        _showControlMarkerCheck = AddCheckRow(root, UiText.ShowControlMarkers);
        HideSettingRow(_showControlMarkerCheck);
        _showValidationPanelCheck = AddCheckRow(root, UiText.ShowValidationPanel);

        HBoxContainer logRow = new();
        logRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(logRow);
        Label logLabel = new();
        logLabel.Text = UiText.LogFolder;
        logLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        logRow.AddChild(logLabel);
        _openLogFolderButton = new();
        _openLogFolderButton.Text = UiText.Open;
        _openLogFolderButton.Pressed += OpenLogFolder;
        logRow.AddChild(_openLogFolderButton);

        _gameScaleOption = AddOptionRow(root, UiText.ExperimentalLabel(UiText.GameScale), new[] { "1.00", "1.10", "1.25", "1.33", "1.50" });
        _uiScaleOption = AddOptionRow(root, UiText.UiScale, new[] { "1.00", "1.10", "1.25", "1.33", "1.50" });

        _resolutionOption.ItemSelected += _ => OnResolutionChanged();
        _showControlMarkerCheck.Toggled += value => OnSettingChanged("show_control_markers", value);
        _showValidationPanelCheck.Toggled += value => OnSettingChanged("show_validation_panel", value);
        _gameScaleOption.ItemSelected += _ => OnGameScaleChanged();
        _uiScaleOption.ItemSelected += _ => OnUiScaleChanged();
    }

    private void BuildProgressSection(VBoxContainer root)
    {
        _autoSaveIntervalOption = AddOptionRow(root, UiText.AutoSaveInterval, new[] { "5 秒", "10 秒", "30 秒", "60 秒" });
        _cloudSyncCheck = AddCheckRow(root, UiText.ReservedLabel(UiText.CloudSync));
        _milestoneTipsCheck = AddCheckRow(root, UiText.ExperimentalLabel(UiText.MilestoneTips));
        HideSettingRow(_milestoneTipsCheck);
        _globalDebugOverlayCheck = AddCheckRow(root, UiText.GlobalDebugOverlay);

        RichTextLabel hint = new();
        hint.FitContent = true;
        hint.ScrollActive = false;
        hint.Text = UiText.DevHintCloudSync;
        root.AddChild(hint);

        _autoSaveIntervalOption.ItemSelected += _ => OnAutoSaveIntervalChanged();
        _cloudSyncCheck.Toggled += value => OnSettingChanged("cloud_sync", value);
        _milestoneTipsCheck.Toggled += value => OnSettingChanged("milestone_tips", value);
        _globalDebugOverlayCheck.Toggled += value => OnSettingChanged("global_debug_overlay", value);
    }

    private CheckButton AddCheckRow(VBoxContainer parent, string title)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        parent.AddChild(row);

        Label label = new();
        label.Text = title;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);

        CheckButton check = new();
        row.AddChild(check);
        return check;
    }

    private static void HideSettingRow(Control control)
    {
        if (control.GetParent() is Control row)
        {
            row.Visible = false;
        }
    }

    private OptionButton AddOptionRow(VBoxContainer parent, string title, string[] options)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        parent.AddChild(row);

        Label label = new();
        label.Text = title;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);

        OptionButton option = new();
        option.CustomMinimumSize = new Vector2(140.0f, 0.0f);
        foreach (string item in options)
        {
            option.AddItem(item);
        }
        row.AddChild(option);
        return option;
    }

    private void ShowSettingsSection(string sectionId)
    {
        _activeSettingsSection = sectionId;

        _settingsSystemRoot.Visible = sectionId == "system";
        _settingsDisplayRoot.Visible = sectionId == "display";
        _settingsProgressRoot.Visible = sectionId == "progress";

        _settingsSystemBtn.ButtonPressed = sectionId == "system";
        _settingsDisplayBtn.ButtonPressed = sectionId == "display";
        _settingsProgressBtn.ButtonPressed = sectionId == "progress";
    }

    private void UpdateSettingsUiVisibility()
    {
        bool isSettings = ActiveRightTabName == "SettingsTab";
        bool isBug = _isShowingRightTab && ActiveRightTabName == "BugTab";
        bool isCultivation = !_isShowingRightTab && ActiveLeftTabName == "CultivationTab";
        bool isEquipment = !_isShowingRightTab && ActiveLeftTabName == "EquipmentTab";
        bool isBackpack = !_isShowingRightTab && ActiveLeftTabName == "BackpackTab";
        bool isValidation = !_isShowingRightTab && ActiveLeftTabName == "ValidationTab";
        _settingsNavRoot.Visible = isSettings;
        _settingsActionRoot.Visible = isSettings;
        _settingsSystemRoot.Visible = isSettings && _activeSettingsSection == "system";
        _settingsDisplayRoot.Visible = isSettings && _activeSettingsSection == "display";
        _settingsProgressRoot.Visible = isSettings && _activeSettingsSection == "progress";
        _bugFeedbackRoot.Visible = isBug;
        _cultivationRoot.Visible = isCultivation;
        _equipmentRoot.Visible = isEquipment;
        _backpackRoot.Visible = isBackpack;
        _validationRoot.Visible = isValidation;
        _leftContentLabel.Visible = !isSettings && !isBug && !isCultivation && !isEquipment && !isBackpack && !isValidation;
        _rightPage.Visible = false;
    }

    private void UpdateSettingsControlsFromState()
    {
        _isApplyingSettingsUi = true;

        _languageOption.Selected = _settings["language"].AsString() == "en-US" ? 1 : 0;
        _keepOnTopCheck.ButtonPressed = _settings["keep_on_top"].AsBool();
        _taskbarIconCheck.ButtonPressed = _settings["taskbar_icon"].AsBool();
        _vsyncCheck.ButtonPressed = _settings["vsync"].AsBool();
        _showControlMarkerCheck.ButtonPressed = _settings["show_control_markers"].AsBool();
        _showValidationPanelCheck.ButtonPressed = _settings["show_validation_panel"].AsBool();
        _cloudSyncCheck.ButtonPressed = _settings["cloud_sync"].AsBool();
        _milestoneTipsCheck.ButtonPressed = _settings["milestone_tips"].AsBool();
        _globalDebugOverlayCheck.ButtonPressed = _settings["global_debug_overlay"].AsBool();

        _fpsOption.Selected = _settings["max_fps"].AsInt32() switch
        {
            30 => 0,
            60 => 1,
            120 => 2,
            _ => 3
        };

        SelectOptionByText(_resolutionOption, _settings["resolution"].AsString());
        SelectOptionByText(_gameScaleOption, _settings["game_scale"].AsDouble().ToString("0.00", CultureInfo.InvariantCulture));
        SelectOptionByText(_uiScaleOption, _settings["ui_scale"].AsDouble().ToString("0.00", CultureInfo.InvariantCulture));
        _autoSaveIntervalOption.Selected = _settings["auto_save_interval_sec"].AsInt32() switch
        {
            5 => 0,
            10 => 1,
            30 => 2,
            _ => 3
        };

        _isApplyingSettingsUi = false;
    }

    private static void SelectOptionByText(OptionButton option, string text)
    {
        for (int i = 0; i < option.ItemCount; i++)
        {
            if (option.GetItemText(i) == text)
            {
                option.Selected = i;
                return;
            }
        }
    }

    private void ResetSettings()
    {
        _settings["language"] = "zh-CN";
        _settings["keep_on_top"] = false;
        _settings["taskbar_icon"] = true;
        _settings["startup_animation"] = true;
        _settings["admin_mode"] = false;
        _settings["handwriting_support"] = false;
        _settings["vsync"] = true;
        _settings["max_fps"] = 60;
        _settings["resolution"] = "1600x900";
        _settings["show_control_markers"] = true;
        _settings["show_validation_panel"] = false;
        _settings["game_scale"] = 1.33;
        _settings["ui_scale"] = 1.0;
        _settings["auto_save_interval_sec"] = 10;
        _settings["cloud_sync"] = false;
        _settings["milestone_tips"] = true;
        _settings["global_debug_overlay"] = false;

        ApplySettingsRuntime();
        UpdateSettingsControlsFromState();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnLanguageChanged()
    {
        if (_isApplyingSettingsUi) return;
        _settings["language"] = _languageOption.Selected == 1 ? "en-US" : "zh-CN";
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnFpsChanged()
    {
        if (_isApplyingSettingsUi) return;
        int maxFps = _fpsOption.Selected switch
        {
            0 => 30,
            1 => 60,
            2 => 120,
            _ => 0
        };
        _settings["max_fps"] = maxFps;
        ApplySettingsRuntime();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnResolutionChanged()
    {
        if (_isApplyingSettingsUi) return;
        _settings["resolution"] = _resolutionOption.GetItemText(_resolutionOption.Selected);
        ApplyResolution();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnGameScaleChanged()
    {
        if (_isApplyingSettingsUi) return;
        _settings["game_scale"] = ParseOptionFloat(_gameScaleOption);
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnUiScaleChanged()
    {
        if (_isApplyingSettingsUi) return;
        _settings["ui_scale"] = ParseOptionFloat(_uiScaleOption);
        ApplyUiScale();
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OnAutoSaveIntervalChanged()
    {
        if (_isApplyingSettingsUi) return;
        int interval = _autoSaveIntervalOption.Selected switch
        {
            0 => 5,
            1 => 10,
            2 => 30,
            _ => 60
        };
        _settings["auto_save_interval_sec"] = interval;
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private double ParseOptionFloat(OptionButton option)
    {
        string text = option.GetItemText(option.Selected);
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            return value;
        }
        return 1.0;
    }

    private void OnSettingChanged(string key, bool value, bool applyNow = false)
    {
        if (_isApplyingSettingsUi) return;
        _settings[key] = value;
        if (applyNow)
        {
            ApplySettingsRuntime();
        }
        EmitSignal(SignalName.ActiveTabsChanged, ActiveLeftTabName, ActiveRightTabName);
    }

    private void OpenLogFolder()
    {
        string path = ProjectSettings.GlobalizePath("user://");
        OS.ShellOpen(path);
    }

    private void CopyLogFolderPath()
    {
        string path = ProjectSettings.GlobalizePath("user://");
        DisplayServer.ClipboardSet(path);
        _bugFeedbackStatusLabel.Text = UiText.BugFeedbackCopied;
    }

    private void ExportFeedbackPack()
    {
        string description = _bugFeedbackInput.Text.Trim();
        if (string.IsNullOrEmpty(description))
        {
            _bugFeedbackStatusLabel.Text = UiText.BugFeedbackEmptyWarning;
            return;
        }

        string feedbackDir = "user://feedback";
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(feedbackDir));
        long timestamp = (long)Time.GetUnixTimeFromSystem();
        string filePath = $"{feedbackDir}/feedback_{timestamp}.txt";

        using FileAccess? file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            _bugFeedbackStatusLabel.Text = UiText.BugFeedbackExportFailed;
            return;
        }

        StringBuilder sb = new();
        sb.AppendLine("# Xiuxian Demo Feedback");
        sb.AppendLine($"timestamp_unix={timestamp}");
        sb.AppendLine($"left_tab={ActiveLeftTabName}");
        sb.AppendLine($"right_tab={ActiveRightTabName}");
        sb.AppendLine($"data_dir={ProjectSettings.GlobalizePath("user://")}");
        sb.AppendLine($"save_file={ProjectSettings.GlobalizePath("user://save_state.cfg")}");
        sb.AppendLine();
        sb.AppendLine("[description]");
        sb.AppendLine(description);
        file.StoreString(sb.ToString());

        _bugFeedbackStatusLabel.Text = UiText.BugFeedbackExportedPrefix + ProjectSettings.GlobalizePath(filePath);
    }

    private void ApplySettingsRuntime()
    {
        bool keepOnTop = _settings["keep_on_top"].AsBool();
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop, keepOnTop);

        bool vsync = _settings["vsync"].AsBool();
        DisplayServer.WindowSetVsyncMode(vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

        Engine.MaxFps = _settings["max_fps"].AsInt32();
        ApplyResolution();
        ApplyUiScale();
    }

    private void ApplyResolution()
    {
        string resolution = _settings["resolution"].AsString();
        string[] parts = resolution.Split('x');
        if (parts.Length != 2)
        {
            return;
        }

        if (int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
        {
            DisplayServer.WindowSetSize(new Vector2I(width, height));
        }
    }

    private void ApplyUiScale()
    {
        double uiScale = _settings["ui_scale"].AsDouble();
        GetWindow().ContentScaleFactor = (float)uiScale;
    }

    private void RefreshCurrentPageContent()
    {
        if (ActiveRightTabName == "SettingsTab")
        {
            _leftTitleLabel.Text = UiText.SettingsTitle;
            UpdateSettingsUiVisibility();
            ShowSettingsSection(_activeSettingsSection);
            return;
        }

        UpdateSettingsUiVisibility();

        if (_isShowingRightTab)
        {
            _leftTitleLabel.Text = ButtonTextForTab("TopStrip/RightTabs", ActiveRightTabName);
            if (ActiveRightTabName == "BugTab")
            {
                UpdateSettingsUiVisibility();
                return;
            }
            AnimateContentSwap(_leftContentLabel, _leftTween, _rightTabContentMap[ActiveRightTabName], tween => _leftTween = tween, false);
            return;
        }

        _leftTitleLabel.Text = ButtonTextForTab("TopStrip/LeftTabs", ActiveLeftTabName);
        if (ActiveLeftTabName == "CultivationTab")
        {
            RefreshCultivationPanelContent();
            _cultivationContentLabel.Text = GetLeftTabContent(ActiveLeftTabName);
            UpdateSettingsUiVisibility();
            return;
        }
        if (ActiveLeftTabName == "EquipmentTab")
        {
            _equipmentContentLabel.Text = GetLeftTabContent(ActiveLeftTabName);
            UpdateSettingsUiVisibility();
            return;
        }
        if (ActiveLeftTabName == "BackpackTab")
        {
            RefreshBackpackGrid();
            UpdateSettingsUiVisibility();
            return;
        }
        if (ActiveLeftTabName == "ValidationTab")
        {
            RefreshValidationPanelContent();
            UpdateSettingsUiVisibility();
            return;
        }
        AnimateContentSwap(_leftContentLabel, _leftTween, GetLeftTabContent(ActiveLeftTabName), tween => _leftTween = tween, true);
    }

    private void CloseWindow()
    {
        if (GetParent() is SubmenuWindowController submenu)
        {
            submenu.ToggleVisible();
        }
    }

    private void ApplyStaticTexts()
    {
        SetButtonText("TopStrip/LeftTabs/CultivationTab", UiText.LeftTabCultivation);
        SetButtonText("TopStrip/LeftTabs/BattleLogTab", UiText.LeftTabBattleLog);
        SetButtonText("TopStrip/LeftTabs/EquipmentTab", UiText.LeftTabEquipment);
        SetButtonText("TopStrip/LeftTabs/BackpackTab", UiText.LeftTabBackpack);
        SetButtonText("TopStrip/LeftTabs/StatsTab", UiText.LeftTabStats);
        SetButtonText("TopStrip/LeftTabs/ValidationTab", UiText.LeftTabValidation);
        SetButtonText("TopStrip/RightTabs/BugTab", UiText.RightTabBug);
        SetButtonText("TopStrip/RightTabs/SettingsTab", UiText.RightTabSettings);
        _closeButton.Text = "X";
    }

    private void SetButtonText(string nodePath, string text)
    {
        if (!HasNode(nodePath))
        {
            return;
        }

        GetNode<Button>(nodePath).Text = text;
    }

    private void BindButtons(IEnumerable<string> tabKeys, string groupPath, System.Action<string> setter)
    {
        foreach (string tabName in tabKeys)
        {
            if (!HasNode($"{groupPath}/{tabName}"))
            {
                continue;
            }

            Button button = GetNode<Button>($"{groupPath}/{tabName}");
            button.Pressed += () => setter(tabName);
        }
    }

    private void SyncButtons(string groupPath, IEnumerable<string> tabKeys, string activeTab)
    {
        foreach (string key in tabKeys)
        {
            if (!HasNode($"{groupPath}/{key}"))
            {
                continue;
            }

            Button button = GetNode<Button>($"{groupPath}/{key}");
            button.ButtonPressed = key == activeTab;
        }
    }

    private string ButtonTextForTab(string groupPath, string tabName)
    {
        if (!HasNode($"{groupPath}/{tabName}"))
        {
            return tabName;
        }

        return GetNode<Button>($"{groupPath}/{tabName}").Text;
    }

    private void AnimateContentSwap(
        RichTextLabel label,
        Tween? activeTween,
        string nextText,
        System.Action<Tween?> storeTween,
        bool isLeftPage)
    {
        activeTween?.Kill();

        Vector2 basePos = label.Position;
        float offset = isLeftPage ? 10.0f : -10.0f;

        Tween outTween = CreateTween();
        outTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        outTween.TweenProperty(label, "modulate", new Color(1, 1, 1, 0.18f), 0.08f);
        outTween.Parallel().TweenProperty(label, "position:x", basePos.X + offset, 0.08f);
        outTween.Finished += () =>
        {
            label.Text = nextText;
            label.Position = basePos - new Vector2(offset, 0.0f);

            Tween inTween = CreateTween();
            inTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            inTween.TweenProperty(label, "modulate", Colors.White, 0.12f);
            inTween.Parallel().TweenProperty(label, "position", basePos, 0.12f);
            storeTween(inTween);
        };

        storeTween(outTween);
    }
}
