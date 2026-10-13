using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class RecipeProgressState : Node, IDictionaryPersistable, IRecipeProgressState
    {
        [Signal]
        public delegate void RecipeProgressChangedEventHandler(string selectedRecipeId, float currentProgress, float requiredProgress);

        public string SelectedRecipeId { get; private set; } = string.Empty;
        public CraftingProductionPlan Production { get; } = new();
        public float CurrentProgress { get; private set; }
        public float RequiredProgress { get; private set; } = 100.0f;

        private bool UsesProductionPlan => CraftingProductionPlan.Supports(ActivityRegistry.GetRecipe(SelectedRecipeId)?.SystemId ?? "");

        public bool HasSelectedRecipe => !string.IsNullOrEmpty(SelectedRecipeId);

        public void SelectRecipe(string recipeId)
        {
            if (SelectedRecipeId == (recipeId ?? string.Empty)) return;
            Production.Configure(false);
            Production.Stop();
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
            if (!HasSelectedRecipe) return false;
            if (UsesProductionPlan && !Production.CanProduce) return false;
            double inputRate = 1.0;
            CurrentProgress = BatchProgressRules.Advance(CurrentProgress, inputEvents, RequiredProgress, inputRate);
            if (UsesProductionPlan) CurrentProgress = Production.BoundProgress(CurrentProgress, RequiredProgress);
            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
            return CurrentProgress >= RequiredProgress;
        }

        public void CompleteBatch()
        {
            if (CurrentProgress < RequiredProgress || (UsesProductionPlan && !Production.CanProduce)) return;
            CurrentProgress = BatchProgressRules.Consume(CurrentProgress, RequiredProgress);
            if (UsesProductionPlan)
            {
                Production.CommitBatch();
                if (Production.Finished) CurrentProgress = 0;
            }
            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }

        public virtual Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            var data = new Godot.Collections.Dictionary<string, Variant>
            {
                ["selected_recipe"] = SelectedRecipeId,
                ["progress"] = CurrentProgress,
                ["required_progress"] = RequiredProgress,
            };
            Production.WriteTo(data);
            return data;
        }

        public virtual void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            SelectedRecipeId = data.ContainsKey("selected_recipe") ? data["selected_recipe"].AsString() : string.Empty;
            CurrentProgress = data.ContainsKey("progress") ? Mathf.Max(0.0f, (float)data["progress"].AsDouble()) : 0.0f;
            RequiredProgress = data.ContainsKey("required_progress") ? Mathf.Max(1.0f, (float)data["required_progress"].AsDouble()) : 100.0f;
            Production.ReadFrom(data, HasSelectedRecipe);
            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }
    }
}
