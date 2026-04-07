using Godot;
using Xiuxian.Scripts.Services;

public static class UiText
{
    public const string DragHandle = "↕";
    public const string ResizeHandle = "↔";
    public const string BookButton = "📖";
    public const string SpiritStoneLabelPrefix = "灵石";
    public const string DefaultZoneName = "幽泉洞窟";
    public const string DefaultMonsterName = "妖物";
    public const string SettingsTitle = "设置";
    public const string RealmFallback = "炼气一层";

    public const string LeftTabCultivation = "修炼概况";
    public const string LeftTabBattleLog = "战斗日志";
    public const string LeftTabEquipment = "装备情况";
    public const string LeftTabBackpack = "背包";
    public const string LeftTabShop = "坊市";
    public const string LeftTabStats = "统计概览";
    public const string LeftTabValidation = "配置校验";
    public const string RightTabOnline = "联机";
    public const string RightTabBug = "Bug反馈";
    public const string RightTabSettings = "设置";

    public const string SystemSection = "系统";
    public const string DisplaySection = "画面";
    public const string ProgressSection = "进度";
    public const string PrivacySection = "隐私";
    public const string ResetAndApply = "重置并应用";
    public const string Quit = "退出";
    public const string Open = "打开";
    public const string DevHintCloudSync = "说明：云端同步功能仍在开发中，当前仅保存开关状态。";
    public const string Language = "语言";
    public const string KeepOnTop = "窗口置顶";
    public const string TaskbarIcon = "任务栏图标";
    public const string StartupAnimation = "开机启动动画";
    public const string AdminMode = "管理员模式";
    public const string HandwritingSupport = "手写支持";
    public const string Vsync = "垂直同步";
    public const string MaxFps = "帧率上限";
    public const string Resolution = "主界面分辨率";
    public const string ShowControlMarkers = "显示界面控制标记";
    public const string LogFolder = "日志文件夹";
    public const string ShowValidationPanel = "显示配置校验面板";
    public const string GameScale = "游戏缩放比例";
    public const string UiScale = "界面缩放比例";
    public const string AutoSaveInterval = "自动存档频率";
    public const string CloudSync = "云端同步";
    public const string MilestoneTips = "里程碑提示";
    public const string GlobalDebugOverlay = "全局调试信息";
    public const string PrivacyInputCollection = "输入采集";
    public const string PrivacyInputCollectionHint = "关闭后游戏将暂停所有键鼠统计，修炼进度停止。";
    public const string PrivacyCollectionStatus = "采集状态";
    public const string PrivacyCollectionPaused = "已暂停";
    public const string PrivacyStatement = "隐私声明";
    public const string PrivacyStatementBody = "本应用仅统计键盘/鼠标操作次数，不记录按键内容或屏幕信息。\n所有数据仅存储在本地，不上传至任何服务器。\n您可随时通过上方开关暂停采集。";
    public const string PrivacyNoticeTitle = "隐私说明";
    public const string PrivacyNoticeBody = "本应用通过统计键盘与鼠标的操作次数来驱动游戏进度。\n我们不记录任何按键内容或屏幕信息。\n所有数据仅保存在您的电脑上。";
    public const string PrivacyNoticeConfirm = "了解，开始修行";
    public const string CultivationRhythmEnabled = "周天提醒";
    public const string CultivationRhythmStrength = "提醒强度";
    public const string CultivationRhythmCycle = "小周天周期";
    public const string CultivationRhythmStrengthNone = "无提醒";
    public const string CultivationRhythmStrengthWeak = "弱提醒";
    public const string CultivationRhythmStrengthStrong = "强提醒";
    public const string ShopCategoryConsumables = "消耗品";
    public const string ShopCategoryExpansion = "扩容";
    public const string ShopCategoryUtility = "便利";
    public const string ShopCategoryRare = "稀有";
    public const string ReservedSuffix = "（预留）";
    public const string ExperimentalSuffix = "（实验）";
    public const string BugFeedbackTitle = "Bug反馈";
    public const string BugFeedbackHint = "描述你刚刚遇到的问题、触发步骤，以及是否能稳定复现。";
    public const string BugFeedbackInputPlaceholder = "例如：切到设置页后窗口宽度异常，点击关闭后再次打开可复现。";
    public const string CopyLogPath = "复制日志路径";
    public const string ExportFeedbackPack = "导出反馈文件";
    public const string OpenDataFolder = "打开数据目录";
    public const string BugFeedbackEmptyWarning = "请先输入问题描述，再导出反馈文件。";
    public const string BugFeedbackCopied = "已复制日志目录到剪贴板。";
    public const string BugFeedbackExportedPrefix = "反馈文件已导出：";
    public const string BugFeedbackExportFailed = "反馈文件导出失败，请稍后重试。";
    public const string BreakthroughButtonLabel = "突破";
    public const string BreakthroughButtonReadyLabel = "立即突破";
    public const string BossWeaknessInsightButton = "参悟Boss弱点";
    public const string MasterySectionTitle = "熟练度领悟";
    public const string CultivationUnavailable = "当前无法读取修炼进度。";
    public const string CultivationUnavailableTooltip = "修炼进度未加载。";
    public const string CultivationBreakthroughReadyTooltip = "可突破，点击提升境界。";
    public const string BossWeaknessInsightReadyTooltip = "副本精通 Lv4 生效中：可看破当前 Boss 弱点，使其属性下降10%。";
    public const string BossWeaknessInsightLockedTooltip = "仅可在Boss挑战中使用，且需要副本精通 Lv4。";
    public const string MasteryUnlockSuccessPrefix = "领悟成功：";
    public const string MasteryMaxLevelTooltip = "已达到当前系统的最高熟练度。";
    public const string MasteryUnavailableTooltip = "悟性或境界不足，暂时无法继续领悟。";
    public const string BackpackEquipWeapon = "装备背包武器";
    public const string BackpackEquipArmor = "装备背包护具";
    public const string BackpackEquipAccessory = "装备背包饰品";
    public const string BackpackNoMaterials = "当前背包中没有材料掉落。";
    public const string BackpackNoEquipment = "当前背包中没有未装备物品。";
    public const string BackpackSectionMaterials = "材料与掉落";
    public const string BackpackSectionEquipment = "背包装备";
    public const string EquipmentPageHint = "新获得的装备会先进入背包，不会自动替换当前已穿戴物品。请在背包页手动整理与穿戴。";
    public const string ActionModeDungeon = "主行为: 副本";
    public const string ActionModeCultivation = "主行为: 修炼";
    public const string ActionModeAlchemy = "主行为: 炼丹";
    public const string ActionModeSmithing = "主行为: 炼器";
    public const string LevelSelectPrefix = "副本:";
    public const string ActionModeQuickToggle = "切主行为";
    public const string NextLevelQuickButton = "下一副本";

    public static string SpiritStone(int amount) => $"{SpiritStoneLabelPrefix} {amount}";
    public static string RealmStage(int realmLevel, double percent) => $"炼气{realmLevel}层 {percent:0}%";
    public static string BatchInputAndAp(int inputEvents, double apFinal) => $"本批输入 {inputEvents} | 本批AP(资源) {apFinal:0.0}";
    public static string MasterySystemName(string systemId) => systemId switch
    {
        PlayerActionState.ModeDungeon => "副本",
        PlayerActionState.ModeCultivation => "修炼",
        PlayerActionState.ModeAlchemy => "炼丹",
        PlayerActionState.ModeSmithing => "炼器",
        PlayerActionState.ModeTalisman => "符箓",
        PlayerActionState.ModeBodyCultivation => "体修",
        _ => systemId,
    };

    public static string MasteryUnlockButton(string systemId, int nextLevel) => $"{MasterySystemName(systemId)} Lv{nextLevel}";

    public static string CultivationRhythmCycleOption(int minutes) => $"{minutes} 分钟";

    public static string PrivacyCollectionRunning(long count) => $"采集中 · 本次会话已记录 {count:N0} 次操作";

    public static string MasteryUnlockTooltip(string systemId, int currentLevel, int nextLevel, double cost, int requiredRealmLevel)
        => $"{MasterySystemName(systemId)} Lv{currentLevel} -> Lv{nextLevel}，消耗{cost:0}悟性，需要炼气{requiredRealmLevel}层。";

    public static string MasteryStatusLine(string systemId, int currentLevel, string effectDescription, string nextUnlockDescription)
        => $"- {MasterySystemName(systemId)} Lv{currentLevel}: {effectDescription} | 下一阶：{nextUnlockDescription}";

    public static string MasteryEffectDescription(string effectId, double effectValue) => effectId switch
    {
        SubsystemMasteryRules.DungeonBossWeaknessEffectId => $"Boss 弱点洞察，属性下降{effectValue * 100:0}%",
        SubsystemMasteryRules.CultivationLingqiBonusEffectId => $"修炼灵气获取 +{effectValue * 100:0}%",
        "dungeon_elite_rate_bonus" => $"精英遭遇率 +{effectValue * 100:0}%",
        "dungeon_drop_rate_bonus" => $"掉落率 +{effectValue * 100:0}%",
        "cultivation_parallel_explore_factor" => $"并行推进效率 +{effectValue * 100:0}%",
        "cultivation_breakthrough_exp_reduction" => $"突破经验需求 -{effectValue * 100:0}%",
        "alchemy_unlock_juling_san" => "解锁聚灵散",
        "alchemy_bonus_output" => $"炼丹额外产出 +{effectValue:0}",
        "alchemy_high_tier_formula" => "预留高阶丹方入口",
        "smithing_max_enhance_level" => $"强化上限提升至 +{effectValue:0}",
        "smithing_material_discount" => $"强化材料消耗 -{effectValue * 100:0}%",
        SubsystemMasteryRules.TalismanSecondRecipeUnlockEffectId => "解锁疾风符",
        SubsystemMasteryRules.TalismanMaterialDiscountEffectId => $"制符材料 -{effectValue * 100:0}%",
        SubsystemMasteryRules.TalismanExtraBattleUseEffectId => $"单战可用 {effectValue:0} 张",
        SubsystemMasteryRules.BodyCultivationSecondTechniqueUnlockEffectId => "解锁灵肤术",
        SubsystemMasteryRules.BodyCultivationMaterialDiscountEffectId => $"体修材料 -{effectValue * 100:0}%",
        SubsystemMasteryRules.BodyCultivationExtraCapEffectId => $"体修上限 +{effectValue:0}",
        _ => effectId,
    };
    public static string ExploreFrame(int frame) => $"探索中 | 帧 {frame}";
    public static string ExploreProgress(float progress) => $"进度 {progress:0.0}%";
    public static string Encounter(string monsterName) => $"遭遇{monsterName}，输入推进战斗回合";
    public static string BattleRound(int round, string monsterName, int hp) => $"Round {round} | {monsterName} HP {hp}";
    public static string BattleInProgress(string monsterName) => $"战斗中.. {monsterName}";
    public static string BattleVictory(string monsterName) => $"战斗胜利，结算{monsterName}战利品";

    public const string ExploreIdle = "探索待命";
    public const string WaitingInput = "等待输入...";
    public const string ZoneComplete = "区域探索完成，切换下一区域";

    public static string CultivationOverview(
        int realmLevel,
        double realmExp,
        double realmExpRequired,
        double expPercent,
        double lingqi,
        double insight,
        int spiritStones)
    {
        return
            $"{LeftTabCultivation}\n" +
            $"- 当前境界: 炼气{realmLevel}层\n" +
            $"- 境界经验: {realmExp:0.0}/{realmExpRequired:0.0} ({expPercent:0}%)\n" +
            $"- 灵气: {lingqi:0.0}\n" +
            $"- 悟性: {insight:0.0}\n" +
            $"- 灵石: {spiritStones}";
    }

    public static string CultivationBreakthroughStatus(bool canBreakthrough, double remainingExp)
    {
        return canBreakthrough
            ? "境界经验已满，可以尝试突破！"
            : $"距离突破还需 {remainingExp:0.0} 经验";
    }

    public static string CultivationBreakthroughTooltip(bool canBreakthrough, double remainingExp)
    {
        return canBreakthrough
            ? CultivationBreakthroughReadyTooltip
            : $"进度未满，还需 {remainingExp:0.0} 经验。";
    }

    public readonly record struct StatsOverviewData(
        int TotalInputs,
        long KeyCount,
        long ClickCount,
        long ScrollSteps,
        long JoypadButtonCount,
        long JoypadAxisCount,
        double MoveDistance,
        double ActiveSeconds,
        int RealmLevel,
        double RealmExp,
        double CurrentRealmDays,
        string CurrentActionName,
        string MasterySummary,
        double CurrentLingqi,
        double CurrentInsight,
        int CurrentSpiritStones,
        double TotalLingqi,
        double TotalInsight,
        int TotalSpiritStones,
        double TotalSpentInsight,
        int TotalSpentSpiritStones,
        int TotalSmallCycles,
        int TotalGrandCycles,
        int TotalRestCount,
        int TotalMeditationInsights,
        int BattleCount,
        int BattleWins,
        int BattleLosses,
        double WinRate,
        int TotalBossBattles,
        int TotalEliteBattles,
        int TotalAlchemyCrafts,
        int TotalSmithingCrafts,
        int TotalTalismanCrafts,
        int TotalCookingCrafts,
        int TotalFormationCrafts,
        int TotalMiningCompletions,
        int TotalFishingCompletions,
        int TemperCount,
        int BoneforgeCount,
        int BloodflowCount,
        int UnlockedPlots,
        int ActivePlots,
        int ReadyPlots,
        int IdlePlots,
        int TotalGardenPlants,
        int TotalGardenHarvests,
        int TotalGardenAutoHarvests,
        string SelectedPlotSummary);

    public static string StatsOverview(StatsOverviewData data)
    {
        return
            $"{LeftTabStats}\n\n" +
            $"【总览】\n" +
            $"● 当前主行为：{data.CurrentActionName}\n" +
            $"● 当前境界：炼气 {data.RealmLevel} 层\n" +
            $"● 当前境界经验：{data.RealmExp:0.0}\n" +
            $"● 当前境界停留：{data.CurrentRealmDays:0.0} 天\n" +
            $"● 累计总输入：{data.TotalInputs:N0}\n" +
            $"● 累计在线时长：{data.ActiveSeconds:N0} 秒 (≈{data.ActiveSeconds / 3600.0:0.0} 小时)\n" +
            $"● 精通概览：{data.MasterySummary}\n\n" +
            $"【资源】\n" +
            $"● 当前灵气：{data.CurrentLingqi:N0}\n" +
            $"● 当前悟性：{data.CurrentInsight:0.0}\n" +
            $"● 当前灵石：{data.CurrentSpiritStones}\n" +
            $"● 累计获得灵气：{data.TotalLingqi:N0}\n" +
            $"● 累计获得悟性：{data.TotalInsight:0.0}\n" +
            $"● 累计获得灵石：{data.TotalSpiritStones}\n" +
            $"● 累计消耗悟性：{data.TotalSpentInsight:0.0}\n" +
            $"● 累计消耗灵石：{data.TotalSpentSpiritStones}\n" +
            $"● 小周天完成数：{data.TotalSmallCycles}\n" +
            $"● 大周天完成数：{data.TotalGrandCycles}\n" +
            $"● 调息次数：{data.TotalRestCount}\n" +
            $"● 入定领悟次数：{data.TotalMeditationInsights}\n\n" +
            $"【战斗】\n" +
            $"● 累计战斗场次：{data.BattleCount}\n" +
            $"● 战斗胜场：{data.BattleWins}\n" +
            $"● 战斗败场：{data.BattleLosses}\n" +
            $"● 战斗胜率：{data.WinRate * 100.0:0.0}%\n" +
            $"● Boss 战次数：{data.TotalBossBattles}\n" +
            $"● 精英战次数：{data.TotalEliteBattles}\n\n" +
            $"【制作与采集】\n" +
            $"● 炼丹完成次数：{data.TotalAlchemyCrafts}\n" +
            $"● 炼器完成次数：{data.TotalSmithingCrafts}\n" +
            $"● 符箓完成次数：{data.TotalTalismanCrafts}\n" +
            $"● 烹饪完成次数：{data.TotalCookingCrafts}\n" +
            $"● 阵法完成次数：{data.TotalFormationCrafts}\n" +
            $"● 采矿完成次数：{data.TotalMiningCompletions}\n" +
            $"● 垂钓完成次数：{data.TotalFishingCompletions}\n" +
            $"● 体修·淬体次数：{data.TemperCount}\n" +
            $"● 体修·锻骨次数：{data.BoneforgeCount}\n" +
            $"● 体修·活血次数：{data.BloodflowCount}\n\n" +
            $"【灵田】\n" +
            $"● 已解锁田位：{data.UnlockedPlots}\n" +
            $"● 当前种植田位：{data.ActivePlots}\n" +
            $"● 当前成熟田位：{data.ReadyPlots}\n" +
            $"● 当前空闲田位：{data.IdlePlots}\n" +
            $"● 累计播种次数：{data.TotalGardenPlants}\n" +
            $"● 累计收获次数：{data.TotalGardenHarvests}\n" +
            $"● 自动收获次数：{data.TotalGardenAutoHarvests}\n" +
            $"● 当前田位状态：{data.SelectedPlotSummary}\n\n" +
            $"【输入明细】\n" +
            $"● 累计按键次数：{data.KeyCount:N0}\n" +
            $"● 累计鼠标点击：{data.ClickCount:N0}\n" +
            $"● 累计滚轮刻度：{data.ScrollSteps:N0}\n" +
            $"● 累计手柄按键：{data.JoypadButtonCount:N0}\n" +
            $"● 累计手柄轴输入：{data.JoypadAxisCount:N0}\n" +
            $"● 累计鼠标移动距离：{data.MoveDistance:N0} 像素";
    }

    private static string FormatDuration(double totalSeconds)
    {
        int seconds = Mathf.Max(0, Mathf.RoundToInt((float)totalSeconds));
        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;
        return $"{hours}小时{minutes}分";
    }

    public static string CultivationTemplate =>
        $"{LeftTabCultivation}\n- 当前境界\n- 突破条件\n- 心法加成";

    public static string EquipmentTemplate =>
        $"{LeftTabEquipment}\n- 武器/护具/饰品\n- 词条预览\n- 套装效果";

    public static string BackpackTemplate =>
        $"{LeftTabBackpack}\n- 材料与掉落\n- 未装备物品\n- 快速整理";

    public static string ShopTemplate =>
        $"{LeftTabShop}\n- 消耗品货架\n- 扩容许可\n- 便利增益\n- 稀有收藏";

    public static string EquipmentEmpty =>
        $"{LeftTabEquipment}\n当前未装备任何物品。\n默认测试装会在空存档时自动注入。";

    public static string BattleLogEmpty =>
        $"{LeftTabBattleLog}\n暂无战斗记录。开始探索后，战斗日志将显示在此处。";

    public static string StatsTemplate =>
        $"{LeftTabStats}\n- 总输入次数\n- 累计探索时长\n- 战斗胜率";

    public static string BugTemplate =>
        $"{RightTabBug}\n- 描述问题\n- 复制日志路径\n- 导出反馈包";

    public static string BackpackItemName(string itemId)
    {
        return itemId switch
        {
            "spirit_herb" => "灵草",
            "spirit_flower" => "灵花",
            "spirit_fruit" => "灵果",
            "seed_spirit_herb" => "灵草种子",
            "seed_spirit_flower" => "灵花种子",
            "seed_spirit_fruit" => "灵果种子",
            "cold_iron_ore" => "寒铁矿",
            "spirit_jade" => "灵玉",
            "mithril" => "秘银",
            "spirit_fish" => "灵鱼",
            "spirit_pearl" => "灵珠",
            "dragon_saliva" => "龙涎",
            "ripening_elixir" => "催熟灵液",
            "mining_refresh_token" => "矿脉刷新令",
            "fishing_bait" => "灵鱼饵",
            "page_fragment" => "异闻录残页",
            "lingqi_shard" => "灵气碎片",
            "broken_talisman" => "碎符",
            "spirit_ink" => "灵墨",
            "beast_bone" => "兽骨",
            "talisman_fire_charm" => "火符",
            "talisman_shield_charm" => "盾符",
            "food_spirit_porridge" => "灵鱼粥",
            "food_fruit_jelly" => "灵果蜜饯",
            "food_dragon_soup" => "龙涎鱼汤",
            "formation_spirit_plate" => "聚灵阵盘",
            "formation_guard_flag" => "护体阵旗",
            "formation_harvest_array" => "丰饶阵盘",
            "formation_craft_array" => "工巧阵盘",
            "potion_huiqi_dan" => "回气丹",
            "potion_juling_san" => "聚灵散",
            "talisman_burst_charm" => "炸裂符",
            _ => itemId
        };
    }

    public static string SlotLabel(EquipmentSlotType slot)
    {
        return slot switch
        {
            EquipmentSlotType.Weapon => "武器",
            EquipmentSlotType.Armor => "护具",
            EquipmentSlotType.Accessory => "饰品",
            _ => "装备"
        };
    }

    public static string EquipmentRarityLabel(EquipmentRarityTier rarity)
    {
        return rarity switch
        {
            EquipmentRarityTier.CommonTool => "俗器",
            EquipmentRarityTier.Artifact => "法器",
            EquipmentRarityTier.Spirit => "灵器",
            EquipmentRarityTier.Treasure => "宝器",
            _ => "装备"
        };
    }

    public static string EquipmentSourceLabel(EquipmentSourceStage sourceStage)
    {
        return sourceStage switch
        {
            EquipmentSourceStage.Starter => "开局",
            EquipmentSourceStage.Normal => "普通掉落",
            EquipmentSourceStage.Elite => "精英掉落",
            EquipmentSourceStage.Boss => "Boss掉落",
            EquipmentSourceStage.Exchange => "兑换",
            EquipmentSourceStage.FirstClear => "首通奖励",
            _ => "来源未知",
        };
    }

    public static string FormationSummary(string formationId)
    {
        return formationId switch
        {
            "formation_spirit_plate" => "灵气收益 +8%，战斗攻击 +2",
            "formation_guard_flag" => "战斗防御 +5%",
            "formation_harvest_array" => "采集推进 +12%",
            "formation_craft_array" => "加工推进 +12%",
            _ => "当前未激活阵法",
        };
    }

    public static string ReservedLabel(string title) => $"{title}{ReservedSuffix}";

    public static string ExperimentalLabel(string title) => $"{title}{ExperimentalSuffix}";
}
