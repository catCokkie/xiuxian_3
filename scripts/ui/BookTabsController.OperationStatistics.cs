using Godot;
using System;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private static readonly string[] OperationScenarios = { "unspecified", "typing", "reading", "mouse_work", "mixed_work", "playtest" };
    private OperationStatisticsRecorder? _operationStatistics;
    private OptionButton? _operationScenario;
    private Button? _operationStartStop;
    private Label? _operationStatus;
    public string OperationStatisticsScenario => OperationScenarios[Math.Clamp(_operationScenario?.Selected ?? 0, 0, OperationScenarios.Length - 1)];
    public void BindOperationStatistics(OperationStatisticsRecorder recorder) { _operationStatistics = recorder; RefreshOperationStatistics(); }

    private void BuildOperationStatisticsUi(VBoxContainer parent)
    {
        VBoxContainer root = new() { Name = "OperationStatistics", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        root.AddThemeConstantOverride("separation", 8); parent.AddChild(root);
        root.AddChild(new Label { Text = "人工试玩 · 操作统计" });
        root.AddChild(new Label { Text = "手动标注当前情境，统计输入类型、原始/有效总数及游戏操作。默认关闭，每次启动需重新开始；只保存本地汇总，不记录具体按键、位置或应用内容。",
            AutowrapMode = TextServer.AutowrapMode.WordSmart });
        HFlowContainer row = new(); row.AddThemeConstantOverride("h_separation", 8); row.AddThemeConstantOverride("v_separation", 8); root.AddChild(row);
        _operationScenario = new() { Name = "OperationScenario", TooltipText = "这是你自己选择的测试情境，游戏不会识别你使用的应用或工作内容。" };
        foreach (string label in new[] { "未标注", "文字输入", "阅读思考", "鼠标操作", "混合办公", "试玩管理" }) _operationScenario.AddItem(label);
        row.AddChild(_operationScenario);
        _operationScenario.ItemSelected += _ => { _operationStatistics?.RecordEvent("scenario_selected"); RefreshOperationStatistics(); };
        _operationStartStop = new() { Name = "OperationStartStop", Text = "开始统计" }; row.AddChild(_operationStartStop);
        _operationStartStop.Pressed += () =>
        {
            if (_operationStatistics == null) return;
            if (_operationStatistics.IsRecording) _operationStatistics.Stop();
            else _operationStatistics.Start(OperationStatisticsDirectory());
            RefreshOperationStatistics();
        };
        Button export = new() { Name = "OperationExport", Text = "导出本次CSV" }; row.AddChild(export);
        export.Pressed += () => { _operationStatistics?.Export(); RefreshOperationStatistics(); };
        Button folder = new() { Name = "OperationFolder", Text = "打开统计目录" }; row.AddChild(folder);
        folder.Pressed += () =>
        {
            string directory = OperationStatisticsDirectory();
            if (DirAccess.DirExistsAbsolute(directory)) OS.ShellOpen(directory);
        };
        Button reconnect = new() { Name = "OperationReconnect", Text = "重连采集",
            TooltipText = "输入不再增长时重建全局采集连接；保留暂停状态、已有统计和游戏进度。" };
        row.AddChild(reconnect);
        reconnect.Pressed += () =>
        {
            var hook = ServiceLocator.Instance?.InputHookService;
            hook?.StopHook(); hook?.StartHook();
            _operationStatistics?.RecordEvent("capture_reconnect"); RefreshOperationStatistics();
        };
        _operationStatus = new() { Name = "OperationStatisticsStatus", AutowrapMode = TextServer.AutowrapMode.WordSmart }; root.AddChild(_operationStatus);
        RefreshOperationStatistics();
    }
    public static string OperationStatisticsDirectory() => ProjectSettings.GlobalizePath(
        Array.IndexOf(OS.GetCmdlineUserArgs(), "--smoke-test") >= 0 ? "res://.godot/operation_statistics" : "user://operation_statistics");
    private void RefreshOperationStatistics()
    {
        if (_operationStatus == null || _operationStartStop == null) return;
        _operationStartStop.Disabled = _operationStatistics == null;
        _operationStartStop.Text = _operationStatistics?.IsRecording == true ? "停止统计" : "开始新统计";
        var totals = _operationStatistics?.Session?.Totals;
        _operationStatus.Text = (_operationStatistics?.Status ?? "统计服务尚未就绪。")
            + "\n采集状态：" + (ServiceLocator.Instance?.InputHookService?.CaptureStatus ?? "采集服务未就绪")
            + (totals == null ? "" : $"\n本次原始输入{totals.RawTotal}，有效输入{totals.AcceptedTotal}，封顶未计入{totals.LimitedTotal}。"
                + $"\n键盘{totals.Keys} / 点击{totals.Clicks} / 滚轮{totals.Scroll} / 手柄按键{totals.JoyButtons} / 轴输入{totals.JoyAxes}；鼠标移动{totals.MouseDistance:0}像素（不计入输入次数）。")
            + (totals == null ? "" : $"\nAP原始{totals.RawAp:0.##} / 有效{totals.AcceptedAp:0.##}。输入次数并非录入字数。")
            + "\n原始输入用于核对实际采集；有效输入用于游戏推进，默认60秒最多600次。统计仅包含按下开始之后的输入，滚轮按完整刻度累计。"
            + "\n" + (_operationStatistics?.LastPath.Length > 0 ? _operationStatistics.LastPath : OperationStatisticsDirectory());
    }
}
