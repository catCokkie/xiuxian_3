using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class PotionInventoryState : Node
    {
        [Signal]
        public delegate void PotionInventoryChangedEventHandler(string potionId, int amount, int newTotal);

        private readonly Godot.Collections.Dictionary<string, Variant> _potions = new();

        public int GetPotionCount(string potionId)
        {
            return _potions.ContainsKey(potionId) ? _potions[potionId].AsInt32() : 0;
        }

        public void AddPotion(string potionId, int amount)
        {
            if (string.IsNullOrEmpty(potionId) || amount <= 0)
            {
                return;
            }

            int next = GetPotionCount(potionId) + amount;
            _potions[potionId] = next;
            EmitSignal(SignalName.PotionInventoryChanged, potionId, amount, next);
        }

        public bool ConsumePotion(string potionId, int amount = 1)
        {
            if (amount <= 0)
            {
                return true;
            }

            int current = GetPotionCount(potionId);
            if (current < amount)
            {
                return false;
            }

            int next = current - amount;
            if (next <= 0)
            {
                _potions.Remove(potionId);
            }
            else
            {
                _potions[potionId] = next;
            }

            EmitSignal(SignalName.PotionInventoryChanged, potionId, -amount, next);
            return true;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return new Godot.Collections.Dictionary<string, Variant>(_potions);
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            _potions.Clear();
            foreach (string key in data.Keys)
            {
                _potions[key] = data[key].AsInt32();
            }
        }

        public System.Collections.Generic.Dictionary<string, int> GetPotionEntries()
        {
            var result = new System.Collections.Generic.Dictionary<string, int>();
            foreach (string key in _potions.Keys)
            {
                result[key] = _potions[key].AsInt32();
            }

            return result;
        }
    }
}
