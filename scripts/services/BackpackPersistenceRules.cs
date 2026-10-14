using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class BackpackPersistenceRules
    {
        public readonly record struct BackpackSnapshot(
            IReadOnlyDictionary<string, int> Items,
            IReadOnlyList<EquipmentStatProfile> EquipmentProfiles,
            IReadOnlyList<EquipmentInstanceData> EquipmentInstances,
            IReadOnlyDictionary<string, int>? ReservedCounts = null);

        public static Dictionary<string, object> ToPlainDictionary(BackpackSnapshot snapshot)
        {
            var result = new Dictionary<string, object>();
            foreach (KeyValuePair<string, int> kv in snapshot.Items)
            {
                result[kv.Key] = kv.Value;
            }

            var equipmentProfiles = new List<object>();
            foreach (EquipmentStatProfile profile in snapshot.EquipmentProfiles)
            {
                equipmentProfiles.Add(EquipmentProfileCodec.ToPlainDictionary(profile));
            }

            var equipmentInstances = new List<object>();
            foreach (EquipmentInstanceData instance in snapshot.EquipmentInstances)
            {
                equipmentInstances.Add(EquipmentInstanceCodec.ToPlainDictionary(instance));
            }

            result["__equipment_profiles"] = equipmentProfiles;
            result["__equipment_instances"] = equipmentInstances;
            var reserves = new Dictionary<string, object>();
            if (snapshot.ReservedCounts != null)
                foreach (var entry in snapshot.ReservedCounts) reserves[entry.Key] = entry.Value;
            result[InventoryReserveRules.SaveKey] = reserves;
            return result;
        }

        public static BackpackSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            var items = new Dictionary<string, int>();
            foreach (KeyValuePair<string, object> kv in data)
            {
                if (kv.Key == "__equipment_profiles" || kv.Key == "__equipment_instances" || kv.Key == InventoryReserveRules.SaveKey)
                {
                    continue;
                }

                items[kv.Key] = System.Convert.ToInt32(kv.Value);
            }

            var equipmentProfiles = new List<EquipmentStatProfile>();
            foreach (object item in SaveValueConversionRules.GetList(data, "__equipment_profiles"))
            {
                if (item is not IDictionary<string, object> dict)
                {
                    continue;
                }

                equipmentProfiles.Add(EquipmentProfileCodec.FromPlainDictionary(dict) with { IsEquipped = false });
            }

            var equipmentInstances = new List<EquipmentInstanceData>();
            foreach (object item in SaveValueConversionRules.GetList(data, "__equipment_instances"))
            {
                if (item is not IDictionary<string, object> dict)
                {
                    continue;
                }

                equipmentInstances.Add(EquipmentInstanceCodec.FromPlainDictionary(dict) with { IsEquipped = false });
            }

            var reserved = data.TryGetValue(InventoryReserveRules.SaveKey, out var value) && value is IDictionary<string, object> dictReserves
                ? InventoryReserveRules.Read(dictReserves) : new Dictionary<string, int>();
            return new BackpackSnapshot(items, equipmentProfiles, equipmentInstances, reserved);
        }
    }
}
