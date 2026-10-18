using System.Collections.Generic;

namespace Xiuxian.Scripts.Services;

public static class SuppressionRules
{
    public const string PotionId = "potion_yangyuan_dan";
    public const int Rounds = 5;
    public static string Normalize(string choice) => choice is "potion" or "none" ? choice : "talisman";
    public static string Recipe(string choice) => Normalize(choice) switch { "potion" => RecipeUnlockRules.Yangyuan, "none" => "", _ => RecipeUnlockRules.Zhenling };
    public static bool ShouldUsePotion(string choice, bool start, int healing, bool alreadySuppressed, int owned)
        => Normalize(choice) == "potion" && start && healing > 0 && !alreadySuppressed && owned > 0;
    public static int Window(IEnumerable<string> talismans, bool potionUsed)
        => System.Math.Max(TalismanRules.GetSuppressionRounds(talismans), potionUsed ? Rounds : 0);
}
