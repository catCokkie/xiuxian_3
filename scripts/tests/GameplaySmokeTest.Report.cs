using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Xiuxian.Scripts.Game;
using Xiuxian.Scripts.Services;

namespace Xiuxian.Scripts.Tests
{
    public partial class GameplaySmokeTest
    {
        private void RunBossReportChecks(PrototypeRootController root, ExploreProgressController controller, ServiceLocator s)
        {
            var state = s.ChallengeGoalState!; var loader = s.LevelConfigLoader!;
            s.PlayerProgressState!.FromDictionary(new() { ["realm_level"] = 9 });
            s.SubsystemMasteryState!.FromDictionary(new() { ["talisman"] = 4, ["cooking"] = 4 });
            s.CookingState!.FromDictionary(new()); s.BackpackState!.FromDictionary(new()); s.PotionInventoryState!.FromDictionary(new()); state.FromDictionary(new());
            s.PlayerActionState!.SetConsumablePolicy("none"); s.PlayerActionState.SetAction("dungeon"); loader.TrySetActiveLevelIfUnlocked("lv_qi_005");
            string boss = loader.GetBossMonsterId(); state.TryStart(loader.ActiveLevelId, boss, new System.Collections.Generic.Dictionary<string, int>());
            int oldRoundLimit = controller.MaxBossBattleRounds;
            var tabs = Descendants(root).OfType<BookTabsController>().Single();
            void Begin(int limit)
            {
                controller.MaxBossBattleRounds = limit;
                controller.FromRuntimeDictionary(new() { ["explore_progress"] = 100f, ["battle_state"] = "boss_ready" });
                controller.AdvanceHealthRecovery(100);
                if (!controller.StartBossChallengeFromUi()) throw new InvalidOperationException("real report fixture could not start Boss challenge");
            }
            void Input(int count)
            {
                for (int i = 0; i < count; i++) s.InputActivityState!.RegisterKeyDown(); s.InputActivityState!._Process(61);
            }
            Begin(20); var battle = controller.ToRuntimeDictionary(); battle["player_hp"] = 1; battle["enemy_attack_power"] = 10000; battle["enemy_hp"] = 100000; battle["enemy_max_hp"] = 100000;
            controller.FromRuntimeDictionary(battle); Input(battle["inputs_per_battle_round_runtime"].AsInt32());
            var death = state.LastBossReport!;
            Check(death.Result == "death" && death.Rounds == 1 && death.EnemyHp > 0 && controller.ToRuntimeDictionary()["battle_round_counter"].AsInt32() == 0,
                "Boss death captures actual rounds and remaining HP before defeat resets runtime");
            Check(death.StatisticsComplete && !state.Goal.Completed && death.EnemyHealing == 0 && death.Potions.Count == 0, "lethal death round invents neither healing nor supply usage nor goal completion");
            tabs.RestoreActiveTabs("CultivationTab", "BugTab"); var toggle = (Button)tabs.FindChild("BossReviewToggle", true, false);
            Check(toggle.Visible && !toggle.ButtonPressed, "last Boss report is available as an optional collapsed review");
            toggle.ButtonPressed = true;
            var summary = (Label)tabs.FindChild("BossReviewSummary", true, false);
            Check(summary.Text.Contains("优先比较护具") && summary.Text.Contains("敌方剩余气血"), "death review prioritizes survival from real facts");
            state.FromDictionary(Roundtrip(state.ToDictionary())); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(state.LastBossReport?.Result == "death" && state.LastBossReport.Rounds == 1 && !state.Goal.Completed, "Boss report survives native serialization independently of goal completion");
            state.Cancel(); Check(state.LastBossReport?.Result == "death", "canceling a goal retains the last real Boss report for review");
            string healingRegion = loader.GetUnlockedLevelIds().First(id => loader.TryGetMonsterStatProfile(loader.GetBossMonsterId(id), out var profile) && profile.HealPerRound > 0);
            loader.TrySetActiveLevelIfUnlocked(healingRegion); boss = loader.GetBossMonsterId();
            state.TryStart(loader.ActiveLevelId, boss, new System.Collections.Generic.Dictionary<string, int>());
            Begin(2); battle = controller.ToRuntimeDictionary(); battle["enemy_attack_power"] = 1; battle["enemy_hp"] = 100000; battle["enemy_max_hp"] = 100000;
            controller.FromRuntimeDictionary(battle); int threshold = battle["inputs_per_battle_round_runtime"].AsInt32(); Input(threshold);
            var mid = controller.ToRuntimeDictionary(); var attempt = (Godot.Collections.Dictionary<string, Variant>)mid["boss_attempt"];
            int healing = attempt["enemy_healing"].AsInt32();
            Check(healing > 0 && state.LastBossReport!.Result == "death", "ongoing Boss regeneration is observed without overwriting the last completed report");
            controller.FromRuntimeDictionary(Roundtrip(mid)); Input(threshold);
            var timeout = state.LastBossReport!;
            Check(timeout.Result == "timeout" && timeout.Rounds == 2 && timeout.EnemyHp > 0 && timeout.EnemyHealing >= healing && timeout.StatisticsComplete,
                "real timeout includes capped enemy recovery and restores mid-battle statistics");
            Check(BossChallengeReportRules.Advice(timeout)[0].Contains("武器") && !state.Goal.Completed, "timeout prioritizes output and does not finish the challenge goal");
            string armorRegion = loader.GetUnlockedLevelIds().First(id => loader.TryGetMonsterStatProfile(loader.GetBossMonsterId(id), out var profile) && profile.StrategyTag == "armor");
            loader.TrySetActiveLevelIfUnlocked(armorRegion); string armorBoss = loader.GetBossMonsterId();
            state.TryStart(armorRegion, armorBoss, new System.Collections.Generic.Dictionary<string, int>()); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(summary.Text.Contains("其他目标") && !((Button)tabs.FindChild("BossReviewPreparation", true, false)).Visible,
                "a report from another Boss is not used as the current goal's adjustment advice");
            s.PlayerActionState.SetConsumablePolicy("all"); s.PotionInventoryState.AddPotion("potion_huiqi_dan", 1); s.BackpackState.AddItem("talisman_fire_charm", 1);
            s.BackpackState.AddItem("food_iron_fish_meal", 1); s.CookingState.TryActivateMeal("food_iron_fish_meal");
            Begin(2); battle = controller.ToRuntimeDictionary(); battle["player_hp"] = Math.Max(1, battle["player_max_hp"].AsInt32() / 4); battle["enemy_attack_power"] = 1;
            battle["enemy_hp"] = 100000; battle["enemy_max_hp"] = 100000; controller.FromRuntimeDictionary(battle);
            threshold = battle["inputs_per_battle_round_runtime"].AsInt32(); Input(threshold); mid = controller.ToRuntimeDictionary();
            attempt = (Godot.Collections.Dictionary<string, Variant>)mid["boss_attempt"];
            var potions = (Godot.Collections.Dictionary<string, Variant>)attempt["potions"]; var talismans = (Godot.Collections.Dictionary<string, Variant>)attempt["talismans"];
            Check(potions.ContainsKey("potion_huiqi_dan") && potions["potion_huiqi_dan"].AsInt32() == 1 && talismans.ContainsKey("talisman_fire_charm") && talismans["talisman_fire_charm"].AsInt32() == 1,
                "Boss statistics record only successfully consumed potion and relevant talisman IDs");
            Check(attempt["player_healing"].AsInt32() > 5 && attempt["damage_to_boss"].AsInt32() > 0, "actual potion meal recovery and talisman damage are counted without nominal overfill");
            controller.FromRuntimeDictionary(Roundtrip(mid)); Input(threshold);
            Check(state.LastBossReport!.Potions.GetValueOrDefault("potion_huiqi_dan") == 1 && state.LastBossReport.Talismans.GetValueOrDefault("talisman_fire_charm") == 1 && state.LastBossReport.StatisticsComplete,
                "supply counts survive mid-battle reload and are not charged or counted twice");
            s.PlayerActionState.SetConsumablePolicy("none"); s.CookingState.FromDictionary(new()); Begin(2);
            battle = controller.ToRuntimeDictionary(); battle.Remove("boss_attempt"); battle["battle_round_counter"] = 1; battle["enemy_hp"] = 100000; battle["enemy_max_hp"] = 100000; battle["enemy_attack_power"] = 1;
            controller.FromRuntimeDictionary(battle); Input(battle["inputs_per_battle_round_runtime"].AsInt32()); tabs.RestoreActiveTabs("CultivationTab", "BugTab");
            Check(state.LastBossReport!.Result == "timeout" && !state.LastBossReport.StatisticsComplete && summary.Text.Contains("统计不完整"),
                "legacy in-progress battle labels unavailable historical counters instead of fabricating a complete summary");
            Begin(20); battle = controller.ToRuntimeDictionary(); battle["enemy_hp"] = 1; battle["enemy_attack_power"] = 1; controller.FromRuntimeDictionary(battle); Input(battle["inputs_per_battle_round_runtime"].AsInt32());
            var victory = state.LastBossReport!;
            Check(victory.Result == "victory" && victory.EnemyHp == 0 && victory.BossId == armorBoss && victory.LevelId == armorRegion && state.Goal.Completed,
                "victory report captures original Boss identity before automatic region advancement");
            double qi = s.ResourceWalletState!.Lingqi; int wins = controller.ToRuntimeDictionary()["total_battle_win_count"].AsInt32();
            state.FromDictionary(Roundtrip(state.ToDictionary())); Check(state.LastBossReport!.Result == "victory" && s.ResourceWalletState.Lingqi == qi && controller.ToRuntimeDictionary()["total_battle_win_count"].AsInt32() == wins,
                "reading a saved victory report cannot replay rewards or battle statistics");
            state.Cancel(); toggle.ButtonPressed = false; s.PlayerActionState.SetConsumablePolicy("none");
            battle = controller.ToRuntimeDictionary(); battle["explore_progress"] = 0f; battle["battle_state"] = "in_battle";
            battle["enemy_hp"] = 1; battle["enemy_attack_power"] = 1; battle["battle_monster_id"] = loader.GetOfflineWeightedMonsters(loader.ActiveLevelId).First().Monster.MonsterId;
            controller.FromRuntimeDictionary(battle); Input(battle["inputs_per_battle_round_runtime"].AsInt32());
            Check(state.LastBossReport!.BossId == armorBoss && state.LastBossReport.TimestampUnix == victory.TimestampUnix, "ordinary encounter never overwrites the Boss review archive");
            controller.MaxBossBattleRounds = oldRoundLimit;
        }
    }
}
