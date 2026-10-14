using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests
{
    // Run with the dedicated scene and -- --smoke-test. Real saves and input hooks stay disabled.
    public partial class GameplaySmokeTest : Node
    {
        private int _checks;
        public override void _Ready()
        {
            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--smoke-test") < 0) { GetTree().Quit(2); return; }
            AddChild(GD.Load<PackedScene>("res://scenes/PrototypeRoot.tscn").Instantiate());
            CallDeferred(nameof(RunChecks));
        }
        private void Check(bool success, string label)
        {
            if (!success) throw new InvalidOperationException(label);
            _checks++; GD.Print($"SMOKE PASS: {label}");
        }
        private void RunChecks()
        {
            try
            {
                var s = ServiceLocator.Instance!;
                var root = GetNode<PrototypeRootController>("PrototypeRoot");
                var controller = root.GetNode<ExploreProgressController>("ExploreProgressController");
                s.PlayerProgressState!.FromDictionary(new() { ["realm_level"] = 5 });
                s.SubsystemMasteryState!.FromDictionary(new() { ["dungeon"] = 4, ["cultivation"] = 3, ["alchemy"] = 4, ["talisman"] = 4, ["cooking"] = 4, ["garden"] = 4, ["fishing"] = 4 });
                s.ResourceWalletState!.AddLingqi(100000);
                s.ResourceWalletState.AddSpiritStones(10000);
                s.PlayerActionState!.SetConsumablePolicy("none");
                s.PlayerActionState.SetAction("dungeon");
                double exp = s.PlayerProgressState.RealmExp;
                for (int i = 0; i < 100; i++) s.InputActivityState!.RegisterKeyDown();
                s.InputActivityState!._Process(.1);
                Check(s.PlayerProgressState.RealmExp > exp, "online dungeon grants realm experience");
                s.InputActivityState.RegisterMouseMove(10000000); s.InputActivityState._Process(.1);
                s.InputActivityState.RegisterMouseMove(10000000); s.InputActivityState._Process(.1);
                Check(s.InputActivityState.ApFinal == 0, "movement observes the shared AP cap");

                s.PlayerActionState.SetAction("talisman");
                var talisman = s.TalismanState!;
                talisman.SelectRecipe("talisman_fire_charm");
                talisman.Production.Configure(true);
                controller.AdvanceOfflineCrafting(400);
                Check(talisman.CurrentProgress == 0, "missing materials freeze partial crafting");
                s.BackpackState!.AddItem("broken_talisman", 30); s.BackpackState.AddItem("spirit_ink", 30);
                talisman.AdvanceProgress(410);
                controller.AdvanceOfflineCrafting(10);
                Check(s.BackpackState.GetItemCount("talisman_fire_charm") == 2 && talisman.CurrentProgress == 60, "ready crafting retries and preserves new batch overflow");
                talisman.SelectRecipe("talisman_fire_charm");
                int logged = s.EventLogState!.GetEntries().Count(x => x.CategoryId == EventLogState.CategoryCraft);
                for (int i = 0; i < 200; i++) s.InputActivityState.RegisterKeyDown();
                s.InputActivityState._Process(61);
                Check(s.EventLogState.GetEntries().Count(x => x.CategoryId == EventLogState.CategoryCraft) == logged + 1, "successful online craft records exactly one event");

                s.CookingState!.SelectRecipe("cooking_spirit_porridge");
                s.BackpackState.AddItem("food_clear_fish_soup", 1); s.BackpackState.AddItem("food_iron_fish_meal", 2);
                Check(s.CookingState.TryActivateMeal("food_clear_fish_soup"), "activate meal consumes an owned portion");
                s.CookingState.AdvanceMeal(100);
                Check(s.CookingState.TryActivateMeal("food_iron_fish_meal") && s.BackpackState.GetItemCount("food_clear_fish_soup") == 1, "meal replacement refunds the partial portion");
                var mealSave = Roundtrip(s.CookingState.ToDictionary()); s.CookingState.FromDictionary(mealSave);
                Check(s.CookingState.ActiveMealId == "food_iron_fish_meal" && s.CookingState.ActiveMealRemainingSeconds > 0, "meal survives Godot dictionary save roundtrip");
                Check(s.CookingState.TryActivateMeal("food_clear_fish_soup") && Math.Abs(s.CookingState.ActiveMealRemainingSeconds - 2294) < .01, "returning to a meal keeps its remaining time");

                s.BackpackState.AddItem("spirit_herb", 30);
                s.PlayerActionState.SetAction("alchemy"); s.AlchemyState!.SelectRecipe("recipe_huiqi_dan");
                int potions = s.PotionInventoryState!.GetPotionEntries().GetValueOrDefault("potion_huiqi_dan");
                s.AlchemyState.Production.Configure(true);
                controller.AdvanceOfflineCrafting(450);
                Check(s.PotionInventoryState.GetPotionEntries().GetValueOrDefault("potion_huiqi_dan") == potions + 6 && s.AlchemyState.CurrentProgress == 50, "offline alchemy awards two funded batches");
                Check(s.GardenState!.GetUnlockedPlotCount() == 6, "garden mastery unlocks six plots");
                Check(s.PlayerProgressState.TryApplyBodyCultivationReward("body_cultivation_temper", 4, out _) && s.PlayerProgressState.BodyCultivationMaxHpRate == .03, "body cultivation grants permanent percentage stats");

                controller.FromRuntimeDictionary(new() { ["explore_progress"] = 100f, ["battle_state"] = "boss_ready" });
                s.PlayerActionState.SetAction("dungeon"); controller._Process(.1);
                controller.AdvanceHealthRecovery(20);
                Check(controller.ToRuntimeDictionary()["battle_state"].AsString() == "boss_ready", "full exploration waits at BossReady");
                var challenge = root.GetNode<Control>("MainBarWindow/Chrome/BattleTrack").GetChildren().OfType<Button>().First(x => x.Text == "挑战 Boss");
                challenge.EmitSignal(Button.SignalName.Pressed);
                Check(controller.ToRuntimeDictionary()["battle_state"].AsString() == "in_battle", "manual challenge starts the boss");
                var battle = controller.ToRuntimeDictionary(); battle["player_hp"] = 1; battle["enemy_hp"] = 999; battle["enemy_attack_power"] = 400;
                controller.FromRuntimeDictionary(battle);
                for (int i = 0; i < 500; i++) s.InputActivityState.RegisterKeyDown();
                s.InputActivityState._Process(61); controller._Process(.1);
                Check(controller.ExploreProgressValue == 100 && controller.ToRuntimeDictionary()["battle_state"].AsString() == "boss_ready", "boss defeat preserves exploration for retry");
                Check(s.LevelConfigLoader!.BuildOfflineEquipmentFactory("lv_qi_001")?.Invoke(0).EquipmentTemplateId != null, "offline equipment uses real configured templates");
                Check(!s.GuideState!.Complete("first_input"), "completed guide steps do not repeat");
                s.GuideState.FromDictionary(Roundtrip(s.GuideState.ToDictionary()));
                Check(!s.GuideState.Complete("first_input"), "guide progress survives ConfigFile serialization");

                var firstClear = s.LevelConfigLoader.BuildOfflineEquipmentFactory("lv_qi_001")!.Invoke(1);
                int equipmentBefore = s.BackpackState.GetEquipmentInstances().Length;
                s.BackpackState.AddEquipmentInstance(firstClear);
                Check(s.BackpackState.GetEquipmentInstances().Length == equipmentBefore + 1, "offline generated equipment enters the real backpack");
                s.LevelConfigLoader.ConsumeOfflineDropRolls("lv_qi_001", 2);
                var counts = (Godot.Collections.Dictionary<string, Variant>)s.LevelConfigLoader.ToRuntimeDictionary()["daily_roll_count_by_table"];
                Check(counts.Values.Sum(x => x.AsInt32()) >= 2, "offline drops consume persisted economy quotas");
                controller.FromRuntimeDictionary(new() { ["explore_progress"] = 90f });
                controller.ApplyOfflineExploreProgress(50);
                controller.FromRuntimeDictionary(Roundtrip(controller.ToRuntimeDictionary()));
                Check(controller.ExploreProgressValue == 100, "offline exploration clamps and survives serialization");

                s.PlayerActionState.SetAction("fishing"); s.FishingState!.SelectPond("fishing_spirit_fish");
                s.FishingState.AdvanceProgress(120); s.FishingState.CompleteBatch();
                int capacity = s.FishingState.RemainingCapacity;
                s.FishingState.FromDictionary(Roundtrip(s.FishingState.ToDictionary()));
                Check(s.FishingState.RemainingCapacity == capacity && capacity == 39, "fishing capacity survives serialization without refilling");
                Check(!s.FishingState.CanSelectPond("fishing_hualong_pond"), "advanced fishing requires its purchased permit");
                s.GardenState.SelectCrop("garden_spirit_herb"); s.GardenState.SelectPlot(0);
                s.BackpackState.AddItem("seed_spirit_herb", 10);
                int coins = s.ResourceWalletState.SpiritStones;
                Check(s.GardenState.TryPlantSelectedCrop(out _) && s.ResourceWalletState.SpiritStones == coins - 2, "planting charges a seed and two stones");
                Check(s.GardenState.TryResearchSelectedCrop(), "garden breeding consumes owned seeds");
                var gardenSave = s.GardenState.ToDictionary(); s.GardenState.FromDictionary(Roundtrip(gardenSave));
                Check(((Godot.Collections.Dictionary<string, Variant>)s.GardenState.ToDictionary()["breeding_level"])["garden_spirit_herb"].AsInt32() == 1, "breeding survives serialization");

                var rhythm = s.CultivationRhythmState!;
                rhythm.SetStrength(CultivationRhythmRules.StrengthNone);
                rhythm.FromDictionary(new() { ["enabled"] = true, ["strength"] = CultivationRhythmRules.StrengthNone, ["cycle_minutes"] = 30, ["current_cycle_active_seconds"] = 1799.0 });
                int cycles = rhythm.TotalSmallCycles;
                int rhythmEvents = s.EventLogState.GetEntries().Count(x => x.CategoryId == EventLogState.CategoryCycle);
                s.ActivityConversionService!.EmitSignal(ActivityConversionService.SignalName.SettlementApplied, 0.0, 1.0, 0.0, 0.0, 1.0);
                Check(rhythm.TotalSmallCycles == cycles + 1 && s.EventLogState.GetEntries().Count(x => x.CategoryId == EventLogState.CategoryCycle) == rhythmEvents + 1, "silent cycles still count active time and record history");
                s.InputActivityState._Process(6);
                double idleExp = s.PlayerProgressState.RealmExp;
                double idleQi = s.ResourceWalletState.Lingqi;
                s.ActivityConversionService._Process(60);
                Check(s.ResourceWalletState.Lingqi > idleQi && s.PlayerProgressState.RealmExp == idleExp, "idle rest recovers qi without generating input experience");
                Check(UiText.RealmStage(10, 0).StartsWith("筑基1层"), "major realm labels match the actual breakthrough gate");
                var offline = DungeonOfflineSettlementRules.BuildDungeonOfflineSettlement("lv_qi_001", 5000, .02, 20,
                    new CharacterStatBlock(10000, 1000, 100, 100, 0, 1.5), s.LevelConfigLoader.GetOfflineWeightedMonsters("lv_qi_001"),
                    1, 1, s.LevelConfigLoader.GetOfflineAverageItemDropsPerVictory("lv_qi_001"), int.MaxValue, 1, 2,
                    s.LevelConfigLoader.GetOfflineAverageEquipmentDropsPerVictory("lv_qi_001"),
                    s.LevelConfigLoader.BuildOfflineEquipmentFactory("lv_qi_001"), equipmentRoll: 0);
                Check(offline.EquipmentDrops.Count >= 1, "fractional equipment chance can produce an actual offline drop");
                Check(s.LevelConfigLoader.TryBuildLevelCompletionReward(out _, out _, out _, out _, out _, out _)
                    && s.LevelConfigLoader.GetLastGeneratedFirstClearEquipmentRewards().Any(x => x.EquipmentTemplateId == "eq_armor_qi_outer_moss_robe" && x.RarityTier == EquipmentRarityTier.Artifact), "first region guarantees the designed artifact robe");
                var book = root.GetNode<Control>("SubmenuBookWindow");
                Check(Descendants(book).OfType<ProgressBar>().Count() >= 11, "all subsystem mastery bars exist in the loaded UI");
                Check(Descendants(book).OfType<Button>().Any(x => x.Name == "MealUse"), "cooking exposes a meal activation control");
                Check(s.LevelConfigLoader.ValidationIssueCount == 0, "merged level and equipment configuration validates");
                var sunken = s.LevelConfigLoader.RollSunkenEquipment();
                Check(sunken != null && sunken.SourceStage == EquipmentSourceStage.Fishing && (sunken.RarityTier == EquipmentRarityTier.Artifact || sunken.RarityTier == EquipmentRarityTier.Spirit), "fishing rare reward creates a real artifact or spirit equipment instance");
                controller.FromRuntimeDictionary(new() { ["explore_progress"] = 0f });
                s.PlayerActionState.SetAction("dungeon");
                var defaultOffline = root.BuildOfflineDungeonSettlement(86400, 0);
                Check(defaultOffline.ActionTargetId == "lv_qi_001" && defaultOffline.EquipmentDrops.Count >= 1 && defaultOffline.ExploreProgressGain == 100, "default dungeon action resolves its offline equipment and target");
                s.PlayerActionState.SetAction("garden"); s.PlayerActionState.SetWorkMode(true); controller._Process(1.1);
                Check(!root.GetNode<Label>("MainBarWindow/Chrome/BattleTrack/BattleInfoLabel").Visible && !root.GetNode<Label>("MainBarWindow/Chrome/BattleTrack/RoundInfoLabel").Visible, "work mode stays quiet after garden presentation refresh");
                s.PlayerActionState.SetWorkMode(false); s.PlayerActionState.SetAction("fishing");
                int purchaseCoins = s.ResourceWalletState.SpiritStones;
                Check(!s.ShopState!.TryPurchase(ShopRules.ItemBackpackExpand1, out _) && s.ResourceWalletState.SpiritStones == purchaseCoins, "unimplemented backpack expansion cannot charge stones");
                Check(!s.ShopState.CanPurchase(ShopRules.ItemGardenPlot3, out _), "full garden does not sell redundant plot expansion");
                Check(s.ShopState.TryPurchase(ShopRules.ItemFishingPermitHualongpo, out _) && s.FishingState.CanSelectPond("fishing_hualong_pond"), "purchased fishing permit immediately unlocks the pond");
                RunReturnAndRecoveryChecks((PrototypeRootController)root, controller, s);
                RunProductionChecks((PrototypeRootController)root, controller, s);
                RunPreparationAndProjectChecks((PrototypeRootController)root, controller, s);
                RunConsumptionChecks((PrototypeRootController)root, controller, s);
                RunMasteryAvailabilityChecks((PrototypeRootController)root, controller, s);
                RunChallengeGoalChecks((PrototypeRootController)root, controller, s);
                RunProductionBlockChecks((PrototypeRootController)root, controller, s);
                GD.Print($"SMOKE SUCCESS: {_checks} checks");
                if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--render-smoke") >= 0) CaptureUi(root);
                else GetTree().Quit(0);
            }
            catch (Exception error) { GD.PushError($"SMOKE FAILURE: {error}"); GetTree().Quit(1); }
        }
        private void RunReturnAndRecoveryChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var rootType = typeof(PrototypeRootController);
            var settle = rootType.GetMethod("ApplyOfflineSettlementIfNeeded", flags)!;
            var savedAt = rootType.GetField("_lastLoadedSavedUnix", flags)!;
            var applied = rootType.GetField("_offlineSettlementApplied", flags)!;
            controller.FromRuntimeDictionary(new() { ["player_hp"] = 0, ["player_max_hp"] = 100, ["explore_progress"] = 100f, ["battle_state"] = "boss_ready" });
            s.PlayerActionState!.SetAction("dungeon");
            controller._Process(.1);
            Check(!controller.CanStartBossChallenge, "low HP gates Boss challenge during visible automatic rest");
            var button = root.GetNode<Control>("MainBarWindow/Chrome/BattleTrack").GetChildren().OfType<Button>().First(x => x.Text == "休整中…");
            Check(button.Visible && button.Disabled, $"resting Boss button remains visible and disabled (visible={button.Visible}, disabled={button.Disabled}, action={s.PlayerActionState.ActionId}, progress={controller.ExploreProgressValue})");
            controller.AdvanceHealthRecovery(2);
            int hp = controller.ToRuntimeDictionary()["player_hp"].AsInt32();
            controller.FromRuntimeDictionary(Roundtrip(controller.ToRuntimeDictionary()));
            Check(controller.ToRuntimeDictionary()["player_hp"].AsInt32() == hp, "native save roundtrip preserves injured HP");
            s.LevelConfigLoader!.TrySetActiveLevel("lv_qi_002");
            Check(controller.ToRuntimeDictionary()["player_hp"].AsInt32() == hp, "switching region preserves HP without a free refill");
            controller.AdvanceHealthRecovery(20);
            Check(controller.ToRuntimeDictionary()["player_hp"].AsInt32() == controller.ToRuntimeDictionary()["player_max_hp"].AsInt32(), "real-time rest restores HP without input");

            foreach (string mode in new[] { "cultivation", "dungeon", "alchemy", "smithing", "talisman", "cooking", "formation", "garden", "mining", "fishing", "body_cultivation" })
            {
                s.PlayerActionState.SetAction(mode);
                float mining = s.MiningState!.CurrentProgress, fishing = s.FishingState!.CurrentProgress, body = s.BodyCultivationState!.CurrentProgress;
                double before = s.PlayerProgressState!.RealmExp;
                savedAt.SetValue(root, (long)Time.GetUnixTimeFromSystem() - 600);
                applied.SetValue(root, false);
                Check((bool)settle.Invoke(root, null)! && s.PlayerProgressState.RealmExp - before >= 33.6 - .001
                    && s.PlayerProgressState.RealmExp - before < 33.8, $"actual return entry pays shared experience for {mode}");
                Check(s.MiningState.CurrentProgress == mining && s.FishingState.CurrentProgress == fishing && s.BodyCultivationState.CurrentProgress == body,
                    $"return from {mode} does not simulate unsupported gathering or body training");
                double settledExp = s.PlayerProgressState.RealmExp;
                Check(!(bool)settle.Invoke(root, null)! && s.PlayerProgressState.RealmExp == settledExp, $"return settlement for {mode} applies once per load");
            }

            s.CookingState!.FromDictionary(new());
            s.FormationState!.FromDictionary(new());
            s.PlayerProgressState!.FromDictionary(new() { ["realm_level"] = 9 });
            var expectedStats = CharacterStatRules.BuildFinalStats(PlayerBaseStatRules.BuildBaseStats(9, s.LevelConfigLoader!.PlayerBaseHp, s.LevelConfigLoader.PlayerAttackPerRound), s.EquippedItemsState!.GetEquippedProfiles());
            Check(controller.ToRuntimeDictionary()["player_max_hp"].AsInt32() == expectedStats.MaxHp, "realm change refreshes final maximum HP even while return summary is visible");
            s.PlayerActionState.SetAction("dungeon");
            s.LevelConfigLoader.TrySetActiveLevel("lv_qi_004");
            string boss = s.LevelConfigLoader.GetBossMonsterId();
            controller.FromRuntimeDictionary(new() { ["battle_state"] = "in_battle", ["battle_monster_id"] = boss,
                ["player_hp"] = 100, ["player_max_hp"] = 122, ["enemy_hp"] = 230, ["enemy_max_hp"] = 230,
                ["enemy_attack_power"] = 1, ["inputs_per_battle_round_runtime"] = 24, ["has_lingqi_drop_buff"] = true });
            // Disable critical luck to assert the real controller's Juling/healing interaction.
            typeof(ExploreProgressController).GetField("_battleConsumableModifier", flags)!.SetValue(controller, new CharacterStatModifier(CritChanceDelta: -1));
            typeof(ExploreProgressController).GetMethod("AdvanceBattleByInput", flags)!.Invoke(controller, new object[] { 24 });
            var stats = CharacterStatRules.BuildFinalStats(PlayerBaseStatRules.BuildBaseStats(9, s.LevelConfigLoader.PlayerBaseHp, s.LevelConfigLoader.PlayerAttackPerRound), s.EquippedItemsState!.GetEquippedProfiles());
            Check(controller.ToRuntimeDictionary()["enemy_hp"].AsInt32() == 230 - Math.Max(1, stats.Attack - 4) + 2, "saved Juling reward buff does not suppress healing in the actual battle controller");
            controller.FromRuntimeDictionary(new() { ["battle_state"] = "boss_ready", ["explore_progress"] = 100f, ["player_hp"] = 100 });
            s.SubsystemMasteryState!.FromDictionary(new() { ["body_cultivation"] = 2 });
            Check(s.PlayerProgressState.TryApplyBodyCultivationReward("body_cultivation_boneforge", 2, out string reward) && reward.Contains("额外防御 +1"), "first native skin training announces its effective starter defense");
            s.BodyCultivationState!.SelectRecipe("body_cultivation_boneforge"); s.PlayerActionState.SetAction("body_cultivation");
            string preview = (string)typeof(ExploreProgressController).GetMethod("BuildGenericRecipeProgressText", flags)!.Invoke(controller, null)!;
            Check(preview.Contains("DEF") && preview.Contains("→"), "body training shows actual next defense and integer threshold preview");
        }

        private void RunMasteryAvailabilityChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
        {
            var mastery = s.SubsystemMasteryState!; var wallet = s.ResourceWalletState!;
            wallet.AddInsight(1000); mastery.FromDictionary(new() { ["alchemy"] = 3, ["formation"] = 3 });
            var tabs = Descendants(root).OfType<BookTabsController>().Single(); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            var alchemyButton = (Button)tabs.FindChild("MasteryUpgrade_alchemy", true, false);
            var formationButton = (Button)tabs.FindChild("MasteryUpgrade_formation", true, false);
            Check(alchemyButton.Disabled && alchemyButton.Text.Contains("尚未开放") && alchemyButton.TooltipText.Contains("不会扣除悟性"),
                "unimplemented alchemy upgrade exposes a disabled informative entry even with abundant insight");
            double insight = wallet.Insight;
            alchemyButton.EmitSignal(Button.SignalName.Pressed);
            Check(mastery.GetLevel("alchemy") == 3 && wallet.Insight == insight, "direct UI signal cannot bypass reserved mastery purchase protection");
            Check(!formationButton.Disabled && formationButton.TooltipText.Contains("工巧阵盘") && !formationButton.TooltipText.Contains("自修复"),
                "formation upgrade advertises its actual craft recipe instead of unsupported repair");
            formationButton.EmitSignal(Button.SignalName.Pressed);
            Check(mastery.GetLevel("formation") == 4 && Math.Abs(wallet.Insight - (insight - 100)) < .001, "implemented formation upgrade charges exactly its existing cost");
            s.BackpackState!.AddItem("mithril", 1); s.BackpackState.AddItem("spirit_ink", 2);
            s.FormationState!.SelectRecipe("formation_craft_array"); s.FormationState.Production.Start(false, 1);
            s.PlayerActionState!.SetAction("formation"); controller.AdvanceOfflineCrafting(10000);
            Check(s.BackpackState.GetItemCount("formation_craft_array") > 0 && s.FormationState.TryActivatePrimary("formation_craft_array"),
                "formation level4 unlock is a funded producible and activatable craft array");
            Check(FormationRules.GetEffectProfile(s.FormationState.ActivePrimaryId, 4).CraftSpeedRate > 0, "advertised craft array provides actual processing speed");
            mastery.FromDictionary(new() { ["alchemy"] = 4, ["formation"] = 4 }); mastery.FromDictionary(Roundtrip(mastery.ToDictionary()));
            tabs.RestoreActiveTabs("CultivationTab", "BugTab"); insight = wallet.Insight;
            alchemyButton.EmitSignal(Button.SignalName.Pressed);
            Check(mastery.GetLevel("alchemy") == 4 && wallet.Insight == insight && alchemyButton.Text.Contains("旧档保留"),
                "legacy alchemy level4 survives native save roundtrip and cannot be recharged");
            Check(AlchemyRules.CompleteRecipe("recipe_huiqi_dan", 4).PotionCount == 3, "legacy alchemy keeps its existing bonus output");
        }

        private static Godot.Collections.Dictionary<string, Variant> Roundtrip(Godot.Collections.Dictionary<string, Variant> data)
        {
            using ConfigFile encoded = new(); using ConfigFile decoded = new();
            encoded.SetValue("smoke", "state", data);
            if (decoded.Parse(encoded.EncodeToText()) != Error.Ok) throw new InvalidOperationException("ConfigFile serialization failed");
            return (Godot.Collections.Dictionary<string, Variant>)decoded.GetValue("smoke", "state");
        }
        private static System.Collections.Generic.IEnumerable<Node> Descendants(Node node)
        {
            foreach (Node child in node.GetChildren())
            {
                yield return child;
                foreach (Node nested in Descendants(child)) yield return nested;
            }
        }
        private async void CaptureUi(Node root)
        {
            try
            {
                root.GetNode<Control>("SubmenuBookWindow").Show();
                foreach (var tabs in Descendants(root).OfType<BookTabsController>())
                {
                    tabs.RestoreActiveTabs("CultivationTab", "BugTab");
                    tabs.ShowReturnCard("闭关结算", "离线炼丹完成 2 批，获得丹药 6 颗；剩余进度保留。");
                }
                for (int frame = 0; frame < 3; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var capture = GetViewport().GetTexture().GetImage();
                if (capture.IsEmpty() || capture.SavePng("res://.godot/repair_ui.png") != Error.Ok) throw new InvalidOperationException("UI image capture failed");
                GD.Print("SMOKE UI: res://.godot/repair_ui.png");
                var scroll = Descendants(root).OfType<ScrollContainer>().First(x => x.Name == "CultivationScroll");
                Descendants(root).OfType<Button>().First(x => x.Name == "MasteryToggle").ButtonPressed = true;
                scroll.ScrollVertical = 700;
                for (int frame = 0; frame < 2; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var masteryCapture = GetViewport().GetTexture().GetImage();
                if (masteryCapture.SavePng("res://.godot/repair_ui_mastery.png") != Error.Ok) throw new InvalidOperationException("Mastery UI capture failed");
                GD.Print("SMOKE UI: res://.godot/repair_ui_mastery.png");
                foreach (var tabs in Descendants(root).OfType<BookTabsController>()) tabs.RestoreActiveTabs("ShopTab", "BugTab");
                Descendants(root).OfType<Button>().First(x => x.Text == "扩容").EmitSignal(Button.SignalName.Pressed);
                for (int frame = 0; frame < 2; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var shopCapture = GetViewport().GetTexture().GetImage();
                if (shopCapture.SavePng("res://.godot/repair_ui_shop.png") != Error.Ok) throw new InvalidOperationException("Shop UI capture failed");
                GD.Print("SMOKE UI: res://.godot/repair_ui_shop.png"); GetTree().Quit(0);
            }
            catch (Exception error) { GD.PushError($"SMOKE UI FAILURE: {error}"); GetTree().Quit(1); }
        }
    }
}
