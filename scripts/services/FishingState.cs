using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class FishingState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void FishingChangedEventHandler(string selectedRecipeId, float currentProgress, float requiredProgress);

        public string SelectedRecipeId { get; private set; } = string.Empty;
        public float CurrentProgress { get; private set; }
        public float RequiredProgress { get; private set; } = 120.0f;
        public bool HasSelectedPond => !string.IsNullOrEmpty(SelectedRecipeId);

        public void SelectPond(string recipeId)
        {
            if (!FishingRules.TryGetPond(recipeId, out FishingRules.PondSpec pond))
            {
                SelectedRecipeId = string.Empty;
                CurrentProgress = 0.0f;
                RequiredProgress = 120.0f;
            }
            else
            {
                SelectedRecipeId = pond.RecipeId;
                CurrentProgress = 0.0f;
                RequiredProgress = pond.RequiredInputs;
            }

            EmitSignal(SignalName.FishingChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }

        public bool AdvanceProgress(int inputEvents)
        {
            if (!HasSelectedPond)
            {
                return false;
            }

            FishingRules.FishingProgressDecision decision = FishingRules.AdvanceProgress(CurrentProgress, inputEvents, Mathf.RoundToInt(RequiredProgress));
            CurrentProgress = decision.NextProgress;
            EmitSignal(SignalName.FishingChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
            return decision.CompletedBatch;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return FishingPersistenceRules.ToDictionary(new FishingPersistenceRules.FishingSnapshot(SelectedRecipeId, CurrentProgress, RequiredProgress));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            FishingPersistenceRules.FishingSnapshot snapshot = FishingPersistenceRules.FromDictionary(data);
            SelectedRecipeId = snapshot.SelectedRecipeId;
            CurrentProgress = snapshot.Progress;
            RequiredProgress = snapshot.RequiredProgress;
            EmitSignal(SignalName.FishingChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }
    }
}
