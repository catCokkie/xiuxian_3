using Godot;
using System;
using System.Collections.Generic;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private VBoxContainer _inheritPanel = null!;
    private OptionButton _inheritSource = null!, _inheritTarget = null!;
    private Label _inheritPreview = null!, _inheritResult = null!;
    private Button _inheritConfirm = null!;
    private string _inheritSourceId = "", _inheritTargetId = "";
    private EquipmentInheritanceCheck? _inheritReviewed;

    private void BuildInheritanceControls(VBoxContainer parent)
    {
        Button toggle = new() { Name = "InheritanceToggle", Text = "强化继承 ▸", ToggleMode = true };
        parent.AddChild(toggle);
        _inheritPanel = new() { Name = "InheritancePanel", Visible = false, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        parent.AddChild(_inheritPanel);
        toggle.Toggled += open => { _inheritPanel.Visible = open; toggle.Text = open ? "强化继承 ▾" : "强化继承 ▸"; RefreshInheritanceControls(); };
        _inheritPanel.AddChild(new Label { Text = "源装备（当前穿戴）" });
        _inheritSource = new() { Name = "InheritanceSource", FitToLongestItem = false, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _inheritPanel.AddChild(_inheritSource);
        _inheritPanel.AddChild(new Label { Text = "目标装备（背包同槽）" });
        _inheritTarget = new() { Name = "InheritanceTarget", FitToLongestItem = false, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _inheritPanel.AddChild(_inheritTarget);
        _inheritPreview = new() { Name = "InheritancePreview", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _inheritPanel.AddChild(_inheritPreview);
        var row = new HFlowContainer(); _inheritPanel.AddChild(row);
        _inheritConfirm = new() { Name = "InheritanceConfirm", Text = "继承并换装", Disabled = true }; row.AddChild(_inheritConfirm);
        _inheritResult = new() { Name = "InheritanceResult", AutowrapMode = TextServer.AutowrapMode.WordSmart }; _inheritPanel.AddChild(_inheritResult);
        _inheritSource.ItemSelected += index =>
        {
            _inheritSourceId = _inheritSource.GetItemMetadata((int)index).AsString(); _inheritTargetId = "";
            _inheritResult.Text = ""; RefreshInheritanceControls();
        };
        _inheritTarget.ItemSelected += index =>
        {
            _inheritTargetId = _inheritTarget.GetItemMetadata((int)index).AsString(); _inheritResult.Text = ""; RefreshInheritanceControls();
        };
        _inheritConfirm.Pressed += () =>
        {
            if (_inheritReviewed is not EquipmentInheritanceCheck reviewed || _exploreProgressController == null || ServiceLocator.Instance == null) return;
            EquipmentExchangeService.TryInherit(ServiceLocator.Instance, _exploreProgressController, reviewed, out string result);
            _inheritResult.Text = result; RefreshDynamicTabContent();
        };
        RefreshInheritanceControls();
    }

    private void RefreshInheritanceControls()
    {
        if (_inheritPanel == null || _equippedItemsState == null || _backpackState == null || _exploreProgressController == null || ServiceLocator.Instance == null) return;
        var sources = _equippedItemsState.GetEquippedProfiles();
        if (!Array.Exists(sources, p => p.EquipmentId == _inheritSourceId)) _inheritSourceId = sources.Length > 0 ? sources[0].EquipmentId : "";
        FillInheritanceOptions(_inheritSource, sources, _inheritSourceId, "暂无穿戴装备");
        _equippedItemsState.TryGetEquippedProfileById(_inheritSourceId, out var source);
        var targets = new List<EquipmentStatProfile>();
        foreach (var profile in _backpackState.GetEquipmentProfiles())
            if (profile.Slot == source.Slot && profile.EquipmentId != source.EquipmentId) targets.Add(profile);
        if (!targets.Exists(p => p.EquipmentId == _inheritTargetId)) _inheritTargetId = "";
        FillInheritanceOptions(_inheritTarget, targets, _inheritTargetId, targets.Count == 0 ? "背包暂无同槽装备" : "请选择目标装备");
        var check = EquipmentExchangeService.Preview(ServiceLocator.Instance, _exploreProgressController, _inheritSourceId, _inheritTargetId);
        _inheritReviewed = check; _inheritConfirm.Disabled = !check.CanCommit;
        string text = "炼气三层 · 炼器 Lv2 · 首通第一区解锁。旧装备保留并归零；不返材料，不重抽词条。";
        if (!string.IsNullOrEmpty(check.Target.EquipmentId) && !string.IsNullOrEmpty(check.Source.EquipmentId))
        {
            text += $"\n{check.Source.DisplayName} +{check.Source.EnhanceLevel} → +0，放回背包。\n{check.Target.DisplayName} +{check.Target.EnhanceLevel} → +{check.ResultLevel}，换上。\n目标上限 +{check.TargetCap}；消耗 {check.LingqiCost:0} 灵气（源等级累计基础费用的25%，向上取整）。";
            if (check.Source.EnhanceLevel > check.TargetCap) text += $"\n超出上限的 {check.Source.EnhanceLevel - check.TargetCap} 级将损失，费用仍按源 +{check.Source.EnhanceLevel} 计算。";
            if (check.Target.EnhanceLevel > 0) text += "\n目标原有强化将被覆盖，等级不相加。";
            var equipment = _equippedItemsState.GetEquippedProfiles();
            for (int i = 0; i < equipment.Length; i++) if (equipment[i].Slot == check.Target.Slot) equipment[i] = check.Result;
            var before = _exploreProgressController.GetPreparationStats(); var after = _exploreProgressController.GetPreparationStats(equipment);
            text += $"\n换装后总属性：\n气血 {before.MaxHp} → {after.MaxHp}（{Signed(after.MaxHp - before.MaxHp)}） / 攻击 {before.Attack} → {after.Attack}（{Signed(after.Attack - before.Attack)}）\n防御 {before.Defense} → {after.Defense}（{Signed(after.Defense - before.Defense)}） / 速度 {before.Speed} → {after.Speed}（{Signed(after.Speed - before.Speed)}）\n暴击 {before.CritChance:P1} → {after.CritChance:P1} / 暴伤 {before.CritDamage:P1} → {after.CritDamage:P1}";
            text += "\n相关装备的未完成强化进度将清零，生产计划暂停。";
        }
        if (!check.CanCommit) text += "\n" + check.BlockReason;
        _inheritPreview.Text = text;
    }

    private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
    private static void FillInheritanceOptions(OptionButton option, IReadOnlyList<EquipmentStatProfile> profiles, string selected, string empty)
    {
        option.Clear(); option.AddItem(empty); option.SetItemMetadata(0, ""); int select = 0;
        foreach (var profile in profiles)
        {
            option.AddItem($"{UiText.SlotLabel(profile.Slot)} · {profile.DisplayName} +{profile.EnhanceLevel}");
            int index = option.ItemCount - 1; option.SetItemMetadata(index, profile.EquipmentId);
            if (profile.EquipmentId == selected) select = index;
        }
        option.Select(select); option.Disabled = profiles.Count == 0;
        option.TooltipText = option.GetItemText(select);
    }
}
