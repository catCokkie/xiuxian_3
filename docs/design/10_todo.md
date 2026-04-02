# Codex 实施待办清单

> 项目：xiuxian_4 — Godot 4.5.1 + C# (.NET 8) 桌宠修仙挂机游戏
> 生成日期：2026-03-24
> 每个任务标记了优先级(P0/P1/P2)、依赖和验收标准。
>
> **唯一定义点原则**：各 TASK 中引用的配方/材料/数值以 `02_systems.md` 对应 § 为权威来源。TASK 描述仅做摘要引用，不另立口径。
> 约定：修改后必须通过 `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj` 零失败。
> 维护规则：`docs/design/10_todo.md` 是任务状态唯一真源；`AGENTS.md` 仅保留流程规则，不重复维护任务进度。

## 当前进度记录（更新：2026-04-02）

### 总览
- 代码任务：Phase 1–8 主功能实现项除 `TASK-06` 外已全部落地
- 测试现状：`dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj` 当前存在 1~2 项不稳定失败；2026-04-02 两次复跑分别为 `315/316` 与 `314/316`，集中在 `ActivityRegistryTests` / `GenericCraftingProgressionTests`
- 存档版本：v9（v5→v6→v7→v8→v9 迁移链完整）
- 子系统：11 个活动模式（原 12 个，悟道已移除），11 × 4 = 44 条精通定义总计 1900 悟性
- 待人工验收：`TASK-06 场景文件 UTF-8 编码修复`
- 已删除系统：AFK 检测、灵宠数值（pet_affinity/pet_mood）、悟道（EnlightenmentRules/State）
- 深化迭代：`16_subsystem_deepening.md` 追踪 25 项深化意见（D-01~D-25），D-01 已完成
- 维护规则：新增任务或状态变更时，只更新本文件，不在 agent 指令文件重复记录

### 待办事项

| 优先级 | 编号 | 内容 | 对应文档 |
|--------|------|------|---------|
| P0 | N-04 | 符箓/体修代码↔文档对齐：代码中符箓只有 2 个英文名、体修只有 2 个功法，需补齐至文档定义的 3 个 | D-02/D-03 |
| P0 | N-05 | ActivityRegistry / 通用配方测试稳定性修复：当前 `dotnet test` 复跑存在 1~2 项不稳定失败，集中在 `ActivityRegistryTests` / `GenericCraftingProgressionTests` | — |
| P1 | — | 周天系统代码实现（§6.1 "运功周天"机制） | `02_systems.md` §6.1 |
| P1 | — | 渐进解锁代码实现（§21 按 realm_level 解锁模式/Tab） | `02_systems.md` §21 |
| P1 | — | 坊市系统代码实现（§22 四货架商店） | `02_systems.md` §22 |
| P1 | — | 灵田重构为真实时间驱动（当前代码仍为输入驱动） | `02_systems.md` §13 |
| P1 | — | 输入防刷：衰减→滑动窗口硬封顶 600/min | `INPUT_SYSTEM.md` |
| P1 | — | 隐私首启卡片 + 设置页 | `15_ui_prompts.md` §13/§14 |
| P2 | — | 统计概览 Tab 扩充（5 区 30+ 指标，需新增 `PlayerStatsState`） | `15_ui_prompts.md` §7 |
| P2 | — | 区域 3-5 新怪物代码接入（阴潮蛇崽/孢子幼体/骨仆从） | `09_level_monster_drop_sample.json` |
| V2 | — | 成就/里程碑系统 | — |
| V2 | — | 灵石商店商品扩展 | `02_systems.md` §22 |
| V2 | — | 赛季/重生机制 | — |

---

### 变更日志

#### 2026-04-02：文档瘦身（4 项压缩操作）

| 编号 | 操作 | 效果 |
|------|------|------|
| C-01 | `10_todo.md` 已完成任务压缩为归档表 | 1732→433 行（-75%） |
| C-02 | `06_bottom_exploration_battle.md` 唯一内容合入 `02_systems.md` §4，删除源文件 | -341 行，文件数 -1 |
| C-03 | `16_subsystem_deepening.md` 重写为紧凑表格 | 337→41 行（-88%） |
| C-04 | `13_offline_settlement.md` 合并 18B/18C/18G 三个"不支持离线"桩为一张表 | ~700→428 行 |

#### 2026-04-02：设计文档全面优化（4 轮审查 + 26 项变更）

**系统新增（7项）**：
- **周天系统**：§6.1 从纯通知层重构为"运功周天"——小周天(25min活跃)奖励悟性+2~5、大周天(4×小周天)奖励灵石、调息 buff 灵气+50%。见 `02_systems.md` §6.1、`05_ui_style.md`、`16_subsystem_deepening.md` D-25
- **坊市系统**：§22 四分类货架（消耗品/扩容/便利/稀有），灵石消费端统一承接。见 `02_systems.md` §22、`15_ui_prompts.md` §15、`03_progression_and_balance.md`
- **渐进解锁**：§21 初始 3 模式(副本/修炼/炼丹)+4 Tab，按 realm_level 分 5 波解锁，未解锁=不可见。见 `02_systems.md` §21、`15_ui_prompts.md`
- **隐私感知**：设置页新增隐私子页(§13)+首启说明卡片(§14)。见 `15_ui_prompts.md`、`00_vision.md`
- **灵田真实时间驱动**：唯一不需输入的系统——播种后自动生长(30min/2h/8h)，手动收获，无过熟。见 `02_systems.md` §13、`13_offline_settlement.md` §18A
- **灵渔 4 池子**：灵泉浅滩/幽潭/龙涎渊/化龙泊，每池专属稀有掉落连接不同下游。见 `02_systems.md` §15
- **灵膳具体化**：4 种灵膳(清心鱼汤/化灵鱼脍/凝神鱼羹/铁壁鱼膳)，长效被动增益(30-60min活跃时间)，与丹药定位区分。见 `02_systems.md` §17

**系统移除（3项，含代码清理）**：
- **AFK 检测**：删除 `AfkDetectionRules.cs`/测试/Runtime AFK 逻辑/JSON `anti_afk_rule`（4 文件删除+多处编辑）
- **灵宠数值**：删除 `pet_affinity`/`pet_mood` 全链路（~20 代码文件+8 文档），底栏动画保留为纯视觉
- **悟道系统**：删除 `EnlightenmentRules`/`EnlightenmentState` 全链路（~18 代码文件+14 文档），12→11 模式，精通 2080→1900 悟性

**机制替换（4项）**：
- **输入防刷**：高频衰减 → 滑动窗口硬封顶 600 次/分钟。见 `INPUT_SYSTEM.md`、`02_systems.md` §1/§8
- **动态 Tab 合并战斗日志**：独立战斗日志 Tab 删除，合并至动态 Tab "战斗"筛选视图，页签 7→6。见 `15_ui_prompts.md` §4
- **灵膳口径统一**：06 文档从"3 场战斗"→"活跃输入时间 30-60min"，与 02 §17 对齐
- **体修材料递增**：从 +50% 指数 → +1 线性。见 `02_systems.md` §19

**内容新增（2项）**：
- **区域 3-5 主题怪物**：阴潮蛇崽(水灵珀)/孢子幼体(毒孢粉)/骨仆从(冥骨片) 替换苔藓史莱姆。见 `09_level_monster_drop_sample.json`
- **内容模板**：07 新增 §4b 灵膳 + §4c 鱼塘 + §4d 作物模板，含 V1 实例数据（4 灵膳/4 鱼塘/3 作物）

**UI 设计补充（1项）**：
- **统计概览 Tab 丰富化**：从 4 分类扩充为 5 个可折叠区（总览/资源/战斗/制作与采集/灵田），30+ 指标。见 `15_ui_prompts.md` §7

**审计修复（4项）**：
- R-08：解锁新模式时自动赠送 Lv1 精通（02 §21）
- R-09：01_core_loop 灵渔描述改为摘要+引用
- R-10：13_offline §3.2 新增灵田例外注释
- R-14：体修材料 +50% 指数→+1 线性

**文档一致性修复（50+ 处）**：
- 战斗日志独立 Tab 引用：05/06/04/02/16 全部更新
- 高频衰减→硬封顶：03/INPUT_SYSTEM/16 D-08/D-22 更新
- 灵宠相关：00/01/02§7/03/04/06 全部清理
- 悟道相关：00/01/02/03/04/05/06/07/11/13/14/15/16/AGENT_PROMPT 全部清理
- 12→11 系统计数：全文档统一

**代码验证**：316 测试全部通过，0 编译错误

#### 2026-03-30
- **UI 主面板重构**：修复信号断连/空白屏幕；重写 `MainBarWindow.tscn` 三行布局
- **UI 设计提示词逐项实现**：依据 `15_ui_prompts.md` 12 个页面提示词逐一对照实现（#1~#12）
- **D-01 Toast 通知系统**：`ToastController.cs` 队列式通知，331 测试通过

#### 2026-03-29
- **`TASK-30~42` 全部落地**：熟练度/存档/UI/原 12 子系统横向扩展，后续于 2026-04-02 清理为 11 系统，存档升至 v8
- **存档安全加固**：SaveAllState 事务化、MigrateToLatest 按步回滚、ValidatePostMigration
- **健壮性加固**：ReadStateSafe、null/负值防御、信号退订修复
- **可维护性加固**：LevelDefaults 常量、IDictionaryPersistable、StatePersistenceManager
- **架构优化**：MaxRollingQueueSize、InputActivityRulesTests（317 测试通过）
- **背包格子化重构**：三分区 GridContainer、稀有度着色
- **配置校验页面布局修复**：PanelContainer 自动布局
- **UI 设计提示词文档创建**：`15_ui_prompts.md` 12 页面提示词
- **装备子系统关联扩充**：11 §15 矩阵设计、25 装备模板、5 新系列、锻造配方

#### 2026-03-26
- **设计变更**：境界突破→纯经验驱动，悟性→子系统精通货币（`TASK-26` 被 `TASK-30` 系列替代）
- **设计变更 #2**：子系统从 4 扩至 12（参考 Melvor），新增 Phase 7（TASK-36~42）

### 已完成

- `TASK-01 拆分 LevelConfigLoader 上帝对象` ✅
  - `LevelConfigLoader.cs` 已瘦身为 159 行兼容门面，保留 `/root/LevelConfigLoader` 访问路径
  - 已拆出 `LevelConfigProvider`、`MonsterConfigService`、`LevelRuntimeStateService`、`ConfigValidationService`
  - `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj` 已通过（162/162）
- `TASK-02 拆分 ExploreProgressController` ✅
  - `ExploreProgressController.cs` 已收口为 11 行入口，运行时胶水迁入 `ExploreProgressController.Runtime.cs`
  - 已拆出 `ExploreGameLogic.cs`（纯逻辑）与 `BattleTrackVisualizer.cs`（战斗轨道可视化）
  - 已补充 `ExploreGameLogicTests.cs`，覆盖探索完成、战斗触发、Boss 失败重置
- `TASK-03 存档版本迁移框架` ✅
  - 已新增 `scripts/services/SaveMigrationRules.cs`（当前 LatestVersion = 6）
  - 已接入 `PrototypeRootController.LoadUnifiedState()`
  - 已补充 `tests/Xiuxian2.Tests/SaveMigrationRulesTests.cs`
  - 已新增文档 `docs/design/14_save_migration.md`
- `TASK-04 离线结算集成实现` ✅
  - `ApplyOfflineSettlementIfNeeded()` 已在 `PrototypeRootController.LoadAllState()` 中调用
  - 修炼/副本双路径离线结算已完整接入
- `TASK-05 装备正式内容闭环（Phase A）` ✅
  - 装备模板 JSON 已有 4 个模板覆盖 3 槽位
  - `BuildEquipmentOverviewText()` / `BuildBackpackOverviewText()` 已实现
  - `EquipFromBackpack()` 一键装备已可用
- `TASK-15 隐藏未实现的设置项` ✅
  - 已在设置页隐藏 `taskbar_icon`、`show_control_markers`、`milestone_tips`
  - 保留存档键，避免影响旧存档兼容
- `TASK-19 活动模式扩展（4 模式）` ✅
  - `PlayerActionState` 新增 `ModeAlchemy` / `ModeSmithing`，`ToggleMode()` 支持 4 模式循环
  - `PlayerActionCapabilityRules` 已覆盖 alchemy/smithing 分支
- `TASK-20 炼丹系统实现` ✅
  - `AlchemyRules.cs`（配方：回气丹=灵草×2, 聚灵散=灵草×3）+ `AlchemyState.cs` 已实现
  - `PotionInventoryState.cs` 丹药背包已接入；`CraftingProgressionService.cs` 统管进度推进
  - `AlchemyRulesTests.cs` 测试已覆盖
- `TASK-21 炼器/强化系统实现` ✅
  - `SmithingRules.cs` + `SmithingState.cs` 已实现，强化公式 `1.08^lv`
- `TASK-22 Boss 挑战系统` ✅
  - `BossEncounterRules.cs` 已实现（BossProfile 生成、超时判负、弱点领悟）
  - `BossEncounterRulesTests.cs` 含 4 个测试
- `TASK-23 战斗消耗品（丹药自动使用）` ✅
  - `ConsumableUsageRules.cs` 已实现（回气丹 HP<50%、聚灵散战斗开始时）
- `TASK-24 灵石经济接入代码` ✅
  - `ResourceWalletState.SpiritStones` + `AddSpiritStones()` 已实现
  - `RewardRules.CalculateBattleSpiritStoneReward()` 按区域缩放（Normal=3+danger×2, Elite+2, Boss=15）
- `TASK-25 战斗失败规则 + 副本循环` ✅
  - `BattleDefeatDecision.ShouldResetExploreProgress`：Boss 失败=true，普通/精英=false
  - `BattleLifecycleRules.DetermineDefeatReset()` 已正确实现；区域解锁 `unlocked_zone_ids` 已接入
- `TASK-26 悟性新消耗路径` ✅ → 已废弃，被 TASK-30~35 替代
  - 原实现的 `InsightSpendRules`（Boss 弱点、高阶炼丹解锁）将在 TASK-30/32 中重构
  - 悟性消费模型从零散消耗改为子系统熟练度解锁统一体系
- `TASK-29 存档 v5→v6 迁移代码` ✅
  - `SaveMigrationRules.LatestVersion = 6`，`MigrateV5ToV6()` 已实现
- `TASK-07 测试补全 — 战斗数学与结算` ✅
  - 已新增 `tests/Xiuxian2.Tests/BattleRulesTests.cs`
  - 已覆盖战斗伤害、缩放伤害、回合胜负与战斗流程分支
- `TASK-08 测试补全 — 存档往返` ✅
  - 已新增 `tests/Xiuxian2.Tests/SaveRoundTripTests.cs`
  - 已补充 `Backpack/Wallet/PlayerProgress/EquippedItems` 纯持久化规则，运行时 State 已统一委托到规则层
- `TASK-10 统计概览 Tab 内容实现` ✅
  - `BuildStatsOverviewText()` 已展示累计输入、活跃时长、当前境界停留、战斗总数/胜率、累计资源获得
  - `InputActivityState.TotalActiveSeconds`、`ResourceWalletState.TotalEarned*`、战斗累计计数均已接入持久化
  - 已补充 `tests/Xiuxian2.Tests/UiTextStatsTests.cs`
- `TASK-09 服务定位器替代硬编码路径` ✅
  - 已新增 `scripts/services/ServiceLocator.cs` 统一集中 autoload 访问
  - `project.godot` 已注册 `ServiceLocator` 为首个 autoload
  - `PrototypeRootController` / `BookTabsController` 已改为通过 `ServiceLocator.Instance` 获取全局状态
- `TASK-13 离线结算与每日上限一致性` ✅
  - 离线副本结算已接入剩余 daily rolls 约束，材料/装备掉落会按剩余配额缩放
  - 运行时掉落状态已支持按关卡聚合剩余日配额，并在跨日场景下自动视为重置
  - 已补充 `DungeonOfflineSettlementRulesTests` 覆盖离线掉落被日上限截断场景
- `TASK-12 UI 自适应布局修复` ✅
  - `MainBarLayoutController` 已提取布局常量并按容器宽度动态计算战斗轨道/按钮/进度条宽度
  - `ResizeHandleButton` 已改为右侧相对锚定，避免依赖绝对 `1040.0` 偏移
  - `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj` 已通过（163/163）
- `TASK-16 Steam Cloud 接口抽象` ✅
  - 已新增 `ISaveCloudProvider` / `NullCloudProvider` / `SteamCloudProvider`
  - `CloudSaveSyncService` 已改为 provider 工厂 + 接口调用，不再内嵌 Steam 反射桥实现
  - `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj` 已通过（163/163）
- `TASK-17 全局反挂机规则` ✅ → 已移除（2026-04-01）
  - 原实现曾引入 `AfkDetectionRules` 与探索倍率衰减
  - 现行版本已删除 AFK 检测、运行时倍率逻辑与相关存档/文档口径
- `TASK-18 遭遇率境界缩放` ✅
  - 已按 `playerRealmLevel - zoneDangerLevel` 缩放遭遇率，并 clamp 到 `[0.05, 0.95]`
  - 当前关卡 `danger_level` 已接入运行时 `ActiveLevelManager` / `LevelConfigLoader`
  - 已补充遭遇率缩放与触发拦截测试，`dotnet test` 全绿（171/171）

### 已落地的补充修复（待办外）
- `修炼概况` 页已改为“状态总览 + 当前判断”结构
  - 当前会展示主行为、当前重心、成长状态、战斗准备、资源判断与当前判断
  - 文案重心从纯数值堆叠调整为中性状态判断，避免在多子系统方向上替玩家做全局决策
- 主行为切换文案已补充收益提示
  - 当前模式切换按钮与下拉项统一改为“模式名｜收益说明”格式，如“副本｜材料装备”“炼丹｜战斗丹药”
  - 玩家在主界面即可理解四种模式的短期用途，降低模式切换心智成本
- 玩家高频摘要文案已统一到“结果/状态/收益”表达
  - 离线结算、战斗日志、修炼概况中的关键判断句已尽量统一为标签式总结，减少流水账和指令式口吻
  - 该方向更适配后续向多子系统扩展时的自由选择体验
- 已补上子菜单中的"突破"按钮
- 已接入背包页基础入口与装备/背包展示线路
- 已默认隐藏主条状态栏，缓解主形象区域与状态栏重叠问题
- 文档审计修复（2026-03-25）：
  - `06_bottom_exploration_battle.md` §3a/§5 Boss 失败规则统一为"进度归零"
  - `03_progression_and_balance.md` 炼丹材料名统一为灵草、聚灵散输入 300→220、Boss 领悟消耗 50→30-80
  - `12_equipment_sample_qi_refining.json` 兑换配方 cost_items→cost_spirit_stones
  - `11_equipment_content_system.md` 兑换字段 cost_items→cost_spirit_stones
  - `14_save_migration.md` 当前版本 v5→v6、"预写迁移"→"已实现迁移"

### 已处理但仍需人工验收
- `TASK-06 场景文件 UTF-8 编码修复`
  - 代码侧中文文本与场景文件已修复
  - 仍需在 Godot 编辑器内手动打开主场景，最终确认无 Parse Error

### 明确延后到 V2
- `TASK-11 宠物亲密度最小闭环`
  - 已删除（2026-04-01），`pet_affinity`、`pet_mood` 及全部相关代码已移除
  - 等级、全局增益、互动/羁绊闭环统一延后到 V2 设计

### V2 候选任务：阵法全局加成器重构

#### TASK-FORM-01: 阵法状态专用化（P2）
**状态**: 进行中（2026-03-30）
**依赖**: TASK-38, TASK-40, TASK-41
**背景**: 当前 `FormationState` 复用 `RecipeProgressState`，只能表达“当前制作配方 + 进度”，不足以承载“已拥有阵法 / 当前激活阵法 / 多槽位 / 自修复”等全局阵法玩法。

**涉及文件**:
- `scripts/services/FormationState.cs` — **新建**，阵法专用状态
- `scripts/services/ServiceLocator.cs` — 阵法状态类型调整
- `scripts/game/PrototypeRootController.cs` — 统一存档注册更新
- `scripts/services/SaveMigrationRules.cs` — 旧阵法状态向新结构迁移

**目标字段**:
- `crafted_ids[]`
- `inventory{formation_id: count}`
- `active_primary_id`
- `active_secondary_id`
- `selected_recipe`
- `progress`

**验收标准**:
- 阵法系统不再依赖 `RecipeProgressState` 作为运行时状态
- 专用状态可完整 round-trip 持久化
- 为双槽 / 自修复 / 当前激活阵法预留字段

**当前进展**:
- 已新增 `scripts/services/FormationState.cs`，承载 `crafted_ids`、`inventory`、`active_primary_id`、`active_secondary_id`、`selected_recipe`、`progress`
- 已新增 `scripts/services/FormationPersistenceRules.cs`，将阵法专用状态从 Godot Node 持久化逻辑中剥离，便于纯规则测试
- `project.godot`、`scripts/services/ServiceLocator.cs`、`scripts/game/PrototypeRootController.cs`、`scripts/game/ExploreProgressController.Runtime.cs` 已切到专用 `FormationState`
- 已新增 `scripts/services/IRecipeProgressState.cs`，用于兼容当前通用配方推进链路，避免阵法专用化后打断现有加工模式运行时逻辑
- `tests/Xiuxian2.Tests/SaveRoundTripTests.cs` 已补阵法状态 round-trip 覆盖

#### TASK-FORM-02: 阵法效果模型统一化（P2）
**状态**: 进行中（2026-03-30）
**依赖**: TASK-FORM-01
**背景**: 当前 `FormationRules` 仅支持少量战斗/收益修正，且效果表达零散。需要升级为“全局可切换加成器”的统一效果模型。

**涉及文件**:
- `scripts/services/FormationRules.cs`
- `scripts/services/ActivityEffectRules.cs`
- `tests/Xiuxian2.Tests/FormationRulesTests.cs`

**步骤**:
1. 新增统一效果结构（建议 `FormationEffectProfile`）
2. 统一表达以下维度：
   - 战斗：攻击 / 防御 / 生存 / 掉落
   - 采集：灵田 / 矿脉 / 灵渔速度或产量
   - 加工：炼丹 / 炼器 / 符箓 / 烹饪 / 阵法速度或折扣
   - 修行：灵气 / 悟性 / 修行效率
3. 提供 `GetFormationProfile(formationId, masteryLevel)` 与 `GetMaxSlotCount(masteryLevel)`

**首批建议阵法**:
- `formation_spirit_plate`：聚灵阵（灵气收益）
- `formation_guard_flag`：护体阵（战斗防御）
- `formation_harvest_array`：丰饶阵（采集效率）
- `formation_craft_array`：工巧阵（加工效率）

**验收标准**:
- 阵法效果不再只表现为单个 `CharacterStatModifier`
- 所有阵法效果可通过统一 profile 查询
- 阵法精通 Lv2/Lv3/Lv4 可自然映射到效果增强 / 双槽 / 自修复

**当前进展**:
- `scripts/services/FormationRules.cs` 已新增 `FormationEffectProfile`，统一表达战斗加成、灵气收益、采集速度、加工速度和“是否为已知阵法”
- 现有 `formation_spirit_plate` / `formation_guard_flag` 已切到 profile 查询，同时补入 `formation_harvest_array` / `formation_craft_array` 两个首批非战斗阵法定义
- `GetModifier()` / `GetLingqiRewardRate()` 已改为从 profile 派生，兼容现有调用点；新增 `GetGatherSpeedRate()` / `GetCraftSpeedRate()` 供后续全局接线使用
- `scripts/services/ActivityEffectRules.cs` 已改为统一通过 `GetActiveFormationProfile(...)` 读取阵法效果，避免战斗 / 收益入口各自维护判定逻辑
- `tests/Xiuxian2.Tests/FormationRulesTests.cs` 已补 profile 语义覆盖：战斗阵、采集阵、加工阵、精通倍率、双槽与自修复

#### TASK-FORM-03: 全局生效链路接入（P1）
**状态**: 进行中（2026-03-30）
**依赖**: TASK-FORM-02
**背景**: 阵法需要成为真正的“全局策略姿态”，而不是只对战斗或灵气收益产生局部修正。

**涉及文件**:
- `scripts/game/ExploreProgressController.Runtime.cs`
- `scripts/game/PrototypeRootController.cs`
- `scripts/services/ActivityEffectRules.cs`
- `tests/Xiuxian2.Tests/ActivityEffectRulesTests.cs`

**步骤**:
1. 战斗链：读取当前激活阵法并应用到战斗属性
2. 收益链：将阵法加成接入灵气 / 悟性 / 掉落结算
3. 采集链：将阵法加成接入灵田 / 矿脉 / 灵渔推进
4. 加工链：将阵法加成接入炼丹 / 炼器 / 符箓 / 烹饪 / 阵法制作

**验收标准**:
- 切换当前阵法后，不同主行为的推进或收益会即时改变
- 副本 / 采集 / 加工 / 修行至少各有 1 类阵法可感知收益
- 测试覆盖不同上下文下的阵法生效结果

**当前进展**:
- `scripts/game/ExploreProgressController.Runtime.cs` 已将阵法采集速度加成接入 `AdvanceGardenByInput()` / `AdvanceMiningByInput()` / `AdvanceFishingByInput()`
- 同文件已将阵法加工速度加成接入 `AdvanceAlchemyByInput()` / `AdvanceSmithingByInput()` / `AdvanceGenericRecipeByInput()`，通过统一 `ApplyProgressRate()` 缩放输入推进量
- 当前推进链优先读取 `FormationState.ActivePrimaryId`，未激活时回退到 `SelectedRecipeId`，保证旧流程仍可感知阵法效果
- `tests/Xiuxian2.Tests/ActivityEffectRulesTests.cs` 已补 `CollectFormationGatherSpeedRate()` / `CollectFormationCraftSpeedRate()` 运行时 helper 覆盖
- 阵法对采集/加工链路的运行时 helper 覆盖已补齐

#### TASK-FORM-04: 阵法切换 UI 与可视化（P2）
**状态**: 进行中（2026-03-30）
**依赖**: TASK-FORM-01, TASK-FORM-02, TASK-FORM-03
**背景**: 若玩家看不到“当前生效阵法”和“它正在加成什么”，阵法就仍然只是隐藏数值，而不是正式系统。

**涉及文件**:
- `scripts/ui/BookTabsController.cs`
- `scripts/ui/UiText.cs`
- `scripts/game/ExploreProgressPresentationRules.cs`

**步骤**:
1. 在书卷中新增阵法激活区：已拥有阵法、当前激活阵法、切换按钮
2. 展示“当前主行为下，本阵法提供的加成摘要”
3. 主底栏补充当前阵法简称 / 状态提示
4. 为 V2 预留副阵槽 UI（Lv3 双槽）

**验收标准**:
- 玩家可在 UI 中查看并切换当前生效阵法
- 当前阵法名称、效果摘要、适配方向可被清晰理解
- 不需要阅读日志或代码即可知道阵法是否在生效

**当前进展**:
- `scripts/ui/BookTabsController.cs` 已新增阵法选择与“激活阵法”按钮，当前可在书卷中切换 `FormationState.ActivePrimaryId`
- 阵法精通 Lv3 的双槽效果已接入：当前支持 `ActivePrimaryId + ActiveSecondaryId`，副阵按 50% 效果参与战斗 / 采集 / 加工 / 灵气收益结算
- 制作阵法成功后会同步记录到 `FormationState` 的 `crafted_ids/inventory`，且首次制作会自动激活为当前主阵法
- `scripts/ui/UiText.cs` 已新增阵法效果摘要文案，`scripts/game/ExploreProgressPresentationRules.cs` 已新增 `BuildFormationStatusText()`
- 装备页 / 修炼概况 / 主战斗轨道文案现在会显示当前生效阵法；Lv3 解锁后书卷 UI 也可切换副阵
- `tests/Xiuxian2.Tests/ExploreProgressPresentationRulesTests.cs`、`tests/Xiuxian2.Tests/FormationRulesTests.cs`、`tests/Xiuxian2.Tests/ActivityEffectRulesTests.cs` 已覆盖阵法状态文案、双槽叠加与副阵半效规则

#### TASK-FORM-05: 阵法存档迁移与测试收口（P1）
**状态**: 进行中（2026-03-30）
**依赖**: TASK-FORM-01, TASK-FORM-04
**背景**: 阵法状态结构从“通用 recipe state”升级为“专用全局系统”后，需要补齐 migration 与测试，保证旧档兼容。

**涉及文件**:
- `scripts/services/SaveMigrationRules.cs`
- `tests/Xiuxian2.Tests/SaveMigrationRulesTests.cs`
- `tests/Xiuxian2.Tests/SaveRoundTripTests.cs`

**验收标准**:
- 旧档 `formation.state.selected_recipe/progress` 可自动迁移到新结构
- `active_primary_id` / `inventory` / `crafted_ids` 有稳定默认值
- `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj` 全通过

**当前进展**:
- `scripts/services/SaveMigrationRules.cs` 已升级为 `LatestVersion = 9`，新增 `MigrateV8ToV9()`，将旧阵法通用状态迁移到专用结构
- v8→v9 现会为 `formation.state` 补齐 `active_primary_id` / `active_secondary_id` / `crafted_ids` / `inventory`，并优先从旧背包中的阵法物品推导库存与默认激活阵法
- `tests/Xiuxian2.Tests/SaveMigrationRulesTests.cs` 已新增 v8 阵法状态迁移断言，覆盖旧 `selected_recipe/progress` 向专用阵法结构的升级
- `tests/Xiuxian2.Tests/SaveRoundTripTests.cs` 已补阵法空结构/默认值 round-trip 测试，验证专用状态在缺字段场景下稳定恢复
- 阵法专用状态的迁移与 round-trip 测试已补覆盖

---

## 已完成任务归档（Phase 1-8）

> 以下为 TASK-01~42 + Phase 8 文档审计的精简完成记录。详细实施方案已归档，如需查看历史版本请用 git log。

| Phase | TASK | 标题 | 完成日期 | 关键产出 |
|-------|------|------|---------|---------|
| **P1: P0** | 01 | 拆分 LevelConfigLoader 上帝对象 | 03-25 | LevelConfigProvider/MonsterConfigService/LevelRuntimeStateService/ConfigValidationService |
| | 02 | 拆分 ExploreProgressController | 03-25 | ExploreGameLogic.cs + BattleTrackVisualizer.cs |
| | 03 | 存档版本迁移框架 | 03-24 | SaveMigrationRules.cs + 14_save_migration.md |
| | 04 | 离线结算集成实现 | 03-25 | 修炼/副本双路径离线结算 |
| | 05 | 装备正式内容闭环 Phase A | 03-25 | 4 模板 3 槽位 + 一键装备 |
| | 06 | 场景文件 UTF-8 编码修复 | — | **需人工验收**：Godot 编辑器打开主场景确认无 Parse Error |
| **P2: P1** | 07 | 测试补全 — 战斗数学 | 03-25 | BattleRulesTests.cs |
| | 08 | 测试补全 — 存档往返 | 03-25 | SaveRoundTripTests.cs |
| | 09 | 服务定位器替代硬编码路径 | 03-26 | ServiceLocator.cs |
| | 10 | 统计概览 Tab 内容实现 | 03-26 | TotalActiveSeconds/TotalEarned*/战斗统计 |
| | 11 | 宠物亲密度最小闭环 | — | **已移除**（04-01），灵宠系统整体删除 |
| | 12 | UI 自适应布局修复 | 03-26 | MainBarLayoutController 动态计算 |
| | 13 | 离线结算与每日上限一致性 | 03-26 | 日配额截断 + 跨日重置 |
| **P3: P2** | 14 | 消除魔法数字 | 03-26 | GameBalanceConstants 集中管理 |
| | 15 | 隐藏未实现的设置项 | 03-24 | 隐藏 3 项 + 保留存档键 |
| | 16 | Steam Cloud 接口抽象 | 03-26 | ISaveCloudProvider / NullCloudProvider |
| | 17 | 全局反挂机规则 | 03-26 | **已移除**（04-01），AFK 系统整体删除 |
| | 18 | 遭遇率境界缩放 | 03-26 | playerRealm - zoneDanger 缩放 clamp [0.05, 0.95] |
| **P4: Melvor 扩展** | 19 | 活动模式扩展 4 模式 | 03-26 | ModeAlchemy / ModeSmithing |
| | 20 | 炼丹系统实现 | 03-26 | AlchemyRules/State + PotionInventoryState |
| | 21 | 炼器/强化系统实现 | 03-26 | SmithingRules/State, 强化公式 1.08^lv |
| | 22 | Boss 挑战系统 | 03-26 | BossEncounterRules（Profile/超时/弱点） |
| | 23 | 战斗消耗品自动使用 | 03-26 | ConsumableUsageRules（HP<50% 回气丹） |
| **P5: 审计修复** | 24 | 灵石经济接入代码 | 03-26 | SpiritStones + 区域缩放奖励 |
| | 25 | 战斗失败规则 + 副本循环 | 03-26 | Boss 失败进度归零 + zone 解锁 |
| | 26 | 悟性新消耗路径 | — | **已废弃**，被 TASK-30 系列替代 |
| | 27 | 突破丹已取消 | — | 无需实施 |
| | 28 | Boss 内容样本填充 | 03-26 | 5 区域 Boss 数据 |
| | 29 | 存档 v5→v6 迁移代码 | 03-26 | MigrateV5ToV6() |
| **P6: 精通重构** | 30 | 子系统熟练度规则层 | 03-29 | SubsystemMasteryRules + 44 条定义 |
| | 31 | 存档迁移 v6→v7 | 03-29 | MigrateV6ToV7() |
| | 32 | 境界突破去悟性化 | 03-29 | 纯经验驱动突破 |
| | 33 | 炼丹/炼器熟练度门控 | 03-29 | 精通等级解锁配方 |
| | 34 | 熟练度 UI 入口 | 03-29 | 书卷精通面板 |
| | 35 | 熟练度测试补全 | 03-29 | SubsystemMasteryRulesTests |
| **P7: 11 系统横向扩展** | 36 | 通用活动框架 | 03-29 | ActivityFramework + IActivityDefinition |
| | 37 | 采集系统（灵田/矿脉/灵渔） | 03-29 | GardenState/MiningState/FishingState |
| | 38 | 加工系统（符箓/烹饪/阵法） | 03-29 | TalismanState/CookingState/FormationState |
| | 39 | 修行系统（体修） | 03-29 | BodyCultivationRules/State（悟道已移除） |
| | 40 | 存档迁移 v7→v8 | 03-29 | MigrateV7ToV8() |
| | 41 | 新系统精通树集成 | 03-29 | 11×4 精通定义 = 1900 悟性 |
| | 42 | 新系统测试覆盖 | 03-29 | 全系统 round-trip + 规则测试 |
| **P8: 文档审计** | — | 16 项跨文档一致性修复 | 03-30 | 灵气产消/悟性口径/资源图/离线§18A-H/消耗品/灵石消费 |

### 阵法重构任务（V2 候选，进行中）

| TASK | 标题 | 状态 | 依赖 |
|------|------|------|------|
| FORM-01 | 阵法状态专用化 | 进行中(03-30) | TASK-38/40/41 |
| FORM-02 | 阵法效果模型统一化 | 进行中(03-30) | FORM-01 |
| FORM-03 | 全局生效链路接入 | 进行中(03-30) | FORM-02 |
| FORM-04 | 阵法切换 UI 与可视化 | 进行中(03-30) | FORM-01/02/03 |
| FORM-05 | 阵法存档迁移与测试收口 | 进行中(03-30) | FORM-01/04 |

