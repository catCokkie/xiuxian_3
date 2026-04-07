using Godot;
using System;
using System.IO;

namespace Xiuxian.Scripts.Services
{
    public static class SaveSlotService
    {
        public const int ManualSlotCount = 3;
        public const string WorkingSavePath = "user://save_state.cfg";

        public readonly record struct SaveSlotSummary(
            int SlotId,
            bool Exists,
            int SaveVersion,
            long LastSavedUnix,
            int RealmLevel,
            double ActiveSeconds,
            string ActionId,
            string ActionTargetId);

        public static SaveSlotSummary[] ReadAllSummaries()
        {
            SaveSlotSummary[] result = new SaveSlotSummary[ManualSlotCount];
            for (int i = 0; i < ManualSlotCount; i++)
            {
                result[i] = ReadSummary(i + 1);
            }

            return result;
        }

        public static SaveSlotSummary ReadSummary(int slotId)
        {
            if (!IsValidSlotId(slotId))
            {
                return new SaveSlotSummary(slotId, false, 0, 0, 0, 0.0, string.Empty, string.Empty);
            }

            ConfigFile config = new();
            if (config.Load(GetSlotPath(slotId)) != Error.Ok)
            {
                return new SaveSlotSummary(slotId, false, 0, 0, 0, 0.0, string.Empty, string.Empty);
            }

            return BuildSummary(slotId, config);
        }

        public static SaveSlotSummary BuildSummary(int slotId, ConfigFile config)
        {
            int version = config.GetValue("meta", "version", 0).AsInt32();
            long lastSavedUnix = config.GetValue("meta", "last_saved_unix", 0L).AsInt64();
            Variant progress = config.GetValue("progress", "player", new Godot.Collections.Dictionary<string, Variant>());
            Variant input = config.GetValue("input", "stats", new Godot.Collections.Dictionary<string, Variant>());
            Variant action = config.GetValue("action", "mode", new Godot.Collections.Dictionary<string, Variant>());

            return BuildSummaryFromSections(
                slotId,
                version,
                lastSavedUnix,
                progress.VariantType == Variant.Type.Dictionary ? (Godot.Collections.Dictionary<string, Variant>)progress : null,
                input.VariantType == Variant.Type.Dictionary ? (Godot.Collections.Dictionary<string, Variant>)input : null,
                action.VariantType == Variant.Type.Dictionary ? (Godot.Collections.Dictionary<string, Variant>)action : null);
        }

        public static SaveSlotSummary BuildSummaryFromSections(
            int slotId,
            int version,
            long lastSavedUnix,
            Godot.Collections.Dictionary<string, Variant>? progress,
            Godot.Collections.Dictionary<string, Variant>? input,
            Godot.Collections.Dictionary<string, Variant>? action)
        {
            int realmLevel = progress != null && progress.ContainsKey("realm_level") ? progress["realm_level"].AsInt32() : 0;
            double activeSeconds = input != null && input.ContainsKey("total_active_seconds") ? input["total_active_seconds"].AsDouble() : 0.0;
            string actionId = action != null && action.ContainsKey("action_id") ? action["action_id"].AsString() : string.Empty;
            string actionTargetId = action != null && action.ContainsKey("action_target_id") ? action["action_target_id"].AsString() : string.Empty;

            return new SaveSlotSummary(slotId, true, version, lastSavedUnix, realmLevel, activeSeconds, actionId, actionTargetId);
        }

        public static bool SaveWorkingStateToSlot(int slotId, out string error)
        {
            error = string.Empty;
            if (!IsValidSlotId(slotId))
            {
                error = "无效槽位。";
                return false;
            }

            string source = ProjectSettings.GlobalizePath(WorkingSavePath);
            string destination = ProjectSettings.GlobalizePath(GetSlotPath(slotId));
            if (!File.Exists(source))
            {
                error = "当前自动存档不存在。";
                return false;
            }

            try
            {
                File.Copy(source, destination, true);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryLoadSlotConfig(int slotId, out ConfigFile config, out string error)
        {
            config = new ConfigFile();
            error = string.Empty;
            if (!IsValidSlotId(slotId))
            {
                error = "无效槽位。";
                return false;
            }

            Error loadErr = config.Load(GetSlotPath(slotId));
            if (loadErr != Error.Ok)
            {
                error = $"读取槽位失败：{loadErr}";
                return false;
            }

            return true;
        }

        public static bool DeleteSlot(int slotId, out string error)
        {
            error = string.Empty;
            if (!IsValidSlotId(slotId))
            {
                error = "无效槽位。";
                return false;
            }

            string path = ProjectSettings.GlobalizePath(GetSlotPath(slotId));
            if (!File.Exists(path))
            {
                return true;
            }

            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string GetSlotPath(int slotId)
        {
            return $"user://save_slot_{slotId}.cfg";
        }

        private static bool IsValidSlotId(int slotId)
        {
            return slotId >= 1 && slotId <= ManualSlotCount;
        }
    }
}
