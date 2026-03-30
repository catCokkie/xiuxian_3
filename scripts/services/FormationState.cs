using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public partial class FormationState : Node, IDictionaryPersistable, IRecipeProgressState
    {
        [Signal]
        public delegate void RecipeProgressChangedEventHandler(string selectedRecipeId, float currentProgress, float requiredProgress);

        public string SelectedRecipeId { get; private set; } = string.Empty;
        public float CurrentProgress { get; private set; }
        public float RequiredProgress { get; private set; } = 100.0f;
        public string ActivePrimaryId { get; private set; } = string.Empty;
        public string ActiveSecondaryId { get; private set; } = string.Empty;

        private readonly List<string> _craftedIds = new();
        private readonly Dictionary<string, int> _inventory = new(System.StringComparer.Ordinal);

        public bool HasSelectedRecipe => !string.IsNullOrEmpty(SelectedRecipeId);
        public IReadOnlyList<string> CraftedIds => _craftedIds;
        public IReadOnlyDictionary<string, int> Inventory => _inventory;

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

        public void AddCraftedFormation(string formationId, int count = 1)
        {
            if (string.IsNullOrEmpty(formationId) || count <= 0)
            {
                return;
            }

            if (!_craftedIds.Contains(formationId))
            {
                _craftedIds.Add(formationId);
            }

            _inventory[formationId] = _inventory.TryGetValue(formationId, out int existing) ? existing + count : count;
        }

        public bool TryActivatePrimary(string formationId)
        {
            if (string.IsNullOrEmpty(formationId) || !_inventory.TryGetValue(formationId, out int count) || count <= 0)
            {
                return false;
            }

            ActivePrimaryId = formationId;
            return true;
        }

        public void SetActiveSecondary(string formationId)
        {
            ActiveSecondaryId = formationId;
        }

        public bool TryActivateSecondary(string formationId)
        {
            if (string.IsNullOrEmpty(formationId) || !_inventory.TryGetValue(formationId, out int count) || count <= 0)
            {
                return false;
            }

            ActiveSecondaryId = formationId;
            return true;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return FormationPersistenceRules.ToDictionary(new FormationPersistenceRules.FormationSnapshot(
                SelectedRecipeId,
                CurrentProgress,
                RequiredProgress,
                ActivePrimaryId,
                ActiveSecondaryId,
                _craftedIds,
                _inventory));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            FormationPersistenceRules.FormationSnapshot snapshot = FormationPersistenceRules.FromDictionary(data);
            SelectedRecipeId = snapshot.SelectedRecipeId;
            CurrentProgress = snapshot.Progress;
            RequiredProgress = snapshot.RequiredProgress;
            ActivePrimaryId = snapshot.ActivePrimaryId;
            ActiveSecondaryId = snapshot.ActiveSecondaryId;

            _craftedIds.Clear();
            _craftedIds.AddRange(snapshot.CraftedIds);
            _inventory.Clear();
            foreach ((string formationId, int count) in snapshot.Inventory)
            {
                _inventory[formationId] = count;
            }

            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }
    }
}
