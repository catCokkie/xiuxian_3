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
        private readonly Godot.Collections.Dictionary<string, Variant> _dungeonHunts = new();
        public string GetDungeonHuntTarget(string levelId) => _dungeonHunts.TryGetValue(levelId, out var id) ? id.AsString() : "";
        internal void SetDungeonHuntTarget(string levelId, string monsterId)
        {
            if (GetDungeonHuntTarget(levelId) == monsterId) return;
            if (monsterId == "") _dungeonHunts.Remove(levelId); else _dungeonHunts[levelId] = monsterId;
            EmitSignal(SignalName.ActionChanged, _actionId, _actionTargetId, _actionVariant);
        }

        public string ModeId => _actionId;
        public string ActionId => _actionId;
        public string ActionTargetId => _actionTargetId;
        public string ActionVariant => _actionVariant;
        public string ConsumablePolicy { get; private set; } = "boss_only";
        public string SuppressionChoice { get; private set; } = "talisman";
        public void SetSuppressionChoice(string choice)
        {
            string next = SuppressionRules.Normalize(choice);
            if (SuppressionChoice == next) return;
            SuppressionChoice = next;
            EmitSignal(SignalName.ActionChanged, _actionId, _actionTargetId, _actionVariant);
        }
        public bool WorkMode { get; private set; }
        public void SetWorkMode(bool work)
        {
            WorkMode = work;
            EmitSignal(SignalName.ActionChanged, _actionId, _actionTargetId, _actionVariant);
        }
        public void SetConsumablePolicy(string policy)
        {
            ConsumablePolicy = policy == "all" || policy == "none" ? policy : "boss_only";
            EmitSignal(SignalName.ActionChanged, _actionId, _actionTargetId, _actionVariant);
        }
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
                ["consumable_policy"] = ConsumablePolicy,
                ["suppression_choice"] = SuppressionChoice,
                ["work_mode"] = WorkMode,
                ["dungeon_hunts"] = _dungeonHunts.Duplicate(),
            };
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            SuppressionChoice = SuppressionRules.Normalize(data.TryGetValue("suppression_choice", out var choice)
                && choice.VariantType == Variant.Type.String ? choice.AsString() : "talisman");
            _dungeonHunts.Clear();
            if (data.TryGetValue("dungeon_hunts", out var hunts) && hunts.VariantType == Variant.Type.Dictionary)
                foreach (var item in (Godot.Collections.Dictionary<string, Variant>)hunts)
                    if (_dungeonHunts.Count < 64 && item.Key.Length is > 0 and <= 128 && item.Value.VariantType == Variant.Type.String
                        && item.Value.AsString().Length is > 0 and <= 128) _dungeonHunts[item.Key] = item.Value;
            WorkMode = data.ContainsKey("work_mode") && data["work_mode"].AsBool();
            SetConsumablePolicy(data.ContainsKey("consumable_policy") ? data["consumable_policy"].AsString() : "boss_only");
            PlayerActionStateRules.PlayerActionStateData next = PlayerActionStateRules.FromDictionary(data);
            _actionId = next.ActionId;
            _actionTargetId = next.ActionTargetId;
            _actionVariant = next.ActionVariant;
            EmitSignal(SignalName.ModeChanged, _actionId);
            EmitSignal(SignalName.ActionChanged, _actionId, _actionTargetId, _actionVariant);
        }
    }
}
