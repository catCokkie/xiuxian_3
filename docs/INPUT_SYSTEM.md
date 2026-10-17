# 全局输入采集系统

本文档说明修仙桌宠项目的全局键鼠输入采集系统实现。

## TL;DR — 快速参考

| 服务 | 文件 | 功能 |
|------|------|------|
| `InputActivityState` | `scripts/services/InputActivityState.cs` | 输入数据聚合、AP 计算、滑动窗口硬封顶 |
| `InputHookService` | `scripts/services/InputHookService.cs` | Win32 全局钩子管理 |
| `InputPauseShortcut` | `scripts/services/InputPauseShortcut.cs` | 全局快捷键 (Ctrl+Shift+X) |

**数值公式**: `AP = key×1.0 + click×1.2 + scroll×0.4 + move/600 + joypad_button×1.0 + joypad_axis×0.8`，滑动窗口硬封顶 `MaxInputPerMinute = 600`（超出部分丢弃；手柄输入同样计入硬封顶）

**隐私**: 不记录键值/轨迹，仅统计次数/距离，本地处理不上传。

**快捷键**: Ctrl+Shift+X 暂停/恢复采集。Windows 限定（其他平台自动降级）。

## 系统架构

```
┌─────────────────────────────────────────────────────────────┐
│                    Windows 系统                              │
│  ┌─────────────┐    ┌─────────────┐                        │
│  │  键盘事件   │    │  鼠标事件   │                        │
│  └──────┬──────┘    └──────┬──────┘                        │
│         │                  │                                │
│         ▼                  ▼                                │
│  ┌─────────────────────────────────┐                       │
│  │  SetWindowsHookEx (WH_KEYBOARD_LL) │ 全局低级钩子       │
│  │  SetWindowsHookEx (WH_MOUSE_LL)    │                   │
│  └────────────────┬────────────────┘                       │
│                   │                                         │
└───────────────────┼─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│                    Godot 应用层                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ InputHookService (Autoload)                         │   │
│  │ • 管理 Win32 钩子生命周期                            │   │
│  │ • 原始事件 → 计数转换                                │   │
│  │ • 支持暂停/恢复采集                                  │   │
│  └────────────────────────┬────────────────────────────┘   │
│                           │                                 │
│                           ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ InputActivityState (Autoload)                       │   │
│  │ • 按帧短批次聚合                                     │   │
│  │ • AP 计算：key*1.0 + click*1.2 + scroll*0.4 + move/600│  │
│  │ • 滑动窗口硬封顶：MaxInputPerMinute = 600（超出丢弃） │ │
│  │ • 累计统计（用于存档）                               │   │
│  └────────────────────────┬────────────────────────────┘   │
│                           │                                 │
│                           ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ ExploreProgressController                           │   │
│  │ • 接收离散输入批次信号                               │   │
│  │ • 按输入计数推进探索/战斗                             │   │
│  │ • 更新 UI 显示                                       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## 核心组件

### 1. InputActivityState (输入活动状态)

**职责**：存储、聚合输入数据，计算 Activity Point (AP)

**关键特性**：
- 按帧短批次聚合原始输入，使用实际经过时间计算活跃窗口
- 不记录具体键值，仅统计次数
- 滑动窗口硬封顶：每分钟最大有效输入 600 次，超出直接丢弃
- 累计总统计值用于持久化

**信号**：
```csharp
[Signal]
public delegate void ActivityTickEventHandler(double apThisBatch, double apFinal, double activeSecondsThisBatch);
```

**AP 计算公式**：
```
AP_raw = key_down * 1.0 + mouse_click * 1.2 + scroll_step * 0.4 + move_px / 600
```

**滑动窗口硬封顶**：
```
MaxInputPerMinute = 600
离散计数 = key_down + mouse_click + scroll_step + joypad_button + joypad_axis_input
离散事件最多 600 次；鼠标移动与离散事件同时受 AP 额度约束：
AP额度 = MaxInputPerMinute × max(1, 各离散输入权重)，默认 720 AP/60秒。
MaxInputPerMinute <= 0 时取消两种封顶。
60 秒滑动窗口内超过 MaxInputPerMinute 的输入事件直接丢弃，不计入 AP。
不给任何 UI 提示（避免暴露检测逻辑）。
```

活跃时间按最近一次真实输入后的 5 秒窗口计算，连续帧只累计实际覆盖秒数；超过封顶的真实输入仍表明用户在工作，但不增加被封顶的 AP/离散推进收益。普通空闲不累计活跃时间。

> 原"高频衰减"公式（`decay = clamp(1 - max(0, R-1)×0.25, 0.45, 1)`）已移除，
> 由硬封顶替代。600/min 是当前配置，合法高速输入也可能触发；应结合人工样本中原始/有效输入的差值校准，不能只凭封顶就判定脚本或工作性质。

### 2. InputHookService (输入钩子服务)

**职责**：使用 Win32 API 设置全局低级钩子

**关键特性**：
- 使用 `WH_KEYBOARD_LL` 和 `WH_MOUSE_LL` 钩子
- 仅采集事件计数，不记录键值内容
- 支持暂停/恢复采集
- 程序退出时自动卸载钩子

**Win32 API 使用**：
- `SetWindowsHookEx` - 安装钩子
- `UnhookWindowsHookEx` - 卸载钩子
- `CallNextHookEx` - 传递事件给下一个钩子

**注意**：此服务仅在 Windows 平台有效，其他平台需要降级实现。

## 隐私与合规

### 数据采集原则

| 采集项 | 说明 | 隐私合规 |
|--------|------|----------|
| key_down_count | 键盘按下次数 | ✅ 不记录具体按键 |
| mouse_click_count | 鼠标点击次数 | ✅ 不记录点击位置 |
| mouse_scroll_step | 滚轮滚动步数 | ✅ 不记录方向内容 |
| mouse_move_distance | 移动距离（像素） | ✅ 仅累积距离，不记录轨迹 |
| joypad_button_count | 手柄按键次数 | ✅ 不记录按键映射 |
| joypad_axis_input_count | 手柄轴输入次数 | ✅ 不记录轴值 |

### 不采集的内容

- ❌ 具体按键值（如 'A', 'Enter', 'Ctrl+C'）
- ❌ 鼠标点击的屏幕坐标
- ❌ 窗口标题或焦点信息
- ❌ 逐次输入时间戳序列（可选试玩统计只保存约60秒或游戏情境变化时的汇总段）

### 用户控制

- 全局快捷键 `Ctrl + Shift + X`：暂停/恢复采集（已接线：`InputPauseShortcut` → `InputHookService.TogglePause()`，见 2026-05-13 N-24）
- 底栏状态指示器：显示当前采集状态
- 设置面板：可完全禁用全局钩子

## 使用方法

### 在其他脚本中读取输入状态

```csharp
// 获取输入状态服务
var activityState = GetNode<InputActivityState>("/root/InputActivityState");

// 读取当前 AP
GD.Print($"AP this second: {activityState.ApThisSecond}");
GD.Print($"AP applied: {activityState.ApFinal}");

// 读取累计统计
GD.Print($"Total key presses: {activityState.TotalKeyDownCount}");
```

### 监听 AP 结算事件

```csharp
public override void _Ready()
{
    var activityState = GetNode<InputActivityState>("/root/InputActivityState");
    activityState.ActivityTick += OnActivityTick;
}

private void OnActivityTick(double apThisSecond, double apFinal)
{
    // 使用 apFinal 计算游戏进度
    // 当前硬封顶模型下，apThisSecond 与 apFinal 一致
}
```

### 控制采集状态

```csharp
// 获取钩子服务
var hookService = GetNode<InputHookService>("/root/InputHookService");

// 暂停采集
hookService.SetPaused(true);

// 恢复采集
hookService.SetPaused(false);

// 完全停止钩子（如需重新启动需调用 StartHook）
hookService.StopHook();
```

## 存档集成

输入统计已集成到存档系统，保存字段：

```json
{
  "input": {
    "stats": {
      "total_key_down": 123456,
      "total_mouse_click": 78901,
      "total_scroll_steps": 3456,
      "total_move_distance": 1234567.8,
      "ap_accumulator": 12.34
    },
    "hook_paused": false
  }
}
```

## 调参与监控

### 可配置参数

在 `InputActivityState` 中可调整：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| KeyDownWeight | 1.0 | 按键权重 |
| MouseClickWeight | 1.2 | 鼠标点击权重 |
| ScrollStepWeight | 0.4 | 滚轮步数权重 |
| MovePxDivider | 600.0 | 移动距离除数 |
| InputWindowSeconds | 60.0 | 硬封顶统计窗口长度 |
| MaxInputPerMinute | 600 | 60秒滑动窗口内最大有效输入次数 |

### 调试输出

运行时可在 Godot 输出面板查看：
- `InputHookService: Global hooks started successfully` - 钩子启动成功
- `InputHookService: Paused/Resumed` - 暂停状态变更
- AP/s 显示在底栏活动速率标签

## 常见问题

### Q: 为什么需要管理员权限？
A: 全局钩子通常不需要管理员权限，但某些安全软件可能会拦截。如果遇到问题，尝试以管理员身份运行 Godot 编辑器或导出的游戏。

### Q: 会影响系统性能吗？
A: 影响极小。低级钩子直接在内核态处理，回调函数只做简单的计数累加，不涉及复杂计算或 I/O。

### Q: 支持多显示器吗？
A: 支持。鼠标移动距离按像素计算，跨显示器时会正确累加总距离。

### Q: 如何防止脚本刷分？
A: 滑动窗口硬封顶机制会丢弃 60 秒内超过 600 次的输入，超出部分不计入 AP。鼠标移动也占用同一 AP 额度；`ApAccumulator` 只作显示缓冲，不负责限制结算收益。

## 平台适配

| 平台 | 支持状态 | 说明 |
|------|----------|------|
| Windows | ✅ 完全支持 | 使用 Win32 全局钩子 |
| macOS | ⚠️ 待实现 | 需要 CGEventTap 实现 |
| Linux | ⚠️ 待实现 | 需要 X11/evdev 实现 |

非 Windows 平台当前会静默降级（无全局输入），后续可通过条件编译或接口实现多平台支持。

## 可选的人工试玩操作统计

入口为“设置→隐私→人工试玩·操作统计”（小窗口需向下滚动）。先选情境，再点“开始新统计”；可在统计中改变情境，点“停止统计”结束，或随时点“导出本次CSV”。“打开统计目录”可查看/自行删除日志。统计默认关闭且不随角色存档/应用重启自动恢复；开始后从下一次输入聚合观察起计数，停止时只含已观察的聚合，边界可能相差一帧。隐私采集暂停仍有效，统计可记录暂停期间的零输入和游戏状态。

情境为玩家自报的`unspecified / typing / reading / mouse_work / mixed_work / playtest`，不是自动识别应用、工作内容或生产效率。键盘数是采集器收到的keydown事件数，不是字符数；中文输入法、组合键和按住重复会改变比例。全局Hook可收到系统重复keydown，应用内回退忽略Echo；需结合`Capture`比较，不能混作同一采集方式。当前隐私页的“本次会话”计数为进程内原始输入总数，独立于已保存的历史累计；本次试玩统计还有单独的开始/停止范围。

- 正常日志位置：`user://operation_statistics/operations_<UTC时间>_<随机ID>.jsonl`；隔离冒烟使用`res://.godot/operation_statistics/`，不读写真实玩家日志。
- JSONL的`session_start`包含UTC开始时间、日志schema、存档schema、游戏版本、实际输入权重/封顶窗口与初始探索速度；`sample`是可累加的数据行；`context`是情境变化；`game_event`记录选定的游戏操作/结果；`session_end`是整段总数核对，**不能与sample再相加**。
- 分类计数包括键盘、点击、滚轮格数、手柄按键和轴事件，各有原始/有效份数；`LimitedTotal=RawTotal-AcceptedTotal`。鼠标移动仅保存像素距离，不加入输入次数。`RawAp/AcceptedAp`分开记录，共享AP额度也可能截去移动收益。开始统计或切换情境不重置游戏原有封顶/AP窗口，统计初段可能承接此前的窗口占用。
- 上下文包括自报情境、主活动、区域、境界、探索/战斗/休整状态、书卷是否打开/当前页、工作模式、键鼠采集方式（global/in_app_fallback/paused/unavailable）。隔离输入固定标为`isolated_test`。手柄仍是应用内来源。
- 命名事件覆盖书卷打开/关闭、实际换页、主活动切换、五加工开始/停止、换装/继承成功或拒绝、Boss挑战，以及新发生的战斗/制作/突破/研习/装备结果。结果不是手动点击，事件数也不是完整UI点击次数；同一次输入不能与事件数相加。其他操作及失败原因可通过人工观察补记，不宣称已全面追踪每个控件。
- 时长使用记录期间单调时钟，包含空闲，以及进程存活期间的挂起间隔，不等同工作或注意力时长。活跃秒数仍来自原有5秒输入窗口，不能表示专注；书卷开着也不代表焦点一定在游戏内。日志不保存按键值、逐次输入时间戳、轨迹、应用/窗口信息或自由文本；没有网络上传和存档迁移。
- 约每60秒或游戏上下文变化时落盘，停止/导出会刷新不足一分钟的部分；突发退出可能丢最后未落盘片段，缺`session_end`应按未完整记录分析。文件出错会停止统计并显示原因，不阻止游戏结算；单次10000行上限防止无限记录，达到后需重新开始。历史日志保留到用户自行删除。

批量分析使用Python标准库，无需安装数据分析包：

```powershell
python tools/analyze_operation_statistics.py "<统计目录>" --output .godot/operation_statistics_summary.json
```

命令同时输出汇总JSON及同名CSV，按情境、活动、采集方式、书卷打开、工作模式和战斗状态分组，提供各类型总数、有效/原始每分钟、封顶比例、AP、活跃秒数和分类事件数；忽略末尾汇总的重复份数，对未结束/坏行/重复会话给出说明。请分析自己需要的日志，不将真实用户日志提交Git；截图和反馈中也只分享经过自查的内容。

所有人工验证步骤、状态与证据统一维护在`design/10_todo.md`的人工验收总表，不在本文复制勾选进度。
