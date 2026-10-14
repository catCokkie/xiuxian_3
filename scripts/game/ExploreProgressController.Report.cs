using Godot;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    public partial class ExploreProgressController
    {
        private readonly BossChallengeAttempt _bossAttempt = new();
        private void CaptureBossReport(string result)
        {
            if (!IsBossChallengeBattle()) return;
            string level = _levelConfigLoader!.ActiveLevelId;
            if (_bossAttempt.LevelId != level || _bossAttempt.BossId != _battleMonsterId) _bossAttempt.Begin(level, _battleMonsterId, false);
            ServiceLocator.Instance?.ChallengeGoalState?.RecordBossReport(_bossAttempt.Finish(result, _battleRoundCounter, _enemyHp, _enemyMaxHp, (long)Time.GetUnixTimeFromSystem()));
            _bossAttempt.Clear();
        }
        private void RestoreBossAttempt(Godot.Collections.Dictionary<string, Variant> data)
        {
            if (!_inBattle || !IsBossChallengeBattle()) { _bossAttempt.Clear(); return; }
            var saved = data.TryGetValue("boss_attempt", out var raw) && raw.VariantType == Variant.Type.Dictionary
                ? SaveValueConversionRules.ToPlainDictionary((Godot.Collections.Dictionary<string, Variant>)raw) : new Dictionary<string, object>();
            _bossAttempt.Restore(saved, _levelConfigLoader!.ActiveLevelId, _battleMonsterId);
        }
    }
}
