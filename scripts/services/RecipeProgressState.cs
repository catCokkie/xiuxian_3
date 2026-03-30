using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class RecipeProgressState : Node, IDictionaryPersistable, IRecipeProgressState
    {
        [Signal]
        public delegate void RecipeProgressChangedEventHandler(string selectedRecipeId, float currentProgress, float requiredProgress);

        public string SelectedRecipeId { get; private set; } = string.Empty;
        public float CurrentProgress { get; private set; }
        public float RequiredProgress { get; private set; } = 100.0f;

        public bool HasSelectedRecipe => !string.IsNullOrEmpty(SelectedRecipeId);

        public void SelectRecipe(string recipeId)
        {
            IRecipeDefinition? recipe = ActivityRegistry.GetRecipe(recipeId);
            if (recipe == null)
            {
                SelectedRecipeId = string.Empty;
                CurrentProgress = 0.0f;
                RequiredProgress = 100.0f;
            }
            else
            {
                SelectedRecipeId = recipe.RecipeId;
                CurrentProgress = 0.0f;
                RequiredProgress = recipe.RequiredInputEvents;
            }

            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }

        public bool AdvanceProgress(int inputEvents)
        {
            if (!HasSelectedRecipe)
            {
                return false;
            }

            Xiuxian.Scripts.Game.CraftingProgressionService.GenericProgressResult result = Xiuxian.Scripts.Game.CraftingProgressionService.AdvanceGenericRecipe(SelectedRecipeId, CurrentProgress, inputEvents);
            bool completed = result.CompletedBatch;
            CurrentProgress = completed ? 0.0f : RequiredProgress * result.ProgressPercent / 100.0f;
            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
            return completed;
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
            RequiredProgress = data.ContainsKey("required_progress") ? Mathf.Max(1.0f, (float)data["required_progress"].AsDouble()) : 100.0f;
            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }
    }
}
