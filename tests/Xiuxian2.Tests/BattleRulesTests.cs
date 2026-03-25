using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class BattleRulesTests
{
    [Fact]
    public void ConsumeBattleInputs_ResolvesRoundsAndRemainder()
    {
        BattleInputProgress result = BattleRules.ConsumeBattleInputs(4, 14, 6);

        Assert.Equal(new BattleInputProgress(6, 18, 3, 0), result);
    }

    [Fact]
    public void CalculateAttackDamage_AttackMinusDefense_MinimumOne()
    {
        int damage = BattleRules.CalculateAttackDamage(5, 8);

        Assert.Equal(1, damage);
    }

    [Fact]
    public void CalculateAttackDamage_NormalDamage()
    {
        int damage = BattleRules.CalculateAttackDamage(10, 3);

        Assert.Equal(7, damage);
    }

    [Fact]
    public void CalculateScaledDamage_DividerApplied()
    {
        int damage = BattleRules.CalculateScaledDamage(12, 4);

        Assert.Equal(3, damage);
    }

    [Fact]
    public void CalculateScaledDamage_MinDamageFloor()
    {
        int damage = BattleRules.CalculateScaledDamage(2, 10, 1);

        Assert.Equal(1, damage);
    }

    [Fact]
    public void ResolvePlayerVsMonsterRound_PlayerWins()
    {
        CharacterBattleSnapshot player = new(20, 20, 10, 1, 1, 0.0, 1.5);
        CharacterBattleSnapshot monster = new(6, 6, 4, 0, 1, 0.0, 1.5);

        BattleRoundResult result = BattleRules.ResolvePlayerVsMonsterRound(player, monster, 4, 1);

        Assert.Equal(BattleOutcome.PlayerWon, result.Outcome);
    }

    [Fact]
    public void ResolvePlayerVsMonsterRound_MonsterWins()
    {
        CharacterBattleSnapshot player = new(12, 3, 2, 1, 1, 0.0, 1.5);
        CharacterBattleSnapshot monster = new(12, 12, 6, 5, 1, 0.0, 1.5);

        BattleRoundResult result = BattleRules.ResolvePlayerVsMonsterRound(player, monster, 1, 1);

        Assert.Equal(BattleOutcome.MonsterWon, result.Outcome);
    }

    [Fact]
    public void ResolvePlayerVsMonsterRound_DoubleKO()
    {
        CharacterBattleSnapshot player = new(8, 4, 8, 1, 1, 0.0, 1.5);
        CharacterBattleSnapshot monster = new(8, 8, 4, 0, 1, 0.0, 1.5);

        BattleRoundResult result = BattleRules.ResolvePlayerVsMonsterRound(player, monster, 1, 1);

        Assert.Equal(BattleOutcome.DoubleKnockout, result.Outcome);
    }

    [Fact]
    public void DetermineBattleFlow_VictoryAction()
    {
        BattleFlowDecision result = BattleRules.DetermineBattleFlow(BattleOutcome.PlayerWon);

        Assert.Equal(new BattleFlowDecision(BattleFlowAction.Victory, true), result);
    }

    [Fact]
    public void DetermineBattleFlow_DefeatAction()
    {
        BattleFlowDecision result = BattleRules.DetermineBattleFlow(BattleOutcome.MonsterWon);

        Assert.Equal(new BattleFlowDecision(BattleFlowAction.Defeat, true), result);
    }

    [Fact]
    public void DetermineBattleFlow_DoubleKnockoutAction()
    {
        BattleFlowDecision result = BattleRules.DetermineBattleFlow(BattleOutcome.DoubleKnockout);

        Assert.Equal(new BattleFlowDecision(BattleFlowAction.DoubleKnockout, true), result);
    }

    [Fact]
    public void DetermineBattleFlow_OngoingContinues()
    {
        BattleFlowDecision result = BattleRules.DetermineBattleFlow(BattleOutcome.Ongoing);

        Assert.Equal(new BattleFlowDecision(BattleFlowAction.Continue, false), result);
    }
}
