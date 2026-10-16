using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Xiuxian.Scripts.Tests;

// In-memory experimental configuration. Never writes the shipped content file.
public static class FirstBossScenarioRules
{
    public static string Apply(string content, int exploreInputs, int roundInputs)
    {
        if (exploreInputs < 100 || exploreInputs > 10000) throw new ArgumentOutOfRangeException(nameof(exploreInputs));
        if (roundInputs < 0 || roundInputs > 50) throw new ArgumentOutOfRangeException(nameof(roundInputs));
        var root = JsonNode.Parse(content)!.AsObject();
        JsonObject? first = null;
        foreach (var level in root["levels"]!.AsArray())
            if ((string?)level!["level_id"] == "lv_qi_001") first = level.AsObject();
        if (first == null) throw new InvalidOperationException("First region missing.");
        first["explore"]!["progress_per_100_inputs"] = 10000.0 / exploreInputs;
        var ids = new HashSet<string> { (string)first["boss_monster_id"]! };
        foreach (var spawn in first["spawn_table"]!.AsArray()) ids.Add((string)spawn!["monster_id"]!);
        if (roundInputs > 0)
            foreach (var monster in root["monsters"]!.AsArray())
                if (ids.Contains((string)monster!["monster_id"]!)) monster["combat"]!["inputs_per_round"] = roundInputs;
        return root.ToJsonString();
    }
}
