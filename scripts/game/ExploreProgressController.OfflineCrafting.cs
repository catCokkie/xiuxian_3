using System.Collections.Generic;
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
            string result = messages.Count > 0 ? string.Join("\n", messages) : "暂无成品；保留当前进度。缺料或达到上限时暂停加工。";
            return GetCurrentProductionPlan() is { } plan ? plan.Summary + "\n" + result : result;
        }
    }
}
