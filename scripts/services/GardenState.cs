using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class GardenState : Node
    {
        [Signal]
        public delegate void GardenChangedEventHandler(string selectedRecipeId, float currentProgress, float requiredProgress);

        public string SelectedRecipeId { get; private set; } = string.Empty;
        public float CurrentProgress { get; private set; }
        public float RequiredProgress { get; private set; } = 200.0f;
        public bool HasSelectedCrop => !string.IsNullOrEmpty(SelectedRecipeId);

        public void SelectCrop(string recipeId)
        {
            if (!GardenRules.TryGetCrop(recipeId, out GardenRules.CropSpec crop))
            {
                SelectedRecipeId = string.Empty;
                CurrentProgress = 0.0f;
                RequiredProgress = 200.0f;
            }
            else
            {
                SelectedRecipeId = crop.RecipeId;
                CurrentProgress = 0.0f;
                RequiredProgress = crop.RequiredInputs;
            }

            EmitSignal(SignalName.GardenChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }

        public bool AdvanceProgress(int inputEvents)
        {
            if (!HasSelectedCrop)
            {
                return false;
            }

            GardenRules.GardenProgressDecision decision = GardenRules.AdvanceProgress(CurrentProgress, inputEvents, Mathf.RoundToInt(RequiredProgress));
            CurrentProgress = decision.NextProgress;
            EmitSignal(SignalName.GardenChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
            return decision.CompletedBatch;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return GardenPersistenceRules.ToDictionary(new GardenPersistenceRules.GardenSnapshot(SelectedRecipeId, CurrentProgress, RequiredProgress));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            GardenPersistenceRules.GardenSnapshot snapshot = GardenPersistenceRules.FromDictionary(data);
            SelectedRecipeId = snapshot.SelectedRecipeId;
            CurrentProgress = snapshot.Progress;
            RequiredProgress = snapshot.RequiredProgress;
            EmitSignal(SignalName.GardenChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }
    }
}
