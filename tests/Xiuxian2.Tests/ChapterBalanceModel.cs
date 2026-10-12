using System.Text.Json;
using System.Text.RegularExpressions;
using Xiuxian.Scripts.Services;
using Xiuxian.Scripts.Game;

namespace Xiuxian.Tests;

// A design budget model: it uses runtime combat/conversion/cost rules, but explicitly
// models planned manufacturing, material recycling and counters. It is not a UI playtest.
internal sealed class ChapterBalanceModel
{
    internal static readonly int[] RecommendedRealms = { 3, 4, 6, 8, 9 };
    internal static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "project.godot"))) directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Repository root not found");
        }
    }
    internal static JsonElement Read(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, path)));
        return document.RootElement.Clone();
    }
    internal static int Number(JsonElement obj, string key, int fallback = 0)
        => obj.TryGetProperty(key, out var value) ? value.GetInt32() : fallback;
    internal static string Text(JsonElement obj, string key) => obj.GetProperty(key).GetString()!;
    internal static readonly JsonElement Content = JsonDocument.Parse(EquipmentConfigMergeRules.Merge(
        File.ReadAllText(Path.Combine(Root, "docs/design/09_level_monster_drop_sample.json")),
        File.ReadAllText(Path.Combine(Root, "docs/design/12_equipment_sample_qi_refining.json")))).RootElement.Clone();
    internal static readonly JsonElement Equipment = Read("docs/design/12_equipment_sample_qi_refining.json");
    internal static readonly JsonElement Routes = Read("docs/design/23_deepening_routes.json");
    internal static readonly JsonElement[] Levels = Content.GetProperty("levels").EnumerateArray().ToArray();
    internal static readonly Dictionary<string, JsonElement> Monsters = Content.GetProperty("monsters").EnumerateArray().ToDictionary(x => Text(x, "monster_id"));
    internal static readonly Dictionary<string, JsonElement> Tables = Content.GetProperty("drop_tables").EnumerateArray().ToDictionary(x => Text(x, "drop_table_id"));
    internal static readonly HashSet<string> ExcludedRare = new() { "water_spirit_amber", "toxic_spore_powder", "nether_bone_shard", "sunken_equipment", "spirit_spring_water" };

    internal sealed record CombatResult(bool Won, int Rounds, int RemainingHp, bool HealUsed, int PlayerAttack, int MonsterDefense,
        int StartingHp, int MaxHp, int PlayerDefense);
    internal sealed record StageResult(string Region, int Realm, CombatResult Combat, long Inputs, int Gold, double Lingqi, double Insight);
    internal sealed record BudgetResult(string Route, double InputsPerMinute, int Seed, bool MinimumYield, bool RandomMaterialDrops, bool AllBossesWon, int FinalRealm,
        long Inputs, double Hours, int GoldSpent, int GoldEarned, double LingqiSpent, double InsightSpent,
        int PlannedActions, double CropWaitingMinutes, double PondWaitingMinutes, double RecoveryWaitingSeconds, int OrdinaryDefeats, Dictionary<string, long> InputsByMode,
        Dictionary<string, int> MaterialsSpent, List<StageResult> Stages, Dictionary<string, int> Inventory);

    internal static CharacterStatBlock PlayerStats(int region, int realm, EquipmentStatProfile weapon, double hpRate = 0, double defenseRate = 0, int defenseFlat = 0)
    {
        var runtime = Levels[region].GetProperty("battle_runtime");
        var basis = PlayerBaseStatRules.BuildBaseStats(realm, Number(runtime, "player_base_hp"), Number(runtime, "player_attack_per_round"));
        var equipment = EquipmentStarterLoadout.CreateDefaultProfiles();
        equipment[0] = weapon with { IsEquipped = true };
        var modifiers = CharacterStatRules.CollectEquipmentModifiers(equipment).ToList();
        modifiers.Add(new(MaxHpRate: hpRate, DefenseRate: defenseRate, DefenseFlat: defenseFlat));
        return CharacterStatRules.BuildFinalStats(basis, modifiers);
    }

    internal static CombatResult Fight(int region, int realm, EquipmentStatProfile weapon, JsonElement monster,
        bool suppressRegeneration = false, bool armorCharm = false, double hpRate = 0, double defenseRate = 0, bool allowHeal = true,
        int? startingHp = null, bool isBoss = true, int defenseFlat = 0)
    {
        var stats = PlayerStats(region, realm, weapon, hpRate, defenseRate, defenseFlat);
        var combat = monster.GetProperty("combat");
        var profile = MonsterStatRules.BuildProfile(Text(monster, "monster_id"), Text(monster, "monster_name"), Number(combat, "hp"),
            Number(combat, "attack"), Number(combat, "defense"), combat.GetProperty("speed_factor").GetDouble(), Number(combat, "inputs_per_round"), "normal", true);
        profile = profile with { HealPerRound = Number(monster, "heal_per_round") };
        if (armorCharm)
        {
            var effect = TalismanRules.GetBattleEffect("talisman_fire_charm", monster.TryGetProperty("strategy_tag", out var tag) ? tag.GetString()! : "");
            profile = profile with { BaseStats = profile.BaseStats with { Defense = (int)Math.Round(profile.BaseStats.Defense * (1 - effect.DefenseReduction)) } };
        }
        bool healed = false;
        var runtime = Levels[region].GetProperty("battle_runtime");
        var logic = new ExploreGameLogic { InBattle = true, PlayerHp = startingHp ?? (int)Math.Floor(stats.MaxHp * .70), PlayerMaxHp = stats.MaxHp,
            EnemyHp = profile.BaseStats.MaxHp, EnemyMaxHp = profile.BaseStats.MaxHp, EnemyAttackPower = profile.BaseStats.Attack,
            InputsPerBattleRoundRuntime = profile.InputsPerRound, EnemyDamageDividerRuntime = Number(runtime, "enemy_damage_divider"),
            EnemyMinDamageRuntime = Number(runtime, "enemy_min_damage_per_round") };
        void Heal()
        {
            if (allowHeal && !healed && logic.PlayerHp > 0 && logic.PlayerHp * 2 < logic.PlayerMaxHp)
            {
                int heal = Math.Max(1, (int)Math.Round(logic.PlayerMaxHp * ConsumableUsageRules.HuiqiDanHealPercent));
                logic.PlayerHp = Math.Min(logic.PlayerMaxHp, logic.PlayerHp + heal);
                healed = true;
            }
        }
        Heal();
        int limit = isBoss ? GameBalanceConstants.Explore.MaxBossBattleRounds : 1000;
        var result = logic.AdvanceBattleByInput(limit * profile.InputsPerRound, stats, Array.Empty<EquipmentStatProfile>(), isBoss,
            GameBalanceConstants.Explore.MaxBossBattleRounds, afterRoundResolved: Heal, monsterProfile: profile,
            suppressMonsterHealingRounds: suppressRegeneration ? 5 : 0);
        return new(result.Outcome == BattleOutcome.PlayerWon, result.RoundsResolved, logic.PlayerHp, healed, stats.Attack, profile.BaseStats.Defense,
            startingHp ?? (int)Math.Floor(stats.MaxHp * .70), stats.MaxHp, stats.Defense);
    }

    private readonly string _route;
    private readonly double _rate;
    private readonly Random _random;
    private readonly bool _minimumYield;
    private readonly bool _randomMaterialDrops;
    private readonly int _seed;
    private readonly Dictionary<string, int> _items = new();
    private readonly Dictionary<string, int> _spent = new();
    private readonly Dictionary<string, long> _inputs = new();
    private readonly Dictionary<string, int> _mastery = new();
    private readonly Dictionary<string, int> _pity = new();
    private readonly Dictionary<string, (int Daily, long Day, int Hourly, long Hour)> _quotas = new();
    private readonly List<StageResult> _stages = new();
    private EquipmentStatProfile _weapon = EquipmentStarterLoadout.CreateDefaultProfiles()[0];
    private int _realm = 1, _cleared, _gold, _goldSpent, _goldEarned, _actions;
    private double _exp, _qi, _insight, _qiSpent, _insightSpent, _seconds, _waiting, _pondWaiting, _pondRefreshAt, _mealSeconds, _hpRate, _defenseRate;
    private int _fishCaught;
    private int _currentRegion, _hp = 36, _ordinaryDefeats;
    private double _recoveryRemainder, _recoveryWaiting;
    private bool _fighting;
    private int DefenseFlat => _guardCrafted ? FormationRules.GetModifier("formation_guard_flag").DefenseFlat : 0;

    private void Recover(double seconds)
    {
        var stats = PlayerStats(_currentRegion, _realm, _weapon, _hpRate, _defenseRate, DefenseFlat);
        var recovery = BattleRecoveryRules.Advance(_hp, stats.MaxHp, seconds, _recoveryRemainder, _fighting);
        _hp = recovery.Hp; _recoveryRemainder = recovery.Remainder;
    }
    private void WaitUntilReady(int region)
    {
        _currentRegion = region;
        var stats = PlayerStats(region, _realm, _weapon, _hpRate, _defenseRate, DefenseFlat);
        if (BattleRecoveryRules.CanFight(_hp, stats.MaxHp)) return;
        double wait = BattleRecoveryRules.SecondsUntilReady(_hp, stats.MaxHp) + .001;
        _seconds += wait; _recoveryWaiting += wait; Recover(wait);
    }
    private bool _gardenSeed, _guardCrafted, _bodyTrained;
    private string _mode = "";
    private long TotalInputs => _inputs.Values.Sum();
    private int Count(string id) => _items.GetValueOrDefault(id);
    private int Mastery(string id) => _mastery.GetValueOrDefault(id, 1);
    private void Add(string id, int count) { if (count > 0) _items[id] = Count(id) + count; }
    private void Spend(string id, int count)
    {
        if (Count(id) < count) throw new InvalidOperationException($"Unfunded material: {id}");
        _items[id] = Count(id) - count;
        _spent[id] = _spent.GetValueOrDefault(id) + count;
    }

    internal ChapterBalanceModel(string route, double rate, int seed, bool minimumYield, bool randomMaterialDrops = true)
        => (_route, _rate, _seed, _random, _minimumYield, _randomMaterialDrops) = (route, rate, seed, new Random(seed), minimumYield, randomMaterialDrops);

    private void Run(string mode, int count)
    {
        if (!ProgressiveUnlockRules.IsActionModeUnlocked(mode, _realm)) throw new InvalidOperationException($"Locked activity {mode} at realm {_realm}");
        if (TotalInputs + count > 500_000) throw new InvalidOperationException($"Unbounded preparation: {_route}");
        if (_mode != mode) { _actions++; _mode = mode; }
        _inputs[mode] = _inputs.GetValueOrDefault(mode) + count;
        double delta = 60 / _rate;
        Recover(count * delta);
        for (int i = 0; i < count; i++)
        {
            double active = Math.Min(5, delta);
            var resource = ResourceConversionRules.Calculate(mode, 1, 1, active, 1 + (_realm - 1) * .06, Mastery("cultivation"), 0);
            _qi += resource.Lingqi;
            _insight += resource.Insight * (_mealSeconds > 0 ? 1.1 : 1);
            _exp += resource.RealmExp;
            _mealSeconds = Math.Max(0, _mealSeconds - active);
            _seconds += delta;
        }
    }

    private void ReachRealm(int target)
    {
        while (_realm < target)
        {
            if (_realm == 9 && _cleared != Levels.Length) throw new InvalidOperationException("Foundation before final Boss clear");
            double required = PlayerProgressState.GetExpRequired(_realm);
            if (_exp < required) Run("cultivation", (int)Math.Ceiling((required - _exp) / GameBalanceConstants.ResourceConversion.CultivationExpPerInput));
            _exp -= required;
            _realm++;
            _actions++; // A manual breakthrough is part of the attention budget.
        }
    }

    private void Learn(string system, int level)
    {
        if (!ProgressiveUnlockRules.IsActionModeUnlocked(system, _realm)) throw new InvalidOperationException($"Learning locked system {system}");
        while (Mastery(system) < level)
        {
            if (!SubsystemMasteryRules.TryGetDefinition(system, Mastery(system) + 1, out var definition)) throw new InvalidOperationException("Unknown mastery");
            if (_realm < definition.RequiredRealmLevel) throw new InvalidOperationException("Early mastery");
            if (_insight < definition.InsightCost) Run("cultivation", (int)Math.Ceiling((definition.InsightCost - _insight) / .08));
            _insight -= definition.InsightCost;
            _insightSpent += definition.InsightCost;
            _mastery[system] = definition.Level;
            _actions++;
        }
    }

    private void PayQi(double cost)
    {
        if (_qi < cost) Run("cultivation", (int)Math.Ceiling((cost - _qi) / .9));
        _qi -= cost; _qiSpent += cost;
    }
    private void PayGold(int cost)
    {
        while (_gold < cost) EncounterCheck(Math.Max(0, _cleared - 1));
        _gold -= cost; _goldSpent += cost;
    }

    // Conservative encounter opportunities derived from the actual queue spacing and
    // movement constants. This does not emulate viewport geometry or repeated UI ticks.
    private void EncounterCheck(int region, int? exploreInputs = null)
    {
        WaitUntilReady(region);
        int interval = (int)Math.Ceiling(GameBalanceConstants.Explore.MonsterRespawnSpacing / GameBalanceConstants.Explore.MonsterMovePxPerFrame)
            * GameBalanceConstants.Explore.InputsPerMoveFrame;
        Run("dungeon", exploreInputs ?? interval);
        if (exploreInputs < interval) return;
        var level = Levels[region];
        double chance = BattleStartRules.CalculateEncounterRate(level.GetProperty("explore").GetProperty("base_encounter_rate").GetDouble(), _realm, Number(level, "danger_level"));
        if (_random.NextDouble() > chance) return;
        var spawn = Weighted(level.GetProperty("spawn_table").EnumerateArray().ToArray());
        var monster = Monsters[Text(spawn, "monster_id")];
        double eliteBonus = SubsystemMasteryRules.GetEffectValue("dungeon", Mastery("dungeon"), "dungeon_elite_rate_bonus");
        if (eliteBonus > 0 && _random.NextDouble() < eliteBonus && Text(monster, "move_category") != "elite")
        {
            var elites = level.GetProperty("spawn_table").EnumerateArray().Select(x => Monsters[Text(x, "monster_id")])
                .Where(x => Text(x, "move_category") == "elite").ToArray();
            if (elites.Length > 0) monster = elites[_random.Next(elites.Length)];
        }
        WaitUntilReady(region);
        var outcome = Fight(region, _realm, _weapon, monster, hpRate: _hpRate, defenseRate: _defenseRate, allowHeal: false,
            startingHp: _hp, isBoss: false, defenseFlat: DefenseFlat);
        _fighting = true;
        Run("dungeon", outcome.Rounds * Number(monster.GetProperty("combat"), "inputs_per_round"));
        _fighting = false;
        _hp = outcome.Won ? outcome.RemainingHp : 0;
        if (!outcome.Won) { _ordinaryDefeats++; _recoveryRemainder = 0; }
        if (outcome.Won) Rewards(region, monster, false);
    }

    private JsonElement Weighted(JsonElement[] entries)
    {
        int roll = _random.Next(entries.Sum(x => Number(x, "weight")));
        foreach (var entry in entries) { roll -= Number(entry, "weight"); if (roll < 0) return entry; }
        throw new InvalidOperationException("Empty weighted pool");
    }

    private void Rewards(int region, JsonElement monster, bool boss)
    {
        int stones = RewardRules.CalculateBattleSpiritStoneReward(monster.TryGetProperty("move_category", out var category) ? category.GetString()! : "normal", boss, region);
        _gold += stones; _goldEarned += stones;
        var reward = monster.GetProperty("settlement_reward");
        _qi += Number(reward, "lingqi_min"); _insight += Number(reward, "insight_min");
        var drops = monster.GetProperty("drops");
        string monsterId = Text(monster, "monster_id");
        var table = Tables.Values.FirstOrDefault(x => Text(x, "bind_level_id") == Text(Levels[region], "level_id")
            && x.GetProperty("bind_monster_ids").EnumerateArray().Any(id => id.GetString() == monsterId));
        if (table.ValueKind == JsonValueKind.Undefined) table = Tables[Text(drops, "drop_table_id")];
        string tableId = Text(table, "drop_table_id");
        var economy = table.GetProperty("economy");
        var state = _quotas.GetValueOrDefault(tableId);
        var itemsThisVictory = new Dictionary<string, int>();
        for (int i = 0; i < Number(drops, "drop_roll_count", 1); i++)
        {
            var quota = LevelDropEconomyRules.ConsumeDropRoll(state.Daily, state.Day, Number(economy, "daily_cap_rolls"), state.Hourly, state.Hour,
                (long)_seconds, _quotas.ContainsKey(tableId), _quotas.ContainsKey(tableId));
            state = (quota.DailyCount, quota.DayIndex, quota.HourlyCount, quota.HourIndex);
            if (!quota.Allowed || LevelDropEconomyRules.ShouldSkipDropBySoftCap(Number(economy, "hourly_soft_cap_rolls"), economy.GetProperty("repeat_decay_factor").GetDouble(), quota.HourlyCountAfterConsume, _random.NextDouble())) continue;
            var entry = Weighted(table.GetProperty("entries").EnumerateArray().ToArray());
            if (!_randomMaterialDrops) continue; // Stress fixture: only configured guarantees and pity remain.
            if (!entry.TryGetProperty("item_id", out _)) continue;
            string id = Text(entry, "item_id");
            if (ExcludedRare.Contains(id)) continue; // Never replace a rejected rare roll with a free common roll.
            int quantity = Number(entry, "min_qty", 1);
            if (!_minimumYield) quantity = _random.Next(quantity, Number(entry, "max_qty", quantity) + 1);
            itemsThisVictory[id] = itemsThisVictory.GetValueOrDefault(id) + quantity;
        }
        _quotas[tableId] = state;
        var pity = table.GetProperty("pity");
        string pityId = Text(pity, "item_id");
        var pityResult = LevelDropEconomyRules.ApplyPity(_pity.GetValueOrDefault(tableId), Number(pity, "threshold"), itemsThisVictory.ContainsKey(pityId), Number(pity, "qty", 1));
        _pity[tableId] = pityResult.NextCounter;
        if (pityResult.Triggered) itemsThisVictory[pityId] = itemsThisVictory.GetValueOrDefault(pityId) + pityResult.AddedQty;
        foreach (var entry in itemsThisVictory) Add(entry.Key, entry.Value);
        foreach (var guaranteed in drops.GetProperty("guaranteed_drop").EnumerateArray()) Add(Text(guaranteed, "item_id"), Number(guaranteed, "qty"));
    }

    private void Acquire(string id, int amount)
    {
        int attempts = 0;
        while (Count(id) < amount)
        {
            if (++attempts > 4000) throw new InvalidOperationException($"No affordable source for {id}");
            switch (id)
            {
                case "cold_iron_ore": case "spirit_jade":
                    string nodeId = id == "spirit_jade" ? "mining_spirit_jade" : "mining_cold_iron_ore";
                    MiningRules.TryGetNode(nodeId, out var node);
                    Learn("mining", node.RequiredMasteryLevel);
                    Run("mining", node.RequiredInputs);
                    var ore = MiningRules.GatherOre(nodeId, Mastery("mining"));
                    Add(ore.ItemId, ore.ItemCount);
                    break;
                case "lingqi_shard":
                    // Planned deterministic fallback, never counted as current runtime mining.
                    Acquire("cold_iron_ore", 3); Spend("cold_iron_ore", 3);
                    PayQi(20); Run("mining", 100); Add(id, 1); _actions++;
                    break;
                case "spirit_herb" when _realm >= 2:
                    GrowHerb();
                    break;
                case "spirit_fish": case "spirit_pearl":
                    FishingRules.TryGetPond("fishing_spirit_fish", out var pond);
                    if (_fishCaught >= FishingRules.Capacity(pond.RecipeId, Mastery("fishing")))
                    {
                        double wait = Math.Max(0, _pondRefreshAt - _seconds);
                        _seconds += wait; _pondWaiting += wait; _fishCaught = 0; Recover(wait);
                    }
                    Run("fishing", pond.RequiredInputs);
                    _fishCaught++;
                    if (_fishCaught >= FishingRules.Capacity(pond.RecipeId, Mastery("fishing"))) _pondRefreshAt = _seconds + 1800;
                    // Use normal fishing for pearls and planned 95% food fishing for fish.
                    var fish = id == "spirit_pearl" ? FishingRules.CatchFish(pond.RecipeId, Mastery("fishing"), _random.Next(100))
                        : new FishingRules.CatchResult(_random.Next(100) < 95 ? "spirit_fish" : "spirit_pearl", 1);
                    Add(fish.ItemId, fish.ItemCount);
                    break;
                default:
                    EncounterCheck(Math.Min(4, _cleared));
                    break;
            }
        }
    }

    private void GrowHerb()
    {
        GardenRules.TryGetCrop("garden_spirit_herb", out var crop);
        var seed = ShopRules.GetItems().Single(x => x.ItemId == ShopRules.ItemSpiritHerbSeed);
        if (!_gardenSeed) { PayGold(seed.Price); _gardenSeed = true; }
        PayGold(GardenRules.PlantingSpiritStones);
        int grow = GardenRules.GetEffectiveGrowthSeconds(crop.RecipeId, Mastery("garden"));
        _seconds += grow; _waiting += grow; _actions += 2; Recover(grow);
        int yield = _minimumYield ? crop.MinOutputCount : _random.Next(crop.MinOutputCount, crop.MaxOutputCount + 1);
        if (Mastery("garden") >= 2) yield--;
        else _gardenSeed = false;
        _spent[crop.SeedItemId] = _spent.GetValueOrDefault(crop.SeedItemId) + 1;
        Add(crop.OutputItemId, Math.Max(1, yield));
    }

    private void Craft(string system, string output, IEnumerable<MaterialCost> materials, double qi, int inputs, int count = 1)
    {
        var costs = materials.ToArray();
        foreach (var cost in costs) Acquire(cost.ItemId, cost.Count);
        PayQi(qi); Run(system, inputs);
        foreach (var cost in costs) Spend(cost.ItemId, cost.Count);
        Add(output, count); _actions++;
    }
    private bool TryPrepareCommonStock(IEnumerable<MaterialCost> costs)
    {
        // Optional counters never turn an unlucky sequence into an endless main-route gate.
        var randomCosts = costs.Where(x => x.ItemId is "broken_talisman" or "spirit_ink" or "beast_bone").ToArray();
        for (int attempt = 0; attempt < 10 && randomCosts.Any(x => Count(x.ItemId) < x.Count); attempt++)
            EncounterCheck(Math.Min(4, _cleared));
        return randomCosts.All(x => Count(x.ItemId) >= x.Count);
    }
    private void NewRecipe(string id)
    {
        string rules = File.ReadAllText(Path.Combine(Root, "docs/design/02_systems.md"));
        string row = rules.Split('\n').Single(x => x.StartsWith($"| `{id}` /"));
        var cells = row.Split('|').Select(x => x.Trim()).ToArray();
        var gate = Regex.Match(cells[7], @"realm≥(\d+)");
        if (_realm < int.Parse(gate.Groups[1].Value)) throw new InvalidOperationException("Early planned recipe");
        string system = id.StartsWith("talisman_") ? "talisman" : id == "recipe_plain_fish" ? "cooking" : "alchemy";
        int mastery = int.Parse(Regex.Match(cells[7], @"Lv(\d+)").Groups[1].Value);
        Learn(system, mastery);
        if ((id is "recipe_yangyuan_dan" or "talisman_zhenling") && _cleared < 3) throw new InvalidOperationException("Counter before region 3 clear");
        string output = Regex.Matches(cells[1], "`([^`]+)`")[1].Groups[1].Value;
        var costs = Regex.Matches(cells[3], "`([^`]+)`×(\\d+)").Select(x => new MaterialCost(x.Groups[1].Value, int.Parse(x.Groups[2].Value))).ToArray();
        if (costs.Length == 0) throw new InvalidOperationException("Planned recipe has no parsed materials");
        if (system == "talisman" && !TryPrepareCommonStock(costs)) return;
        Craft(system, output, costs, double.Parse(cells[4]), int.Parse(cells[5]), int.Parse(cells[6]));
    }
    private void HealStock(int wanted, bool selfSupply)
    {
        while (Count("potion_huiqi_dan") < wanted)
        {
            if (selfSupply)
            {
                AlchemyRules.TryGetRecipe("recipe_huiqi_dan", out var potion);
                Craft("alchemy", potion.OutputPotionId, new[] { new MaterialCost(potion.MaterialItemId, potion.MaterialCount) }, potion.LingqiCost, potion.RequiredInputs, potion.OutputCount);
            }
            else
            {
                var bundle = ShopRules.GetItems().Single(x => x.ItemId == ShopRules.ItemHuiqiDanBundle);
                PayGold(bundle.Price); Add(bundle.GrantItemId, bundle.GrantAmount); _actions++;
            }
        }
    }
    private void Enhance(int level)
    {
        while (_weapon.EnhanceLevel < level)
        {
            if (level > SmithingRules.GetMaxEnhanceLevel(Mastery("smithing"), _weapon.Rarity)) throw new InvalidOperationException("Enhance cap");
            var cost = SmithingRules.GetCost(_weapon.EnhanceLevel, Mastery("smithing"));
            Craft("smithing", "", new[] { new MaterialCost("lingqi_shard", cost.Shards), new MaterialCost("broken_talisman", cost.Talismans) }, cost.Lingqi, cost.RequiredInputs, 0);
            _weapon = _weapon with { EnhanceLevel = _weapon.EnhanceLevel + 1 };
        }
    }
    private void Forge()
    {
        var recipe = Equipment.GetProperty("forge_recipes").EnumerateArray().Single(x => Text(x, "recipe_id") == "forge_weapon_iron_spear");
        foreach (var mastery in recipe.GetProperty("required_mastery").EnumerateObject()) Learn(mastery.Name, mastery.Value.GetInt32());
        var costs = recipe.GetProperty("cost_materials").EnumerateArray().Select(x => new MaterialCost(Text(x, "item_id"), Number(x, "qty"))).ToArray();
        Craft("smithing", "", costs, Number(recipe, "cost_lingqi"), Number(recipe, "required_input_events"), 0);
        var template = Equipment.GetProperty("equipment_templates").EnumerateArray().Single(x => Text(x, "equipment_template_id") == Text(recipe, "output_template_id"));
        var attack = template.GetProperty("main_stat_pool").EnumerateArray().Single(x => Text(x, "stat") == "attack_flat");
        _weapon = new("model_iron_spear", "iron spear", EquipmentSlotType.Weapon, new(AttackFlat: Number(attack, "min")), Rarity: 2, IsEquipped: true);
    }

    private void PrepareBranch(int stage)
    {
        var branch = Routes.GetProperty("branches").EnumerateArray().FirstOrDefault(x => Text(x, "id") == _route);
        if (branch.ValueKind == JsonValueKind.Undefined || _realm < Number(branch, "min_realm")) return;
        foreach (var mastery in branch.GetProperty("recommended_mastery").EnumerateObject()) Learn(mastery.Name, mastery.Value.GetInt32());
        if (_route == "branch_forge" && stage == 1) { Forge(); Enhance(3); }
        if (_route == "branch_supply")
        {
            if (stage == 1)
            {
                GrowHerb();
                NewRecipe("recipe_plain_fish"); Spend("food_plain_fish", 1); _mealSeconds = 1200;
            }
            if (stage == 3) NewRecipe("recipe_yangyuan_dan");
        }
        if (_route == "branch_inner")
        {
            if (!_guardCrafted)
            {
                Craft("formation", "formation_guard_flag", new[] { new MaterialCost("cold_iron_ore", 2), new MaterialCost("spirit_jade", 1) }, 0, 220);
                _defenseRate = FormationRules.GetModifier("formation_guard_flag", Mastery("formation")).DefenseRate;
                _guardCrafted = true;
            }
            if (stage == 3) NewRecipe("talisman_zhenling");
            if (stage == 4)
            {
                TalismanRules.TryGetRecipe("talisman_fire_charm", out var charm);
                if (TryPrepareCommonStock(charm.Inputs)) Craft("talisman", charm.RecipeId, charm.Inputs, charm.LingqiCost, charm.RequiredInputEvents);
                if (!_bodyTrained)
                {
                    BodyCultivationRules.TryGetTechnique("body_cultivation_temper", out var body);
                    if (TryPrepareCommonStock(body.Inputs))
                    {
                        Craft("body_cultivation", "", body.Inputs, body.LingqiCost, body.RequiredInputEvents, 0);
                        _hpRate = body.RewardModifier.MaxHpRate; _bodyTrained = true;
                    }
                }
            }
        }
    }

    internal BudgetResult Simulate()
    {
        for (int region = 0; region < Levels.Length; region++)
        {
            _currentRegion = region;
            ReachRealm(RecommendedRealms[region]);
            if (region >= 2 && _weapon.EnhanceLevel < 3) Enhance(3);
            PrepareBranch(region);
            var level = Levels[region];
            int explore = (int)Math.Ceiling(100 / (level.GetProperty("explore").GetProperty("progress_per_100_inputs").GetDouble() / 100 * level.GetProperty("zone_speed_factor").GetDouble()));
            int interval = (int)Math.Ceiling(GameBalanceConstants.Explore.MonsterRespawnSpacing / GameBalanceConstants.Explore.MonsterMovePxPerFrame) * GameBalanceConstants.Explore.InputsPerMoveFrame;
            for (int remaining = explore; remaining > 0; remaining -= interval) EncounterCheck(region, Math.Min(interval, remaining));
            HealStock(1, _route == "branch_supply");
            var boss = Monsters[Text(level, "boss_monster_id")];
            string counter = _route == "branch_supply" ? "potion_yangyuan_dan" : "talisman_zhenling";
            bool suppress = region == 3 && Count(counter) > 0;
            bool armor = region == 4 && Count("talisman_fire_charm") > 0;
            WaitUntilReady(region);
            var result = Fight(region, _realm, _weapon, boss, suppress, armor, _hpRate, _defenseRate,
                startingHp: _hp, defenseFlat: DefenseFlat);
            _fighting = true;
            Run("dungeon", result.Rounds * Number(boss.GetProperty("combat"), "inputs_per_round"));
            _fighting = false;
            _hp = result.Won ? result.RemainingHp : 0;
            if (!result.Won) _recoveryRemainder = 0;
            if (result.HealUsed) Spend("potion_huiqi_dan", 1);
            if (suppress) Spend(counter, 1);
            if (armor) Spend("talisman_fire_charm", 1);
            _actions++; // Manual Boss challenge.
            if (result.Won)
            {
                Rewards(region, boss, true);
                var first = level.GetProperty("rewards").GetProperty("first_clear");
                _qi += Number(first, "lingqi"); _insight += Number(first, "insight");
                _gold += Number(first, "spirit_stones"); _goldEarned += Number(first, "spirit_stones");
                foreach (var item in first.GetProperty("items").EnumerateArray()) Add(Text(item, "item_id"), Number(item, "qty"));
                _cleared++;
            }
            _stages.Add(new(Text(level, "level_id"), _realm, result, TotalInputs, _gold, Math.Round(_qi, 2), Math.Round(_insight, 2)));
            if (!result.Won) break;
        }
        if (_cleared == Levels.Length) ReachRealm(10);
        return new(_route, _rate, _seed, _minimumYield, _randomMaterialDrops, _cleared == Levels.Length, _realm, TotalInputs, Math.Round(_seconds / 3600, 2),
            _goldSpent, _goldEarned, _qiSpent, _insightSpent, _actions, Math.Round(_waiting / 60, 2), Math.Round(_pondWaiting / 60, 2),
            Math.Round(_recoveryWaiting, 2), _ordinaryDefeats, _inputs, _spent, _stages, _items);
    }
}
