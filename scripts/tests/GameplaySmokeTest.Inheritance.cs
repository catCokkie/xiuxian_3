using Godot;
using System;
using System.Linq;
using System.Reflection;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests
{
    public partial class GameplaySmokeTest
    {
        private void RunInheritanceChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
        {
            var bag = s.BackpackState!; var equipped = s.EquippedItemsState!; var wallet = s.ResourceWalletState!;
            var smith = s.SmithingState!; var player = s.PlayerProgressState!; var loader = s.LevelConfigLoader!;
            var tabs = Descendants(root).OfType<BookTabsController>().Single();
            var original = new EquipmentInstanceData("inherit_old", "weapon_cold_iron_spear", "旧寒铁枪", EquipmentSlotType.Weapon,
                "cold_iron", EquipmentRarityTier.Spirit, EquipmentSourceStage.FirstClear, "lv_qi_001", "attack_flat", 5,
                new[] { new EquipmentSubStatData("max_hp_flat", 10), new EquipmentSubStatData("crit_chance_delta", .02) }, 6, 20, 2200000000L, false);
            var next = original with { EquipmentId = "inherit_new", DisplayName = "新寒铁枪", MainStatValue = 8, EnhanceLevel = 1, RarityTier = EquipmentRarityTier.CommonTool, ObtainedUnix = 2200000010L };
            void Reset()
            {
                s.ChallengeGoalState!.Cancel(); s.PlayerActionState!.SetAction("cultivation"); s.PlayerActionState.SetWorkMode(false);
                s.PlayerActionState.SetConsumablePolicy("none"); s.InputHookService!.SetPaused(true);
                player.AutoBreakthrough = false; player.FromDictionary(new() { ["realm_level"] = 3 });
                s.SubsystemMasteryState!.TrySetLevel("smithing", 2); s.FormationState!.FromDictionary(new()); s.CookingState!.FromDictionary(new());
                loader.FromRuntimeDictionary(new() { ["boss_cleared_level_ids"] = new Godot.Collections.Array<string> { "lv_qi_001" } });
                controller.FromRuntimeDictionary(new() { ["battle_state"] = "exploring", ["player_hp"] = 7 });
                equipped.FromDictionary(new()); bag.FromDictionary(new()); smith.FromDictionary(new());
                wallet.FromDictionary(new() { ["lingqi"] = 1000.0 }); bag.AddEquipmentInstance(original);
                EquipmentExchangeService.TryEquipById(s, original.EquipmentId); bag.AddEquipmentInstance(next);
            }
            EquipmentInheritanceCheck Preview() => EquipmentExchangeService.Preview(s, controller, original.EquipmentId, next.EquipmentId);
            bool Commit(EquipmentInheritanceCheck check) => EquipmentExchangeService.TryInherit(s, controller, check, out _);
            void Select(OptionButton option, string id)
            {
                for (int i = 0; i < option.ItemCount; i++) if (option.GetItemMetadata(i).AsString() == id)
                { option.Select(i); option.EmitSignal(OptionButton.SignalName.ItemSelected, (long)i); return; }
                throw new InvalidOperationException("Missing equipment option " + id);
            }
            Reset(); var reviewed = Preview(); var statsBefore = controller.GetPreparationStats();
            Check(reviewed.CanCommit && reviewed.LingqiCost == 83 && reviewed.ResultLevel == 3, "inherit: actual services apply source fee and target rarity cap");
            Check(wallet.Lingqi == 1000 && equipped.GetEquippedProfiles().Single().EnhanceLevel == 6 && bag.HasEquipment(next.EquipmentId), "inherit: preview never mutates equipment or funds");
            foreach (int gate in new[] { 0, 1, 2, 3, 4 })
            {
                Reset();
                if (gate == 0) player.FromDictionary(new() { ["realm_level"] = 2 });
                if (gate == 1) s.SubsystemMasteryState!.TrySetLevel("smithing", 1);
                if (gate == 2) loader.FromRuntimeDictionary(new());
                if (gate == 3) wallet.FromDictionary(new() { ["lingqi"] = 82.0 });
                if (gate == 4) controller.FromRuntimeDictionary(new() { ["battle_state"] = "in_battle", ["player_hp"] = 7 });
                double qi = wallet.Lingqi; Check(!Commit(Preview()) && wallet.Lingqi == qi && bag.HasEquipment(next.EquipmentId), "inherit: actual gate " + gate + " rejects without spending even outside dungeon action");
            }
            Reset(); reviewed = Preview(); equipped.TryEnhanceEquippedProfile(original.EquipmentId);
            Check(!Commit(reviewed) && wallet.Lingqi == 1000, "inherit: changed source invalidates reviewed preview");
            Reset(); reviewed = Preview(); bag.AddEquipmentInstance(next with { MainStatValue = 9 });
            Check(!Commit(reviewed) && wallet.Lingqi == 1000, "inherit: changed target rolls invalidate reviewed preview");
            Reset(); reviewed = Preview(); s.SubsystemMasteryState!.TrySetLevel("smithing", 4);
            // Target common cap remains3: a harmless mastery change is permitted when reviewed outcome and price are identical.
            Check(Commit(reviewed), "inherit: unchanged reviewed result survives irrelevant mastery increase");
            Reset(); bag.AddEquipmentInstance(next with { RarityTier = EquipmentRarityTier.Spirit }); reviewed = Preview(); s.SubsystemMasteryState!.TrySetLevel("smithing", 1);
            Check(!Commit(reviewed) && wallet.Lingqi == 1000, "inherit: changed unlock or cap cannot silently alter reviewed outcome");
            Reset(); equipped.Equip(EquipmentInstanceRules.ToStatProfile(original) with { EnhanceLevel = 9, IsEquipped = true });
            bag.AddEquipmentInstance(next with { RarityTier = EquipmentRarityTier.Spirit }); reviewed = Preview(); s.SubsystemMasteryState!.TrySetLevel("smithing", 4);
            Check(!Commit(reviewed) && wallet.Lingqi == 1000, "inherit: an unlocked but changed target cap requires a new reviewed preview");
            Reset(); smith.SelectTarget(next.EquipmentId, 1); smith.Production.Start(false, 4); smith.AdvanceProgress(55);
            smith.SelectTarget(original.EquipmentId, 6); smith.Production.Start(false, 3); smith.AdvanceProgress(77);
            bag.AddItem("lingqi_shard", 10); bag.SetReservedCount("lingqi_shard", 8);
            reviewed = Preview(); var projected = controller.GetPreparationStats(new[] { reviewed.Result });
            int hp = controller.ToRuntimeDictionary()["player_hp"].AsInt32();
            Check(Commit(reviewed), "inherit: confirmed same-slot transaction succeeds");
            Check(s.EventLogState!.GetEntries().First().CategoryId == EventLogState.CategoryEquipment
                && s.EventLogState.GetEntries().First().Message.Contains("已继承并换装"), "inherit: successful inheritance is recorded as an equipment event");
            Check(wallet.Lingqi == 917 && bag.GetItemCount("lingqi_shard") == 10 && bag.GetReservedCount("lingqi_shard") == 8, "inherit: exact qi fee without material refund or reserve mutation");
            Check(equipped.TryGetEquippedProfileById(next.EquipmentId, out var now) && now.EnhanceLevel == 3
                && bag.TryGetEquipmentById(original.EquipmentId, out var returned, out var oldMetadata) && returned.EnhanceLevel == 0,
                "inherit: target replaces its existing level and original instance returns at zero");
            Check(!Commit(reviewed) && wallet.Lingqi == 917 && bag.GetEquipmentProfiles().Length == 1, "inherit: duplicate confirmation cannot spend twice or duplicate gear");
            Check(controller.GetPreparationStats() == projected && controller.ToRuntimeDictionary()["player_hp"].AsInt32() == Math.Min(hp, projected.MaxHp), "inherit: preview equals runtime final stats and swap never heals current hp");
            Check(smith.TargetEquipmentId == next.EquipmentId && smith.CurrentProgress == 0 && smith.Production.Paused && smith.Production.RoundLimit == 4, "inherit: replacement project loses old-tier progress but keeps its own paused round budget");
            smith.SelectTarget(original.EquipmentId, 0);
            Check(smith.CurrentProgress == 0 && smith.Production.Paused && smith.Production.RoundLimit == 3, "inherit: returned source project also clears old-tier progress and preserves rounds");
            Check(equipped.TryGetEquippedInstanceById(next.EquipmentId, out var known) && known!.SourceStage == next.SourceStage && known.MainStatValue == 8 && known.ObtainedUnix == next.ObtainedUnix
                && known.SubStats.SequenceEqual(next.SubStats), "inherit: known target provenance and rolls are retained");
            equipped.FromDictionary(Roundtrip(equipped.ToDictionary())); bag.FromDictionary(Roundtrip(bag.ToDictionary()));
            Check(equipped.TryGetEquippedInstanceById(next.EquipmentId, out known) && known!.EnhanceLevel == 3 && bag.GetEquipmentInstances().Single().EnhanceLevel == 0, "inherit: equipped and returned instance metadata survive native save serialization");
            equipped.TryEnhanceEquippedProfile(next.EquipmentId); equipped.FromDictionary(Roundtrip(equipped.ToDictionary()));
            Check(equipped.TryGetEquippedInstanceById(next.EquipmentId, out known) && known!.EnhanceLevel == 4, "inherit: later enhancement keeps metadata level synchronized on save");
            var persistence = (StatePersistenceManager)typeof(PrototypeRootController).GetField("_persistenceManager", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(root)!;
            var config = new ConfigFile(); persistence.WriteAll(config);
            var savedGear = (Godot.Collections.Dictionary<string, Variant>)config.GetValue("equipment", "equipped");
            equipped.FromDictionary(Roundtrip(savedGear));
            Check(equipped.TryGetEquippedInstanceById(next.EquipmentId, out known) && known!.ObtainedUnix == next.ObtainedUnix, "inherit: root unified save includes equipped instance provenance");
            var dirty = typeof(PrototypeRootController).GetField("_saveDirty", BindingFlags.NonPublic | BindingFlags.Instance)!; dirty.SetValue(root, false);
            var cellClick = typeof(BookTabsController).GetMethod("OnBackpackGridCellClicked", BindingFlags.NonPublic | BindingFlags.Instance)!;
            cellClick.Invoke(tabs, new object[] { original.EquipmentId });
            Check(equipped.TryGetEquippedInstanceById(original.EquipmentId, out var restored) && restored!.ObtainedUnix == original.ObtainedUnix && restored.SubStats.SequenceEqual(original.SubStats), "equip: actual grid click equips the exact instance with original provenance");
            Check((bool)dirty.GetValue(root)! && bag.TryGetEquipmentById(next.EquipmentId, out var back, out known) && back.EnhanceLevel == 4 && known!.EnhanceLevel == 4, "equip: previous instance returns intact and manual swap dirties unified save");
            Reset(); var legacy = EquipmentInstanceRules.ToStatProfile(original) with { EquipmentId = "legacy_old", IsEquipped = true };
            equipped.FromDictionary(new()); equipped.Equip(legacy); reviewed = EquipmentExchangeService.Preview(s, controller, legacy.EquipmentId, next.EquipmentId);
            Check(Commit(reviewed) && bag.TryGetEquipmentById(legacy.EquipmentId, out _, out var unknown) && unknown == null, "inherit: legacy gear remains usable without inventing unknown production or source records");
            Reset(); smith.SelectTarget("unrelated", 0); smith.Production.Start(true, 1); smith.AdvanceProgress(45); Commit(Preview());
            Check(smith.TargetEquipmentId == "unrelated" && smith.CurrentProgress == 45 && smith.Production.CanProduce, "inherit: unrelated project progress and continuous budget stay intact");
            Reset(); tabs.FromSystemSettingsDictionary(new() { ["unlocked_equipment_tab"] = true, ["unlocked_backpack_tab"] = true }); tabs.RestoreActiveTabs("EquipmentTab", "BugTab");
            var toggle = (Button)tabs.FindChild("InheritanceToggle", true, false); var panel = (Control)tabs.FindChild("InheritancePanel", true, false);
            var source = (OptionButton)tabs.FindChild("InheritanceSource", true, false); var target = (OptionButton)tabs.FindChild("InheritanceTarget", true, false);
            var preview = (Label)tabs.FindChild("InheritancePreview", true, false); var confirm = (Button)tabs.FindChild("InheritanceConfirm", true, false);
            var grid = Descendants(root).OfType<BackpackGridController>().Single(); grid.Refresh();
            var cells = (System.Collections.Generic.List<Control>)typeof(BackpackGridController).GetField("_equipmentCells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(grid)!;
            Check(cells.Count == 1, "equip UI: a known bag instance has one cell rather than a duplicate legacy profile");
            Check(!toggle.ButtonPressed && !panel.Visible && confirm.Disabled, "inherit UI: collapsed by default and requires explicit target selection");
            toggle.ButtonPressed = true; Select(source, original.EquipmentId); Select(target, next.EquipmentId);
            Check(!confirm.Disabled && preview.Text.Contains("83 灵气") && preview.Text.Contains("将损失") && preview.Text.Contains("不相加") && preview.Text.Contains("攻击"), "inherit UI: exact fee cap loss replacement and character stat comparison are visible");
            Check(wallet.Lingqi == 1000 && s.PlayerActionState!.ActionId == "cultivation", "inherit UI: choosing items neither starts production nor changes action");
            dirty.SetValue(root, false); confirm.EmitSignal(Button.SignalName.Pressed);
            Check(wallet.Lingqi == 917 && equipped.TryGetEquippedProfileById(next.EquipmentId, out _) && (bool)dirty.GetValue(root)!, "inherit UI: explicit button performs transaction and marks save dirty");
            Check(((Label)tabs.FindChild("InheritanceResult", true, false)).Text.Contains("已继承并换装"), "inherit UI: success feedback identifies result and returned source");
            toggle.ButtonPressed = false;
        }
    }
}
