namespace Xiuxian.Scripts.Services
{
    public readonly record struct EquipmentStatProfile(
        string EquipmentId,
        string DisplayName,
        EquipmentSlotType Slot,
        CharacterStatModifier Modifier,
        string SetTag = "",
        int Rarity = 1,
        int EnhanceLevel = 0,
        bool IsEquipped = true)
    {
        public static double GetEnhancedValue(double baseValue, int enhanceLevel)
        {
            return baseValue * SmithingRules.GetEnhanceMultiplier(enhanceLevel);
        }

        public CharacterStatModifier ToModifier()
        {
            if (!IsEquipped)
            {
                return default;
            }

            return Modifier with
            {
                MaxHpFlat = (int)System.Math.Round(GetEnhancedValue(Modifier.MaxHpFlat, EnhanceLevel)),
                AttackFlat = (int)System.Math.Round(GetEnhancedValue(Modifier.AttackFlat, EnhanceLevel)),
                DefenseFlat = (int)System.Math.Round(GetEnhancedValue(Modifier.DefenseFlat, EnhanceLevel)),
                SpeedFlat = (int)System.Math.Round(GetEnhancedValue(Modifier.SpeedFlat, EnhanceLevel)),
                MaxHpRate = GetEnhancedValue(Modifier.MaxHpRate, EnhanceLevel),
                AttackRate = GetEnhancedValue(Modifier.AttackRate, EnhanceLevel),
                DefenseRate = GetEnhancedValue(Modifier.DefenseRate, EnhanceLevel),
                SpeedRate = GetEnhancedValue(Modifier.SpeedRate, EnhanceLevel),
                CritChanceDelta = GetEnhancedValue(Modifier.CritChanceDelta, EnhanceLevel),
                CritDamageDelta = GetEnhancedValue(Modifier.CritDamageDelta, EnhanceLevel),
            };
        }
    }
}
