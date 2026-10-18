using Godot;
using System.Linq;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private VBoxContainer? _huntRoot, _huntDetails;
    private Button? _huntToggle;
    private OptionButton? _huntTarget;
    private Label? _huntSummary;
    private bool _refreshingHunt;
    private void BuildDungeonHuntUi(VBoxContainer parent)
    {
        _huntRoot = new() { Name = "DungeonHuntRoot" }; parent.AddChild(_huntRoot);
        _huntToggle = new() { Name = "DungeonHuntToggle", Text = "定向讨伐与材料来源 ▸", ToggleMode = true }; _huntRoot.AddChild(_huntToggle);
        _huntDetails = new() { Name = "DungeonHuntDetails", Visible = false }; _huntRoot.AddChild(_huntDetails);
        _huntTarget = new() { Name = "DungeonHuntTarget", FitToLongestItem = false, ClipText = true }; _huntDetails.AddChild(_huntTarget);
        _huntSummary = new() { Name = "DungeonHuntSummary", AutowrapMode = TextServer.AutowrapMode.WordSmart }; _huntDetails.AddChild(_huntSummary);
        HFlowContainer actions = new() { Name = "DungeonHuntActions" }; _huntDetails.AddChild(actions);
        Button equipment = new() { Name = "HuntReturnEquipment", Text = "返回装备与强化" }; actions.AddChild(equipment);
        equipment.Pressed += () => SetActiveLeftTab("EquipmentTab");
        Button preparation = new() { Name = "HuntReturnPreparation", Text = "返回挑战准备" }; actions.AddChild(preparation);
        preparation.Pressed += () =>
        {
            if (_cultivationRoot.FindChild("PreparationToggle", true, false) is Button toggle) toggle.ButtonPressed = true;
            _cultivationScroll!.EnsureControlVisible(_challengeGoalSummary!);
        };
        _huntToggle.Toggled += open => { _huntDetails.Visible = open; RefreshDungeonHuntUi(); };
        _huntTarget.ItemSelected += index =>
        {
            if (_refreshingHunt) return;
            if (_levelConfigLoader?.TrySetHuntTarget(_huntTarget.GetItemMetadata((int)index).AsString()) == true)
                _operationStatistics?.RecordEvent("dungeon_hunt_changed");
            RefreshDynamicTabContent();
        };
    }
    private void RefreshDungeonHuntUi()
    {
        if (_huntRoot == null || _levelConfigLoader == null) return;
        _huntRoot.Visible = _playerActionState?.IsDungeonMode == true;
        _huntDetails!.Visible = _huntToggle!.ButtonPressed;
        string target = _levelConfigLoader.HuntTarget();
        _huntToggle.Text = target == "" ? "定向讨伐与材料来源 ▸" : "定向讨伐中 · 查看材料与取舍";
        _refreshingHunt = true; _huntTarget!.Clear();
        _huntTarget.AddItem("常规探索 · 原有速度与遭遇分布"); _huntTarget.SetItemMetadata(0, "");
        foreach (string id in _levelConfigLoader.GetHuntTargets())
        {
            int index = _huntTarget.ItemCount;
            _levelConfigLoader.TryGetMonsterStatProfile(id, out var monster);
            _huntTarget.AddItem("讨伐 · " + UiText.LocalizeGameNames(monster.DisplayName)); _huntTarget.SetItemMetadata(index, id);
            if (!_levelConfigLoader.HuntUnlocked) _huntTarget.SetItemDisabled(index, true);
            if (id == target) _huntTarget.Select(index);
        }
        _refreshingHunt = false;
        string materials = target == "" ? "选择本区怪物，可查看其材料来源，并提高其基础出场权重。" : "目标材料（每次胜利的配置期望，非保证）："
            + string.Join("、", _levelConfigLoader.GetMonsterMaterialAverages(target).Where(x => x.Value > 0)
                .Select(x => $"{UiText.BackpackItemName(x.Key)}≈{x.Value:0.##}"));
        _huntSummary!.Text = (!_levelConfigLoader.HuntUnlocked ? "炼气3层且首通幽泉外层后开放定向讨伐。\n" : "") + materials
            + "\n讨伐：探索速度为常规80%，后续新遭遇改用本区出场表，所选怪物的基础权重×2后归一化；常规仍按原序列。不是实际出现率或掉落翻倍，精通精英加成与掉落额度照旧。"
            + "\n只影响后续新生成的遭遇，已有队列与战斗不重抽。探索100%后可继续在线讨伐补料，保留挑战资格；切回常规即停止新遭遇。"
            + "\n离线沿用讨伐速度与基础权重，到Boss就绪停止，不自动挑战。";
    }
    private void OpenHuntForMaterial(string itemId)
    {
        string target = _levelConfigLoader?.BestHuntForMaterial(itemId) ?? "";
        if (target == "" || _levelConfigLoader?.TrySetHuntTarget(target) != true) return;
        _playerActionState?.SetAction("dungeon"); SetActiveLeftTab("CultivationTab");
        _huntToggle!.ButtonPressed = true; RefreshDungeonHuntUi();
        _cultivationScroll!.EnsureControlVisible(_huntDetails!);
        _operationStatistics?.RecordEvent("dungeon_hunt_material");
    }
}
