using System;

namespace Xiuxian.Scripts.Services
{
    public static class InputActivityRules
    {
        public readonly record struct DiscreteInputBatch(
            int KeyDownCount,
            int MouseClickCount,
            int MouseScrollSteps,
            int JoypadButtonCount,
            int JoypadAxisInputCount)
        {
            public int TotalCount => Math.Max(0, KeyDownCount)
                + Math.Max(0, MouseClickCount)
                + Math.Max(0, MouseScrollSteps)
                + Math.Max(0, JoypadButtonCount)
                + Math.Max(0, JoypadAxisInputCount);
        }

        public static int CalculateRemainingWindowAllowance(int currentWindowCount, int maxInputPerMinute)
        {
            if (maxInputPerMinute <= 0)
            {
                return int.MaxValue;
            }

            return Math.Max(0, maxInputPerMinute - Math.Max(0, currentWindowCount));
        }

        public static DiscreteInputBatch ClampDiscreteInputBatch(DiscreteInputBatch pendingBatch, int allowedCount)
        {
            int totalCount = pendingBatch.TotalCount;
            if (allowedCount >= totalCount)
            {
                return pendingBatch;
            }

            if (allowedCount <= 0 || totalCount <= 0)
            {
                return default;
            }

            int[] original =
            {
                Math.Max(0, pendingBatch.KeyDownCount),
                Math.Max(0, pendingBatch.MouseClickCount),
                Math.Max(0, pendingBatch.MouseScrollSteps),
                Math.Max(0, pendingBatch.JoypadButtonCount),
                Math.Max(0, pendingBatch.JoypadAxisInputCount),
            };
            int[] accepted = new int[original.Length];
            double[] remainders = new double[original.Length];
            int distributed = 0;

            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] <= 0)
                {
                    continue;
                }

                double scaled = (double)original[i] * allowedCount / totalCount;
                accepted[i] = Math.Min(original[i], (int)Math.Floor(scaled));
                remainders[i] = scaled - accepted[i];
                distributed += accepted[i];
            }

            int remaining = allowedCount - distributed;
            while (remaining > 0)
            {
                int bestIndex = -1;
                double bestRemainder = double.MinValue;
                for (int i = 0; i < original.Length; i++)
                {
                    if (accepted[i] >= original[i])
                    {
                        continue;
                    }

                    if (remainders[i] > bestRemainder)
                    {
                        bestRemainder = remainders[i];
                        bestIndex = i;
                    }
                }

                if (bestIndex < 0)
                {
                    break;
                }

                accepted[bestIndex]++;
                remainders[bestIndex] = 0.0;
                remaining--;
            }

            return new DiscreteInputBatch(
                accepted[0],
                accepted[1],
                accepted[2],
                accepted[3],
                accepted[4]);
        }

        public static double CalculateRawAp(
            DiscreteInputBatch batch,
            double mouseMoveDistancePx,
            double keyDownWeight,
            double mouseClickWeight,
            double scrollStepWeight,
            double movePxDivider,
            double joypadButtonWeight,
            double joypadAxisWeight)
        {
            double moveAp = movePxDivider > 0.0 ? Math.Max(0.0, mouseMoveDistancePx) / movePxDivider : 0.0;
            return Math.Max(0, batch.KeyDownCount) * keyDownWeight
                + Math.Max(0, batch.MouseClickCount) * mouseClickWeight
                + Math.Max(0, batch.MouseScrollSteps) * scrollStepWeight
                + Math.Max(0, batch.JoypadButtonCount) * joypadButtonWeight
                + Math.Max(0, batch.JoypadAxisInputCount) * joypadAxisWeight
                + moveAp;
        }

        public static double CalculateAccumulator(double currentAccumulator, double apFinal, double drainPerSecond, double delta)
        {
            return Math.Max(0.0, currentAccumulator + apFinal - drainPerSecond * delta);
        }
    }
}
