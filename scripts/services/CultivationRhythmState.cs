using Godot;
using System;

namespace Xiuxian.Scripts.Services
{
    public partial class CultivationRhythmState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void RhythmChangedEventHandler();

        [Signal]
        public delegate void RhythmSummaryReadyEventHandler(string title, string rewardSummary, string suggestion, bool requestAttention);

        [Export] public NodePath ActivityConversionServicePath = "/root/ActivityConversionService";
        [Export] public NodePath ResourceWalletStatePath = "/root/ResourceWalletState";
        [Export] public NodePath PlayerProgressStatePath = "/root/PlayerProgressState";

        private ActivityConversionService? _activityConversionService;
        private ResourceWalletState? _resourceWalletState;
        private PlayerProgressState? _playerProgressState;
        private readonly Random _random = new();

        public bool Enabled { get; private set; } = true;
        public string Strength { get; private set; } = CultivationRhythmRules.StrengthWeak;
        public int CycleMinutes { get; private set; } = CultivationRhythmRules.DefaultCycleMinutes;
        public double CurrentCycleActiveSeconds { get; private set; }
        public int SmallCycleCount { get; private set; }
        public int TotalSmallCycles { get; private set; }
        public int TotalGrandCycles { get; private set; }
        public double RestRemainingSeconds { get; private set; }
        public bool IsGrandRest { get; private set; }
        public int TotalRestCount { get; private set; }
        public int TotalMeditationInsights { get; private set; }

        public bool InRest => RestRemainingSeconds > 0.0;
        public double RestLingqiMultiplier => Enabled && InRest ? 1.0 + CultivationRhythmRules.RestLingqiBonusRate : 1.0;

        public override void _Ready()
        {
            CallDeferred(nameof(ConnectServices));
        }

        public override void _ExitTree()
        {
            if (_activityConversionService != null)
            {
                _activityConversionService.SettlementApplied -= OnSettlementApplied;
            }
        }

        public override void _Process(double delta)
        {
            if (!InRest)
            {
                return;
            }

            double next = Math.Max(0.0, RestRemainingSeconds - delta);
            if (Math.Abs(next - RestRemainingSeconds) < 0.001)
            {
                return;
            }

            RestRemainingSeconds = next;
            if (!InRest)
            {
                IsGrandRest = false;
            }

            EmitSignal(SignalName.RhythmChanged);
        }

        public void SetEnabled(bool enabled)
        {
            if (Enabled == enabled)
            {
                return;
            }

            Enabled = enabled;
            if (!Enabled)
            {
                RestRemainingSeconds = 0.0;
                IsGrandRest = false;
            }

            EmitSignal(SignalName.RhythmChanged);
        }

        public void SetStrength(string strength)
        {
            string normalized = CultivationRhythmRules.NormalizeStrength(strength);
            if (Strength == normalized)
            {
                return;
            }

            Strength = normalized;
            EmitSignal(SignalName.RhythmChanged);
        }

        public void SetCycleMinutes(int cycleMinutes)
        {
            int normalized = CultivationRhythmRules.NormalizeCycleMinutes(cycleMinutes);
            if (CycleMinutes == normalized)
            {
                return;
            }

            CycleMinutes = normalized;
            CurrentCycleActiveSeconds = Math.Min(CurrentCycleActiveSeconds, CultivationRhythmRules.GetCycleDurationSeconds(CycleMinutes));
            EmitSignal(SignalName.RhythmChanged);
        }

        public void ApplyOfflineRealTime(double offlineSeconds)
        {
            if (offlineSeconds <= 0.0 || !InRest)
            {
                return;
            }

            RestRemainingSeconds = Math.Max(0.0, RestRemainingSeconds - offlineSeconds);
            if (!InRest)
            {
                IsGrandRest = false;
            }

            EmitSignal(SignalName.RhythmChanged);
        }

        public string BuildStatusText()
        {
            if (!Enabled)
            {
                return "周天关闭";
            }

            if (InRest)
            {
                return IsGrandRest
                    ? $"入定中 {FormatRemaining(RestRemainingSeconds)}"
                    : $"调息中 {FormatRemaining(RestRemainingSeconds)}";
            }

            double requiredSeconds = CultivationRhythmRules.GetCycleDurationSeconds(CycleMinutes);
            double percent = requiredSeconds > 0.0 ? CurrentCycleActiveSeconds / requiredSeconds * 100.0 : 0.0;
            return $"小周天 {CurrentCycleActiveSeconds / 60.0:0}/{CycleMinutes} 分 ({percent:0}%)";
        }

        public string BuildStatsSummary()
        {
            return $"小周天 {TotalSmallCycles} 次｜大周天 {TotalGrandCycles} 次｜调息 {TotalRestCount} 次｜入定领悟 {TotalMeditationInsights} 次";
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            CultivationRhythmPersistenceRules.RhythmSnapshot snapshot = new(
                Enabled,
                Strength,
                CycleMinutes,
                CurrentCycleActiveSeconds,
                SmallCycleCount,
                TotalSmallCycles,
                TotalGrandCycles,
                RestRemainingSeconds,
                IsGrandRest,
                TotalRestCount,
                TotalMeditationInsights);
            return SaveValueConversionRules.ToVariantDictionary(CultivationRhythmPersistenceRules.ToPlainDictionary(snapshot));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            CultivationRhythmPersistenceRules.RhythmSnapshot snapshot = CultivationRhythmPersistenceRules.FromPlainDictionary(
                SaveValueConversionRules.ToPlainDictionary(data));
            Enabled = snapshot.Enabled;
            Strength = snapshot.Strength;
            CycleMinutes = snapshot.CycleMinutes;
            CurrentCycleActiveSeconds = snapshot.CurrentCycleActiveSeconds;
            SmallCycleCount = snapshot.SmallCycleCount;
            TotalSmallCycles = snapshot.TotalSmallCycles;
            TotalGrandCycles = snapshot.TotalGrandCycles;
            RestRemainingSeconds = snapshot.RestRemainingSeconds;
            IsGrandRest = snapshot.IsGrandRest;
            TotalRestCount = snapshot.TotalRestCount;
            TotalMeditationInsights = snapshot.TotalMeditationInsights;
            EmitSignal(SignalName.RhythmChanged);
        }

        private void ConnectServices()
        {
            _activityConversionService = GetNodeOrNull<ActivityConversionService>(ActivityConversionServicePath);
            _resourceWalletState = GetNodeOrNull<ResourceWalletState>(ResourceWalletStatePath);
            _playerProgressState = GetNodeOrNull<PlayerProgressState>(PlayerProgressStatePath);

            if (_activityConversionService != null)
            {
                _activityConversionService.SettlementApplied += OnSettlementApplied;
            }
        }

        private void OnSettlementApplied(double apFinal10s, double lingqiGain, double insightGain, double realmExpGain)
        {
            if (!Enabled || apFinal10s <= 0.0)
            {
                return;
            }

            if (InRest)
            {
                RestRemainingSeconds = 0.0;
                IsGrandRest = false;
            }

            CurrentCycleActiveSeconds += _activityConversionService?.SettlementIntervalSeconds ?? 10.0;
            double requiredSeconds = CultivationRhythmRules.GetCycleDurationSeconds(CycleMinutes);

            while (CurrentCycleActiveSeconds >= requiredSeconds)
            {
                CurrentCycleActiveSeconds -= requiredSeconds;
                CompleteSmallCycle(lingqiGain, insightGain, realmExpGain);
            }

            EmitSignal(SignalName.RhythmChanged);
        }

        private void CompleteSmallCycle(double lingqiGain, double insightGain, double realmExpGain)
        {
            TotalSmallCycles++;
            SmallCycleCount++;
            TotalRestCount++;

            int insightReward = CultivationRhythmRules.GetSmallCycleInsightReward(_random.Next());
            _resourceWalletState?.AddInsight(insightReward);

            bool isGrandCycle = SmallCycleCount >= CultivationRhythmRules.GrandCycleSmallCycleCount;
            int spiritStoneReward = 0;
            string meditationReward = string.Empty;
            if (isGrandCycle)
            {
                SmallCycleCount = 0;
                TotalGrandCycles++;
                spiritStoneReward = CultivationRhythmRules.GetGrandCycleSpiritStoneReward(_random.Next());
                _resourceWalletState?.AddSpiritStones(spiritStoneReward);

                if (CultivationRhythmRules.ShouldGrantMeditationBonus(_random.Next()))
                {
                    string bonusType = CultivationRhythmRules.GetMeditationBonusType(_random.Next());
                    if (_playerProgressState != null
                        && _playerProgressState.TryApplyZhouTianMeditationBonus(
                            bonusType,
                            CultivationRhythmRules.MeditationBonusRate,
                            out string rewardText))
                    {
                        TotalMeditationInsights++;
                        meditationReward = rewardText;
                    }
                }
            }

            RestRemainingSeconds = CultivationRhythmRules.RestDurationSeconds;
            IsGrandRest = isGrandCycle;

            if (Strength != CultivationRhythmRules.StrengthNone)
            {
                string title = isGrandCycle ? $"大周天圆满（累计 {TotalGrandCycles} 次）" : $"小周天·第 {SmallCycleCount} 轮";
                string rewardSummary = $"+{insightReward} 悟性";
                if (spiritStoneReward > 0)
                {
                    rewardSummary += $"｜+{spiritStoneReward} 灵石";
                }
                if (!string.IsNullOrEmpty(meditationReward))
                {
                    rewardSummary += $"｜{meditationReward}";
                }

                string suggestion = isGrandCycle
                    ? "四周天圆满，可入定休憩"
                    : "运功一周天，建议起身调息片刻";
                EmitSignal(SignalName.RhythmSummaryReady, title, rewardSummary, suggestion, Strength == CultivationRhythmRules.StrengthStrong);
            }
        }

        private static string FormatRemaining(double seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt((float)seconds));
            int minutes = total / 60;
            int remainSeconds = total % 60;
            return $"{minutes:00}:{remainSeconds:00}";
        }
    }
}
