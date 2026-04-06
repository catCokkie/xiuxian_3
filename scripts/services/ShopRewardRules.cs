namespace Xiuxian.Scripts.Services
{
    public static class ShopRewardRules
    {
        public static EquipmentStatProfile BuildPageExchangeReward(int exchangeIndex)
        {
            int normalized = exchangeIndex < 1 ? 1 : exchangeIndex;
            return (normalized % 3) switch
            {
                1 => new EquipmentStatProfile(
                    $"shop_page_weapon_{normalized}",
                    $"异闻灵兵·第{normalized}卷",
                    EquipmentSlotType.Weapon,
                    new CharacterStatModifier(AttackFlat: 8, CritChanceDelta: 0.02),
                    SetTag: "shop_page",
                    Rarity: 2,
                    EnhanceLevel: 0,
                    IsEquipped: false),
                2 => new EquipmentStatProfile(
                    $"shop_page_armor_{normalized}",
                    $"异闻灵甲·第{normalized}卷",
                    EquipmentSlotType.Armor,
                    new CharacterStatModifier(MaxHpFlat: 24, DefenseFlat: 4),
                    SetTag: "shop_page",
                    Rarity: 2,
                    EnhanceLevel: 0,
                    IsEquipped: false),
                _ => new EquipmentStatProfile(
                    $"shop_page_accessory_{normalized}",
                    $"异闻灵饰·第{normalized}卷",
                    EquipmentSlotType.Accessory,
                    new CharacterStatModifier(CritChanceDelta: 0.03, SpeedFlat: 4),
                    SetTag: "shop_page",
                    Rarity: 2,
                    EnhanceLevel: 0,
                    IsEquipped: false),
            };
        }
    }
}
