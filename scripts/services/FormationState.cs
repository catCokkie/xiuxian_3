using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public partial class FormationState : Node, IDictionaryPersistable, IRecipeProgressState
    {
        [Signal]
        public delegate void RecipeProgressChangedEventHandler(string selectedRecipeId, float currentProgress, float requiredProgress);

        public string SelectedRecipeId { get; private set; } = string.Empty;
        public CraftingProductionPlan Production { get; } = new();
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
            if (!Production.CanProduce) return false;
            double inputRate = 1.0;
            CurrentProgress = BatchProgressRules.Advance(CurrentProgress, inputEvents, RequiredProgress, inputRate);
            CurrentProgress = Production.BoundProgress(CurrentProgress, RequiredProgress);
            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
            return CurrentProgress >= RequiredProgress;
        }

        public void CompleteBatch()
        {
            if (CurrentProgress < RequiredProgress || !Production.CanProduce) return;
            CurrentProgress = BatchProgressRules.Consume(CurrentProgress, RequiredProgress);
            Production.CommitBatch();
            if (Production.Finished) CurrentProgress = 0;
            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
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
            var data = FormationPersistenceRules.ToDictionary(new FormationPersistenceRules.FormationSnapshot(
                SelectedRecipeId,
                CurrentProgress,
                RequiredProgress,
                ActivePrimaryId,
                ActiveSecondaryId,
                _craftedIds,
                _inventory));
            Production.WriteTo(data);
            return data;
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            FormationPersistenceRules.FormationSnapshot snapshot = FormationPersistenceRules.FromDictionary(data);
            SelectedRecipeId = snapshot.SelectedRecipeId;
            Production.ReadFrom(data, HasSelectedRecipe);
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
