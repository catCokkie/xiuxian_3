using Xiuxian.Scripts.Services;
using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Game
{
    public partial class ExploreProgressController
    {
        public CharacterStatBlock GetPreparationStats()
        {
            var modifiers = new List<CharacterStatModifier>(BuildNonEquipmentModifiers(false));
            modifiers.Add(new CharacterStatModifier(DefenseRate: ServiceLocator.Instance?.CookingState?.ActiveMeal.DefenseRate ?? 0));
            return CharacterStatRules.BuildFinalStats(
                PlayerBaseStatRules.BuildBaseStats(_playerProgressState?.RealmLevel ?? 1, _levelConfigLoader?.PlayerBaseHp ?? 36, _levelConfigLoader?.PlayerAttackPerRound ?? 4),
                _equippedItemsState?.GetEquippedProfiles() ?? Array.Empty<EquipmentStatProfile>(), modifiers);
        }
        private CraftingProductionPlan? GetCurrentProductionPlan() => (_actionState?.ActionId ?? "") switch
        {
            "alchemy" => _alchemyState?.Production,
            "smithing" => _smithingState?.Production,
            "talisman" => _talismanState?.Production,
            "cooking" => _cookingState?.Production,
            "formation" => _formationState?.Production,
            _ => null,
        };
    }
}
