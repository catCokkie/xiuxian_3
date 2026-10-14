using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Lightweight backpack state for loot settlement and persistence.
    /// </summary>
    public partial class BackpackState : Node, IDictionaryPersistable
    {
        private readonly Dictionary<string, int> _reservedCounts = new();
        public int GetReservedCount(string itemId) => _reservedCounts.TryGetValue(itemId, out int count) ? count : 0;
        public int GetSpendableItemCount(string itemId) => InventoryReserveRules.Spendable(GetItemCount(itemId), GetReservedCount(itemId));
        public Dictionary<string, int> GetReservedEntries() => new(_reservedCounts);
        public Dictionary<string, int> GetSpendableItemEntries()
        {
            var entries = GetItemEntries();
            foreach (string id in new List<string>(entries.Keys)) entries[id] = GetSpendableItemCount(id);
            return entries;
        }
        public void SetReservedCount(string itemId, int count)
        {
            if (string.IsNullOrWhiteSpace(itemId) || itemId.StartsWith("__", System.StringComparison.Ordinal)) return;
            count = System.Math.Clamp(count, 0, InventoryReserveRules.MaxReserve);
            if (count == GetReservedCount(itemId)) return;
            if (count == 0) _reservedCounts.Remove(itemId); else _reservedCounts[itemId] = count;
            // A policy edit changes neither inventory nor buffs, but must dirty the save and refresh consumers.
            EmitSignal(SignalName.InventoryChanged, itemId, 0, GetItemCount(itemId));
            EmitSignal(SignalName.ReserveChanged);
        }
        [Signal]
        public delegate void ReserveChangedEventHandler();
        [Signal]
        public delegate void InventoryChangedEventHandler(string itemId, int amount, int newTotal);

        [Signal]
        public delegate void EquipmentInventoryChangedEventHandler();

        private readonly Godot.Collections.Dictionary<string, Variant> _items = new();
        private readonly Dictionary<string, EquipmentStatProfile> _equipmentProfiles = new();
        private readonly Dictionary<string, EquipmentInstanceData> _equipmentInstances = new();

        public int GetItemCount(string itemId)
        {
            if (_items.ContainsKey(itemId))
            {
                return _items[itemId].AsInt32();
            }

            return 0;
        }

        public void AddItem(string itemId, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int next = GetItemCount(itemId) + amount;
            _items[itemId] = next;
            EmitSignal(SignalName.InventoryChanged, itemId, amount, next);
        }

        public bool RemoveItem(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
            {
                return true;
            }

            int current = GetItemCount(itemId);
            if (GetSpendableItemCount(itemId) < amount)
            {
                return false;
            }

            int next = current - amount;
            if (next <= 0)
            {
                _items.Remove(itemId);
            }
            else
            {
                _items[itemId] = next;
            }

            EmitSignal(SignalName.InventoryChanged, itemId, -amount, next);
            return true;
        }

        public void AddEquipment(EquipmentStatProfile profile)
        {
            _equipmentProfiles[profile.EquipmentId] = profile with { IsEquipped = false };
            EmitSignal(SignalName.EquipmentInventoryChanged);
        }

        public void AddEquipmentInstance(EquipmentInstanceData instance)
        {
            BackpackEquipmentInstanceRules.StoreInstance(_equipmentInstances, _equipmentProfiles, instance);
            EmitSignal(SignalName.EquipmentInventoryChanged);
        }

        public bool HasEquipment(string equipmentId)
        {
            return _equipmentProfiles.ContainsKey(equipmentId) || _equipmentInstances.ContainsKey(equipmentId);
        }

        public bool TryTakeEquipmentBySlot(EquipmentSlotType slot, out EquipmentStatProfile profile)
        {
            if (BackpackEquipmentInstanceRules.TryTakeBySlot(_equipmentInstances, _equipmentProfiles, slot, out _, out profile))
            {
                EmitSignal(SignalName.EquipmentInventoryChanged);
                return true;
            }

            foreach (EquipmentStatProfile item in _equipmentProfiles.Values)
            {
                if (item.Slot == slot)
                {
                    profile = item with { IsEquipped = true };
                    _equipmentProfiles.Remove(item.EquipmentId);
                    EmitSignal(SignalName.EquipmentInventoryChanged);
                    return true;
                }
            }

            profile = default;
            return false;
        }

        public EquipmentInstanceData[] GetEquipmentInstances()
        {
            EquipmentInstanceData[] result = new EquipmentInstanceData[_equipmentInstances.Count];
            int index = 0;
            foreach (EquipmentInstanceData instance in _equipmentInstances.Values)
            {
                result[index] = instance;
                index++;
            }

            return result;
        }

        public EquipmentStatProfile[] GetEquipmentProfiles()
        {
            EquipmentStatProfile[] result = new EquipmentStatProfile[_equipmentProfiles.Count];
            int index = 0;
            foreach (EquipmentStatProfile profile in _equipmentProfiles.Values)
            {
                result[index] = profile;
                index++;
            }

            return result;
        }

        public Dictionary<string, int> GetItemEntries()
        {
            Dictionary<string, int> result = new();
            foreach (string key in _items.Keys)
            {
                result[key] = _items[key].AsInt32();
            }

            return result;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            BackpackPersistenceRules.BackpackSnapshot snapshot = new(
                GetItemEntries(),
                GetEquipmentProfiles(),
                GetEquipmentInstances(), GetReservedEntries());
            return SaveValueConversionRules.ToVariantDictionary(BackpackPersistenceRules.ToPlainDictionary(snapshot));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            BackpackPersistenceRules.BackpackSnapshot snapshot = BackpackPersistenceRules.FromPlainDictionary(
                SaveValueConversionRules.ToPlainDictionary(data));
            _items.Clear();
            _equipmentProfiles.Clear();
            _equipmentInstances.Clear();
            _reservedCounts.Clear();
            if (snapshot.ReservedCounts != null)
                foreach (var entry in snapshot.ReservedCounts) _reservedCounts[entry.Key] = entry.Value;

            foreach (KeyValuePair<string, int> kv in snapshot.Items)
            {
                _items[kv.Key] = kv.Value;
            }

            foreach (EquipmentStatProfile profile in snapshot.EquipmentProfiles)
            {
                _equipmentProfiles[profile.EquipmentId] = profile with { IsEquipped = false };
            }

            foreach (EquipmentInstanceData instance in snapshot.EquipmentInstances)
            {
                BackpackEquipmentInstanceRules.StoreInstance(_equipmentInstances, _equipmentProfiles, instance);
            }
        }
    }
}
