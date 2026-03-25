using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public sealed class ActiveLevelManager
    {
        private readonly LevelConfigProvider _provider;

        private readonly HashSet<string> _unlockedLevelIds = new();
        private readonly HashSet<string> _bossClearedLevelIds = new();
        private readonly List<string> _activeLevelMonsterWave = new();
        private readonly Dictionary<string, int> _activeMoveInputsByCategory = new();
        private int _activeLevelIndex;
        private int _activeLevelWaveIndex;

        public ActiveLevelManager(LevelConfigProvider provider)
        {
            _provider = provider;
            ApplyActiveLevelData();
        }

        public string ActiveLevelId { get; private set; } = string.Empty;
        public string ActiveLevelName { get; private set; } = "Unknown Zone";
        public double ProgressPer100Inputs { get; private set; } = 2.0;
        public double EncounterCheckIntervalProgress { get; private set; } = 20.0;
        public double BaseEncounterRate { get; private set; } = 0.18;
        public double BattlePauseFactor { get; private set; }
        public int PlayerBaseHp { get; private set; } = 36;
        public int PlayerAttackPerRound { get; private set; } = 4;
        public int EnemyDamageDivider { get; private set; } = 4;
        public int EnemyMinDamagePerRound { get; private set; } = 1;

        public IReadOnlyList<string> ActiveLevelMonsterWave => _activeLevelMonsterWave;
        public Dictionary<string, int> ActiveMoveInputsByCategory => _activeMoveInputsByCategory;
        public HashSet<string> UnlockedLevelIds => _unlockedLevelIds;
        public HashSet<string> BossClearedLevelIds => _bossClearedLevelIds;
        public int ActiveLevelIndex
        {
            get => _activeLevelIndex;
            set => _activeLevelIndex = value;
        }
        public int ActiveLevelWaveIndex
        {
            get => _activeLevelWaveIndex;
            set => _activeLevelWaveIndex = value;
        }

        public void ResetForReload()
        {
            _activeLevelIndex = 0;
            _activeLevelWaveIndex = 0;
            _unlockedLevelIds.Clear();
            _bossClearedLevelIds.Clear();
            ApplyActiveLevelData();
        }

        public void RefreshActiveLevelData()
        {
            ApplyActiveLevelData();
        }

        public void EnsureLevelUnlockBootstrap()
        {
            if (_provider.Levels.Count == 0 || _unlockedLevelIds.Count > 0)
            {
                return;
            }

            string firstLevelId = LevelConfigProvider.GetString(_provider.Levels[0], "level_id", "");
            if (!string.IsNullOrEmpty(firstLevelId))
            {
                _unlockedLevelIds.Add(firstLevelId);
            }
        }

        public bool AdvanceToNextLevel()
        {
            if (_provider.Levels.Count == 0)
            {
                return false;
            }

            _activeLevelIndex = (_activeLevelIndex + 1) % _provider.Levels.Count;
            ApplyActiveLevelData();
            return true;
        }

        public bool TryAdvanceToNextUnlockedLevel()
        {
            string next = GetNextUnlockedLevelId(ActiveLevelId);
            return !string.IsNullOrEmpty(next) && next != ActiveLevelId && TrySetActiveLevel(next);
        }

        public bool TrySetActiveLevel(string levelId)
        {
            if (!TryFindLevelIndex(levelId, out int index))
            {
                return false;
            }

            _activeLevelIndex = index;
            ApplyActiveLevelData();
            return true;
        }

        public bool TrySetActiveLevelIfUnlocked(string levelId)
        {
            return IsLevelUnlocked(levelId) && TrySetActiveLevel(levelId);
        }

        public bool TrySetNextUnlockedLevelAsActive()
        {
            string next = GetNextUnlockedLevelId(ActiveLevelId);
            return !string.IsNullOrEmpty(next) && TrySetActiveLevel(next);
        }

        public Godot.Collections.Array<string> GetUnlockedLevelIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (var level in _provider.Levels)
            {
                string levelId = LevelConfigProvider.GetString(level, "level_id", "");
                if (!string.IsNullOrEmpty(levelId) && _unlockedLevelIds.Contains(levelId))
                {
                    result.Add(levelId);
                }
            }

            return result;
        }

        public Godot.Collections.Array<string> GetBossClearedLevelIds()
        {
            var result = new Godot.Collections.Array<string>();
            foreach (string levelId in _bossClearedLevelIds)
            {
                result.Add(levelId);
            }

            return result;
        }

        public bool IsLevelUnlocked(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            EnsureLevelUnlockBootstrap();
            return _unlockedLevelIds.Contains(levelId);
        }

        public bool IsBossMonsterForLevel(string levelId, string monsterId)
        {
            if (string.IsNullOrEmpty(levelId) || string.IsNullOrEmpty(monsterId) || !TryFindLevelIndex(levelId, out int levelIndex))
            {
                return false;
            }

            string bossId = GetLevelBossMonsterId(_provider.Levels[levelIndex]);
            return !string.IsNullOrEmpty(bossId) && bossId == monsterId;
        }

        public string GetBossMonsterId(string levelId = "")
        {
            if (_provider.Levels.Count == 0)
            {
                return string.Empty;
            }

            int levelIndex = _activeLevelIndex;
            if (!string.IsNullOrEmpty(levelId) && TryFindLevelIndex(levelId, out int found))
            {
                levelIndex = found;
            }

            levelIndex = Math.Clamp(levelIndex, 0, _provider.Levels.Count - 1);
            return GetLevelBossMonsterId(_provider.Levels[levelIndex]);
        }

        public bool TryMarkBossDefeatedAndUnlockNext(string levelId, string monsterId, out string unlockedLevelId)
        {
            unlockedLevelId = string.Empty;
            if (!IsBossMonsterForLevel(levelId, monsterId))
            {
                return false;
            }

            _bossClearedLevelIds.Add(levelId);
            string next = BossUnlockRules.ResolveNextUnlockedLevelId(GetConfiguredNextLevelId(levelId), GetNextLevelId(levelId));
            if (string.IsNullOrEmpty(next))
            {
                return false;
            }

            if (_unlockedLevelIds.Add(next))
            {
                unlockedLevelId = next;
                return true;
            }

            return false;
        }

        public Godot.Collections.Array<string> GetSpawnMonsterIds(string levelId = "")
        {
            var result = new Godot.Collections.Array<string>();
            if (_provider.Levels.Count == 0)
            {
                return result;
            }

            int levelIndex = _activeLevelIndex;
            if (!string.IsNullOrEmpty(levelId) && TryFindLevelIndex(levelId, out int found))
            {
                levelIndex = found;
            }

            var level = _provider.Levels[Math.Clamp(levelIndex, 0, _provider.Levels.Count - 1)];
            if (!level.ContainsKey("spawn_table") || level["spawn_table"].VariantType != Variant.Type.Array)
            {
                return result;
            }

            var spawnTable = (Godot.Collections.Array<Variant>)level["spawn_table"];
            var unique = new HashSet<string>();
            foreach (Variant item in spawnTable)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                string monsterId = LevelConfigProvider.GetString(dict, "monster_id", "");
                if (!string.IsNullOrEmpty(monsterId) && unique.Add(monsterId))
                {
                    result.Add(monsterId);
                }
            }

            return result;
        }

        public bool TryGetActiveWaveProgress(out int nextSpawnIndex, out int waveCount, out string nextMonsterId)
        {
            nextSpawnIndex = 0;
            waveCount = _activeLevelMonsterWave.Count;
            nextMonsterId = string.Empty;
            if (waveCount <= 0)
            {
                return false;
            }

            int index = _activeLevelWaveIndex;
            if (index < 0 || index >= waveCount)
            {
                index = 0;
            }

            nextSpawnIndex = index + 1;
            nextMonsterId = _activeLevelMonsterWave[index];
            return true;
        }

        public string RollSpawnMonsterId(RandomNumberGenerator rng)
        {
            if (!TryGetActiveLevel(out var level))
            {
                return string.Empty;
            }

            if (_activeLevelMonsterWave.Count > 0)
            {
                _activeLevelWaveIndex = Math.Clamp(_activeLevelWaveIndex, 0, _activeLevelMonsterWave.Count - 1);
                string waveMonsterId = _activeLevelMonsterWave[_activeLevelWaveIndex];
                _activeLevelWaveIndex = (_activeLevelWaveIndex + 1) % _activeLevelMonsterWave.Count;
                return waveMonsterId;
            }

            if (!level.ContainsKey("spawn_table") || level["spawn_table"].VariantType != Variant.Type.Array)
            {
                return string.Empty;
            }

            var spawnTable = (Godot.Collections.Array<Variant>)level["spawn_table"];
            int totalWeight = 0;
            foreach (Variant item in spawnTable)
            {
                if (item.VariantType == Variant.Type.Dictionary)
                {
                    var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                    totalWeight += Math.Max(0, dict.ContainsKey("weight") ? dict["weight"].AsInt32() : 0);
                }
            }

            if (totalWeight <= 0)
            {
                return string.Empty;
            }

            int roll = rng.RandiRange(1, totalWeight);
            int acc = 0;
            foreach (Variant item in spawnTable)
            {
                if (item.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var dict = (Godot.Collections.Dictionary<string, Variant>)item;
                int weight = Math.Max(0, dict.ContainsKey("weight") ? dict["weight"].AsInt32() : 0);
                if (weight <= 0)
                {
                    continue;
                }

                acc += weight;
                if (roll <= acc)
                {
                    return LevelConfigProvider.GetString(dict, "monster_id", "");
                }
            }

            return string.Empty;
        }

        public Godot.Collections.Dictionary<string, Variant> ToRuntimeDictionary()
        {
            var unlocked = new Godot.Collections.Array<Variant>();
            foreach (string levelId in _unlockedLevelIds)
            {
                unlocked.Add(levelId);
            }

            var bossCleared = new Godot.Collections.Array<Variant>();
            foreach (string levelId in _bossClearedLevelIds)
            {
                bossCleared.Add(levelId);
            }

            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["active_level_id"] = ActiveLevelId,
                ["active_wave_index"] = _activeLevelWaveIndex,
                ["unlocked_level_ids"] = unlocked,
                ["boss_cleared_level_ids"] = bossCleared,
            };
        }

        public void FromRuntimeDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            _unlockedLevelIds.Clear();
            _bossClearedLevelIds.Clear();

            if (data.ContainsKey("active_level_id"))
            {
                string levelId = data["active_level_id"].AsString();
                if (!string.IsNullOrEmpty(levelId))
                {
                    TrySetActiveLevel(levelId);
                }
            }

            if (data.ContainsKey("unlocked_level_ids") && data["unlocked_level_ids"].VariantType == Variant.Type.Array)
            {
                foreach (Variant v in (Godot.Collections.Array<Variant>)data["unlocked_level_ids"])
                {
                    string levelId = v.AsString();
                    if (!string.IsNullOrEmpty(levelId))
                    {
                        _unlockedLevelIds.Add(levelId);
                    }
                }
            }

            if (data.ContainsKey("boss_cleared_level_ids") && data["boss_cleared_level_ids"].VariantType == Variant.Type.Array)
            {
                foreach (Variant v in (Godot.Collections.Array<Variant>)data["boss_cleared_level_ids"])
                {
                    string levelId = v.AsString();
                    if (!string.IsNullOrEmpty(levelId))
                    {
                        _bossClearedLevelIds.Add(levelId);
                    }
                }
            }

            EnsureLevelUnlockBootstrap();

            if (data.ContainsKey("active_wave_index"))
            {
                int savedWaveIndex = data["active_wave_index"].AsInt32();
                _activeLevelWaveIndex = _activeLevelMonsterWave.Count > 0
                    ? Math.Clamp(savedWaveIndex, 0, _activeLevelMonsterWave.Count - 1)
                    : 0;
            }
        }

        private bool TryFindLevelIndex(string levelId, out int levelIndex)
        {
            levelIndex = -1;
            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            for (int i = 0; i < _provider.Levels.Count; i++)
            {
                string id = LevelConfigProvider.GetString(_provider.Levels[i], "level_id", "");
                if (id == levelId)
                {
                    levelIndex = i;
                    return true;
                }
            }

            return false;
        }

        private string GetNextLevelId(string levelId)
        {
            if (!TryFindLevelIndex(levelId, out int index))
            {
                return string.Empty;
            }

            int next = index + 1;
            if (next < 0 || next >= _provider.Levels.Count)
            {
                return string.Empty;
            }

            return LevelConfigProvider.GetString(_provider.Levels[next], "level_id", "");
        }

        private string GetConfiguredNextLevelId(string levelId)
        {
            if (!TryFindLevelIndex(levelId, out int index))
            {
                return string.Empty;
            }

            return LevelConfigProvider.GetString(_provider.Levels[index], "unlock_next_level_id", "");
        }

        private string GetNextUnlockedLevelId(string levelId)
        {
            return LevelUnlockRules.GetNextUnlockedLevelId(GetUnlockedLevelIds(), levelId);
        }

        private bool TryGetActiveLevel(out Godot.Collections.Dictionary<string, Variant> level)
        {
            level = new Godot.Collections.Dictionary<string, Variant>();
            if (_provider.Levels.Count == 0)
            {
                return false;
            }

            _activeLevelIndex = Math.Clamp(_activeLevelIndex, 0, _provider.Levels.Count - 1);
            level = _provider.Levels[_activeLevelIndex];
            return true;
        }

        private void ApplyActiveLevelData()
        {
            if (!TryGetActiveLevel(out var level))
            {
                ActiveLevelId = string.Empty;
                ActiveLevelName = "Unknown Zone";
                ProgressPer100Inputs = 2.0;
                EncounterCheckIntervalProgress = 20.0;
                BaseEncounterRate = 0.18;
                BattlePauseFactor = 0.0;
                PlayerBaseHp = 36;
                PlayerAttackPerRound = 4;
                EnemyDamageDivider = 4;
                EnemyMinDamagePerRound = 1;
                _activeLevelMonsterWave.Clear();
                _activeMoveInputsByCategory.Clear();
                _activeLevelWaveIndex = 0;
                return;
            }

            ActiveLevelId = LevelConfigProvider.GetString(level, "level_id", "");
            ActiveLevelName = LevelConfigProvider.GetString(level, "level_name", "Unknown Zone");

            if (!LevelConfigProvider.TryGetChildDictionary(level, "explore", out var explore))
            {
                ProgressPer100Inputs = 2.0;
                EncounterCheckIntervalProgress = 20.0;
                BaseEncounterRate = 0.18;
                BattlePauseFactor = 0.0;
                _activeMoveInputsByCategory.Clear();
                _activeMoveInputsByCategory["default"] = 4;
            }
            else
            {
                ProgressPer100Inputs = LevelConfigProvider.GetDouble(explore, "progress_per_100_inputs", 2.0);
                EncounterCheckIntervalProgress = LevelConfigProvider.GetDouble(explore, "encounter_check_interval_progress", 20.0);
                BaseEncounterRate = LevelConfigProvider.GetDouble(explore, "base_encounter_rate", 0.18);
                BattlePauseFactor = LevelConfigProvider.GetDouble(explore, "battle_pause_factor", 0.0);
                _activeMoveInputsByCategory.Clear();
                _activeMoveInputsByCategory["default"] = 4;
                if (explore.ContainsKey("move_inputs_by_category") && explore["move_inputs_by_category"].VariantType == Variant.Type.Dictionary)
                {
                    var moveMap = (Godot.Collections.Dictionary<string, Variant>)explore["move_inputs_by_category"];
                    foreach (string key in moveMap.Keys)
                    {
                        _activeMoveInputsByCategory[key] = Math.Max(1, moveMap[key].AsInt32());
                    }
                }
            }

            _activeLevelMonsterWave.Clear();
            _activeLevelWaveIndex = 0;
            if (level.ContainsKey("monster_wave") && level["monster_wave"].VariantType == Variant.Type.Array)
            {
                foreach (Variant item in (Godot.Collections.Array<Variant>)level["monster_wave"])
                {
                    string id = item.AsString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        _activeLevelMonsterWave.Add(id);
                    }
                }
            }

            if (!LevelConfigProvider.TryGetChildDictionary(level, "battle_runtime", out var battleRuntime))
            {
                PlayerBaseHp = 36;
                PlayerAttackPerRound = 4;
                EnemyDamageDivider = 4;
                EnemyMinDamagePerRound = 1;
                return;
            }

            PlayerBaseHp = Math.Max(1, battleRuntime.ContainsKey("player_base_hp") ? battleRuntime["player_base_hp"].AsInt32() : 36);
            PlayerAttackPerRound = Math.Max(1, battleRuntime.ContainsKey("player_attack_per_round") ? battleRuntime["player_attack_per_round"].AsInt32() : 4);
            EnemyDamageDivider = Math.Max(1, battleRuntime.ContainsKey("enemy_damage_divider") ? battleRuntime["enemy_damage_divider"].AsInt32() : 4);
            EnemyMinDamagePerRound = Math.Max(1, battleRuntime.ContainsKey("enemy_min_damage_per_round") ? battleRuntime["enemy_min_damage_per_round"].AsInt32() : 1);
        }

        private static string GetLevelBossMonsterId(Godot.Collections.Dictionary<string, Variant> level)
        {
            string configured = LevelConfigProvider.GetString(level, "boss_monster_id", "");
            if (!string.IsNullOrEmpty(configured))
            {
                return configured;
            }

            if (level.ContainsKey("monster_wave") && level["monster_wave"].VariantType == Variant.Type.Array)
            {
                var wave = (Godot.Collections.Array<Variant>)level["monster_wave"];
                for (int i = wave.Count - 1; i >= 0; i--)
                {
                    string id = wave[i].AsString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        return id;
                    }
                }
            }

            return string.Empty;
        }
    }
}
