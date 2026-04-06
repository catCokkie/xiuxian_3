using System;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class ShopRulesTests
{
    [Fact]
    public void ShopTab_UnlocksAtRealmTwo()
    {
        Assert.False(ShopRules.IsShopTabUnlocked(1));
        Assert.True(ShopRules.IsShopTabUnlocked(2));
    }

    [Fact]
    public void GetItemsByCategory_ReturnsConfiguredShelfItems()
    {
        Assert.Contains(ShopRules.GetItemsByCategory(ShopRules.CategoryConsumables), x => x.ItemId == ShopRules.ItemHuiqiDanBundle);
        Assert.Contains(ShopRules.GetItemsByCategory(ShopRules.CategoryExpansion), x => x.ItemId == ShopRules.ItemBackpackExpand1);
        Assert.Contains(ShopRules.GetItemsByCategory(ShopRules.CategoryUtility), x => x.ItemId == ShopRules.ItemDoubleYield);
        Assert.Contains(ShopRules.GetItemsByCategory(ShopRules.CategoryRare), x => x.ItemId == ShopRules.ItemBreakthroughPill);
    }

    [Fact]
    public void NormalizeCategory_FallsBackToConsumables()
    {
        Assert.Equal(ShopRules.CategoryConsumables, ShopRules.NormalizeCategory("unknown"));
        Assert.Equal(ShopRules.CategoryRare, ShopRules.NormalizeCategory(ShopRules.CategoryRare));
    }

    [Fact]
    public void GetTodayKey_FormatsLocalDateWithoutTime()
    {
        Assert.Equal("2026-04-06", ShopRules.GetTodayKey(new DateTime(2026, 4, 6, 13, 25, 42)));
    }
}
