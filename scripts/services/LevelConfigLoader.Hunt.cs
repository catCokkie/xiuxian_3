using System.Collections.Generic;
using System.Linq;

namespace Xiuxian.Scripts.Services;

public partial class LevelConfigLoader
{
    public bool HuntUnlocked => DungeonHuntRules.IsUnlocked(ServiceLocator.Instance?.PlayerProgressState?.RealmLevel ?? 1,
        GetBossDefeatedLevelIds().Contains("lv_qi_001"));
    public string HuntTarget(string levelId = "")
    {
        if (levelId == "") levelId = ActiveLevelId;
        string id = ServiceLocator.Instance?.PlayerActionState?.GetDungeonHuntTarget(levelId) ?? "";
        if (id == "") return "";
        return HuntUnlocked && GetUnlockedLevelIds().Contains(levelId) && GetHuntTargets(levelId).Contains(id) ? id : "";
    }
    public string[] GetHuntTargets(string levelId = "") => GetSpawnMonsterIds(levelId)
        .Where(id => id != GetBossMonsterId(levelId) && TryGetMonsterStatProfile(id, out var profile) && !profile.IsBoss).ToArray();
    public bool TrySetHuntTarget(string monsterId)
    {
        if (monsterId != "" && (!HuntUnlocked || !GetHuntTargets().Contains(monsterId))) return false;
        ServiceLocator.Instance!.PlayerActionState!.SetDungeonHuntTarget(ActiveLevelId, monsterId); return true;
    }
    public double HuntExploreRate(string levelId = "") => HuntTarget(levelId) == "" ? 1 : DungeonHuntRules.ExploreRate;
    internal int HuntWeight(string levelId, string monsterId, int weight) => DungeonHuntRules.Weight(weight, HuntTarget(levelId) == monsterId);
    public Dictionary<string, double> GetMonsterMaterialAverages(string monsterId, string levelId = "")
        => _offlineProjectionService.BuildOfflineAverageItemDrops(levelId == "" ? ActiveLevelId : levelId, monsterId);
    public string BestHuntForMaterial(string itemId) => GetHuntTargets()
        .Select(id => (Id: id, Amount: GetMonsterMaterialAverages(id).GetValueOrDefault(itemId)))
        .Where(x => x.Amount > 0).OrderByDescending(x => x.Amount).Select(x => x.Id).FirstOrDefault() ?? "";
}
