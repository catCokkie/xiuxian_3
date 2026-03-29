# 11 装备内容系统设计

> **唯一定义点原则**：炼器/强化系统的材料消耗和配方定义在 `02_systems.md` §15。本文仅定义装备品质体系、词条规则和内容样本。

> **实现状态（2026-03-29）**：本文定义的是 V1.5/M2.5 阶段的完整装备系统蓝图。当前 v1 代码实装范围仅限"最小闭环"——starter 装备 + 首通固定奖励 + 手动装备（TASK-05 完成）。完整装备内容池、配置驱动掉落、来源多元化与数据驱动配置待后续迭代。

## 0. 版本信息
- 文档版本：v0.1-draft
- 更新日期：2026-03-24
- 负责人：OpenCode Draft

---

## 1. 设计目标

### 1.1 一句话目标
- 把当前“starter 装备 + 首通固定奖励 + 背包 + 手动装备”的开发验证闭环，升级为玩家可理解、可持续扩展、可配置驱动的正式装备内容闭环。

### 1.2 解决的问题
- 当前装备来源过少，玩家无法形成稳定的换装预期。
- 当前装备信息结构偏调试向，缺少正式内容系统的来源、分层和主题感。
- 当前装备成长字段已预留，但缺少分阶段开放规则，容易在后续迭代中直接跳到过重系统。

### 1.3 本文范围
- 定义装备在 V1.5 / M2.5 阶段的内容定位、来源、稀有度、属性模型、UI 边界、配置结构建议与分期方案。
- 不直接落地完整强化、随机词条爆装、套装养成、交易/拍卖等后续系统。

---

## 2. 当前实现约束

### 2.1 已有能力
- 已具备 `BackpackState`、`EquippedItemsState`、`EquipmentStatProfile` 的最小闭环。
- 空存档会注入 starter 装备，便于直接观察战斗属性变化。
- 首通奖励可发放固定装备，并进入背包而非自动装备。
- 装备属性已接入统一战斗属性管线，可真实影响战斗结果。
- 装备状态可写入统一存档并恢复。

### 2.2 当前产品边界
- 新装备进入背包，不自动替换已装备物品。
- 仅在玩家手动触发装备动作时才替换同槽位旧装备。
- 当前 `装备情况` 页签偏验证向，尚未形成正式库存体验。
- 当前未实现完整装备内容池、随机词条、强化和最终换装 UX。

### 2.3 设计约束
- 必须延续当前“低打扰、低理解成本”的桌宠产品方向。
- 装备系统不能喧宾夺主，不能压过境界成长的主线地位。
- 装备设计应优先服务“探索推进 / 战斗稳定性 / 区域成长节奏”，而不是独立养成黑洞。

---

## 3. 系统定位

### 3.1 装备在整体玩法中的角色
- 装备是“区域推进的长期战力补偿器”。
- 主作用：提升玩家在当前推荐区域内的战斗稳定性、缩短平均战斗回合、强化不同战斗倾向。
- 次作用：为副本推进提供阶段性收获，增强“输入 -> 探索 -> 战斗 -> 掉落 -> 变强”的可感知闭环。

### 3.2 与其他系统的关系
- 与境界系统：
  - 境界仍是主成长轴。
  - 装备不决定是否能突破，只影响玩家在当前区域的效率与容错。
- 与掉落系统：
  - 装备属于掉落物的一种高价值分支，需要纳入区域掉落与保底设计。
- 与背包系统：
  - 装备是背包内的一类特殊内容，需要比材料更高的信息密度与操作优先级。
- 与 UI 系统：
  - 装备页必须从“验证口径”升级为“普通玩家也能理解的换装口径”。

---

## 4. 装备槽位与内容分层

### 4.1 第一阶段正式槽位
- `武器`：主要提供输出向成长。
- `护具`：主要提供生存向成长。
- `饰品`：主要提供轻量特化向成长。

说明：
- 当前代码已存在 `Weapon / Armor / Accessory` 三类槽位。
- 第一阶段应把 `Accessory` 从调试存在升级为正式内容槽位。

### 4.2 内容分层
- `装备模板（template）`
  - 定义部位、主题、基础预算、允许属性池、推荐区域。
- `装备实例（instance）`
  - 玩家背包/已装备中的具体那一件装备。
  - 包含稀有度、主属性、副属性、来源信息、强化等级等实例化结果。
- `装备系列（series）`
  - 用于内容主题归类，例如“洞窟试炼系”“阴木护体系”。
  - 第一阶段不强制绑定完整套装效果，只先承担主题与掉落分组作用。

---

## 5. 来源设计

### 5.1 装备来源分层
- `starter`
  - 开局保底装备，仅用于启动第一轮战斗体验。
  - 不进入正常掉落池。
- `首通奖励`
  - 继续保留，但从“固定一件装备”升级为“首通保底件 / 首通装备箱 / 定向部位奖励”。
- `普通怪掉落`
  - 低概率产出 `俗器 / 法器`。
  - 主要作用是提供基础替换件与惊喜感。
- `精英 / Boss 掉落`
  - 更高概率产出 `法器 / 灵器`。
  - 更容易掉出该区域主题系列的装备。
- `材料兑换`
  - 使用当前区域材料兑换指定部位基础装备。
  - 作用是平衡随机性，避免脸黑导致长期无替换件。

### 5.2 第一阶段推荐来源比例
- 普通怪：装备掉率低，偏补充。
- 精英：装备掉率中等，是区域稳定来源。
- Boss：装备掉率高，承担“本区毕业前的里程碑奖励”。
- 兑换：作为兜底，而不是主获取方式。

#### 5.2.1 来源职责表
| 来源 | 主要职责 | 主掉落层 | 体验目标 |
|------|----------|----------|----------|
| starter | 保证开局可战斗 | `俗器` | 解决“开局没装穿”的问题 |
| 首通奖励 | 给玩家第一次明确升级 | `法器` | 形成首次换装记忆点 |
| 普通怪 | 补充基础替换件 | `俗器`，少量 `法器` | 让日常推进有小惊喜 |
| 精英怪 | 提供稳定成长件 | `法器`，小概率 `灵器` | 让玩家感觉“这一战值得打” |
| Boss | 提供里程碑掉落 | 稳定 `法器`，较高概率 `灵器` | 形成区域毕业前高光时刻 |
| 区域兑换 | 兜底指定部位 | `俗器 / 法器` | 避免脸黑长期无提升 |

#### 5.2.2 第一阶段建议掉装概率口径
说明：以下为“掉出装备件”的建议参考，不含材料与货币掉落；最终以区域预算和实际回归测试结果微调。

| 来源 | 装备掉落概率 | 说明 |
|------|--------------|------|
| 普通怪 | `5% ~ 12%` | 主要让玩家偶尔看到可替换件，不承担主毕业来源 |
| 精英怪 | `20% ~ 35%` | 当前区域最稳定的正式装备来源 |
| Boss | `60% ~ 100%` | 建议至少有一次装备级奖励感知 |
| 首通奖励 | `100%` | 直接发保底件或保底装备箱 |

#### 5.2.3 第一阶段建议稀有度分布
| 来源 | 俗器 | 法器 | 灵器 | 宝器 |
|------|------|------|------|------|
| 普通怪 | 80% | 20% | 0% | 0% |
| 精英怪 | 15% | 75% | 10% | 0% |
| Boss | 0% | 70% | 30% | 0% |
| 首通奖励 | 0% | 100% | 0% | 0% |
| 区域兑换 | 40% | 60% | 0% | 0% |

说明：
- 第一阶段不建议让普通怪直接掉 `灵器`。
- `灵器` 应明确绑定精英/Boss 的高价值时刻，避免日常刷普通怪稀释稀有感。
- `宝器` 完全不进入第一阶段常规掉落池。

### 5.3 当前不做的来源
<!-- MELVOR-CHANGE: 锻造合成已升级为 V1 后续“炼器/强化系统”，从此列表移除 -->
- ~~锻造合成~~（→ 已升级为 `炼器/强化系统`，见 `02_systems.md` 系统 10 和下方 5.4 节）
- 拍卖/交易
- 活动限时装备
- 多货币商店抽取

<!-- MELVOR-CHANGE: 新增炼器/强化来源，参考 Melvor Idle Smithing -->
### 5.4 炼器 / 强化系统（V1 后续新增来源）

设计灵感：参考 Melvor Idle 的 Smithing，让副本掉落的碎片/碎符材料获得消耗出口。

#### 5.4.1 强化规则
- 操作：选择已有装备 → 消耗材料 → 键鼠输入驱动强化进度条 → 完成后 `EnhanceLevel + 1`。
- 强化上限 = `稀有度等级 × 3`：
  - `俗器`（Tier 1）：最高 +3
  - `法器`（Tier 2）：最高 +6
  - `灵器`（Tier 3）：最高 +9
  - `宝器`（Tier 4）：最高 +12
- 每级强化提升装备所有基础属性 **8%**（乘算叠加）。
  - 例：`AttackFlat = 10` 的法器强化到 +3 后 → `AttackFlat = 10 × 1.08³ ≈ 12.6`。
- 强化不会失败，无需保护道具（低打扰设计原则）。

#### 5.4.2 强化材料需求
| 强化等级段 | 碎片消耗 | 碎符消耗 | 灵气消耗 | 输入需求（键鼠事件） |
|-----------|---------|---------|---------|-------------------|
| +1 ~ +3 | ×3 | ×0 | ×30 | 100 + level × 50 |
| +4 ~ +6 | ×5 | ×1 | ×80 | 100 + level × 50 |
| +7 ~ +9 | ×8 | ×3 | ×150 | 100 + level × 50 |
| +10 ~ +12 | ×12 | ×5 | ×250 | 100 + level × 50 |

#### 5.4.3 与现有代码的关系
- `EquipmentInstanceData.EnhanceLevel` 字段已存在但未消费 → 此系统将其激活。
- `EquipmentStatProfile` 需新增 `GetEnhancedValue(baseValue, enhanceLevel)` 方法。
- 战斗属性管线需在最终计算时应用强化倍率。

#### 5.4.4 炼器模式 UI
- 玩家在底栏切换到"炼器"模式后：
  - 子菜单"装备情况"页新增"强化"入口。
  - 选择目标装备后显示：当前等级、下一级材料需求、属性预览。
  - 底栏右下显示强化进度条。

---

## 6. 稀有度与强度层

### 6.1 稀有度定义
- `俗器`（Tier 1）
  - 世俗工艺或粗炼兵甲。
  - 只提供单一基础属性，不承载修仙感叙事。
- `法器`（Tier 2）
  - 已能导引灵气，是炼气期的主力装备层。
  - 作为第一阶段最常见、最稳定的正式装备档位。
- `灵器`（Tier 3）
  - 带有更明确的灵性与主题感。
  - 作为精英/Boss 的主要高价值掉落层。
- `宝器`（Tier 4）
  - 明显高于炼气期常规装备的里程碑档位。
  - 主要作为后续境界、长期成长和稀有目标预留。

### 6.2 稀有度命名原则
- 命名优先贴近修仙题材，而不是直接沿用通用 RPG 的“白绿蓝紫”。
- 稀有度名称既要有世界观味道，也要让玩家一眼能判断上下级。
- 第一阶段只保留 4 档，避免过早引入“玄器 / 圣器 / 仙器”等过高层级名词，导致后续空间被挤压。

### 6.3 第一阶段开放范围
- 实际掉落开放到 `灵器`。
- `宝器` 作为后续高层区域和长期成长预留。
- `俗器` 主要来自 starter、普通怪低概率掉落、兑换基础件。
- `法器` 是第一阶段的主流追求层。
- `灵器` 是当前区域毕业前后的高价值目标层。

### 6.4 稀有度作用
- 决定基础属性预算区间。
- 决定副属性开放数量上限。
- 决定后续强化潜力上限。
- 决定装备在 UI 中的展示层级和掉落提示权重。

### 6.5 稀有度体验定位
- `俗器`
  - 解决“有没有装备”的问题。
  - 玩家看到它时，重点感受是“能换、能用、能起步”。
- `法器`
  - 解决“该区域是否有稳定成长”的问题。
  - 玩家看到它时，重点感受是“这才是正式装备”。
- `灵器`
  - 解决“该区域有没有值得追的高价值件”的问题。
  - 玩家看到它时，重点感受是“明显的里程碑掉落”。
- `宝器`
  - 解决“长期成长有没有远目标”的问题。
  - 第一阶段不承担日常主流掉落责任。

### 6.6 强度原则
- 新区域的低稀有装备，应大概率优于旧区域的同部位基础装备。
- 同区域内，高稀有装备优于低稀有装备，但差距不能过大，避免“一件装决定所有体验”。
- 第一阶段控制装备总战力影响，使其更像“稳定提效”而非“数值碾压”。

### 6.7 掉落分布建议
- 普通怪：`俗器` 为主，少量 `法器`
- 精英怪：`法器` 为主，小概率 `灵器`
- Boss：稳定 `法器`，较高概率 `灵器`
- 兑换：基础 `俗器 / 法器`

### 6.8 UI 呈现建议
- 稀有度不只显示颜色，还应在文案中直接写出档位名。
- 第一阶段颜色建议：
  - `俗器`：灰褐 / 亚麻
  - `法器`：青绿
  - `灵器`：金青或冷金
  - `宝器`：朱金
- 不建议使用 MMO 常见的紫色神装视觉，以免与整体桌宠卷轴气质冲突。

---

## 7. 属性模型

### 7.1 属性分层
- `基础属性`
  - 由装备模板直接定义。
  - 稳定、可预期，是玩家理解装备强弱的核心。
- `附加属性`
  - 由掉落实例生成。
  - 第一阶段只做轻量差异化，不做大幅随机。

### 7.2 部位属性方向
- 武器：
  - 主属性偏 `攻击 / 暴击 / 速度`
- 护具：
  - 主属性偏 `生命 / 防御`
- 饰品：
  - 主属性偏 `暴击 / 暴伤 / 速度 / 低强度资源结算加成`

### 7.3 第一阶段词条规则
- 每件装备固定 1 个主属性。
- 副属性数量：
  - `俗器`：0
  - `法器`：0-1
  - `灵器`：1
  - `宝器`：2（后续开放，当前阶段不实装）
- 暂不开放复杂词条池。

### 7.4 第一阶段允许的属性类型
- 战斗类：
  - `MaxHpFlat`
  - `AttackFlat`
  - `DefenseFlat`
  - `SpeedFlat`
  - `CritChanceDelta`
  - `CritDamageDelta`
- 谨慎开放的资源类：
  - `灵气结算 +x%`（如果需要，建议只给饰品，且数值非常保守）

### 7.5 第一阶段禁止的词条类型
- 掉落率提升
- 保底阈值修改
- 多倍结算
- 跨系统复合收益（如“击杀后额外悟性并降低输入需求”）

---

## 8. 掉落与区域成长规则

### 8.1 区域主题
- 每个区域定义：
  - `主装备主题` 1 套
  - `副装备主题` 1 套
- 主主题负责构建区域记忆点，副主题负责补充多样性。

### 8.2 掉落分布建议
- 普通怪：材料为主，装备为辅。
- 精英怪：材料 + 装备混合，倾向主主题。
- Boss：高价值掉落，倾向灵器或保底装备件。

#### 8.2.1 区域阶段掉落表（建议口径）
| 区域阶段 | 普通怪主层 | 精英主层 | Boss 主层 | 兑换层 | 设计意图 |
|----------|------------|----------|-----------|--------|----------|
| 新手区 / 首区 | `俗器` | `法器` | `法器` | `俗器` | 让玩家先建立“有装可换”的基础认知 |
| 当前主刷区 | `俗器 / 法器` | `法器` | `法器 / 灵器` | `法器` | 让玩家稳定积累本区正式装备 |
| 区域毕业前 | `法器` 少量 | `法器 / 灵器` | `灵器` 倾向 | `法器` | 让玩家在毕业前追逐明确的高价值件 |

#### 8.2.2 部位分布建议
| 来源 | 武器 | 护具 | 饰品 |
|------|------|------|------|
| 普通怪 | 40% | 40% | 20% |
| 精英怪 | 35% | 35% | 30% |
| Boss | 34% | 33% | 33% |
| 首通奖励 | 优先补玩家当前空缺槽位 | 优先补玩家当前空缺槽位 | 不建议首阶段首通奖励主打饰品 |

说明：
- 第一阶段不建议让饰品过早成为最主流掉落，以免资源/特化词条过快稀释基础战斗成长。
- 若玩家某槽位长期为空，可对首通奖励或兑换配方做“缺口优先”修正。

#### 8.2.3 区域毕业判断建议
一个区域可视为“装备层面基本毕业”，需满足以下至少两项：
- 三槽位都已有本区 `法器` 及以上装备。
- 至少有 1 件 `灵器`。
- 玩家平均战斗回合数相较初入该区下降明显。
- 同区普通战斗胜率与稳定性进入目标区间。

### 8.3 掉装保底思路
- 第一阶段不对“极品词条”做保底。
- 仅对“指定战斗次数内至少出 1 件装备”设置轻量保底。
- 更稳妥的正式保底应来自“区域材料兑换指定部位基础装备”。

### 8.4 区域预算
- 每个区域应给一组装备预算区间：
  - 武器预算
  - 护具预算
  - 饰品预算
- 掉落时在预算区间内取值，而不是完全自由生成。
- 这样能保证换装节奏平滑，便于调参与配置校验。

---

## 9. UX 与信息展示边界

### 9.1 装备页第一阶段目标
- 玩家在不查文档的情况下，也能理解：
  - 这件装备是什么部位
  - 稀有度如何
  - 比当前装备强还是弱
  - 来自哪里
  - 装上后会带来什么变化

### 9.2 背包装备至少展示的信息
- 名称
- 部位
- 稀有度
- 主属性摘要
- 副属性摘要（若存在）
- 来源标签（首通 / 普通 / 精英 / Boss / 兑换）
- 对比结果（优于当前 / 持平 / 偏弱）

### 9.3 页签结构建议
- `已装备总览`
  - 三个槽位当前装备
  - 核心总战力变化摘要
- `候选替换列表`
  - 显示背包内可替换的同部位装备
- `选中详情`
  - 当前装备 vs 候选装备的属性对比

### 9.4 操作边界
- 保持现有规则：新装备默认进入背包，不自动替换。
- 装备动作必须由玩家手动触发。
- 第一阶段不做“一键最优装备”。
- 若后续增加分解/锁定，应作为第二阶段内容，不应在第一阶段强塞入口。

### 9.5 获得反馈
- 新获得装备时，战斗结算或日志区域应给出更明确提示。
- 推荐增加轻量入口：`查看装备`。
- 不建议强制弹窗，以免打断桌宠主体验。

---

## 10. 数据结构建议

### 10.1 在现有 `EquipmentStatProfile` 基础上的扩展方向
- 当前字段：
  - `EquipmentId`
  - `DisplayName`
  - `Slot`
  - `Modifier`
  - `SetTag`
  - `Rarity`
  - `EnhanceLevel`
  - `IsEquipped`

### 10.2 建议新增字段
- `EquipmentTemplateId`
- `SourceType`
- `SourceLevelId`
- `SeriesId`
- `MainStatKey`
- `MainStatValue`
- `SubStats[]`
- `PowerBudget`
- `ObtainedUnix`

### 10.3 配置层建议新增的数据表
- `equipment_templates[]`
  - 定义部位、显示名、主题、基础预算、主属性池、副属性池
- `equipment_series[]`
  - 定义系列名称、主题标签、区域归属、后续套装预留
- `equipment_exchange_recipes[]`
  - 定义区域材料 -> 指定部位装备的兑换关系
- `equipment_drop_entries[]` 或在现有掉落表中增加装备条目
  - 让装备正式进入配置驱动掉落系统

### 10.4 JSON 配置草案

#### 10.4.1 `equipment_templates[]`
作用：定义“会掉落出什么样的装备”。这是装备内容系统的核心模板表。

建议字段：
- `equipment_template_id`: 模板 ID，唯一键
- `display_name`: 显示名称
- `slot`: `weapon / armor / accessory`
- `series_id`: 归属系列 ID
- `rarity_tier`: `common_tool / artifact / spirit / treasure` 或直接落地为 `俗器 / 法器 / 灵器 / 宝器`
- `realm_band`: 推荐境界带，如 `qi_1_3`
- `source_stage`: `starter / normal / elite / boss / exchange / first_clear`
- `main_stat_pool`: 主属性池
- `sub_stat_pool`: 副属性池
- `power_budget_min`
- `power_budget_max`
- `flavor_tags`: 用于主题和后续筛选
- `icon_id`: 图标资源键（预留）

推荐示例：

```json
{
  "equipment_template_id": "eq_weapon_qi_outer_moss_blade",
  "display_name": "苔锋短刃",
  "slot": "weapon",
  "series_id": "series_qi_outer_cave",
  "rarity_tier": "artifact",
  "realm_band": "qi_1_3",
  "source_stage": "elite",
  "main_stat_pool": [
    { "stat": "attack_flat", "weight": 70, "min": 4, "max": 6 },
    { "stat": "speed_flat", "weight": 30, "min": 2, "max": 4 }
  ],
  "sub_stat_pool": [
    { "stat": "crit_chance_delta", "weight": 60, "min": 0.01, "max": 0.02 },
    { "stat": "speed_flat", "weight": 40, "min": 1, "max": 2 }
  ],
  "power_budget_min": 12,
  "power_budget_max": 16,
  "flavor_tags": ["cave", "moss", "yin"],
  "icon_id": "weapon_short_blade_01"
}
```

#### 10.4.2 `equipment_series[]`
作用：定义装备系列主题，供掉落池、UI、后续套装预留和区域记忆点使用。

建议字段：
- `series_id`
- `series_name`
- `theme_tags`
- `bind_level_ids`
- `primary_slots`: 该系列更偏向哪些槽位
- `rarity_focus`: 该系列主打稀有度层级
- `description`
- `future_set_bonus_id`: 未来套装效果预留字段

推荐示例：

```json
{
  "series_id": "series_qi_outer_cave",
  "series_name": "幽泉洞窟试炼系",
  "theme_tags": ["cave", "wet", "yin-cold"],
  "bind_level_ids": ["lv_qi_001", "lv_qi_002"],
  "primary_slots": ["weapon", "armor"],
  "rarity_focus": ["artifact", "spirit"],
  "description": "偏向攻击与生存均衡的洞窟探索装备。",
  "future_set_bonus_id": ""
}
```

#### 10.4.3 `equipment_exchange_recipes[]`
作用：定义区域材料兑换装备，作为装备成长的正式兜底方案。

建议字段：
- `recipe_id`
- `level_id`
- `output_template_id`
- `output_rarity_tier`
- `output_slot`
- `cost_spirit_stones`
- `unlock_condition`: 如 `first_clear` / `boss_clear` / `level_unlocked`
- `notes`

推荐示例：

```json
{
  "recipe_id": "recipe_qi_outer_weapon_01",
  "level_id": "lv_qi_001",
  "output_template_id": "eq_weapon_qi_outer_moss_blade",
  "output_rarity_tier": "artifact",
  "output_slot": "weapon",
  "cost_spirit_stones": 80,
  "unlock_condition": "first_clear",
  "notes": "首区武器兜底配方。"
}
```

#### 10.4.4 掉落表整合方案
推荐不要单独再做一套完全平行的 `equipment_drop_tables[]`，而是在现有 `drop_tables[]` 中增加装备条目类型。

建议扩展字段：
- `entry_type`: `item / equipment_template / equipment_box`
- `equipment_template_id`: 当 `entry_type=equipment_template` 时启用
- `equipment_rarity_override`: 可选；允许某些来源强制限定稀有度
- `slot_bias`: 可选；用于 Boss 或首通奖励偏向缺失槽位
- `series_bias`: 可选；提高区域主题系列命中率

推荐条目示例：

```json
{
  "entry_type": "equipment_template",
  "equipment_template_id": "eq_weapon_qi_outer_moss_blade",
  "weight": 18,
  "min_qty": 1,
  "max_qty": 1,
  "equipment_rarity_override": "artifact",
  "slot_bias": "weapon",
  "series_bias": "series_qi_outer_cave"
}
```

#### 10.4.5 关卡首通奖励整合方案
当前 `levels[].rewards.first_clear.items[]` 已能发放普通物品，正式装备系统建议新增以下两种口径之一：

- 方案 A：直接支持装备模板奖励
  - `equipment_templates[]`
- 方案 B：支持“首通装备箱”
  - 箱子本身仍走 item，打开后按缺口/部位规则生成装备

推荐第一阶段优先采用方案 A，简单直接。

示例：

```json
{
  "first_clear": {
    "lingqi": 120,
    "insight": 20,
    "spirit_stones": 60,
    "items": [],
    "equipment_templates": [
      { "equipment_template_id": "eq_armor_qi_outer_moss_robe", "rarity_tier": "artifact", "qty": 1 }
    ]
  }
}
```

#### 10.4.6 稀有度字段落地建议
为了兼容代码与配置演进，建议存档和 JSON 内部字段用稳定英文枚举，UI 再映射为中文：

| 配置枚举 | UI 文案 |
|----------|---------|
| `common_tool` | `俗器` |
| `artifact` | `法器` |
| `spirit` | `灵器` |
| `treasure` | `宝器` |

优点：
- 便于后续代码 switch / 迁移 / 校验
- 不受 UI 文案调整影响
- 与现有 JSON 英文键风格一致

### 10.5 配置校验建议
- 必须检查：
  - 同区域是否缺少某部位来源
  - 同稀有度预算是否越级
  - 主/副属性池是否为空
  - 掉落来源与模板 ID 是否正确绑定
  - 兑换配方是否存在无来源材料或无消费出口

### 10.6 C# 数据模型草案
目标：在不破坏现有 `EquipmentStatProfile`、`BackpackState`、`EquippedItemsState` 和 `LevelConfigLoader` 主结构的前提下，引入“模板 -> 实例 -> 存档”的正式模型。

#### 10.6.1 推荐枚举
```csharp
public enum EquipmentRarityTier
{
    CommonTool,
    Artifact,
    Spirit,
    Treasure
}

public enum EquipmentSourceStage
{
    Starter,
    Normal,
    Elite,
    Boss,
    Exchange,
    FirstClear
}

public enum EquipmentDropEntryType
{
    Item,
    EquipmentTemplate,
    EquipmentBox
}
```

#### 10.6.2 模板层模型
```csharp
public readonly record struct EquipmentStatRollRange(
    string Stat,
    int Weight,
    double Min,
    double Max);

public sealed record EquipmentTemplateData(
    string EquipmentTemplateId,
    string DisplayName,
    EquipmentSlotType Slot,
    string SeriesId,
    EquipmentRarityTier RarityTier,
    string RealmBand,
    EquipmentSourceStage SourceStage,
    IReadOnlyList<EquipmentStatRollRange> MainStatPool,
    IReadOnlyList<EquipmentStatRollRange> SubStatPool,
    int PowerBudgetMin,
    int PowerBudgetMax,
    IReadOnlyList<string> FlavorTags,
    string IconId);

public sealed record EquipmentSeriesData(
    string SeriesId,
    string SeriesName,
    IReadOnlyList<string> ThemeTags,
    IReadOnlyList<string> BindLevelIds,
    IReadOnlyList<EquipmentSlotType> PrimarySlots,
    IReadOnlyList<EquipmentRarityTier> RarityFocus,
    string Description,
    string FutureSetBonusId);

public sealed record EquipmentExchangeRecipeData(
    string RecipeId,
    string LevelId,
    string OutputTemplateId,
    EquipmentRarityTier OutputRarityTier,
    EquipmentSlotType OutputSlot,
    IReadOnlyList<ItemCostData> CostItems,
    string UnlockCondition,
    string Notes);
```

#### 10.6.3 掉落整合模型
```csharp
public sealed record EquipmentDropEntryData(
    EquipmentDropEntryType EntryType,
    string EquipmentTemplateId,
    EquipmentRarityTier? EquipmentRarityOverride,
    EquipmentSlotType? SlotBias,
    string SeriesBias,
    int Weight,
    int MinQty,
    int MaxQty);
```

建议做法：
- 现有 `drop_tables[].entries[]` 仍然保留统一入口。
- 解析时将普通物品条目与装备条目分别识别，再在 roll 阶段统一处理。

#### 10.6.4 实例层模型
建议不要直接让 `EquipmentStatProfile` 承担全部配置与来源信息，而是在运行时新增实例层，再由实例投影到当前战斗属性模型。

```csharp
public sealed record EquipmentSubStatData(
    string Stat,
    double Value);

public sealed record EquipmentInstanceData(
    string EquipmentId,
    string EquipmentTemplateId,
    string DisplayName,
    EquipmentSlotType Slot,
    string SeriesId,
    EquipmentRarityTier RarityTier,
    EquipmentSourceStage SourceStage,
    string SourceLevelId,
    string MainStatKey,
    double MainStatValue,
    IReadOnlyList<EquipmentSubStatData> SubStats,
    int EnhanceLevel,
    int PowerBudget,
    long ObtainedUnix,
    bool IsEquipped);
```

#### 10.6.5 与现有 `EquipmentStatProfile` 的关系
推荐短期策略：
- `EquipmentInstanceData` 负责内容与来源表达
- `EquipmentStatProfile` 继续作为“战斗属性投影对象”

建议增加转换规则类：

```csharp
public static class EquipmentInstanceRules
{
    public static EquipmentStatProfile ToStatProfile(EquipmentInstanceData instance) { ... }
    public static CharacterStatModifier BuildModifier(EquipmentInstanceData instance) { ... }
}
```

这样做的好处：
- 不需要立刻重写战斗规则层
- 现有 `CharacterStatRules`、`BackpackState`、`EquippedItemsState` 仍可逐步兼容
- 后续可平滑迁移存档格式

#### 10.6.6 存档层建议
现有持久化围绕 `EquipmentProfileCodec` 和 `EquipmentProfilePersistenceRules`，建议新增正式实例持久化结构：

```csharp
public readonly record struct EquipmentInstancePersistenceData(
    string EquipmentId,
    string EquipmentTemplateId,
    string DisplayName,
    string Slot,
    string SeriesId,
    string RarityTier,
    string SourceStage,
    string SourceLevelId,
    string MainStatKey,
    double MainStatValue,
    Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> SubStats,
    int EnhanceLevel,
    int PowerBudget,
    long ObtainedUnix,
    bool IsEquipped);
```

存档迁移建议：
- 第一阶段兼容读取旧 `EquipmentStatProfile`
- 若存档中缺少 `EquipmentTemplateId` 等新字段，则按旧逻辑回填为 `legacy_import`
- 新存档写入时优先使用正式实例结构

### 10.7 加载与运行流程草案

#### 10.7.1 推荐服务拆分
- `LevelConfigLoader`
  - 继续负责关卡、怪物、掉落表总配置加载
  - 可扩展读取 `equipment_templates[]`、`equipment_series[]`、`equipment_exchange_recipes[]`
- 新增 `EquipmentContentIndex`
  - 负责按 ID 建索引、做模板查询和校验
- 新增 `EquipmentGenerationRules`
  - 负责从模板 + 稀有度 + 来源生成实例
- 新增 `EquipmentRewardResolver`
  - 负责在战斗掉落、首通奖励、兑换时决定最终发放哪些装备实例

#### 10.7.2 配置加载顺序
1. `LevelConfigLoader.LoadConfigFromText()` 解析根 JSON
2. 先解析 `levels[] / monsters[] / drop_tables[]`
3. 再解析 `equipment_series[]`
4. 再解析 `equipment_templates[]`
5. 再解析 `equipment_exchange_recipes[]`
6. 建立索引：
   - `templateById`
   - `seriesById`
   - `recipesByLevelId`
7. 执行跨表校验
8. 发出 `ConfigLoaded`

原因：
- 模板依赖系列
- 掉落条目和首通奖励可能依赖模板
- 兑换配方依赖模板和关卡

#### 10.7.3 掉落生成流程
```text
战斗结算
-> 读取 drop_table_id
-> 统一 roll entries
-> 若 entry_type=item，按旧逻辑发物品
-> 若 entry_type=equipment_template，进入装备生成规则
-> 生成 EquipmentInstanceData
-> 投影为 EquipmentStatProfile 或直接存入实例背包
-> 写入 BackpackState
-> 输出给战斗日志 / 装备提示
```

#### 10.7.4 首通奖励流程
```text
关卡首次通关
-> 读取 levels[].rewards.first_clear.equipment_templates[]
-> 按模板 ID + 指定稀有度生成实例
-> 检查背包中是否已有同一唯一奖励件
-> 发放到 BackpackState
-> 在日志中标记来源为 FirstClear
```

#### 10.7.5 兑换流程
```text
玩家打开兑换入口
-> 读取当前 level_id 对应的 equipment_exchange_recipes[]
-> 校验 unlock_condition
-> 校验 cost_items
-> 扣除材料
-> 生成指定模板实例
-> 发放到背包
```

#### 10.7.6 UI 数据流建议
- `BookTabsController`
  - 不直接理解模板池与掉落规则
  - 只消费“背包装备实例 + 当前已装备实例 + 对比结果”
- 推荐新增 `EquipmentPresentationRules`
  - 负责把 `EquipmentInstanceData` 转成 UI 友好的摘要文本和对比结构

#### 10.7.7 第一阶段最低实现路径
为了降低改动风险，建议按以下顺序接入：
1. 先让 `LevelConfigLoader` 能读装备配置并校验
2. 再实现“模板 -> 实例 -> 发背包”
3. 再扩展 `BackpackState` 的正式实例存储
4. 再升级装备页 UI
5. 最后做首通奖励和兑换正式接入

这样可以保证：
- 现有战斗和存档逻辑不会一次性重写
- 配置校验页能优先发挥价值
- 每一步都能独立回归验证

---

## 11. 数值与平衡护栏

### 11.1 战力影响上限
- 第一阶段装备不应让战斗强度差异过大。
- 建议控制同区域“完整换装”与“starter 装”的战斗表现差距在可感知但不过分夸张的区间内。

### 11.2 体验目标
- 玩家在 1 个区域内，应该能稳定完成：
  - 至少 1 次明确换装
  - 至少 1 次“普通件 -> 更优件”的感知升级
- 玩家不应因“完全没掉装”而产生长期停滞感。

### 11.3 经济风险控制
- 若开放资源向词条，其总收益必须纳入现有掉落 soft-cap / daily-cap 监控口径。
- 装备系统不能绕开现有防刷护栏。

---

## 12. 分期方案

### 12.1 阶段 A：正式装备闭环
- 目标：从开发验证版升级为玩家可理解的正式闭环。
- 包含：
  - 三槽位正式化
  - 稀有度正式化
  - 普通 / 精英 / Boss 装备来源
  - 区域材料兑换指定部位
  - 装备页信息层升级
  - 同槽位装备对比
- 不包含：
  - 强化
  - 套装效果
  - 多词条深度随机

### 12.2 阶段 B：轻度深度扩展
- 包含：
  - 副属性扩展到 1-2 条
  - 饰品正式掉落池完善
  - 系列主题强化
  - 分解与回收

### 12.3 阶段 C：长期养成扩展
- 包含：
  - 强化系统
  - 套装 / 系列加成
  - 高稀有装备成长路线
  - 更复杂的资源向词条与经济平衡

---

## 13. 推荐落地口径（建议直接采用）
- 三槽位正式化：武器 / 护具 / 饰品
- 四档稀有度：俗器 / 法器 / 灵器 / 宝器
- 第一阶段开放到灵器
- 普通怪少量掉装，精英/Boss 主掉装
- 区域材料可兑换指定部位基础装备
- 每件装备 `1 主属性 + 最多 1 副属性`
- 不上强化、不上完整套装、不上复杂经济词条
- 新装备仍先进背包，必须手动装备

---

## 14. 验收标准
- 玩家能明确理解装备来源、部位、稀有度与替换价值。
- 每个区域至少提供 3 槽位中的稳定装备来源或兑换方案。
- 装备掉落与区域推荐强度存在清晰梯度，不出现严重越级爆装。
- 装备系统不破坏当前境界成长主线，也不绕开现有防刷与掉落限制。
- 后续代码实现可以直接映射到配置表、状态存档与 UI 展示层。

---

## 15. 子系统装备关联设计

> 目标：将装备系统从"仅副本掉落"扩展为与 12 个子系统全面联动，让每个子系统在装备成长链中都有明确作用，形成 Melvor 式的互联蛛网。

### 15.1 关联分层

每个子系统与装备系统的关联归入以下四种角色之一：

| 角色 | 含义 | 示例 |
|------|------|------|
| **直接来源** | 该系统可直接产出/掉落装备实例 | 副本掉落、灵渔沉水装备 |
| **材料供给** | 该系统产出的材料被炼器/锻造消耗 | 矿脉→寒铁→强化；灵田→灵花→炼器辅料 |
| **装备消耗品** | 该系统产出可临时修改装备属性的物品 | 炼丹→洗练丹；符箓→附魔符 |
| **增益协同** | 该系统的产出/效果与装备属性加法叠加 | 悟道永久属性、体修永久HP、阵盘被动、灵膳Buff |

### 15.2 十二系统 × 装备关联矩阵

| # | 子系统 | 关联角色 | 装备链条说明 | 新增装备内容 |
|---|--------|---------|-------------|-------------|
| 1 | **副本** | 直接来源 | 主掉落来源（普通/精英/Boss），驱动"击杀→掉落→换装"核心循环 | ✅ 已有 2 系列 4 模板，扩充见 §15.3 |
| 2 | **修炼** | 增益协同 | 境界等级门控装备稀有度可穿戴；灵气是炼器/锻造的通用燃料 | 新增境界门控规则：灵器需炼气三层、宝器需筑基 |
| 3 | **炼丹** | 装备消耗品 | 新增 2 种装备相关丹药：洗练丹（重置副属性）、铸魂丹（临时提升装备效果） | 2 个新配方 |
| 4 | **炼器** | 直接来源 | 强化系统已有；**新增锻造子路线**：消耗矿石直接锻造基础装备 | 锻造配方 ×6（3 槽位 × 2 稀有度） |
| 5 | **灵田** | 材料供给 | 灵花作为装备附魔/洗练的辅料；灵果作为高阶强化催化剂 | 灵花→洗练丹辅料；灵果→+7 以上强化催化剂 |
| 6 | **矿脉** | 材料供给 | 寒铁/灵玉/秘银是炼器锻造和强化的核心金属材料 | 矿石→锻造配方主材；灵玉→饰品锻造专用 |
| 7 | **灵渔** | 直接来源 | 垂钓低概率捞出沉水装备（饰品为主）；灵珠作为饰品强化材料 | 沉水装备系列 ×3；灵珠→饰品专属强化材料 |
| 8 | **符箓** | 装备消耗品 | 制作装备附魔符（临时属性增幅，持续 N 场战斗后消散） | 附魔符 ×3（攻/防/速） |
| 9 | **烹饪** | 增益协同 | 灵膳 Buff 与装备属性加法叠加；不直接修改装备 | 无新增装备内容（保持系统边界清晰） |
| 10 | **阵法** | 增益协同 | 阵盘作为"准装备"提供常驻被动增益，与正式装备属性叠加 | 无新增（阵盘已是独立装备槽位的功能等价物） |
| 11 | **悟道** | 增益协同 | 永久属性 +% 与装备基础属性加法叠加，形成"装备 × 悟道"乘算放大 | 无新增（悟道本身即隐性装备层） |
| 12 | **体修** | 增益协同 | 永久 HP/DEF +% 与护具属性叠加，是护具价值的放大器 | 无新增（体修本身即隐性装备层） |

### 15.3 新增装备系列与模板

#### 15.3.1 装备系列扩充

在现有 2 个系列基础上，新增 5 个子系统主题系列：

| 系列 ID | 系列名 | 主题来源 | 绑定区域 | 主攻槽位 | 稀有度 | 获取方式 |
|---------|--------|---------|---------|---------|--------|---------|
| series_qi_outer_cave | 幽泉洞窟试炼系 | 副本 | lv_qi_001~002 | 武器/护具 | 法器/灵器 | 副本掉落 |
| series_qi_shadow_swift | 影翼疾行系 | 副本 | lv_qi_002~003 | 武器/饰品 | 法器/灵器 | 精英/Boss |
| **series_forge_cold_iron** | **寒铁锻造系** | **炼器+矿脉** | lv_qi_001~002 | **武器/护具** | **俗器/法器** | **炼器锻造** |
| **series_garden_bloom** | **灵蕊滋养系** | **灵田** | lv_qi_001~002 | **饰品/护具** | **法器** | **灵田材料→炼器锻造** |
| **series_sunken_pearl** | **沉渊灵珠系** | **灵渔** | lv_qi_001~002 | **饰品** | **法器/灵器** | **垂钓直接获取** |
| **series_runic_seal** | **符文铭刻系** | **符箓** | lv_qi_001~002 | **武器/饰品** | **法器** | **符箓材料→炼器锻造** |
| **series_qi_inner_venom** | **毒雾深渊系** | **副本** | lv_qi_002 | **武器/护具** | **法器/灵器** | **副本掉落** |

#### 15.3.2 新增装备模板清单

> 下方列出新增模板的核心字段，完整 JSON 见 `12_equipment_sample_qi_refining.json`。

**A. 副本掉落扩充（series_qi_inner_venom — 毒雾深渊系）**

| 模板 ID | 名称 | 槽位 | 稀有度 | 来源 | 主属性方向 |
|---------|------|------|--------|------|-----------|
| eq_weapon_qi_inner_venom_fang | 毒牙匕首 | 武器 | 法器 | 精英 | ATK+暴击 |
| eq_armor_qi_inner_venom_shell | 蛇鳞甲片 | 护具 | 法器 | 精英 | HP+DEF |
| eq_weapon_qi_inner_venom_mist | 瘴气长刃 | 武器 | 灵器 | Boss | ATK+速度 |
| eq_armor_qi_inner_venom_robe | 毒雾法袍 | 护具 | 灵器 | Boss | HP+DEF+暴击 |

**B. 炼器锻造系（series_forge_cold_iron — 寒铁锻造系）**

| 模板 ID | 名称 | 槽位 | 稀有度 | 锻造材料 | 主属性方向 |
|---------|------|------|--------|---------|-----------|
| eq_weapon_forge_iron_blade | 寒铁阔剑 | 武器 | 俗器 | 寒铁×5+灵气×40 | ATK |
| eq_weapon_forge_iron_spear | 寒铁长矛 | 武器 | 法器 | 寒铁×8+碎片×3+灵气×80 | ATK+暴击 |
| eq_armor_forge_iron_mail | 寒铁锁甲 | 护具 | 俗器 | 寒铁×6+灵气×50 | HP+DEF |
| eq_armor_forge_iron_plate | 寒铁重甲 | 护具 | 法器 | 寒铁×10+灵玉×2+灵气×100 | HP+DEF |
| eq_accessory_forge_iron_ring | 寒铁指环 | 饰品 | 俗器 | 寒铁×3+灵气×30 | DEF |
| eq_accessory_forge_jade_pendant | 灵玉佩 | 饰品 | 法器 | 灵玉×4+寒铁×2+灵气×90 | 暴击+暴伤 |

**C. 灵田滋养系（series_garden_bloom — 灵蕊滋养系）**

| 模板 ID | 名称 | 槽位 | 稀有度 | 锻造材料 | 主属性方向 |
|---------|------|------|--------|---------|-----------|
| eq_armor_garden_bloom_robe | 灵蕊织袍 | 护具 | 法器 | 灵花×5+灵草×3+灵气×70 | HP+速度 |
| eq_accessory_garden_bloom_wreath | 灵花冠环 | 饰品 | 法器 | 灵花×4+灵果×2+灵气×85 | 灵气收益+HP |

**D. 灵渔沉水系（series_sunken_pearl — 沉渊灵珠系）**

| 模板 ID | 名称 | 槽位 | 稀有度 | 获取方式 | 主属性方向 |
|---------|------|------|--------|---------|-----------|
| eq_accessory_sunken_pearl_bead | 沉渊碧珠 | 饰品 | 法器 | 深潭垂钓(3%) | 速度+暴击 |
| eq_accessory_sunken_pearl_chain | 龙涎珠链 | 饰品 | 灵器 | 深潭垂钓(0.5%)+灵渔精通Lv3 | 暴击+暴伤+速度 |
| eq_weapon_sunken_coral_blade | 珊瑚短刃 | 武器 | 法器 | 浅滩垂钓(2%) | ATK+速度 |

**E. 符文铭刻系（series_runic_seal — 符文铭刻系）**

| 模板 ID | 名称 | 槽位 | 稀有度 | 锻造材料 | 主属性方向 |
|---------|------|------|--------|---------|-----------|
| eq_weapon_runic_seal_staff | 符文法杖 | 武器 | 法器 | 碎符×6+灵墨×3+灵气×85 | ATK+暴伤 |
| eq_accessory_runic_seal_band | 符文腕带 | 饰品 | 法器 | 碎符×4+灵墨×2+灵气×65 | 速度+DEF |

### 15.4 装备相关新增丹药（炼丹系统扩充）

| 丹药 ID | 名称 | 配方 | 输入需求 | 效果 | 精通门槛 |
|---------|------|------|---------|------|---------|
| potion_reforge | 洗练丹 | 灵花×3+灵草×2+灵气×100 | 300 次 | 重置目标装备副属性（从模板副属性池重新 roll） | 炼丹精通 Lv3 |
| potion_soul_forge | 铸魂丹 | 灵果×2+龙涎×1+灵气×150 | 400 次 | 下 5 场战斗装备全属性 +12%（一次性） | 炼丹精通 Lv4 |

### 15.5 装备附魔符（符箓系统扩充）

符箓系统新增"附魔符"品类——制作后对已装备物品施加临时增幅，持续固定场次后消散：

| 符咒 ID | 名称 | 配方 | 效果 | 持续 | 精通门槛 |
|---------|------|------|------|------|---------|
| talisman_enchant_atk | 锋锐附魔符 | 碎符×3+灵墨×2+灵气×55 | 目标武器 ATK +15% | 10 场战斗 | 符箓精通 Lv2 |
| talisman_enchant_def | 坚壁附魔符 | 碎符×3+灵珠×1+灵气×55 | 目标护具 DEF +15% | 10 场战斗 | 符箓精通 Lv2 |
| talisman_enchant_spd | 疾影附魔符 | 碎符×4+灵墨×1+龙涎×1+灵气×80 | 目标饰品 Speed +20% | 10 场战斗 | 符箓精通 Lv3 |

### 15.6 炼器锻造子路线（炼器系统扩充）

在现有"强化"路线之外，新增"锻造"子路线——直接消耗矿石材料制造基础装备：

#### 15.6.1 锻造机制
- 操作：选择锻造配方 → 消耗材料 → 键鼠输入驱动锻造进度 → 完成后装备进入背包。
- 锻造产出装备的 EnhanceLevel = 0，属性从模板池 roll。
- 锻造配方按槽位和稀有度分级（俗器配方免费可用，法器配方需炼器精通 Lv2）。

#### 15.6.2 锻造配方表

| 配方 ID | 产出模板 | 材料消耗 | 输入需求 | 精通门槛 |
|---------|---------|---------|---------|---------|
| forge_weapon_iron_blade | eq_weapon_forge_iron_blade | 寒铁×5+灵气×40 | 200 次 | 炼器 Lv1 |
| forge_weapon_iron_spear | eq_weapon_forge_iron_spear | 寒铁×8+碎片×3+灵气×80 | 350 次 | 炼器 Lv2 |
| forge_armor_iron_mail | eq_armor_forge_iron_mail | 寒铁×6+灵气×50 | 250 次 | 炼器 Lv1 |
| forge_armor_iron_plate | eq_armor_forge_iron_plate | 寒铁×10+灵玉×2+灵气×100 | 400 次 | 炼器 Lv2 |
| forge_accessory_iron_ring | eq_accessory_forge_iron_ring | 寒铁×3+灵气×30 | 150 次 | 炼器 Lv1 |
| forge_accessory_jade_pendant | eq_accessory_forge_jade_pendant | 灵玉×4+寒铁×2+灵气×90 | 350 次 | 炼器 Lv2 |
| forge_armor_bloom_robe | eq_armor_garden_bloom_robe | 灵花×5+灵草×3+灵气×70 | 300 次 | 炼器 Lv2 |
| forge_accessory_bloom_wreath | eq_accessory_garden_bloom_wreath | 灵花×4+灵果×2+灵气×85 | 350 次 | 炼器 Lv2 |
| forge_weapon_runic_staff | eq_weapon_runic_seal_staff | 碎符×6+灵墨×3+灵气×85 | 350 次 | 炼器 Lv2+符箓 Lv2 |
| forge_accessory_runic_band | eq_accessory_runic_seal_band | 碎符×4+灵墨×2+灵气×65 | 250 次 | 炼器 Lv2 |

#### 15.6.3 锻造 UI
- 炼器模式下底栏新增"锻造/强化"二级切换：
  - **强化**（现有）：选择已有装备 → 消耗碎片/碎符 → 提升等级。
  - **锻造**（新增）：选择锻造配方 → 消耗矿石/灵花等 → 输入驱动 → 产出新装备。
- 装备情况页新增"可锻造列表"入口，展示当前可用配方及材料状态。

### 15.7 灵渔沉水装备规则

垂钓系统新增"沉水装备"掉落概率层：

| 鱼塘 | 装备掉落率 | 主掉落系列 | 主掉落槽位 | 说明 |
|------|-----------|-----------|-----------|------|
| 灵泉浅滩 | 2% | series_sunken_pearl | 武器/饰品 | 法器为主 |
| 深潭 | 3% 法器 / 0.5% 灵器 | series_sunken_pearl | 饰品 | 灵器需灵渔精通 Lv3 |

- 沉水装备进入背包，来源标记为 `fishing`。
- 沉水灵器有独特标签 `sunken_treasure`，在战斗日志中高亮提示。
- 灵渔精通 Lv4 额外使沉水装备掉率翻倍。

### 15.8 强化材料供给链（矿脉 × 灵田 × 灵渔联动）

强化系统当前只消耗碎片/碎符，扩充后按强化段引入跨系统材料需求：

| 强化段 | 核心材料（现有） | 新增辅料需求 | 来源子系统 |
|--------|----------------|-------------|-----------|
| +1 ~ +3 | 碎片×3 | — | 副本 |
| +4 ~ +6 | 碎片×5+碎符×1 | 寒铁矿×2 | 副本+**矿脉** |
| +7 ~ +9 | 碎片×8+碎符×3 | 灵玉×1+灵花×2 | 副本+**矿脉**+**灵田** |
| +10 ~ +12 | 碎片×12+碎符×5 | 秘银×2+灵果×1+灵珠×1 | 副本+**矿脉**+**灵田**+**灵渔** |

设计意图：
- +1~+3 仅需副本材料，新手无跨系统压力。
- +4~+6 引入矿脉，激励玩家开始矿脉活动。
- +7+ 需要多系统材料，形成"副本→矿脉→灵田→灵渔→炼器强化"的完整资源链：
  - 采集系产出材料 → 炼器消耗材料 → 装备变强 → 推进更难副本 → 更多材料。
- 材料需求不会压过碎片/碎符的主体地位（辅料数量 < 主材数量）。

### 15.9 装备成长路径总览

```
  ┌──────────────────── 直接获取 ────────────────────┐
  │                                                    │
  │  副本掉落          灵渔沉水装备         炼器锻造   │
  │  (精英/Boss)       (垂钓概率)           (矿石配方)  │
  │       │                 │                    │      │
  │       ▼                 ▼                    ▼      │
  │  ┌─────────────────────────────────────────────┐   │
  │  │              背 包 (装备池)                   │   │
  │  │   俗器 → 法器 → 灵器   (稀有度梯度)          │   │
  │  └───────────────┬─────────────────────────────┘   │
  │                   │                                  │
  │           手动装备 │                                  │
  │                   ▼                                  │
  │  ┌─────────────────────────────────────────────┐   │
  │  │              已装备 (3 槽位)                   │   │
  │  └───────────────┬─────────────────────────────┘   │
  └──────────────────┼──────────────────────────────────┘
                     │
        ┌────────────┼────────────┐
        ▼            ▼            ▼
   ┌─────────┐ ┌─────────┐ ┌──────────┐
   │ 炼器强化 │ │ 符箓附魔 │ │ 炼丹洗练 │
   │ 永久+%  │ │ 临时+%  │ │ 重置副属性│
   │         │ │ (N场)   │ │          │
   │ 碎片    │ │ 碎符    │ │ 灵花     │
   │+寒铁矿  │ │+灵墨    │ │+灵草     │
   │+灵玉    │ │+灵珠    │ │+龙涎     │
   │+灵花    │ │+龙涎    │ │+灵果     │
   │+秘银    │ │         │ │          │
   └────┬────┘ └────┬────┘ └────┬─────┘
        │           │           │
        ▼           ▼           ▼
    矿脉+副本    符箓+灵渔     灵田+灵渔
    (材料来源)   (材料来源)    (材料来源)
```

### 15.10 子系统装备验收标准

- 12 个子系统中至少 8 个对装备成长链有直接或间接贡献。
- 玩家在中期（炼气 3-5 层）应能从副本、炼器锻造、灵渔三个来源获取装备。
- +4 以上强化必须依赖至少 2 个非副本子系统的材料。
- 灵渔沉水装备提供"采集系的惊喜掉落感"，不能成为主流装备来源。
- 炼丹洗练丹和符箓附魔符定位为"短期强化手段"，不替代炼器强化的长期价值。
- 各子系统材料在装备链中的消耗量不超过该系统日产的 30%，避免装备系统吸干其他系统的材料供给。

### 15.11 模板数量规划

| 区域 | 副本掉落 | 炼器锻造 | 灵渔沉水 | 兑换 | 合计 |
|------|---------|---------|---------|------|------|
| lv_qi_001 | 4（现有） | 6 | 2 | 2（现有） | 14 |
| lv_qi_002 | 4（新增） | 4 | 1 | 2 | 11 |
| **合计** | **8** | **10** | **3** | **4** | **25** |

对比现状（2 系列 4 模板），模板数量扩充至 25 个，覆盖 7 个系列、3 个装备来源路线，为装备系统提供足够的中期内容深度。
