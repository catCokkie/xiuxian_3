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

        private readonly Godot.Collections.Dictionary<string, Variant> _capacities = new();
        private readonly Godot.Collections.Dictionary<string, Variant> _refreshUnix = new();
        public bool CanSelectPond(string id)
        {
            int mastery = ServiceLocator.Instance?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeFishing) ?? 1;
            string permit = id switch { "fishing_spirit_pearl" => ShopRules.ItemFishingPermitYoutan, "fishing_deep_pond" => ShopRules.ItemFishingPermitLongxianyuan, "fishing_hualong_pond" => ShopRules.ItemFishingPermitHualongpo, _ => "" };
            return FishingRules.CanFishPond(id, mastery) && (permit == "" || (ServiceLocator.Instance?.ShopState?.GetLifetimePurchaseCount(permit) ?? 0) > 0);
        }
        public int RemainingCapacity
        {
            get
            {
                if (!HasSelectedPond) return 0;
                int mastery = ServiceLocator.Instance?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeFishing) ?? 1;
                int maximum = FishingRules.Capacity(SelectedRecipeId, mastery);
                if (!_capacities.ContainsKey(SelectedRecipeId)) _capacities[SelectedRecipeId] = maximum;
                if (_refreshUnix.ContainsKey(SelectedRecipeId) && Time.GetUnixTimeFromSystem() >= _refreshUnix[SelectedRecipeId].AsInt64())
                { _capacities[SelectedRecipeId] = maximum; _refreshUnix.Remove(SelectedRecipeId); }
                return _capacities[SelectedRecipeId].AsInt32();
            }
        }

        public void SelectPond(string recipeId)
        {
            if (!CanSelectPond(recipeId)) return;
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
            if (!HasSelectedPond || !CanSelectPond(SelectedRecipeId) || RemainingCapacity <= 0) return false;
            double inputRate = 1.0;
            int mastery = ServiceLocator.Instance?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeFishing) ?? 1;
            double speed = SubsystemMasteryRules.GetEffectValue(PlayerActionState.ModeFishing, mastery, SubsystemMasteryRules.FishingSpeedBonusEffectId);
            inputRate = 1.0 / (1.0 - speed);
            CurrentProgress = BatchProgressRules.Advance(CurrentProgress, inputEvents, RequiredProgress, inputRate);
            EmitSignal(SignalName.FishingChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
            return CurrentProgress >= RequiredProgress;
        }

        public void CompleteBatch()
        {
            if (CurrentProgress < RequiredProgress) return;
            CurrentProgress = BatchProgressRules.Consume(CurrentProgress, RequiredProgress);
            _capacities[SelectedRecipeId] = System.Math.Max(0, RemainingCapacity - 1);
            if (_capacities[SelectedRecipeId].AsInt32() == 0) _refreshUnix[SelectedRecipeId] = (long)Time.GetUnixTimeFromSystem() + 1800;
            EmitSignal(SignalName.FishingChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            var data = FishingPersistenceRules.ToDictionary(new FishingPersistenceRules.FishingSnapshot(SelectedRecipeId, CurrentProgress, RequiredProgress));
            data["pond_capacity"] = _capacities.Duplicate(); data["pond_refresh_unix"] = _refreshUnix.Duplicate();
            return data;
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            _capacities.Clear(); _refreshUnix.Clear();
            if (data.ContainsKey("pond_capacity")) foreach (var pair in (Godot.Collections.Dictionary<string, Variant>)data["pond_capacity"]) _capacities[pair.Key] = System.Math.Max(0, pair.Value.AsInt32());
            if (data.ContainsKey("pond_refresh_unix")) foreach (var pair in (Godot.Collections.Dictionary<string, Variant>)data["pond_refresh_unix"]) _refreshUnix[pair.Key] = pair.Value;
            FishingPersistenceRules.FishingSnapshot snapshot = FishingPersistenceRules.FromDictionary(data);
            SelectedRecipeId = snapshot.SelectedRecipeId;
            CurrentProgress = snapshot.Progress;
            RequiredProgress = snapshot.RequiredProgress;
            EmitSignal(SignalName.FishingChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }
    }
}
