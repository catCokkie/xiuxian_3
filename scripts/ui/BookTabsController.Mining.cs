using Godot;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private OptionButton? _miningOperation;
    private void BuildMiningOperationPicker(HFlowContainer row)
    {
        _miningOperation = new() { Name = "MiningOperation" };
        _miningOperation.AddItem("开采矿石"); _miningOperation.AddItem("回收寒铁"); row.AddChild(_miningOperation);
        _miningOperation.ItemSelected += index =>
        {
            if (index == 1 && (_playerProgressState?.RealmLevel ?? 1) < 3) return;
            _miningState?.SetRecycling(index == 1); RefreshDynamicTabContent();
        };
    }
    private void RefreshMiningOperationPicker()
    {
        if (_miningOperation == null) return;
        _miningOperation.SetItemDisabled(1, (_playerProgressState?.RealmLevel ?? 1) < 3);
        _miningOperation.Select(_miningState?.Recycling == true ? 1 : 0);
        _miningOperation.TooltipText = "炼气3层开放回收寒铁；切换会保存进度并暂停回收，返回后手动继续。回收只在当前采矿活动在线推进。";
    }
}
