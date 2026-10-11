using Godot;
using System;

namespace Xiuxian.Scripts.Services
{
    public partial class CookingState : RecipeProgressState
    {
        public string ActiveMealId { get; private set; } = "";
        public double ActiveMealRemainingSeconds { get; private set; }
        public MealRules.MealSpec ActiveMeal => ActiveMealRemainingSeconds > 0 ? MealRules.Get(ActiveMealId) : default;
        private readonly Godot.Collections.Dictionary<string, Variant> _returnedDurations = new();
        public bool TryActivateMeal(string itemId)
        {
            var services = ServiceLocator.Instance;
            var spec = MealRules.Get(itemId);
            int mastery = services?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeCooking) ?? 1;
            if (string.IsNullOrEmpty(spec.Id) || mastery < spec.RequiredMastery || services?.BackpackState?.RemoveItem(itemId, 1) != true) return false;
            if (ActiveMealRemainingSeconds > 0)
            {
                services.BackpackState.AddItem(ActiveMealId, 1);
                _returnedDurations[ActiveMealId] = ActiveMealRemainingSeconds;
            }
            ActiveMealId = spec.Id;
            ActiveMealRemainingSeconds = _returnedDurations.ContainsKey(spec.Id) ? _returnedDurations[spec.Id].AsDouble() : MealRules.Duration(spec.Id, mastery);
            _returnedDurations.Remove(spec.Id);
            EmitSignal(SignalName.RecipeProgressChanged, SelectedRecipeId, CurrentProgress, RequiredProgress);
            return true;
        }
        public void AdvanceMeal(double activeSeconds)
        {
            if (ActiveMealRemainingSeconds <= 0 || activeSeconds <= 0) return;
            var services = ServiceLocator.Instance;
            int available = services?.BackpackState?.GetItemCount(ActiveMealId) ?? 0;
            var tick = MealRules.Advance(ActiveMealRemainingSeconds, activeSeconds, available,
                MealRules.Duration(ActiveMealId, services?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeCooking) ?? 1));
            if (tick.RefillsConsumed > 0) services?.BackpackState?.RemoveItem(ActiveMealId, tick.RefillsConsumed);
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
            return data;
        }
        public override void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            base.FromDictionary(data);
            ActiveMealId = data.ContainsKey("active_meal_id") ? MealRules.NormalizeId(data["active_meal_id"].AsString()) : "";
            ActiveMealRemainingSeconds = data.ContainsKey("active_meal_remaining_seconds") ? Math.Max(0, data["active_meal_remaining_seconds"].AsDouble()) : 0;
            var returned = data.ContainsKey("returned_meal_durations")
                ? ((Godot.Collections.Dictionary<string, Variant>)data["returned_meal_durations"]).Duplicate()
                : new Godot.Collections.Dictionary<string, Variant>();
            _returnedDurations.Clear();
            foreach (var item in returned) _returnedDurations[item.Key] = item.Value;
        }
    }
}
