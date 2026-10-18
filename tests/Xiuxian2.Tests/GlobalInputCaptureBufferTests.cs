using Xiuxian.Scripts.Services;

namespace Xiuxian2.Tests;

public class GlobalInputCaptureBufferTests
{
    [Fact]
    public void CapturesExactCountsAndPathWithoutPerEventAllocations()
    {
        var buffer = new GlobalInputCaptureBuffer(); buffer.ObserveMouseMove(0, 0); buffer.Drain();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 1; i <= 8000; i++)
        {
            buffer.ObserveMouseMove(i * 3, i * 4); buffer.ObserveKeyDown(); buffer.ObserveClick();
            buffer.ObserveScroll(30, false);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        var sample = buffer.Drain();
        Assert.Equal(8000, sample.Keys); Assert.Equal(8000, sample.Clicks);
        Assert.Equal(2000, sample.Scroll); Assert.Equal(40000, sample.MouseDistance, 6);
        Assert.Equal(0, allocated); Assert.Equal(default, buffer.Drain());
    }

    [Fact]
    public async Task ConcurrentDrainingConservesAllCountsAndMovement()
    {
        var buffer = new GlobalInputCaptureBuffer(); const int count = 100000;
        long keys = 0, clicks = 0, scroll = 0; double distance = 0;
        var producer = Task.Run(() =>
        {
            buffer.ObserveMouseMove(0, 0);
            for (int i = 1; i <= count; i++)
            {
                buffer.ObserveKeyDown(); buffer.ObserveClick(); buffer.ObserveScroll(120, false);
                buffer.ObserveMouseMove(i * 3, i * 4);
            }
        });
        void Drain()
        {
            var sample = buffer.Drain(); keys += sample.Keys; clicks += sample.Clicks;
            scroll += sample.Scroll; distance += sample.MouseDistance;
        }
        while (!producer.IsCompleted) { Drain(); await Task.Yield(); }
        await producer; Drain();
        Assert.Equal(count, keys); Assert.Equal(count, clicks); Assert.Equal(count, scroll);
        Assert.Equal(count * 5, distance, 5);
    }

    [Fact]
    public void PausePreservesCapturedTailAndRejectsNewInputWithoutACursorJump()
    {
        var buffer = new GlobalInputCaptureBuffer();
        buffer.ObserveKeyDown(); buffer.ObserveMouseMove(0, 0); buffer.ObserveMouseMove(3, 4);
        buffer.ObserveScroll(60, false); buffer.SetPaused(true);
        buffer.ObserveKeyDown(); buffer.ObserveClick(); buffer.ObserveMouseMove(9000, 9000);
        buffer.ObserveScroll(120, true);
        Assert.Equal(new CapturedInputBatch(1, 0, 0, 5), buffer.Drain());
        buffer.SetPaused(false); buffer.ObserveMouseMove(-50, -50); buffer.ObserveMouseMove(-47, -46);
        buffer.ObserveScroll(60, false); buffer.ObserveScroll(60, true);
        Assert.Equal(new CapturedInputBatch(0, 0, 0, 5), buffer.Drain());
    }

    [Fact]
    public void MouseDistanceHandlesNegativeAndLargeScreenCoordinates()
    {
        var buffer = new GlobalInputCaptureBuffer();
        buffer.ObserveMouseMove(int.MinValue, -2); buffer.ObserveMouseMove(int.MaxValue, -2);
        Assert.Equal(4294967295d, buffer.Drain().MouseDistance);
    }
}
