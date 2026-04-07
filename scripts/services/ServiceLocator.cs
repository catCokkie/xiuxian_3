using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class ServiceLocator : Node
    {
        public static ServiceLocator? Instance { get; private set; }

        public InputActivityState? InputActivityState { get; private set; }
        public InputHookService? InputHookService { get; private set; }
        public InputPauseShortcut? InputPauseShortcut { get; private set; }
        public BackpackState? BackpackState { get; private set; }
        public AlchemyState? AlchemyState { get; private set; }
        public PotionInventoryState? PotionInventoryState { get; private set; }
        public SmithingState? SmithingState { get; private set; }
        public GardenState? GardenState { get; private set; }
        public MiningState? MiningState { get; private set; }
        public FishingState? FishingState { get; private set; }
        public RecipeProgressState? TalismanState { get; private set; }
        public RecipeProgressState? CookingState { get; private set; }
        public FormationState? FormationState { get; private set; }
        public RecipeProgressState? BodyCultivationState { get; private set; }
        public ResourceWalletState? ResourceWalletState { get; private set; }
        public PlayerStatsState? PlayerStatsState { get; private set; }
        public PlayerProgressState? PlayerProgressState { get; private set; }
        public CultivationRhythmState? CultivationRhythmState { get; private set; }
        public ShopState? ShopState { get; private set; }
        public PlayerActionState? PlayerActionState { get; private set; }
        public EquippedItemsState? EquippedItemsState { get; private set; }
        public SubsystemMasteryState? SubsystemMasteryState { get; private set; }
        public LevelConfigLoader? LevelConfigLoader { get; private set; }
        public ActivityConversionService? ActivityConversionService { get; private set; }
        public CloudSaveSyncService? CloudSaveSyncService { get; private set; }

        public override void _EnterTree()
        {
            Instance = this;
        }

        public override void _Ready()
        {
            Refresh();
        }

        public override void _ExitTree()
        {
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        public void Refresh()
        {
            InputActivityState = GetNodeOrNull<InputActivityState>("/root/InputActivityState");
            InputHookService = GetNodeOrNull<InputHookService>("/root/InputHookService");
            InputPauseShortcut = GetNodeOrNull<InputPauseShortcut>("/root/InputPauseShortcut");
            BackpackState = GetNodeOrNull<BackpackState>("/root/BackpackState");
            AlchemyState = GetNodeOrNull<AlchemyState>("/root/AlchemyState");
            PotionInventoryState = GetNodeOrNull<PotionInventoryState>("/root/PotionInventoryState");
            SmithingState = GetNodeOrNull<SmithingState>("/root/SmithingState");
            GardenState = GetNodeOrNull<GardenState>("/root/GardenState");
            MiningState = GetNodeOrNull<MiningState>("/root/MiningState");
            FishingState = GetNodeOrNull<FishingState>("/root/FishingState");
            TalismanState = GetNodeOrNull<RecipeProgressState>("/root/TalismanState");
            CookingState = GetNodeOrNull<RecipeProgressState>("/root/CookingState");
            FormationState = GetNodeOrNull<FormationState>("/root/FormationState");
            BodyCultivationState = GetNodeOrNull<RecipeProgressState>("/root/BodyCultivationState");
            ResourceWalletState = GetNodeOrNull<ResourceWalletState>("/root/ResourceWalletState");
            PlayerStatsState = GetNodeOrNull<PlayerStatsState>("/root/PlayerStatsState");
            PlayerProgressState = GetNodeOrNull<PlayerProgressState>("/root/PlayerProgressState");
            CultivationRhythmState = GetNodeOrNull<CultivationRhythmState>("/root/CultivationRhythmState");
            ShopState = GetNodeOrNull<ShopState>("/root/ShopState");
            PlayerActionState = GetNodeOrNull<PlayerActionState>("/root/PlayerActionState");
            EquippedItemsState = GetNodeOrNull<EquippedItemsState>("/root/EquippedItemsState");
            SubsystemMasteryState = GetNodeOrNull<SubsystemMasteryState>("/root/SubsystemMasteryState");
            LevelConfigLoader = GetNodeOrNull<LevelConfigLoader>("/root/LevelConfigLoader");
            ActivityConversionService = GetNodeOrNull<ActivityConversionService>("/root/ActivityConversionService");
            CloudSaveSyncService = GetNodeOrNull<CloudSaveSyncService>("/root/CloudSaveSyncService");
        }
    }
}
