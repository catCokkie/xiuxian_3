using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Centralizes save schema upgrades so future persistence changes can be appended
    /// without scattering version checks across controllers.
    /// </summary>
    public static class SaveMigrationRules
    {
        public const int LatestVersion = 6;

        private const string DefaultLeftTab = "CultivationTab";
        private const string DefaultRightTab = "BugTab";

        public sealed class MigrationStore : IMigrationStore
        {
            private readonly Dictionary<string, Dictionary<string, object>> _sections = new(StringComparer.Ordinal);

            public bool HasSectionKey(string section, string key)
            {
                return _sections.TryGetValue(section, out Dictionary<string, object> values) && values.ContainsKey(key);
            }

            public object GetValue(string section, string key, object defaultValue)
            {
                if (_sections.TryGetValue(section, out Dictionary<string, object> values) && values.ContainsKey(key))
                {
                    return values[key];
                }

                return defaultValue;
            }

            public void SetValue(string section, string key, object value)
            {
                if (!_sections.TryGetValue(section, out Dictionary<string, object> values))
                {
                    values = new Dictionary<string, object>(StringComparer.Ordinal);
                    _sections[section] = values;
                }

                values[key] = value;
            }
        }

        public static bool NeedsMigration(int savedVersion) => savedVersion < LatestVersion;

        public static void MigrateToLatest(ConfigFile cfg, int fromVersion)
        {
            MigrateToLatest(new ConfigFileMigrationStore(cfg), fromVersion);
        }

        public static void MigrateToLatest(MigrationStore cfg, int fromVersion)
        {
            MigrateToLatest((IMigrationStore)cfg, fromVersion);
        }

        private static void MigrateToLatest(IMigrationStore cfg, int fromVersion)
        {
            int version = NormalizeStartingVersion(fromVersion);
            while (version < LatestVersion)
            {
                switch (version)
                {
                    case 1:
                        MigrateV1ToV2(cfg);
                        version = 2;
                        break;
                    case 2:
                        MigrateV2ToV3(cfg);
                        version = 3;
                        break;
                    case 3:
                        MigrateV3ToV4(cfg);
                        version = 4;
                        break;
                    case 4:
                        MigrateV4ToV5(cfg);
                        version = 5;
                        break;
                    case 5:
                        MigrateV5ToV6(cfg);
                        version = 6;
                        break;
                    default:
                        version = LatestVersion;
                        break;
                }

                cfg.SetValue("meta", "version", version);
            }
        }

        private static int NormalizeStartingVersion(int fromVersion)
        {
            if (fromVersion <= 0)
            {
                return 1;
            }

            return Math.Min(fromVersion, LatestVersion);
        }

        private static void MigrateV1ToV2(IMigrationStore cfg)
        {
            if (!cfg.HasSectionKey("ui", "submenu_active_left_tab"))
            {
                string legacyTab = ReadString(cfg.GetValue("ui", "submenu_active_tab", DefaultLeftTab), DefaultLeftTab);
                cfg.SetValue("ui", "submenu_active_left_tab", string.IsNullOrEmpty(legacyTab) ? DefaultLeftTab : legacyTab);
            }

            if (!cfg.HasSectionKey("ui", "submenu_active_right_tab"))
            {
                cfg.SetValue("ui", "submenu_active_right_tab", DefaultRightTab);
            }
        }

        private static void MigrateV2ToV3(IMigrationStore cfg)
        {
            Dictionary<string, object> action = EnsureDictionaryValue(cfg, "action", "mode");
            string persistedActionId = ReadString(GetDictionaryValue(action, "action_id"), string.Empty);
            string persistedTargetId = ReadString(GetDictionaryValue(action, "action_target_id"), string.Empty);
            string persistedVariant = ReadString(GetDictionaryValue(action, "action_variant"), string.Empty);
            string legacyModeId = ReadString(GetDictionaryValue(action, "mode_id"), string.Empty);

            PlayerActionStateRules.PlayerActionStateData normalized = PlayerActionStateRules.FromPersistedValues(
                persistedActionId,
                persistedTargetId,
                persistedVariant,
                legacyModeId);

            action["mode_id"] = normalized.ActionId;
            action["action_id"] = normalized.ActionId;
            action["action_target_id"] = normalized.ActionTargetId;
            action["action_variant"] = normalized.ActionVariant;
            cfg.SetValue("action", "mode", action);
        }

        private static void MigrateV3ToV4(IMigrationStore cfg)
        {
            if (!cfg.HasSectionKey("settings", "system"))
            {
                cfg.SetValue("settings", "system", new Dictionary<string, object>(StringComparer.Ordinal));
            }
        }

        private static void MigrateV4ToV5(IMigrationStore cfg)
        {
            Dictionary<string, object> backpack = EnsureDictionaryValue(cfg, "backpack", "items");
            if (!backpack.ContainsKey("__equipment_profiles"))
            {
                backpack["__equipment_profiles"] = new List<object>();
            }

            if (!backpack.ContainsKey("__equipment_instances"))
            {
                backpack["__equipment_instances"] = new List<object>();
            }

            cfg.SetValue("backpack", "items", backpack);

            if (!cfg.HasSectionKey("equipment", "equipped"))
            {
                cfg.SetValue("equipment", "equipped", new Dictionary<string, object>(StringComparer.Ordinal));
            }

            if (!cfg.HasSectionKey("meta", "last_saved_unix"))
            {
                cfg.SetValue("meta", "last_saved_unix", 0L);
            }
        }

        private static void MigrateV5ToV6(IMigrationStore cfg)
        {
            Dictionary<string, object> wallet = EnsureDictionaryValue(cfg, "resource", "wallet");
            if (!wallet.ContainsKey("spirit_stones"))
            {
                wallet["spirit_stones"] = 0;
            }
            cfg.SetValue("resource", "wallet", wallet);

            Dictionary<string, object> alchemy = EnsureDictionaryValue(cfg, "alchemy", "state");
            if (!alchemy.ContainsKey("selected_recipe"))
            {
                alchemy["selected_recipe"] = string.Empty;
            }
            if (!alchemy.ContainsKey("progress"))
            {
                alchemy["progress"] = 0.0;
            }
            cfg.SetValue("alchemy", "state", alchemy);

            Dictionary<string, object> smithing = EnsureDictionaryValue(cfg, "smithing", "state");
            if (!smithing.ContainsKey("target_equipment_id"))
            {
                smithing["target_equipment_id"] = string.Empty;
            }
            if (!smithing.ContainsKey("progress"))
            {
                smithing["progress"] = 0.0;
            }
            cfg.SetValue("smithing", "state", smithing);

            if (!cfg.HasSectionKey("backpack", "potions"))
            {
                cfg.SetValue("backpack", "potions", new Dictionary<string, object>(StringComparer.Ordinal));
            }

            Dictionary<string, object> boss = EnsureDictionaryValue(cfg, "boss", "runtime");
            if (!boss.ContainsKey("defeated_zones"))
            {
                boss["defeated_zones"] = new List<object>();
            }
            if (!boss.ContainsKey("current_hp"))
            {
                boss["current_hp"] = -1;
            }
            cfg.SetValue("boss", "runtime", boss);

            Dictionary<string, object> explore = EnsureDictionaryValue(cfg, "explore", "runtime");
            string zoneId = ReadString(GetDictionaryValue(explore, "zone_id"), "lv_qi_001");
            double progress = ReadDouble(GetDictionaryValue(explore, "explore_progress"), 0.0);
            if (progress >= 100.0)
            {
                explore["explore_progress"] = 0.0;
            }
            cfg.SetValue("explore", "runtime", explore);

            if (!cfg.HasSectionKey("level", "zone_cycle_counts"))
            {
                cfg.SetValue("level", "zone_cycle_counts", new Dictionary<string, object>(StringComparer.Ordinal));
            }

            if (!cfg.HasSectionKey("level", "unlocked_zone_ids"))
            {
                cfg.SetValue("level", "unlocked_zone_ids", BuildUnlockedZoneIds(zoneId));
            }

            Dictionary<string, object> backpack = EnsureDictionaryValue(cfg, "backpack", "items");
            backpack.Remove("novice_breakthrough_pill");
            cfg.SetValue("backpack", "items", backpack);

            Dictionary<string, object> action = EnsureDictionaryValue(cfg, "action", "mode");
            string modeId = ReadString(GetDictionaryValue(action, "mode_id"), PlayerActionState.ActionDungeon);
            if (!IsSupportedMode(modeId))
            {
                action["mode_id"] = PlayerActionState.ActionDungeon;
                action["action_id"] = PlayerActionState.ActionDungeon;
            }
            cfg.SetValue("action", "mode", action);
        }

        private static List<object> BuildUnlockedZoneIds(string zoneId)
        {
            string[] orderedZones = { "lv_qi_001", "lv_qi_002", "lv_qi_003", "lv_qi_004", "lv_qi_005" };
            int targetIndex = Array.IndexOf(orderedZones, zoneId);
            if (targetIndex < 0)
            {
                targetIndex = 0;
            }

            var result = new List<object>();
            for (int i = 0; i <= targetIndex; i++)
            {
                result.Add(orderedZones[i]);
            }

            return result;
        }

        private static bool IsSupportedMode(string modeId)
        {
            return modeId == PlayerActionState.ActionDungeon
                || modeId == PlayerActionState.ActionCultivation
                || modeId == "alchemy"
                || modeId == "smithing";
        }

        private static Dictionary<string, object> EnsureDictionaryValue(IMigrationStore cfg, string section, string key)
        {
            object data = cfg.GetValue(section, key, new Dictionary<string, object>(StringComparer.Ordinal));
            if (data is Dictionary<string, object> dict)
            {
                return dict;
            }

            return new Dictionary<string, object>(StringComparer.Ordinal);
        }

        private interface IMigrationStore
        {
            bool HasSectionKey(string section, string key);
            object GetValue(string section, string key, object defaultValue);
            void SetValue(string section, string key, object value);
        }

        private sealed class ConfigFileMigrationStore : IMigrationStore
        {
            private readonly ConfigFile _cfg;

            public ConfigFileMigrationStore(ConfigFile cfg)
            {
                _cfg = cfg;
            }

            public bool HasSectionKey(string section, string key)
            {
                return _cfg.HasSectionKey(section, key);
            }

            public object GetValue(string section, string key, object defaultValue)
            {
                return ConvertFromVariant(_cfg.GetValue(section, key, ConvertToVariant(defaultValue)));
            }

            public void SetValue(string section, string key, object value)
            {
                _cfg.SetValue(section, key, ConvertToVariant(value));
            }
        }

        private static object GetDictionaryValue(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out object value) ? value : string.Empty;
        }

        private static string ReadString(object value, string fallback)
        {
            if (value is string text)
            {
                return string.IsNullOrEmpty(text) ? fallback : text;
            }

            return value?.ToString() ?? fallback;
        }

        private static double ReadDouble(object value, double fallback)
        {
            return value switch
            {
                double d => d,
                float f => f,
                int i => i,
                long l => l,
                _ => double.TryParse(value?.ToString(), out double parsed) ? parsed : fallback,
            };
        }

        private static Variant ConvertToVariant(object value)
        {
            switch (value)
            {
                case null:
                    return default;
                case string text:
                    return text;
                case int intValue:
                    return intValue;
                case long longValue:
                    return longValue;
                case float floatValue:
                    return floatValue;
                case double doubleValue:
                    return doubleValue;
                case bool boolValue:
                    return boolValue;
                case Dictionary<string, object> dict:
                    var godotDict = new Godot.Collections.Dictionary<string, Variant>();
                    foreach ((string key, object itemValue) in dict)
                    {
                        godotDict[key] = ConvertToVariant(itemValue);
                    }

                    return godotDict;
                case IList list:
                    var array = new Godot.Collections.Array<Variant>();
                    foreach (object item in list)
                    {
                        array.Add(ConvertToVariant(item));
                    }

                    return array;
                default:
                    return value.ToString() ?? string.Empty;
            }
        }

        private static object ConvertFromVariant(Variant value)
        {
            switch (value.VariantType)
            {
                case Variant.Type.Nil:
                    return null;
                case Variant.Type.Bool:
                    return value.AsBool();
                case Variant.Type.Int:
                    return value.AsInt64();
                case Variant.Type.Float:
                    return value.AsDouble();
                case Variant.Type.String:
                    return value.AsString();
                case Variant.Type.Dictionary:
                    var dict = new Dictionary<string, object>(StringComparer.Ordinal);
                    var godotDict = (Godot.Collections.Dictionary<string, Variant>)value;
                    foreach (string key in godotDict.Keys)
                    {
                        dict[key] = ConvertFromVariant(godotDict[key]);
                    }

                    return dict;
                case Variant.Type.Array:
                    var list = new List<object>();
                    var godotArray = (Godot.Collections.Array<Variant>)value;
                    foreach (Variant item in godotArray)
                    {
                        list.Add(ConvertFromVariant(item));
                    }

                    return list;
                default:
                    return value.ToString();
            }
        }
    }
}
