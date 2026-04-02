using Godot;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Global main-action mode:
    /// - dungeon: explore + battle loop
    /// - cultivation: pause dungeon progression and focus on cultivation conversion
    /// </summary>
    public partial class PlayerActionState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void ModeChangedEventHandler(string modeId);

        [Signal]
        public delegate void ActionChangedEventHandler(string actionId, string actionTargetId, string actionVariant);

        public const string ModeDungeon = "dungeon";
        public const string ModeCultivation = "cultivation";
        public const string ModeAlchemy = "alchemy";
        public const string ModeSmithing = "smithing";
        public const string ModeGarden = "garden";
        public const string ModeMining = "mining";
        public const string ModeFishing = "fishing";
        public const string ModeTalisman = "talisman";
        public const string ModeCooking = "cooking";
        public const string ModeFormation = "formation";
        public const string ModeBodyCultivation = "body_cultivation";

        public const string ActionDungeon = ModeDungeon;
        public const string ActionCultivation = ModeCultivation;
        public const string ActionAlchemy = ModeAlchemy;
        public const string ActionSmithing = ModeSmithing;
        public const string ActionGarden = ModeGarden;
        public const string ActionMining = ModeMining;
        public const string ActionFishing = ModeFishing;
        public const string ActionTalisman = ModeTalisman;
        public const string ActionCooking = ModeCooking;
        public const string ActionFormation = ModeFormation;
        public const string ActionBodyCultivation = ModeBodyCultivation;

        private string _actionId = ActionDungeon;
        private string _actionTargetId = string.Empty;
        private string _actionVariant = string.Empty;

        public string ModeId => _actionId;
        public string ActionId => _actionId;
        public string ActionTargetId => _actionTargetId;
        public string ActionVariant => _actionVariant;
        public bool IsDungeonMode => _actionId == ActionDungeon;
        public bool IsCultivationMode => _actionId == ActionCultivation;
        public bool IsAlchemyMode => _actionId == ActionAlchemy;
        public bool IsSmithingMode => _actionId == ActionSmithing;
        public bool IsDungeonAction => _actionId == ActionDungeon;
        public bool IsCultivationAction => _actionId == ActionCultivation;
        public bool IsAlchemyAction => _actionId == ActionAlchemy;
        public bool IsSmithingAction => _actionId == ActionSmithing;

        public void SetMode(string modeId)
        {
            SetAction(modeId);
        }

        public void SetAction(string actionId, string actionTargetId = "", string actionVariant = "")
        {
            PlayerActionStateRules.PlayerActionStateData next = PlayerActionStateRules.Normalize(actionId, actionTargetId, actionVariant);
            if (next.ActionId == _actionId && next.ActionTargetId == _actionTargetId && next.ActionVariant == _actionVariant)
            {
                return;
            }

            bool actionChanged = next.ActionId != _actionId;
            _actionId = next.ActionId;
            _actionTargetId = next.ActionTargetId;
            _actionVariant = next.ActionVariant;

            if (actionChanged)
            {
                EmitSignal(SignalName.ModeChanged, _actionId);
            }

            EmitSignal(SignalName.ActionChanged, _actionId, _actionTargetId, _actionVariant);
        }

        private static readonly string[] AllModes =
        {
            ActionDungeon, ActionCultivation, ActionAlchemy, ActionSmithing,
            ActionGarden, ActionMining, ActionFishing,
            ActionTalisman, ActionCooking, ActionFormation,
            ActionBodyCultivation,
        };

        public void ToggleMode()
        {
            int current = System.Array.IndexOf(AllModes, _actionId);
            int next = (current + 1) % AllModes.Length;
            SetAction(AllModes[next]);
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["mode_id"] = _actionId,
                ["action_id"] = _actionId,
                ["action_target_id"] = _actionTargetId,
                ["action_variant"] = _actionVariant,
            };
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            PlayerActionStateRules.PlayerActionStateData next = PlayerActionStateRules.FromDictionary(data);
            _actionId = next.ActionId;
            _actionTargetId = next.ActionTargetId;
            _actionVariant = next.ActionVariant;
            EmitSignal(SignalName.ModeChanged, _actionId);
            EmitSignal(SignalName.ActionChanged, _actionId, _actionTargetId, _actionVariant);
        }
    }
}
