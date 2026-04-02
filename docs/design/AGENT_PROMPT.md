# Agent 执行提示词 — xiuxian_4 实施任务

> 生成日期：2026-03-24
> 目标：基于已完成的设计文档审计和讨论决策，执行代码实施任务。

---

## 项目概况

- **引擎**：Godot 4.5.1 + C# (.NET 8)
- **产品**：桌宠修仙挂机游戏（低打扰桌面宠物，键鼠输入驱动修炼进度）
- **测试命令**：`dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj`
- **约定**：所有代码修改必须通过测试零失败

## 工作区结构

```
xiuxian_4/
├── scripts/
│   ├── game/          # 控制器（PrototypeRootController, ExploreProgressController）
│   ├── services/      # 纯规则/状态类（无 Godot 依赖，可单元测试）
│   ├── ui/            # UI 控制器
│   └── tests/         # 编辑器内测试（非主测试）
├── tests/Xiuxian2.Tests/  # xUnit 单元测试（主测试套件）
├── docs/design/       # 设计文档（所有设计决策的权威来源）
└── scenes/            # Godot 场景文件
```

## 设计文档索引

所有实施的权威依据来自以下文档。在实施任何任务前，**必须先阅读对应设计文档的相关章节**：

| 文档 | 关键内容 |
|------|---------|
| `10_todo.md` | 完整任务清单 TASK-01 到 TASK-42，含依赖、步骤、验收标准 |
| `02_systems.md` | 系统总表（19 个系统），§9 炼丹，§10 炼器，§11 灵石经济，§12-§19 新子系统 |
| `02_systems.md` §4 | 副本循环模型，战斗失败后果，Boss 挑战，区域选择（原 06 已合并） |
| `03_progression_and_balance.md` | 悟性消耗路径，日产消预算表，11 系统数值平衡 |
| `13_offline_settlement.md` | §17 炼丹离线，§18 炼器离线，§18A-§18H 新系统离线 |
| `14_save_migration.md` | v5→v6→v7→v8→v9 迁移字段表 |
| `05_ui_style.md` | Toast 通知规范，11 模式选择器设计 |
| `07_content_template.md` | §3.5 Boss 模板字段，§4a 丹药模板，采集/烹饪/体修模板 |
| `01_core_loop.md` | 11 活动模式核心循环，资源蛛网，V2 扩展蓝图 |
| `11_equipment_content_system.md` | §5.4 炼器/强化系统 |
| `09_level_monster_drop_sample.json` | 内容样本数据 |

> **注意**：具体 TASK 状态与执行顺序以 `10_todo.md` 为准。本文描述整体工作流程与工具库，不维护任务进度。

## 关键设计决策摘要

以下决策已在讨论中确认，实施时必须遵循：

### 1. 副本循环模型（最重要的架构变更）

**每个区域可无限循环刷取**：
```
0% → 遇怪战斗(获掉落) → 100% → Boss 战斗 → 进度归零 → 0% → ...
```
- Boss **胜利**：结算掉落 + `repeat_clear`/`first_clear` 奖励 → 进度归零 → 下一循环
  - 首杀额外解锁下一区域
- Boss **失败**：进度归零 → 重新开始本区域循环（自然时间惩罚）
- 普通/精英怪失败：**不**归零，仅跳过掉落
- 玩家可在已解锁区域之间自由切换（切换时进度归零）
- 代码：`BattleDefeatDecision.ShouldResetExploreProgress` = Boss 失败时 `true`，普通/精英 `false`
- 存档新增 `unlocked_zone_ids[]` 和 `zone_cycle_counts{}`

### 2. 突破丹已取消
- V1 不存在突破丹，`novice_breakthrough_pill` 已从所有掉落表/JSON 中移除
- 精英掉落表原突破丹 slot 改为 `spirit_herb`（50:50 权重）
- 首通奖励原突破丹改为 `spirit_stones` 货币（60/90/130/180/250 按区域递增）
- V2 预留：若引入突破失败概率，可作为保底道具回归

### 3. 灵石经济
- `ResourceWalletState` 新增 `SpiritStones` (int)
- 产出：战斗基础奖励(5-15/场)、出售材料、首通奖励(60-200)、Boss 首杀(300-500)
- V1 消费：区域装备兑换(50-200)
- V1 后续消费（本轮不实施）：Boss 重挑刷新(50/次)、丹药急购(30/个)
- 出售材料必须弹窗确认

### 4. 离线结算无硬性上限
- 炼丹离线：无批次上限，材料耗尽即停
- 炼器离线：无等级上限，材料/EnhanceLevel 上限即停
- 副本离线：计算完整循环数 + 剩余进度

### 5. 悟性保留，3 消耗路径
- 境界突破（已有）
- Boss 弱点领悟（30-80 悟性，-10% Boss 属性）
- 高阶炼丹配方（20 悟性/批）
- ~~装备鉴定~~（已取消）

### 6. Toast 通知系统
- 新建 `scripts/ui/ToastController.cs`
- `ShowToast(level, message, durationOverride?)`
- 三级：重要(3-4s 金色)、普通(2-3s 淡褐)、信息(1.5-2s 半透明)
- 同时最多 1 条排队
- 离线结算回归时逐条播报（间隔 0.8s，<5 分钟不播报）

## 推荐执行顺序

按批次执行，同批次内可并行：

```
批次 1（基础）:    TASK-06（待验收）, TASK-03✅, TASK-01（部分进展）
批次 2（核心）:    TASK-04✅, TASK-05✅, TASK-02
批次 3（测试）:    TASK-07, TASK-08, TASK-09, TASK-10
批次 4（补充）:    TASK-11, TASK-12, TASK-13
批次 5（优化）:    TASK-14, TASK-15✅, TASK-16, TASK-17, TASK-18
批次 6（Melvor）:  TASK-19✅, TASK-22✅
批次 7（依赖19）:  TASK-20✅, TASK-21✅
批次 8（依赖20）:  TASK-23✅
批次 9（审计修复）: TASK-24✅, TASK-25✅, TASK-26✅, TASK-28, TASK-29✅
```

已完成任务：TASK-03, 04, 05, 15, 19, 20, 21, 22, 23, 24, 25, 26, 29。
TASK-27 已取消。
TASK-06 代码已修复，待 Godot 编辑器人工验收。
TASK-01 部分进展（ActiveLevelManager 已拆出）。

## 每个任务的执行流程

对于每个 TASK：
1. **阅读** `10_todo.md` 中该 TASK 的完整描述（依赖、背景、步骤、验收标准）
2. **阅读** 对应的设计文档章节（TASK 描述中标注了文档路径）
3. **检查** 依赖 TASK 是否已完成
4. **阅读** 涉及的现有代码文件，理解当前实现
5. **实施** 代码修改
6. **运行** `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj` 确认零失败
7. **补充** 必要的新单元测试（每个 TASK 的验收标准中有测试要求）

## 变更标记

文档中有两类标记可帮助定位所有设计变更点：
- `<!-- MELVOR-CHANGE -->` — Melvor Idle 启发的系统扩展
- `<!-- REVIEW-FIX-Px -->` — 文档审计修复（P0/P1/P2）

在代码实施时，如果不确定某个设计细节，搜索这些标记找到对应文档段落。

## 注意事项

1. **纯规则类放 `scripts/services/`**，不依赖 Godot Node，方便单元测试
2. **控制器放 `scripts/game/`**，负责 Godot 节点操作和规则层调用
3. **不要在 services 层引用 Godot 类型**（如 `Node`、`GD`）
4. **存档使用 ConfigFile**：`user://save_state.cfg`，当前 schema v5，实施 v6 迁移见 TASK-29
5. **ExploreProgressController 是 1600+ 行上帝对象**，修改时注意不要引入新的耦合，TASK-02 会拆分它
6. **LevelConfigLoader 是 1531 行上帝对象**，TASK-01 会拆分它
7. **`PlayerActionCapability` 枚举** 定义了各模式能力：`ConsumesApSettlement`, `GrantsCultivationInputExp`, `AdvancesDungeon`, `RunsBattle`, `GeneratesLoot`, `SupportsOfflineSettlement`
8. **11 活动模式**：`dungeon`, `cultivation`, `alchemy`, `smithing`, `garden`, `mining`, `fishing`, `talisman`, `cooking`, `formation`, `body_cultivation` — 在 `PlayerActionState` 中通过 mode_id 字符串区分

## 设计工作规则

- 改变产品行为时，先更新设计文档再改代码。
- 涉及数值平衡的变更，必须明确写出公式、上限和调参钩子。
- 每个系统需定义输入/输出、存档字段、UI 入口。
- 输入采集仅存储次数和强度，不记录原始键值或鼠标轨迹。
- 共享 UI 文本统一维护在 `scripts/ui/UiText.cs`，不散落硬编码字符串。
- Godot 场景文件 `*.tscn` 使用 UTF-8 无 BOM 编码。
