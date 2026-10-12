using System.Text.Json;
using Xiuxian.Scripts.Services;
using Xunit.Abstractions;

namespace Xiuxian.Tests;

public sealed class ChapterBalanceTests(ITestOutputHelper output)
{
    [Fact]
    public void MinimumEquipmentCanClearRecommendedBossesWithoutRareDropsOrCriticalLuck()
    {
        var weapon = EquipmentStarterLoadout.CreateDefaultProfiles()[0] with { EnhanceLevel = 3 };
        var results = new List<ChapterBalanceModel.CombatResult>();
        for (int i = 0; i < ChapterBalanceModel.Levels.Length; i++)
        {
            string id = ChapterBalanceModel.Text(ChapterBalanceModel.Levels[i], "boss_monster_id");
            results.Add(ChapterBalanceModel.Fight(i, ChapterBalanceModel.RecommendedRealms[i], weapon, ChapterBalanceModel.Monsters[id]));
        }
        output.WriteLine(JsonSerializer.Serialize(results));
        Assert.All(results, result => Assert.True(result.Won, $"Unpassable baseline: ATK {result.PlayerAttack}, DEF {result.MonsterDefense}, rounds {result.Rounds}"));
    }

    [Fact]
    public void RegenerationCounterOffersAnEarlierChallengeWithoutBecomingRequired()
    {
        var weapon = EquipmentStarterLoadout.CreateDefaultProfiles()[0] with { EnhanceLevel = 3 };
        var boss = ChapterBalanceModel.Monsters[ChapterBalanceModel.Text(ChapterBalanceModel.Levels[3], "boss_monster_id")];
        Assert.False(ChapterBalanceModel.Fight(3, 7, weapon, boss).Won);
        Assert.True(ChapterBalanceModel.Fight(3, 7, weapon, boss, suppressRegeneration: true).Won);
        Assert.True(ChapterBalanceModel.Fight(3, 8, weapon, boss).Won);
    }

    [Fact]
    public void FinalBossRewardsBasicHealingAndArmorPreparation()
    {
        var weapon = EquipmentStarterLoadout.CreateDefaultProfiles()[0] with { EnhanceLevel = 3 };
        var boss = ChapterBalanceModel.Monsters[ChapterBalanceModel.Text(ChapterBalanceModel.Levels[4], "boss_monster_id")];
        var baseline = ChapterBalanceModel.Fight(4, 9, weapon, boss);
        Assert.True(baseline.Won);
        Assert.True(baseline.HealUsed);
        Assert.False(ChapterBalanceModel.Fight(4, 9, weapon, boss, allowHeal: false).Won);
        var prepared = ChapterBalanceModel.Fight(4, 9, weapon, boss, armorCharm: true);
        Assert.True(prepared.Won);
        Assert.True(prepared.Rounds < baseline.Rounds);
    }

    [Fact]
    public void FourBlueprintRoutesHaveFundedSinglePipelineBudgets()
    {
        var results = new List<ChapterBalanceModel.BudgetResult>();
        foreach (string route in new[] { "minimum", "branch_forge", "branch_supply", "branch_inner" })
            foreach (double rate in new[] { 8.0, 30.0, 90.0 })
                foreach (int seed in new[] { 17, 2026, 910 })
                    foreach (bool minimumYield in new[] { false, true })
                        results.Add(new ChapterBalanceModel(route, rate, seed, minimumYield).Simulate());
        var stressCases = new[] { "minimum", "branch_forge", "branch_supply", "branch_inner" }
            .Select(route => new ChapterBalanceModel(route, 8, 2026, true, randomMaterialDrops: false).Simulate()).ToList();
        string? report = Environment.GetEnvironmentVariable("XIUXIAN_ROUTE_REPORT");
        if (!string.IsNullOrWhiteSpace(report))
        {
            string path = Path.GetFullPath(Path.Combine(ChapterBalanceModel.Root, report));
            if (!path.StartsWith(ChapterBalanceModel.Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Report outside repository");
            File.WriteAllText(path, JsonSerializer.Serialize(new
            {
                Model = "design_budget_simulation",
                Scope = "Continuous HP and runtime battle/recovery/stat rules with planned operations; not a native UI or full queue playthrough",
                Assumptions = new[] { "one input pipeline", "keyboard AP=1", "no critical hits", "persistent absolute HP; default boss-only healing, at most one basic heal per battle",
                    "no rare equipment or ingredients", "ordinary drops use fixed seeds and actual quotas/pity", "conservative queue encounter estimate",
                    "garden waiting is charged separately, without free input income", "basic armor retained; optional first-clear armor not required",
                    "exploration pauses for sampled ordinary encounters; farming costs are charged separately", "planned actions count is not measured UI clicks or return visits",
                    "out-of-combat recovery is 5% max HP per real second; starts require 70%; extra waits pay no inputs or currency",
                    "battle calls ExploreGameLogic and a single shared stat composition", "input income is online; no offline income is added", "manual realm 10 breakthrough requires all five first clears",
                    "extra preparation farming is charged without reducing later exploration budgets, which overestimates inputs" },
                PlannedOperations = new[] { "forge_weapon_iron_spear", "deep_mining_recycle", "deep_garden_reserve_seed", "deep_fishing_supply", "recipe_plain_fish", "recipe_yangyuan_dan", "talisman_zhenling" },
                Cases = results,
                StressCases = stressCases,
                StressScope = "Zero weighted dungeon material rewards; configured guarantees/pity remain, fishing uses its ordinary seeded rule; optional counter farming stops after ten sampled opportunities"
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        foreach (var result in results) output.WriteLine($"{result.Route} {result.InputsPerMinute}/min seed={result.Seed} minYield={result.MinimumYield}: win={result.AllBossesWon} inputs={result.Inputs} gold={result.GoldSpent} qi={result.LingqiSpent} insight={result.InsightSpent} actions={result.PlannedActions}");
        Assert.All(results.Concat(stressCases), result =>
        {
            Assert.True(result.AllBossesWon, $"{result.Route} failed at {result.Stages.Last().Region}");
            Assert.Equal(10, result.FinalRealm);
            Assert.Equal(result.Inputs, result.InputsByMode.Values.Sum());
            Assert.All(result.Inventory.Values, count => Assert.True(count >= 0));
            Assert.All(result.Stages, stage => Assert.True(stage.Gold >= 0 && stage.Lingqi >= 0 && stage.Insight >= 0));
            Assert.All(result.Stages, stage => Assert.InRange(stage.Combat.StartingHp, BattleRecoveryRules.ReadyHp(stage.Combat.MaxHp), stage.Combat.MaxHp));
            Assert.DoesNotContain(result.MaterialsSpent.Keys, id => ChapterBalanceModel.ExcludedRare.Contains(id));
        });
        Assert.True(stressCases.Single(x => x.Route == "branch_forge").InputsByMode["mining"] > results.First(x => x.Route == "branch_forge").InputsByMode["mining"]);
    }
}
