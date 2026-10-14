using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>A single explicit challenge purpose; inventory and production remain independent.</summary>
    public sealed class ChallengeGoalPlan
    {
        public static readonly IReadOnlyList<string> Recipes = Array.AsReadOnly(new[] { "recipe_huiqi_dan", "talisman_fire_charm", "talisman_shield_charm" });
        public string LevelId { get; private set; } = "";
        public string BossId { get; private set; } = "";
        public bool Completed { get; private set; }
        public bool HasGoal => LevelId != "" && BossId != "";
        private readonly Dictionary<string, int> _targets = new(StringComparer.Ordinal);
        public int GetTarget(string recipeId) => _targets.GetValueOrDefault(recipeId);
        public bool Start(string levelId, string bossId, IReadOnlyDictionary<string, int> targets)
        {
            if (string.IsNullOrWhiteSpace(levelId) || string.IsNullOrWhiteSpace(bossId)) return false;
            if (HasGoal && !Completed && LevelId == levelId && BossId == bossId) return false;
            LevelId = levelId; BossId = bossId; Completed = false; _targets.Clear();
            foreach (string recipe in Recipes) _targets[recipe] = Math.Clamp(targets.GetValueOrDefault(recipe), 0, 99);
            return true;
        }
        public bool SetTarget(string recipeId, int count)
        {
            if (!HasGoal || Completed || !System.Linq.Enumerable.Contains(Recipes, recipeId)) return false;
            count = Math.Clamp(count, 0, 99);
            if (GetTarget(recipeId) == count) return false;
            _targets[recipeId] = count; return true;
        }
        public bool SkipPreparation()
        {
            bool changed = false;
            foreach (string recipe in Recipes) changed |= SetTarget(recipe, 0);
            return changed;
        }
        public bool CompleteVictory(string levelId, string bossId)
        {
            if (!HasGoal || Completed || levelId != LevelId || bossId != BossId) return false;
            Completed = true; return true;
        }
        public bool Cancel()
        {
            if (!HasGoal) return false;
            LevelId = BossId = ""; Completed = false; _targets.Clear(); return true;
        }
        public Dictionary<string, object> ToPlainDictionary()
        {
            var targets = new Dictionary<string, object>();
            foreach (var pair in _targets) targets[pair.Key] = pair.Value;
            return new() { ["level_id"] = LevelId, ["boss_id"] = BossId, ["completed"] = Completed, ["stock_targets"] = targets };
        }
        public void Restore(IDictionary<string, object> data)
        {
            LevelId = ReadId(data, "level_id"); BossId = ReadId(data, "boss_id"); _targets.Clear();
            Completed = HasGoal && data.TryGetValue("completed", out var complete) && complete is bool flag && flag;
            if (!HasGoal) { LevelId = BossId = ""; return; }
            if (data.TryGetValue("stock_targets", out var raw) && raw is IDictionary<string, object> targets)
                foreach (string recipe in Recipes)
                {
                    int count = 0;
                    try { if (targets.TryGetValue(recipe, out var value)) count = Convert.ToInt32(value); }
                    catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { }
                    _targets[recipe] = Math.Clamp(count, 0, 99);
                }
        }
        private static string ReadId(IDictionary<string, object> data, string key)
            => data.TryGetValue(key, out var value) && value is string id && !string.IsNullOrWhiteSpace(id) && id.Length <= 128 ? id : "";
    }
}
