using Xiuxian.Scripts.Services;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Game
{
    public partial class ExploreProgressController
    {
        public bool IsInBattle => _inBattle;
        public CharacterStatBlock GetPreparationStats() => GetPreparationStats(_equippedItemsState?.GetEquippedProfiles() ?? Array.Empty<EquipmentStatProfile>());
        public CharacterStatBlock GetPreparationStats(IReadOnlyList<EquipmentStatProfile> equipment)
        {
            var modifiers = new List<CharacterStatModifier>(BuildNonEquipmentModifiers(false));
            modifiers.Add(new CharacterStatModifier(DefenseRate: ServiceLocator.Instance?.CookingState?.ActiveMeal.DefenseRate ?? 0));
            return CharacterStatRules.BuildFinalStats(
                PlayerBaseStatRules.BuildBaseStats(_playerProgressState?.RealmLevel ?? 1, _levelConfigLoader?.PlayerBaseHp ?? 36, _levelConfigLoader?.PlayerAttackPerRound ?? 4),
                equipment, modifiers);
        }
        private CraftingProductionPlan? GetCurrentProductionPlan() => (_actionState?.ActionId ?? "") switch
        {
            "mining" when _miningState?.Recycling == true => _miningState.RecyclingProduction,
            "alchemy" => _alchemyState?.Production,
            "smithing" => _smithingState?.Production,
            "talisman" => _talismanState?.Production,
            "cooking" => _cookingState?.Production,
            "formation" => _formationState?.Production,
            _ => null,
        };

        public ProductionCheck GetProductionCheck(string system)
        {
            int mastery = _subsystemMasteryState?.GetLevel(system) ?? 1;
            int realm = _playerProgressState?.RealmLevel ?? 1;
            if (system == "mining" && _miningState?.Recycling == true)
                return CraftingProgressionService.CheckRecipe(RecipeUnlockRules.Recycle, _backpackState, _resourceWalletState, mastery, _miningState.RecyclingProduction, realm);
            if (system == "smithing")
                return _smithingState != null && _equippedItemsState?.TryGetEquippedProfileById(_smithingState.TargetEquipmentId, out EquipmentStatProfile profile) == true
                    ? CraftingProgressionService.CheckSmithing(profile, _backpackState, _resourceWalletState, mastery, _smithingState.Production, realm)
                    : ProductionBlockRules.Unavailable(_smithingState?.TargetEquipmentId ?? "");
            IRecipeProgressState? state = system switch { "talisman" => _talismanState, "cooking" => _cookingState, "formation" => _formationState, "body_cultivation" => _bodyCultivationState, _ => null };
            string recipeId = system == "alchemy" ? _alchemyState?.SelectedRecipeId ?? "" : state?.SelectedRecipeId ?? "";
            var plan = system == "alchemy" ? _alchemyState?.Production : CraftingProductionPlan.Supports(system) ? state?.Production : null;
            return CraftingProgressionService.CheckRecipe(recipeId, _backpackState, _resourceWalletState, mastery, plan, realm);
        }
        private bool ShowProductionBlock(string system)
        {
            var check = GetProductionCheck(system);
            if (check.CanProduce) return false;
            _roundInfoLabel.Text = GetCurrentProductionPlan()?.Summary ?? "";
            _battleInfoLabel.Text = ProductionBlockRules.Summary(check) + " 保留当前进度。";
            if (_actionState?.WorkMode != true) _battleInfoLabel.Show();
            return true;
        }
    }
}
