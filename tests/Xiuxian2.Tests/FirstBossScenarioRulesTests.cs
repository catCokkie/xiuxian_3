using System.Text.Json.Nodes;
using Xiuxian.Scripts.Tests;

namespace Xiuxian.Tests;

public sealed class FirstBossScenarioRulesTests
{
    private const string Content = """
    {"levels":[{"level_id":"lv_qi_001","explore":{"progress_per_100_inputs":2},"boss_monster_id":"boss","spawn_table":[{"monster_id":"slime"}]},{"level_id":"lv_qi_002","explore":{"progress_per_100_inputs":2.4}}],"monsters":[{"monster_id":"slime","combat":{"inputs_per_round":18,"hp":20}},{"monster_id":"boss","combat":{"inputs_per_round":20,"hp":80}},{"monster_id":"later","combat":{"inputs_per_round":22}}]}
    """;

    [Theory]
    [InlineData(1500, 2)] [InlineData(1500, 4)] [InlineData(1500, 8)]
    [InlineData(2500, 2)] [InlineData(2500, 4)] [InlineData(2500, 8)]
    [InlineData(5000, 2)] [InlineData(5000, 4)] [InlineData(5000, 8)]
    public void ScenarioChangesOnlyFirstRegionProgressAndItsMonsters(int explore, int rounds)
    {
        var result = JsonNode.Parse(FirstBossScenarioRules.Apply(Content, explore, rounds))!;
        Assert.Equal(10000.0 / explore, (double)result["levels"]![0]!["explore"]!["progress_per_100_inputs"]!);
        Assert.Equal(2.4, (double)result["levels"]![1]!["explore"]!["progress_per_100_inputs"]!);
        Assert.Equal(rounds, (int)result["monsters"]![0]!["combat"]!["inputs_per_round"]!);
        Assert.Equal(rounds, (int)result["monsters"]![1]!["combat"]!["inputs_per_round"]!);
        Assert.Equal(22, (int)result["monsters"]![2]!["combat"]!["inputs_per_round"]!);
        Assert.Equal(80, (int)result["monsters"]![1]!["combat"]!["hp"]!);
    }

    [Fact]
    public void BaselinePreservesPerMonsterThresholds()
    {
        var result = JsonNode.Parse(FirstBossScenarioRules.Apply(Content, 5000, 0))!;
        Assert.Equal(18, (int)result["monsters"]![0]!["combat"]!["inputs_per_round"]!);
        Assert.Equal(20, (int)result["monsters"]![1]!["combat"]!["inputs_per_round"]!);
    }

    [Theory]
    [InlineData(0, 4)] [InlineData(10001, 4)] [InlineData(1500, -1)] [InlineData(1500, 51)]
    public void InvalidScenariosFailBeforeAnyRun(int explore, int rounds)
        => Assert.Throws<ArgumentOutOfRangeException>(() => FirstBossScenarioRules.Apply(Content, explore, rounds));
}
