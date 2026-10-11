# 17 设计审查与评定报告

> 审查日期：2026-05-26（本会话）
> 审查范围：`docs/design/` 全部编号设计文档（00~16，共 14 份 .md）+ `AGENT_PROMPT.md` + `docs/INPUT_SYSTEM.md` + `README.md` + 关键规则层代码交叉核对
> 依据：`AGENTS.md` 唯一定义点原则——任务状态以 `10_todo.md` 为真源，数值/配方/公式以 `02_systems.md` 对应 § 为权威
> 本文件是**设计评定文档**，不是任务状态真源；任务状态与待办仍只维护在 `10_todo.md`。

---

## 0. 评定结论（TL;DR）

整体设计成熟度 **B+（良好）**：核心循环、数值口径、存档迁移、防刷护栏等主干设计扎实且与代码高度一致；
主要扣分点集中在 **边缘系统的文档滞后**（灵渔/灵膳/坊市许可/区域 3-5 新怪）与 **若干文档↔文档自相矛盾**（Boss 保底稀有度、灵膳持续口径、页签清单）。

| 维度 | 评分 | 说明 |
|------|------|------|
| 文档覆盖度 | A | 14 份编号文档覆盖愿景/核心循环/系统/数值/UI/内容模板/离线/存档/里程碑，索引完整 |
| 文档↔代码一致性 | B- | 主干（输入/境界/战斗/存档/精通/阵法/周天/事件流）已对齐；除原 ALIGN-01~04 外，第二轮实现层审查（§7）新发现：被动恢复未实装（P0）、5 加工模式离线批次产出未实现（P1）、09 首通配置矛盾（P2） |
| 文档内部一致性 | B- | 发现 20 处自相矛盾或过期口径（R-01~R-20，详见 §2），本次已全部修复 |
| 任务管理纪律 | A- | `10_todo.md` 真源维护良好，但总览与 `16_subsystem_deepening.md` 的 D-02/D-03 状态过期 |
| 数值与平衡文档 | B+ | 产消预算表、悟性 1900、灵气产消比均有推导；Boss 首杀保底稀有度一处矛盾已修复 |
| 可扩展性设计 | A- | 通用活动框架/材料注册表/存档迁移链/渐进解锁均有明确扩展路径 |

---

## 1. 审查方法

1. 通读 `docs/design/` 全部编号文档（00~16）与 `docs/INPUT_SYSTEM.md`。
2. 对关键规则层代码（`TalismanRules`、`BodyCultivationRules`、`FishingRules`、`CookingRules`、`ActivityRegistry`、`ShopRules/ShopState`、`ProgressiveUnlockRules`、`SubsystemMasteryRules`、`GardenRules`、`SmithingRules`、`AlchemyRules`、`BossEncounterRules`、`BattleTrackVisualizer`、`InputActivityRules` 等）做文档↔代码事实核对。
3. 所有任务状态以 `10_todo.md` 为准，不把代码实现反写成新真源。

---

## 2. 本次发现并已修复的设计文档缺陷

### 2.1 状态过期类（文档滞后于已落地实现）

| # | 位置 | 缺陷 | 处置 |
|---|------|------|------|
| R-01 | `16_subsystem_deepening.md` D-02 | 符箓对齐标为 P0 待实现；实际 `TalismanRules` 已有 3 符咒（破甲/疾风/炸裂），N-04 已落地 | 改为 ✅ 已完成 |
| R-02 | `16_subsystem_deepening.md` D-03 | 体修第 3 功法标为 P0 待实现；实际 `BodyCultivationRules` 已有活血诀，N-04 已落地 | 改为 ✅ 已完成 |
| R-03 | `16_subsystem_deepening.md` 总览计数 | 待实现 18 项 → 应为 16 项 | 同步计数 |
| R-04 | `10_todo.md` 总览 | "D-02/D-03 为 P0 待实施" 与 N-04 完成记录自相矛盾 | 总览改为已完成口径 |
| R-05 | `INPUT_SYSTEM.md` §快捷键 | "Ctrl+Shift+X 暂停/恢复采集（待实现）"；N-24 已接线 `InputPauseShortcut` | 移除"待实现" |
| R-06 | `02_systems.md` §11 | "灵石消费端由商店系统统一承接…在 V1.x 或 V2 补齐"；坊市 §22 已落地（v11 存档、ShopState） | 补"已由 §22 坊市承接"口径 |
| R-07 | `11_equipment_content_system.md` 头部实现状态 | "当前 v1 代码实装范围仅限最小闭环"；2026-05-26 起装备来源已含炼器锻造/化龙泊垂钓 | 补实现状态注记 |

### 2.2 文档↔文档矛盾类

| # | 位置 | 矛盾 | 处置（以何为准） |
|---|------|------|------|
| R-08 | `02_systems.md` §4.7 vs `03_progression_and_balance.md` §Boss | Boss 首杀保底装备：02 写"法器"，03 写"灵器" | 以 02 §4.7/§5 + `12_equipment_sample_qi_refining.json`（first_clear → `rarity_tier: artifact`）为准，03 改为"法器" |
| R-09 | `07_content_template.md` §8.3 vs §17 | 灵膳持续口径：§8.3 写"持续战斗场数"，§17 为"活跃输入时间 30-60min" | 以 §17 为准，§8.3 改为活跃输入时间 |
| R-10 | `07_content_template.md` §8.3 vs §18 | 阵盘："同时仅激活 1 个"；§18 为 1 主阵 + 精通 Lv3 副阵（50% 效果） | 以 §18 为准，§8.3 补副阵口径 |
| R-11 | `02_systems.md` §6 / `05_ui_style.md` §2 / `15_ui_prompts.md` §2 vs §21/§22 | 左侧页签清单缺"坊市"（代码 7 左页签含 ShopTab） | 三处统一补坊市 |
| R-12 | `02_systems.md` §11 vs §22 | 灵渔扩容：§11 写"钓点位 100/300 灵石"，§22 为"鱼塘许可 150/300/500" | 以 §22 坊市为准，§11 标注被 §22 取代 |
| R-13 | `02_systems.md` §12.2 vs 代码 | 副本精通 Lv4："Boss 全属性永久 -10%"；代码 `BossEncounterRules` 为 Boss 战中每战可看破一次（-10%） | 标注"永久"为 V2 目标口径，现行实现为每战一次 |
| R-14 | `15_ui_prompts.md` 头部页面计数 | "6 左 Tab + 1 右 Tab"合计 15 页，标 16；实际 7 左 + 2 右 = 16 | 修正计数说明 |
| R-15 | `04_milestones.md` M2 当前实现口径 | 状态机"最小集"仅 Exploring/InBattle/ExploreComplete；现行 5 态含 BossReady/BossChallenge | 补注当前 5 态口径 |
| R-18 | `02_systems.md` §3 vs §22 + 代码 | 突破丹：§3 写"V1 取消 / V2 预留"，但 §22 坊市在售"突破丹（筑基用）"2000 灵石（`ShopRules.ItemBreakthroughPill` 已接线） | §22 为现行口径：不进掉落表、坊市可购便利路径 |
| R-19 | `02_systems.md` §11 / `AGENT_PROMPT.md` §3 vs `09 JSON` | 首通灵石范围：文档写"60-200"，JSON 实际 60/90/130/180/250 按区域递增 | 以 09 JSON 为准，文档改 60-250 |
| R-20 | `AGENT_PROMPT.md` §2/§5/索引/注意事项 | 突破丹取消、悟性 3 消耗路径、schema v5、迁移表 v5→v9 全部过期 | 更新为现行口径（突破丹坊市可购、悟性唯一出口=熟练度、存档 v14、fail-closed 副本迁移） |

### 2.3 文档缺口类（代码已有、文档缺失）

| # | 位置 | 缺口 | 处置 |
|---|------|------|------|
| R-16 | `INPUT_SYSTEM.md` / `02_systems.md` §1 / `03_progression_and_balance.md` §AP | 代码已支持手柄（`JoypadButtonCount`/轴，权重 1.0/0.8，计入硬封顶与统计），文档仅描述键鼠 | 补手柄输入口径 |
| R-17 | `02_systems.md` §4.2 vs §17 | 灵膳"用完自动消耗下一份同类"与"激活新灵膳替换旧灵膳"语义未在同一处闭合（ALIGN-02 步骤 3 明确要求语义唯一） | §17 补显式语义块作为权威 |

---

## 3. 已知但未修复（属于任务，不属于文档缺陷）

以下为 `10_todo.md` 中已立项的 ALIGN/深化任务，文档与代码的差距是**待实施任务本身**，不是本次审查应擅改的口径：

- **ALIGN-01（P1）**：灵渔 4 鱼塘（灵泉浅滩/幽潭/龙涎渊/化龙泊）、容量/刷新、坊市许可解锁、专属稀有掉落——代码当前仅 3 鱼塘（垂钓灵鱼/打捞灵珠/深潭寻珍），文档 §15/§17/§22 为正式目标口径。
- **ALIGN-02（P1）**：灵膳 4 菜谱 + 单激活存档模型（`consumable.active_meal`/`cooking.active_meal_id`）——代码当前 3 菜谱 + 战斗开始自动消耗模型，文档 §17 为正式目标口径。
- **ALIGN-03（P1）**：鱼塘许可/背包扩容的效果接线——代码当前鱼塘许可与背包扩容为"记账"状态（商品说明已如实标注）。
- **ALIGN-04（P2）**：区域 3-5 新怪（`monster_yin_serpentling`/sporeling/bone_servant）表现层映射——`BattleTrackVisualizer` 仍只映射 slime/bat/spider，新怪回退 `MO`。
- **D-02/D-03**：本次审查已确认代码完成（3 符咒/3 功法），状态已同步。
- **D-04~D-20 / D-24**：`16_subsystem_deepening.md` 中的其余深化可选项，均为 V1 后续/可选增强，不属于缺陷。

---

## 4. 遗留设计决策建议（供后续迭代拍板）

1. **Boss 首杀保底奖励三处口径合一**：建议统一为——保底 1 件**法器**装备（JSON `rarity_tier: artifact`）+ 按区域 JSON 的 `first_clear` 数值（灵气/悟性/灵石递增）+ `boss_unique_material_id` 预留；`02 §4.7 / 03 §Boss / 09 JSON` 三处已对齐，后续加区域时按 09 JSON 填数即可。
2. **副本精通 Lv4 弱点洞察模型二选一**：现行"Boss 战内每战看破一次（-10%）"（`BossEncounterRules.CanApplyWeaknessInsight`）与 02 §12.2 的"全属性永久 -10%"不同；建议 V1 保留每战一次（与 UI 按钮、D-10 符箓克制同批演进），把"永久被动"归入 V2。
3. **灵膳续杯语义**：§17 已补权威语义——同种自动续杯 + 激活不同灵膳替换旧灵膳 + 同时仅 1 种；ALIGN-02 按此实现即可。
4. **手柄输入的数值权重**：代码 1.0/0.8、计入 600/min 硬封顶；建议在正式版前做一次手柄 vs 键鼠效率比对，防止手柄刷分（文档已补口径，数值是否调整留待平衡验收）。
5. **页签数量收敛**：7 左 + 2 右页签为当前实现事实；`04_milestones` 的旧"6 页签/7 页签"说法应随里程碑文档整体更新（本次仅补注）。
6. **突破丹与首通灵石**：已统一为"掉落表取消、坊市可购突破丹（2000 灵石）"与"首通灵石 60/90/130/180/250"；后续新增区域按 09 JSON 填数即可，勿再写回"60-200"。

---

## 5. 总体评定

**优点**
- 主干闭环设计自洽：输入→AP→资源→境界→探索→战斗→掉落→装备→加工→消费，11 系统蛛网 + 坊市统一消费端，经济学推导完整。
- 存档迁移链（v1→v14）逐版本文档化，fail-closed 副本迁移、幂等、单向原则清晰，是同类项目少见的高质量工程文档。
- 数值文档可执行：公式、上限、产消比、调参旋钮都有明确出处；1900 悟性在 `SubsystemMasteryRules.cs` 精确落地。
- 防刷（600/min 硬封顶）、隐私（仅计数不记录内容）、低打扰（周天/动态页/未读徽标）的产品约束贯穿始终。

**风险与改进建议**
1. **边缘系统实现滞后是当前最大风险**：灵渔/灵膳/坊市许可三条 P1 一致性命中"文档正式、代码半落地"，建议 ALIGN-01/02/03 优先于新内容开发。**第二轮审查（§7）进一步发现灵气被动恢复未实装（P0）与离线批次产出未实现（P1）——这两项不在原 ALIGN 清单内，是"文档声称已落地/设计完整但代码缺失"的隐藏缺口，优先级高于边缘系统对齐。**
2. **文档↔文档矛盾需制度化**：本次一次修复 20 处，说明缺少"改一处同步引用处"的检查；建议把 `03/04/05/07/15/AGENT_PROMPT/README` 中对 `02_systems.md` 与 JSON 样本的引用视为"派生口径"，改动 § 时顺带 grep 同步（本轮的突破丹/首通灵石/页签清单均为该问题实例）。
3. **README/TESTING 面向公开仓库**：README 的 Tab 清单与当前 7+2 页签不一致，已同步；后续 UI 变更需同步 README。
4. **手柄输入是新出现的事实**：文档已补，需在验收阶段确认其与"键鼠驱动"产品定位的兼容性（是否纳入 V1 宣传口径）。

---

## 6. 后续动作

- 本次修复均为文档口径修正，不改变 `ALIGN-01~04` 目标设计与优先级；`10_todo.md` 已同步本次审查结论（见变更日志）。
- 代码基线：`dotnet test tests/Xiuxian2.Tests/Xiuxian2.Tests.csproj` 以 `10_todo.md` 记录的 354/354 为准（本会话沙箱环境无法运行测试宿主进程，未改动任何代码）。

---

## 7. 实现层设计检查（第二轮，2026-05-26）

> 范围：场景/AutoLoad 接线、内容 JSON 配置内部一致性、数值实现 vs 设计公式、架构分层与死标记。
> 方法：逐文件核对代码事实，所有结论以 grep/读取结果为准；任务状态仍只维护在 `10_todo.md`。

### 7.1 通过项（实现层设计与文档一致）

- **场景接线**：`project.godot` 27 个 AutoLoad 覆盖全部状态/服务；`PrototypeRoot.tscn` = 主控制器 + MainBarWindow + SubmenuBookWindow + ExploreProgressController + ToastController，与 02 §6/05 双窗口 + Toast 设计一致。
- **场景节点分组**：MainBarWindow 含 Book/Badges/Battle 三组，SubmenuBookWindow 含 TopStrip/SpreadBody/BottomStrip/Actions，与 05/15 结构一致。
- **离线分段模型**：`OfflineSettlementRules` 12/8/6/3 次每分、24h 上限、>36h 守卫 ×0.25，与 13 §5/§12 完全一致。
- **周天**：25/45/60/90 分钟档位、休息灵气 +50% 已落地（`BookTabsController` / `CultivationRhythmRules.RestLingqiBonusRate = 0.50`）。
- **突破丹**：`ShopState.BreakthroughExpReductionRate = 0.10` 已在 `PlayerProgressState` 生效，与 §22 一致。
- **数值常量集中**：600/min 硬封顶、0.9/0.08 转化系数、探索 0.02%/遭遇 18%+danger×4%、战斗 18 输入/回合、Boss 20 回合等均在 `GameBalanceConstants` 集中管理。
- **材料注册表**：16 种材料与注释数量一致；每材料标注来源/消费系统，蛛网结构成立。
- **存档迁移**：v1→v14 链与 `14_save_migration.md` 一致。

### 7.2 新发现（R-21~R-30）

| # | 类型 | 发现 | 处置 |
|---|------|------|------|
| R-21 | 配置矛盾（P2） | `09_level_monster_drop_sample.json` lv_qi_001 首通发 `eq_accessory_qi_shadow_ring`（灵器/spirit、qi_2_4 段、source_stage=boss），代码实际读 09；与 02 §4.7/§5"首通保底法器"、11 §5.2.1、12 JSON `first_clear_equipment_examples`（artifact）矛盾 | 立项 `ALIGN-07` |
| R-23 | 文档缺口（已修） | 境界经验产出公式（修炼 0.35/输入直给 + 其他模式灵气×0.25 派生）在 03 中完全缺失，玩家无法从文档推算突破耗时 | 03 已补"境界经验产出"节 |
| R-24 | 实现缺口（P0） | 灵气被动恢复 +5/min 未实装：`ResourceWalletState` 无 tick、`AddLingqi` 全库仅 4 处调用；03 §9.4 曾声称"已在 TASK-37~39 落地"（声明不实），产消比 ~2.0:1 结论依赖该机制 | 文档标注 + 立项 `ALIGN-05`（P0） |
| R-25 | 实现缺口（P1） | 炼丹/炼器/符箓/烹饪/阵法离线批次产出（13 §17/§18/§18C-E）未实现：离线入口只有修炼/副本/灵田三条路径，上述模式离线全部走修炼口径；N-22 的"炼丹/符箓离线摘要"仅为标题文案 | 文档标注 + 立项 `ALIGN-06`（P1） |
| R-27 | 死标记（随 ALIGN-06） | `PlayerActionCapability.SupportsOfflineSettlement` 对所有模式恒 true 且代码无消费方；语义与 13 §2.2（矿脉/灵渔/体修不支持）相反 | 随 ALIGN-06 收口 |
| R-28 | 工程卫生 | `scenes/ui/MainBarWindow.tscn.bak`、仓库根 `fix_todo.js` 为残留文件 | 建议清理（不擅自删） |
| R-30 | 死配置（随 ALIGN-07） | 09 `boss_fields.first_kill_equipment_rarity_floor`（5 区域均 spirit）与 `boss_unique_material_id`（5 区域均空串）代码零消费 | 随 ALIGN-07 收口；07 §3.5 标注 V2 预留 |

### 7.3 误报更正

第一轮审查中基于 spill 截断文本怀疑 02 §4.4 存在"玩家基础 HP 100 / 攻击 10 / 修炼 lingqi×2"等过期示例值（原拟记 R-22/R-26）；本轮以 grep 实证当前文件已无这些行，**不构成缺陷**，从发现清单剔除。教训：spill 截断内容不可作为缺陷依据，必须以 grep/定点读取复核。

### 7.4 实现层评级

| 维度 | 评分 | 说明 |
|------|------|------|
| 架构分层 | A- | services 纯规则（可无 Node 依赖测试）/ game 控制器 / ui 分离清晰；`CraftingProgressionService` 通用化与 27 AutoLoad 分工明确 |
| 数值落地 | B | 常量集中、公式与文档主干一致；但被动恢复（P0）与境界经验产出文档化（已修）暴露了"数值设计先行、实现缺位"的盲区 |
| 内容配置 | B | 09/12 JSON 结构完整、新怪已入配置；但首通装备跨文件矛盾（R-21）与 boss_fields 死字段（R-30）说明配置缺校验闭环 |
| 离线系统 | C+ | 修炼/副本/灵田三条路径扎实；但设计已扩展到 5 个加工模式（13 §17/§18/§18C-E）从未实现，能力标记与文档相反（R-25/R-27） |
| 工程卫生 | B+ | 无 Godot 依赖泄漏进 services；存在 `.tscn.bak` / `fix_todo.js` 残留（R-28） |
