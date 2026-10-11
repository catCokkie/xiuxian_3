# Agent 工作规则 — xiuxian_4

## 项目与目录

- Godot 4.5.1 + C# (.NET 8) 桌面修仙挂机游戏，键鼠输入驱动修炼进度。
- `scripts/services/`：纯规则和状态类，不依赖 Godot，便于单元测试。
- `scripts/game/`：游戏控制器、规则调用和场景接线。
- `scripts/ui/`：UI 控制器、文本和素材查询。
- `scripts/tests/`、`scenes/*SmokeTest.tscn`：原生场景冒烟检查。
- `tests/Xiuxian2.Tests/`：xUnit 测试。
- `assets/`：运行时素材及索引；`tools/art/`：素材构建和校验工具。
- `docs/design/`：设计依据；`docs/art/`：素材图鉴和生成提示词。

## 唯一任务状态来源

- 任务状态、完成记录、阻塞与阶段进度只维护在 [10_todo.md](10_todo.md)。
- 实施前读取该文件中的依赖、验收标准和最新记录；不在本文件或临时计划中复述进度。
- 每次完成任务，必须在同一工作会话更新该文件，并区分已验证结果与待验收事项。
- 若其他说明与该文件的任务状态冲突，以该文件为准。

## 设计依据

实施前阅读对应文档章节；具体任务的执行顺序和依赖以 `10_todo.md` 为准。

| 文档 | 内容 |
|------|------|
| `01_core_loop.md` | 核心循环与扩展方向 |
| `02_systems.md` | 活动、副本、精通、资源和商店规则 |
| `03_progression_and_balance.md` | 成长公式、资源预算和数值平衡 |
| `05_ui_style.md` | 页面、字体、图标和通知规范 |
| `07_content_template.md` | 内容模板与字段约定 |
| `09_level_monster_drop_sample.json` | 区域、怪物、掉落与装备配置样本 |
| `11_equipment_content_system.md` | 装备模板、实例和强化规则 |
| `13_offline_settlement.md` | 离线结算规则 |
| `14_save_migration.md` | 存档字段和迁移规则 |
| `20_asset_direction.md` | 素材分类、风格、命名、来源与扩展规范 |
| `21_asset_prompt_library.md` | 各类素材提示词模板 |

## 执行与验证

1. 读取任务及设计依据，检查依赖和现有实现。
2. 改变产品行为时，同步更新对应设计文档，再实施代码与接线。
3. 涉及数值时写清公式、上限及调参入口；每个系统定义输入、输出、存档字段和 UI 入口。
4. 运行适合变更的验证。代码修改须通过 `dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj`，零失败。
5. 页面与素材变更需运行相关原生冒烟场景并检查实际截图；素材清单变更运行 `python tools/art/validate_assets.py`。
6. 将结果与验证边界记入 `10_todo.md`，不要把构建成功当成完整的人工验收。

## 实现约定

- 规则层避免引用 `Node`、`GD` 等 Godot 类型；控制器负责引擎与规则之间的衔接。
- 控制器按职责拆分，避免继续集中无关逻辑；活动能力通过 `PlayerActionCapability` 和活动注册表处理。
- 存档使用 ConfigFile；版本以 `SaveMigrationRules.LatestVersion` 为准。遵守副本迁移和失败保护，不直接修改真实玩家存档做测试。
- 冒烟场景关闭真实输入 Hook，并使用内存测试状态。
- 输入采集只保存计数和强度，不记录原始键值或鼠标轨迹。
- 共享 UI 文本统一维护在 `scripts/ui/UiText.cs`；素材查询集中在 `GameArtCatalog`，稳定 ID 与 `assets/asset_manifest.json` 对齐。
- Godot 场景文件使用 UTF-8 无 BOM；新增资源按素材规范保留来源、许可和提示词。
- 提交时包含源文件、必要的 Godot 资源元数据、设计依据和验证工具；排除构建缓存、临时计划、个人配置和备份。
