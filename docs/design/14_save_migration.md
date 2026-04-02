# 14 存档版本迁移规约

## 目标
- 为 `user://save_state.cfg` 建立统一的 schema 迁移入口。
- 让 `PrototypeRootController` 不再散落多个 `version < x` 分支。
- 为后续装备、统计、设置扩展提供可追加的升级链。

## 当前版本
- 最新存档版本：`v8`（Phase 7 扩展后）
- 统一迁移入口：`scripts/services/SaveMigrationRules.cs`

## 迁移原则
- 迁移必须幂等：同一版本补丁重复执行不应破坏数据。
- 迁移必须单向：仅允许 `vN -> vN+1` 链式升级，不做跨级特判分叉。
- 迁移必须保守：优先补齐缺失字段和默认容器，不重写玩家已有值。
- 迁移后立即将 `meta.version` 写为目标版本，并在加载完成后持久化回磁盘。

## 当前升级链

### v1 -> v2
- 将旧键 `ui.submenu_active_tab` 提升为：
  - `ui.submenu_active_left_tab`
  - `ui.submenu_active_right_tab`
- 右侧页签默认值为 `BugTab`

### v2 -> v3
- 规范化 `action.mode` 字典
- 补齐：
  - `mode_id`
  - `action_id`
  - `action_target_id`
  - `action_variant`

### v3 -> v4
- 补齐 `settings.system` 容器

### v4 -> v5
- 补齐装备存档容器：
  - `backpack.items.__equipment_profiles`
  - `backpack.items.__equipment_instances`
  - `equipment.equipped`
- 若缺失则补齐 `meta.last_saved_unix`

## 接入要求
- 统一读取入口必须先执行 `SaveMigrationRules.MigrateToLatest()`，再调用各 `Read*State()`。
- 如果本次加载发生迁移，结束后必须重新执行一次保存，将升级后的结构落盘。

## 后续新增版本
1. 将 `LatestVersion` 加一。
2. 新增一个 `MigrateVxToVy(ConfigFile cfg)`。
3. 只在该方法内处理本次版本新增字段。
4. 为该迁移补至少一个测试：
   - 旧版本输入
   - 升级后结构断言
   - 已是最新版本时无副作用

<!-- REVIEW-DONE: v5→v6 迁移规则已完成 -->

## 已实现迁移：v5 -> v6（Melvor 扩展）

### 新增字段
以下字段在 v6 中必须存在，迁移时候补齐默认值：

| 字段路径 | 默认值 | 来源 |
|---------|---------|------|
| `wallet.spirit_stones` | `0` | 灵石经济系统 |
| `alchemy.selected_recipe` | `""` | 炼丹系统 |
| `alchemy.progress` | `0.0` | 炼丹系统 |
| `backpack.potions` | `{}` (空字典) | 丹药背包 |
| `smithing.target_equipment_id` | `""` | 炼器系统 |
| `smithing.progress` | `0.0` | 炼器系统 |
| `boss.defeated_zones` | `[]` (空数组) | Boss 挑战 |
| `boss.current_hp` | `-1` (无战斗) | Boss 断点续战 |
| `unlocked_zone_ids` | `["lv_qi_001"]` | 副本循环模型 |
| `zone_cycle_counts` | `{}` (空字典) | 副本循环统计 |

### 迁移特殊处理
- v5 存档中若 `explore_progress >= 100`，归零为 `0`（副本循环模型下已触发 Boss 的进度不保留）。
- v5 存档中的 `zone_id` 加入 `unlocked_zone_ids`；之前的所有区域也根据 `zone_id` 顺序补入。
- v5 存档中若存在 `novice_breakthrough_pill`（背包物品），迁移时移除（已取消）。

---

## 已实现迁移：v6 -> v7（子系统熟练度 INSIGHT-MASTERY-CHANGE）

> 关联任务：TASK-31
> 核心变更：新增 `mastery.*` 存档字段，支持 11 系统熟练度持久化。

### 新增字段

| 字段路径 | 默认值 | 来源 |
|---------|---------|------|
| `mastery.dungeon_level` | `1` | 副本精通 |
| `mastery.cultivation_level` | `1` | 修炼精通 |
| `mastery.alchemy_level` | `1` | 炼丹精通 |
| `mastery.smithing_level` | `1` | 炼器精通 |

### 迁移特殊处理
- 若旧存档存在 `advanced_alchemy_unlocked = true`，则设 `mastery.alchemy_level = 2`。
- 若旧存档存在 `boss_weakness_unlocked = true`，则设 `mastery.dungeon_level = 4`（Boss 弱点现为副本精通 Lv4 被动）。
- 其他 mastery 字段均默认为 1（初始等级）。

---

## 已实现迁移：v7 -> v8（12 子系统横向扩展 SUBSYSTEM-EXPAND）

> 关联任务：TASK-40
> 核心变更：8 个新子系统的状态字段 + 8 个新精通字段。

### 新增字段

#### 采集系统状态

| 字段路径 | 默认值 | 来源 |
|---------|---------|------|
| `garden.plots` | `{}` (空字典) | 灵田：田地槽位 |
| `garden.last_harvest_unix` | `0` | 灵田：上次收获时间戳 |
| `mining.active_node` | `""` | 矿脉：当前矿点 ID |
| `mining.node_durability` | `0` | 矿脉：矿点剩余耐久 |
| `fishing.active_pond` | `""` | 灵渔：当前鱼塘 ID |
| `fishing.catch_count` | `0` | 灵渔：累计捕获数 |

#### 加工系统状态

| 字段路径 | 默认值 | 来源 |
|---------|---------|------|
| `talisman.selected_recipe` | `""` | 符箓：当前配方 |
| `talisman.progress` | `0.0` | 符箓：制作进度 |
| `talisman.inventory` | `{}` (空字典) | 符箓：符咒库存 |
| `cooking.selected_recipe` | `""` | 烹饪：当前配方 |
| `cooking.progress` | `0.0` | 烹饪：制作进度 |
| `cooking.inventory` | `{}` (空字典) | 烹饪：灵膳库存 |
| `formation.selected_recipe` | `""` | 阵法：当前配方 |
| `formation.progress` | `0.0` | 阵法：制作进度 |
| `formation.active_id` | `""` | 阵法：当前激活阵盘 ID |
| `formation.inventory` | `{}` (空字典) | 阵法：阵盘库存 |

#### 修行系统状态

| 字段路径 | 默认值 | 来源 |
|---------|---------|------|
| `body_cult.selected_technique` | `""` | 体修：当前功法 |
| `body_cult.temper_count` | `0` | 体修：淬体累计次数 |
| `body_cult.forge_count` | `0` | 体修：炼骨累计次数 |

#### 新系统精通字段

| 字段路径 | 默认值 | 来源 |
|---------|---------|------|
| `mastery.garden_level` | `1` | 灵田精通 |
| `mastery.mining_level` | `1` | 矿脉精通 |
| `mastery.fishing_level` | `1` | 灵渔精通 |
| `mastery.talisman_level` | `1` | 符箓精通 |
| `mastery.cooking_level` | `1` | 烹饪精通 |
| `mastery.formation_level` | `1` | 阵法精通 |
| `mastery.body_cultivation_level` | `1` | 体修精通 |

### 迁移特殊处理
- 所有新字段均使用空/零默认值，无需从旧字段推导。
- `formation.active_id = ""`：新存档无激活阵盘。
- 全部 7 个新精通字段默认 `1`（初始等级）。
- `garden.last_harvest_unix = 0`：首次收获时由运行时写入。

### 模式字段迁移
- 若 `action.mode.mode_id` 值为 `"dungeon"` 或 `"cultivation"`，保持不变。
- 其他值回退为 `"dungeon"`。
- 新增合法值：`"alchemy"`、`"smithing"`。

### 实现步骤
1. `LatestVersion` 从 5 变为 6。
2. 新增 `MigrateV5ToV6(ConfigFile cfg)`：
   - 补齐上表所有字段（若已存在则不覆写）。
   - 验证 `action.mode.mode_id` 合法性。
3. 新增测试：v5 存档输入 → v6 后字段完整、模式合法、无副作用。

---

<!-- INSIGHT-MASTERY-CHANGE: v6→v7 迁移规格 -->
## v6 → v7 迁移规格（子系统熟练度）

### 版本变更
- 当前版本：**v7**
- 触发条件：`meta.version < 7`

### 新增字段
| 字段路径 | 默认值 | 来源 |
|---------|---------|------|
| `mastery.dungeon_level` | `1` | 子系统熟练度（副本精通） |
| `mastery.cultivation_level` | `1` | 子系统熟练度（修炼精通） |
| `mastery.alchemy_level` | `1` | 子系统熟练度（炼丹精通） |
| `mastery.smithing_level` | `1` | 子系统熟练度（炼器精通） |

### 迁移特殊处理
- 若 v6 存档中存在 `advanced_alchemy_unlocked = true`（旧版高阶炼丹解锁标记），将 `mastery.alchemy_level` 设为 `2`（等价于炼丹精通 Lv2），然后移除该旧字段。
- 旧版 Boss 弱点领悟为单次付费效果，无法精确迁移为永久被动。`mastery.dungeon_level` 保持默认 `1`，玩家需重新解锁。
- 悟性（`insight`）余额保留不变，迁移后可用于解锁新的熟练度等级。

### 实现步骤
1. `LatestVersion` 从 6 变为 7。
2. 新增 `MigrateV6ToV7(ConfigFile cfg)`：
   - 补齐上表所有字段（若已存在则不覆写）。
   - 检查并转换 `advanced_alchemy_unlocked` 旧标记。
3. 新增测试：v6 存档输入 → v7 后 mastery 字段完整、旧标记已转换。
