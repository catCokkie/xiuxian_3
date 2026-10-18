using System;

namespace Xiuxian.Scripts.Services;

// Presentation only. Never produces resources, input events, achievements or save data.
public sealed class GainFeedbackState
{
    public double Age { get; private set; } = 100;
    public double Pulse { get; private set; }
    public double Experience { get; private set; }
    public double Lingqi { get; private set; }
    public double Insight { get; private set; }
    public long Inputs { get; private set; }
    public bool Limited { get; private set; }
    private double _limitedAge;
    public const double BurstGapSeconds = 2.5;

    public void ObserveLimit(int raw, int accepted)
    {
        if (raw > 0)
        {
            Limited = accepted == 0; _limitedAge = 0;
            if (Limited) Pulse = 0;
        }
    }

    public void ObserveReward(int inputs, double ap, double experience, double lingqi, double insight)
    {
        // Passive recovery has its own source; it must not look like an input reward at the cap.
        if (ap <= 0 && experience <= 0 && insight <= 0) return;
        if (Age >= BurstGapSeconds) { Experience = Lingqi = Insight = 0; Inputs = 0; }
        Inputs += Math.Max(0, inputs);
        Experience += Math.Max(0, experience); Lingqi += Math.Max(0, lingqi); Insight += Math.Max(0, insight);
        Age = 0; Pulse = 1; Limited = false;
    }

    public void Tick(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0) return;
        Age += seconds; Pulse = Math.Max(0, Pulse - seconds / .45);
        _limitedAge += seconds;
        if (_limitedAge >= BurstGapSeconds) Limited = false;
    }

    public static string NextRealmGoal(int realm, double experience, double required, bool canBreakthrough, bool hasGate)
    {
        if (realm >= RealmBreakthroughRules.ChapterEndLevel) return "本篇境界已达上限 · 新经验继续保留";
        if (canBreakthrough) return "已可突破 · 提升境界，开启下一段修行";
        if (experience >= required && !hasGate) return "经验已足 · 击败最终 Boss 后即可突破";
        return $"距突破还需 {Math.Max(0, required - experience):0.##} 经验";
    }
}
