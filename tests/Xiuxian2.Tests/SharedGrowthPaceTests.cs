using System.Text.Json;
using Xiuxian.Scripts.Services;
using Xunit.Abstractions;

namespace Xiuxian.Tests;

public sealed class SharedGrowthPaceTests(ITestOutputHelper output)
{
    private sealed record Sample(int Day, double InputsPerMinute, double OldExperience, double Experience,
        int RealmIfBreakthroughConfirmed, double BankedExperience, double BasicInputs, double RealInputs);

    [Fact]
    public void LowMediumAndHighInputWorkdaysRetainInputValueWithoutAnOfflineBasicGrowthAdvantage()
    {
        var samples = new List<Sample>();
        foreach (double rate in new[] { 0.0, 2.0, 30.0, 90.0 })
        {
            var clock = new SharedGrowthClock(); clock.Claim(1000); double basic = 0, total = 0, bank = 0; int realm = 1;
            for (int day = 1; day <= 7; day++)
            {
                // 8h online / 16h offline, default non-cultivation activity, AP=1. No gameplay or spending is simulated.
                double online = clock.Claim(1000 + (day - 1) * 86400 + 8 * 3600);
                var saved = clock.ToPlainDictionary(); clock = new(); clock.Restore(saved);
                double offline = clock.Claim(1000 + day * 86400); basic += online + offline;
                double real = rate * 8 * 60;
                double gained = OfflineActionSettlementRules.BuildSharedFromBudget(online + offline, 1, "dungeon").RealmExpGain
                    + ResourceConversionRules.Calculate("dungeon", real, (int)real, 0, 1, 1, 0).RealmExp;
                total += gained; bank += gained;
                while (realm < 9 && bank >= PlayerProgressState.GetExpRequired(realm)) { bank -= PlayerProgressState.GetExpRequired(realm); realm++; }
                if (day is 1 or 3 or 7)
                    samples.Add(new(day, rate, day * (real + OfflineSettlementRules.CalculateOfflineInputBudget(16 * 3600)) * .28,
                        total, realm, bank, basic, day * real));
                Assert.Equal(day * 6360, basic, 5);
            }
        }
        foreach (int day in new[] { 1, 3, 7 })
        {
            var rows = samples.Where(x => x.Day == day).ToArray();
            Assert.True(rows.Zip(rows.Skip(1), (a, b) => b.Experience > a.Experience).All(x => x));
            Assert.Equal(day * 1780.8, rows[0].Experience, 5);
            Assert.All(rows, row => Assert.Equal(row.Experience, (row.BasicInputs + row.RealInputs) * .28, 5));
        }
        var firstBreakthrough = new[] { 0.0, 2.0, 30.0, 90.0 }.Select(rate =>
        {
            double exp = 0; int minute = 0; var clock = new SharedGrowthClock(); clock.Claim(1000);
            while (exp < PlayerProgressState.GetExpRequired(1)) { minute++; exp += (clock.Claim(1000 + minute * 60) + rate) * .28; }
            return new { InputsPerMinute = rate, OldMinutes = rate == 0 ? (double?)null : Math.Ceiling(300 / (rate * .28)), NewMinutes = minute };
        }).ToArray();
        Assert.Equal(new[] { 119, 96, 26, 11 }, firstBreakthrough.Select(x => x.NewMinutes));
        foreach (var sample in samples) output.WriteLine(JsonSerializer.Serialize(sample));
        string? report = Environment.GetEnvironmentVariable("XIUXIAN_GROWTH_REPORT");
        if (!string.IsNullOrWhiteSpace(report))
        {
            string root = ChapterBalanceModel.Root; string path = Path.GetFullPath(Path.Combine(root, report));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Report outside repository");
            File.WriteAllText(path, JsonSerializer.Serialize(new {
                Model = "shared_basic_growth_budget", Scope = "Formula scenarios using the runtime growth clock, resource conversion and realm thresholds; not player testing or a full chapter playthrough",
                Assumptions = new[] { "8h online / 16h offline per day", "keyboard AP=1; non-cultivation input rate0.28", "no meals, shops, mastery purchases, spending or equipment", "realm estimate assumes breakthrough is confirmed whenever affordable; stops at realm9 without final Boss clear", "no claims about first Boss time, win rate, farming or UI management count", "real input and shared time growth are counted separately once" },
                Samples = samples, FirstRealmBreakthrough = firstBreakthrough,
                FirstHour = new[] { 0.0, 2.0, 30.0, 90.0 }.Select(rate => new {
                    InputsPerMinute = rate, OldExperience = rate * 60 * .28,
                    Experience = (SharedGrowthClock.BudgetBetween(0, 3600) + rate * 60) * .28 }),
                RestartCheck = new { WholeDayInputs = 6360, ReconnectedInputs = 6360, OldFreshBudgetEveryTenMinutes = 17280 },
                LongAbsenceCheck = new { OneWeekSingleClaimInputs = 6360, SevenDailyClaimsInputs = 44520 }
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
