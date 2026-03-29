# UI 页面设计提示词

> 用于在 v0.dev / Figma AI / Galileo AI 等工具中生成 UI 原型图。
> 生成日期：2026-03-29
> 页面总数：12（主底栏 + 书本外框 + 6 左 Tab + 1 右 Tab + 3 设置子页）

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
- ActionMode dropdown: "副本" (shows 12 modes: 副本/修炼/炼丹/炼器/灵田/矿脉/灵渔/符箓/烹饪/阵法/悟道/体修)
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
    "修炼概况" | "战斗日志" | "装备情况" | "背包" | "统计概览" | "配置校验"
    Active tab: darker brown fill, pressed state
  - Right-aligned tabs (separated 6px):
    "Bug反馈" | "设置"
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

## 4. 战斗日志 (Battle Log Tab)

**文件**: `BookTabsController.cs` → `BuildBattleLogText()`

```
Design the "战斗日志" (Battle Log) page, 856×336px, parchment background.

Simple scrollable text log with recent combat entries, newest at top.
Each battle entry block:
- Header line: "[14:32:05] 遭遇 阴潮蛇 (精英)" — timestamp + enemy name + type badge
  Elite enemies: amber highlight. Boss: red highlight. Normal: no highlight.
- Result line: "战斗胜利 — 3 回合" or "战斗失败 — 超时"
  Win: green text. Loss: red text.
- Rewards line: "掉落：灵草 x2, 灵气 +45, 寒铁矿 x1"
  Items in brackets with quantity.
- Separator: thin horizontal line (#B8A080)

If no battles yet: centered placeholder text "暂无战斗记录。开始探索后，战斗日志将显示在此处。"

Layout: pure vertical scroll, no columns, monospace-like alignment for readability.
Font size: 12px for entries, 11px for timestamps.
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
  "悟道加成：灵气收益 +4%, 悟性收益 +10%"
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

```
Design the "统计概览" (Stats Overview) page, 856×336px, parchment background.
Pure text page, no interactive elements. Scrollable.

Title: "统计概览" (dark brown, 16px)

Content organized as a vertical list of stat entries, left-aligned:
Each entry: "● 统计名称：数值" format, 13px text, 6px line spacing.

Section "输入统计":
- 累计按键次数：12,345
- 累计鼠标点击：8,901
- 累计滚轮刻度：2,456
- 累计鼠标移动距离：1,234,567 像素
- 累计在线时长：14,520 秒 (≈4.0 小时)

Section "成长统计":
- 历史最高境界：炼气 5 层
- 当前境界天数：2.3 天

Section "战斗统计":
- 累计战斗场次：567
- 战斗胜率：78.3%

Section "资源统计":
- 累计获得灵气：45,678
- 累计获得悟性：890.5
- 累计获得亲密度：234
- 累计获得灵石：56

Clean, data-dashboard feel. Numbers right-aligned or tabulated for easy scanning.
Subtle horizontal dividers between sections.
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

Top — Section navigation (3 toggle buttons, horizontal):
"系统" (active, highlighted) | "画面" | "进度"
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

Top: "系统" | "画面" (active) | "进度" navigation

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

Top: "系统" | "画面" | "进度" (active) navigation

Setting rows:
1. "自动存档频率" — Dropdown: "5秒" / "10秒" / "30秒" / "60秒"
2. "云端同步" — Checkbox toggle + hint text below:
   "说明：云端同步功能仍在开发中，当前仅保存开关状态。" (small, muted)
3. "全局调试信息" — Checkbox toggle (shows debug overlay)

Fewer items than other sections. Clean, spacious layout.
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
