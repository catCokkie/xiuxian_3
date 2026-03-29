using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public partial class SubsystemMasteryState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void MasteryChangedEventHandler(string systemId, int newLevel);

        private readonly Godot.Collections.Dictionary<string, Variant> _levels = new();

        public override void _Ready()
        {
            EnsureDefaults();
        }

        public int GetLevel(string systemId)
        {
            EnsureDefaults();
            return _levels.ContainsKey(systemId) ? System.Math.Clamp(_levels[systemId].AsInt32(), 1, 4) : 1;
        }

        public bool TrySetLevel(string systemId, int level)
        {
            EnsureDefaults();
            int clamped = System.Math.Clamp(level, 1, 4);
            if (GetLevel(systemId) == clamped)
            {
                return false;
            }

            _levels[systemId] = clamped;
            EmitSignal(SignalName.MasteryChanged, systemId, clamped);
            return true;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            EnsureDefaults();
            return SubsystemMasteryPersistenceRules.ToDictionary(ToLevelDictionary());
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            _levels.Clear();
            Dictionary<string, int> levels = SubsystemMasteryPersistenceRules.FromDictionary(data);
            foreach (string systemId in SubsystemMasteryRules.GetAllSystemIds())
            {
                _levels[systemId] = levels[systemId];
            }
        }

        private Dictionary<string, int> ToLevelDictionary()
        {
            var result = new Dictionary<string, int>(System.StringComparer.Ordinal);
            foreach (string systemId in SubsystemMasteryRules.GetAllSystemIds())
            {
                result[systemId] = GetLevel(systemId);
            }

            return result;
        }

        private void EnsureDefaults()
        {
            foreach (string systemId in SubsystemMasteryRules.GetAllSystemIds())
            {
                if (!_levels.ContainsKey(systemId))
                {
                    _levels[systemId] = 1;
                }
            }
        }
    }
}
