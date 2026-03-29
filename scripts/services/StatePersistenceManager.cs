using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Manages a registry of (section, key, state) slots for uniform ConfigFile persistence.
    /// Eliminates per-state Read*/Write* boilerplate in the root controller.
    /// </summary>
    public sealed class StatePersistenceManager
    {
        private readonly List<(string section, string key, IDictionaryPersistable state)> _slots = new();

        public void Register(string section, string key, IDictionaryPersistable? state)
        {
            if (state != null)
            {
                _slots.Add((section, key, state));
            }
        }

        public void WriteAll(ConfigFile config)
        {
            foreach (var (section, key, state) in _slots)
            {
                config.SetValue(section, key, state.ToDictionary());
            }
        }

        public void ReadAll(ConfigFile config)
        {
            foreach (var (section, key, state) in _slots)
            {
                ReadSlotSafe(config, section, key, state);
            }
        }

        private static void ReadSlotSafe(ConfigFile config, string section, string key, IDictionaryPersistable state)
        {
            try
            {
                Variant data = config.GetValue(section, key, new Godot.Collections.Dictionary<string, Variant>());
                if (data.VariantType == Variant.Type.Dictionary)
                {
                    state.FromDictionary((Godot.Collections.Dictionary<string, Variant>)data);
                }
            }
            catch (System.Exception ex)
            {
                GD.PushError($"StatePersistenceManager: failed to read [{section}/{key}] — skipped: {ex.Message}");
            }
        }
    }
}
