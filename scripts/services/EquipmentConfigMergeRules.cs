using System.Linq;
using System.Text.Json.Nodes;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentConfigMergeRules
    {
        public static string Merge(string levelText, string equipmentText)
        {
            var root = JsonNode.Parse(levelText)!.AsObject();
            var equipment = JsonNode.Parse(equipmentText)!.AsObject();
            foreach (string section in new[] { "equipment_series", "equipment_templates", "equipment_exchange_recipes" })
                if (equipment[section] != null) root[section] = equipment[section]!.DeepClone();

            if (equipment["drop_table_equipment_examples"] is JsonArray additions && root["drop_tables"] is JsonArray tables)
                foreach (var addition in additions.OfType<JsonObject>())
                {
                    var table = tables.OfType<JsonObject>().FirstOrDefault(x => x["drop_table_id"]?.ToString() == addition["drop_table_id"]?.ToString());
                    if (table?["entries"] is not JsonArray entries || addition["entries"] is not JsonArray added) continue;
                    foreach (var entry in added.OfType<JsonObject>())
                        if (!entries.OfType<JsonObject>().Any(x => x["equipment_template_id"]?.ToString() == entry["equipment_template_id"]?.ToString()))
                            entries.Add(entry.DeepClone());
                }
            if (equipment["first_clear_equipment_examples"] is JsonArray firstClears && root["levels"] is JsonArray levels)
                foreach (var first in firstClears.OfType<JsonObject>())
                {
                    var level = levels.OfType<JsonObject>().FirstOrDefault(x => x["level_id"]?.ToString() == first["level_id"]?.ToString());
                    if (level?["rewards"]?["first_clear"] is not JsonObject reward || first["first_clear"]?["equipment_templates"] is not JsonArray templates) continue;
                    reward["equipment_templates"] = templates.DeepClone();
                }
            return root.ToJsonString();
        }
    }
}
