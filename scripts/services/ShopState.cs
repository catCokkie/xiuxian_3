using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public partial class ShopState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void ShopChangedEventHandler();

        [Signal]
        public delegate void ShopNoticeReadyEventHandler(string title, string detail);

        [Export] public NodePath ActivityConversionServicePath = "/root/ActivityConversionService";
        [Export] public NodePath ResourceWalletStatePath = "/root/ResourceWalletState";
        [Export] public NodePath BackpackStatePath = "/root/BackpackState";
        [Export] public NodePath PotionInventoryStatePath = "/root/PotionInventoryState";
        [Export] public NodePath GardenStatePath = "/root/GardenState";
        [Export] public NodePath MiningStatePath = "/root/MiningState";

        private ActivityConversionService? _activityConversionService;
        private ResourceWalletState? _resourceWalletState;
        private BackpackState? _backpackState;
        private PotionInventoryState? _potionInventoryState;
        private GardenState? _gardenState;
        private MiningState? _miningState;
        private readonly Dictionary<string, int> _lifetimePurchases = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _dailyPurchases = new(StringComparer.Ordinal);

        public string DailyResetDate { get; private set; } = string.Empty;
        public double ActiveDoubleYieldSeconds { get; private set; }

        public double DoubleYieldMultiplier => ActiveDoubleYieldSeconds > 0.0 ? 2.0 : 1.0;
        public int GardenPlotUnlockCount => CountPurchasedGardenPlots();
        public int BackpackExpansionCount => GetTotalPurchased(
            ShopRules.ItemBackpackExpand1,
            ShopRules.ItemBackpackExpand2,
            ShopRules.ItemBackpackExpand3);
        public bool HasAutoHarvest => GetLifetimePurchaseCount(ShopRules.ItemAutoHarvest) > 0;
        public bool HasBreakthroughPill => GetLifetimePurchaseCount(ShopRules.ItemBreakthroughPill) > 0;
        public double BreakthroughExpReductionRate => HasBreakthroughPill ? 0.10 : 0.0;

        public override void _Ready()
        {
            ApplyDailyResetIfNeeded();
            CallDeferred(nameof(ConnectServices));
        }

        public override void _ExitTree()
        {
            if (_activityConversionService != null)
            {
                _activityConversionService.SettlementApplied -= OnSettlementApplied;
            }
        }

        public bool CanPurchase(string itemId, out string reason)
        {
            ApplyDailyResetIfNeeded();
            reason = string.Empty;

            if (!ShopRules.TryGetItem(itemId, out ShopRules.ShopItemDefinition item))
            {
                reason = "商品不存在";
                return false;
            }

            int realmLevel = ServiceLocator.Instance?.PlayerProgressState?.RealmLevel ?? 1;
            if (realmLevel < item.UnlockRealmLevel)
            {
                reason = $"需炼气 {item.UnlockRealmLevel} 层";
                return false;
            }

            if (!string.IsNullOrEmpty(item.RequiredMasterySystemId))
            {
                int masteryLevel = ServiceLocator.Instance?.SubsystemMasteryState?.GetLevel(item.RequiredMasterySystemId) ?? 1;
                if (masteryLevel < item.RequiredMasteryLevel)
                {
                    reason = $"需{UiText.MasterySystemName(item.RequiredMasterySystemId)}精通 Lv{item.RequiredMasteryLevel}";
                    return false;
                }
            }

            if (item.LifetimeLimit > 0 && GetLifetimePurchaseCount(itemId) >= item.LifetimeLimit)
            {
                reason = "已购";
                return false;
            }

            if (item.DailyLimit > 0 && GetDailyPurchaseCount(itemId) >= item.DailyLimit)
            {
                reason = $"今日已购 {GetDailyPurchaseCount(itemId)}/{item.DailyLimit}";
                return false;
            }

            int spiritStones = _resourceWalletState?.SpiritStones ?? 0;
            if (spiritStones < item.Price)
            {
                reason = "灵石不足";
                return false;
            }

            if (item.GrantKind == ShopRules.GrantPotion && _potionInventoryState == null)
            {
                reason = "丹药背包未加载";
                return false;
            }

            if (item.GrantKind == ShopRules.GrantBackpackItem && _backpackState == null)
            {
                reason = "背包未加载";
                return false;
            }

            return true;
        }

        public bool TryPurchase(string itemId, out string summary)
        {
            summary = string.Empty;
            if (!CanPurchase(itemId, out _)
                || !ShopRules.TryGetItem(itemId, out ShopRules.ShopItemDefinition item)
                || _resourceWalletState == null
                || !_resourceWalletState.SpendSpiritStones(item.Price))
            {
                return false;
            }

            if (!ApplyGrant(item, out string grantSummary))
            {
                return false;
            }

            _lifetimePurchases[item.ItemId] = GetLifetimePurchaseCount(item.ItemId) + 1;
            if (item.DailyLimit > 0)
            {
                _dailyPurchases[item.ItemId] = GetDailyPurchaseCount(item.ItemId) + 1;
            }

            summary = string.IsNullOrEmpty(grantSummary) ? item.DisplayName : $"{item.DisplayName}｜{grantSummary}";
            EmitSignal(SignalName.ShopNoticeReady, $"购入 {item.DisplayName}", $"花费 {item.Price} 灵石");
            EmitSignal(SignalName.ShopChanged);
            return true;
        }

        public bool TryExchangePageFragments(out string rewardSummary)
        {
            rewardSummary = string.Empty;
            if (_backpackState == null || _backpackState.GetItemCount("page_fragment") < 10)
            {
                return false;
            }

            if (!_backpackState.RemoveItem("page_fragment", 10))
            {
                return false;
            }

            int exchangeIndex = GetLifetimePurchaseCount("shop_page_exchange") + 1;
            EquipmentStatProfile reward = ShopRewardRules.BuildPageExchangeReward(exchangeIndex);
            _backpackState.AddEquipment(reward);
            _lifetimePurchases["shop_page_exchange"] = exchangeIndex;
            rewardSummary = $"集齐 10 页，兑换 {reward.DisplayName}";
            EmitSignal(SignalName.ShopNoticeReady, "异闻录兑换完成", reward.DisplayName);
            EmitSignal(SignalName.ShopChanged);
            return true;
        }

        public bool TryUseRipeningElixir(out string summary)
        {
            summary = string.Empty;
            if (_backpackState == null || _gardenState == null || !_gardenState.HasSelectedPlotCrop)
            {
                return false;
            }

            GardenState.PlotStatus plot = _gardenState.GetSelectedPlotStatus();
            if (plot.IsReady || plot.IsEmpty)
            {
                return false;
            }

            if (!_backpackState.RemoveItem("ripening_elixir", 1))
            {
                return false;
            }

            float boosted = _gardenState.ApplyBonusProgressFraction(0.5f);
            summary = $"催熟灵液生效，减少剩余生长时间 {GardenState.FormatDuration(boosted)}";
            EmitSignal(SignalName.ShopNoticeReady, "使用催熟灵液", summary);
            EmitSignal(SignalName.ShopChanged);
            return true;
        }

        public bool TryUseMiningRefreshToken(out string summary)
        {
            summary = string.Empty;
            if (_backpackState == null || _miningState == null || !_miningState.HasSelectedNode)
            {
                return false;
            }

            if (!_backpackState.RemoveItem("mining_refresh_token", 1))
            {
                return false;
            }

            _miningState.RefreshCurrentNode();
            summary = "矿脉耐久已重置";
            EmitSignal(SignalName.ShopNoticeReady, "使用矿脉刷新令", summary);
            EmitSignal(SignalName.ShopChanged);
            return true;
        }

        public int GetLifetimePurchaseCount(string itemId)
        {
            return _lifetimePurchases.TryGetValue(itemId, out int count) ? count : 0;
        }

        public int GetDailyPurchaseCount(string itemId)
        {
            ApplyDailyResetIfNeeded();
            return _dailyPurchases.TryGetValue(itemId, out int count) ? count : 0;
        }

        public string BuildStatusSummary()
        {
            int pageFragments = _backpackState?.GetItemCount("page_fragment") ?? 0;
            string doubleYield = ActiveDoubleYieldSeconds > 0.0
                ? $"双倍产出剩余 {FormatDuration(ActiveDoubleYieldSeconds)}"
                : "双倍产出未激活";
            return $"{doubleYield} | 残页 {pageFragments}/10 | 灵田扩容 {2 + GardenPlotUnlockCount}/6 | 背包扩容 +{BackpackExpansionCount * 10}";
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            ShopPersistenceRules.ShopSnapshot snapshot = new(
                new Dictionary<string, int>(_lifetimePurchases),
                new Dictionary<string, int>(_dailyPurchases),
                DailyResetDate,
                ActiveDoubleYieldSeconds);
            return SaveValueConversionRules.ToVariantDictionary(ShopPersistenceRules.ToPlainDictionary(snapshot));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            ShopPersistenceRules.ShopSnapshot snapshot = ShopPersistenceRules.FromPlainDictionary(
                SaveValueConversionRules.ToPlainDictionary(data));
            _lifetimePurchases.Clear();
            _dailyPurchases.Clear();
            foreach ((string key, int value) in snapshot.LifetimePurchases)
            {
                _lifetimePurchases[key] = value;
            }
            foreach ((string key, int value) in snapshot.DailyPurchases)
            {
                _dailyPurchases[key] = value;
            }

            DailyResetDate = snapshot.DailyResetDate;
            ActiveDoubleYieldSeconds = snapshot.ActiveDoubleYieldSeconds;
            ApplyDailyResetIfNeeded();
            EmitSignal(SignalName.ShopChanged);
        }

        private void ConnectServices()
        {
            _activityConversionService = GetNodeOrNull<ActivityConversionService>(ActivityConversionServicePath);
            _resourceWalletState = GetNodeOrNull<ResourceWalletState>(ResourceWalletStatePath);
            _backpackState = GetNodeOrNull<BackpackState>(BackpackStatePath);
            _potionInventoryState = GetNodeOrNull<PotionInventoryState>(PotionInventoryStatePath);
            _gardenState = GetNodeOrNull<GardenState>(GardenStatePath);
            _miningState = GetNodeOrNull<MiningState>(MiningStatePath);

            if (_activityConversionService != null)
            {
                _activityConversionService.SettlementApplied += OnSettlementApplied;
            }
        }

        private void OnSettlementApplied(double apFinal10s, double lingqiGain, double insightGain, double realmExpGain)
        {
            if (apFinal10s <= 0.0 || ActiveDoubleYieldSeconds <= 0.0)
            {
                return;
            }

            ActiveDoubleYieldSeconds = Math.Max(0.0, ActiveDoubleYieldSeconds - (_activityConversionService?.SettlementIntervalSeconds ?? 10.0));
            EmitSignal(SignalName.ShopChanged);
        }

        private bool ApplyGrant(ShopRules.ShopItemDefinition item, out string summary)
        {
            summary = string.Empty;
            switch (item.GrantKind)
            {
                case ShopRules.GrantPotion:
                    if (_potionInventoryState == null)
                    {
                        return false;
                    }

                    _potionInventoryState.AddPotion(item.GrantItemId, item.GrantAmount);
                    summary = $"获得 {UiText.BackpackItemName(item.GrantItemId)} x{item.GrantAmount}";
                    return true;
                case ShopRules.GrantBackpackItem:
                    if (_backpackState == null)
                    {
                        return false;
                    }

                    _backpackState.AddItem(item.GrantItemId, item.GrantAmount);
                    summary = $"获得 {UiText.BackpackItemName(item.GrantItemId)} x{item.GrantAmount}";
                    return true;
                case ShopRules.GrantActiveBuff:
                    ActiveDoubleYieldSeconds += 3600.0;
                    summary = $"激活双倍产出 {FormatDuration(ActiveDoubleYieldSeconds)}";
                    return true;
                case ShopRules.GrantPermanent:
                    summary = item.Description;
                    return true;
                default:
                    return false;
            }
        }

        private void ApplyDailyResetIfNeeded()
        {
            string today = ShopRules.GetTodayKey(DateTime.Now);
            if (DailyResetDate == today)
            {
                return;
            }

            DailyResetDate = today;
            _dailyPurchases.Clear();
        }

        private int CountPurchasedGardenPlots()
        {
            return GetTotalPurchased(
                ShopRules.ItemGardenPlot3,
                ShopRules.ItemGardenPlot4,
                ShopRules.ItemGardenPlot5,
                ShopRules.ItemGardenPlot6);
        }

        private int GetTotalPurchased(params string[] itemIds)
        {
            int total = 0;
            for (int i = 0; i < itemIds.Length; i++)
            {
                total += GetLifetimePurchaseCount(itemIds[i]);
            }

            return total;
        }

        private static string FormatDuration(double totalSeconds)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt((float)totalSeconds));
            int minutes = seconds / 60;
            int remainSeconds = seconds % 60;
            return $"{minutes:00}:{remainSeconds:00}";
        }
    }
}
