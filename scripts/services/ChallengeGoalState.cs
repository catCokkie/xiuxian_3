using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Xiuxian.Scripts.Services
{
    public partial class ChallengeGoalState : Node, IDictionaryPersistable
    {
        [Signal] public delegate void GoalChangedEventHandler();
        public ChallengeGoalPlan Goal { get; } = new();
        public BossChallengeReport? LastBossReport { get; private set; }
        public int LoadRevision { get; private set; }
        public bool TryStart(string levelId, string bossId, IReadOnlyDictionary<string, int> targets)
        {
            var loader = ServiceLocator.Instance?.LevelConfigLoader;
            if (loader == null || !loader.GetUnlockedLevelIds().Contains(levelId) || loader.GetBossMonsterId(levelId) != bossId
                || !loader.TryGetMonsterStatProfile(bossId, out _) || !Goal.Start(levelId, bossId, targets)) return false;
            EmitSignal(SignalName.GoalChanged); return true;
        }
        public void SetTarget(string recipeId, int count) { if (Goal.SetTarget(recipeId, count)) EmitSignal(SignalName.GoalChanged); }
        public void SkipPreparation() { if (Goal.SkipPreparation()) EmitSignal(SignalName.GoalChanged); }
        public void Cancel() { if (Goal.Cancel()) EmitSignal(SignalName.GoalChanged); }
        public void RecordBossVictory(string levelId, string bossId) { if (Goal.CompleteVictory(levelId, bossId)) EmitSignal(SignalName.GoalChanged); }
        public void RecordBossReport(BossChallengeReport report) { LastBossReport = report; EmitSignal(SignalName.GoalChanged); }
        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            var data = Goal.ToPlainDictionary(); data["last_boss_report"] = LastBossReport?.ToPlainDictionary() ?? new Dictionary<string, object>();
            return SaveValueConversionRules.ToVariantDictionary(data);
        }
        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            var plain = SaveValueConversionRules.ToPlainDictionary(data);
            Goal.Restore(plain);
            LastBossReport = plain.TryGetValue("last_boss_report", out var report) && report is IDictionary<string, object> fields ? BossChallengeReport.Restore(fields) : null;
            LoadRevision++;
        }
    }
}
