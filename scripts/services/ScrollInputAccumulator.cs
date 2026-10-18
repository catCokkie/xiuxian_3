using System;

namespace Xiuxian.Scripts.Services;

// Count wheel travel in detents, independently for each axis. Never retain direction.
public sealed class ScrollInputAccumulator
{
    private double _vertical;
    private double _horizontal;

    public int AddWindowsDelta(int delta, bool horizontal = false) => Add(Math.Abs((double)delta) / 120.0, horizontal);

    public int Add(double detents, bool horizontal = false)
    {
        if (!double.IsFinite(detents) || detents <= 0) return 0;
        ref double remainder = ref (horizontal ? ref _horizontal : ref _vertical);
        double total = remainder + detents;
        int steps = (int)Math.Min(int.MaxValue, Math.Floor(total + 1e-9));
        remainder = Math.Max(0, total - steps);
        return steps;
    }

    public void Reset() { _vertical = 0; _horizontal = 0; }
}
