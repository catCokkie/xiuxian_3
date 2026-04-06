using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public static class GardenPersistenceRules
    {
        public readonly record struct GardenPlotSnapshot(string CropId, long PlantedAtUnix, bool IsReady);
        public readonly record struct GardenSnapshot(string SelectedRecipeId, int SelectedPlotIndex, GardenPlotSnapshot[] Plots);

        public static GardenSnapshot CreateDefault()
        {
            return new GardenSnapshot(string.Empty, 0, CreateEmptyPlots());
        }

        public static GardenPlotSnapshot[] CreateEmptyPlots()
        {
            GardenPlotSnapshot[] plots = new GardenPlotSnapshot[GardenRules.MaxPlotCount];
            for (int i = 0; i < plots.Length; i++)
            {
                plots[i] = default;
            }

            return plots;
        }

        public static Dictionary<string, object> ToPlainDictionary(GardenSnapshot snapshot)
        {
            List<object> plots = new(snapshot.Plots.Length);
            for (int i = 0; i < snapshot.Plots.Length; i++)
            {
                plots.Add(new Dictionary<string, object>
                {
                    ["crop_id"] = snapshot.Plots[i].CropId,
                    ["planted_at_unix"] = snapshot.Plots[i].PlantedAtUnix,
                    ["is_ready"] = snapshot.Plots[i].IsReady,
                });
            }

            return new Dictionary<string, object>
            {
                ["selected_recipe"] = snapshot.SelectedRecipeId,
                ["selected_plot_index"] = snapshot.SelectedPlotIndex,
                ["plots"] = plots,
            };
        }

        public static Dictionary<string, object> ToLegacyPlainDictionary(GardenSnapshot snapshot)
        {
            GardenPlotSnapshot firstActivePlot = default;
            for (int i = 0; i < snapshot.Plots.Length; i++)
            {
                if (!string.IsNullOrEmpty(snapshot.Plots[i].CropId))
                {
                    firstActivePlot = snapshot.Plots[i];
                    break;
                }
            }

            double progress = 0.0;
            double requiredProgress = 200.0;
            if (!string.IsNullOrEmpty(firstActivePlot.CropId) && GardenRules.TryGetCrop(firstActivePlot.CropId, out GardenRules.CropSpec crop))
            {
                requiredProgress = crop.GrowthSeconds;
                if (firstActivePlot.IsReady)
                {
                    progress = requiredProgress;
                }
            }

            return new Dictionary<string, object>
            {
                ["selected_recipe"] = snapshot.SelectedRecipeId,
                ["progress"] = progress,
                ["required_progress"] = requiredProgress,
            };
        }

        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(GardenSnapshot snapshot)
        {
            return SaveValueConversionRules.ToVariantDictionary(ToPlainDictionary(snapshot));
        }

        public static GardenSnapshot FromPlainDictionary(IDictionary<string, object> data)
        {
            if (!data.ContainsKey("plots"))
            {
                return ConvertLegacySnapshot(data);
            }

            GardenPlotSnapshot[] plots = CreateEmptyPlots();
            List<object> rawPlots = SaveValueConversionRules.GetList(data, "plots");
            int count = Math.Min(plots.Length, rawPlots.Count);
            for (int i = 0; i < count; i++)
            {
                if (rawPlots[i] is IDictionary<string, object> plot)
                {
                    plots[i] = new GardenPlotSnapshot(
                        SaveValueConversionRules.GetString(plot, "crop_id", string.Empty),
                        Math.Max(0L, SaveValueConversionRules.GetLong(plot, "planted_at_unix", 0L)),
                        SaveValueConversionRules.GetBool(plot, "is_ready", false));
                }
            }

            int selectedPlotIndex = Math.Clamp(
                SaveValueConversionRules.GetInt(data, "selected_plot_index", 0),
                0,
                GardenRules.MaxPlotCount - 1);

            string selectedRecipeId = SaveValueConversionRules.GetString(data, "selected_recipe", string.Empty);
            if (!string.IsNullOrEmpty(selectedRecipeId) && !GardenRules.TryGetCrop(selectedRecipeId, out _))
            {
                selectedRecipeId = string.Empty;
            }

            return new GardenSnapshot(selectedRecipeId, selectedPlotIndex, plots);
        }

        public static GardenSnapshot FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            return FromPlainDictionary(SaveValueConversionRules.ToPlainDictionary(data));
        }

        private static GardenSnapshot ConvertLegacySnapshot(IDictionary<string, object> data)
        {
            GardenSnapshot snapshot = CreateDefault();
            string selectedRecipeId = SaveValueConversionRules.GetString(data, "selected_recipe", string.Empty);
            if (string.IsNullOrEmpty(selectedRecipeId) || !GardenRules.TryGetCrop(selectedRecipeId, out GardenRules.CropSpec crop))
            {
                return snapshot;
            }

            double progress = Math.Max(0.0, SaveValueConversionRules.GetDouble(data, "progress", 0.0));
            double requiredProgress = Math.Max(1.0, SaveValueConversionRules.GetDouble(data, "required_progress", crop.GrowthSeconds));
            double progressFraction = Math.Clamp(progress / requiredProgress, 0.0, 1.0);
            int migratedGrowthSeconds = crop.GrowthSeconds;
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool isReady = progressFraction >= 1.0;
            long plantedAtUnix = isReady
                ? nowUnix - migratedGrowthSeconds
                : nowUnix - (long)Math.Round(migratedGrowthSeconds * progressFraction);

            GardenPlotSnapshot[] plots = CreateEmptyPlots();
            plots[0] = new GardenPlotSnapshot(selectedRecipeId, Math.Max(0L, plantedAtUnix), isReady);
            return new GardenSnapshot(selectedRecipeId, 0, plots);
        }
    }
}
