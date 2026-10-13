namespace Xiuxian.Scripts.Services
{
    public interface IRecipeProgressState
    {
        CraftingProductionPlan Production { get; }
        string SelectedRecipeId { get; }
        float CurrentProgress { get; }
        float RequiredProgress { get; }
        bool HasSelectedRecipe { get; }

        void SelectRecipe(string recipeId);
        bool AdvanceProgress(int inputEvents);
        void CompleteBatch();
    }
}
