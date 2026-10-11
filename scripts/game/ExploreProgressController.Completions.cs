using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Game
{
    public partial class ExploreProgressController
    {
        private void RecordActivityCompletion(string message)
        {
            if (_settlingOfflineCrafts)
            {
                _offlineCraftMessages[message] = _offlineCraftMessages.TryGetValue(message, out int count) ? count + 1 : 1;
                return;
            }
            ServiceLocator.Instance?.EventLogState?.AddEvent(EventLogState.CategoryCraft, message);
        }
    }
}
