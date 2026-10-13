using System.Collections.Generic;
using System.Linq;

namespace Xiuxian.Scripts.Services
{
    public static class RealmBreakthroughRules
    {
        public const int LayersPerRealm = 9;
        public const int ChapterEndLevel = 10;
        public const string FinalRegionId = "lv_qi_005";
        public static bool IsAtContentLimit(int level) => level >= ChapterEndLevel;
        public static bool MeetsRegionalGate(int currentLevel, IEnumerable<string> clearedRegions)
            => currentLevel != LayersPerRealm || clearedRegions.Contains(FinalRegionId);
        public static bool IsChapterComplete(int level, IEnumerable<string> clearedRegions)
            => IsAtContentLimit(level) && clearedRegions.Contains(FinalRegionId);
        // V1 only contains the Qi-refining region chain. Later major gates need their own content.
        public static bool CanCrossRealm(int currentLevel, IEnumerable<string> clearedRegions)
            => !IsAtContentLimit(currentLevel) && MeetsRegionalGate(currentLevel, clearedRegions);
        public static string DisplayName(int level) => level <= LayersPerRealm ? $"炼气{level}层" : $"筑基{level - LayersPerRealm}层";
    }
}
