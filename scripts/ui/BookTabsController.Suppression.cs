using Godot;
using System.Linq;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private HFlowContainer? _suppressionChoiceRow;
    private OptionButton? _suppressionChoice;
    private bool _refreshingSuppression;
    private void BuildSuppressionChoice(VBoxContainer parent)
    {
        _suppressionChoiceRow = new() { Name = "SuppressionChoiceRow" }; parent.AddChild(_suppressionChoiceRow);
        _suppressionChoiceRow.AddChild(new Label { Text = "禁疗准备" });
        _suppressionChoice = new() { Name = "SuppressionChoice", TooltipText = "只使用选中的禁疗来源，缺货时不会改用另一种；无回血敌人不用。恢复丹和消耗品总策略仍独立，改变选择不改当前战斗。" };
        _suppressionChoice.AddItem("镇灵符"); _suppressionChoice.AddItem("抑生丹"); _suppressionChoice.AddItem("不用禁疗 · 装备路线");
        _suppressionChoiceRow.AddChild(_suppressionChoice);
        _suppressionChoice.ItemSelected += index =>
        {
            if (_refreshingSuppression) return;
            _playerActionState?.SetSuppressionChoice(index == 1 ? "potion" : index == 2 ? "none" : "talisman");
            _operationStatistics?.RecordEvent("suppression_choice_changed"); RefreshPreparationUi();
        };
    }
    private void RefreshSuppressionChoice(bool hasBoss, MonsterStatProfile boss)
    {
        if (_suppressionChoiceRow == null) return;
        _suppressionChoiceRow.Visible = hasBoss && boss.HealPerRound > 0;
        _refreshingSuppression = true;
        _suppressionChoice!.Select((_playerActionState?.SuppressionChoice ?? "talisman") switch { "potion" => 1, "none" => 2, _ => 0 });
        _suppressionChoice.Disabled = _playerActionState?.ConsumablePolicy == "none";
        _refreshingSuppression = false;
    }
    private string PreparationCounter(MonsterStatProfile boss) => boss.HealPerRound > 0
        ? SuppressionRules.Recipe(_playerActionState?.SuppressionChoice ?? "talisman")
        : ChallengePreparationRules.ChooseTalisman(boss.StrategyTag, 4);
    private static string SuppressionCostSummary(string recipeId)
    {
        var recipe = ActivityRegistry.GetRecipe(recipeId)!;
        return recipe.DisplayName + "：" + string.Join("+", recipe.Inputs.Select(x => UiText.BackpackItemName(x.ItemId) + x.Count))
            + $"、{recipe.LingqiCost:0}灵气、{recipe.RequiredInputEvents}输入";
    }
}
