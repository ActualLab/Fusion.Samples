using System.Runtime.CompilerServices;

namespace Samples.Blazor.UI.Services;

public sealed class FpsCounter(int ringBufferSize = 20)
{
    private RingBuffer<CpuTimestamp> _timestamps = new(ringBufferSize);

    public double Value {
        get {
            var frameCount = _timestamps.Count;
            if (frameCount < 2)
                return 0;

            var duration = (_timestamps[frameCount - 1] - _timestamps[0]).TotalSeconds;
            return (frameCount - 1) / duration;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddFrame()
        => AddFrame(CpuTimestamp.Now);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddFrame(CpuTimestamp timestamp)
        => _timestamps.PushTailAndMoveHeadIfFull(timestamp);
}
