using System;

namespace Xiuxian.Scripts.Services
{
    public static class BatchProgressRules
    {
        // A ready batch is frozen until materials are available and rewards are committed.
        public static float Advance(float current, int inputs, float required, double rate = 1.0)
            => current >= Math.Max(1, required) ? current : Math.Max(0, current) + (float)(Math.Max(0, inputs) * Math.Max(0, rate));
        public static float Consume(float current, float required) => Math.Max(0, current - Math.Max(1, required));
    }

    public sealed class ProgressRateAccumulator
    {
        public double Remainder { get; set; }
        public int Apply(int inputs, double multiplier)
        {
            double total = Math.Max(0, inputs) * Math.Max(0, multiplier) + Remainder;
            int whole = (int)Math.Floor(total + 1e-9);
            Remainder = Math.Max(0, total - whole);
            return whole;
        }
    }
}
