using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class SmithingState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void SmithingChangedEventHandler(string targetEquipmentId, float currentProgress, float requiredProgress);

        public string TargetEquipmentId { get; private set; } = string.Empty;
        public float CurrentProgress { get; private set; }
        public float RequiredProgress { get; private set; } = 100.0f;

        public bool HasTarget => !string.IsNullOrEmpty(TargetEquipmentId);

        public void SelectTarget(string equipmentId, int currentEnhanceLevel)
        {
            TargetEquipmentId = equipmentId ?? string.Empty;
            CurrentProgress = 0.0f;
            RequiredProgress = SmithingRules.GetCost(currentEnhanceLevel).RequiredInputs;
            EmitSignal(SignalName.SmithingChanged, TargetEquipmentId, CurrentProgress, RequiredProgress);
        }

        public bool AdvanceProgress(int inputEvents)
        {
            if (!HasTarget)
            {
                return false;
            }

            CurrentProgress += Mathf.Max(0, inputEvents);
            bool completed = CurrentProgress >= RequiredProgress;
            if (completed)
            {
                CurrentProgress = 0.0f;
            }

            EmitSignal(SignalName.SmithingChanged, TargetEquipmentId, CurrentProgress, RequiredProgress);
            return completed;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["target_equipment_id"] = TargetEquipmentId,
                ["progress"] = CurrentProgress,
                ["required_progress"] = RequiredProgress,
            };
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            TargetEquipmentId = data.ContainsKey("target_equipment_id") ? data["target_equipment_id"].AsString() : string.Empty;
            CurrentProgress = data.ContainsKey("progress") ? Mathf.Max(0.0f, (float)data["progress"].AsDouble()) : 0.0f;
            RequiredProgress = data.ContainsKey("required_progress") ? Mathf.Max(1.0f, (float)data["required_progress"].AsDouble()) : 100.0f;
            EmitSignal(SignalName.SmithingChanged, TargetEquipmentId, CurrentProgress, RequiredProgress);
        }
    }
}
