using Godot;
using System;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    public static class CraftingProgressionService
    {
        public static bool AdvanceAlchemy(AlchemyState? alchemyState, int inputEvents, out float progressPercent)
        {
            progressPercent = 0.0f;
            if (alchemyState == null || inputEvents <= 0 || !alchemyState.HasSelectedRecipe)
            {
                return false;
            }

            bool completedBatch = alchemyState.AdvanceProgress(inputEvents);
            progressPercent = alchemyState.RequiredProgress > 0.0f
                ? alchemyState.CurrentProgress / alchemyState.RequiredProgress * 100.0f
                : 0.0f;
            return completedBatch;
        }

        public static bool TryCompleteAlchemyBatch(
            AlchemyState? alchemyState,
            BackpackState? backpackState,
            ResourceWalletState? resourceWalletState,
            int alchemyMasteryLevel,
            PotionInventoryState? potionInventoryState,
            int outputMultiplier,
            out string rewardText)
        {
            rewardText = string.Empty;
            if (alchemyState == null || backpackState == null || resourceWalletState == null || potionInventoryState == null || string.IsNullOrEmpty(alchemyState.SelectedRecipeId))
            {
                return false;
            }

            if (!AlchemyRules.CanStartRecipe(alchemyState.SelectedRecipeId, resourceWalletState.Lingqi, backpackState.GetItemEntries(), alchemyMasteryLevel))
            {
                return false;
            }

            AlchemyRules.AlchemyCompletionResult result = AlchemyRules.CompleteRecipe(alchemyState.SelectedRecipeId, alchemyMasteryLevel);
            if (string.IsNullOrEmpty(result.PotionItemId) || !backpackState.RemoveItem(result.MaterialItemId, result.MaterialCount))
            {
                return false;
            }

            if (!resourceWalletState.SpendLingqi(result.LingqiCost))
            {
                backpackState.AddItem(result.MaterialItemId, result.MaterialCount);
                return false;
            }

            ServiceLocator.Instance?.PlayerStatsState?.RecordLingqiSpend(result.LingqiCost);

            int finalPotionCount = result.PotionCount * System.Math.Max(1, outputMultiplier);
            potionInventoryState.AddPotion(result.PotionItemId, finalPotionCount);
            rewardText = $"炼丹完成，获得 {UiText.BackpackItemName(result.PotionItemId)} x{finalPotionCount}";
            return true;
        }

        public static bool AdvanceSmithing(SmithingState? smithingState, int inputEvents, out float progressPercent)
        {
            progressPercent = 0.0f;
            if (smithingState == null || inputEvents <= 0 || !smithingState.HasTarget)
            {
                return false;
            }

            bool completed = smithingState.AdvanceProgress(inputEvents);
            progressPercent = smithingState.RequiredProgress > 0.0f
                ? smithingState.CurrentProgress / smithingState.RequiredProgress * 100.0f
                : 0.0f;
            return completed;
        }

        public static bool TryCompleteSmithingBatch(
            SmithingState? smithingState,
            EquippedItemsState? equippedItemsState,
            BackpackState? backpackState,
            ResourceWalletState? resourceWalletState,
            int smithingMasteryLevel,
            EquipmentStatProfile targetProfile,
            out EquipmentStatProfile enhancedProfile,
            out string rewardText)
        {
            enhancedProfile = default;
            rewardText = string.Empty;
            if (smithingState == null || equippedItemsState == null || backpackState == null || resourceWalletState == null)
            {
                return false;
            }

            if (!SmithingRules.CanEnhance(targetProfile, backpackState, resourceWalletState, smithingMasteryLevel))
            {
                return false;
            }

            SmithingCost cost = SmithingRules.GetCost(targetProfile.EnhanceLevel, smithingMasteryLevel);
            if (!backpackState.RemoveItem("lingqi_shard", cost.Shards))
            {
                return false;
            }

            if (cost.Talismans > 0 && !backpackState.RemoveItem("broken_talisman", cost.Talismans))
            {
                backpackState.AddItem("lingqi_shard", cost.Shards);
                return false;
            }

            if (!resourceWalletState.SpendLingqi(cost.Lingqi))
            {
                backpackState.AddItem("lingqi_shard", cost.Shards);
                if (cost.Talismans > 0)
                {
                    backpackState.AddItem("broken_talisman", cost.Talismans);
                }

                return false;
            }

            ServiceLocator.Instance?.PlayerStatsState?.RecordLingqiSpend(cost.Lingqi);

            if (!equippedItemsState.TryEnhanceEquippedProfile(targetProfile.EquipmentId))
            {
                return false;
            }

            if (!equippedItemsState.TryGetEquippedProfileById(targetProfile.EquipmentId, out enhancedProfile))
            {
                return false;
            }

            smithingState.SelectTarget(enhancedProfile.EquipmentId, enhancedProfile.EnhanceLevel);
            rewardText = $"强化完成，{enhancedProfile.DisplayName} +{enhancedProfile.EnhanceLevel}";
            return true;
        }

        public readonly record struct GenericProgressResult(float ProgressPercent, bool CompletedBatch);

        public static GenericProgressResult AdvanceGenericRecipe(
            string recipeId,
            float currentProgress,
            int inputEvents)
        {
            IRecipeDefinition? recipe = ActivityRegistry.GetRecipe(recipeId);
            if (recipe == null || inputEvents <= 0)
            {
                return new GenericProgressResult(0f, false);
            }

            float next = Math.Max(0f, currentProgress) + Math.Max(0, inputEvents);
            float threshold = Math.Max(1, recipe.RequiredInputEvents);
            bool completed = next >= threshold;
            float percent = completed ? 100f : next / threshold * 100f;
            return new GenericProgressResult(percent, completed);
        }

        public static bool TryCompleteGenericBatch(
            string recipeId,
            BackpackState? backpackState,
            ResourceWalletState? resourceWalletState,
            int masteryLevel,
            int outputMultiplier,
            out string rewardText)
        {
            rewardText = string.Empty;
            IRecipeDefinition? recipe = ActivityRegistry.GetRecipe(recipeId);
            if (recipe == null || backpackState == null || resourceWalletState == null)
            {
                return false;
            }

            if (masteryLevel < recipe.RequiredMasteryLevel)
            {
                return false;
            }

            MaterialCost[] effectiveInputs = GetEffectiveInputs(recipe, masteryLevel);

            if (resourceWalletState.Lingqi < recipe.LingqiCost)
            {
                return false;
            }

            for (int i = 0; i < effectiveInputs.Length; i++)
            {
                if (backpackState.GetItemCount(effectiveInputs[i].ItemId) < effectiveInputs[i].Count)
                {
                    return false;
                }
            }

            if (!resourceWalletState.SpendLingqi(recipe.LingqiCost))
            {
                return false;
            }

            ServiceLocator.Instance?.PlayerStatsState?.RecordLingqiSpend(recipe.LingqiCost);

            for (int i = 0; i < effectiveInputs.Length; i++)
            {
                MaterialCost input = effectiveInputs[i];
                if (!backpackState.RemoveItem(input.ItemId, input.Count))
                {
                    for (int j = 0; j < i; j++)
                    {
                        backpackState.AddItem(effectiveInputs[j].ItemId, effectiveInputs[j].Count);
                    }

                    resourceWalletState.AddLingqi(recipe.LingqiCost);
                    return false;
                }
            }

            foreach (MaterialOutput output in recipe.Outputs)
            {
                backpackState.AddItem(output.ItemId, output.Count * System.Math.Max(1, outputMultiplier));
            }

            rewardText = recipe.Outputs.Count > 0
                ? $"{recipe.DisplayName}完成，获得 {UiText.BackpackItemName(recipe.Outputs[0].ItemId)} x{recipe.Outputs[0].Count * System.Math.Max(1, outputMultiplier)}"
                : $"{recipe.DisplayName}完成";
            return true;
        }

        public static bool CanAffordGenericRecipe(
            string recipeId,
            BackpackState? backpackState,
            ResourceWalletState? resourceWalletState,
            int masteryLevel)
        {
            IRecipeDefinition? recipe = ActivityRegistry.GetRecipe(recipeId);
            if (recipe == null || backpackState == null || resourceWalletState == null || masteryLevel < recipe.RequiredMasteryLevel)
            {
                return false;
            }

            if (resourceWalletState.Lingqi < recipe.LingqiCost)
            {
                return false;
            }

            MaterialCost[] effectiveInputs = GetEffectiveInputs(recipe, masteryLevel);
            for (int i = 0; i < effectiveInputs.Length; i++)
            {
                if (backpackState.GetItemCount(effectiveInputs[i].ItemId) < effectiveInputs[i].Count)
                {
                    return false;
                }
            }

            return true;
        }

        private static MaterialCost[] GetEffectiveInputs(IRecipeDefinition recipe, int masteryLevel)
        {
            double discount = recipe.SystemId switch
            {
                PlayerActionState.ModeTalisman => TalismanRules.GetMaterialDiscount(masteryLevel),
                PlayerActionState.ModeBodyCultivation => BodyCultivationRules.GetMaterialDiscount(masteryLevel),
                _ => 0.0,
            };

            MaterialCost[] effectiveInputs = new MaterialCost[recipe.Inputs.Count];
            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                MaterialCost input = recipe.Inputs[i];
                int count = discount > 0.0
                    ? Math.Max(0, (int)Math.Floor(input.Count * (1.0 - discount)))
                    : input.Count;
                effectiveInputs[i] = new MaterialCost(input.ItemId, count);
            }

            return effectiveInputs;
        }
    }
}
