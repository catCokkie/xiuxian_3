# 15 Melvor Idle 启发的资源循环与多活动模式设计

## 0. 设计背景
- 参考游戏：[Melvor Idle](https://melvoridle.com/) — 一款以多活动模式、资源循环闭环著称的放置类游戏。
- 核心借鉴：Melvor 的系统分类思路（8+ 独立活动模式，每个模式有独立进度和资源产出/消耗），而非照搬其具体玩法。
- 适配原则：保持"低打扰桌宠"产品方向，不引入需要频繁手动操作的重系统。

## 1. 设计决策记录

### 1.1 分期策略
- 选择 **V1 + V2 分阶段** 而非一次性全量实现。
- V1：4 个活动模式（副本、修炼、炼丹、炼器）+ Boss 挑战。
- V2：扩展到 8 个活动模式（新增符箓、灵田、炼宝、灵宠互动）。

### 1.2 副本模型
- 选择 **进度 + Boss 模型** 而非 Melvor 的波次链模型。
- 理由：桌宠场景下玩家不会长时间盯屏，波次链的"一口气打到底"体验不适合。
- 区域探索到 100% 后触发 Boss 挑战，败了可以回去升级再战。

### 1.3 模式切换体验
- 选择 **轻切换**（底栏下拉选择器）而非弹窗确认。
- 切换不中断 AP 结算，仅改变"输入事件驱动什么进度"。

## 2. V1 — 4 活动模式

### 2.1 模式总览
| 模式 | 输入驱动 | 主要产出 | 主要消耗 | 对应系统文档 |
|------|---------|---------|---------|-------------|
| 副本（Dungeon） | 探索进度 + 战斗 | 灵草、碎片、碎符、灵气、悟性、装备 | 无（纯产出端） | `06_bottom_exploration_battle.md` |
| 修炼（Cultivation） | 境界经验 | 灵气（高效）、悟性（高效）、境界经验 | 无（纯产出端） | `01_core_loop.md` |
| 炼丹（Alchemy） | 炼丹进度 | 丹药（回气丹、聚灵散等） | 灵草 + 灵气 | `02_systems.md` §9 |
| 炼器（Smithing） | 强化进度 | 装备强化等级提升 | 碎片/碎符 + 灵气 | `02_systems.md` §10, `11_equipment.md` §5.4 |

### 2.2 资源流转矩阵
<!-- REVIEW-FIX: 补充灵石行、更新悟性消耗状态 -->
| 资源 | 产出来源 | 消耗去向 | 当前状态 |
|------|---------|---------|---------|
| 灵气 | 所有模式（AP → lingqi） | 炼丹、炼器、突破 | ✅ 有产出，✅ 消耗已定义（日收支 ~1.15:1） |
| 悟性 | 所有模式（AP → insight） | 境界突破、Boss 弱点窥探、高阶炼丹、装备鉴定 | ✅ 有产出，✅ 消耗已扩展（日收支 ~0.96:1） |
| 灵草 | 副本掉落 | 炼丹配方 | ✅ 有产出，❌ 无消耗 → TASK-20 修复 |
| 碎片 | 副本掉落 | 炼器/强化 | ✅ 有产出，❌ 无消耗 → TASK-21 修复 |
| 碎符 | 副本掉落 | 炼器/强化（高级） | ✅ 有产出，❌ 无消耗 → TASK-21 修复 |
| 丹药 | 炼丹产出 | 战斗自动消耗 | ❌ 不存在 → TASK-20 + TASK-23 新增 |
| 装备 | 副本掉落 + Boss | 穿戴 + 强化 | ✅ 掉落有，❌ 强化未激活 → TASK-21 |
| **灵石** | Boss 掉落、精英怪、装备出售 | 装备兑换、Boss 重置、丹药购买 | ❌ 代码无字段 → TASK-24 新增（见 `02_systems.md` §11） |

### 2.3 资源循环图
```
        ┌────── 灵草 ──────┐
        │                  ▼
  ┌───────────┐      ┌───────────┐
  │  副本探索  │      │  炼丹系统  │──→ 丹药 ──→ 战斗中自动消耗
  │           │      └───────────┘
  │ 产出：     │           ▲
  │  灵草      │           │ 灵气
  │  碎片/碎符  │      ┌───────────┐
  │  灵气/悟性  │      │  打坐修炼  │──→ 灵气(高效) + 悟性 + 境界经验
  │  装备      │      └───────────┘
  └─────┬─────┘           │ 灵气
        │ 碎片/碎符        ▼
        │            ┌───────────┐
        └──────────→ │  炼器/强化  │──→ 装备 EnhanceLevel ↑ ──→ 战力 ↑
                     └───────────┘
```

## 3. V1 — Boss 挑战系统

### 3.1 触发条件
- 区域探索进度达到 100% 后，进入 Boss 挑战（不直接切下一区域）。
- Boss 为每个区域独有，属性 = 精英怪 × 2-3 倍。

### 3.2 Boss 战斗规则（副本循环模型）
- 回合上限 20 回合，超时判负。
- 胜利 → 结算 Boss 掉落 + 区域奖励 → 探索进度归零 → 开始下一循环。
  - 首次胜利：额外 `first_clear` 奖励 + 解锁下一区域。
  - 重复胜利：`repeat_clear` 奖励。
- 失败 → 探索进度归零 → 重新开始本区域循环（自然时间惩罚，不扣资源）。
- 丹药在 Boss 战斗中同样自动消耗。
- 玩家可在任意已解锁区域之间自由切换刷取。

### 3.3 Boss 掉落
- 首次击杀：保底 1 件灵器 + 大量灵气/悟性。
- 重复击杀：法器为主的普通 Boss 掉落表。

## 4. V2 扩展蓝图（预留）

### 4.1 V2 新增活动模式（V1 不实现）
| 模式 | 设计意图 | 产出 | 消耗 |
|------|---------|------|------|
| 符箓 | 制作战斗 Buff 符 | 符咒（战斗加成） | 碎符 + 灵气 |
| 灵田 | 自动种植灵草 | 灵草（稳定产出） | 灵气种子 |
| 炼宝 | 合成高阶装备 | 宝器级装备 | 多件灵器 + 稀有材料 |
| 灵宠互动 | 灵宠养成 | 灵宠被动加成 | pet_affinity + 材料 |

### 4.2 V2 资源扩展
- Boss 独有材料 → 高阶配方消耗。
- 灵田种子 → 新增灵气消耗出口。
- 符箓系统 → 碎符的高级消耗方式。

## 5. 代码影响评估

### 5.1 需要修改的核心文件
| 文件 | 改动内容 | 对应 TASK |
|------|---------|----------|
| `PlayerActionState.cs` | 新增 2 个模式常量，`SetMode()` 替代 `ToggleMode()` | TASK-19 |
| `PlayerActionCapabilityRules.cs` | 新增 alchemy/smithing 能力分支 | TASK-19 |
| `ExploreProgressController.cs` | `OptionButton` 4 项，模式分发逻辑 | TASK-19 |
| `EquipmentInstanceData` | `EnhanceLevel` 字段激活消费 | TASK-21 |
| `EquipmentStatProfile` | 新增强化倍率计算 | TASK-21 |
| `BattleRules.cs` | 集成消耗品逻辑 | TASK-23 |

### 5.2 需要新建的文件
| 文件 | 用途 | 对应 TASK |
|------|------|----------|
| `AlchemyRules.cs` | 炼丹配方逻辑 | TASK-20 |
| `AlchemyState.cs` | 炼丹状态 | TASK-20 |
| `PotionInventoryState.cs` | 丹药背包 | TASK-20 |
| `SmithingRules.cs` | 强化逻辑 | TASK-21 |
| `SmithingState.cs` | 强化状态 | TASK-21 |
| `BossEncounterRules.cs` | Boss 逻辑 | TASK-22 |
| `ConsumableUsageRules.cs` | 丹药自动消耗 | TASK-23 |

## 6. 变更标记索引

以下是本次 Melvor 整合在各文档中的标记位置，搜索 `MELVOR-CHANGE` 即可定位：

| 文档 | 标记内容 | 描述 |
|------|---------|------|
| `02_systems.md` | 系统清单 | 新增系统 9（炼丹）、10（炼器） |
| `02_systems.md` | 系统 9 详细设计 | 炼丹系统完整描述 |
| `02_systems.md` | 系统 10 详细设计 | 炼器/强化系统完整描述 |
| `02_systems.md` | C# 领域类 | 新增 8 个领域类建议 |
| `02_systems.md` | 资源循环图 | 4 模式产出/消耗关系 |
| `01_core_loop.md` | 分钟循环 | 主行为从 2→4 模式 |
| `01_core_loop.md` | 资源循环说明 | 4 模式闭环策略 |
| `06_bottom_exploration_battle.md` | InBattle 丹药消耗 | 战斗中自动使用丹药 |
| `06_bottom_exploration_battle.md` | BossChallenge 状态 | 新增 Boss 挑战阶段 |
| `06_bottom_exploration_battle.md` | 状态机 | 新增 BossChallenge 状态 |
| `06_bottom_exploration_battle.md` | 底栏 4 模式显示 | 模式切换 UI 规则 |
| `11_equipment_content_system.md` | 5.3 当前不做 | 锻造合成移出 |
| `11_equipment_content_system.md` | 5.4 炼器/强化 | 新增完整强化系统设计 |
| `03_progression_and_balance.md` | 炼丹数值 | 配方成本与效果公式 |
| `03_progression_and_balance.md` | 炼器数值 | 强化缩放与材料成本 |
| `03_progression_and_balance.md` | Boss 数值 | Boss 属性缩放与掉落预算 |
| `10_todo.md` | Phase 4 | TASK-19 到 TASK-23 |
| `10_todo.md` | Phase 5 Review-Fix | TASK-24 到 TASK-29（文档审计修复） |

### 6.2 Review-Fix 标记索引

搜索 `REVIEW-FIX` 可定位文档审计修复点：

| 文档 | 标记 | 描述 |
|------|------|------|
| `02_systems.md` | REVIEW-FIX-P0 | §11 灵石经济系统 |
| `02_systems.md` | REVIEW-FIX-P1 | §3 突破丹消费设计 |
| `03_progression_and_balance.md` | REVIEW-FIX-P1 | 悟性新消耗路径 + 日收支平衡验算 |
| `05_ui_style.md` | REVIEW-FIX-P2 | Toast 通知规范 |
| `06_bottom_exploration_battle.md` | REVIEW-FIX-P0 | §3a 战斗失败后果 |
| `06_bottom_exploration_battle.md` | REVIEW-FIX-P2 | §11 新手引导流程 |
| `07_content_template.md` | REVIEW-FIX-P1 | §3.5 Boss 模板字段 |
| `07_content_template.md` | REVIEW-FIX-P2 | §4a 消耗品/丹药模板 |
| `13_offline_settlement.md` | REVIEW-FIX-P0 | §17 炼丹离线 + §18 炼器离线 |
| `14_save_migration.md` | REVIEW-FIX-P1 | v5→v6 迁移规格 |
| `04_milestones.md` | REVIEW-FIX-P2 | M3 里程碑更新 |
| `15_melvor_resource_loop.md` | REVIEW-FIX | 资源矩阵补充灵石 + 悟性状态更新 |
