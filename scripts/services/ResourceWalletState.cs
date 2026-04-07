using Godot;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Runtime wallet for AP conversion outputs.
    /// </summary>
    public partial class ResourceWalletState : Node, IDictionaryPersistable
    {
        [Signal]
        public delegate void WalletChangedEventHandler(double lingqi, double insight, int spiritStones);

        public double Lingqi { get; private set; }
        public double Insight { get; private set; }
        public int SpiritStones { get; private set; }
        public double TotalEarnedLingqi { get; private set; }
        public double TotalEarnedInsight { get; private set; }
        public int TotalEarnedSpiritStones { get; private set; }

        public void AddLingqi(double amount)
        {
            if (amount <= 0.0)
            {
                return;
            }

            Lingqi += amount;
            TotalEarnedLingqi += amount;
            EmitSignal(SignalName.WalletChanged, Lingqi, Insight, SpiritStones);
        }

        public bool SpendLingqi(double amount)
        {
            if (amount <= 0.0)
            {
                return true;
            }

            if (Lingqi < amount)
            {
                return false;
            }

            Lingqi -= amount;
            EmitSignal(SignalName.WalletChanged, Lingqi, Insight, SpiritStones);
            return true;
        }

        public void AddInsight(double amount)
        {
            if (amount <= 0.0)
            {
                return;
            }

            Insight += amount;
            TotalEarnedInsight += amount;
            EmitSignal(SignalName.WalletChanged, Lingqi, Insight, SpiritStones);
        }

        public bool SpendInsight(double amount)
        {
            if (amount <= 0.0)
            {
                return true;
            }

            if (Insight < amount)
            {
                return false;
            }

            Insight -= amount;
            ServiceLocator.Instance?.PlayerStatsState?.RecordInsightSpend(amount);
            EmitSignal(SignalName.WalletChanged, Lingqi, Insight, SpiritStones);
            return true;
        }

        public void AddSpiritStones(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SpiritStones += amount;
            TotalEarnedSpiritStones += amount;
            EmitSignal(SignalName.WalletChanged, Lingqi, Insight, SpiritStones);
        }

        public bool SpendSpiritStones(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (SpiritStones < amount)
            {
                return false;
            }

            SpiritStones -= amount;
            ServiceLocator.Instance?.PlayerStatsState?.RecordSpiritStoneSpend(amount);
            EmitSignal(SignalName.WalletChanged, Lingqi, Insight, SpiritStones);
            return true;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            ResourceWalletPersistenceRules.WalletSnapshot snapshot = new(
                Lingqi,
                Insight,
                SpiritStones,
                TotalEarnedLingqi,
                TotalEarnedInsight,
                TotalEarnedSpiritStones);
            return SaveValueConversionRules.ToVariantDictionary(ResourceWalletPersistenceRules.ToPlainDictionary(snapshot));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            ResourceWalletPersistenceRules.WalletSnapshot snapshot = ResourceWalletPersistenceRules.FromPlainDictionary(
                SaveValueConversionRules.ToPlainDictionary(data));
            Lingqi = snapshot.Lingqi;
            Insight = snapshot.Insight;
            SpiritStones = snapshot.SpiritStones;
            TotalEarnedLingqi = snapshot.TotalEarnedLingqi;
            TotalEarnedInsight = snapshot.TotalEarnedInsight;
            TotalEarnedSpiritStones = snapshot.TotalEarnedSpiritStones;
            EmitSignal(SignalName.WalletChanged, Lingqi, Insight, SpiritStones);
        }
    }
}
