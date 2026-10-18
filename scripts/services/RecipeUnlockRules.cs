using System;
using System.Collections.Generic;
using System.Linq;

namespace Xiuxian.Scripts.Services
{
    public static class RecipeUnlockRules
    {
        public const string Recycle = "deep_mining_recycle";
        public const string Zhenling = "talisman_zhenling";
        public const string Yangyuan = "recipe_yangyuan_dan";
        public static int RequiredRealm(string recipeId, string system) => Math.Max(
            ProgressiveUnlockRules.GetRequiredRealmLevel(system), recipeId == Recycle ? 3 : recipeId is Zhenling or Yangyuan ? 4 : 1);
        public static string RequiredRegion(string recipeId) => recipeId is Zhenling or Yangyuan ? "lv_qi_003" : "";
        public static bool HasRegion(string recipeId, IEnumerable<string>? cleared) => RequiredRegion(recipeId) == ""
            || cleared?.Contains(RequiredRegion(recipeId)) == true;
        public static bool Available(IRecipeDefinition recipe, int realm, int mastery, IEnumerable<string>? cleared) =>
            realm >= RequiredRealm(recipe.RecipeId, recipe.SystemId) && mastery >= recipe.RequiredMasteryLevel && HasRegion(recipe.RecipeId, cleared);
        public static string Requirements(IRecipeDefinition recipe) => $"需{RealmBreakthroughRules.DisplayName(RequiredRealm(recipe.RecipeId, recipe.SystemId))}及{UiText.MasterySystemName(recipe.SystemId)}精通Lv{recipe.RequiredMasteryLevel}"
            + (RequiredRegion(recipe.RecipeId) == "" ? "。" : "，并首通第三地区。仅阻断战斗前5回合敌方回血，第6回合恢复；禁疗丹/符只选择一种。");
    }
}
