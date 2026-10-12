using System;

namespace Xiuxian.Scripts.Services
{
    public static class BattleRecoveryRules
    {
        public const double HpRatePerSecond = .05;
        public const double ReadyHpRate = .70;
        public readonly record struct Recovery(int Hp, double Remainder);

        public static int ReadyHp(int maxHp) => Math.Max(1, (int)Math.Ceiling(Math.Max(1, maxHp) * ReadyHpRate));
        public static bool CanFight(int hp, int maxHp) => hp >= ReadyHp(maxHp);
        public static double SecondsUntilReady(int hp, int maxHp)
            => Math.Max(0, ReadyHp(maxHp) - hp) / (Math.Max(1, maxHp) * HpRatePerSecond);

        public static Recovery Advance(int hp, int maxHp, double seconds, double remainder = 0, bool inBattle = false)
        {
            maxHp = Math.Max(1, maxHp);
            hp = Math.Clamp(hp, 0, maxHp);
            remainder = double.IsFinite(remainder) ? Math.Clamp(remainder, 0, .999999999) : 0;
            if (inBattle || !double.IsFinite(seconds) || seconds <= 0) return new(hp, remainder);
            double recovered = remainder + maxHp * HpRatePerSecond * Math.Min(seconds, 20);
            int gain = (int)Math.Floor(recovered + 1e-9);
            int next = Math.Min(maxHp, hp + gain);
            return new(next, next == maxHp ? 0 : Math.Max(0, recovered - gain));
        }
    }
}
