using System;
using System.Collections.Generic;
using System.Linq;

namespace Xiuxian.Scripts.Services
{
    public enum ProductionBlockKind { NoTarget, TargetUnavailable, Finished, Paused, ActivityLocked, RegionClear, Mastery, EnhanceCap, Material, Reserve, Lingqi }
    public sealed record ProductionBlock(ProductionBlockKind Kind, string ItemId = "", int RequiredLevel = 0);
    public sealed record ProductionCheck(IReadOnlyList<ProductionBlock> Blocks, IReadOnlyList<PreparationMaterial> Materials,
        double LingqiRequired, double LingqiOwned, int MasteryLevel, int EnhanceLevel = 0, int EnhanceCap = 0)
    {
        public bool CanProduce => Blocks.Count == 0;
        public ProductionBlock? Primary => Blocks.FirstOrDefault();
    }

    // A read-only check for the next single batch. UI, input advancement and final spending share it.
    public static class ProductionBlockRules
    {
        public static ProductionCheck Check(IEnumerable<MaterialCost> inputs, double lingqiCost, double lingqi,
            IReadOnlyDictionary<string, int> inventory, IReadOnlyDictionary<string, int>? reserves, int mastery,
            int requiredMastery = 1, CraftingProductionPlan? plan = null, int requiredRealm = 0, string missingRegion = "")
        {
            if (plan?.Finished == true) return new(new[] { new ProductionBlock(ProductionBlockKind.Finished) }, Array.Empty<PreparationMaterial>(), 0, lingqi, mastery);
            var materials = inputs.Where(x => x.Count > 0).GroupBy(x => x.ItemId)
                .Select(x => new PreparationMaterial(x.Key, x.Sum(y => y.Count),
                    Math.Max(0, inventory.GetValueOrDefault(x.Key)), Math.Max(0, reserves?.GetValueOrDefault(x.Key) ?? 0))).ToArray();
            var blocks = new List<ProductionBlock>();
            if (plan?.Paused == true) blocks.Add(new(ProductionBlockKind.Paused));
            if (requiredRealm > 0) blocks.Add(new(ProductionBlockKind.ActivityLocked, RequiredLevel: requiredRealm));
            if (missingRegion != "") blocks.Add(new(ProductionBlockKind.RegionClear, missingRegion));
            if (mastery < requiredMastery) blocks.Add(new(ProductionBlockKind.Mastery, RequiredLevel: requiredMastery));
            foreach (var material in materials)
                if (material.Missing > 0) blocks.Add(new(material.Owned >= material.Required ? ProductionBlockKind.Reserve : ProductionBlockKind.Material, material.ItemId));
            if (lingqi < lingqiCost) blocks.Add(new(ProductionBlockKind.Lingqi));
            return new(blocks, materials, lingqiCost, lingqi, mastery);
        }
        public static ProductionCheck Unavailable(string target) => new(new[] { new ProductionBlock(
            string.IsNullOrEmpty(target) ? ProductionBlockKind.NoTarget : ProductionBlockKind.TargetUnavailable) }, Array.Empty<PreparationMaterial>(), 0, 0, 0);
        public static ProductionCheck Alchemy(string recipeId, double lingqi, IReadOnlyDictionary<string, int> inventory,
            IReadOnlyDictionary<string, int>? reserves, int mastery, CraftingProductionPlan? plan = null, int requiredRealm = 0)
        {
            if (!AlchemyRules.TryGetRecipe(recipeId, out var recipe)) return Unavailable(recipeId);
            return Check(new[] { new MaterialCost(recipe.MaterialItemId, recipe.MaterialCount) }, recipe.LingqiCost,
                lingqi, inventory, reserves, mastery, recipe.RequiredMasteryLevel, plan, requiredRealm);
        }
        public static ProductionCheck Smithing(EquipmentStatProfile equipment, double lingqi, IReadOnlyDictionary<string, int> inventory,
            IReadOnlyDictionary<string, int>? reserves, int mastery, CraftingProductionPlan? plan = null, int requiredRealm = 0)
        {
            int cap = SmithingRules.GetMaxEnhanceLevel(mastery, equipment.Rarity);
            var cost = SmithingRules.GetCost(equipment.EnhanceLevel, mastery);
            var check = Check(new[] { new MaterialCost("lingqi_shard", cost.Shards), new MaterialCost("broken_talisman", cost.Talismans) },
                cost.Lingqi, lingqi, inventory, reserves, mastery, plan: plan, requiredRealm: requiredRealm);
            if (check.Primary?.Kind == ProductionBlockKind.Finished || equipment.EnhanceLevel < cap) return check with { EnhanceLevel = equipment.EnhanceLevel, EnhanceCap = cap };
            int nextMastery = new[] { 2, 4 }.FirstOrDefault(x => x > mastery && SmithingRules.GetMaxEnhanceLevel(x, equipment.Rarity) > cap);
            // At cap there is no executable next batch; do not invent a material or qi deficit for it.
            return check with { Blocks = check.Blocks.Where(x => x.Kind is ProductionBlockKind.Paused or ProductionBlockKind.Finished or ProductionBlockKind.ActivityLocked)
                .Append(new ProductionBlock(ProductionBlockKind.EnhanceCap, RequiredLevel: nextMastery)).ToArray(),
                Materials = Array.Empty<PreparationMaterial>(), LingqiRequired = 0, EnhanceLevel = equipment.EnhanceLevel, EnhanceCap = cap };
        }
        public static string Describe(ProductionCheck check, ProductionBlock block) => block.Kind switch
        {
            ProductionBlockKind.NoTarget => "先选择制作项目。",
            ProductionBlockKind.TargetUnavailable => "制作项目当前不可用，请重新选择。",
            ProductionBlockKind.Finished => "指定轮数已完成；可修改设置开始新计划。",
            ProductionBlockKind.Paused => "计划已手动停止或切换项目暂停；点击继续后才会推进。",
            ProductionBlockKind.ActivityLocked => $"活动尚未开放，需{RealmBreakthroughRules.DisplayName(block.RequiredLevel)}。",
            ProductionBlockKind.RegionClear => "配方尚未开放：需首通第三地区；可前往副本查看条件。",
            ProductionBlockKind.Mastery => $"精通不足：当前Lv{check.MasteryLevel}，需Lv{block.RequiredLevel}。",
            ProductionBlockKind.EnhanceCap => $"强化已达当前上限：+{check.EnhanceLevel} / +{check.EnhanceCap}。"
                + (block.RequiredLevel > 0 ? $"精通Lv{block.RequiredLevel}可提高上限。" : "已达装备稀有度或最终强化上限，可比较其他装备。"),
            ProductionBlockKind.Lingqi => $"灵气不足：需{check.LingqiRequired:0.##} / 持有{check.LingqiOwned:0.##} / 缺{Math.Max(0, check.LingqiRequired - check.LingqiOwned):0.##}。",
            _ => DescribeMaterial(check.Materials.First(x => x.ItemId == block.ItemId), block.Kind == ProductionBlockKind.Reserve)
        };
        private static string DescribeMaterial(PreparationMaterial material, bool reserve) =>
            (reserve ? "保留量阻挡：" : "材料不足：") + $"{UiText.BackpackItemName(material.ItemId)}需{material.Required} / 持有{material.Owned} / 保留{material.Reserved} / 可用{material.Spendable} / 缺{material.Missing}。";
        public static string Summary(ProductionCheck check) => check.Primary is { } block ? Describe(check, block) : "材料与灵气满足，可推进当前批次。";
        public static string Details(ProductionCheck check)
        {
            var lines = check.Blocks.Select(x => Describe(check, x)).ToList();
            foreach (var material in check.Materials.Where(x => x.Missing == 0))
                lines.Add($"{UiText.BackpackItemName(material.ItemId)}：需{material.Required} / 持有{material.Owned} / 保留{material.Reserved} / 可用{material.Spendable}。");
            if (check.LingqiRequired > 0 && !check.Blocks.Any(x => x.Kind == ProductionBlockKind.Lingqi)) lines.Add($"灵气：需{check.LingqiRequired:0.##} / 持有{check.LingqiOwned:0.##}。");
            return string.Join("\n", lines);
        }
    }
}
