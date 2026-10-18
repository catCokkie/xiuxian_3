using System;
using System.Threading;

namespace Xiuxian.Scripts.Services;

public readonly record struct CapturedInputBatch(int Keys, int Clicks, int Scroll, double MouseDistance);

// One hook-thread producer, one game-thread consumer. The callback never waits for a game lock.
// Mouse coordinates and scroll fractions are transient producer state, never exported or logged.
public sealed class GlobalInputCaptureBuffer
{
    private int _keys, _clicks, _scroll;
    private double _distance;
    private int _captureState;
    private int _producerState = -1;
    private int _lastX, _lastY;
    private bool _hasPoint;
    private readonly ScrollInputAccumulator _wheel = new();

    public void SetPaused(bool paused)
    {
        int current = Volatile.Read(ref _captureState);
        Interlocked.Exchange(ref _captureState, unchecked(((current + 2) & ~1) | (paused ? 1 : 0)));
    }

    public void ObserveKeyDown()
    {
        if ((Volatile.Read(ref _captureState) & 1) == 0) Interlocked.Increment(ref _keys);
    }

    public void ObserveClick()
    {
        if ((Volatile.Read(ref _captureState) & 1) == 0) Interlocked.Increment(ref _clicks);
    }

    private bool BeginMouseObservation()
    {
        int state = Volatile.Read(ref _captureState);
        if ((state & 1) != 0) return false;
        if (state != _producerState)
        {
            _producerState = state; _hasPoint = false; _wheel.Reset();
        }
        return true;
    }

    public void ObserveScroll(int delta, bool horizontal)
    {
        if (!BeginMouseObservation()) return;
        int steps = _wheel.AddWindowsDelta(delta, horizontal);
        if (steps > 0) Interlocked.Add(ref _scroll, steps);
    }

    public void ObserveMouseMove(int x, int y)
    {
        if (!BeginMouseObservation()) return;
        if (_hasPoint)
        {
            double dx = (double)x - _lastX, dy = (double)y - _lastY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            // A brief CAS retry is independent of UI/save locks, with no per-event allocation.
            double before;
            do { before = Volatile.Read(ref _distance); }
            while (Interlocked.CompareExchange(ref _distance, before + distance, before) != before);
        }
        _lastX = x; _lastY = y; _hasPoint = true;
    }

    public CapturedInputBatch Drain() => new(Interlocked.Exchange(ref _keys, 0),
        Interlocked.Exchange(ref _clicks, 0), Interlocked.Exchange(ref _scroll, 0),
        Interlocked.Exchange(ref _distance, 0));
}
