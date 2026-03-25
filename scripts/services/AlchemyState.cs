using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class AlchemyState : Node
    {
        [Signal]
        public delegate void AlchemyChangedEventHandler(string selectedRecipeId, float currentProgress, float requiredProgress);

        public string SelectedRecipeId { get; private set; } = string.Empty;
        public float CurrentProgress { get; private set; }
        public float RequiredProgress { get; private set; } = 200.0f;

        public bool HasSelectedRecipe => !string.IsNullOrEmpty(SelectedRecipeId);

        public void SelectRecipe(string recipeId)
        {
            if (!AlchemyRules.TryGetRecipe(recipeId, out AlchemyRules.RecipeSpec recipe))
            {
                SelectedRecipeId = string.Empty;
                CurrentProgress = 0.0f;
                RequiredProgress = 200.0f;
            }
            else
            {
                SelectedRecipeId = recipe.RecipeId;
                CurrentProgress = 0.0f;
                RequiredProgress = recipe.RequiredInputs;
            }

            EmitSignal(SignalName.AlchemyChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }

        public bool AdvanceProgress(int inputEvents)
        {
            if (!HasSelectedRecipe)
            {
                return false;
            }

            AlchemyRules.AlchemyProgressDecision decision = AlchemyRules.AdvanceProgress(CurrentProgress, inputEvents, Mathf.RoundToInt(RequiredProgress));
            CurrentProgress = decision.NextProgress;
            EmitSignal(SignalName.AlchemyChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
            return decision.CompletedBatch;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["selected_recipe"] = SelectedRecipeId,
                ["progress"] = CurrentProgress,
                ["required_progress"] = RequiredProgress,
            };
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            SelectedRecipeId = data.ContainsKey("selected_recipe") ? data["selected_recipe"].AsString() : string.Empty;
            CurrentProgress = data.ContainsKey("progress") ? Mathf.Max(0.0f, (float)data["progress"].AsDouble()) : 0.0f;
            RequiredProgress = data.ContainsKey("required_progress") ? Mathf.Max(1.0f, (float)data["required_progress"].AsDouble()) : 200.0f;
            EmitSignal(SignalName.AlchemyChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }
    }
}
