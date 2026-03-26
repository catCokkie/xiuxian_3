using Godot;
using System;

namespace Xiuxian.Scripts.Services
{
    public sealed class MonsterConfigService
    {
        private readonly LevelConfigProvider _provider;
        private readonly ActiveLevelManager _activeLevelManager;

        public MonsterConfigService(LevelConfigProvider provider, ActiveLevelManager activeLevelManager)
        {
            _provider = provider;
            _activeLevelManager = activeLevelManager;
        }

        public bool TryGetMonsterCombatParams(
            string monsterId,
            out string monsterName,
            out int hp,
            out int inputsPerRound,
            out int attack)
        {
            monsterName = "Enemy";
            hp = 24;
            inputsPerRound = GameBalanceConstants.Explore.InputsPerBattleRound;
            attack = 4;

            if (!TryGetMonsterStatProfile(monsterId, out MonsterStatProfile profile))
            {
                return false;
            }

            monsterName = profile.DisplayName;
            hp = profile.BaseStats.MaxHp;
            inputsPerRound = profile.InputsPerRound;
            attack = profile.BaseStats.Attack;
            return true;
        }

        public bool TryGetMonsterStatProfile(string monsterId, out MonsterStatProfile profile)
        {
            profile = new MonsterStatProfile(
                monsterId,
                "Enemy",
                new CharacterStatBlock(24, 4, 0, 100, 0.0, 1.5),
                InputsPerRound: 18,
                MoveCategory: "normal",
                IsBoss: false);

            if (!_provider.TryGetMonster(monsterId, out var monster))
            {
                return TryBuildFallbackBossProfile(monsterId, out profile);
            }

            string monsterName = LevelConfigProvider.GetString(monster, "monster_name", profile.DisplayName);
            string moveCategory = LevelConfigProvider.GetString(monster, "move_category", "");
            if (string.IsNullOrEmpty(moveCategory))
            {
                moveCategory = LevelConfigProvider.GetString(monster, "rarity", "normal");
            }

            int hp = profile.BaseStats.MaxHp;
            int attack = profile.BaseStats.Attack;
            int defense = profile.BaseStats.Defense;
            double speedFactor = 1.0;
            int inputsPerRound = profile.InputsPerRound;
            if (LevelConfigProvider.TryGetChildDictionary(monster, "combat", out var combat))
            {
                hp = Math.Max(1, combat.ContainsKey("hp") ? combat["hp"].AsInt32() : hp);
                attack = Math.Max(1, combat.ContainsKey("attack") ? combat["attack"].AsInt32() : attack);
                defense = Math.Max(0, combat.ContainsKey("defense") ? combat["defense"].AsInt32() : defense);
                speedFactor = combat.ContainsKey("speed_factor") ? combat["speed_factor"].AsDouble() : speedFactor;
                inputsPerRound = Math.Max(1, combat.ContainsKey("inputs_per_round") ? combat["inputs_per_round"].AsInt32() : inputsPerRound);
            }

            string activeLevelId = _activeLevelManager.ActiveLevelId;
            bool isBoss = !string.IsNullOrEmpty(activeLevelId) && _activeLevelManager.IsBossMonsterForLevel(activeLevelId, monsterId);
            profile = MonsterStatRules.BuildProfile(monsterId, monsterName, hp, attack, defense, speedFactor, inputsPerRound, moveCategory, isBoss);
            return true;
        }

        public bool TryGetMonsterVisualConfig(
            string monsterId,
            out string portraitPath,
            out string animationType,
            out double animationSpeed,
            out double animationAmplitude,
            out Color tint)
        {
            portraitPath = "";
            animationType = "none";
            animationSpeed = 0.0;
            animationAmplitude = 0.0;
            tint = Colors.White;

            if (!_provider.TryGetMonster(monsterId, out var monster))
            {
                return false;
            }

            if (!LevelConfigProvider.TryGetChildDictionary(monster, "visual", out var visual))
            {
                return true;
            }

            portraitPath = LevelConfigProvider.GetString(visual, "portrait", "");
            animationType = LevelConfigProvider.GetString(visual, "animation", "none");
            animationSpeed = LevelConfigProvider.GetDouble(visual, "anim_speed", 0.0);
            animationAmplitude = LevelConfigProvider.GetDouble(visual, "anim_amplitude", 0.0);

            if (visual.ContainsKey("tint"))
            {
                tint = LevelConfigProvider.ParseColorVariant(visual["tint"], tint);
            }

            return true;
        }

        public bool TryGetMonsterMoveRule(string monsterId, out string moveCategory, out int inputsPerMove)
        {
            moveCategory = "normal";
            inputsPerMove = 4;

            if (!_provider.TryGetMonster(monsterId, out var monster))
            {
                return false;
            }

            moveCategory = LevelConfigProvider.GetString(monster, "move_category", "");
            if (string.IsNullOrEmpty(moveCategory))
            {
                moveCategory = LevelConfigProvider.GetString(monster, "rarity", "normal");
            }

            if (_activeLevelManager.ActiveMoveInputsByCategory.TryGetValue(moveCategory, out int configured))
            {
                inputsPerMove = Math.Max(1, configured);
            }
            else if (_activeLevelManager.ActiveMoveInputsByCategory.TryGetValue("default", out int fallback))
            {
                inputsPerMove = Math.Max(1, fallback);
            }

            return true;
        }

        private bool TryBuildFallbackBossProfile(string monsterId, out MonsterStatProfile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(monsterId) || !_provider.TryGetLevelAtIndex(_activeLevelManager.ActiveLevelIndex, out var level))
            {
                return false;
            }

            if (LevelConfigProvider.GetLevelBossMonsterId(level) != monsterId)
            {
                return false;
            }

            if (!level.ContainsKey("monster_wave") || level["monster_wave"].VariantType != Variant.Type.Array)
            {
                return false;
            }

            var wave = (Godot.Collections.Array<Variant>)level["monster_wave"];
            for (int i = wave.Count - 1; i >= 0; i--)
            {
                string eliteAnchorId = wave[i].AsString();
                if (string.IsNullOrEmpty(eliteAnchorId) || eliteAnchorId == monsterId)
                {
                    continue;
                }

                if (!TryGetMonsterStatProfile(eliteAnchorId, out MonsterStatProfile eliteProfile))
                {
                    continue;
                }

                profile = BossEncounterRules.BuildBossProfile(monsterId, $"{eliteProfile.DisplayName} Boss", eliteProfile, 2.5);
                return true;
            }

            return false;
        }
    }
}
