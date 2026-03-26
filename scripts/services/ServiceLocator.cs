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
        public ResourceWalletState? ResourceWalletState { get; private set; }
        public PlayerProgressState? PlayerProgressState { get; private set; }
        public PlayerActionState? PlayerActionState { get; private set; }
        public EquippedItemsState? EquippedItemsState { get; private set; }
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
            ResourceWalletState = GetNodeOrNull<ResourceWalletState>("/root/ResourceWalletState");
            PlayerProgressState = GetNodeOrNull<PlayerProgressState>("/root/PlayerProgressState");
            PlayerActionState = GetNodeOrNull<PlayerActionState>("/root/PlayerActionState");
            EquippedItemsState = GetNodeOrNull<EquippedItemsState>("/root/EquippedItemsState");
            LevelConfigLoader = GetNodeOrNull<LevelConfigLoader>("/root/LevelConfigLoader");
            ActivityConversionService = GetNodeOrNull<ActivityConversionService>("/root/ActivityConversionService");
            CloudSaveSyncService = GetNodeOrNull<CloudSaveSyncService>("/root/CloudSaveSyncService");
        }
    }
}
