using Godot;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public partial class GardenState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void GardenChangedEventHandler(string selectedRecipeId, float currentProgress, float requiredProgress);

        public readonly record struct PlotStatus(
            int PlotIndex,
            bool IsUnlocked,
            string CropId,
            bool IsEmpty,
            bool IsReady,
            double ElapsedSeconds,
            double RequiredSeconds,
            double RemainingSeconds);

        private const double SyncIntervalSeconds = 1.0;

        private readonly GardenPersistenceRules.GardenPlotSnapshot[] _plots = GardenPersistenceRules.CreateEmptyPlots();
        private double _syncAccumulator;

        public string SelectedRecipeId { get; private set; } = string.Empty;
        public int SelectedPlotIndex { get; private set; }
        public bool HasSelectedCropChoice => !string.IsNullOrEmpty(SelectedRecipeId);
        public bool HasSelectedPlotCrop => !GetSelectedPlotStatus().IsEmpty;
        public float CurrentProgress => (float)GetSelectedPlotStatus().ElapsedSeconds;
        public float RequiredProgress => (float)GetSelectedPlotStatus().RequiredSeconds;

        public override void _Ready()
        {
            NormalizeSelectedPlotIndex();
            SyncRealTime(emitSignal: false);
        }

        public override void _Process(double delta)
        {
            _syncAccumulator += delta;
            if (_syncAccumulator < SyncIntervalSeconds)
            {
                return;
            }

            _syncAccumulator = 0.0;
            SyncRealTime();
        }

        public void SelectCrop(string recipeId)
        {
            if (!GardenRules.TryGetCrop(recipeId, out GardenRules.CropSpec crop)
                || GetGardenMasteryLevel() < crop.RequiredMasteryLevel)
            {
                SelectedRecipeId = string.Empty;
            }
            else
            {
                SelectedRecipeId = crop.RecipeId;
            }

            EmitGardenChanged();
        }

        public void SelectPlot(int plotIndex)
        {
            SelectedPlotIndex = NormalizePlotIndex(plotIndex);
            EmitGardenChanged();
        }

        public int GetUnlockedPlotCount()
        {
            int purchasedPlotCount = ServiceLocator.Instance?.ShopState?.GardenPlotUnlockCount ?? 0;
            return GardenRules.GetUnlockedPlotCount(purchasedPlotCount);
        }

        public PlotStatus GetSelectedPlotStatus()
        {
            return GetPlotStatus(SelectedPlotIndex);
        }

        public PlotStatus GetPlotStatus(int plotIndex)
        {
            int normalizedIndex = Math.Clamp(plotIndex, 0, GardenRules.MaxPlotCount - 1);
            bool isUnlocked = normalizedIndex < GetUnlockedPlotCount();
            GardenPersistenceRules.GardenPlotSnapshot plot = _plots[normalizedIndex];
            if (!isUnlocked)
            {
                return new PlotStatus(normalizedIndex, false, string.Empty, true, false, 0.0, 0.0, 0.0);
            }

            if (string.IsNullOrEmpty(plot.CropId) || !GardenRules.TryGetCrop(plot.CropId, out _))
            {
                return new PlotStatus(normalizedIndex, true, string.Empty, true, false, 0.0, 0.0, 0.0);
            }

            int masteryLevel = GetGardenMasteryLevel();
            double requiredSeconds = GardenRules.GetEffectiveGrowthSeconds(plot.CropId, masteryLevel);
            double elapsedSeconds = plot.IsReady
                ? requiredSeconds
                : Math.Max(0.0, GetNowUnix() - plot.PlantedAtUnix);
            bool isReady = plot.IsReady || elapsedSeconds >= requiredSeconds;
            double clampedElapsed = Math.Min(requiredSeconds, elapsedSeconds);
            double remainingSeconds = isReady ? 0.0 : Math.Max(0.0, requiredSeconds - clampedElapsed);
            return new PlotStatus(normalizedIndex, true, plot.CropId, false, isReady, clampedElapsed, requiredSeconds, remainingSeconds);
        }

        public bool CanPlantSelectedCrop(out string reason)
        {
            SyncRealTime();
            reason = string.Empty;
            PlotStatus plot = GetSelectedPlotStatus();
            if (!plot.IsUnlocked)
            {
                reason = "该田位尚未解锁";
                return false;
            }

            if (!plot.IsEmpty)
            {
                reason = plot.IsReady ? "请先收获当前作物" : "该田位仍在生长中";
                return false;
            }

            if (!HasSelectedCropChoice || !GardenRules.TryGetCrop(SelectedRecipeId, out GardenRules.CropSpec crop))
            {
                reason = "请先选择可种植作物";
                return false;
            }

            int masteryLevel = GetGardenMasteryLevel();
            if (masteryLevel < crop.RequiredMasteryLevel)
            {
                reason = $"需灵田精通 Lv{crop.RequiredMasteryLevel}";
                return false;
            }

            BackpackState? backpackState = ServiceLocator.Instance?.BackpackState;
            if (backpackState == null)
            {
                reason = "背包未加载";
                return false;
            }

            if (backpackState.GetItemCount(crop.SeedItemId) <= 0)
            {
                reason = $"{UiText.BackpackItemName(crop.SeedItemId)}不足";
                return false;
            }

            return true;
        }

        public bool TryPlantSelectedCrop(out string summary)
        {
            summary = string.Empty;
            if (!CanPlantSelectedCrop(out _)
                || !GardenRules.TryGetCrop(SelectedRecipeId, out GardenRules.CropSpec crop))
            {
                return false;
            }

            BackpackState? backpackState = ServiceLocator.Instance?.BackpackState;
            if (backpackState == null || !backpackState.RemoveItem(crop.SeedItemId, 1))
            {
                return false;
            }

            _plots[SelectedPlotIndex] = new GardenPersistenceRules.GardenPlotSnapshot(
                crop.RecipeId,
                GetNowUnix(),
                false);
            ServiceLocator.Instance?.PlayerStatsState?.RecordGardenPlant();
            summary = $"{FormatPlotName(SelectedPlotIndex)}播下 {UiText.BackpackItemName(crop.SeedItemId)}";
            EmitGardenChanged();
            return true;
        }

        public bool CanHarvestSelectedPlot(out string reason)
        {
            SyncRealTime();
            reason = string.Empty;
            PlotStatus plot = GetSelectedPlotStatus();
            if (!plot.IsUnlocked)
            {
                reason = "该田位尚未解锁";
                return false;
            }

            if (plot.IsEmpty)
            {
                reason = "当前田位为空";
                return false;
            }

            if (!plot.IsReady)
            {
                reason = "作物尚未成熟";
                return false;
            }

            if (ServiceLocator.Instance?.BackpackState == null)
            {
                reason = "背包未加载";
                return false;
            }

            return true;
        }

        public bool TryHarvestSelectedPlot(out string summary)
        {
            summary = string.Empty;
            if (!CanHarvestSelectedPlot(out _))
            {
                return false;
            }

            PlotStatus plot = GetSelectedPlotStatus();
            BackpackState? backpackState = ServiceLocator.Instance?.BackpackState;
            if (backpackState == null)
            {
                return false;
            }

            GardenRules.HarvestResult harvest = GardenRules.ResolveHarvest(
                plot.CropId,
                GetGardenMasteryLevel(),
                Random.Shared.Next(),
                Random.Shared.Next());
            GrantHarvest(backpackState, harvest, applyShopMultiplier: true);
            _plots[SelectedPlotIndex] = default;
            ServiceLocator.Instance?.PlayerStatsState?.RecordGardenHarvest(autoHarvest: false);
            summary = BuildHarvestSummary(plot.PlotIndex, harvest, applyShopMultiplier: true);
            EmitGardenChanged();
            return true;
        }

        public float ApplyBonusProgressFraction(float remainingFraction)
        {
            SyncRealTime();
            PlotStatus plot = GetSelectedPlotStatus();
            if (plot.IsEmpty || plot.IsReady || plot.RequiredSeconds <= 0.0 || remainingFraction <= 0.0f)
            {
                return 0.0f;
            }

            double remainingSeconds = plot.RemainingSeconds;
            double bonusSeconds = Math.Clamp(remainingSeconds * remainingFraction, 0.0, remainingSeconds);
            GardenPersistenceRules.GardenPlotSnapshot snapshot = _plots[SelectedPlotIndex];
            snapshot = snapshot with
            {
                PlantedAtUnix = Math.Max(0L, snapshot.PlantedAtUnix - (long)Math.Ceiling(bonusSeconds)),
            };

            int masteryLevel = GetGardenMasteryLevel();
            int requiredSeconds = GardenRules.GetEffectiveGrowthSeconds(snapshot.CropId, masteryLevel);
            if (GetNowUnix() - snapshot.PlantedAtUnix >= requiredSeconds)
            {
                snapshot = snapshot with { IsReady = true };
            }

            _plots[SelectedPlotIndex] = snapshot;
            EmitGardenChanged();
            return (float)bonusSeconds;
        }

        public int GetActivePlotCount()
        {
            int count = 0;
            int unlockedPlots = GetUnlockedPlotCount();
            for (int i = 0; i < unlockedPlots; i++)
            {
                if (!string.IsNullOrEmpty(_plots[i].CropId))
                {
                    count++;
                }
            }

            return count;
        }

        public int GetReadyPlotCount()
        {
            int count = 0;
            int unlockedPlots = GetUnlockedPlotCount();
            for (int i = 0; i < unlockedPlots; i++)
            {
                if (GetPlotStatus(i).IsReady)
                {
                    count++;
                }
            }

            return count;
        }

        public string BuildOverviewSummary()
        {
            int unlockedPlots = GetUnlockedPlotCount();
            int activePlots = 0;
            int readyPlots = 0;
            for (int i = 0; i < unlockedPlots; i++)
            {
                PlotStatus plot = GetPlotStatus(i);
                if (!plot.IsEmpty)
                {
                    activePlots++;
                }
                if (plot.IsReady)
                {
                    readyPlots++;
                }
            }

            return $"田位 {activePlots}/{unlockedPlots} | 待收获 {readyPlots} | 空闲 {Math.Max(0, unlockedPlots - activePlots)}";
        }

        public string BuildSelectedPlotSummary()
        {
            PlotStatus plot = GetSelectedPlotStatus();
            if (!plot.IsUnlocked)
            {
                return $"{FormatPlotName(plot.PlotIndex)}尚未解锁";
            }

            if (plot.IsEmpty)
            {
                return $"{FormatPlotName(plot.PlotIndex)}空闲";
            }

            string cropName = GardenRules.TryGetCrop(plot.CropId, out GardenRules.CropSpec crop)
                ? crop.DisplayName.Replace("种植", string.Empty)
                : UiText.BackpackItemName(plot.CropId);
            return plot.IsReady
                ? $"{FormatPlotName(plot.PlotIndex)}：{cropName}已成熟，可立即收获"
                : $"{FormatPlotName(plot.PlotIndex)}：{cropName}剩余 {FormatDuration(plot.RemainingSeconds)}";
        }

        public bool ApplyOfflineRealTime()
        {
            return SyncRealTimeCore(emitSignal: true);
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            GardenPersistenceRules.GardenPlotSnapshot[] plots = new GardenPersistenceRules.GardenPlotSnapshot[_plots.Length];
            for (int i = 0; i < _plots.Length; i++)
            {
                plots[i] = _plots[i];
            }

            return GardenPersistenceRules.ToDictionary(new GardenPersistenceRules.GardenSnapshot(
                SelectedRecipeId,
                SelectedPlotIndex,
                plots));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            GardenPersistenceRules.GardenSnapshot snapshot = GardenPersistenceRules.FromDictionary(data);
            SelectedRecipeId = snapshot.SelectedRecipeId;
            SelectedPlotIndex = Math.Clamp(snapshot.SelectedPlotIndex, 0, GardenRules.MaxPlotCount - 1);
            for (int i = 0; i < _plots.Length; i++)
            {
                _plots[i] = i < snapshot.Plots.Length ? snapshot.Plots[i] : default;
            }

            NormalizeSelectedPlotIndex();
            EmitGardenChanged();
        }

        private void SyncRealTime(bool emitSignal = true)
        {
            SyncRealTimeCore(emitSignal);
        }

        private bool SyncRealTimeCore(bool emitSignal)
        {
            NormalizeSelectedPlotIndex();
            bool changed = false;
            int unlockedPlots = GetUnlockedPlotCount();
            int masteryLevel = GetGardenMasteryLevel();
            BackpackState? backpackState = ServiceLocator.Instance?.BackpackState;
            bool autoHarvest = ServiceLocator.Instance?.ShopState?.HasAutoHarvest ?? false;
            long nowUnix = GetNowUnix();

            for (int i = 0; i < unlockedPlots; i++)
            {
                GardenPersistenceRules.GardenPlotSnapshot snapshot = _plots[i];
                if (string.IsNullOrEmpty(snapshot.CropId) || !GardenRules.TryGetCrop(snapshot.CropId, out _))
                {
                    continue;
                }

                int requiredSeconds = GardenRules.GetEffectiveGrowthSeconds(snapshot.CropId, masteryLevel);
                bool matured = snapshot.IsReady || nowUnix - snapshot.PlantedAtUnix >= requiredSeconds;
                if (!matured)
                {
                    continue;
                }

                if (autoHarvest && backpackState != null)
                {
                    changed |= ResolveAutoHarvest(i, nowUnix, masteryLevel, backpackState);
                    continue;
                }

                if (!snapshot.IsReady)
                {
                    _plots[i] = snapshot with { IsReady = true };
                    changed = true;
                }
            }

            if (changed && emitSignal)
            {
                EmitGardenChanged();
            }

            return changed;
        }

        private bool ResolveAutoHarvest(int plotIndex, long nowUnix, int masteryLevel, BackpackState backpackState)
        {
            GardenPersistenceRules.GardenPlotSnapshot snapshot = _plots[plotIndex];
            if (string.IsNullOrEmpty(snapshot.CropId) || !GardenRules.TryGetCrop(snapshot.CropId, out GardenRules.CropSpec crop))
            {
                return false;
            }

            bool changed = false;
            long plantedAtUnix = snapshot.PlantedAtUnix;
            string cropId = snapshot.CropId;

            for (int cycle = 0; cycle < 256; cycle++)
            {
                int requiredSeconds = GardenRules.GetEffectiveGrowthSeconds(cropId, masteryLevel);
                long readyAtUnix = plantedAtUnix + requiredSeconds;
                if (nowUnix < readyAtUnix)
                {
                    _plots[plotIndex] = new GardenPersistenceRules.GardenPlotSnapshot(cropId, plantedAtUnix, false);
                    return true;
                }

                GardenRules.HarvestResult harvest = GardenRules.ResolveHarvest(
                    cropId,
                    masteryLevel,
                    Random.Shared.Next(),
                    Random.Shared.Next());
                GrantHarvest(backpackState, harvest, applyShopMultiplier: false);
                ServiceLocator.Instance?.PlayerStatsState?.RecordGardenHarvest(autoHarvest: true);
                changed = true;

                if (!backpackState.RemoveItem(crop.SeedItemId, 1))
                {
                    _plots[plotIndex] = default;
                    return true;
                }

                plantedAtUnix = readyAtUnix;
            }

            _plots[plotIndex] = new GardenPersistenceRules.GardenPlotSnapshot(cropId, plantedAtUnix, false);
            return changed;
        }

        private void GrantHarvest(BackpackState backpackState, GardenRules.HarvestResult harvest, bool applyShopMultiplier)
        {
            int outputMultiplier = applyShopMultiplier
                ? Math.Max(1, (int)Math.Round(ServiceLocator.Instance?.ShopState?.DoubleYieldMultiplier ?? 1.0))
                : 1;
            if (!string.IsNullOrEmpty(harvest.ItemId) && harvest.ItemCount > 0)
            {
                backpackState.AddItem(harvest.ItemId, harvest.ItemCount * outputMultiplier);
            }

            if (!string.IsNullOrEmpty(harvest.BonusSeedItemId) && harvest.BonusSeedCount > 0)
            {
                backpackState.AddItem(harvest.BonusSeedItemId, harvest.BonusSeedCount);
            }
        }

        private string BuildHarvestSummary(int plotIndex, GardenRules.HarvestResult harvest, bool applyShopMultiplier)
        {
            int outputMultiplier = applyShopMultiplier
                ? Math.Max(1, (int)Math.Round(ServiceLocator.Instance?.ShopState?.DoubleYieldMultiplier ?? 1.0))
                : 1;
            string summary = $"{FormatPlotName(plotIndex)}收获 {UiText.BackpackItemName(harvest.ItemId)} x{harvest.ItemCount * outputMultiplier}";
            if (!string.IsNullOrEmpty(harvest.BonusSeedItemId) && harvest.BonusSeedCount > 0)
            {
                summary += $" | 额外获得 {UiText.BackpackItemName(harvest.BonusSeedItemId)} x{harvest.BonusSeedCount}";
            }

            return summary;
        }

        private void EmitGardenChanged()
        {
            PlotStatus selectedPlot = GetSelectedPlotStatus();
            string signalRecipeId = selectedPlot.IsEmpty ? SelectedRecipeId : selectedPlot.CropId;
            EmitSignal(
                SignalName.GardenChanged,
                signalRecipeId,
                (float)selectedPlot.ElapsedSeconds,
                (float)selectedPlot.RequiredSeconds);
        }

        private void NormalizeSelectedPlotIndex()
        {
            SelectedPlotIndex = NormalizePlotIndex(SelectedPlotIndex);
        }

        private int NormalizePlotIndex(int plotIndex)
        {
            int unlockedPlots = Math.Max(1, GetUnlockedPlotCount());
            return Math.Clamp(plotIndex, 0, unlockedPlots - 1);
        }

        private int GetGardenMasteryLevel()
        {
            return ServiceLocator.Instance?.SubsystemMasteryState?.GetLevel(PlayerActionState.ModeGarden) ?? 1;
        }

        private static long GetNowUnix()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static string FormatDuration(double totalSeconds)
        {
            int seconds = Math.Max(0, (int)Math.Ceiling(totalSeconds));
            int hours = seconds / 3600;
            int minutes = (seconds % 3600) / 60;
            int remainSeconds = seconds % 60;
            return hours > 0
                ? $"{hours:00}:{minutes:00}:{remainSeconds:00}"
                : $"{minutes:00}:{remainSeconds:00}";
        }

        private static string FormatPlotName(int plotIndex)
        {
            return $"{plotIndex + 1}号田";
        }
    }
}
