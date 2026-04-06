using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class ShopRules
    {
        public const string CategoryConsumables = "consumables";
        public const string CategoryExpansion = "expansion";
        public const string CategoryUtility = "utility";
        public const string CategoryRare = "rare";

        public const string ItemHuiqiDanBundle = "shop_huiqi_dan_bundle";
        public const string ItemSpiritHerbSeed = "shop_spirit_herb_seed";
        public const string ItemSpiritFlowerSeed = "shop_spirit_flower_seed";
        public const string ItemSpiritFruitSeed = "shop_spirit_fruit_seed";
        public const string ItemRipeningElixir = "shop_ripening_elixir";
        public const string ItemMiningRefreshToken = "shop_mining_refresh_token";
        public const string ItemFishingBait = "shop_fishing_bait";

        public const string ItemGardenPlot3 = "shop_garden_plot_3";
        public const string ItemGardenPlot4 = "shop_garden_plot_4";
        public const string ItemGardenPlot5 = "shop_garden_plot_5";
        public const string ItemGardenPlot6 = "shop_garden_plot_6";
        public const string ItemBackpackExpand1 = "shop_backpack_expand_1";
        public const string ItemBackpackExpand2 = "shop_backpack_expand_2";
        public const string ItemBackpackExpand3 = "shop_backpack_expand_3";
        public const string ItemFishingPermitYoutan = "shop_fishing_permit_youtan";
        public const string ItemFishingPermitLongxianyuan = "shop_fishing_permit_longxianyuan";
        public const string ItemFishingPermitHualongpo = "shop_fishing_permit_hualongpo";

        public const string ItemAutoHarvest = "shop_auto_harvest";
        public const string ItemDoubleYield = "shop_double_yield";

        public const string ItemBreakthroughPill = "shop_breakthrough_pill";
        public const string ItemPageFragment = "shop_page_fragment";

        public const string GrantPotion = "potion";
        public const string GrantBackpackItem = "backpack_item";
        public const string GrantPermanent = "permanent";
        public const string GrantActiveBuff = "active_buff";

        public readonly record struct ShopItemDefinition(
            string ItemId,
            string Category,
            string DisplayName,
            string Description,
            int Price,
            string GrantKind,
            string GrantItemId = "",
            int GrantAmount = 1,
            int UnlockRealmLevel = 2,
            int DailyLimit = 0,
            int LifetimeLimit = 0,
            string RequiredMasterySystemId = "",
            int RequiredMasteryLevel = 0);

        private static readonly ShopItemDefinition[] Items =
        {
            new(ItemHuiqiDanBundle, CategoryConsumables, "回气丹 ×5", "不想炼丹时的替代路径。", 30, GrantPotion, "potion_huiqi_dan", 5),
            new(ItemSpiritHerbSeed, CategoryConsumables, "灵草种子", "灵田播种用，当前版本会先存入背包。", 20, GrantBackpackItem, "seed_spirit_herb", 1),
            new(ItemSpiritFlowerSeed, CategoryConsumables, "灵花种子", "灵田播种用，当前版本会先存入背包。", 80, GrantBackpackItem, "seed_spirit_flower", 1),
            new(ItemSpiritFruitSeed, CategoryConsumables, "灵果种子", "灵田播种用，当前版本会先存入背包。", 200, GrantBackpackItem, "seed_spirit_fruit", 1),
            new(ItemRipeningElixir, CategoryConsumables, "催熟灵液", "可在修炼页对当前灵田进度使用。", 30, GrantBackpackItem, "ripening_elixir", 1),
            new(ItemMiningRefreshToken, CategoryConsumables, "矿脉刷新令", "可在修炼页重置当前矿脉耐久。", 30, GrantBackpackItem, "mining_refresh_token", 1),
            new(ItemFishingBait, CategoryConsumables, "灵鱼饵 ×10", "接下来 10 次钓鱼会自动消耗鱼饵，提高额外掉落概率。", 50, GrantBackpackItem, "fishing_bait", 10),

            new(ItemGardenPlot3, CategoryExpansion, "灵田田位 +1（第 3 格）", "记录灵田扩容许可；灵田多格重构后生效。", 200, GrantPermanent, LifetimeLimit: 1),
            new(ItemGardenPlot4, CategoryExpansion, "灵田田位 +1（第 4 格）", "记录灵田扩容许可；灵田多格重构后生效。", 400, GrantPermanent, LifetimeLimit: 1),
            new(ItemGardenPlot5, CategoryExpansion, "灵田田位 +1（第 5 格）", "记录灵田扩容许可；灵田多格重构后生效。", 600, GrantPermanent, LifetimeLimit: 1),
            new(ItemGardenPlot6, CategoryExpansion, "灵田田位 +1（第 6 格）", "记录灵田扩容许可；灵田多格重构后生效。", 800, GrantPermanent, LifetimeLimit: 1),
            new(ItemBackpackExpand1, CategoryExpansion, "背包扩容 +10 格（第 1 次）", "记录背包扩容许可，当前会显示扩容进度。", 300, GrantPermanent, LifetimeLimit: 1),
            new(ItemBackpackExpand2, CategoryExpansion, "背包扩容 +10 格（第 2 次）", "记录背包扩容许可，当前会显示扩容进度。", 600, GrantPermanent, LifetimeLimit: 1),
            new(ItemBackpackExpand3, CategoryExpansion, "背包扩容 +10 格（第 3 次）", "记录背包扩容许可，当前会显示扩容进度。", 1000, GrantPermanent, LifetimeLimit: 1),
            new(ItemFishingPermitYoutan, CategoryExpansion, "鱼塘许可·幽潭", "记录鱼塘许可购买状态。", 150, GrantPermanent, LifetimeLimit: 1, RequiredMasterySystemId: PlayerActionState.ModeFishing, RequiredMasteryLevel: 2),
            new(ItemFishingPermitLongxianyuan, CategoryExpansion, "鱼塘许可·龙涎渊", "记录鱼塘许可购买状态。", 300, GrantPermanent, LifetimeLimit: 1, RequiredMasterySystemId: PlayerActionState.ModeFishing, RequiredMasteryLevel: 3),
            new(ItemFishingPermitHualongpo, CategoryExpansion, "鱼塘许可·化龙泊", "记录鱼塘许可购买状态。", 500, GrantPermanent, LifetimeLimit: 1, RequiredMasterySystemId: PlayerActionState.ModeFishing, RequiredMasteryLevel: 4),

            new(ItemAutoHarvest, CategoryUtility, "自动收获符", "灵田成熟后自动收获并重播；灵田重构后生效。", 500, GrantPermanent, LifetimeLimit: 1),
            new(ItemDoubleYield, CategoryUtility, "双倍产出符（1h 活跃时间）", "所有采集/加工产出 ×2。", 100, GrantActiveBuff, LifetimeLimit: 0, DailyLimit: 3),

            new(ItemBreakthroughPill, CategoryRare, "突破丹（筑基用）", "永久降低突破经验需求 10%。", 2000, GrantPermanent, LifetimeLimit: 1),
            new(ItemPageFragment, CategoryRare, "异闻录·残页", "收集 10 页可兑换一件随机装备。", 500, GrantBackpackItem, "page_fragment", 1, DailyLimit: 3),
        };

        private static readonly Dictionary<string, ShopItemDefinition> ItemMap = BuildItemMap();

        public static IReadOnlyList<ShopItemDefinition> GetItems() => Items;

        public static IReadOnlyList<ShopItemDefinition> GetItemsByCategory(string category)
        {
            string normalized = NormalizeCategory(category);
            List<ShopItemDefinition> result = new();
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].Category == normalized)
                {
                    result.Add(Items[i]);
                }
            }

            return result;
        }

        public static bool TryGetItem(string itemId, out ShopItemDefinition item)
        {
            return ItemMap.TryGetValue(itemId, out item);
        }

        public static string NormalizeCategory(string? category)
        {
            return category switch
            {
                CategoryExpansion => CategoryExpansion,
                CategoryUtility => CategoryUtility,
                CategoryRare => CategoryRare,
                _ => CategoryConsumables,
            };
        }

        public static bool IsShopTabUnlocked(int realmLevel)
        {
            return Math.Max(1, realmLevel) >= 2;
        }

        public static string GetTodayKey(DateTime now)
        {
            return now.ToString("yyyy-MM-dd");
        }

        public static string CategoryDisplayName(string category)
        {
            return NormalizeCategory(category) switch
            {
                CategoryExpansion => "扩容",
                CategoryUtility => "便利",
                CategoryRare => "稀有",
                _ => "消耗品",
            };
        }

        private static Dictionary<string, ShopItemDefinition> BuildItemMap()
        {
            Dictionary<string, ShopItemDefinition> result = new(StringComparer.Ordinal);
            for (int i = 0; i < Items.Length; i++)
            {
                result[Items[i].ItemId] = Items[i];
            }

            return result;
        }
    }
}
