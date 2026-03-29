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
        public bool HasSelectedNode => !string.IsNullOrEmpty(SelectedRecipeId);

        public void SelectNode(string recipeId)
        {
            if (!MiningRules.TryGetNode(recipeId, out MiningRules.NodeSpec node))
            {
                SelectedRecipeId = string.Empty;
                CurrentProgress = 0.0f;
                RequiredProgress = 180.0f;
                CurrentDurability = MiningRules.DefaultNodeDurability;
            }
            else
            {
                SelectedRecipeId = node.RecipeId;
                CurrentProgress = 0.0f;
                RequiredProgress = node.RequiredInputs;
                CurrentDurability = MiningRules.DefaultNodeDurability;
            }

            EmitSignal(SignalName.MiningChanged, SelectedRecipeId, CurrentProgress, RequiredProgress, CurrentDurability);
        }

        public bool AdvanceProgress(int inputEvents)
        {
            if (!HasSelectedNode)
            {
                return false;
            }

            MiningRules.MiningProgressDecision decision = MiningRules.AdvanceProgress(CurrentProgress, inputEvents, Mathf.RoundToInt(RequiredProgress), CurrentDurability);
            CurrentProgress = decision.NextProgress;
            CurrentDurability = decision.NextDurability;
            EmitSignal(SignalName.MiningChanged, SelectedRecipeId, CurrentProgress, RequiredProgress, CurrentDurability);
            return decision.CompletedBatch;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return MiningPersistenceRules.ToDictionary(new MiningPersistenceRules.MiningSnapshot(SelectedRecipeId, CurrentProgress, RequiredProgress, CurrentDurability));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            MiningPersistenceRules.MiningSnapshot snapshot = MiningPersistenceRules.FromDictionary(data);
            SelectedRecipeId = snapshot.SelectedRecipeId;
            CurrentProgress = snapshot.Progress;
            RequiredProgress = snapshot.RequiredProgress;
            CurrentDurability = snapshot.CurrentDurability;
            EmitSignal(SignalName.MiningChanged, SelectedRecipeId, CurrentProgress, RequiredProgress, CurrentDurability);
        }
    }
}
