# 14 存档版本迁移规约

## 目标
- 为 `user://save_state.cfg` 建立统一的 schema 迁移入口。
- 让 `PrototypeRootController` 不再散落多个 `version < x` 分支。
- 为后续装备、统计、设置扩展提供可追加的升级链。

## 当前版本
- 最新存档版本：`v14`
- 统一迁移入口：`scripts/services/SaveMigrationRules.cs`
- 当前升级链：`v1 → v2 → v3 → v4 → v5 → v6 → v7 → v8 → v9 → v10 → v11 → v12 → v13 → v14`

## 迁移原则
- 迁移必须幂等：同一版本补丁重复执行不应破坏数据。
- 迁移必须单向：仅允许 `vN -> vN+1` 链式升级，不做跨级特判分叉。
- 迁移必须保守：优先补齐缺失字段和默认容器，不重写玩家已有值。
- 迁移结束后立即将 `meta.version` 写为目标版本，并在加载完成后持久化回磁盘。
- 迁移必须 fail-closed：正式读取前先在副本上执行整条迁移链；任一步失败都中止加载，并保留原始存档不被半迁移结果污染。

## 接入要求
- 统一读取入口优先调用 `SaveMigrationRules.TryMigrateToLatestCopy()`，对副本执行完整迁移链，再将准备好的配置交给各 `Read*State()`。
- 若副本迁移失败，加载流程必须中止，不得继续读取半迁移结果，也不得回退到旧路径静默覆盖当前主存档。
- 如果本次加载发生迁移，结束后必须重新执行一次保存，将升级后的结构落盘。
- 所有新增迁移都必须补测试，至少覆盖：
  - 旧版本输入
  - 升级后结构断言
  - 已是最新版本时无副作用
  - 副本迁移成功不污染源配置 / 副本迁移失败保留源配置原样

## 后续新增版本
1. 将 `LatestVersion` 加一。
2. 新增一个 `MigrateVxToVy(IMigrationStore cfg)`。
3. 只在该方法内处理本次版本新增字段。
4. 为该迁移补至少一个测试。

## 已实现迁移总览

| 版本 | 关键变更 |
|------|----------|
| `v1 -> v2` | `ui.submenu_active_tab` 拆分为左右页签 |
| `v2 -> v3` | 规范化 `action.mode` 的持久化结构 |
| `v3 -> v4` | 补齐 `settings.system` 容器 |
| `v4 -> v5` | 补齐装备存档容器与 `meta.last_saved_unix` |
| `v5 -> v6` | 接入灵石、炼丹、炼器、Boss、区域循环等 Melvor 扩展字段 |
| `v6 -> v7` | 引入 `mastery.levels` 子系统熟练度字典 |
| `v7 -> v8` | 补齐 7 个新增系统的运行时状态容器与体修进度字段 |
| `v8 -> v9` | 阵法从通用 recipe state 升级为专用全局状态 |
| `v9 -> v10` | 接入周天系统状态与周天永久加成字段 |
| `v10 -> v11` | 接入坊市状态容器 |
| `v11 -> v12` | 将灵田旧结构归一化到多田位真实时间快照 |
| `v12 -> v13` | 接入统计概览 `stats.player` 容器 |
| `v13 -> v14` | 归一化统计字段，补齐资源消耗与战斗/灵田长期统计默认值 |

## 已实现迁移：v5 -> v6（Melvor 扩展）

### 新增字段

| 字段路径 | 默认值 | 来源 |
|---------|--------|------|
| `resource.wallet.spirit_stones` | `0` | 灵石经济 |
| `alchemy.state.selected_recipe` | `""` | 炼丹系统 |
| `alchemy.state.progress` | `0.0` | 炼丹系统 |
| `smithing.state.target_equipment_id` | `""` | 炼器/强化 |
| `smithing.state.progress` | `0.0` | 炼器/强化 |
| `backpack.potions` | `{}` | 丹药背包 |
| `boss.runtime.defeated_zones` | `[]` | Boss 挑战 |
| `boss.runtime.current_hp` | `-1` | Boss 断点续战 |
| `level.unlocked_zone_ids` | `["lv_qi_001"]` | 区域解锁 |
| `level.zone_cycle_counts` | `{}` | 区域循环统计 |

### 迁移特殊处理
- 若 `explore.runtime.explore_progress >= 100`，迁移时归零为 `0.0`。
- 若缺失 `level.unlocked_zone_ids`，会按当前 `zone_id` 反推应已解锁的前序区域。
- 旧背包中的 `novice_breakthrough_pill` 会被移除。
- `action.mode.mode_id` 若不再合法，会回退到 `dungeon`。

## 已实现迁移：v6 -> v7（子系统熟练度）

### 新增字段
- `mastery.levels.{system_id}`：以字典形式持久化当前熟练度等级。
- 当前 `system_id` 集合：`dungeon`、`cultivation`、`alchemy`、`smithing`、`garden`、`mining`、`fishing`、`talisman`、`cooking`、`formation`、`body_cultivation`。

### 迁移特殊处理
- 若 `progress.player.advanced_alchemy_study_unlocked == true`，则将 `mastery.levels.alchemy` 至少提升到 `2`。
- `action.mode` 会统一通过 `PlayerActionStateRules.FromPersistedValues(...)` 规范化，补齐 `action_id`、`action_target_id`、`action_variant`。

## 已实现迁移：v7 -> v8（7 新系统横向扩展）

### 新增状态容器

| 字段路径 | 默认值 |
|---------|--------|
| `garden.state` | `{ selected_recipe: "", progress: 0.0, required_progress: 200.0 }` |
| `mining.state` | `{ selected_recipe: "", progress: 0.0, required_progress: 180.0, current_durability: MiningRules.DefaultNodeDurability }` |
| `fishing.state` | `{ selected_recipe: "", progress: 0.0, required_progress: 120.0 }` |
| `talisman.state` | `{ selected_recipe: "", progress: 0.0, required_progress: 100.0 }` |
| `cooking.state` | `{ selected_recipe: "", progress: 0.0, required_progress: 100.0 }` |
| `formation.state` | `{ selected_recipe: "", progress: 0.0, required_progress: 100.0 }` |
| `body_cultivation.state` | `{ selected_recipe: "", progress: 0.0, required_progress: 100.0 }` |

### 新增进度字段

| 字段路径 | 默认值 |
|---------|--------|
| `progress.player.body_cultivation_max_hp_flat` | `0` |
| `progress.player.body_cultivation_attack_flat` | `0` |
| `progress.player.body_cultivation_defense_flat` | `0` |
| `progress.player.body_cultivation_temper_count` | `0` |
| `progress.player.body_cultivation_boneforge_count` | `0` |

### 迁移特殊处理
- 本步不从旧字段推导新系统进度，只补齐缺失容器与默认值。
- `mastery.levels` 会再次对当前全部 `system_id` 做 `EnsureMissing(...)`，保证缺键场景可恢复。

## 已实现迁移：v8 -> v9（阵法状态专用化）

### 新增字段

| 字段路径 | 默认值 | 说明 |
|---------|--------|------|
| `formation.state.active_primary_id` | `""` | 当前主阵 |
| `formation.state.active_secondary_id` | `""` | 当前副阵 |
| `formation.state.crafted_ids` | `[]` | 已制作阵法列表 |
| `formation.state.inventory` | `{}` | 阵法库存 |

### 迁移特殊处理
- `formation.state.inventory` 会优先从旧背包中的阵法物品推导，当前识别的阵法 ID 为：
  - `formation_spirit_plate`
  - `formation_guard_flag`
  - `formation_harvest_array`
  - `formation_craft_array`
- 若旧档仅有 `selected_recipe`，但背包里没有对应阵法，也会将该 `selected_recipe` 以 `1` 份补入库存，避免丢失制作结果。
- `crafted_ids` 由迁移后的库存中所有 `count > 0` 的阵法 ID 自动生成。
- 若 `active_primary_id` 为空，则按以下顺序自动补齐：
  - 优先使用 `selected_recipe`（前提是库存中拥有它）
  - 否则回退到库存中的第一件阵法
- `active_secondary_id` 默认留空，由后续运行时/UI 决定是否启用副阵。

## 已实现迁移：v9 -> v10（周天系统）

### 新增字段
- `progress.player.zhoutian_max_hp_rate`
- `progress.player.zhoutian_attack_rate`
- `progress.player.zhoutian_defense_rate`
- `rhythm.state`：包含 `enabled`、`strength`、`cycle_minutes`、`current_cycle_active_seconds`、`small_cycle_count`、`total_small_cycles`、`total_grand_cycles`、`rest_remaining_seconds`、`is_grand_rest`、`total_rest_count`、`total_meditation_insights`

### 迁移特殊处理
- 周天永久加成字段默认补零，避免旧档直接获得属性收益。
- `rhythm.state` 统一补齐为可直接运行的默认结构，旧档缺少该节点时按“已开启、弱提醒、默认周期”安全启动。

## 已实现迁移：v10 -> v11（坊市系统）

### 新增字段
- `shop.state`：使用 `ShopPersistenceRules.CreateDefault()` 生成完整默认坊市状态，覆盖货架购买计数、每日限购与活跃 buff 计时字段。

### 迁移特殊处理
- 旧档缺少坊市节点时一次性补齐默认状态，不从其他系统反推购买历史。

## 已实现迁移：v11 -> v12（灵田真实时间多田位）

### 新增字段
- `garden.state`：改为 `GardenPersistenceRules.GardenSnapshot` 结构，包含最多 6 个田位、选中田位与真实时间种植快照。

### 迁移特殊处理
- 通过 `GardenPersistenceRules.FromPlainDictionary(...)` 读取旧结构，再用 `ToPlainDictionary(...)` 写回统一格式，兼容旧单进度灵田到多田位真实时间模型。

## 已实现迁移：v12 -> v13（统计概览容器）

### 新增字段
- `stats.player`：初始化为 `PlayerStatsPersistenceRules.PlayerStatsSnapshot` 默认结构，承接战斗、制作、采集、灵田与资源消耗长期统计。

### 迁移特殊处理
- 旧档不存在 `stats.player` 时补空容器；已有字段则保持原值，不做覆盖。

## 已实现迁移：v13 -> v14（统计字段扩展）

### 新增字段
- `stats.player.total_spent_lingqi`
- `stats.player.total_spent_spirit_stones`
- `stats.player.spent_spirit_stones_on_shop`
- `stats.player.spent_spirit_stones_on_seeds`
- `stats.player.spent_spirit_stones_on_other`
- 以及战斗消耗品、累计击杀、灵田自动收获等 v14 统计字段的默认值

### 迁移特殊处理
- 通过 `PlayerStatsPersistenceRules.FromPlainDictionary(...)` + `ToPlainDictionary(...)` 做一次规则层归一化，补齐新增字段并裁正负值/缺省值。

## 测试覆盖
- `tests/Xiuxian2.Tests/SaveMigrationRulesTests.cs` 已覆盖从早期版本迁移到最新版本的基础断言。
- `tests/Xiuxian2.Tests/SaveMigrationRulesTests.cs` 已覆盖 `v8 -> v9` 阵法专用状态升级。
- `tests/Xiuxian2.Tests/SaveMigrationRulesTests.cs` 已覆盖副本迁移成功不污染源配置、失败保留源配置原样的 fail-closed 行为。
- `tests/Xiuxian2.Tests/SaveRoundTripTests.cs` 已覆盖阵法、灵田与统计等关键状态的 round-trip。
