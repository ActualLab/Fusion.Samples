using System.Diagnostics;
using ActualLab.Benchmarking;
using ActualLab.OS;

namespace Samples.RpcBenchmark.Client;

public class BenchmarkRunner : BenchmarkRunnerBase<double>
{
    private volatile Func<BenchmarkWorker, Func<CancellationToken, Task>> _currentOperationFactory = null!;

    public ClientCommand Command { get; }
    public bool IsStreaming { get; }
    public bool IsGrpc { get; }
    public BenchmarkWorker[] Workers { get; }

    public BenchmarkRunner(ClientCommand command, Func<ITestService> clientFactory, bool isStreaming)
    {
        Command = command;
        IsStreaming = isStreaming;
        TryCount = command.TryCount;
        TitleFormatter = title => $"  {title,-9}: ";
        ResultFormatter = x => $"{x.FormatCount(),7}";
        if (command.Benchmark == BenchmarkKind.Streams)
            Units = "items/s";
        var clientConcurrency = command.ClientConcurrencyValue;
        var workerCount = command.WorkersValue;

        var client = clientFactory.Invoke();
        IsGrpc = client is GrpcTestClient;
        Workers = new BenchmarkWorker[workerCount];
        for (var i = 0; i < Workers.Length; i++) {
            if (i % clientConcurrency == 0 && i != 0)
                client = clientFactory.Invoke();
            Workers[i] = IsGrpc
                ? new GrpcBenchmarkWorker(client)
                : new BenchmarkWorker(client);
        }
    }

    public async Task RunAll(string title, CancellationToken cancellationToken)
    {
        var clientCount = (Workers.Length + Command.ClientConcurrencyValue - 1) / Command.ClientConcurrencyValue;
        WriteLine($"{title} @ {Workers.Length} workers, {Command.ClientConcurrencyValue} concurrency ({clientCount} clients):");
        if (Command.Benchmark == BenchmarkKind.Calls) {
            await RunOneWithLatency("Sum", w => w.Sum, cancellationToken);
            await RunOneWithLatency("GetUser", w => w.GetUser, cancellationToken);
            await RunOneWithLatency("SayHello", w => w.SayHello, cancellationToken);
        }
        else {
            await RunOne("Stream1", w => w.Stream1, cancellationToken);
            await RunOne("Stream100", w => w.Stream100, cancellationToken);
            await RunOne("Stream10K", w => w.Stream10K, cancellationToken);
        }

        // Dispose clients (and the ServiceProvider each one owns) - in parallel, since a
        // graceful WebSocket close per client is slow when done sequentially.
        var clients = Workers.Select(w => w.Client).ToHashSet();
        await Task.WhenAll(clients.Select(ClientFactories.DisposeClient)).ConfigureAwait(false);
        await Task.Delay(500).ConfigureAwait(false); // Wait when HTTP connections actually get closed
    }

    // Protected & private methods

    protected override async Task Warmup(CancellationToken cancellationToken)
    {
        const int partCount = 3;
        var partDuration = Command.WarmupDuration / partCount;
        for (var i = 0; i < partCount; i++)
            await GetCallFrequency(partDuration, cancellationToken).ConfigureAwait(false);
    }

    protected override async Task<double> Benchmark(CancellationToken cancellationToken)
    {
        var callFrequency = await GetCallFrequency(Command.Duration, cancellationToken).ConfigureAwait(false);
        if (Command.Benchmark == BenchmarkKind.Streams)
            callFrequency *= BenchmarkWorker.StreamLength;
        return callFrequency;
    }

    private Task<double> GetCallFrequency(double duration, CancellationToken cancellationToken)
        => Benchmarks.CallFrequency(Workers, duration, cancellationToken, _currentOperationFactory, w => w.WhenReady());

    private Task RunOne(
        string title,
        Func<BenchmarkWorker, Func<CancellationToken, Task>> operationFactory,
        CancellationToken cancellationToken)
    {
        Title = title;
        Interlocked.Exchange(ref _currentOperationFactory, operationFactory);
        return Run(cancellationToken);
    }

    private async Task RunOneWithLatency(
        string title,
        Func<BenchmarkWorker, Func<CancellationToken, Task>> operationFactory,
        CancellationToken cancellationToken)
    {
        // Run throughput without trailing newline
        WriteNewLine = false;
        await RunOne(title, operationFactory, cancellationToken);
        WriteNewLine = true;

        // Run latency pass and append to same line
        var (p50, p95, p99) = await MeasureLatency(operationFactory, cancellationToken);
        WriteLine($", p50={p50}, p95={p95}, p99={p99}");
    }

    private async Task<(string P50, string P95, string P99)> MeasureLatency(
        Func<BenchmarkWorker, Func<CancellationToken, Task>> operationFactory,
        CancellationToken cancellationToken)
    {
        const int sampleMask = 127; // Sample every 128th call (~0.78%)

        var recorders = new LatencyRecorder[Workers.Length];
        for (var i = 0; i < recorders.Length; i++)
            recorders[i] = new LatencyRecorder();

        // Wrap operations to sample latency on ~1% of calls
        Func<int, Func<CancellationToken, Task>> wrappedFactory = i => {
            var recorder = recorders[i];
            var operation = operationFactory(Workers[i]);
            var count = 0L;
            return async ct => {
                if ((count++ & sampleMask) == 0) {
                    var start = Stopwatch.GetTimestamp();
                    await operation(ct).ConfigureAwait(false);
                    recorder.Record(Stopwatch.GetTimestamp() - start);
                }
                else {
                    await operation(ct).ConfigureAwait(false);
                }
            };
        };

        // Brief warmup (system is already warm from throughput pass)
        await Benchmarks.CallFrequency(
            Workers.Length, Command.WarmupDuration / 3, cancellationToken,
            wrappedFactory, i => Workers[i].WhenReady());

        // Reset recorders to discard warmup samples
        foreach (var r in recorders)
            r.Reset();
        GC.Collect();

        // Measure
        await Benchmarks.CallFrequency(
            Workers.Length, Command.Duration, cancellationToken, wrappedFactory);

        return LatencyRecorder.ComputePercentiles(recorders);
    }
}
