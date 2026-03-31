using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Samples.RpcBenchmark.Client;

public sealed class LatencyRecorder
{
    private static readonly double TicksToUs = 1_000_000.0 / Stopwatch.Frequency;

    private readonly long[] _samples;
    private int _count;

    public int Count => _count;

    public LatencyRecorder(int capacity = 100_000)
    {
        _samples = new long[capacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Record(long elapsedTicks)
    {
        var idx = _count;
        if (idx < _samples.Length) {
            _samples[idx] = elapsedTicks;
            _count = idx + 1;
        }
    }

    public void Reset() => _count = 0;

    public static (string P50, string P95, string P99) ComputePercentiles(LatencyRecorder[] recorders)
    {
        var totalCount = 0;
        foreach (var r in recorders)
            totalCount += r.Count;

        if (totalCount == 0)
            return ("n/a", "n/a", "n/a");

        var all = new long[totalCount];
        var offset = 0;
        foreach (var r in recorders) {
            r._samples.AsSpan(0, r.Count).CopyTo(all.AsSpan(offset));
            offset += r.Count;
        }

        Array.Sort(all);

        return (
            FormatLatency(all[(int)(totalCount * 0.50)]),
            FormatLatency(all[(int)(totalCount * 0.95)]),
            FormatLatency(all[Math.Min((int)(totalCount * 0.99), totalCount - 1)])
        );
    }

    public static string FormatLatency(long ticks)
    {
        var us = ticks * TicksToUs;
        return us switch {
            < 1000 => $"{us:F0}μs",
            < 1_000_000 => $"{us / 1000:F1}ms",
            _ => $"{us / 1_000_000:F2}s",
        };
    }
}
