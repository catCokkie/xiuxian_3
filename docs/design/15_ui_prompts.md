@# UI 页面设计提示词

> 用于在 v0.dev / Figma AI / Galileo AI 等工具中生成 UI 原型图。
> 生成日期：2026-03-29
> 页面总数：16（主底栏 + 书本外框 + 6 左 Tab + 坊市 + 1 右 Tab + 4 设置子页 + 首启隐私卡片）

---

## 通用风格前缀

> 所有提示词前加这段，确保风格一致。

```
Style guide for a Chinese cultivation (修仙) desktop pet idle game inspired by Melvor Idle.
Theme: ancient scroll / warm parchment aesthetic.
Colors:
- Frame border: #986244 (dark warm brown), 3px, rounded 10px
- Page background: #D7C7A7 (warm parchment beige), 2px border #8E6440, rounded 8px
- Section cards: #F0E4C8 (light cream), 1px border #A8804E, rounded 6px
- Dark text: #4B3622, secondary text: #5C4633
- Bottom bar strip: #745038 (dark brown, semi-transparent)
- Accent/active: #C8A050 (gold-brown)
Font: sans-serif with Chinese character support. All labels in Simplified Chinese.
No heavy decoration, minimal iconography, scholarly/calligraphy vibe.
Desktop resolution reference: 1152×648. Book window: 960×520.
```

---

## 1. 主底栏 (Main HUD Bar)

**文件**: `MainBarWindow.tscn` / `MainBarLayoutController.cs`

```
Design a bottom-anchored horizontal HUD bar (1152×210px) for a 修仙 idle game.
Background: semi-transparent dark brown panel (#986244, 0.97 opacity), rounded 10px top corners.

Row 1 — Battle Visualization (top, Y:6–164):
- Left: small "DragHandle" button (44×28, text "↕", top-left corner)
- Main area: "BattleTrack" panel spanning full width (clip_contents=true).
  Inside shows a horizontal battle scene:
  - Player marker (left side, ~X:60): small green diamond icon with "玩家" label
  - Up to 5 monster slot markers (spaced X:200–900): small red circle icons,
    each with a name label above ("阴潮蛇", "暗穴蛛") and a tiny HP bar below.
  - Light grid lines or distance markings on the track background.
  Background: slightly darker than frame (#7A4A34, 0.4 opacity)

Row 2 — Controls (bottom strip, Y:172, height:26px):
Left group:
- RealmStageLabel: "炼气3层 45%" (gold text)
- ActionMode dropdown: "副本" (only shows unlocked modes; initial: 副本/修炼/炼丹; others unlock progressively by realm_level, see 02_systems.md §21)
- LevelOption dropdown: "幽泉洞外围 (lv_qi_001)"

Center-right group:
- CultivationLabel: "修炼进度" small text
- CultivationProgressBar: thin horizontal bar showing realm XP (60%)
- BreakthroughButton: "突破" small button (enabled when XP full, otherwise grayed)
- ExploreProgressBar: thin horizontal bar showing zone exploration (35%)

Right-anchored:
- BookButton: "📖" (opens book submenu)
- ResizeHandle: "⟷" (resize grip)
```

---

## 2. 子菜单书本窗口外框 (Book Window Frame)

**文件**: `SubmenuBookWindow.tscn` / `SubmenuWindowController.cs` / `BookTabsController.cs`

```
Design the outer frame of a book-style submenu window (960×520px) for a 修仙 idle game.
Floating, draggable window with shadow.

Structure:
- Outer frame: #986244 panel, 3px border, rounded 10px, slight drop shadow
- Top-left: "X" close button (44×32)
- Top strip (Y:8–46): horizontal tab bar
  - Left-aligned tabs (toggle buttons, separated 4px):
    Only shows unlocked tabs. Initial: "修炼概况" | "动态"
    Unlock later: "装备情况" (first equipment) | "背包" (first item) | "统计概览" (realm≥3) | "配置校验" (dev toggle)
    Active tab: darker brown fill, pressed state
  - Right-aligned tabs (separated 6px):
    "Bug反馈" | "设置" (always visible)
  - New tab unlock: tab appears with 3s gold glow animation, then returns to normal style
  - See 02_systems.md §21 for full unlock conditions
- Center body (Y:54–456): SpreadBody panel, #D7C7A7 background, 2px border #8E6440, rounded 8px
  - Inner LeftPage panel (12px margins): #CDB896 background, 1px border, rounded 6px
    - LeftTitle label (top-left, 16px from edges)
    - Content area below title
- Bottom bar (Y:464–506): dark strip (#745038, 0.86 opacity), rounded 4px
  - Right-aligned: coin icon "●" + "灵石 14" label
```

---

## 3. 修炼概况 (Cultivation Overview Tab)

**文件**: `BookTabsController.cs` → `BuildCultivationUi()` / `BuildCultivationOverviewText()`

```
Design the "修炼概况" (Cultivation Overview) page content, 856×336px inside a parchment panel.

Top section — Status & Actions:
- CultivationStatusLabel: "境界经验已满，可以尝试突破！" (or "距离突破还需 1234.5 经验")
  Green when ready, amber when not.
- Action row (horizontal, 8px gap):
  - "突破" button (primary action, gold accent when available, gray when not)
  - "参悟Boss弱点" button (requires 副本精通Lv4, grayed with tooltip if locked)
  - Alchemy recipe dropdown + "开始炼丹" button
- Gathering row (horizontal, 8px gap):
  - Garden: dropdown "灵花种植" + "开始种植" button
  - Mining: dropdown "寒铁矿脉" + "开始采矿" button
  - Fishing: dropdown "灵鱼塘" + "开始垂钓" button

Middle section — Mastery Overview:
- RichText area showing 12 subsystem mastery levels in compact format:
  "副本 Lv2 → 精英率+5% | 修炼 Lv1 | 炼丹 Lv2 → 可炼聚灵散 | ..."
- 4 mastery unlock buttons in a row:
  Each button: "[系统名] Lv{next}" with cost "悟性 80" below.
  Enabled (gold border) when affordable, disabled (gray) when not.
  Systems: 副本精通, 修炼精通, 炼丹精通, 炼器精通

Bottom section — Scrollable RichText:
- "当前状态": 主行为 副本探索, 当前重心 全自动刷怪
- "成长状态": 炼气3层, 经验 450/1000 (45%), 突破状态, 悟性储备 156.0
- "战斗准备": 当前区域强度, 装备情况, 丹药库存
- "资源判断": 灵气收支, 灵石余额
- "当前判断": AI recommendation text
```

---

## 4. 动态 (Event Feed Tab — includes Battle Log)

> 原"战斗日志"Tab 已合并至此。战斗事件通过"战斗"筛选按钮查看，不再有独立战斗日志 Tab。

**文件**: `BookTabsController.cs` → `BuildEventFeedUi()` / `EventLogState.cs`

```
Design the "动态" (Event Feed) page, 856×336px, parchment background.
Persistent event log showing all game events in chronological order, newest at top.
This is the core "catch-up" view for a desktop pet idle game where the user often
looks away — they can scroll through everything that happened while they were gone.

Top — Filter Row (horizontal, 8px gap):
- 7 small toggle buttons, each a category filter:
  "全部" (default active, gold highlight) | "战斗" | "制作" | "突破" | "精通" | "装备" | "系统" | "周天"
  Active: filled gold accent (#C8A050). Inactive: outline only.
  Multiple filters can be active simultaneously. "全部" deselects others.

Middle — Scrollable Event List (full remaining height):
Each event entry is a single row or compact block:
- Timestamp (left, 11px, muted #5C4633): "14:32"
- Category icon (inline, 12px): ⚔ battle / ⚗ craft / ⬆ breakthrough / ⭐ mastery / 🛡 equipment / ⌛ system / 🔄 cycle
- Message (13px, dark text #4B3622):
  - Battle: "击败 阴潮蛇（精英）— 3回合 — 掉落：灵草×2, 寒铁×1"
  - Battle loss: "败于 暗穴蛛王 — 超时"
  - Craft: "炼丹完成 ×5 — 回气丹×3, 聚灵散×2"
  - Breakthrough: "突破成功 → 炼气3层" (金色 #C8A050)
  - Mastery: "领悟成功 → 副本精通 Lv3" (金色)
  - Equipment: "获得 寒铁剑 [法器]" (稀有度着色)
  - System: "离线结算：2.5 小时 — 灵气+1200, 悟性+45"
  - Cycle: "小周天·第3轮圆满 — 悟性+3（周天奖励）"
- Separator: 1px horizontal line (#D0C0A0), subtle

"战斗" filter view (selecting "战斗" button):
- Shows only battle events, but with expanded detail compared to "全部" view:
  - Header line: "[14:32:05] 遭遇 阴潮蛇 (精英)" — timestamp + enemy name + type badge
    Elite enemies: amber highlight. Boss: red highlight. Normal: no highlight.
  - Result line: "战斗胜利 — 3 回合" or "战斗失败 — 超时"
    Win: green text. Loss: red text.
  - Rewards line: "掉落：灵草 x2, 灵气 +45, 寒铁矿 x1"
  This expanded format replaces the former standalone "战斗日志" Tab.

High-value events have accent styling:
- Breakthrough / 宝器-tier equipment: gold left-border (3px #C8A050)
- Battle loss / system warning: red-ish left-border (3px #B85450)

If no events yet: centered placeholder "暂无动态。开始操作后，所有事件将记录在此处。"

Bottom status line (right-aligned, 11px muted):
"共 156 条，显示最近 200 条"

Key design principle: this is the player's "inbox" — clean, scannable, no clutter.
Font: 12-13px for entries, 11px for timestamps and status.
```

---

## 5. 装备情况 (Equipment Tab)

**文件**: `BookTabsController.cs` → `BuildEquipmentUi()` / `BuildEquipmentOverviewText()`

```
Design the "装备情况" (Equipment) page, 856×336px, parchment background.

Top — Action Row:
- 3 equip buttons: "装备背包武器" "装备背包护具" "装备背包饰品" (small, compact)
- Smithing controls: target equipment dropdown + "开始强化" button
- Hint text (small, italic): "新装备先进入背包，需手动穿戴。"

Middle — Stat Summary (card-style):
- Title: "装备情况"
- "当前已穿戴 3 件"
- Stat block (table-like):
  |          | 基础  | 装备后 |
  | 气血(HP) | 100   | 156    |
  | 攻击     | 25    | 38     |
  | 防御     | 10    | 18     |
  Improvement numbers shown in green "+XX"

Bottom — Equipped Items (scrollable):
- 3 horizontal equipment cards:
  Card layout: [Slot icon ⚔/🛡/💎] [Name "寒铁剑 +3"] [Rarity badge color-coded]
  Below: "主属性：Attack +25" / "副属性：Speed +3, Crit +2%"
  Rarity colors: 俗器=#8C8072, 法器=#6689B3, 灵器=#8C66B3, 宝器=#BF8C40

- Smithing progress (if active):
  "强化目标：寒铁剑 +3 (65%)" with progress bar

- Permanent bonuses section:
  "体修加成：气血 +3, 攻击 +2, 防御 +1"
```

---

## 6. 背包 (Backpack Grid Tab)

**文件**: `BackpackGridController.cs`

```
Design the "背包" (Backpack) grid inventory page, 856×336px, parchment background.
Scrollable if content overflows.

Top — Action Row:
- 3 compact buttons: "装备背包武器" "装备背包护具" "装备背包饰品"

Section 1 — "材料与掉落":
- Section header: small dark-brown label, 13px
- Grid of 64×64 cells, 4px gap, auto-wrap to fill width (~12 columns).
  Cell style: muted green (#6B9461, 0.85 opacity), 1px dark border (#735234),
  rounded 3px corners.
  Content: 2-3 character name centered ("灵草", "寒铁", "灵玉", "灵墨"),
  font 13px warm white. Bottom-right: quantity "x5" in 11px pale yellow.
  Hover: gold border highlight + floating tooltip above cell showing
  "灵草 x5" in dark popup (#1E1A14, 0.94 opacity, gold border, rounded 4px).

Section 2 — "丹药库存":
- Same grid style but blue-tinted cells (#5A7AA6, 0.85 opacity).
  Items: "回气丹 x2", "聚灵散 x1"

Section 3 — "背包装备":
- Same 64×64 grid but color-coded by rarity tier:
  俗器(CommonTool): gray #8C8072 | 法器(Artifact): blue #6689B3
  灵器(Spirit): purple #8C66B3 | 宝器(Treasure): gold #BF8C40
- Each cell: slot icon top-left (⚔🛡💎, 11px), equipment name centered (12px, max 4 chars).
  Hover tooltip: multi-line showing "[武器] 寒铁剑\n法器 | 普通掉落\n主属性: Attack +25\n副属性: Speed +3"

Empty state: centered text "背包空空如也" in muted brown.
```

---

## 7. 统计概览 (Stats Overview Tab)

**文件**: `BookTabsController.cs` → `BuildStatsOverviewText()`
**解锁**: realm ≥ 3（渐进解锁 §21）

```
Design the "统计概览" (Stats Overview) page, 856×336px, parchment background.
Scrollable vertical layout. No interactive elements, pure data dashboard.

Title: "统计概览" (dark brown, 16px bold)

Content: 5 collapsible sections, each with header + stat entries.
Section headers: 14px bold, gold left-border 3px #C8A050, 
  click to collapse/expand (default all expanded).
Stat entries: "● 统计名称：数值" format, 13px, 6px line spacing.
Numbers right-aligned for easy scanning.
Subtle horizontal dividers (#D4C4A0, 1px) between sections.

--- Section 1: "总览" ---
- 游戏总时长：14h 32m（活跃输入时间）
- 真实经过天数：7 天
- 累计按键次数：34,567
- 累计鼠标点击：12,345
- 累计滚轮刻度：5,678
- 小周天完成数：42
- 大周天完成数：8
- 调息次数：38

--- Section 2: "资源" ---
- 累计获得灵气：145,678
- 累计消耗灵气：132,400
- 累计获得灵石：2,340
- 累计消耗灵石：1,870（坊市 1,200 / 种子 450 / 其他 220）
- 累计获得悟性：890.5
- 累计消耗悟性：720.0

--- Section 3: "战斗" ---
- 累计战斗场次：567
- 总胜率：78.3%
- 累计击杀怪物：443
- 最高连胜：23 场
- Boss 首杀记录：（列表，每行 = Boss名 + 首杀日期）
  - 蛛后·幽缠 — 第 2 天
  - 蝠王·暗啸 — 第 4 天
- 累计消耗丹药：45
- 累计消耗符咒：23

--- Section 4: "制作与采集" ---
Per-system stats, one line each:
- 炼丹：制作 89 次 | 精通 Lv 3
- 炼器：制作 34 次 | 精通 Lv 2
- 符箓：制作 12 次 | 精通 Lv 1
- 烹饪：制作 28 次 | 精通 Lv 2
- 矿脉：采集 156 次 | 精通 Lv 3
- 灵渔：采集 98 次 | 精通 Lv 2
- 修炼：完成 234 次

--- Section 5: "灵田" ---
- 总播种次数：67
- 总收获次数：62
- 作物统计：（列表）
  - 灵草：收获 35 次，产出 105 株
  - 灵花：收获 18 次，产出 36 朵
  - 灵果：收获 9 次，产出 9 颗
- 催熟使用次数：5
- 田位数量：4 / 6

Data source: ResourceWalletState.TotalEarned* fields + 
  new PlayerStatsState tracking object (cumulative counters).
```

---

## 8. 配置校验 (Validation Tab)

**文件**: `BookTabsController.cs` → `BuildValidationUi()` / `RefreshValidationPanelContent()`

```
Design the "配置校验" (Config Validation) developer page, 856×336px, parchment background.
Scrollable vertical layout with 3 distinct card sections, 10px gap.

Card 1 — "筛选信息" (Filter Info):
- Background: light cream card (#F0E4C8), 1px border #A8804E, rounded 6px, 12px padding
- Title: "筛选信息" in dark brown, 14px, bold-ish
- Button row (8px gap):
  "范围：全部" toggle cycling through: 全部/config/level/monster/drop_table
  "关卡：全部关卡" toggle: current level only vs all levels
- Info label (12px): "当前关卡：幽泉洞外围 (lv_qi_001)\n校验过滤：全部类型"

Card 2 — "校验结果" (Validation Results):
- Same card style
- Status: "共 15 项校验，关卡 5 项 通过 14 / 15" (12px)
- Results content (12px, monospace-like):
  Each line: "✓ config.levels: 5 levels indexed" (green ✓) or
  "✗ equipment_series: no equipment series indexed" (red ✗)
  Content auto-expands, no inner scroll

Card 3 — "模拟结果" (Simulation Results):
- Same card style
- Filter row: "模拟关卡：Youquan Cave Outer (lv_qi_001)" button
              "模拟怪物：自动" button
- Action row: "模拟 200 次" "模拟 1000 次" buttons (compact)
- Status: simulation result summary (12px)
- Results: win rate, avg turns, damage stats in clean tabular format.
  Content auto-expands.

Cards must NOT overlap. Each sizes to its content.
Developer tool aesthetic but matching the warm book theme.
```

---

## 9. Bug 反馈 (Bug Feedback Tab)

**文件**: `BookTabsController.cs` → `BuildBugFeedbackUi()`

```
Design the "Bug反馈" (Bug Feedback) page, 856×336px, parchment background.

Top — Hint text (RichText, 12px, muted brown):
"描述你刚刚遇到的问题、触发步骤，以及是否能稳定复现。"

Middle — Text input area:
- Large TextEdit (full width, ~180px height), parchment-tinted background
- Placeholder: "例如：切到设置页后窗口宽度异常，点击关闭后再次打开可复现。"
- Clean border, slightly inset

Bottom — Action row (horizontal, 8px gap):
- "复制日志路径" button (copies log folder path to clipboard)
- "导出反馈文件" button (exports ZIP with logs + description)
- "打开数据目录" button (opens OS file explorer)

Below actions — Status label:
- Shows data folder path: "数据目录：C:/Users/.../AppData/Roaming/Godot/..."
- Small text, muted color

Clean, minimal, focused on text input. No complex layout.
```

---

## 10. 设置 — 系统 (Settings → System)

**文件**: `BookTabsController.cs` → `BuildSettingsUi()`

```
Design the "设置 > 系统" (Settings > System) page, 856×336px, parchment background.

Top — Section navigation (4 toggle buttons, horizontal):
"系统" (active, highlighted) | "画面" | "进度" | "隐私"
Active: darker brown fill. Inactive: outline only.

Content — Vertical list of setting rows, each row is:
[Label (left, 40% width)] [Control (right, 60% width)]
Row height ~32px, 8px vertical spacing.

Rows:
1. "语言" — Dropdown: "简体中文" / "English"
2. "窗口置顶" — Checkbox toggle (on/off)
3. "垂直同步" — Checkbox toggle
4. "帧率上限" — Dropdown: "30" / "60" / "120" / "不限"

Bottom — Action row (right-aligned):
- "重置并应用" button (resets all settings to defaults)
- "退出" button (red-ish tint, closes game)

Clean form layout, aligned labels and controls.
```

---

## 11. 设置 — 画面 (Settings → Display)

**文件**: `BookTabsController.cs` → `BuildSettingsUi()`

```
Design the "设置 > 画面" (Settings > Display) page, same frame as System tab.

Top: "系统" | "画面" (active) | "进度" | "隐私" navigation

Setting rows:
1. "主界面分辨率" — Dropdown: "1280x720" / "1600x900" / "1920x1080" / "2560x1440"
2. "显示配置校验面板" — Checkbox toggle
3. "日志文件夹" — Label showing path + "打开" button
4. "游戏缩放比例" — Dropdown: "1.00" / "1.10" / "1.25" / "1.33" / "1.50"
5. "界面缩放比例" — Dropdown: "1.00" / "1.10" / "1.25" / "1.33" / "1.50"

Same form layout style as System section.
```

---

## 12. 设置 — 进度 (Settings → Progress)

**文件**: `BookTabsController.cs` → `BuildSettingsUi()`

```
Design the "设置 > 进度" (Settings > Progress) page, same frame as System tab.

Top: "系统" | "画面" | "进度" (active) | "隐私" navigation

Setting rows:
1. "自动存档频率" — Dropdown: "5秒" / "10秒" / "30秒" / "60秒"
2. "云端同步" — Checkbox toggle + hint text below:
   "说明：云端同步功能仍在开发中，当前仅保存开关状态。" (small, muted)
3. "全局调试信息" — Checkbox toggle (shows debug overlay)

Fewer items than other sections. Clean, spacious layout.
```

---

## 13. 设置 — 隐私与采集 (Settings → Privacy)

**文件**: `BookTabsController.cs` → `BuildSettingsUi()`

```
Design the "设置 > 隐私" (Settings > Privacy) page, same frame as System tab.

Top: "系统" | "画面" | "进度" | "隐私" (active) navigation

Setting rows:
1. "输入采集" — Toggle switch (开/关), default ON
   Hint text below: "关闭后游戏将暂停所有键鼠统计，修炼进度停止。"
2. "采集状态" — Read-only label, dynamic:
   ON → "采集中 · 本次会话已记录 {N} 次操作"
   OFF → "已暂停"
3. "隐私声明" — Read-only text block (muted, small font):
   "本应用仅统计键盘/鼠标操作次数，不记录按键内容或屏幕信息。
    所有数据仅存储在本地，不上传至任何服务器。
    您可随时通过上方开关暂停采集。"

Clean layout, generous vertical spacing. Privacy statement text uses
#8B7355 color, 12px font.
```

---

## 14. 首次启动隐私说明卡片 (First-Launch Privacy Card)

**文件**: `PrototypeRootController.cs` → `ShowFirstLaunchPrivacyCard()`

```
Design a first-launch privacy notice card, centered on screen.
Only shown ONCE on first ever launch (flag saved to local config).

Card: 480×220px, parchment background (#F5E6C8), rounded corners 8px,
      subtle drop shadow, border 1px #C8A050.

Content (centered, vertical stack, 16px padding):
- Icon: lock SVG, 32px, centered
- Title: "隐私说明" — bold, 18px, #4A3728
- Body (14px, #6B5B4E, line-height 1.6):
  "本应用通过统计键盘与鼠标的操作次数来驱动游戏进度。
   我们不记录任何按键内容或屏幕信息。
   所有数据仅保存在您的电脑上。"
- Spacer 12px
- Button: "了解，开始修行" — gold fill (#C8A050), white text,
  centered, 160×36px, click → close card & set flag

Background: semi-transparent dark overlay (#000, 0.3 opacity)
Card appears with a gentle fade-in (0.3s).
```

---

## §15 坊市页面（Spirit Stone Shop）

> realm ≥ 2 解锁，书本窗口左侧 Tab 之一。Melvor GP Shop 风格分类货架。

```
Page: "坊市" — Spirit Stone Shop
Layout: Left vertical sub-tabs (4 categories) + right content area

Top bar:
- Title: "坊市" — bold 18px #4A3728
- Balance badge (right-aligned): coin icon + "灵石: {amount}" — 16px #C8A050

Left sub-tabs (vertical, 80px wide, stacked):
- "消耗品" (default selected)
- "扩容"
- "便利"
- "稀有"
Active tab: gold left-border 3px #C8A050, bg #FAF0DC
Inactive: bg transparent, text #8B7355

Right content area — scrollable grid of shop items:
Each item card (full-width row, 48px height):
- Left: item icon (24×24)
- Center: item name (14px bold #4A3728) + description (12px #8B7355)
- Right: price button
  - Affordable: gold bg #C8A050, white text "{price} 灵石", clickable
  - Cannot afford: border #C8A050, text #C8A050 dim, price in red
  - Sold out (limited purchase): grey bg #D0C8B8, text "已购" 
  - Daily limit reached: grey bg, text "今日已购 {n}/{max}"

Item rows separated by 1px #E8DCC8 divider.

Sub-tab: "扩容"
- Items that require mastery level show lock icon + "需精通Lv{n}"
  if mastery not met — item visible but not purchasable.

Sub-tab: "稀有"
- Items with collection mechanic: show progress "残页 {owned}/10"
  below item name in 11px #8B7355.

Toast on purchase: "购入 {item_name} ×{qty}" — standard event toast style.
Purchase also writes to 动态 Tab: "坊市：购入 {item_name} ×{qty}，花费 {price} 灵石"
```

---

## 推荐工具

| 工具 | 适合场景 | 地址 |
|------|---------|------|
| **v0.dev** | 布局原型，生成可预览的 HTML/CSS，最适合快速迭代 | v0.dev |
| **Figma + Magician** | 精细调整，可导出切图供 Godot 使用 | figma.com |
| **Motiff** | 国产 AI UI 设计，中文理解好 | motiff.com |
| **Galileo AI** | 完整页面生成，适合探索不同方向 | usegalileo.ai |

## 使用建议

1. 先在 **v0.dev** 用通用风格前缀 + 具体页面提示词生成布局原型
2. 截图导入 **Figma** 做精细调整（间距、字号、圆角）
3. 记下最终确定的尺寸/颜色/间距数值，回 Godot 对照修改 C# 代码中的常量
4. 颜色值和尺寸均从项目实际代码中提取，可直接复用
