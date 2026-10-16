using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public partial class EquippedItemsState
    {
        private readonly Dictionary<string, EquipmentInstanceData> _instances = new();

        public bool TryGetEquippedInstanceById(string id, out EquipmentInstanceData? instance)
        {
            instance = null;
            if (!TryGetEquippedProfileById(id, out var profile) || !_instances.TryGetValue(id, out var stored)) return false;
            instance = stored with { EnhanceLevel = profile.EnhanceLevel, IsEquipped = true };
            return true;
        }

        internal void CommitEquipmentSwap(EquipmentStatProfile profile, EquipmentInstanceData? instance)
        {
            if (_equippedBySlot.TryGetValue(profile.Slot, out var old))
            {
                if (instance == null && old.EquipmentId == profile.EquipmentId) _instances.TryGetValue(old.EquipmentId, out instance);
                _instances.Remove(old.EquipmentId);
            }
            _equippedBySlot[profile.Slot] = profile with { IsEquipped = true };
            if (instance != null && MatchesProfile(instance, profile))
                _instances[profile.EquipmentId] = instance with { EnhanceLevel = profile.EnhanceLevel, IsEquipped = true };
        }

        internal void NotifyEquipmentChanged() => EmitSignal(SignalName.EquippedItemsChanged);

        private List<object> SerializeInstances()
        {
            var result = new List<object>();
            foreach (string id in _instances.Keys)
                if (TryGetEquippedInstanceById(id, out var instance)) result.Add(EquipmentInstanceCodec.ToPlainDictionary(instance!));
            return result;
        }

        private void RestoreInstances(IDictionary<string, object> data)
        {
            _instances.Clear();
            foreach (object entry in SaveValueConversionRules.GetList(data, "__instances"))
            {
                if (entry is not IDictionary<string, object> record) continue;
                var instance = EquipmentInstanceCodec.FromPlainDictionary(record);
                if (TryGetEquippedProfileById(instance.EquipmentId, out var profile) && MatchesProfile(instance, profile))
                    _instances[instance.EquipmentId] = instance with { EnhanceLevel = profile.EnhanceLevel, IsEquipped = true };
            }
        }

        private static bool MatchesProfile(EquipmentInstanceData instance, EquipmentStatProfile profile)
            => (EquipmentInstanceRules.ToStatProfile(instance) with { EnhanceLevel = profile.EnhanceLevel, IsEquipped = profile.IsEquipped }) == profile;
    }
}
