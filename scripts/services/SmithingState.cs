using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class SmithingState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void SmithingChangedEventHandler(string targetEquipmentId, float currentProgress, float requiredProgress);

        public string TargetEquipmentId { get; private set; } = string.Empty;
        public CraftingProductionPlan Production { get; } = new();
        private readonly CraftingProjectRecords _projects = new();
        public float CurrentProgress { get; private set; }
        public float RequiredProgress { get; private set; } = 100.0f;

        private int _targetEnhanceLevel;

        public bool HasTarget => !string.IsNullOrEmpty(TargetEquipmentId);

        public void SelectTarget(string equipmentId, int currentEnhanceLevel)
        {
            if (TargetEquipmentId == (equipmentId ?? string.Empty)) { SynchronizeTargetLevel(currentEnhanceLevel); return; }
            Production.Stop();
            _projects.Capture(TargetEquipmentId, CurrentProgress, RequiredProgress, Production, _targetEnhanceLevel);
            Production.Configure(false);
            Production.Stop();
            TargetEquipmentId = equipmentId ?? string.Empty;
            CurrentProgress = 0.0f;
            RequiredProgress = SmithingRules.GetCost(currentEnhanceLevel).RequiredInputs;
            _targetEnhanceLevel = currentEnhanceLevel;
            if (_projects.TryRestore(TargetEquipmentId, Production, out float savedProgress, out _, out int savedLevel))
                CurrentProgress = savedLevel == currentEnhanceLevel ? savedProgress : 0;

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
            _targetEnhanceLevel = enhanceLevel;
            RequiredProgress = SmithingRules.GetCost(enhanceLevel).RequiredInputs;
            EmitSignal(SignalName.SmithingChanged, TargetEquipmentId, CurrentProgress, RequiredProgress);
        }

        public void SynchronizeTargetLevel(int enhanceLevel)
        {
            if (_targetEnhanceLevel == enhanceLevel) return;
            // A completed low-tier budget is never reused after external enhancement.
            CurrentProgress = 0;
            RefreshRequiredProgress(enhanceLevel);
        }

        public void NotifyProductionChanged() => EmitSignal(SignalName.SmithingChanged, TargetEquipmentId, CurrentProgress, RequiredProgress);

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            var data = new Godot.Collections.Dictionary<string, Variant>
            {
                ["target_equipment_id"] = TargetEquipmentId,
                ["progress"] = CurrentProgress,
                ["required_progress"] = RequiredProgress,
                ["enhance_level"] = _targetEnhanceLevel,
            };
            Production.WriteTo(data);
            _projects.Capture(TargetEquipmentId, CurrentProgress, RequiredProgress, Production, _targetEnhanceLevel);
            _projects.WriteTo(data);

            return data;
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            TargetEquipmentId = data.ContainsKey("target_equipment_id") ? data["target_equipment_id"].AsString() : string.Empty;
            CurrentProgress = data.ContainsKey("progress") ? Mathf.Max(0.0f, (float)data["progress"].AsDouble()) : 0.0f;
            RequiredProgress = data.ContainsKey("required_progress") ? Mathf.Max(1.0f, (float)data["required_progress"].AsDouble()) : 100.0f;
            Production.ReadFrom(data, HasTarget);
            _projects.ReadFrom(data);
            _targetEnhanceLevel = data.ContainsKey("enhance_level") ? data["enhance_level"].AsInt32() : System.Math.Max(0, (int)((RequiredProgress - 100) / 50));
            EmitSignal(SignalName.SmithingChanged, TargetEquipmentId, CurrentProgress, RequiredProgress);
        }
    }
}
