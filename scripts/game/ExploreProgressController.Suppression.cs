using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game;

public partial class ExploreProgressController
{
    private bool _usedSuppressionPotion;
    private int BattleSuppressionRounds => SuppressionRules.Window(_battleTalismans, _usedSuppressionPotion);
    private void TryUseSuppressionPotion(bool isBattleStart)
    {
        if (!SuppressionRules.ShouldUsePotion(_actionState?.SuppressionChoice ?? "talisman", isBattleStart,
            ResolveCurrentMonsterProfile()?.HealPerRound ?? 0, BattleSuppressionRounds > 0,
            _potionInventoryState?.GetPotionCount(SuppressionRules.PotionId) ?? 0)) return;
        if (_potionInventoryState?.ConsumePotion(SuppressionRules.PotionId) != true) return;
        _usedSuppressionPotion = true;
        _playerStatsState?.RecordBattlePotionConsumption();
        _consumedPotionsThisBattle.Add(UiText.BackpackItemName(SuppressionRules.PotionId));
        _bossAttempt.RecordPotion(SuppressionRules.PotionId, 0);
    }
}
