# 14 存档版本迁移规约

## 目标
- 为 `user://save_state.cfg` 建立统一的 schema 迁移入口。
- 让 `PrototypeRootController` 不再散落多个 `version < x` 分支。
- 为后续装备、统计、设置扩展提供可追加的升级链。

## 当前版本
- 最新存档版本：`v5`
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

<!-- REVIEW-FIX-P1: 预写 v5→v6 迁移规则，覆盖炼丹/炼器/Boss/灵石/4模式新字段 -->

## 预写迁移：v5 -> v6（Melvor 扩展）

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
