using System.Collections.Generic;
using Xiuxian.Scripts.Services;
namespace Xiuxian.Scripts.Game
{
    public partial class ExploreProgressController
    {
        private bool _settlingOfflineCrafts;
        private readonly Dictionary<string, int> _offlineCraftMessages = new();
        public string AdvanceOfflineCrafting(int inputs)
        {
            _offlineCraftMessages.Clear();
            _settlingOfflineCrafts = true;
            try
            {
                if (IsAlchemyMode()) AdvanceAlchemyByInput(inputs);
                else if (IsSmithingMode()) AdvanceSmithingByInput(inputs);
                else if (IsGenericRecipeMode()) AdvanceGenericRecipeByInput(inputs);
            }
            finally { _settlingOfflineCrafts = false; }
            var messages = new List<string>();
            foreach (var entry in _offlineCraftMessages) messages.Add($"{entry.Key}（{entry.Value} 批）");
            string result = messages.Count > 0 ? string.Join("\n", messages) : "暂无成品；保留当前进度。";
            if (GetCurrentProductionPlan() is not { } plan) return result;
            var check = GetProductionCheck(_actionState?.ActionId ?? "");
            string state = check.CanProduce ? "离线加工输入已结算，当前批次仍可继续。" : ProductionBlockRules.Summary(check);
            return plan.Summary + "\n" + result + "\n" + state
                + (check.Blocks.Count > 1 ? "\n其余条件：" + string.Join("\n", System.Linq.Enumerable.Select(System.Linq.Enumerable.Skip(check.Blocks, 1), x => ProductionBlockRules.Describe(check, x))) : "");
        }
    }
}
