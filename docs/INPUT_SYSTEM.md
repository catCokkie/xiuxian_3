# 全局输入采集系统

本文档说明修仙桌宠项目的全局键鼠输入采集系统实现。

## TL;DR — 快速参考

| 服务 | 文件 | 功能 |
|------|------|------|
| `InputActivityState` | `scripts/services/InputActivityState.cs` | 输入数据聚合、AP 计算、滑动窗口硬封顶 |
| `InputHookService` | `scripts/services/InputHookService.cs` | Win32 全局钩子管理 |
| `InputPauseShortcut` | `scripts/services/InputPauseShortcut.cs` | 全局快捷键 (Ctrl+Shift+X) |

**数值公式**: `AP = key×1.0 + click×1.2 + scroll×0.4 + move/600 + joypad_button×1.0 + joypad_axis×0.8`，滑动窗口硬封顶 `MaxInputPerMinute = 600`（超出部分不增加游戏收益，原始采集总数仍保留；手柄输入同样计入硬封顶）

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
统计区显示原始/有效输入及封顶差值；核对实际操作时查看原始输入。
```

活跃时间按最近一次真实输入后的 5 秒窗口计算，连续帧只累计实际覆盖秒数；超过封顶的真实输入仍表明用户在工作，但不增加被封顶的 AP/离散推进收益。普通空闲不累计活跃时间。

> 原"高频衰减"公式（`decay = clamp(1 - max(0, R-1)×0.25, 0.45, 1)`）已移除，
> 由硬封顶替代。600/min 是当前配置，合法高速输入也可能触发；应结合人工样本中原始/有效输入的差值校准，不能只凭封顶就判定脚本或工作性质。

### 2. InputHookService (输入钩子服务)

**职责**：使用 Win32 API 设置全局低级钩子

**关键特性**：
- 使用 `WH_KEYBOARD_LL` 和 `WH_MOUSE_LL` 钩子
- 仅采集事件计数，不记录键值内容
- 使用独立Windows消息线程安装和处理两类Hook，回调只向`GlobalInputCaptureBuffer`做原子累加，不获取游戏状态/存档/UI共用的Monitor锁，不调用Godot Node、场景、信号或写文件；信号与重连由游戏线程处理
- `InputActivityState`在每批结算前请求清空采集缓冲，游戏线程一次性写入分类计数和距离；开始/停止/导出同样先排空缓冲，保持既有统计边界及滑动窗口口径。鼠标路径在采集线程逐事件累计，只传汇总距离，不降采样丢弃移动或离散事件
- 支持暂停/恢复采集；暂停停止接收新输入，已经入队的输入正常结算，不清空封顶窗口
- 程序退出时由采集线程卸载钩子，失败安装时清理已成功安装的一类Hook

**Win32 API 使用**：
- `SetWindowsHookEx` - 安装钩子
- `UnhookWindowsHookEx` - 卸载钩子
- `CallNextHookEx` - 传递事件给下一个钩子

**注意**：此服务仅在 Windows 平台有效，其他平台需要降级实现。

## 输入经验与境界进度的核对

经验按有效离散输入计算，默认修炼模式每次0.35经验，其余活动每次0.28经验；鼠标移动只折算AP资源，不直接增加输入经验。输入经验与静修时间成长分别入账，不能把原始输入数、AP、录入字数或经验百分比混为同一数量。当前活动的单次经验系数显示在修炼首页，境界百分比保留一位小数；悬停可看精确经验/本层需求。突破扣除本层经验并保留溢出，界面百分比变小并不代表丢失全部历史经验。

`FlushPendingInput`的零额外时间批次也结算有效输入经验和AP资源；它不增加活跃时间、周天时长、灵膳耗时或静修时间预算，下一帧不重复发放。周天摘要计入这类批次已发资源，但不凭空推进周期。设置→隐私在统计关闭时也显示本次启动的累计有效输入、实发输入经验、实发在线静修经验；这些进程计数不随角色存档保存/加载重置，离线结算、其他奖励和突破扣费另计。跨活动比较输入经验时，需按各活动的实际有效输入分别乘系数。

统计`session_start`、导出时新增的`checkpoint`以及`session_end`保存进程有效输入/输入经验/在线静修经验快照，分别为`process_effective_inputs`、`process_input_experience`、`process_online_basic_experience`；经验修订标识`experience_settlement_revision=2`，同时记录输入经验系数及修炼额外经验开关。这些是累计快照，取终点减起点，不能逐行累加；`checkpoint`本身的输入Counts为0。CSV末尾三列导出相应快照，sample仍是输入计数累加行。

分析工具JSON新增`ExperienceComparisons`，用导出/结束快照之前的sample，按各行活动计算“有效输入×对应经验系数”的理论输入经验，再与实发输入经验差值核对，并单列在线静修经验及计数守恒；不一致时给出提示。进行中日志仅核对到最近一次导出快照，不把后续sample冒充已核实实发值；缺起点或终点快照的旧日志不具备实际经验核对证据。分组CSV继续用于输入类型/速率分析，实际经验核对见JSON和命令输出；原始JSONL/CSV可用于手动检查快照。

## 高频输入下的界面更新

游戏结算、输入次数、AP和战斗回合按既有批次即时处理，显示刷新独立调度。活动、钱包、经验及制作/采集进度的连续信号只标记书卷待刷新，书卷可见时合并为最多每0.25秒一次完整动态刷新；实际物品/装备/事件变化和用户操作仍可立即更新。书卷隐藏时跳过动态页面构建，但保留装备/背包的持久解锁历史；重新打开立即读取当前状态。隐私面板定时显示计数，不逐个鼠标事件重绘。

`GameplaySmokeTest.tscn -- --smoke-test --mouse-performance-smoke`提供隔离的120帧鼠标移动检查，覆盖隐藏/可见页面重建次数、完整距离和重新打开刷新，并打印托管分配及处理耗时。它不启动真实Hook，不测量物理鼠标到屏幕的实际延迟；硬件采样率、其他系统钩子及GPU占用仍需真实环境复核。完整验收状态和本次测量数据只维护在`design/10_todo.md`。

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

入口为“设置→隐私→人工试玩·操作统计”（小窗口需向下滚动）。先选情境，再点“开始新统计”；可在统计中改变情境，点“停止统计”结束，或随时点“导出本次CSV”。统计区显示实际采集状态（全局/仅游戏窗口/暂停/不可用）；计数停止增长时可点“重连采集”，重建Hook连接并保留原来的暂停状态和统计。全局已连接表示安装/消息线程状态，并不能证明系统从未静默移除Hook。“打开统计目录”可查看/自行删除日志。统计默认关闭且不随角色存档/应用重启自动恢复；开始前先结算旧队列，再开启本次观察，停止/导出前结算已收到的队列，不推进额外时间、不重置封顶窗口，下一帧不重复计数。隐私采集暂停仍有效，已收到的输入不因暂停被丢弃，统计可记录暂停期间的零输入和游戏状态。开始/停止边界指游戏处理这些命令时的队列边界，操作系统尚未投递的事件不在此范围。

情境为玩家自报的`unspecified / typing / reading / mouse_work / mixed_work / playtest`，不是自动识别应用、工作内容或生产效率。键盘数是采集器收到的keydown事件数，不是字符数；中文输入法、组合键和按住重复会改变比例。全局Hook与应用内回退均计入按住产生的重复keydown；释放不计。采集来源仍需结合`Capture`比较，回退只能收到游戏窗口内的事件。当前隐私页的“本次启动”计数为进程内原始输入总数，独立于已保存的历史累计；本次试玩统计还有单独的开始/停止范围。

- 正常日志位置：`user://operation_statistics/operations_<UTC时间>_<随机ID>.jsonl`；隔离冒烟使用`res://.godot/operation_statistics/`，不读写真实玩家日志。
- JSONL的`session_start`包含UTC开始时间、日志schema、存档schema、游戏版本、实际输入权重/封顶窗口与初始探索速度；`input_counting_revision=2`标识含Echo与滚轮余数累计的新口径（旧日志缺此字段），并保存键盘/滚轮计数定义；`sample`是可累加的数据行；`context`是情境变化；`game_event`记录选定的游戏操作/结果；`checkpoint`是导出时的非累加经验快照；`session_end`是整段总数核对，**不能与sample再相加**。
- 滚轮按完整刻度计数：Windows累计同轴绝对增量，每120单位记一步，方向不保留；横向和纵向分别累计。回退使用Godot的`Factor`累计，同样覆盖左右滚动。小增量不会逐事件截断，累计不足一格暂不加次数；暂停/重连时清零小数余量并重建鼠标位置基准，避免把暂停期间的位移算进去。
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

实现依据：[微软WM_MOUSEWHEEL增量说明](https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-mousewheel)要求累计小于120的高精度增量；[LowLevelKeyboardProc说明](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc)明确消息线程、超时静默卸载及专用线程建议。
