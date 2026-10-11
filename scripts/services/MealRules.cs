using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class MealRules
    {
        public readonly record struct MealSpec(string Id, string Name, int RequiredMastery, double Seconds,
            double LingqiRate = 0, double InsightRate = 0, int ExtraOutput = 0, double DefenseRate = 0, int HealPerRound = 0);
        public static readonly MealSpec[] Meals =
        {
            new("food_clear_fish_soup", "清心鱼汤", 1, 1800, InsightRate: .20),
            new("food_spirit_fish_slices", "化灵鱼脍", 2, 1800, LingqiRate: .15),
            new("food_focus_fish_stew", "凝神鱼羹", 4, 2700, ExtraOutput: 1),
            new("food_iron_fish_meal", "铁壁鱼膳", 4, 3600, DefenseRate: .10, HealPerRound: 5),
        };
        public static string NormalizeId(string id) => id switch
        {
            "food_spirit_porridge" => Meals[0].Id, "food_fruit_jelly" => Meals[1].Id,
            "food_dragon_soup" => Meals[2].Id, _ => id
        };
        public static MealSpec Get(string id)
        {
            foreach (var meal in Meals) if (meal.Id == NormalizeId(id)) return meal;
            return default;
        }
        public static double Duration(string id, int mastery) => Get(id).Seconds * (mastery >= 3 ? 1.33 : 1);
        public readonly record struct TickResult(double Remaining, int RefillsConsumed);
        public static TickResult Advance(double remaining, double activeSeconds, int inventory, double duration)
        {
            double next = remaining - Math.Max(0, activeSeconds);
            int consumed = 0;
            while (next <= 0 && consumed < Math.Max(0, inventory) && duration > 0) { next += duration; consumed++; }
            return new(Math.Max(0, next), consumed);
        }
    }
}
