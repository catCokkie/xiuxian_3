using System.Collections.Generic;
using System.Linq;

namespace Xiuxian.Scripts.Services
{
    public static class RealmBreakthroughRules
    {
        public const int LayersPerRealm = 9;
        // V1 only contains the Qi-refining region chain. Later major gates need their own content.
        public static bool CanCrossRealm(int currentLevel, IEnumerable<string> clearedRegions)
            => currentLevel != LayersPerRealm || clearedRegions.Contains("lv_qi_005");
        public static string DisplayName(int level) => level <= LayersPerRealm ? $"炼气{level}层" : $"筑基{level - LayersPerRealm}层";
    }
}
