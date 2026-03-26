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
            PlayerProgressState? playerProgressState,
            PotionInventoryState? potionInventoryState,
            out string rewardText)
        {
            rewardText = string.Empty;
            if (alchemyState == null || backpackState == null || resourceWalletState == null || potionInventoryState == null || string.IsNullOrEmpty(alchemyState.SelectedRecipeId))
            {
                return false;
            }

            if (!AlchemyRules.CanStartRecipe(alchemyState.SelectedRecipeId, resourceWalletState.Lingqi, backpackState.GetItemEntries(), playerProgressState?.HasUnlockedAdvancedAlchemyStudy ?? false))
            {
                return false;
            }

            AlchemyRules.AlchemyCompletionResult result = AlchemyRules.CompleteRecipe(alchemyState.SelectedRecipeId);
            if (string.IsNullOrEmpty(result.PotionItemId) || !backpackState.RemoveItem(result.MaterialItemId, result.MaterialCount))
            {
                return false;
            }

            if (!resourceWalletState.SpendLingqi(result.LingqiCost))
            {
                backpackState.AddItem(result.MaterialItemId, result.MaterialCount);
                return false;
            }

            potionInventoryState.AddPotion(result.PotionItemId, result.PotionCount);
            rewardText = $"炼丹完成，获得 {UiText.BackpackItemName(result.PotionItemId)} x{result.PotionCount}";
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

            if (!SmithingRules.CanEnhance(targetProfile, backpackState, resourceWalletState))
            {
                return false;
            }

            SmithingCost cost = SmithingRules.GetCost(targetProfile.EnhanceLevel);
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

        // --- Generic activity framework methods ---

        public readonly record struct GenericProgressResult(float ProgressPercent, bool CompletedBatch);

        /// <summary>
        /// Advance progress for any recipe registered in <see cref="ActivityRegistry"/>.
        /// Returns progress percentage and whether a batch completed.
        /// </summary>
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

        /// <summary>
        /// Consume inputs and produce outputs for a completed generic recipe batch.
        /// Returns false if the player cannot afford the cost.
        /// </summary>
        public static bool TryCompleteGenericBatch(
            string recipeId,
            BackpackState? backpackState,
            ResourceWalletState? resourceWalletState,
            out string rewardText)
        {
            rewardText = string.Empty;
            IRecipeDefinition? recipe = ActivityRegistry.GetRecipe(recipeId);
            if (recipe == null || backpackState == null || resourceWalletState == null)
            {
                return false;
            }

            // Check lingqi
            if (resourceWalletState.Lingqi < recipe.LingqiCost)
            {
                return false;
            }

            // Check material inputs
            foreach (MaterialCost input in recipe.Inputs)
            {
                if (backpackState.GetItemCount(input.ItemId) < input.Count)
                {
                    return false;
                }
            }

            // Deduct lingqi
            if (!resourceWalletState.SpendLingqi(recipe.LingqiCost))
            {
                return false;
            }

            // Deduct materials (with rollback on failure)
            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                MaterialCost input = recipe.Inputs[i];
                if (!backpackState.RemoveItem(input.ItemId, input.Count))
                {
                    // Rollback previously removed inputs
                    for (int j = 0; j < i; j++)
                    {
                        backpackState.AddItem(recipe.Inputs[j].ItemId, recipe.Inputs[j].Count);
                    }
                    resourceWalletState.AddLingqi(recipe.LingqiCost);
                    return false;
                }
            }

            // Grant outputs
            foreach (MaterialOutput output in recipe.Outputs)
            {
                backpackState.AddItem(output.ItemId, output.Count);
            }

            rewardText = recipe.Outputs.Count > 0
                ? $"{recipe.DisplayName}完成，获得 {recipe.Outputs[0].ItemId} x{recipe.Outputs[0].Count}"
                : $"{recipe.DisplayName}完成";
            return true;
        }
    }
}
