using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class MiningState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void MiningChangedEventHandler(string selectedRecipeId, float currentProgress, float requiredProgress, int currentDurability);

        public string SelectedRecipeId { get; private set; } = string.Empty;
        public float CurrentProgress { get; private set; }
        public float RequiredProgress { get; private set; } = 180.0f;
        public int CurrentDurability { get; private set; } = MiningRules.DefaultNodeDurability;
        public bool Recycling { get; private set; }
        public float RecycleProgress { get; private set; }
        public const float RecycleRequired = 100;
        public CraftingProductionPlan RecyclingProduction { get; } = CreateRecyclePlan();
        private static CraftingProductionPlan CreateRecyclePlan() { var plan = new CraftingProductionPlan(); plan.Stop(); return plan; }
        public void SetRecycling(bool recycling)
        {
            if (Recycling == recycling) return;
            RecyclingProduction.Stop(); Recycling = recycling; NotifyProductionChanged();
        }
        public void NotifyProductionChanged() => EmitSignal(SignalName.MiningChanged, SelectedRecipeId, CurrentProgress, RequiredProgress, CurrentDurability);
        public void AdvanceRecycling(int inputs)
        {
            if (!Recycling || !RecyclingProduction.CanProduce) return;
            RecycleProgress = RecyclingProduction.BoundProgress(BatchProgressRules.Advance(RecycleProgress, inputs, RecycleRequired), RecycleRequired);
            NotifyProductionChanged();
        }
        public void CompleteRecycling()
        {
            if (!Recycling || !RecyclingProduction.CanProduce || RecycleProgress < RecycleRequired) return;
            RecycleProgress = BatchProgressRules.Consume(RecycleProgress, RecycleRequired); RecyclingProduction.CommitBatch(); NotifyProductionChanged();
        }
        public bool HasSelectedNode => !string.IsNullOrEmpty(SelectedRecipeId);

        public void SelectNode(string recipeId)
        {
            SetRecycling(false);
            if (SelectedRecipeId == recipeId && HasSelectedNode) return;
            if (!MiningRules.TryGetNode(recipeId, out MiningRules.NodeSpec node))
            {
                SelectedRecipeId = string.Empty;
                CurrentProgress = 0.0f;
                RequiredProgress = 180.0f;
                CurrentDurability = MiningRules.GetEffectiveMaxDurability(ServiceLocator.Instance?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeMining) ?? 1);
            }
            else
            {
                SelectedRecipeId = node.RecipeId;
                CurrentProgress = 0.0f;
                RequiredProgress = node.RequiredInputs;
                CurrentDurability = MiningRules.GetEffectiveMaxDurability(ServiceLocator.Instance?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeMining) ?? 1);
            }

            EmitSignal(SignalName.MiningChanged, SelectedRecipeId, CurrentProgress, RequiredProgress, CurrentDurability);
        }

        public bool AdvanceProgress(int inputEvents)
        {
            if (!HasSelectedNode) return false;
            double inputRate = 1.0 / (1 - MiningRules.GetSpeedBonus(ServiceLocator.Instance?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeMining) ?? 1));
            CurrentProgress = BatchProgressRules.Advance(CurrentProgress, inputEvents, RequiredProgress, inputRate);
            EmitSignal(SignalName.MiningChanged, SelectedRecipeId, CurrentProgress, RequiredProgress, CurrentDurability);
            return CurrentProgress >= RequiredProgress;
        }

        public void CompleteBatch()
        {
            if (CurrentProgress < RequiredProgress) return;
            CurrentProgress = BatchProgressRules.Consume(CurrentProgress, RequiredProgress);
            int mastery = ServiceLocator.Instance?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeMining) ?? 1;
            CurrentDurability = CurrentDurability <= 1 ? MiningRules.GetEffectiveMaxDurability(mastery) : CurrentDurability - 1;
            EmitSignal(SignalName.MiningChanged, SelectedRecipeId, CurrentProgress, RequiredProgress, CurrentDurability);
        }

        public void RefreshCurrentNode()
        {
            if (!HasSelectedNode)
            {
                return;
            }

            CurrentDurability = MiningRules.GetEffectiveMaxDurability(ServiceLocator.Instance?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeMining) ?? 1);
            EmitSignal(SignalName.MiningChanged, SelectedRecipeId, CurrentProgress, RequiredProgress, CurrentDurability);
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            var data = MiningPersistenceRules.ToDictionary(new MiningPersistenceRules.MiningSnapshot(SelectedRecipeId, CurrentProgress, RequiredProgress, CurrentDurability));
            data["recycling"] = Recycling; data["recycle_progress"] = RecycleProgress;
            data["recycle_production"] = SaveValueConversionRules.ToVariantDictionary(RecyclingProduction.ToPlainDictionary());
            return data;
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            MiningPersistenceRules.MiningSnapshot snapshot = MiningPersistenceRules.FromDictionary(data);
            Recycling = data.TryGetValue("recycling", out var operation) && operation.AsBool();
            RecycleProgress = data.TryGetValue("recycle_progress", out var progress) ? System.Math.Max(0, progress.AsSingle()) : 0;
            if (!float.IsFinite(RecycleProgress)) RecycleProgress = 0;
            if (data.TryGetValue("recycle_production", out var plan) && plan.VariantType == Variant.Type.Dictionary)
                RecyclingProduction.Restore(SaveValueConversionRules.ToPlainDictionary((Godot.Collections.Dictionary<string, Variant>)plan));
            else { RecyclingProduction.Configure(false, 1); RecyclingProduction.Stop(); }
            RecycleProgress = RecyclingProduction.BoundProgress(RecycleProgress, RecycleRequired);
            SelectedRecipeId = snapshot.SelectedRecipeId;
            CurrentProgress = snapshot.Progress;
            RequiredProgress = snapshot.RequiredProgress;
            CurrentDurability = snapshot.CurrentDurability;
            EmitSignal(SignalName.MiningChanged, SelectedRecipeId, CurrentProgress, RequiredProgress, CurrentDurability);
        }
    }
}
