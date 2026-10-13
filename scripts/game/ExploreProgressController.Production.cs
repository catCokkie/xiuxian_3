using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    public partial class ExploreProgressController
    {
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
