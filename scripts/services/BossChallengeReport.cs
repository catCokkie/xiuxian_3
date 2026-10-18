using System;
using System.Collections.Generic;
using System.Linq;

namespace Xiuxian.Scripts.Services
{
    public readonly record struct BattleRoundObservation(int DamageToMonster, int MonsterHealing, int PlayerHealing);
    public sealed record BossChallengeReport(string LevelId, string BossId, string Result, int Rounds, int EnemyHp, int EnemyMaxHp,
        int DamageToBoss, int EnemyHealing, int PlayerHealing, IReadOnlyDictionary<string, int> Potions,
        IReadOnlyDictionary<string, int> Talismans, bool StatisticsComplete, long TimestampUnix,
        CharacterStatBlock? StartingStats = null, int StartingHp = 0, int RoundLimit = 0)
    {
        public Dictionary<string, object> ToPlainDictionary() => new()
        {
            ["level_id"] = LevelId, ["boss_id"] = BossId, ["result"] = Result, ["rounds"] = Rounds,
            ["enemy_hp"] = EnemyHp, ["enemy_max_hp"] = EnemyMaxHp, ["damage_to_boss"] = DamageToBoss,
            ["enemy_healing"] = EnemyHealing, ["player_healing"] = PlayerHealing,
            ["potions"] = Potions.ToDictionary(x => x.Key, x => (object)x.Value),
            ["talismans"] = Talismans.ToDictionary(x => x.Key, x => (object)x.Value),
            ["statistics_complete"] = StatisticsComplete, ["timestamp_unix"] = TimestampUnix,
            ["starting_stats"] = StartingStats is CharacterStatBlock stats ? new Dictionary<string, object>
                { ["max_hp"] = stats.MaxHp, ["attack"] = stats.Attack, ["defense"] = stats.Defense, ["speed"] = stats.Speed,
                    ["crit_chance"] = stats.CritChance, ["crit_damage"] = stats.CritDamage } : new(),
            ["starting_hp"] = StartingHp, ["round_limit"] = RoundLimit
        };
        public static BossChallengeReport? Restore(IDictionary<string, object> data)
        {
            string level = ReadId(data, "level_id"), boss = ReadId(data, "boss_id"), result = ReadId(data, "result");
            if (level == "" || boss == "" || result is not ("victory" or "death" or "timeout" or "double_knockout")) return null;
            int maxHp = Math.Max(1, ReadCount(data, "enemy_max_hp"));
            return new(level, boss, result, ReadCount(data, "rounds"), Math.Min(maxHp, ReadCount(data, "enemy_hp")), maxHp,
                ReadCount(data, "damage_to_boss"), ReadCount(data, "enemy_healing"), ReadCount(data, "player_healing"),
                ReadItems(data, "potions"), ReadItems(data, "talismans"), ReadComplete(data),
                data.TryGetValue("timestamp_unix", out var timestamp) ? timestamp switch { long unix when unix >= 0 => unix, int unix when unix >= 0 => unix, _ => 0 } : 0,
                ReadStartingStats(data), ReadCount(data, "starting_hp"), ReadCount(data, "round_limit"));
        }
        internal static CharacterStatBlock? ReadStartingStats(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("starting_stats", out var raw) || raw is not IDictionary<string, object> stats
                || !new[] { "max_hp", "attack", "defense", "speed" }.All(key => stats.TryGetValue(key, out var value) && ValidCount(value))
                || ReadCount(stats, "max_hp") <= 0) return null;
            double Ratio(string key, double fallback) => stats.TryGetValue(key, out var value) && value is int or long or double or float
                && double.IsFinite(Convert.ToDouble(value)) ? Math.Clamp(Convert.ToDouble(value), 0, 100) : fallback;
            return new CharacterStatBlock(ReadCount(stats, "max_hp"), ReadCount(stats, "attack"), ReadCount(stats, "defense"), ReadCount(stats, "speed"), Ratio("crit_chance", 0), Ratio("crit_damage", 1.5));
        }
        internal static bool ReadComplete(IDictionary<string, object> data) => data.TryGetValue("statistics_complete", out var flag) && flag is true
            && new[] { "damage_to_boss", "enemy_healing", "player_healing" }.All(key => data.TryGetValue(key, out var raw) && ValidCount(raw))
            && new[] { "potions", "talismans" }.All(key => data.TryGetValue(key, out var raw) && raw is IDictionary<string, object> items && items.Count <= 64
                && items.All(x => x.Key.Length is > 0 and <= 128 && ValidCount(x.Value)));
        private static bool ValidCount(object? raw)
        {
            if (raw is not (int or long or double or float)) return false;
            double count = Convert.ToDouble(raw);
            return double.IsFinite(count) && count >= 0 && count <= int.MaxValue && count == Math.Floor(count);
        }
        internal static int ReadCount(IDictionary<string, object> data, string key)
            => data.TryGetValue(key, out var raw) && ValidCount(raw) ? Convert.ToInt32(raw) : 0;
        internal static string ReadId(IDictionary<string, object> data, string key)
            => data.TryGetValue(key, out var raw) && raw is string id && !string.IsNullOrWhiteSpace(id) && id.Length <= 128 ? id : "";
        internal static Dictionary<string, int> ReadItems(IDictionary<string, object> data, string key)
        {
            var result = new Dictionary<string, int>();
            if (data.TryGetValue(key, out var raw) && raw is IDictionary<string, object> items)
                foreach (var entry in items.Take(64))
                    if (entry.Key.Length is > 0 and <= 128 && ValidCount(entry.Value) && Convert.ToInt32(entry.Value) > 0) result[entry.Key] = Convert.ToInt32(entry.Value);
            return result;
        }
    }
    public sealed class BossChallengeAttempt
    {
        public string LevelId { get; private set; } = "";
        public string BossId { get; private set; } = "";
        public bool StatisticsComplete { get; private set; }
        public int DamageToBoss { get; private set; }
        public int EnemyHealing { get; private set; }
        public int PlayerHealing { get; private set; }
        private readonly Dictionary<string, int> _potions = new(), _talismans = new();
        private CharacterStatBlock? _startingStats;
        private int _startingHp, _roundLimit;
        public void Begin(string level, string boss, bool complete = true, CharacterStatBlock? stats = null, int hp = 0, int limit = 0)
        {
            LevelId = level; BossId = boss; StatisticsComplete = complete;
            DamageToBoss = EnemyHealing = PlayerHealing = 0; _potions.Clear(); _talismans.Clear();
            _startingStats = stats; _startingHp = Math.Max(0, hp); _roundLimit = Math.Max(0, limit);
        }
        public void Clear() => Begin("", "", false);
        private static int Add(int value, int amount) => (int)Math.Min(int.MaxValue, (long)value + Math.Max(0, amount));
        public void Observe(BattleRoundObservation round)
        {
            if (BossId == "") return;
            DamageToBoss = Add(DamageToBoss, round.DamageToMonster); EnemyHealing = Add(EnemyHealing, round.MonsterHealing); PlayerHealing = Add(PlayerHealing, round.PlayerHealing);
        }
        public void RecordPotion(string id, int actualHealing)
        {
            if (BossId == "") return;
            _potions[id] = Add(_potions.GetValueOrDefault(id), 1); PlayerHealing = Add(PlayerHealing, actualHealing);
        }
        public void RecordTalisman(string id, int actualDamage)
        {
            if (BossId == "") return;
            _talismans[id] = Add(_talismans.GetValueOrDefault(id), 1); DamageToBoss = Add(DamageToBoss, actualDamage);
        }
        public BossChallengeReport Finish(string result, int rounds, int enemyHp, int enemyMaxHp, long timestamp)
            => new(LevelId, BossId, result, Math.Max(0, rounds), Math.Max(0, enemyHp), Math.Max(1, enemyMaxHp),
                DamageToBoss, EnemyHealing, PlayerHealing, new Dictionary<string, int>(_potions), new Dictionary<string, int>(_talismans), StatisticsComplete, timestamp,
                _startingStats, _startingHp, _roundLimit);
        public Dictionary<string, object> ToPlainDictionary() => Finish("", 0, 0, 1, 0).ToPlainDictionary();
        public void Restore(IDictionary<string, object> data, string level, string boss)
        {
            Begin(level, boss, false);
            if (BossChallengeReport.ReadId(data, "level_id") != level || BossChallengeReport.ReadId(data, "boss_id") != boss) return;
            StatisticsComplete = BossChallengeReport.ReadComplete(data);
            _startingStats = BossChallengeReport.ReadStartingStats(data);
            _startingHp = BossChallengeReport.ReadCount(data, "starting_hp"); _roundLimit = BossChallengeReport.ReadCount(data, "round_limit");
            DamageToBoss = BossChallengeReport.ReadCount(data, "damage_to_boss"); EnemyHealing = BossChallengeReport.ReadCount(data, "enemy_healing"); PlayerHealing = BossChallengeReport.ReadCount(data, "player_healing");
            foreach (var item in BossChallengeReport.ReadItems(data, "potions")) _potions[item.Key] = item.Value;
            foreach (var item in BossChallengeReport.ReadItems(data, "talismans")) _talismans[item.Key] = item.Value;
        }
    }
    public static class BossChallengeReportRules
    {
        public static string Comparison(BossChallengeReport current, BossChallengeReport? previous)
        {
            if (previous == null || previous.LevelId != current.LevelId || previous.BossId != current.BossId) return "";
            string text = $"同一Boss相邻两次实战（上次 → 本次）：\n{ResultName(previous.Result)} / {previous.Rounds}回合 → {ResultName(current.Result)} / {current.Rounds}回合；敌方剩余气血{previous.EnemyHp}/{previous.EnemyMaxHp} → {current.EnemyHp}/{current.EnemyMaxHp}。";
            if (previous.StartingStats is CharacterStatBlock before && current.StartingStats is CharacterStatBlock after)
                text += $"\n出战基础属性：攻击{before.Attack} → {after.Attack}，防御{before.Defense} → {after.Defense}，速度{before.Speed} → {after.Speed}；出战气血{previous.StartingHp}/{before.MaxHp} → {current.StartingHp}/{after.MaxHp}，回合上限{previous.RoundLimit} → {current.RoundLimit}。";
            else text += "\n旧战报缺少出战快照，不能推断强化或属性变化。";
            if (previous.StatisticsComplete && current.StatisticsComplete)
                text += $"\n敌方实际回血{previous.EnemyHealing} → {current.EnemyHealing}；丹药使用{previous.Potions.Values.Sum()} → {current.Potions.Values.Sum()}，符箓使用{previous.Talismans.Values.Sum()} → {current.Talismans.Values.Sum()}。";
            else text += "\n存在不完整战报，不比较整场回血与消耗。";
            return text + "\n记录反映两次实际结果；境界、气血、符药及随机效果都可能变化，不代表某项准备的独立收益或胜率。";
        }
        public static string ResultName(string result) => result switch { "victory" => "胜利", "timeout" => "超时败退", "double_knockout" => "双方倒下", _ => "气血耗尽" };
        public static string Summary(BossChallengeReport report) => $"{ResultName(report.Result)} · {report.Rounds}回合 · 敌方剩余气血{report.EnemyHp}/{report.EnemyMaxHp}";
        public static string Details(BossChallengeReport report)
        {
            string Items(IReadOnlyDictionary<string, int> items) => items.Count == 0 ? "无" : string.Join("、", items.Select(x => $"{UiText.BackpackItemName(x.Key)}×{x.Value}"));
            string prefix = report.StatisticsComplete ? "本次完整统计" : "统计不完整，以下仅含已有记录，不代表整场总量";
            return Summary(report) + $"\n{prefix}：敌方实际回血{report.EnemyHealing}，我方实际回血（丹药/灵膳）{report.PlayerHealing}，攻击/符箓实际损血{report.DamageToBoss}。"
                + $"\n已用丹药：{Items(report.Potions)}；已用符箓：{Items(report.Talismans)}。";
        }
        public static IReadOnlyList<string> Advice(BossChallengeReport report)
        {
            if (report.Result == "victory") return Array.Empty<string>();
            var advice = new List<string> { report.Result == "timeout" ? "优先比较武器与强化，提高持续伤害；准备数量达标不代表能在回合上限内击败Boss。"
                : report.Result == "double_knockout" ? "本次双方同时倒下，优先改善生存或先手；可比较护具和装备速度。"
                : "优先比较护具、强化与气血；休整达到出战条件后再重试。" };
            if (report.StatisticsComplete && report.EnemyHealing > 0 && report.EnemyHealing >= Math.Max(1, report.DamageToBoss / 4))
                advice.Add(report.Talismans.GetValueOrDefault(RecipeUnlockRules.Zhenling) > 0
                    ? $"本次已使用镇灵符，前5回合之后敌方仍实际回血{report.EnemyHealing}；可提高持续伤害，重复携带不会延长禁疗。"
                    : $"敌方本次实际回血{report.EnemyHealing}，可提高持续伤害，或选择镇灵符阻断前5回合回血（第6回合恢复）；制作条件见挑战准备。");
            else if (report.StatisticsComplete && report.Potions.Count == 0 && report.Result != "timeout")
                advice.Add("可检查气血补给与消耗策略，也可继续走装备路线；准备仍可跳过。");
            return advice;
        }
    }
}
