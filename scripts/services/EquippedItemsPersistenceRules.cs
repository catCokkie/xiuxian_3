using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class EquippedItemsPersistenceRules
    {
        public static Dictionary<string, object> ToPlainDictionary(IReadOnlyDictionary<EquipmentSlotType, EquipmentStatProfile> equippedBySlot)
        {
            var result = new Dictionary<string, object>();
            foreach (KeyValuePair<EquipmentSlotType, EquipmentStatProfile> kv in equippedBySlot)
            {
                result[kv.Key.ToString()] = EquipmentProfileCodec.ToPlainDictionary(kv.Value);
            }

            return result;
        }

        public static Dictionary<EquipmentSlotType, EquipmentStatProfile> FromPlainDictionary(IDictionary<string, object> data)
        {
            var result = new Dictionary<EquipmentSlotType, EquipmentStatProfile>();
            foreach (KeyValuePair<string, object> kv in data)
            {
                if (kv.Value is not IDictionary<string, object> dict || !System.Enum.TryParse(kv.Key, out EquipmentSlotType slot))
                {
                    continue;
                }

                EquipmentStatProfile profile = EquipmentProfileCodec.FromPlainDictionary(dict);
                result[slot] = profile with { Slot = slot, IsEquipped = true };
            }

            return result;
        }
    }
}
