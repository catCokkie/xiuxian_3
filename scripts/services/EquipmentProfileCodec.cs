using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentProfileCodec
    {
        public static Dictionary<string, object> ToPlainDictionary(EquipmentStatProfile profile)
        {
            EquipmentProfilePersistenceData data = EquipmentProfilePersistenceRules.ToData(profile);
            return new Dictionary<string, object>
            {
                ["equipment_id"] = data.EquipmentId,
                ["display_name"] = data.DisplayName,
                ["slot"] = data.Slot,
                ["set_tag"] = data.SetTag,
                ["rarity"] = data.Rarity,
                ["enhance_level"] = data.EnhanceLevel,
                ["is_equipped"] = data.IsEquipped,
                ["modifier"] = ModifierToPlainDictionary(profile.Modifier),
            };
        }

        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(EquipmentStatProfile profile)
        {
            return SaveValueConversionRules.ToVariantDictionary(ToPlainDictionary(profile));
        }

        public static EquipmentStatProfile FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            return FromPlainDictionary(SaveValueConversionRules.ToPlainDictionary(data));
        }

        public static EquipmentStatProfile FromPlainDictionary(IDictionary<string, object> data)
        {
            CharacterStatModifier modifier = default;
            if (data.TryGetValue("modifier", out object rawModifier) && rawModifier is IDictionary<string, object> modifierData)
            {
                modifier = ModifierFromPlainDictionary(modifierData);
            }

            string equipmentId = SaveValueConversionRules.GetString(data, "equipment_id", "");

            return EquipmentProfilePersistenceRules.FromData(new EquipmentProfilePersistenceData(
                equipmentId,
                SaveValueConversionRules.GetString(data, "display_name", ""),
                SaveValueConversionRules.GetString(data, "slot", EquipmentSlotType.Weapon.ToString()),
                SaveValueConversionRules.GetString(data, "set_tag", ""),
                SaveValueConversionRules.GetInt(data, "rarity", 1),
                SaveValueConversionRules.GetInt(data, "enhance_level", 0),
                !data.ContainsKey("is_equipped") || SaveValueConversionRules.GetBool(data, "is_equipped", true),
                modifier.MaxHpFlat,
                modifier.AttackFlat,
                modifier.DefenseFlat,
                modifier.SpeedFlat,
                modifier.MaxHpRate,
                modifier.AttackRate,
                modifier.DefenseRate,
                modifier.SpeedRate,
                modifier.CritChanceDelta,
                modifier.CritDamageDelta));
        }

        private static Dictionary<string, object> ModifierToPlainDictionary(CharacterStatModifier modifier)
        {
            return new Dictionary<string, object>
            {
                ["max_hp_flat"] = modifier.MaxHpFlat,
                ["attack_flat"] = modifier.AttackFlat,
                ["defense_flat"] = modifier.DefenseFlat,
                ["speed_flat"] = modifier.SpeedFlat,
                ["max_hp_rate"] = modifier.MaxHpRate,
                ["attack_rate"] = modifier.AttackRate,
                ["defense_rate"] = modifier.DefenseRate,
                ["speed_rate"] = modifier.SpeedRate,
                ["crit_chance_delta"] = modifier.CritChanceDelta,
                ["crit_damage_delta"] = modifier.CritDamageDelta,
            };
        }

        private static Godot.Collections.Dictionary<string, Variant> ModifierToDictionary(CharacterStatModifier modifier)
        {
            return SaveValueConversionRules.ToVariantDictionary(ModifierToPlainDictionary(modifier));
        }

        private static CharacterStatModifier ModifierFromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            return ModifierFromPlainDictionary(SaveValueConversionRules.ToPlainDictionary(data));
        }

        private static CharacterStatModifier ModifierFromPlainDictionary(IDictionary<string, object> data)
        {
            return new CharacterStatModifier(
                MaxHpFlat: SaveValueConversionRules.GetInt(data, "max_hp_flat", 0),
                AttackFlat: SaveValueConversionRules.GetInt(data, "attack_flat", 0),
                DefenseFlat: SaveValueConversionRules.GetInt(data, "defense_flat", 0),
                SpeedFlat: SaveValueConversionRules.GetInt(data, "speed_flat", 0),
                MaxHpRate: SaveValueConversionRules.GetDouble(data, "max_hp_rate", 0.0),
                AttackRate: SaveValueConversionRules.GetDouble(data, "attack_rate", 0.0),
                DefenseRate: SaveValueConversionRules.GetDouble(data, "defense_rate", 0.0),
                SpeedRate: SaveValueConversionRules.GetDouble(data, "speed_rate", 0.0),
                CritChanceDelta: SaveValueConversionRules.GetDouble(data, "crit_chance_delta", 0.0),
                CritDamageDelta: SaveValueConversionRules.GetDouble(data, "crit_damage_delta", 0.0));
        }
    }
}
