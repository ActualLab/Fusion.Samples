using System.Net.Http;

namespace ActualLab.Benchmarking;

public static class Benchmarks
{
    private const long CancellationTokenSourceRenewMask = 127;
    private static readonly TimeSpan PreMeasureWarmup = TimeSpan.FromSeconds(0.5);

    public static HttpClient? RemoteGCClient { get; set; }

    public static async Task SettleDown()
    {
        if (RemoteGCClient != null)
            try { await RemoteGCClient.GetAsync("gc-collect").ConfigureAwait(false); } catch { }
        for (var i = 0; i < 3; i++) {
            GC.Collect();
            await Task.Delay(250);
        }
    }

    public static Task<double> CallFrequency(
        int workerCount,
        double duration,
        CancellationToken cancellationToken,
        Func<int, Func<CancellationToken, Task>> workerOperationFactory,
        Func<int, Task>? workerReadyFactory = null)
        => CallFrequency(
            Enumerable.Range(0, workerCount).ToArray(),
            duration,
            cancellationToken,
            workerOperationFactory,
            workerReadyFactory);

    public static async Task<double> CallFrequency<TWorker>(
        TWorker[] workers,
        double duration,
        CancellationToken cancellationToken,
        Func<TWorker, Func<CancellationToken, Task>> workerOperationFactory,
        Func<TWorker, Task>? workerReadyFactory = null,
        Func<TWorker, bool>? backgroundWorkerPredicate = null)
    {
        var timeRangeSource = TaskCompletionSourceExt.New<(CpuTimestamp MeasuresAt, CpuTimestamp EndsAt)>();
        var tasks = new (Task<long> Task, bool IsBackground)[workers.Length];
        for (var i = 0; i < workers.Length; i++) {
            var worker = workers[i];
            var task = CallCount(timeRangeSource.Task, cancellationToken, workerOperationFactory.Invoke(worker));
            var isBackground = backgroundWorkerPredicate != null && backgroundWorkerPredicate.Invoke(worker);
            tasks[i] = (task, isBackground);
            if (workerReadyFactory != null)
                await workerReadyFactory.Invoke(worker).ConfigureAwait(false);
        }
        var now = CpuTimestamp.Now;
        var measuresAt = now + PreMeasureWarmup;
        var endsAt = measuresAt + TimeSpan.FromSeconds(duration);
        timeRangeSource.SetResult((measuresAt, endsAt));

        var sum = 0L;
        foreach (var (task, isBackground) in tasks) {
            var result = await task.ConfigureAwait(false);
            if (!isBackground)
                sum += result;
        }
        return sum / duration;
    }

    public static async Task<long> CallCount(
        Task<(CpuTimestamp MeasuresAt, CpuTimestamp EndsAt)> timeRangeTask,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task> operation)
    {
        CancellationTokenSource? cts = null;
        var (measuresAt, endsAt) = await timeRangeTask.ConfigureAwait(false);

        // Pre-measurement warmup: throw calls until measurement window starts
        var now = CpuTimestamp.Now;
        while (now < measuresAt) {
            if (cts == null || cts.IsCancellationRequested) {
                cts?.Dispose();
                cts = cancellationToken.CreateLinkedTokenSource();
            }
            await operation.Invoke(cts!.Token).ConfigureAwait(false);
            now = CpuTimestamp.Now;
        }

        // Measurement: count all calls initiated before endsAt (let them complete even if they finish after)
        var count = 0L;
        now = CpuTimestamp.Now;
        while (now < endsAt) {
            if ((count & CancellationTokenSourceRenewMask) == 0) {
                cts?.Dispose();
                cts = cancellationToken.CreateLinkedTokenSource();
            }
            await operation.Invoke(cts!.Token).ConfigureAwait(false);
            count++;
            now = CpuTimestamp.Now;
        }
        cts?.Dispose();
        return count;
    }
}
