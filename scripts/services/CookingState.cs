using Godot;
using System;

namespace Xiuxian.Scripts.Services
{
    public partial class CookingState : RecipeProgressState
    {
        public string ActiveMealId { get; private set; } = "";
        public double ActiveMealRemainingSeconds { get; private set; }
        public MealRules.MealSpec ActiveMeal => ActiveMealRemainingSeconds > 0 ? MealRules.Get(ActiveMealId) : default;
        public MealUsagePlan MealUsage { get; } = new();
        public string PlannedMealId { get; private set; } = "";
        private readonly Godot.Collections.Dictionary<string, Variant> _returnedDurations = new();
        public bool TryActivateMeal(string itemId, bool continuous = false, int portions = 1)
        {
            var services = ServiceLocator.Instance;
            var spec = MealRules.Get(itemId);
            int mastery = services?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeCooking) ?? 1;
            if (string.IsNullOrEmpty(spec.Id) || mastery < spec.RequiredMastery) return false;
            if (ActiveMealId == spec.Id && ActiveMealRemainingSeconds > 0)
            {
                // Applying a budget to the current portion must not consume or refresh it.
                MealUsage.Configure(continuous, portions, true);
                PlannedMealId = spec.Id;
                NotifyProductionChanged();
                return true;
            }
            if (services?.BackpackState?.RemoveItem(spec.Id, 1) != true) return false;
            if (ActiveMealRemainingSeconds > 0)
            {
                services.BackpackState.AddItem(ActiveMealId, 1);
                _returnedDurations[ActiveMealId] = ActiveMealRemainingSeconds;
            }
            ActiveMealId = spec.Id;
            PlannedMealId = spec.Id;
            ActiveMealRemainingSeconds = _returnedDurations.ContainsKey(spec.Id) ? _returnedDurations[spec.Id].AsDouble() : MealRules.Duration(spec.Id, mastery);
            _returnedDurations.Remove(spec.Id);
            MealUsage.Configure(continuous, portions, true);
            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
            return true;
        }
        public void StopMealRenewal()
        {
            MealUsage.StopRenewal();
            NotifyProductionChanged();
        }
        public void AdvanceMeal(double activeSeconds)
        {
            if (ActiveMealRemainingSeconds <= 0 || activeSeconds <= 0) return;
            var services = ServiceLocator.Instance;
            int available = MealUsage.AllowedRefills(services?.BackpackState?.GetSpendableItemCount(ActiveMealId) ?? 0);
            var tick = MealRules.Advance(ActiveMealRemainingSeconds, activeSeconds, available,
                MealRules.Duration(ActiveMealId, services?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeCooking) ?? 1));
            if (tick.RefillsConsumed > 0 && services?.BackpackState?.RemoveItem(ActiveMealId, tick.RefillsConsumed) != true)
                tick = MealRules.Advance(ActiveMealRemainingSeconds, activeSeconds, 0, 0);
            MealUsage.RecordPortions(tick.RefillsConsumed);
            ActiveMealRemainingSeconds = tick.Remaining;
            if (ActiveMealRemainingSeconds <= 0) ActiveMealId = "";
            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
        }
        public override Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            var data = base.ToDictionary();
            data["active_meal_id"] = ActiveMealId;
            data["active_meal_remaining_seconds"] = ActiveMealRemainingSeconds;
            data["returned_meal_durations"] = _returnedDurations.Duplicate();
            data["planned_meal_id"] = PlannedMealId;
            data["meal_usage"] = SaveValueConversionRules.ToVariantDictionary(MealUsage.ToPlainDictionary());
            return data;
        }
        public override void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            base.FromDictionary(data);
            ActiveMealId = data.ContainsKey("active_meal_id") ? MealRules.NormalizeId(data["active_meal_id"].AsString()) : "";
            ActiveMealRemainingSeconds = data.ContainsKey("active_meal_remaining_seconds") ? Math.Max(0, data["active_meal_remaining_seconds"].AsDouble()) : 0;
            if (!double.IsFinite(ActiveMealRemainingSeconds) || string.IsNullOrEmpty(MealRules.Get(ActiveMealId).Id)) ActiveMealRemainingSeconds = 0;
            if (ActiveMealRemainingSeconds <= 0) ActiveMealId = "";
            PlannedMealId = data.ContainsKey("planned_meal_id") ? MealRules.NormalizeId(data["planned_meal_id"].AsString()) : ActiveMealId;
            if (string.IsNullOrEmpty(MealRules.Get(PlannedMealId).Id)) PlannedMealId = ActiveMealId;
            MealUsage.Restore(data.TryGetValue("meal_usage", out var usage) && usage.VariantType == Variant.Type.Dictionary
                ? SaveValueConversionRules.ToPlainDictionary((Godot.Collections.Dictionary<string, Variant>)usage) : null, ActiveMealRemainingSeconds > 0);
            var returned = data.ContainsKey("returned_meal_durations")
                ? ((Godot.Collections.Dictionary<string, Variant>)data["returned_meal_durations"]).Duplicate()
                : new Godot.Collections.Dictionary<string, Variant>();
            _returnedDurations.Clear();
            foreach (var item in returned)
            {
                string id = MealRules.NormalizeId(item.Key);
                double remaining = item.Value.AsDouble();
                if (!string.IsNullOrEmpty(MealRules.Get(id).Id) && double.IsFinite(remaining) && remaining > 0)
                    _returnedDurations[id] = remaining;
            }
        }
    }
}
