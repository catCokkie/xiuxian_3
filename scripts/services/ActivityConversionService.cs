using Godot;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Converts AP to resources immediately for each accepted activity batch.
    /// </summary>
    public partial class ActivityConversionService : Node
    {
        [Signal]
        public delegate void SettlementAppliedEventHandler(
            double apFinal,
            double lingqiGain,
            double insightGain,
            double realmExpGain,
            double activeSeconds);

        [Export] public NodePath ActivityStatePath = "/root/InputActivityState";
        [Export] public NodePath WalletStatePath = "/root/ResourceWalletState";
        [Export] public NodePath ProgressStatePath = "/root/PlayerProgressState";
        [Export] public NodePath RhythmStatePath = "/root/CultivationRhythmState";
        [Export] public NodePath ActionStatePath = "/root/PlayerActionState";

        [Export] public double LingqiFactor = GameBalanceConstants.ResourceConversion.LingqiFactor;
        [Export] public double InsightFactor = GameBalanceConstants.ResourceConversion.InsightFactor;
        [Export] public double BaseExpPerInput = GameBalanceConstants.ResourceConversion.BaseExpPerInput;
        [Export] public bool CultivationInputExpEnabled = true;
        [Export] public double CultivationExpPerInput = GameBalanceConstants.ResourceConversion.CultivationExpPerInput;

        private InputActivityState _activityState = null!;
        private ResourceWalletState _walletState = null!;
        private PlayerProgressState _progressState = null!;
        private CultivationRhythmState? _rhythmState;
        private PlayerActionState _actionState = null!;
        // Process-local attribution counters; never restored from player saves.
        public double SessionInputExperience { get; private set; }
        public double SessionOnlineBasicGrowthExperience { get; private set; }
        public double CurrentInputExperienceRate => CultivationInputExpEnabled && _actionState?.ActionId == PlayerActionState.ActionCultivation
            ? CultivationExpPerInput : BaseExpPerInput;

        public override void _Ready()
        {
            _automaticGrowthEnabled = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--smoke-test") < 0;
            _activityState = GetNodeOrNull<InputActivityState>(ActivityStatePath);
            _walletState = GetNodeOrNull<ResourceWalletState>(WalletStatePath);
            _progressState = GetNodeOrNull<PlayerProgressState>(ProgressStatePath);
            _rhythmState = GetNodeOrNull<CultivationRhythmState>(RhythmStatePath);
            _actionState = GetNodeOrNull<PlayerActionState>(ActionStatePath);

            if (_activityState == null || _walletState == null || _progressState == null)
            {
                GD.PushWarning("ActivityConversionService: missing required autoload state node(s).");
                return;
            }

            _activityState.ActivityTick += OnActivityTick;

        }

        public override void _ExitTree()
        {
            if (_activityState != null)
            {
                _activityState.ActivityTick -= OnActivityTick;

            }
        }

        public override void _Process(double delta)
        {
            AdvanceBasicGrowthClock();
            if (_walletState == null || _activityState == null || _rhythmState?.InRest != true
                || _activityState.SecondsSinceLastInput <= 5.0) return;
            // Rest is real time recovery and does not create input experience or an active cycle.
            _walletState.AddLingqi(System.Math.Max(0, delta) * GameBalanceConstants.ResourceConversion.PassiveLingqiPerMinute / 60.0 * _rhythmState.RestLingqiMultiplier);
        }

        private void OnActivityTick(double apThisSecond, double apFinal, double activeSecondsThisBatch)
        {
            if (!PlayerActionCapabilityRules.HasCapability(_actionState, PlayerActionCapability.ConsumesApSettlement))
            {
                return;
            }

            ApplySettlement(apFinal, activeSecondsThisBatch);
        }

        private void ApplySettlement(double apFinal, double activeSeconds)
        {
            if (activeSeconds <= 0.0 && apFinal <= 0.0 && _activityState.InputEventsThisSecond <= 0) return;
            activeSeconds = System.Math.Max(0, activeSeconds);
            _progressState.AddRealmActiveSeconds(activeSeconds);
            var services = ServiceLocator.Instance;
            var result = ResourceConversionRules.Calculate(
                _actionState?.ActionId ?? PlayerActionState.ActionDungeon,
                apFinal, _activityState.InputEventsThisSecond, activeSeconds,
                _progressState.GetRealmMultiplier(),
                services?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeCultivation) ?? 1,
                ActivityEffectRules.CollectFormationLingqiRate(
                    services?.FormationState?.ActivePrimaryId ?? string.Empty,
                    services?.FormationState?.ActiveSecondaryId ?? string.Empty,
                    services?.BackpackState?.GetItemEntries() ?? new System.Collections.Generic.Dictionary<string, int>(),
                    services?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeFormation) ?? 1),
                _rhythmState?.RestLingqiMultiplier ?? 1.0, CultivationInputExpEnabled,
                LingqiFactor, InsightFactor, BaseExpPerInput, CultivationExpPerInput);
            double lingqiGain = result.Lingqi;
            double insightGain = result.Insight;
            var meal = services?.CookingState?.ActiveMeal ?? default;
            lingqiGain *= 1 + meal.LingqiRate;
            insightGain *= 1 + meal.InsightRate;
            double realmExpGain = result.RealmExp;
            _walletState.AddLingqi(lingqiGain);
            _walletState.AddInsight(insightGain);
            _progressState.AddRealmExp(realmExpGain);
            SessionInputExperience += realmExpGain;

            EmitSignal(SignalName.SettlementApplied, apFinal, lingqiGain, insightGain, realmExpGain, activeSeconds);
            services?.CookingState?.AdvanceMeal(activeSeconds);
        }
    }
}
