using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class SmithingState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void SmithingChangedEventHandler(string targetEquipmentId, float currentProgress, float requiredProgress);

        public string TargetEquipmentId { get; private set; } = string.Empty;
        public CraftingProductionPlan Production { get; } = new();
        public float CurrentProgress { get; private set; }
        public float RequiredProgress { get; private set; } = 100.0f;

        public bool HasTarget => !string.IsNullOrEmpty(TargetEquipmentId);

        public void SelectTarget(string equipmentId, int currentEnhanceLevel)
        {
            if (TargetEquipmentId == (equipmentId ?? string.Empty)) return;
            Production.Configure(false);
            Production.Stop();
            TargetEquipmentId = equipmentId ?? string.Empty;
            CurrentProgress = 0.0f;
            RequiredProgress = SmithingRules.GetCost(currentEnhanceLevel).RequiredInputs;
            EmitSignal(SignalName.SmithingChanged, TargetEquipmentId, CurrentProgress, RequiredProgress);
        }

        public bool AdvanceProgress(int inputEvents)
        {
            if (!HasTarget) return false;
            if (!Production.CanProduce) return false;
            double inputRate = 1.0;
            CurrentProgress = BatchProgressRules.Advance(CurrentProgress, inputEvents, RequiredProgress, inputRate);
            // Successive enhancement tiers cost different inputs. CommitBatch limits
            // actual completions and discards unused overflow at the requested end.
            EmitSignal(SignalName.SmithingChanged, TargetEquipmentId, CurrentProgress, RequiredProgress);
            return CurrentProgress >= RequiredProgress;
        }

        public void CompleteBatch()
        {
            if (CurrentProgress < RequiredProgress || !Production.CanProduce) return;
            CurrentProgress = BatchProgressRules.Consume(CurrentProgress, RequiredProgress);
            Production.CommitBatch();
            if (Production.Finished) CurrentProgress = 0;
            EmitSignal(SignalName.SmithingChanged, TargetEquipmentId, CurrentProgress, RequiredProgress);
        }

        public void RefreshRequiredProgress(int enhanceLevel)
        {
            RequiredProgress = SmithingRules.GetCost(enhanceLevel).RequiredInputs;
            EmitSignal(SignalName.SmithingChanged, TargetEquipmentId, CurrentProgress, RequiredProgress);
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            var data = new Godot.Collections.Dictionary<string, Variant>
            {
                ["target_equipment_id"] = TargetEquipmentId,
                ["progress"] = CurrentProgress,
                ["required_progress"] = RequiredProgress,
            };
            Production.WriteTo(data);
            return data;
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            TargetEquipmentId = data.ContainsKey("target_equipment_id") ? data["target_equipment_id"].AsString() : string.Empty;
            CurrentProgress = data.ContainsKey("progress") ? Mathf.Max(0.0f, (float)data["progress"].AsDouble()) : 0.0f;
            RequiredProgress = data.ContainsKey("required_progress") ? Mathf.Max(1.0f, (float)data["required_progress"].AsDouble()) : 100.0f;
            Production.ReadFrom(data, HasTarget);
            EmitSignal(SignalName.SmithingChanged, TargetEquipmentId, CurrentProgress, RequiredProgress);
        }
    }
}
