# 素材提示词模板库

美术规则见`20_asset_direction.md`；`../art/generation_prompts.json`保留历史v1图片生成提示词。现行角色使用`tools/art/build_actor_assets.py`的简朴SVG构形，以下角色模板作为设计简报，优先修改源形状。任务状态只记录在`10_todo.md`，模板不代表功能或动画已经开放。

## 使用方法

1. 先明确稳定内容 ID、类别、家族、展示尺寸、状态和使用页面。
2. 新角色/怪物参考`assets/characters/player/cultivator_idle_plain_v2.svg`，同物种变体沿用该物种的SVG源形状。
3. 将下面的基础模板与一个具体主体描述组合，保留负面约束。一个主体/状态对应一条提示词，不用一张“全套素材拼图”代替各自文件。
4. 检查透明度与四周留白，再放到纸面、深色背景和实际24/80px大小上比较；SVG同样需要原生渲染验收。
5. 审核通过后加入索引与内容配置；品质框、数字、文案由引擎显示，不交给图片模型绘制。

## A：角色 / 怪物基础模板

```text
Asset type: one simple flat SVG character for an original Chinese xianxia desktop idle game.
Reference: the existing plain_v2 SVG family. Use a 32-unit grid, 96px canvas,
warm brown 1.2-unit rounded outlines, readable polygons and compact proportions.
Subject: {one exact species or character}; {silhouette}; {two identifying features};
{idle / specific action pose}; {side-facing right for player / front or side for enemy}.
Palette: parchment ivory, muted jade, copper gold, warm dark brown;
regional accent {accent}, used sparingly. At most seven flat colors per subject, no shading.
Composition: exactly ONE subject, full body and appendages, centered square canvas,
approximately 80 percent height, generous transparent padding, readable at 80px and 24px.
Constraints: genuinely transparent alpha; no frame, no ground, no cast shadow,
no background, no checkerboard, no lettering, no watermark, no extra subjects,
no gradients, no glow, no surface texture, no photorealism, no glossy 3D,
no noisy tiny ornaments; distinguish a Boss by silhouette before adding decoration.
```

| 家族 / 主体 | 形态参数 | 普通怪与 Boss 区分 |
| --- | --- | --- |
| 苔藓史莱姆 | 扁圆凝胶、两枚眼、少量苔芽、灰绿 | 以后精英用不同苔簇和体表裂纹，不复制铜皇冠到所有怪物 |
| 影蝠 | 小躯干、尖耳、张开的灰紫角翼 | 领主用更宽翼形、米白翼缘、铜色胸饰 |
| 洞穴蛛 | 低矮腹部、可分辨的分节腿、赭红 | 蛛后用大腹、甲冠、玉眼，腿仍与腹部清楚分开 |
| 阴潮蛇 | 盘曲 S 形、蓝玉色、米白腹部 | 蛇王有角和更大的头冠轮廓，不能变成完全不同的飞龙物种 |
| 深渊菌 | 大菌帽、矮菌柄、米白斑点 | 巨人用宽体、根臂、块状根足与铜色裂纹 |
| 幽骨 | 米白骨质、暗棕关节、玉符 | 骨王用宽骨甲、冠与短披风，控制尖刺细节密度 |
| 主角变体 | 同一脸型、发髻、玉带、米白袍、铜坠 | 境界先换袖纹/冠/腰饰，保持 24px 剪影一致 |

反复生成角色状态时另加：`The input is an IDENTITY reference. Preserve face, body proportions, robe construction, palette, pixel density and baseline. Change only the requested pose.`

## B：物品 / 装备图标

小图标优先修改 SVG 配方，确定性更强；以下可作为委托或模型制作的统一设计简报。

```text
Asset type: one small inventory icon, not an icon card.
Subject: {one concrete item}; family {herb / mineral / fish / potion / talisman /
food / formation / weapon / armor / accessory}; two identifying features {features}.
Style: simple 32-unit grid illustration, bold warm-brown outline, parchment ivory,
muted jade and copper-gold details; regional accent {accent}. Large readable shapes,
minimal interior detail, no gradients. Match the existing project SVG icon family.
Composition: centered single object, roughly 24 units inside a 32-unit canvas,
transparent background, readable at 28px.
Constraints: no text, numbers, rarity frame, level badge, shadow, scenery or watermark.
```

| 子类 | 主体词模板 | 变化规则 |
| --- | --- | --- |
| 草/花/果 | `one herb sprig / five-petal spirit flower / round spirit fruit` | 叶、花瓣、果体分别构成主要轮廓 |
| 种子 | `one small ivory seed pouch with a single plant symbol` | 对应植物图标色；不写种子名称 |
| 矿/玉 | `one angular cold iron ore / a jade crystal shard` | 矿用不规则块，玉用完整切面；铁、银、玉保持不同材质符号 |
| 鱼 | `one small {species} fish, distinct tail, dorsal fin and body armor` | 新物种应改变鳍和体型，不能一直只换色 |
| 丹药/灵液 | `one squat ceramic medicine bottle / tied powder pouch` | 不同剂型先改容器；药性再用小色块区分 |
| 符箓/令牌 | `one vertical parchment charm / compact bronze token` | 无字几何符纹，功能色不同，不用可读文字 |
| 灵膳 | `one shallow bowl of fish soup / plate of fish slices` | 碗、盘与食材形状体现菜谱，颜色只作辅助 |
| 阵法 | `one octagonal formation plate / short guard flag` | 阵眼或旗形体现用途，保留外围留白 |
| 武器 | `one {short blade / bone dagger / broad iron sword / spear / runic staff}` | 武器长度与刃型优先；系列花纹为次要层 |
| 护具 | `one {moss robe / serpent scale armor / cold iron mail / heavy plate}` | 袍、鳞、锁甲、板甲轮廓和纹理分开 |
| 饰品 | `one {ring / jade pendant / flower wreath / pearl bead / chain}` | 改变轮廓；别让所有饰品成为圆宝石 |

装备提示词按“系列 + 槽位 + 基础造型”组合。实例词条、强化等级、品质采用运行时叠加；新增模板只需新增 `equipment:<template_id>`，不会扩大为“模板 × 品质 × 强化等级”的图片矩阵。

## C：区域与地点题图

```text
Use case: stylized-concept.
Asset type: a small panoramic location vignette for a parchment game page.
Subject: {region scene}; only three to five major readable shapes.
Style: restrained 2D pixel-style background, warm umber silhouette, parchment ivory,
muted regional accent {accent}, subtle upper-left light. Match the creature family.
Composition: wide 4:1 landscape; strongest shapes at the outer thirds; calm low-detail
central area; no characters, no UI, no lettering, no dramatic high-contrast effects.
Constraints: no photorealism, no glossy 3D, no watermark, no busy noise.
```

| 区域 ID | 主体填词 | 色彩/识别点 |
| --- | --- | --- |
| `lv_qi_001` | mossy cave entrance, two rounded rocks, a few small ferns | 苔绿、宽入口、干燥石块 |
| `lv_qi_002` | deeper cave, angular stalactites and a few thin spider threads | 灰紫/赭红、竖向石柱，蛛丝不能变成密网 |
| `lv_qi_003` | a narrow cave rift with a shallow blue-jade underground stream | 蓝玉水面、裂隙 |
| `lv_qi_004` | a quiet cave hollow with two large olive mushroom caps | 橄榄绿菌帽、少量孢子 |
| `lv_qi_005` | an ancient ivory-stone hall, two weathered pillars, a small jade seal | 米白骨石、暗棕柱体、玉符 |

鱼塘、灵田与矿脉地点图沿用模板，分别替换为水面/田垄/矿岩；不把同一张题图同时裁成横幅、方形和圆形头像，需要分别确定构图。

## D：动画与特效

```text
Asset type: {one animation frame / transparent effect layer}.
Reference: approved character or effect identity; preserve exact scale and palette.
Action: {single precisely described motion phase}.
Constraints: locked camera, locked pixel grid, consistent feet baseline and canvas,
transparent alpha, no scenery, no blur trail unless explicitly part of this effect,
no changing costume, proportions or light direction across frames.
```

| 类型 | 帧与节奏设计简报 | 分层约束 |
| --- | --- | --- |
| 待机 | 4–6 帧，低频呼吸，脚底不动 | 角色本体，不生成地面影子 |
| 修炼 | 4–6 帧，安静盘坐，轻微袖动 | 灵气另做特效层 |
| 攻击 | 4–6 帧，预备、出手、回收 | 攻击轨迹独立；禁止每帧改变装备 |
| 受击 | 2–3 帧，短促后退，快速恢复 | 不通过整帧高饱和闪白表现 |
| 突破 | 主体6–8帧；淡金光环独立 | 低密度铜金/玉绿点，不使用全屏闪光 |
| 休息 | 4–6帧，坐姿/闭眼 | 保持陪伴氛围，工作模式更克制 |
| 掉落/护盾 | 4帧，小玉色碎点 / 薄盾轮廓 | 24–48px 透明层，不遮住名称与气血 |

图片生成能提供姿态参考，逐帧一致性需要在像素编辑器中校正。正式动画交付包含帧顺序、帧时长、原点、碰撞/显示范围；不能仅交一张没有元数据的混排图。

## E：声音设计简报

```text
One gentle {event} cue for a quiet Chinese cultivation desktop companion game.
Duration {0.1–0.7 seconds}. Soft wooden / ceramic / muted bell timbre, short decay,
restrained level, no speech, no sharp transient, no long reverb, no background noise.
Do not compose continuous music for a one-shot event. Provide clean mono WAV.
```

事件填词：`soft interface click`、`craft complete`、`battle win`、`realm breakthrough`、`missing material hint`。环境声另用 `subtle cave water / gentle paper-and-wood room ambience`，需要独立循环边界与音量约定。音乐与环境声必须有静音设置，不能按击杀次数密集叠加。

## F：后续内容补充

NPC 按“身份 + 职业工具 + 一个剪影特征”描述；成就图标按“事件载体 + 小标记”描述；季节服装只替换局部装饰。新增区域先定义区域色、三种普通怪、一个 Boss 和两种专属材料，再批量写入同一份提示词清单。

宣传图使用单独的 16:9 构图提示词，可以把批准的角色与地点图作为参考；不要把带标题、场景和品质边框的宣传图反向当作游戏 sprite 使用。

## G：操作深化产物图标

使用已有物品SVG模板与暖棕/米白/玉绿/铜金配色，48px源图、28px显示；每个图只加一个可辨识的小标记。配方与效果以`02_systems.md`§24.5为准，素材制作状态只见`10_todo.md`。

| 产物ID | 提示词填项 / 构形简报 | 类别 |
|--------|-----------------------|------|
| `potion_huiqi_seed` | 小药瓶、单颗草种、玉绿药液；不用品质星或数字 | 消耗品 |
| `potion_lingdun_dan` | 小药瓶、薄盾轮廓、淡铜金瓶塞 | 消耗品 |
| `potion_yangyuan_dan` | 小药瓶、被一道封线截住的芽纹、玉绿主体 | 消耗品 |
| `talisman_zhenling` | 米白纸符、暖棕封纹、单个铜金结印；不画可读文字 | 消耗品 |
| `food_plain_fish` | 简朴米白汤碗、单鱼剪影、细玉绿蒸汽 | 食品 |
| `food_mining_stew` | 厚陶碗、鱼纹、单个小矿镐标记 | 食品 |
| `food_garden_tea` | 小茶盏、单枝花叶、温和玉绿点缀 | 食品 |
| `formation_pathfinder` | 圆阵盘、单个方向指针、疏松铜金环纹 | 阵法 |

操作切换、路线进度和锁定信息采用现有图标加文字，不将界面状态烘焙进产物图。不要为三支线制作不同画风的同名素材；图标和其实际用途应同批接线。
