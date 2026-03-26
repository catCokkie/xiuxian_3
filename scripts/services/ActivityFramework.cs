using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public enum ActivityCategory
    {
        Gathering,
        Processing,
        Cultivation,
    }

    public readonly record struct MaterialCost(string ItemId, int Count);
    public readonly record struct MaterialOutput(string ItemId, int Count);

    /// <summary>
    /// A single recipe within an activity system (e.g. one alchemy recipe, one smithing tier).
    /// </summary>
    public interface IRecipeDefinition
    {
        string RecipeId { get; }
        string SystemId { get; }
        string DisplayName { get; }
        IReadOnlyList<MaterialCost> Inputs { get; }
        double LingqiCost { get; }
        int RequiredInputEvents { get; }
        IReadOnlyList<MaterialOutput> Outputs { get; }
        int RequiredMasteryLevel { get; }
    }

    /// <summary>
    /// Describes one activity system (e.g. alchemy, smithing, garden).
    /// </summary>
    public interface IActivityDefinition
    {
        string SystemId { get; }
        string DisplayName { get; }
        ActivityCategory Category { get; }
        bool SupportsOffline { get; }
        double OfflineEfficiency { get; }
        IReadOnlyList<IRecipeDefinition> GetRecipes();
    }

    public sealed class SimpleRecipeDefinition : IRecipeDefinition
    {
        public string RecipeId { get; init; } = string.Empty;
        public string SystemId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public IReadOnlyList<MaterialCost> Inputs { get; init; } = System.Array.Empty<MaterialCost>();
        public double LingqiCost { get; init; }
        public int RequiredInputEvents { get; init; }
        public IReadOnlyList<MaterialOutput> Outputs { get; init; } = System.Array.Empty<MaterialOutput>();
        public int RequiredMasteryLevel { get; init; }
    }

    public sealed class SimpleActivityDefinition : IActivityDefinition
    {
        public string SystemId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public ActivityCategory Category { get; init; }
        public bool SupportsOffline { get; init; }
        public double OfflineEfficiency { get; init; } = 1.0;

        private readonly List<IRecipeDefinition> _recipes = new();
        public IReadOnlyList<IRecipeDefinition> GetRecipes() => _recipes;

        public void AddRecipe(IRecipeDefinition recipe) => _recipes.Add(recipe);
    }
}
