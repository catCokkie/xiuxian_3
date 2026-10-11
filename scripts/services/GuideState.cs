using Godot;
using System.Collections.Generic;
namespace Xiuxian.Scripts.Services
{
    public partial class GuideState : Node, IDictionaryPersistable
    {
        [Signal] public delegate void NoticeReadyEventHandler(string message);
        private readonly HashSet<string> _completed = new();
        public bool Complete(string step)
        {
            string message = step switch
            {
                "first_input" => "正常键鼠操作会推进修炼，无需额外点击。",
                "first_encounter" => "遇到敌人了，继续正常操作即可推进战斗回合。",
                "first_victory" => "首战胜利！打开书卷查看掉落和装备。",
                "first_book" => "书卷里可以查看修炼进度、资源与动态记录。",
                _ => ""
            };
            if (message == "" || !_completed.Add(step)) return false;
            ServiceLocator.Instance?.EventLogState?.AddEvent(EventLogState.CategorySystem, message);
            EmitSignal(SignalName.NoticeReady, message);
            return true;
        }
        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
            => new() { ["steps_completed"] = new Godot.Collections.Array<string>(_completed) };
        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            _completed.Clear();
            if (data.ContainsKey("steps_completed")) foreach (string step in (Godot.Collections.Array<string>)data["steps_completed"]) _completed.Add(step);
        }
    }
}
