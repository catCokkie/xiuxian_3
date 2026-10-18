using System;

namespace Xiuxian.Scripts.Services;

public static class DungeonHuntRules
{
    public const double ExploreRate = .8;
    public static bool IsUnlocked(int realm, bool firstRegionCleared) => realm >= 3 && firstRegionCleared;
    public static int Weight(int weight, bool targeted) => (int)Math.Min(int.MaxValue,
        Math.Max(0L, weight) * (targeted ? 2L : 1L));
}
