# Codex 实施待办清单

> 项目：xiuxian_4 — Godot 4.5.1 + C# (.NET 8) 桌宠修仙挂机游戏
> 生成日期：2026-03-24
> 每个任务标记了优先级(P0/P1/P2)、依赖和验收标准。
>
> **唯一定义点原则**：各 TASK 中引用的配方/材料/数值以 `02_systems.md` 对应 § 为权威来源。TASK 描述仅做摘要引用，不另立口径。
> 约定：修改后必须通过 `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj` 零失败。
> 维护规则：`docs/design/10_todo.md` 是任务状态唯一真源；`AGENTS.md` 仅保留流程规则，不重复维护任务进度。

## 当前进度记录（更新：2026-03-26）

### 总览
- 代码任务：除 `TASK-06` 外，Phase 1–5 实现项已全部落地并通过 `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj`
- 待人工验收：`TASK-06 场景文件 UTF-8 编码修复`
- 延后到 V2：`TASK-11 宠物亲密度最小闭环`
- 维护规则：新增任务或状态变更时，只更新本文件，不在 agent 指令文件重复记录
- **设计变更（2026-03-26）**：境界突破改为纯经验驱动，悟性完全转为子系统熟练度解锁货币。`TASK-26` 被 `TASK-30` 系列替代。详见 `02_systems.md` §12。
- **设计变更（2026-03-26 #2）**：子系统从 4 个扩展至 12 个（参考 Melvor Idle），新增 Phase 7（TASK-36~42）。详见 `02_systems.md` §13-§20、`01_core_loop.md` 设计决策记录。

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
- `TASK-17 全局反挂机规则` ✅
  - 已新增 `AfkDetectionRules`，定义 `60s -> 0.5x`、`120s -> 0x` 探索倍率
  - `InputActivityState` 已追踪“距离上次输入秒数”和“本批输入前空闲秒数”
  - 探索推进已接入反挂机倍率，AFK 时会暂停副本推进并显示暂停文案
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
  - 完整的亲密度等级、全局增益、互动/羁绊闭环已延后到 V2
  - `pet_affinity` 目前仍保留为累计资源/统计字段，`pet_mood` 仍会影响当前资源转化倍率
  - 等级、全局增益、互动/羁绊闭环统一延后到 V2 设计

---

## Phase 1: P0 — 阻塞发布

### TASK-01: 拆分 LevelConfigLoader 上帝对象
**状态**: 已完成（2026-03-25）
**依赖**: 无 (基础重构，其他任务受益)
**背景**: `scripts/services/LevelConfigLoader.cs` 共 1531 行、49 个公有方法、33 个私有字段，混合了 6 类职责：配置加载/索引、活跃关卡管理、怪物查询、掉落经济、校验/模拟、运行时持久化。

**涉及文件**:
- `scripts/services/LevelConfigLoader.cs` — 拆分源
- `scripts/services/LevelConfigProvider.cs` — 配置解析与索引
- `scripts/services/MonsterConfigService.cs` — 怪物展示/战斗参数查询
- `scripts/services/LevelRuntimeStateService.cs` — 掉落、结算、运行时持久化与模拟
- `scripts/services/ConfigValidationService.cs` — 配置校验摘要
- `project.godot` — autoload 注册需更新
- `scripts/game/PrototypeRootController.cs` — GetNode 路径 `"/root/LevelConfigLoader"`
- `scripts/game/ExploreProgressController.cs` — GetNode 路径 `"/root/LevelConfigLoader"`
- `scripts/ui/BookTabsController.cs` — 调用校验/模拟方法
- `tests/Xiuxian2.Tests/` — 现有测试引用

**步骤**:
1. 新建 `scripts/services/LevelConfigProvider.cs`:
   - 迁移所有只读配置方法：`LoadConfig()`, `LoadConfigFromText()`, `TryGetMonster()`, `TryGetDropTable()`, `TryGetMonsterStatProfile()`, `TryGetMonsterVisualConfig()`, `TryGetMonsterCombatParams()`, `TryGetMonsterMoveRule()`, `GetLevelIds()`, `GetLevelName()`, `GetSpawnMonsterIds()`, `TryGetEquipmentSeries()`, `TryGetEquipmentTemplate()`, `GetEquipmentSeriesIds()`, `GetEquipmentTemplateIds()`, `GetEquipmentExchangeLevelIds()`, `GetEquipmentExchangeRecipes()`
   - 迁移所有索引字段：`_rootData`, `_levels`, `_monsterById`, `_dropTableById`, `_equipmentSeriesById`, `_equipmentTemplateById`, `_equipmentExchangeRecipesByLevelId`
   - 迁移内部方法：`ParseLevelsSection()`, `IndexMonsters()`, `IndexDropTables()`, `IndexEquipmentSeries()`, `IndexEquipmentTemplates()`, `IndexEquipmentExchangeRecipes()`
   - 保留信号 `ConfigLoaded`
2. 新建 `scripts/services/ActiveLevelManager.cs`:
   - 依赖注入 `LevelConfigProvider`
   - 迁移关卡推进方法：`AdvanceToNextLevel()`, `TryAdvanceToNextUnlockedLevel()`, `TrySetActiveLevel()`, `TrySetActiveLevelIfUnlocked()`, `TrySetNextUnlockedLevelAsActive()`
   - 迁移关卡状态字段：`_activeLevelIndex`, `_unlockedLevelIds`, `_bossClearedLevelIds`, `_activeLevelMonsterWave`, `_activeLevelWaveIndex`, `_activeMoveInputsByCategory`
   - 迁移 Boss/解锁方法：`IsBossMonsterForLevel()`, `TryMarkBossDefeatedAndUnlockNext()`
   - 迁移运行时持久化：`ToRuntimeDictionary()`, `FromRuntimeDictionary()`
   - 迁移公有属性：`ActiveLevelId`, `ActiveLevelName`, `ProgressPer100Inputs`, `EncounterCheckIntervalProgress`, `BaseEncounterRate`, `BattlePauseFactor` 等
3. 新建 `scripts/services/ConfigValidationService.cs`:
   - 迁移校验方法：`ValidateConfiguration()`, `ValidateLevelSpawnTable()`, `ValidateMonsters()`, `ValidateDropTables()`, `ValidateEquipmentConfiguration()`
   - 迁移模拟方法：`RunBattleSimulation()`, `RunBattleSimulationFiltered()`, `RunBattleSimulationCore()`
   - 迁移字段：`_validationIssues`, `_validationEntries`, `_lastSimulationReport`
4. 保留 `LevelConfigLoader.cs` 作为兼容入口或直接删除，所有外部引用指向新类。
5. 在 `project.godot` 中将 autoload 更新为新节点名（或保留原名但内部委托）。
6. 更新 `PrototypeRootController._Ready()`, `ExploreProgressController._Ready()`, `BookTabsController` 中所有 GetNode 调用。
7. 确保所有现有测试通过（特别是 `CoreRegressionRulesTests`, `ConfigValidationViewFormatterTests`）。

**验收标准**:
- `LevelConfigLoader.cs` 不超过 200 行（或已删除）
- 三个新类职责单一，无交叉依赖
- `dotnet test` 全部通过
- 游戏启动后关卡加载、切换、战斗、校验功能正常

**完成说明**:
- 当前实现保留 `LevelConfigLoader` 作为兼容门面，`project.godot` 与现有 `/root/LevelConfigLoader` 调用点无需同步改名
- 除规划中的 `LevelConfigProvider` / `ConfigValidationService` / `ActiveLevelManager` 外，额外拆出了 `MonsterConfigService` 与 `LevelRuntimeStateService`，进一步隔离怪物查询与运行时结算职责

---

### TASK-02: 拆分 ExploreProgressController
**状态**: 已完成（2026-03-25）
**依赖**: TASK-01（引用 LevelConfigLoader 的部分需先稳定）
**背景**: `scripts/game/ExploreProgressController.cs` 共 1558 行、27 个 Export、89 个私有字段，混合了探索逻辑、战斗逻辑、怪物队列 UI、战斗轨道 UI、调试面板、持久化。

**涉及文件**:
- `scripts/game/ExploreProgressController.cs` — 拆分源
- `scripts/game/ExploreProgressController.Runtime.cs` — Godot 胶水层与状态同步
- `scripts/game/ExploreGameLogic.cs` — 纯探索/战斗逻辑
- `scripts/ui/BattleTrackVisualizer.cs` — 怪物队列与战斗轨道 UI
- `tests/Xiuxian2.Tests/ExploreGameLogicTests.cs` — 新增逻辑测试
- `scenes/PrototypeRoot.tscn` — 节点结构可能需调整
- `scripts/game/PrototypeRootController.cs` — 引用 `ExploreProgressController`

**步骤**:
1. 新建 `scripts/game/ExploreGameLogic.cs`（纯 C# 类，不继承 Node）:
   - 迁移探索状态字段：`_exploreProgress`, `_currentZone`, `_moveFrameCounter`, `_queueMoveInputPending`
   - 迁移战斗状态字段：`_inBattle`, `_battleRoundCounter`, `_pendingBattleInputEvents`, `_battleMonsterId`, `_battleMonsterName`, HP 系列字段
   - 迁移业务方法：`AdvanceExploreByInput()`, `TryStartBattle()`, `ConfigureBattleMonster()`, `CompleteBattle()`, `HandleBattleDefeat()`, `ApplyBattleRewards()`, `ApplyLevelCompletionRewards()`, `AdvanceBattleByInput()`
   - 迁移持久化：`ToRuntimeDictionary()`, `FromRuntimeDictionary()`
   - 对外暴露状态查询属性（ExploreProgress, InBattle, PlayerHp, EnemyHp 等）和事件回调（BattleStarted, BattleEnded, ProgressChanged, LevelCompleted）
2. 新建 `scripts/ui/BattleTrackVisualizer.cs`（Node 脚本）:
   - 迁移所有战斗轨道 UI 字段：`_battleInfoLabel`, `_roundInfoLabel`, `_playerHpLabel`, `_enemyHpLabel`, `_playerMarker`, `_enemySlotTexture` 等
   - 迁移怪物队列字段：`_monsterMarkers`, `_monsterMarkerIds`, `_monsterSlots`, `_monsterMoveInputPending`, `_monsterMoveInputThreshold`
   - 迁移 UI 方法：`UpdateHpLabels()`, `RefreshActorSlots()`, `RefreshMonsterSlots()`, `ApplyEnemyVisualConfig()`, `ApplyMarkerVisual()`, `ResetTrackVisual()`, `CacheMonsterMarkers()`, `CacheMonsterSlots()`, `MoveMonsterQueueByInputs()`
   - 迁移动画字段：`_activeEnemyVisualMonsterId`, `_enemySlotAnimType/Speed/Amplitude/BasePosition`, `_enemyVisualTime`
3. `ExploreProgressController` 保留为薄胶水层:
   - 持有 `ExploreGameLogic` 和 `BattleTrackVisualizer` 引用
   - `OnInputBatchTick()` → 委托给 `ExploreGameLogic.ProcessInputBatch()`
   - 监听 `ExploreGameLogic` 事件 → 通知 `BattleTrackVisualizer` 更新

**验收标准**:
- `ExploreProgressController.cs` 不超过 300 行
- `ExploreGameLogic` 无 Godot UI 依赖（纯逻辑，可单元测试）
- 新增至少 3 个测试覆盖 `ExploreGameLogic`（探索推进、战斗开始触发、战败重置）
- 游戏运行时探索、战斗、怪物动画、进度条全部正常

**完成说明**:
- 当前 `ExploreProgressController.cs` 为薄入口文件，实际运行时胶水保留在 `ExploreProgressController.Runtime.cs`
- 探索/战斗结算状态已下沉到 `ExploreGameLogic`，战斗轨道、怪物队列与敌人表现已下沉到 `BattleTrackVisualizer`
- `ExploreGameLogicTests.cs` 已覆盖任务要求的 3 个核心场景

---

### TASK-03: 存档版本迁移框架
**状态**: 已完成（2026-03-24）
**依赖**: 无
**背景**: `PrototypeRootController` 中 `SaveSchemaVersion = 5`，`LoadUnifiedState()` 只检查版本号但无迁移逻辑。装备 `EquipmentInstanceData` 有 15 个字段，后续扩展会破坏旧存档。

**涉及文件**:
- `scripts/game/PrototypeRootController.cs` — `LoadUnifiedState()`, `SaveAllState()` 方法
- 新建 `scripts/services/SaveMigrationRules.cs`

**步骤**:
1. 新建 `scripts/services/SaveMigrationRules.cs`（纯静态类）:
   ```
   public static class SaveMigrationRules
   {
       public static int LatestVersion => 6;
       // 未来新增: MigrateV5ToV6(ConfigFile cfg), MigrateV6ToV7(ConfigFile cfg)...
       public static bool NeedsMigration(int savedVersion) => savedVersion < LatestVersion;
       public static void MigrateToLatest(ConfigFile cfg, int fromVersion);
   }
   ```
2. `MigrateToLatest()` 循环调用 `MigrateVxToVy()` 升级链，每步更新 `meta.version`。
3. 在 `PrototypeRootController.LoadUnifiedState()` 中，读取 version 后调用 `SaveMigrationRules.MigrateToLatest()` 再继续加载。
4. 新增测试 `SaveMigrationRulesTests.cs`：验证 `NeedsMigration(5)` 返回 true, `NeedsMigration(6)` 返回 false。
5. 在 `docs/design/` 中新增 `14_save_migration.md` 文档化迁移规约。

**验收标准**:
- 旧版本(5)存档可被自动升级到 v6
- `SaveMigrationRules.MigrateToLatest()` 对已是最新版本的存档无操作
- 新增测试通过
- 存档路径 `user://save_state.cfg` 读写正常

---

### TASK-04: 离线结算集成实现
**状态**: 已完成（2026-03-25）
**依赖**: 无（Rules 层已测试就绪）
**背景**: 4 个 Rules 类已完整实现并有测试覆盖（`OfflineSettlementRules`, `DungeonOfflineSettlementRules`, `DungeonOfflineProjectionRules`, `OfflineSummaryPresentationRules`）。`PrototypeRootController.ApplyOfflineSettlementIfNeeded()` 方法已存在但需验证完整调用链。

**涉及文件**:
- `scripts/game/PrototypeRootController.cs` — `ApplyOfflineSettlementIfNeeded()`, `BuildOfflineDungeonSettlement()`
- `scripts/game/ExploreProgressController.cs` — `ShowOfflineSummary(string title, string body)` 方法
- `scripts/services/OfflineSettlementRules.cs` — `EvaluateOfflineSeconds()`, `BuildCultivationOfflineSettlement()`
- `scripts/services/OfflineSummaryPresentationRules.cs` — `BuildTitle()`, `BuildBody()`

**步骤**:
1. 验证 `PrototypeRootController.ApplyOfflineSettlementIfNeeded()` 在 `LoadAllState()` 末尾被调用。
2. 验证离线时间获取：从存档中读取上次退出时间 (`_activityState` 的最后活跃时间戳)，与当前 `Time.GetUnixTimeFromSystem()` 求差。
3. 验证修炼路径完整：`BuildCultivationOfflineSettlement()` 结果应用到 `ResourceWalletState.AddLingqi/AddInsight/AddPetAffinity()` 和 `PlayerProgressState.AddRealmExp()`。
4. 验证副本路径完整：`BuildOfflineDungeonSettlement()` 结果的 `ItemDrops` 应用到 `BackpackState.AddItem()`；`ExploreProgressGain` 更新到 `_exploreProgressController`。
5. 验证 `ShowOfflineSummary()` 在 UI 中展示弹窗：检查该方法是否将标题和内容文本写入某个可见 Label 并设置 `_offlineSummaryVisible = true`。
6. 如上述任一环节缺失，补全实现。
7. 新增集成测试 `OfflineSettlementIntegrationTests.cs`:
   - 修炼模式离线 1 小时 → 验证 lingqi/insight 增量正确
   - 副本模式离线 2 小时 → 验证遭遇数、胜率加权、物品掉落
   - 离线 0 秒 → 无结算
   - 离线 48 小时（可疑）→ 验证 GuardMode 触发 25% 惩罚

**验收标准**:
- 关闭游戏 → 等待 30 秒 → 重启 → 出现离线结算弹窗
- 弹窗内容格式："灵气+X | 悟性+Y | ..."
- 结算后资源正确增加（与 `OfflineSettlementRules` 公式一致）
- 所有新增 + 现有测试通过

---

### TASK-05: 装备正式内容闭环（Phase A）
**状态**: 已完成（2026-03-25）
**依赖**: 无
**背景**: 当前装备系统仅有：初始装备(`EquipmentStarterLoadout`)、首通固定奖励(`FirstClearEquipmentRewardRules`)、手动装备。缺少完整的装备模板 JSON、普通/精英掉落生成、装备 UI 对比。设计文档 `docs/design/11_equipment_content_system.md` 定义了 Stage A 范围：3 槽位(武器/护甲/饰品)、4 品级(俗器/法器/灵器/宝器)。

**涉及文件**:
- `docs/design/12_equipment_sample_qi_refining.json` — 现有炼气阶段装备样本
- `scripts/services/EquipmentGenerationRules.cs` — `GenerateFromSpec()`, `PickWeightedEntry()`
- `scripts/services/EquipmentDropResolutionRules.cs` — 加权掉落选择
- `scripts/services/EquipmentDropInstanceGenerationRules.cs` — 实例化掉落
- `scripts/services/EquipmentPresentationRules.cs` — UI 展示文本
- `scripts/ui/BookTabsController.cs` — `BuildEquipmentOverviewText()`, `BuildBackpackOverviewText()`
- `scripts/services/BackpackEquipmentInstanceRules.cs` — `TryTakeBySlot()`, `StoreInstance()`

**步骤**:
1. 扩充 `12_equipment_sample_qi_refining.json`：确保每个槽位(weapon/armor/accessory)至少有 2 个模板，覆盖俗器和法器两个品级。
2. 验证 `EquipmentGenerationRules.GenerateFromSpec()` 能正确从 JSON 模板生成 `EquipmentInstanceData`（main_stat + sub_stats 按品级数量 0/1/1/2）。
3. 在 `BookTabsController.BuildEquipmentOverviewText()` 中：
   - 显示当前装备的属性面板（基础属性 + 装备加成 = 最终属性）
   - 对每个已装备项显示名称、品级、主属性、副属性
4. 在 `BookTabsController.BuildBackpackOverviewText()` 中：
   - 对每个背包内装备实例显示：名称、品级标签（调用 `EquipmentPresentationRules.BuildRarityLabel()`）、来源标签
   - 对比当前已装备项：调用 `EquipmentPresentationRules.BuildComparisonHint()` 显示强弱对比
5. 实现"一键装备"交互：在背包装备条目中添加操作入口，调用 `BackpackEquipmentInstanceRules.TryTakeBySlot()` → `EquippedItemsState.TryEquipReplacing()`，旧装备返回背包。
6. 新增测试 `EquipmentContentClosureTests.cs`：
   - 从模板生成装备 → 验证字段完整
   - 装备 → 卸装 → 验证背包/装备栏状态一致
   - 对比提示 → 验证文案正确

**验收标准**:
- 打开子菜单"装备情况"Tab → 可看到 3 个槽位(武器/护甲/饰品)当前装备及属性
- 打开子菜单"背包"Tab → 可看到装备列表，每项有品级标签和对比提示
- 点击背包中装备可替换当前槽位，旧装备返回背包
- 存档 → 重载 → 装备状态不丢失

---

### TASK-06: 场景文件 UTF-8 编码修复
**状态**: 代码修复完成，待 Godot 编辑器人工验收
**依赖**: 无
**背景**: `scenes/ui/MainBarWindow.tscn` 中包含中文乱码(mojibake)，如 "閳?" "娑?" "缁涘绶?"。项目问题复盘已记录此问题（BOM 触发 Godot 解析失败）。

**涉及文件**:
- `scenes/ui/MainBarWindow.tscn`
- `scenes/ui/SubmenuBookWindow.tscn`
- `scenes/PrototypeRoot.tscn`

**步骤**:
1. 用二进制编辑器检查三个 `.tscn` 文件首字节，确认无 `EF BB BF` (BOM)。
2. 搜索 `MainBarWindow.tscn` 中所有 `text = "..."` 行，将乱码文本替换为 `scripts/ui/UiText.cs` 中定义的对应常量值（中文）。
3. 确认所有中文字符串使用 UTF-8 编码正确显示。
4. 保存时确保编辑器设置为 "UTF-8 without BOM"。
5. 启动 Godot 编辑器，打开 `scenes/PrototypeRoot.tscn`，确认无 Parse Error。

**验收标准**:
- 三个 `.tscn` 文件无 BOM 前缀
- `MainBarWindow.tscn` 中所有 `text = "..."` 行的中文正确显示
- Godot 编辑器可正常打开主场景，无 Parse Error

---

## Phase 2: P1 — 体验与健壮性

### TASK-07: 测试补全 — 战斗数学与结算
**状态**: 已完成（2026-03-25）
**依赖**: TASK-02（`ExploreGameLogic` 拆出后更易测试）
**背景**: 现有 20 个测试文件覆盖 Rules 层，但缺少 `BattleRules` 核心战斗数学测试。

**涉及文件**:
- 新建 `tests/Xiuxian2.Tests/BattleRulesTests.cs`
- `scripts/services/BattleRules.cs` — `ConsumeBattleInputs()`, `CalculateAttackDamage()`, `CalculateScaledDamage()`, `ResolvePlayerVsMonsterRound()`, `DetermineBattleFlow()`

**步骤**:
1. 新建 `BattleRulesTests.cs`，遵循项目现有模式（xUnit `[Fact]`, 单断言）。
2. 编写测试用例：
   - `CalculateAttackDamage_AttackMinusDefense_MinimumOne`: attack=5, defense=8 → 1
   - `CalculateAttackDamage_NormalDamage`: attack=10, defense=3 → 7
   - `CalculateScaledDamage_DividerApplied`: attack=12, divider=4 → 3
   - `CalculateScaledDamage_MinDamageFloor`: attack=2, divider=10, min=1 → 1
   - `ResolvePlayerVsMonsterRound_PlayerWins`: player HP > 0, monster HP ≤ 0 → PlayerWon
   - `ResolvePlayerVsMonsterRound_MonsterWins`: player HP ≤ 0, monster HP > 0 → MonsterWon
   - `ResolvePlayerVsMonsterRound_DoubleKO`: both HP ≤ 0 → DoubleKnockout
   - `DetermineBattleFlow_VictoryAction`: round result PlayerWon → Victory flow action

**验收标准**:
- 新增 ≥ 8 个测试
- `dotnet test` 全部通过

---

### TASK-08: 测试补全 — 存档往返
**状态**: 已完成（2026-03-25）
**依赖**: TASK-03（迁移框架就绪后测试更有意义）
**背景**: 缺少存档 Save → Load 往返测试，无法保证格式变更不破坏。

**涉及文件**:
- 新建 `tests/Xiuxian2.Tests/SaveRoundTripTests.cs`
- 新建 `scripts/services/BackpackPersistenceRules.cs`
- 新建 `scripts/services/PlayerProgressPersistenceRules.cs`
- 新建 `scripts/services/EquippedItemsPersistenceRules.cs`
- 新建 `scripts/services/SaveValueConversionRules.cs`
- `scripts/services/BackpackState.cs` — `ToDictionary()`, `FromDictionary()`
- `scripts/services/ResourceWalletState.cs` — `ToDictionary()`, `FromDictionary()`
- `scripts/services/PlayerProgressState.cs` — `ToDictionary()`, `FromDictionary()`
- `scripts/services/EquippedItemsState.cs` — `ToDictionary()`, `FromDictionary()`
- `scripts/services/EquipmentInstanceCodec.cs` — `ToDictionary()`, `FromDictionary()`

**步骤**:
1. 对每个 State 类编写往返测试：构造状态 → `ToDictionary()` → `FromDictionary()` → 断言与原始一致。
2. 对 `EquipmentInstanceCodec` 编写：构造 `EquipmentInstanceData` → `ToDictionary()` → `FromDictionary()` → 断言 15 个字段全等。
3. 对 `EquipmentProfileCodec` 编写同上。

**验收标准**:
- 新增 ≥ 5 个往返测试
- 覆盖 BackpackState, ResourceWalletState, PlayerProgressState, EquipmentInstanceCodec, EquipmentProfileCodec

---

### TASK-09: 服务定位器替代硬编码路径
**状态**: 已完成（2026-03-26）
**依赖**: TASK-01（新服务类就位后）
**背景**: 项目中所有 Controller/Service 通过 `GetNodeOrNull<T>("/root/XxxState")` 硬编码字符串获取依赖，有 12 个 Autoload 节点。路径字符串分散在 `PrototypeRootController`, `ExploreProgressController`, `BookTabsController` 等多个文件中。

**涉及文件**:
- 新建 `scripts/services/ServiceLocator.cs`
- `project.godot` — 注册 ServiceLocator 为 autoload
- 所有使用 `GetNodeOrNull<>("/root/...")` 的文件

**步骤**:
1. 新建 `scripts/services/ServiceLocator.cs`，继承 Node：
   ```
   public partial class ServiceLocator : Node
   {
       public static ServiceLocator Instance { get; private set; }
       public InputActivityState InputActivity => GetNode<InputActivityState>("/root/InputActivityState");
       // 为每个 autoload 提供强类型属性（缓存在 _Ready 中）
   }
   ```
2. 在 `_Ready()` 中缓存所有 12 个服务引用。
3. 在 `project.godot` 中注册为第一个 autoload。
4. 逐步替换各文件中的 `GetNodeOrNull<>()` 调用为 `ServiceLocator.Instance.XxxState`。
5. 保留旧 Export NodePath 字段但标记 `[Obsolete]`，后续移除。

**验收标准**:
- 所有 `"/root/XxxState"` 硬编码路径集中到 `ServiceLocator` 一处
- 现有功能不变，所有测试通过
- 新增任何 Autoload 只需在 ServiceLocator 加一个属性

**完成说明**:
- 已新增 `scripts/services/ServiceLocator.cs`，集中缓存 `InputActivityState`、`BackpackState`、`LevelConfigLoader`、`CloudSaveSyncService` 等 autoload 引用
- `project.godot` 已将 `ServiceLocator` 注册为首个 autoload，保证其他全局节点可被统一解析
- `scripts/game/PrototypeRootController.cs` 与 `scripts/ui/BookTabsController.cs` 已移除分散的 `GetNodeOrNull<T>("/root/...")` 调用，统一改为 `ServiceLocator.Instance`
- 当前已完成核心调用点迁移，但仓库中仍有部分 `"/root/..."` 路径保留在其他旧调用点，后续如继续收口可再统一清理
- 已验证 `UiTextStatsTests` 与 `SaveRoundTripTests` 通过

---

### TASK-10: 统计概览 Tab 内容实现
**状态**: 已完成（2026-03-26）
**依赖**: 无
**背景**: `BookTabsController` 中 "StatsTab" 已有 `BuildStatsOverviewText()` 方法，但内容仅包含输入计数。设计文档提到应有更丰富的统计。

**涉及文件**:
- `scripts/ui/BookTabsController.cs` — `BuildStatsOverviewText()`
- `scripts/ui/UiText.cs` — 需新增统计相关文案常量

**步骤**:
1. 在 `BuildStatsOverviewText()` 中扩展以下统计项：
   - 累计输入量（键盘/鼠标/滚轮/移动距离 — 已有 `InputActivityState` 追踪）
   - 累计活跃时间（需在 `InputActivityState` 新增 `TotalActiveSeconds` 字段）
   - 当前境界及在当前境界的天数
   - 累计战斗次数和胜率（需在 `ExploreProgressController` 或 `ExploreGameLogic` 新增计数器）
   - 累计获得灵气/悟性总量（需在 `ResourceWalletState` 新增 `TotalEarnedLingqi` 等字段）
2. 在 `UiText.cs` 中新增对应的中文标签常量。
3. 确保持久化：新字段在 `ToDictionary()`/`FromDictionary()` 中包含。

**验收标准**:
- 打开子菜单"统计概览"Tab → 展示至少 6 项有意义的统计数据
- 重启后统计数据不丢失
- 存档版本号 +1（或在 TASK-03 迁移框架内处理）

**完成说明**:
- `scripts/ui/BookTabsController.cs` 已输出累计输入量、累计活跃时间、当前境界与停留天数、累计战斗与胜率、累计获得灵气/悟性/灵宠亲和/灵石
- `scripts/services/InputActivityState.cs` 已持久化 `total_active_seconds`
- `scripts/services/ResourceWalletState.cs` 已持久化 `TotalEarnedLingqi` / `TotalEarnedInsight` / `TotalEarnedPetAffinity` / `TotalEarnedSpiritStones`
- `scripts/game/ExploreProgressController.Runtime.cs` 已持久化 `total_battle_count` / `total_battle_win_count`
- `tests/Xiuxian2.Tests/UiTextStatsTests.cs` 已覆盖统计文本格式化

---

### TASK-11: 宠物亲密度最小闭环
**状态**: 延后至 V2（2026-03-26 决策）
**依赖**: 无
**背景**: `ResourceWalletState` 中 `pet_affinity` 字段持续累积（每 10 秒 AP * 0.03），但无消费和效果。`PlayerProgressState` 有 `petMood` 字段和 `GetMoodMultiplier()` 方法。

**V1 决策**:
- 当前版本不将 `pet_affinity` / `pet_mood` 扩展成独立主系统，完整成长闭环延后到 V2。
- `pet_affinity` 可继续作为低权重累计统计保留，但不在 V1 中扩展为等级、全局增益或主动培养闭环。
- `pet_mood` 不再作为 V1 重点体验方向，但当前仍保留在资源转化倍率中；后续如保留，应在 V2 与互动/羁绊玩法一起重构其定位。
- 因此本任务整体延期到 V2，当前不进入近期待办批次。

**涉及文件**:
- `scripts/services/ResourceWalletState.cs` — `PetAffinity` 字段
- `scripts/services/PlayerProgressState.cs` — `petMood`, `GetMoodMultiplier()`
- `scripts/ui/BookTabsController.cs` — 修炼概况页展示
- `scripts/ui/UiText.cs`
- 新建 `scripts/services/PetAffinityRules.cs`

**步骤**:
1. 新建 `scripts/services/PetAffinityRules.cs`（纯静态类）：
   - `int GetAffinityLevel(double totalAffinity)` — 亲密度等级(1-10)：阈值 = [0, 50, 150, 350, 700, 1200, 2000, 3500, 5500, 8000]
   - `double GetAffinityBonusMultiplier(int level)` — 全局加成：1.0 + (level - 1) * 0.02（最高 1.18）
   - `string GetAffinityLevelName(int level)` — 等级名称（"陌生", "认识", "熟悉", "信任", "亲近", "默契", "心有灵犀", "形影不离", "灵宠一体", "至臻之契"）
2. 在修炼概况页 (`BuildCultivationOverviewText()`) 展示当前亲密度等级和加成。
3. 在 `ActivityConversionService` 中将亲密度等级加成应用到资源转化公式。
4. 新增测试 `PetAffinityRulesTests.cs`。

**原 V2 目标验收标准**:
- 修炼概况页显示"灵宠亲和 Lv.X (名称) — 加成 +Y%"
- 亲密度随时间增长，等级提升反映到加成倍率
- 新增测试通过

---

### TASK-12: UI 自适应布局修复
**状态**: 已完成（2026-03-26）
**依赖**: 无
**背景**: `scenes/ui/MainBarWindow.tscn` 中布局使用硬编码绝对偏移（`offset_left = 1040.0` 等），`MainBarLayoutController.cs` 中有大量 magic number（`textRowY = controlRowY + 34.0f`）。

**涉及文件**:
- `scenes/ui/MainBarWindow.tscn` — 修改 anchor/margin
- `scripts/ui/MainBarLayoutController.cs` — `ApplyLayout()`, `UpdateRightAnchoredLayout()`

**步骤**:
1. 在 `.tscn` 中将关键控件的锚点改为相对锚点（anchor_left/right/top/bottom），避免绝对偏移。
2. 对 ResizeHandle 使用 `anchor_right = 1.0` + `offset_left = -60`（相对右侧）而非绝对 `1040.0`。
3. 在 `MainBarLayoutController.ApplyLayout()` 中将 magic number 提取为命名常量。
4. 测试分辨率：在 Godot 的 Project Settings → Display → Window 中切换 1280×720、1920×1080、2560×1440 验证。
5. 测试 DPI 缩放：在 `BookTabsController` 的 `ui_scale` 设置项中切换 1.0 / 1.25 / 1.5 验证。

**验收标准**:
- 最小宽度(800px)下核心信息可见，无控件重叠
- 最大宽度(2560px)下布局合理延展
- DPI 1.5x 下文字不超出容器

**完成说明**:
- `scripts/ui/MainBarLayoutController.cs` 已提取关键布局常量（边距、间距、最小/默认宽度、行间距），替代散落 magic number
- `UpdateRightAnchoredLayout()` 已改为基于当前容器宽度动态分配 `BattleTrack`、`ExploreProgressBar`、`CultivationProgressBar`、模式按钮与副本按钮宽度
- `MinWidth` 默认值已提升到 `800`，与任务验收口径对齐
- `scenes/ui/MainBarWindow.tscn` 中 `ResizeHandleButton` 已改为右锚定 + 负偏移，避免依赖固定像素右侧位置
- 已执行 `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj`，当前 163 个测试全部通过

---

### TASK-13: 离线结算与每日上限一致性
**状态**: 已完成（2026-03-26）
**依赖**: TASK-04
**背景**: 掉落表有 `daily_cap`(120 rolls) 和 `hourly_soft_cap`(20 rolls)。`DungeonOfflineSettlementRules.BuildDungeonOfflineSettlement()` 中 `equipmentDropCap: 2` 是局部硬编码，但未接入 `LevelDropEconomyRules` 的全局上限体系。

**涉及文件**:
- `scripts/services/DungeonOfflineSettlementRules.cs`
- `scripts/services/LevelDropEconomyRules.cs` — `ConsumeDropRoll()`, `ShouldSkipDropBySoftCap()`

**步骤**:
1. 在 `DungeonOfflineSettlementRules.BuildDungeonOfflineSettlement()` 中，增加参数 `int remainingDailyRolls`，将掉落数与剩余配额取 min。
2. `PrototypeRootController.BuildOfflineDungeonSettlement()` 中从 `LevelConfigLoader` 读取当日已消耗 roll 次数，传入。
3. 如跨日（离线前为昨天，恢复时为今天），重置配额。
4. 新增测试：离线满上限场景 → 掉落被截断到 daily_cap 剩余量。

**验收标准**:
- 离线结算不会导致掉落超出日上限
- 跨日离线正确重置配额
- 新增测试通过

**完成说明**:
- `scripts/services/DungeonOfflineSettlementRules.cs` 已新增 `remainingDailyRolls` / `averageDropRollsPerVictory` 参数，并按剩余配额对离线材料/装备掉落做缩放
- `scripts/services/OfflineProjectionService.cs` 已新增离线平均掉落 roll 估算，用于计算配额缩放比例
- `scripts/services/LevelRuntimeStateService.cs` 已支持按关卡聚合当前剩余 daily rolls，并在 `savedDay != currentDay` 时将已用计数视为重置
- `scripts/game/PrototypeRootController.cs` 已在离线副本结算中传入目标关卡的剩余日配额与平均 roll 数据
- `tests/Xiuxian2.Tests/DungeonOfflineSettlementRulesTests.cs` 已覆盖“离线掉落被剩余日配额截断”场景

---

## Phase 3: P2 — 打磨与长期健康

### TASK-14: 消除魔法数字
**状态**: 已完成（2026-03-26）
**依赖**: TASK-01, TASK-02
**背景**: 业务逻辑中散布硬编码常量。

**涉及文件**:
- `scripts/services/InputActivityRules.cs` — AP baseline 6.0, decay threshold 1.0, decay rate 0.25, floor 0.45
- `scripts/services/ActivityConversionService.cs` — lingqi factor 0.9, insight 0.08, pet_affinity 0.03
- `scripts/services/EquipmentGenerationRules.cs` — sub-stat counts [0,1,1,2] by rarity
- `scripts/game/PrototypeRootController.cs` — `ApplyOfflineSettlementIfNeeded()` 中 apPerInput/lingqiFactor 等
- `scripts/game/ExploreProgressController.cs` — `ProgressPerInput = 0.02f`, battle params

**步骤**:
1. 新建 `scripts/services/GameBalanceConstants.cs`（静态常量类）。
2. 将上述所有裸数字迁移到该类中，分 region（`InputDecay`, `ResourceConversion`, `EquipmentGeneration`, `Offline`）。
3. 替换所有引用点。
4. 确保所有现有测试通过。

**验收标准**:
- 全项目搜索 `0.9` / `0.08` / `0.03` / `6.0` 无业务逻辑中的裸数字
- 所有常量集中在 `GameBalanceConstants.cs`

**完成说明**:
- 已新增 `scripts/services/GameBalanceConstants.cs`，集中定义 `InputDecay`、`ResourceConversion`、`EquipmentGeneration`、`Offline`、`Explore` 五组核心数值
- `scripts/services/InputActivityState.cs` 已改为从 `GameBalanceConstants.InputDecay` 读取 AP baseline、decay threshold、decay rate、floor
- `scripts/services/ActivityConversionService.cs` 与 `scripts/game/PrototypeRootController.cs` 已改为复用 `GameBalanceConstants.ResourceConversion` / `Offline` 中的资源转化与离线修炼参数
- `scripts/services/EquipmentGenerationRules.cs` 已改为复用 rarity 对应副词条数量常量
- `scripts/game/ExploreProgressController.Runtime.cs`、`scripts/game/ExploreGameLogic.cs`、`scripts/services/MonsterConfigService.cs`、`scripts/ui/BattleTrackVisualizer.cs` 已统一复用探索默认参数常量
- 已执行 `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj`，当前 171 个测试全部通过

---

### TASK-15: 隐藏未实现的设置项
**状态**: 已完成（2026-03-24，按“直接隐藏”方案执行）
**依赖**: 无
**背景**: `BookTabsController` 定义的 17 个设置项中，`taskbar_icon`, `startup_animation`, `admin_mode`, `handwriting_support`, `show_control_markers`, `milestone_tips` 均为 stub。

**涉及文件**:
- `scripts/ui/BookTabsController.cs` — 设置页构建逻辑

**步骤**:
1. 在设置页构建逻辑中，对上述 6 个 stub 设置项：
   - 选项一：从 UI 中隐藏（不显示）
   - 选项二：保留但附加 "(即将推出)" 标签并禁用交互
2. 推荐选项一（隐藏），减少信息噪音。

**验收标准**:
- 打开设置页 → 不出现未实现的设置项（或标注"即将推出"且禁用）
- 功能性设置项（vsync, max_fps, language, ui_scale 等）正常可用

---

### TASK-16: Steam Cloud 接口抽象
**状态**: 已完成（2026-03-26）
**依赖**: 无
**背景**: `scripts/services/CloudSaveSyncService.cs` 通过反射动态调用 Steamworks API，难以测试和维护。

**涉及文件**:
- `scripts/services/CloudSaveSyncService.cs`
- 新建 `scripts/services/ISaveCloudProvider.cs`
- 新建 `scripts/services/NullCloudProvider.cs`

**步骤**:
1. 新建接口 `ISaveCloudProvider`:
   ```
   public interface ISaveCloudProvider
   {
       bool IsAvailable { get; }
       bool TryUpload(string localPath);
       bool TryDownload(string localPath);
   }
   ```
2. 将 `CloudSaveSyncService` 内部的反射逻辑封装到 `SteamCloudProvider : ISaveCloudProvider`。
3. 新建 `NullCloudProvider : ISaveCloudProvider`（所有方法返回 false），用于非 Steam 环境。
4. `CloudSaveSyncService` 改为使用工厂模式选择 Provider。
5. 在 `PrototypeRootController` 中通过接口调用，不再直接依赖 Steam 反射。

**验收标准**:
- 无 Steam SDK 环境下游戏正常运行（使用 NullCloudProvider）
- 有 Steam SDK 时自动选择 SteamCloudProvider
- 接口可用于未来测试 mock

**完成说明**:
- 已新增 `scripts/services/ISaveCloudProvider.cs`，统一暴露 `IsAvailable` / `TryUpload()` / `TryDownload()`
- 已新增 `scripts/services/NullCloudProvider.cs`，在非 Steam 环境下稳定返回不可用
- 已新增 `scripts/services/SteamCloudProvider.cs`，将原 `CloudSaveSyncService` 内嵌的 Steam 反射逻辑抽离为独立 provider
- `scripts/services/CloudSaveSyncService.cs` 已改为通过 `CreateProvider()` 选择 provider，并只负责本地路径与日志封装
- 已执行 `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj`，当前 163 个测试全部通过

---

### TASK-17: 全局反挂机规则
**状态**: 已完成（2026-03-26）
**依赖**: 无
**背景**: 个别怪物有 `anti_afk_rule`，但无全局"零输入 → 暂停进度"机制。

**涉及文件**:
- `scripts/services/InputActivityState.cs` — 需追踪零输入持续时间
- `scripts/game/ExploreProgressController.cs` (或 TASK-02 后的 `ExploreGameLogic.cs`) — 暂停探索推进
- 新建 `scripts/services/AfkDetectionRules.cs`

**步骤**:
1. 新建 `AfkDetectionRules.cs`（纯静态类）：
   - `bool IsAfk(double secondsSinceLastInput, double threshold = 120.0)` — 超过 2 分钟无输入视为 AFK
   - `double GetProgressMultiplier(double secondsSinceLastInput)` — 0-60s: 1.0, 60-120s: 0.5, >120s: 0.0
2. 在 `InputActivityState` 中新增 `SecondsSinceLastInput` 属性（基于 `_Process` 中的 delta 累加，每次 RegisterXxx 重置）。
3. 在探索推进逻辑中检查 `AfkDetectionRules.GetProgressMultiplier()` 并应用到 `explore_progress_gain`。
4. 新增测试 `AfkDetectionRulesTests.cs`。

**验收标准**:
- 无输入 2 分钟后探索进度不再推进
- 恢复输入后进度立即恢复
- 新增测试通过

**完成说明**:
- 已新增 `scripts/services/AfkDetectionRules.cs`，实现 `IsAfk()` 与 `GetProgressMultiplier()`：`0-60s = 1.0`、`60-120s = 0.5`、`>=120s = 0.0`
- `scripts/services/InputActivityState.cs` 已新增 `SecondsSinceLastInput` 与 `SecondsSinceLastInputBeforeLatestBatch`，可区分当前空闲时长与“本批输入发生前”的空闲时长
- 所有 `RegisterXxx()` 输入入口现在都会重置空闲计时，并保留本批输入前的 idle snapshot
- `scripts/game/ExploreProgressController.Runtime.cs` 已在探索推进时应用 AFK 倍率：`0.5x` 时半速推进，`0x` 时暂停副本推进并显示 `AFK 暂停` 提示
- 已新增 `tests/Xiuxian2.Tests/AfkDetectionRulesTests.cs`
- 已执行 `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj`，当前 169 个测试全部通过

---

### TASK-18: 遭遇率境界缩放
**状态**: 已完成（2026-03-26）
**依赖**: 无
**背景**: 当前遭遇率 `base_rate = 18% + danger_level * 4%`，不考虑玩家境界与关卡推荐境界的差异。

**涉及文件**:
- `scripts/services/BattleStartRules.cs` — `DetermineEncounterStart()`
- `scripts/game/ExploreProgressRules.cs`

**步骤**:
1. 在 `BattleStartRules.DetermineEncounterStart()` 中新增参数 `int playerRealmLevel, int zoneDangerLevel`。
2. 计算境界差：`levelDiff = playerRealmLevel - zoneDangerLevel`。
3. 应用缩放：`encounter_rate = base_rate * (1.0 + levelDiff * 0.05)`，clamp 到 [0.05, 0.95]。
4. 高境界打低图遭遇率升高（更快清图），低境界进高图遭遇率降低（更安全探索）。
5. 新增测试。

**验收标准**:
- 境界 3 打危险度 1 的图 → 遭遇率高于基准
- 境界 1 打危险度 3 的图 → 遭遇率低于基准
- 基准（境界=危险度）→ 遭遇率不变

**完成说明**:
- `scripts/services/BattleStartRules.cs` 已新增 `CalculateEncounterRate()`，按 `base_rate * (1 + levelDiff * 0.05)` 计算并 clamp 到 `[0.05, 0.95]`
- `scripts/services/ActiveLevelManager.cs` 已在运行时读取当前关卡 `danger_level`
- `scripts/services/LevelConfigLoader.cs` 已暴露 `ActiveLevelDangerLevel`
- `scripts/game/ExploreGameLogic.cs` / `scripts/game/ExploreProgressController.Runtime.cs` 已在实际遇怪判定中传入 `baseEncounterRate`、玩家境界、关卡危险度与随机 roll
- `tests/Xiuxian2.Tests/CoreRegressionRulesTests.cs` 已新增遭遇率缩放与缩放后判定测试
- 已执行 `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj`，当前 171 个测试全部通过

---

## 任务依赖图

```
TASK-06 (UTF-8)           ─── 独立（待人工验收）
TASK-03 (存档迁移)        ─── ✅ 已完成
TASK-04 (离线结算)        ─── ✅ 已完成
TASK-05 (装备闭环)        ─── ✅ 已完成
TASK-01 (拆 ConfigLoader) ─── ✅ 已完成
  └→ TASK-02 (拆 ExploreCtrl) ─── ✅ 已完成
      └→ TASK-07 (战斗测试) ─── ✅ 已完成
  └→ TASK-09 (服务定位器) ─── ✅ 已完成
  └→ TASK-14 (魔法数字)   ─── ✅ 已完成
TASK-03 → TASK-08 (存档往返测试) ─── ✅ 已完成
TASK-04 → TASK-13 (离线×上限) ─── ✅ 已完成
TASK-10 (统计概览)        ─── ✅ 已完成
TASK-11 (宠物亲密度)      ─── 延后至 V2
TASK-12 (UI 自适应)       ─── ✅ 已完成
TASK-15 (隐藏设置)        ─── ✅ 已完成
TASK-16 (Cloud 抽象)      ─── ✅ 已完成
TASK-17 (反挂机)          ─── ✅ 已完成
TASK-18 (遭遇率缩放)      ─── ✅ 已完成
```

## 推荐执行顺序

**批次 1**: TASK-06（待验收）
**批次 2**: TASK-11（延后至 V2，不进入当前版本执行）

---

<!-- MELVOR-CHANGE: 新增 Phase 4，包含 Melvor Idle 启发的系统扩展任务 -->
## Phase 4: Melvor-Inspired 系统扩展

### TASK-19: 活动模式扩展 — 从 2 模式到 4 模式
**状态**: 已完成（2026-03-25）
**依赖**: 无（可独立实施，但建议在 TASK-01/02 之后以获得更好架构）
**背景**: 当前 `PlayerActionState` 仅支持 `ModeDungeon` / `ModeCultivation` 两种硬编码模式，`ToggleMode()` 为二元切换。参考 Melvor Idle 多活动设计，需扩展为 4 模式。

**涉及文件**:
- `scripts/services/PlayerActionState.cs` — 新增 `ModeAlchemy`, `ModeSmithing` 常量
- `scripts/services/PlayerActionCapabilityRules.cs` — 新增 alchemy/smithing 的能力集
- `scripts/game/ExploreProgressController.cs` — `ConfigureActionModeOptionButton()` 支持 4 项, `OnActionModeOptionSelected()` 改为 switch
- `scripts/services/PlayerActionCapability.cs` — 新增 `AdvancesAlchemy`, `AdvancesSmithing` 枚举值

**步骤**:
1. 在 `PlayerActionState` 中新增 `ModeAlchemy = "alchemy"`, `ModeSmithing = "smithing"` 常量。
2. 将 `ToggleMode()` 替换为 `SetMode(string mode)`，接受 4 个合法值。
3. 在 `PlayerActionCapabilityRules` 的 switch 中新增 alchemy 和 smithing 分支。
4. 在 `ExploreProgressController.ConfigureActionModeOptionButton()` 中添加炼丹/炼器选项。
5. 在 `OnActionModeOptionSelected()` 中改为 4 模式 switch。
6. 存档字段 `player.action_mode` 支持 4 个值。

**验收标准**:
- 底栏下拉选择器显示 4 个模式：副本、修炼、炼丹、炼器
- 切换模式后 AP 结算不中断，仅改变进度类型
- 存档 → 重载后模式不丢失

---

### TASK-20: 炼丹系统实现
**状态**: 已完成（2026-03-25）
**依赖**: TASK-19（需要 alchemy 模式可切换）
**背景**: 当前副本掉落的灵草类材料无消费出口。参考 Melvor Idle Herblore，实现配方驱动的炼丹系统。

**涉及文件**:
- 新建 `scripts/services/AlchemyRules.cs` — 配方校验、材料消耗、产出计算（纯静态）
- 新建 `scripts/services/AlchemyState.cs` — 当前配方、进度
- 新建 `scripts/services/PotionInventoryState.cs` — 丹药背包状态
- `scripts/services/BackpackState.cs` — 新增丹药存储区
- `scripts/ui/BookTabsController.cs` — 新增"炼丹"子页或在修炼概况中展示

**步骤**:
1. 新建 `AlchemyRules.cs`：
   - `bool CanStartRecipe(string recipeId, ResourceWalletState wallet, BackpackState backpack)` — 校验材料
   - `AlchemyResult CompleteRecipe(string recipeId)` — 返回产出丹药 ID 和数量
   - 静态配方表：`回气丹`（`spirit_herb`×2 + 灵气×50 → 回气丹×2）、`聚灵散`（`spirit_herb`×3 + 灵气×80 → 聚灵散×1）
2. 新建 `AlchemyState.cs`（继承 Node，Autoload）：
   - `SelectedRecipeId`, `CurrentProgress`, `RequiredProgress`
   - `AdvanceProgress(int inputEvents)` — 推进炼丹进度
3. 新建 `PotionInventoryState.cs`：
   - `Dictionary<string, int>` 丹药堆叠
   - `ToDictionary()` / `FromDictionary()` 支持存档
4. 在 `ExploreProgressController` 中 alchemy 模式 `_Process` 路径调用 `AlchemyState.AdvanceProgress()`。
5. 新增测试 `AlchemyRulesTests.cs`。

**验收标准**:
- 切换到炼丹模式 → 键鼠输入推进炼丹进度条
- 材料不足时显示提示，不开始炼丹
- 完成后丹药进入背包，数量正确
- 存档 → 重载后炼丹状态和丹药库存不丢失

---

### TASK-21: 炼器 / 强化系统实现
**状态**: 已完成（2026-03-25）
**依赖**: TASK-19（需要 smithing 模式可切换）
**背景**: `EquipmentInstanceData.EnhanceLevel` 字段已存在但未消费。参考 Melvor Idle Smithing，实现装备强化系统。

**涉及文件**:
- 新建 `scripts/services/SmithingRules.cs` — 强化校验、材料消耗、属性计算（纯静态）
- 新建 `scripts/services/SmithingState.cs` — 当前强化目标、进度
- `scripts/services/EquipmentStatProfile.cs` — 新增 `GetEnhancedValue(double baseValue, int enhanceLevel)`
- 战斗属性管线 — 应用强化倍率

**步骤**:
1. 新建 `SmithingRules.cs`：
   - `int GetMaxEnhanceLevel(int rarityTier)` → `rarityTier * 3`
   - `bool CanEnhance(EquipmentInstanceData equipment, BackpackState backpack, ResourceWalletState wallet)`
   - `SmithingCost GetCost(int currentLevel)` — 返回碎片/碎符/灵气消耗
   - `double GetEnhanceMultiplier(int enhanceLevel)` → `Math.Pow(1.08, enhanceLevel)`
2. 新建 `SmithingState.cs`（继承 Node，Autoload）：
   - `TargetEquipmentId`, `CurrentProgress`, `RequiredProgress`
3. 在 `EquipmentStatProfile` 中新增方法，战斗属性计算时应用强化倍率。
4. 新增测试 `SmithingRulesTests.cs`。

**验收标准**:
- 切换到炼器模式 → 选择装备 → 键鼠输入推进强化进度
- 强化完成后 `EnhanceLevel + 1`，战斗属性立即生效
- 达到上限后无法继续强化
- 存档 → 重载后强化等级不丢失

---

### TASK-22: Boss 挑战系统
**状态**: 已完成（2026-03-25）
**依赖**: 无
**背景**: 当前区域探索 100% 后直接切换下一区域，缺乏里程碑感。参考 Melvor 但采用进度+Boss 模型。

**涉及文件**:
- `scripts/services/BattleLifecycleRules.cs` — 新增 Boss 相关决策
- `scripts/game/ExploreProgressController.cs` — 区域完成后进入 Boss 而非直接切区
- 新建 `scripts/services/BossEncounterRules.cs`
- `scripts/services/LevelConfigLoader.cs` — Boss 配置加载

**步骤**:
1. 新建 `BossEncounterRules.cs`（纯静态）：
   - `MonsterData GetBossForZone(string zoneId)` — 返回 Boss 数据（精英怪属性 × multiplier）
   - `bool IsBossDefeated(string zoneId, List<string> defeatedBossZones)`
   - `BattleOutcome ResolveBossTimeout(int currentRound, int maxRounds)` — 超时判负
2. 在 `ExploreProgressController` 中，区域 100% 后检查 Boss 是否已击败：
   - 未击败 → 进入 `BossChallenge` 状态
   - 已击败 → 直接切下一区域
3. Boss 掉落：首次击杀保底灵器，重复击杀使用普通 Boss 掉落表。
4. 新增测试 `BossEncounterRulesTests.cs`。

**验收标准**:
- 区域 100% 后出现 Boss 挑战而非立即切区
- Boss 属性 = 精英怪 × 2-3 倍
- 首次击杀 Boss 获得灵器级掉落
- 失败后探索进度归零，但可重新推进并再次挑战 Boss
- 击败后解锁下一区域

---

### TASK-23: 战斗消耗品（丹药自动使用）
**状态**: 已完成（2026-03-25）
**依赖**: TASK-20（需要丹药系统就绪）
**背景**: 丹药需在战斗中自动消耗以完成资源循环闭环。

**涉及文件**:
- 新建 `scripts/services/ConsumableUsageRules.cs` — 丹药触发判定（纯静态）
- `scripts/services/BattleRules.cs` — 在回合结算中调用消耗品逻辑
- `scripts/services/BattleRoundResult.cs` — 新增 `consumed_potions` 字段
- `scripts/services/PotionInventoryState.cs` — 消耗丹药

**步骤**:
1. 新建 `ConsumableUsageRules.cs`：
   - `List<PotionUsage> DetermineAutoConsume(BattleState state, PotionInventoryState potions)` — 根据 HP 阈值等条件返回应消耗的丹药列表
   - 回气丹触发条件：HP < 50%，每场最多 1 次
   - 聚灵散触发条件：战斗开始时检查，有则消耗
2. 在 `BattleRules` 回合结算流程中调用 `ConsumableUsageRules`。
3. 消耗记录写入 `BattleRoundResult.consumed_potions` 和战斗日志。
4. 新增测试 `ConsumableUsageRulesTests.cs`。

**验收标准**:
- 持有回气丹 + HP < 50% 时自动消耗并恢复 HP
- 持有聚灵散时战斗开始自动消耗，掉落加成生效
- 丹药消耗后库存正确减少
- 战斗日志显示丹药消耗记录

---

## 更新后的任务依赖图

```
（原有依赖关系不变）

TASK-19 (4 模式扩展)     ─── ✅ 已完成
  └→ TASK-20 (炼丹系统)    ─── ✅ 已完成
      └→ TASK-23 (战斗消耗品)  ─── ✅ 已完成
  └→ TASK-21 (炼器/强化)    ─── ✅ 已完成
TASK-22 (Boss 挑战)      ─── ✅ 已完成
```

## 更新后的推荐执行顺序

**批次 1-5**: 已完成，当前仅剩 `TASK-06` 人工验收与 `TASK-11` V2 延后项需要单独跟踪
**批次 6（Melvor 扩展）**: TASK-19 ✅, TASK-22 ✅
**批次 7（依赖 TASK-19）**: TASK-20 ✅, TASK-21 ✅
**批次 8（依赖 TASK-20）**: TASK-23 ✅
**批次 9（审计修复）**: TASK-24 ✅, TASK-25 ✅, TASK-26 ✅, TASK-28, TASK-29 ✅

---

<!-- REVIEW-FIX: 以下为文档审计后新增实施任务 -->
## Phase 5: Review-Fix — 文档审计修复实施

### TASK-24: 灵石经济接入代码 (P1)
**状态**: 已完成（2026-03-25）
**依赖**: TASK-19
**背景**: `ResourceWalletState` 中缺少 `SpiritStones` 字段，但设计已定义灵石产出/消耗闭环（见 `02_systems.md` §11）。
**涉及文件**:
- `scripts/services/ResourceWalletState.cs` — 新增 `SpiritStones` 字段
- `scripts/services/RewardRules.cs` — Boss/精英怪战斗产出灵石
- `PrototypeRootController.cs` — 存档读写 `wallet.spirit_stones`
**验收标准**:
- 灵石在钱包中可读写、可存档、可恢复。
- Boss 首杀奖励包含灵石。

### TASK-25: 战斗失败规则 + 副本循环实施 (P1)
**状态**: 已完成（2026-03-25）
**依赖**: 无
**背景**: `BattleDefeatDecision` 已有 `ShouldResetExploreProgress` 和 `ShouldResetLevel` 字段。副本采用循环刷取模型：Boss 胜利/失败后进度均归零，普通/精英怪失败不归零（见 `06_bottom_exploration_battle.md` §3a + §4）。
**涉及文件**:
- `scripts/services/BattleDefeatDecision.cs` — Boss 失败时 `ShouldResetExploreProgress = true`
- `scripts/services/BattleLifecycleRules.cs` — 在失败分支显式赋值
- `scripts/game/ExploreProgressController.cs` — Boss 胜利后进度归零 + `repeat_clear` 结算
- 新增区域选择器 UI（子菜单或底栏下拉）
**验收标准**:
- Boss 失败后进度归零，重新开始本区域循环。
- Boss 胜利后进度归零，结算 `repeat_clear` 奖励，继续下一循环。
- 玩家可在已解锁区域之间自由切换。
- 新增单元测试覆盖 3 种失败场景 + 循环归零场景。

### TASK-26: 悟性新消耗路径 (P1)
**状态**: 已废弃 → 被 TASK-30 系列替代（2026-03-26）
**原因**: 悟性消费模型重构 — 从「境界突破+Boss弱点+高阶炼丹」零散消耗改为「子系统熟练度解锁」统一消费体系。
**依赖**: TASK-20, TASK-22
**替代任务**: TASK-30（子系统熟练度规则层）、TASK-31（存档迁移 v6→v7）、TASK-32（境界突破去悟性化）、TASK-33（炼丹/炼器熟练度门控）、TASK-34（熟练度 UI）、TASK-35（测试补全）

### TASK-27: 突破丹已取消 — 无需实施
**状态**: 已取消（V1 决策：移除突破丹，掉落 slot 用于灵石/灵草，见 `02_systems.md` §3）。
- 突破成功后丹药被消耗。
- 无突破丹时行为不变。

### TASK-28: Boss 内容样本填充 (P2)
**依赖**: TASK-22
**背景**: Boss 模板字段已补充（`07_content_template.md` §3.5），但 `09_level_monster_drop_sample.json` 缺少 Boss 内容样本。
**涉及文件**:
- `docs/design/09_level_monster_drop_sample.json` — 新增 Boss 怪物条目
- `docs/design/09_level_monster_drop_sample.json` — 新增 Boss JSON 数据
**验收标准**:
- 练气期至少 1 个 Boss 样本含完整字段。
- JSON 可被 `LevelConfigLoader` 正常解析。

### TASK-29: 存档 v5→v6 迁移代码 (P1)
**状态**: 已完成（2026-03-25）
**依赖**: TASK-24
**背景**: 迁移规格已预写在 `14_save_migration.md`，需在 `SaveMigrationRules.cs` 中实现。
**涉及文件**:
- `scripts/services/SaveMigrationRules.cs` — 新增 `MigrateV5ToV6()` 方法
- `tests/Xiuxian2.Tests/SaveMigrationRulesTests.cs` — v5→v6 测试用例
**验收标准**:
- v5 存档加载后自动升级到 v6。
- 新字段 (`spirit_stones`, `alchemy.*`, `smithing.*`, `boss.*`, `backpack.potions`) 均有合理默认值。
- 现有 v5 测试不受影响。

---

## Phase 6: 悟性→子系统熟练度重构（INSIGHT-MASTERY-CHANGE）

> 设计变更日期：2026-03-26
> 核心变更：境界突破改为纯经验驱动，悟性完全转为子系统熟练度解锁货币。
> 参考设计：`02_systems.md` §12、`03_progression_and_balance.md` 悟性消费设计。

### TASK-30: 子系统熟练度规则层 (P0)
**状态**: 未开始
**优先级**: P0（其他熟练度相关任务依赖此项）
**依赖**: 无（但 scope 已扩展至 12 系统，需在 TASK-36 之后最终化）
**背景**: 实现 `SubsystemMasteryRules.cs`（纯静态规则）和 `SubsystemMasteryState.cs`（Godot Node 持久化），定义 **12** 个子系统各 4 级熟练度的解锁成本、境界门槛、效果查询。

**涉及文件**:
- `scripts/services/SubsystemMasteryRules.cs` — **新建**，纯静态：各系统等级定义、解锁成本、效果查询、境界门槛校验
- `scripts/services/SubsystemMasteryState.cs` — **新建**，Godot Node，持久化 `mastery.{systemId}_level`（12 个系统字段）
- `scripts/services/InsightSpendRules.cs` — 重构：移除 `BossWeakness*` / `AdvancedAlchemy*` 常量，新增 `CanUnlockMastery()` / `GetMasteryCost()` / `SpendInsightForMastery()`
- `project.godot` — 注册 `SubsystemMasteryState` 为 autoload（或通过 ServiceLocator）

**步骤**:
1. 定义 `MasteryDefinition` record：`(string SystemId, int Level, double InsightCost, int RequiredRealmLevel, string EffectId, string DisplayName)`
2. 硬编码 12 系统 × 4 级 = 48 条 `MasteryDefinition`（按 `02_systems.md` §12.2 表格 + 新增系统 §13-§20 的精通树）
3. 实现 `GetCurrentLevel()` / `CanUnlock()` / `TryUnlock()` / `GetEffectValue()` 接口
4. `SubsystemMasteryState` 提供 `ToDictionary()` / `FromDictionary()` 用于存档读写
5. 重构 `InsightSpendRules.cs`：移除旧消费逻辑，统一为 mastery 解锁入口
6. 使用 `IReadOnlyList<MasteryDefinition>` 注册表，后续新增系统只需追加定义行

**验收标准**:
- `SubsystemMasteryRules` 可查询任意 12 个系统的当前等级、下一级成本、是否可解锁
- `InsightSpendRules` 不再包含 Boss 弱点 / 高阶炼丹 相关常量
- 总解锁成本 = ~2080 悟性（与 `03_progression_and_balance.md` 一致）
- `dotnet test` 全部通过

---

### TASK-31: 存档迁移 v6→v7（熟练度字段）(P0)
**状态**: 未开始
**优先级**: P0
**依赖**: TASK-30
**背景**: 新增 `mastery.*` 存档字段，需要 v6→v7 迁移。

**涉及文件**:
- `scripts/services/SaveMigrationRules.cs` — `LatestVersion = 7`，新增 `MigrateV6ToV7()`
- `tests/Xiuxian2.Tests/SaveMigrationRulesTests.cs` — v6→v7 测试用例
- `docs/design/14_save_migration.md` — 更新迁移规格

**步骤**:
1. `MigrateV6ToV7()`：新增 `mastery.dungeon_level=1`、`mastery.cultivation_level=1`、`mastery.alchemy_level=1`、`mastery.smithing_level=1`（默认值均为 1）
2. 若旧存档已有 `InsightSpendRules` 的单次解锁标记（如 `advanced_alchemy_unlocked`），自动转换为对应熟练度等级
3. 更新 `LatestVersion` 常量

**验收标准**:
- v6 存档加载后自动升级到 v7，`mastery.*` 字段正确填充
- 现有 v6 测试不受影响
- 往返测试覆盖 mastery 字段

---

### TASK-32: 境界突破去悟性化 (P1)
**状态**: 未开始
**优先级**: P1
**依赖**: TASK-30
**背景**: 确认代码中境界突破不消耗悟性。当前实现已是"经验满即可突破"，但需确保所有相关代码路径无遗漏。

**涉及文件**:
- `scripts/services/InsightSpendRules.cs` — 确认无境界突破相关消耗（TASK-30 会重构此文件）
- `scripts/game/PrototypeRootController.cs` — 确认突破逻辑无悟性扣除
- `scripts/services/BossEncounterRules.cs` — Boss 弱点领悟改为副本精通 Lv4 被动效果，移除单次悟性付费
- `tests/Xiuxian2.Tests/BossEncounterRulesTests.cs` — 更新测试，`CanApplyWeaknessInsight` 替换为 mastery 查询
- `tests/Xiuxian2.Tests/InsightSpendRulesTests.cs` — 新增/更新测试覆盖 mastery 解锁逻辑

**验收标准**:
- 突破流程无悟性扣除
- Boss 弱点效果由副本精通 Lv4 永久被动驱动
- `dotnet test` 全部通过

---

### TASK-33: 炼丹/炼器熟练度门控 (P1)
**状态**: 未开始
**优先级**: P1
**依赖**: TASK-30
**背景**: 将炼丹配方可用性和炼器强化段上限改为依赖熟练度等级。

**涉及文件**:
- `scripts/services/AlchemyRules.cs` — 聚灵散可用性改为依赖炼丹精通 Lv2；Lv3 额外 +1 产量
- `scripts/services/SmithingRules.cs` — 强化段上限改为依赖炼器精通等级（Lv1: +3, Lv2: +6, Lv4: +9）；Lv3 材料 -10%
- `scripts/game/CraftingProgressionService.cs` — 注入 mastery 查询
- `scripts/game/ExploreProgressController.Runtime.cs` — 副本精通效果接入（Lv2 精英率、Lv3 掉率）
- `scripts/services/ActivityConversionService.cs` — 修炼精通效果接入（Lv2 灵气+10%、Lv4 经验-8%）
- `tests/Xiuxian2.Tests/AlchemyRulesTests.cs` — 更新测试：配方可用性依赖 mastery level
- `tests/Xiuxian2.Tests/SmithingRulesTests.cs` — 更新测试：强化上限依赖 mastery level

**验收标准**:
- 新存档（所有 mastery Lv1）只能炼回气丹，只能强化到 +3
- 解锁炼丹 Lv2 后可炼聚灵散
- 解锁炼器 Lv2 后可强化到 +6
- `dotnet test` 全部通过

---

### TASK-34: 熟练度 UI 入口 (P1)
**状态**: 未开始
**优先级**: P1
**依赖**: TASK-30, TASK-31
**背景**: 在子菜单中提供熟练度解锁入口，让玩家可以查看和花费悟性。

**涉及文件**:
- `scripts/ui/BookTabsController.cs` — 修炼概况页或新增"领悟"页签，展示 4 系统熟练度状态与解锁按钮
- `scripts/ui/UiText.cs` — 新增熟练度相关文案

**步骤**:
1. 在修炼概况页底部（或新增独立页签）展示 4 系统当前等级和下一级解锁信息
2. 每条展示：系统名、当前等级、效果描述、下一级成本（悟性）、境界门槛、解锁按钮
3. 悟性不足或境界不够时按钮灰显+提示
4. 解锁后即时刷新页面，Toast 提示"领悟成功"

**验收标准**:
- 可在子菜单中查看到 4 个系统的熟练度状态
- 悟性充足时可成功解锁，悟性/境界不足时有明确提示
- 解锁后效果立即生效

---

### TASK-35: 熟练度测试补全 (P1)
**状态**: 未开始
**优先级**: P1
**依赖**: TASK-30, TASK-33
**背景**: 为子系统熟练度补全单元测试。

**涉及文件**:
- `tests/Xiuxian2.Tests/SubsystemMasteryRulesTests.cs` — **新建**
- `tests/Xiuxian2.Tests/InsightSpendRulesTests.cs` — **更新**（原有测试需适配新接口）

**测试覆盖**:
- `CanUnlock_ReturnsTrueWhenInsightAndRealmSufficient`
- `CanUnlock_ReturnsFalseWhenInsightInsufficient`
- `CanUnlock_ReturnsFalseWhenRealmTooLow`
- `CanUnlock_ReturnsFalseWhenAlreadyMaxLevel`
- `TryUnlock_DeductsInsight_AdvancesLevel`
- `GetEffectValue_ReturnsCorrectBonusForDungeonMastery`
- `GetEffectValue_ReturnsCorrectBonusForCultivationMastery`
- `GetEffectValue_ReturnsCorrectBonusForAlchemyMastery`
- `GetEffectValue_ReturnsCorrectBonusForSmithingMastery`
- `FullUnlockCost_Equals2080Insight`（数值一致性校验：12 系统 × 4 级 ≈ 2080）
- 存档迁移 v6→v7 round-trip

**验收标准**:
- 至少 10 个新测试覆盖熟练度核心逻辑（12 系统覆盖）
- `dotnet test` 全部通过

---

## Phase 7: 12 子系统横向扩展（SUBSYSTEM-EXPAND）

> 设计变更日期：2026-03-26
> 核心变更：子系统从 4 个扩展至 12 个，参考 Melvor Idle 多系统设计。
> 新增采集系 3 个（灵田/矿脉/灵渔）、加工系 3 个（符箓/烹饪/阵法）、修行系 2 个（悟道/体修）。
> 参考设计：`02_systems.md` §13-§20、`01_core_loop.md`、`03_progression_and_balance.md`。
> 可扩展框架：`IActivityDefinition` / `IRecipeDefinition` / `ActivityRegistry` / `MaterialRegistry`

### TASK-36: 通用活动框架（Generic Activity Framework）(P0)
**状态**: ✅ 已完成
**优先级**: P0（所有新系统依赖此项）
**依赖**: TASK-30（需 mastery 接口作为 gate 查询）
**背景**: 建立可扩展的活动框架，使所有采集/加工/修行系统能通过定义数据接入，无需为每个系统编写独立推进逻辑。

**完成说明**:
- `ActivityFramework.cs`: IActivityDefinition/IRecipeDefinition 接口 + SimpleRecipeDefinition/SimpleActivityDefinition 实现
- `ActivityRegistry.cs`: 注册表，内置 alchemy + smithing 2 个系统，支持 Register/GetBySystem/GetRecipe
- `MaterialRegistry.cs`: 16 种材料定义（4 灵田 + 4 矿脉 + 3 灵渔 + 5 战斗掉落）
- `PlayerActionState.cs`: 12 模式常量 + ToggleMode() 12 模式循环
- `PlayerActionStateRules.cs`: Normalize() 支持 12 模式
- `PlayerActionCapabilityRules.cs`: 12 模式能力映射（采集=AP+掉落+离线；加工=AP+离线；修行=AP+悟性+离线）
- `CraftingProgressionService.cs`: 新增 AdvanceGenericRecipe() + TryCompleteGenericBatch() 通用推进
- 新增 27 个测试（ActivityRegistryTests 9 + MaterialRegistryTests 6 + GenericCraftingProgressionTests 4 + 原有测试不变），总计 190 测试全部通过

**涉及文件**:
- `scripts/services/IActivityDefinition.cs` — **新建**，定义活动接口：`SystemId`, `RecipeId`, `InputCost`, `OutputItems`, `RequiredMasteryLevel`
- `scripts/services/IRecipeDefinition.cs` — **新建**，定义配方接口：`RecipeId`, `Inputs[]`, `Outputs[]`, `QiCost`, `InputEventsRequired`
- `scripts/services/ActivityRegistry.cs` — **新建**，注册表：按 SystemId 检索所有活动/配方定义
- `scripts/services/MaterialRegistry.cs` — **新建**，材料注册表：定义 16 种材料的 ID/名称/来源/消费者
- `scripts/services/PlayerActionState.cs` — 扩展：新增 `ModeGarden` / `ModeMining` / `ModeFishing` / `ModeTalisman` / `ModeCooking` / `ModeFormation` / `ModeEnlightenment` / `ModeBodyCultivation` 共 8 个新模式常量
- `scripts/services/PlayerActionCapabilityRules.cs` — 扩展：12 个模式的输入→通用活动推进分支
- `scripts/game/CraftingProgressionService.cs` — 重构：从 alchemy/smithing 专用逻辑改为 `ActivityRegistry` 驱动的通用推进

**步骤**:
1. 定义 `IActivityDefinition` 接口和 `IRecipeDefinition` 接口
2. 实现 `ActivityRegistry`，支持 `Register(IActivityDefinition)` / `GetBySystem(string systemId)` / `GetRecipe(string recipeId)`
3. 实现 `MaterialRegistry`，枚举 16 种材料及其产消关系
4. 扩展 `PlayerActionState`，增加 8 个新模式常量，`ToggleMode()` 支持 12 模式循环
5. 重构 `CraftingProgressionService.AdvanceCrafting()` 为基于 `ActivityRegistry` 的通用推进
6. 将现有 `AlchemyRules` / `SmithingRules` 的配方数据注册到 `ActivityRegistry`

**验收标准**:
- `PlayerActionState` 支持 12 个模式切换
- `ActivityRegistry` 包含至少现有 alchemy + smithing 的配方
- `CraftingProgressionService` 能通过 registry 驱动现有炼丹/炼器流程
- 框架可通过新增 `IActivityDefinition` 实现类来扩展新系统，无需修改推进逻辑
- `dotnet test` 全部通过（现有 alchemy/smithing 测试不回归）

---

### TASK-37: 采集系统实现（灵田/矿脉/灵渔）(P1)
**状态**: 未开始
**优先级**: P1
**依赖**: TASK-36
**背景**: 实现 3 个采集子系统，为加工系统提供原材料。

**涉及文件**:
- `scripts/services/GardenRules.cs` — **新建**，灵田规则：作物种植/收获/离线生长
- `scripts/services/GardenState.cs` — **新建**，灵田状态持久化
- `scripts/services/MiningRules.cs` — **新建**，矿脉规则：矿点开采/耐久/稀有矿
- `scripts/services/MiningState.cs` — **新建**，矿脉状态持久化
- `scripts/services/FishingRules.cs` — **新建**，灵渔规则：鱼塘垂钓/稀有鱼
- `scripts/services/FishingState.cs` — **新建**，灵渔状态持久化
- 各系统注册到 `ActivityRegistry`

**V1 内容（每系统）**:
- 灵田：3 作物（灵花、灵果、灵草(替代)），离线生长效率 50%
- 矿脉：3 矿点（寒铁矿、灵玉、碎片(替代)），矿点耐久 100 次采集后刷新
- 灵渔：3 鱼塘（灵鱼、灵珠、龙涎），稀有鱼塘需精通 Lv2

**验收标准**:
- 3 个采集系统可通过模式切换进入
- 输入事件驱动采集进度，完成后获得对应材料
- 材料正确进入背包/库存
- 各系统至少 3 个测试用例
- `dotnet test` 全部通过

---

### TASK-38: 加工系统实现（符箓/烹饪/阵法）(P1)
**状态**: 未开始
**优先级**: P1
**依赖**: TASK-36, TASK-37（需要采集产出的材料作为输入）
**背景**: 实现 3 个加工子系统，消费采集材料产出战斗消耗品/增益。

**涉及文件**:
- `scripts/services/TalismanRules.cs` — **新建**，符箓规则：符咒制作
- `scripts/services/TalismanState.cs` — **新建**，符箓状态
- `scripts/services/CookingRules.cs` — **新建**，烹饪规则：灵膳制作
- `scripts/services/CookingState.cs` — **新建**，烹饪状态
- `scripts/services/FormationRules.cs` — **新建**，阵法规则：阵盘/阵旗制作
- `scripts/services/FormationState.cs` — **新建**，阵法状态
- 各系统注册到 `ActivityRegistry`

**V1 内容（每系统）**:
- 符箓：2 配方（火符 = 碎符×2 + 灵墨×1 → ATK+15% 单战; 盾符 = 碎符×3 + 兽骨×1 → DEF+20% 单战）
- 烹饪：3 配方（灵鱼粥 = 灵鱼×2 + 灵草×1 + 灵气×40 → HP+15% 3场; 灵果蜜饯 = 灵果×2 + 灵花×1 + 灵气×60 → 全属性+5% 3场; 龙涎鱼汤 = 灵鱼×1 + 龙涎×1 + 灵气×100 → 灵气掉落+30% 3场）（详见 `02_systems.md` §17）
- 阵法：2 配方（聚灵阵盘 = 灵玉×3 + 龙涎×1 → 灵气+8% 常驻; 护体阵旗 = 寒铁矿×2 + 灵玉×1 → DEF+5% 常驻）

**验收标准**:
- 3 个加工系统可通过模式切换进入
- 材料消耗正确，产出物品进入对应库存
- 符咒/灵膳为战斗消耗品，阵盘为常驻装备（同时仅 1 个生效）
- 各系统至少 3 个测试用例
- `dotnet test` 全部通过

---

### TASK-39: 修行系统实现（悟道/体修）(P1)
**状态**: 未开始
**优先级**: P1
**依赖**: TASK-36
**背景**: 实现 2 个修行子系统，提供永久小幅加成作为长期目标。

**涉及文件**:
- `scripts/services/EnlightenmentRules.cs` — **新建**，悟道规则：灵气→永久悟性获取效率
- `scripts/services/EnlightenmentState.cs` — **新建**，悟道状态
- `scripts/services/BodyCultivationRules.cs` — **新建**，体修规则：材料+灵气→永久属性
- `scripts/services/BodyCultivationState.cs` — **新建**，体修状态
- 各系统注册到 `ActivityRegistry`

**V1 内容（每系统）**:
- 悟道：2 功法（冥想 = 灵气 200 → 悟性获取+2%，可重复 20 次; 参悟 = 灵气 500 + 灵珠×1 → 悟性获取+5%，可重复 10 次）
- 体修：2 功法（淬体 = 灵气 300 + 兽骨×2 → HP+1%，可重复 20 次; 炼骨 = 灵气 500 + 寒铁矿×2 → DEF+1%，可重复 15 次）

**验收标准**:
- 2 个修行系统可通过模式切换进入
- 材料/灵气消耗正确，永久加成正确累加
- 永久加成有累计上限，递减规则清晰
- 各系统至少 3 个测试用例
- `dotnet test` 全部通过

---

### TASK-40: 存档迁移 v7→v8（新系统字段）(P0)
**状态**: 未开始
**优先级**: P0
**依赖**: TASK-37, TASK-38, TASK-39
**背景**: 为 8 个新系统的状态字段新增 v7→v8 存档迁移。

**涉及文件**:
- `scripts/services/SaveMigrationRules.cs` — `LatestVersion = 8`，新增 `MigrateV7ToV8()`
- `tests/Xiuxian2.Tests/SaveMigrationRulesTests.cs` — v7→v8 测试用例
- `docs/design/14_save_migration.md` — 更新迁移规格（见 TASK-42 配套）

**迁移字段**:
```
garden.plot_*          = empty / 0    // 灵田田地
mining.node_*          = 0            // 矿脉矿点耐久
fishing.pond_*         = 0            // 灵渔鱼塘
talisman.inventory_*   = 0            // 符咒库存
cooking.inventory_*    = 0            // 灵膳库存
formation.active_id    = ""           // 当前激活阵盘
enlightenment.count_*  = 0            // 悟道修炼次数
body_cult.count_*      = 0            // 体修修炼次数
mastery.garden_level   = 1            // 新系统精通等级
mastery.mining_level   = 1
mastery.fishing_level  = 1
mastery.talisman_level = 1
mastery.cooking_level  = 1
mastery.formation_level= 1
mastery.enlightenment_level = 1
mastery.body_cultivation_level = 1
```

**验收标准**:
- v7 存档加载后自动升级到 v8，新字段正确填充默认值
- 现有 v6→v7 迁移不受影响
- 往返测试覆盖新字段
- `dotnet test` 全部通过

---

### TASK-41: 新系统精通树集成 (P1)
**状态**: 未开始
**优先级**: P1
**依赖**: TASK-30, TASK-37, TASK-38, TASK-39
**背景**: 将 8 个新系统的精通树（各 4 级）集成到 `SubsystemMasteryRules`。

**涉及文件**:
- `scripts/services/SubsystemMasteryRules.cs` — 追加 8 系统 × 4 级 = 32 条 `MasteryDefinition`
- `scripts/services/SubsystemMasteryState.cs` — 追加 8 个 `mastery.{system}_level` 字段
- `tests/Xiuxian2.Tests/SubsystemMasteryRulesTests.cs` — 新增 8 系统精通测试

**精通效果概要**:
| 系统 | Lv2 | Lv3 | Lv4 |
|------|-----|-----|-----|
| 灵田 | 生长+15% | 稀有+10% | 离线100% |
| 矿脉 | 耐久+20% | 稀有+10% | 双倍10% |
| 灵渔 | 速度+15% | 稀有鱼塘解锁 | 双倍8% |
| 符箓 | 双出10% | 材料-10% | 附魔概率 |
| 烹饪 | 持续+1场 | 双出15% | 额外效果 |
| 阵法 | 效果+10% | 双槽 | 自修复 |
| 悟道 | 效率+10% | 冥想上限+10 | 参悟上限+5 |
| 体修 | 效率+10% | 淬体上限+10 | 炼骨上限+5 |

**验收标准**:
- `SubsystemMasteryRules` 包含 12 系统 × 4 级 = 48 条定义
- 总解锁成本 ≈ 2080 悟性
- 各精通效果正确接入对应系统规则
- `dotnet test` 全部通过

---

### TASK-42: 新系统测试覆盖 (P1)
**状态**: 未开始
**优先级**: P1
**依赖**: TASK-37, TASK-38, TASK-39, TASK-40, TASK-41
**背景**: 为 Phase 7 增加的 8 个新系统补全测试。

**涉及文件**:
- `tests/Xiuxian2.Tests/GardenRulesTests.cs` — **新建**
- `tests/Xiuxian2.Tests/MiningRulesTests.cs` — **新建**
- `tests/Xiuxian2.Tests/FishingRulesTests.cs` — **新建**
- `tests/Xiuxian2.Tests/TalismanRulesTests.cs` — **新建**
- `tests/Xiuxian2.Tests/CookingRulesTests.cs` — **新建**
- `tests/Xiuxian2.Tests/FormationRulesTests.cs` — **新建**
- `tests/Xiuxian2.Tests/EnlightenmentRulesTests.cs` — **新建**
- `tests/Xiuxian2.Tests/BodyCultivationRulesTests.cs` — **新建**
- `tests/Xiuxian2.Tests/ActivityRegistryTests.cs` — **新建**
- `tests/Xiuxian2.Tests/SaveMigrationRulesTests.cs` — 追加 v7→v8 迁移测试

**最低测试覆盖（每系统 ≥ 3 个）**:
- 基本流程：输入→进度→产出
- 材料不足时拒绝
- 精通门控：未解锁高级配方/功法时不可用
- 存档往返：状态正确持久化与恢复

**验收标准**:
- 每个新系统至少 3 个测试（共 ≥ 24 个新测试）
- `ActivityRegistry` 至少 5 个测试（注册/查询/重复注册/未找到等）
- `dotnet test` 全部通过

---

### Phase 7 依赖图

```
  TASK-30 (mastery rules, Phase 6)
     |
  TASK-36 (activity framework)  ← 所有新系统的基础
   / | \
  37 38 39   (采集 / 加工 / 修行)
   \ | /
  TASK-40 (save migration v7→v8)
  TASK-41 (mastery tree integration)
     |
  TASK-42 (test coverage)
```

### Phase 7 执行建议
1. **先 TASK-36**：建立框架，将现有 alchemy/smithing 迁移到 registry，确保零回归
2. **TASK-37/38/39 可并行**：三个系统组互不依赖，但 38 需要 37 的材料产出
3. **TASK-40 最后**：所有系统稳定后再固化存档格式
4. **TASK-41 与 40 可并行**：mastery 数据定义不依赖存档迁移
5. **TASK-42 收尾**：贯穿各系统完成过程中持续补充

---

## Phase 8: 文档审计修复（16 项）— 已完成

> 本轮审计覆盖全部 16 篇设计文档，发现并修复 16 项跨文档一致性问题。

### 已修复问题清单
| # | 优先级 | 问题 | 修复内容 | 涉及文档 |
|---|--------|------|---------|----------|
| 1 | P0 | 灵气产消赤字 0.68:1 | 被动恢复 +5/min + 新系统降耗 → 1.85:1 | 03, 02 |
| 2 | P0 | §12.3 悟性口径仍为 4 系统 | 统一为 12 系统/2080 悟性/43 天 | 02 |
| 3 | P0 | 烹饪配方 02 vs 10 矛盾 | TASK-38 对齐 02 §17（3 配方） | 10 |
| 4 | P1 | 资源循环图仍为 4 模式 | 替换为 12 模式蛛网大图 | 02 |
| 5 | P1 | 新系统无离线结算设计 | 新增 §18A-§18H（8 系统） | 13 |
| 6 | P1 | 符咒/灵膳/阵盘无战斗集成 | InBattle 新增 3 类消耗品规则 | 06 |
| 7 | P1 | 灵石消费端空缺 | 扩充 8 项新消费（种子商店/矿脉刷新等） | 02 |
| 8 | P1 | 重复定义缺唯一定义点 | 03/06/10/11 添加 cross-ref 注释 | 03/06/10/11 |
| 9 | P2 | 12 模式无 UI 选择器设计 | 分组二级菜单 + 样式规范 | 05, 06 |
| 10 | P2 | 里程碑未反映 Phase 6-7 | 新增 M5/M6 里程碑 | 04 |
| 11 | P2 | ~15 处 REVIEW-FIX 未清理 | 全部改为 REVIEW-DONE | 04/05/06/07/13/14/15 |
| 12 | P2 | 09 JSON 缺新材料 | lv_qi_003-005 新增 spirit_ink/beast_bone/toxic_gland | 09 |
| 13 | P3 | 08 内容示例已脱节 | 标注为历史草稿 | 08 |
| 14 | P3 | 悟道/体修递减未量化 | 新增递减曲线表 + 公式 | 03 |
| 15 | P3 | 阵盘双槽与效果+20% 矛盾 | 统一为 Lv3 双槽 | 02, 03, 10 |
| 16 | P3 | 种子缺消费出口 | 新增育种研究 + 炼丹辅料 + 回售 | 02 |
