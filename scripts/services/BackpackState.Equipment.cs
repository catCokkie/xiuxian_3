namespace Xiuxian.Scripts.Services
{
    public partial class BackpackState
    {
        public bool TryGetEquipmentById(string id, out EquipmentStatProfile profile, out EquipmentInstanceData? instance)
        {
            _equipmentInstances.TryGetValue(id, out instance);
            return _equipmentProfiles.TryGetValue(id, out profile);
        }

        // Callers validate first, then write both stores before publishing any signals.
        internal void CommitEquipmentSwap(string targetId, EquipmentStatProfile? returned, EquipmentInstanceData? instance)
        {
            _equipmentProfiles.Remove(targetId);
            _equipmentInstances.Remove(targetId);
            if (returned is not EquipmentStatProfile profile) return;
            _equipmentProfiles[profile.EquipmentId] = profile with { IsEquipped = false };
            _equipmentInstances.Remove(profile.EquipmentId);
            if (instance != null)
                _equipmentInstances[profile.EquipmentId] = instance with { EnhanceLevel = profile.EnhanceLevel, IsEquipped = false };
        }

        internal void NotifyEquipmentChanged() => EmitSignal(SignalName.EquipmentInventoryChanged);
    }
}
