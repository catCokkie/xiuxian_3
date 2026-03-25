using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class SaveMigrationRulesTests
{
    [Fact]
    public void NeedsMigration_ReturnsTrueForOlderSchema()
    {
        Assert.True(SaveMigrationRules.NeedsMigration(4));
    }

    [Fact]
    public void NeedsMigration_ReturnsFalseForLatestSchema()
    {
        Assert.False(SaveMigrationRules.NeedsMigration(SaveMigrationRules.LatestVersion));
    }

    [Fact]
    public void MigrateToLatest_PromotesLegacyUiAndActionFields()
    {
        SaveMigrationRules.MigrationStore cfg = new();
        cfg.SetValue("meta", "version", 1);
        cfg.SetValue("ui", "submenu_active_tab", "StatsTab");
        cfg.SetValue("action", "mode", new System.Collections.Generic.Dictionary<string, object>
        {
            ["mode_id"] = PlayerActionState.ModeCultivation
        });

        SaveMigrationRules.MigrateToLatest(cfg, 1);

        Assert.Equal(SaveMigrationRules.LatestVersion, System.Convert.ToInt32(cfg.GetValue("meta", "version", 0)));
        Assert.Equal("StatsTab", System.Convert.ToString(cfg.GetValue("ui", "submenu_active_left_tab", string.Empty)));
        Assert.Equal("BugTab", System.Convert.ToString(cfg.GetValue("ui", "submenu_active_right_tab", string.Empty)));

        var action =
            (System.Collections.Generic.Dictionary<string, object>)cfg.GetValue("action", "mode", new System.Collections.Generic.Dictionary<string, object>());
        Assert.Equal(PlayerActionState.ActionCultivation, System.Convert.ToString(action["mode_id"]));
        Assert.Equal(PlayerActionState.ActionCultivation, System.Convert.ToString(action["action_id"]));
        Assert.Equal(string.Empty, System.Convert.ToString(action["action_target_id"]));
        Assert.Equal(string.Empty, System.Convert.ToString(action["action_variant"]));
    }

    [Fact]
    public void MigrateToLatest_BackfillsEquipmentContainersForV4Save()
    {
        SaveMigrationRules.MigrationStore cfg = new();
        cfg.SetValue("meta", "version", 4);
        cfg.SetValue("backpack", "items", new System.Collections.Generic.Dictionary<string, object>
        {
            ["herb"] = 3
        });

        SaveMigrationRules.MigrateToLatest(cfg, 4);

        var backpack =
            (System.Collections.Generic.Dictionary<string, object>)cfg.GetValue("backpack", "items", new System.Collections.Generic.Dictionary<string, object>());
        Assert.Equal(SaveMigrationRules.LatestVersion, System.Convert.ToInt32(cfg.GetValue("meta", "version", 0)));
        Assert.True(backpack.ContainsKey("__equipment_profiles"));
        Assert.True(backpack.ContainsKey("__equipment_instances"));
        Assert.Equal(0L, System.Convert.ToInt64(cfg.GetValue("meta", "last_saved_unix", -1L)));

        var equipped = cfg.GetValue("equipment", "equipped", new System.Collections.Generic.Dictionary<string, object>());
        Assert.IsType<System.Collections.Generic.Dictionary<string, object>>(equipped);
    }

    [Fact]
    public void MigrateToLatest_PromotesV5SaveToV6WithSpiritStoneAndLoopDefaults()
    {
        SaveMigrationRules.MigrationStore cfg = new();
        cfg.SetValue("meta", "version", 5);
        cfg.SetValue("resource", "wallet", new System.Collections.Generic.Dictionary<string, object>
        {
            ["lingqi"] = 12.0,
            ["insight"] = 3.0,
            ["pet_affinity"] = 1.0,
        });
        cfg.SetValue("explore", "runtime", new System.Collections.Generic.Dictionary<string, object>
        {
            ["zone_id"] = "lv_qi_003",
            ["explore_progress"] = 100.0,
        });
        cfg.SetValue("backpack", "items", new System.Collections.Generic.Dictionary<string, object>
        {
            ["novice_breakthrough_pill"] = 2,
            ["spirit_herb"] = 5,
        });
        cfg.SetValue("action", "mode", new System.Collections.Generic.Dictionary<string, object>
        {
            ["mode_id"] = "unknown_mode",
            ["action_id"] = "unknown_mode",
            ["action_target_id"] = "lv_qi_003",
            ["action_variant"] = "",
        });

        SaveMigrationRules.MigrateToLatest(cfg, 5);

        Assert.Equal(6, System.Convert.ToInt32(cfg.GetValue("meta", "version", 0)));

        var wallet = (System.Collections.Generic.Dictionary<string, object>)cfg.GetValue("resource", "wallet", new System.Collections.Generic.Dictionary<string, object>());
        Assert.Equal(0L, System.Convert.ToInt64(wallet["spirit_stones"]));

        var alchemy = (System.Collections.Generic.Dictionary<string, object>)cfg.GetValue("alchemy", "state", new System.Collections.Generic.Dictionary<string, object>());
        Assert.Equal(string.Empty, System.Convert.ToString(alchemy["selected_recipe"]));
        Assert.Equal(0.0, System.Convert.ToDouble(alchemy["progress"]));

        var smithing = (System.Collections.Generic.Dictionary<string, object>)cfg.GetValue("smithing", "state", new System.Collections.Generic.Dictionary<string, object>());
        Assert.Equal(string.Empty, System.Convert.ToString(smithing["target_equipment_id"]));
        Assert.Equal(0.0, System.Convert.ToDouble(smithing["progress"]));

        var potions = cfg.GetValue("backpack", "potions", new System.Collections.Generic.Dictionary<string, object>());
        Assert.IsType<System.Collections.Generic.Dictionary<string, object>>(potions);

        var boss = (System.Collections.Generic.Dictionary<string, object>)cfg.GetValue("boss", "runtime", new System.Collections.Generic.Dictionary<string, object>());
        Assert.Equal(-1L, System.Convert.ToInt64(boss["current_hp"]));
        Assert.IsType<System.Collections.Generic.List<object>>(boss["defeated_zones"]);

        var unlocked = (System.Collections.Generic.List<object>)cfg.GetValue("level", "unlocked_zone_ids", new System.Collections.Generic.List<object>());
        Assert.Equal(new[] { "lv_qi_001", "lv_qi_002", "lv_qi_003" }, unlocked.Cast<string>().ToArray());

        var zoneCycles = cfg.GetValue("level", "zone_cycle_counts", new System.Collections.Generic.Dictionary<string, object>());
        Assert.IsType<System.Collections.Generic.Dictionary<string, object>>(zoneCycles);

        var explore = (System.Collections.Generic.Dictionary<string, object>)cfg.GetValue("explore", "runtime", new System.Collections.Generic.Dictionary<string, object>());
        Assert.Equal(0.0, System.Convert.ToDouble(explore["explore_progress"]));

        var backpack = (System.Collections.Generic.Dictionary<string, object>)cfg.GetValue("backpack", "items", new System.Collections.Generic.Dictionary<string, object>());
        Assert.False(backpack.ContainsKey("novice_breakthrough_pill"));
        Assert.Equal("dungeon", System.Convert.ToString(((System.Collections.Generic.Dictionary<string, object>)cfg.GetValue("action", "mode", new System.Collections.Generic.Dictionary<string, object>()))["mode_id"]));
    }

    [Fact]
    public void MigrateToLatest_DoesNothingWhenAlreadyLatest()
    {
        SaveMigrationRules.MigrationStore cfg = new();
        cfg.SetValue("meta", "version", SaveMigrationRules.LatestVersion);
        cfg.SetValue("ui", "submenu_active_left_tab", "BackpackTab");

        SaveMigrationRules.MigrateToLatest(cfg, SaveMigrationRules.LatestVersion);

        Assert.Equal("BackpackTab", System.Convert.ToString(cfg.GetValue("ui", "submenu_active_left_tab", string.Empty)));
        Assert.False(cfg.HasSectionKey("ui", "submenu_active_right_tab"));
    }
}
