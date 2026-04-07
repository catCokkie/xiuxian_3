using Godot;

namespace Xiuxian.Scripts.Services
{
    public partial class PlayerStatsState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void StatsChangedEventHandler();

        public int TotalBattleLosses { get; private set; }
        public int TotalBossBattles { get; private set; }
        public int TotalEliteBattles { get; private set; }
        public int TotalAlchemyCrafts { get; private set; }
        public int TotalSmithingCrafts { get; private set; }
        public int TotalTalismanCrafts { get; private set; }
        public int TotalCookingCrafts { get; private set; }
        public int TotalFormationCrafts { get; private set; }
        public int TotalMiningCompletions { get; private set; }
        public int TotalFishingCompletions { get; private set; }
        public int TotalGardenPlants { get; private set; }
        public int TotalGardenHarvests { get; private set; }
        public int TotalGardenAutoHarvests { get; private set; }
        public int TotalSpentSpiritStones { get; private set; }
        public double TotalSpentInsight { get; private set; }

        public void RecordBattle(bool won, bool isBoss, bool isElite)
        {
            bool changed = false;
            if (!won)
            {
                TotalBattleLosses++;
                changed = true;
            }

            if (isBoss)
            {
                TotalBossBattles++;
                changed = true;
            }

            if (isElite)
            {
                TotalEliteBattles++;
                changed = true;
            }

            if (changed)
            {
                EmitSignal(SignalName.StatsChanged);
            }
        }

        public void RecordAlchemyCraft(int batchCount = 1)
        {
            if (batchCount <= 0)
            {
                return;
            }

            TotalAlchemyCrafts += batchCount;
            EmitSignal(SignalName.StatsChanged);
        }

        public void RecordSmithingCraft(int batchCount = 1)
        {
            if (batchCount <= 0)
            {
                return;
            }

            TotalSmithingCrafts += batchCount;
            EmitSignal(SignalName.StatsChanged);
        }

        public void RecordTalismanCraft(int batchCount = 1)
        {
            if (batchCount <= 0)
            {
                return;
            }

            TotalTalismanCrafts += batchCount;
            EmitSignal(SignalName.StatsChanged);
        }

        public void RecordCookingCraft(int batchCount = 1)
        {
            if (batchCount <= 0)
            {
                return;
            }

            TotalCookingCrafts += batchCount;
            EmitSignal(SignalName.StatsChanged);
        }

        public void RecordFormationCraft(int batchCount = 1)
        {
            if (batchCount <= 0)
            {
                return;
            }

            TotalFormationCrafts += batchCount;
            EmitSignal(SignalName.StatsChanged);
        }

        public void RecordMiningCompletion(int count = 1)
        {
            if (count <= 0)
            {
                return;
            }

            TotalMiningCompletions += count;
            EmitSignal(SignalName.StatsChanged);
        }

        public void RecordFishingCompletion(int count = 1)
        {
            if (count <= 0)
            {
                return;
            }

            TotalFishingCompletions += count;
            EmitSignal(SignalName.StatsChanged);
        }

        public void RecordGardenPlant(int count = 1)
        {
            if (count <= 0)
            {
                return;
            }

            TotalGardenPlants += count;
            EmitSignal(SignalName.StatsChanged);
        }

        public void RecordGardenHarvest(bool autoHarvest, int count = 1)
        {
            if (count <= 0)
            {
                return;
            }

            TotalGardenHarvests += count;
            if (autoHarvest)
            {
                TotalGardenAutoHarvests += count;
            }

            EmitSignal(SignalName.StatsChanged);
        }

        public void RecordSpiritStoneSpend(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            TotalSpentSpiritStones += amount;
            EmitSignal(SignalName.StatsChanged);
        }

        public void RecordInsightSpend(double amount)
        {
            if (amount <= 0.0)
            {
                return;
            }

            TotalSpentInsight += amount;
            EmitSignal(SignalName.StatsChanged);
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            PlayerStatsPersistenceRules.PlayerStatsSnapshot snapshot = new(
                TotalBattleLosses,
                TotalBossBattles,
                TotalEliteBattles,
                TotalAlchemyCrafts,
                TotalSmithingCrafts,
                TotalTalismanCrafts,
                TotalCookingCrafts,
                TotalFormationCrafts,
                TotalMiningCompletions,
                TotalFishingCompletions,
                TotalGardenPlants,
                TotalGardenHarvests,
                TotalGardenAutoHarvests,
                TotalSpentSpiritStones,
                TotalSpentInsight);
            return SaveValueConversionRules.ToVariantDictionary(PlayerStatsPersistenceRules.ToPlainDictionary(snapshot));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            PlayerStatsPersistenceRules.PlayerStatsSnapshot snapshot = PlayerStatsPersistenceRules.FromPlainDictionary(
                SaveValueConversionRules.ToPlainDictionary(data));
            TotalBattleLosses = snapshot.TotalBattleLosses;
            TotalBossBattles = snapshot.TotalBossBattles;
            TotalEliteBattles = snapshot.TotalEliteBattles;
            TotalAlchemyCrafts = snapshot.TotalAlchemyCrafts;
            TotalSmithingCrafts = snapshot.TotalSmithingCrafts;
            TotalTalismanCrafts = snapshot.TotalTalismanCrafts;
            TotalCookingCrafts = snapshot.TotalCookingCrafts;
            TotalFormationCrafts = snapshot.TotalFormationCrafts;
            TotalMiningCompletions = snapshot.TotalMiningCompletions;
            TotalFishingCompletions = snapshot.TotalFishingCompletions;
            TotalGardenPlants = snapshot.TotalGardenPlants;
            TotalGardenHarvests = snapshot.TotalGardenHarvests;
            TotalGardenAutoHarvests = snapshot.TotalGardenAutoHarvests;
            TotalSpentSpiritStones = snapshot.TotalSpentSpiritStones;
            TotalSpentInsight = snapshot.TotalSpentInsight;
            EmitSignal(SignalName.StatsChanged);
        }
    }
}
