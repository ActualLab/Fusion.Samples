using ActualLab.OS;

namespace Samples.RpcBenchmark.Client;

public static class ParameterSearcher
{
    private const int TopCandidates = 3;
    private const int VerificationRuns = 2;

    public static async Task<(int Workers, int ClientConcurrency)> FindBest(
        ClientCommand command,
        Func<ITestService> clientFactory,
        bool isStreaming,
        CancellationToken cancellationToken)
    {
        var cpuCount = (int)HardwareInfo.ProcessorCount;
        var isCalls = command.Benchmark == BenchmarkKind.Calls;

        // Use shorter durations for search probes
        var testDuration = Math.Min(command.Duration, 2.0);
        var warmupDuration = Math.Min(command.WarmupDuration, 1.0);

        Func<BenchmarkWorker, Func<CancellationToken, Task>> operationFactory =
            isCalls ? w => w.Sum : w => w.Stream100;

        // Initial values
        var concurrency = isCalls ? 200 : 4;
        var workers = isCalls ? cpuCount * 400 : cpuCount * 8;

        // Ranges
        var minWorkers = Math.Max(cpuCount, 1);
        var maxWorkers = isCalls ? cpuCount * 2000 : cpuCount * 64;
        var minConcurrency = isCalls ? 5 : 1;
        var maxConcurrency = isCalls ? 1000 : 32;

        // Cache of all (workers, concurrency, score) results
        var cache = new List<(int Workers, int Concurrency, double Score)>();

        async Task<double> Evaluate(int w, int cc)
        {
            // Check cache for exact match
            foreach (var (cw, ccc, cs) in cache) {
                if (cw == w && ccc == cc)
                    return cs;
            }
            var score = await RunQuickTest(
                clientFactory, isStreaming, w, cc,
                testDuration, warmupDuration, operationFactory, command.Benchmark,
                cancellationToken);
            cache.Add((w, cc, score));
            return score;
        }

        // Initial worker search at default concurrency
        await Benchmarks.SettleDown();
        await Benchmarks.SettleDown();
        Write($"  cc={concurrency}, workers ");
        var (foundWorkers, score) = await ExponentialSearch("w", workers, minWorkers, maxWorkers, w => Evaluate(w, concurrency));
        WriteLine();
        if (score < 0)
            return (-1, -1); // Operation not supported by this framework
        workers = foundWorkers;

        // Coordinate descent: alternate between optimizing concurrency and workers
        for (var iter = 0; iter < 3; iter++) {
            await Benchmarks.SettleDown();
            await Benchmarks.SettleDown();
            Write($"  w={workers}, concurrency ");
            var (newConcurrency, _) = await ExponentialSearch(
                "cc", concurrency, minConcurrency, maxConcurrency, cc => Evaluate(workers, cc));
            WriteLine();
            var concurrencyChanged = newConcurrency != concurrency;
            concurrency = newConcurrency;

            if (!concurrencyChanged && iter > 0)
                break;

            await Benchmarks.SettleDown();
            await Benchmarks.SettleDown();
            Write($"  cc={concurrency}, workers ");
            var (newWorkers, __) = await ExponentialSearch(
                "w", workers, minWorkers, maxWorkers, w => Evaluate(w, concurrency));
            WriteLine();
            var workersChanged = newWorkers != workers;
            workers = newWorkers;

            if (!workersChanged)
                break;
        }

        // Verification: pick top candidates, triple-test each, select the best
        var candidates = cache
            .Where(r => r.Score > 0)
            .OrderByDescending(r => r.Score)
            .DistinctBy(r => (r.Workers, r.Concurrency))
            .Take(TopCandidates)
            .ToList();

        if (candidates.Count > 1) {
            Write("  Verifying top candidates: ");
            var bestScore = 0.0;
            var bestW = workers;
            var bestCC = concurrency;
            foreach (var (cw, ccc, cachedScore) in candidates) {
                var maxScore = cachedScore;
                for (var i = 0; i < VerificationRuns; i++) {
                    var s = await RunQuickTest(
                        clientFactory, isStreaming, cw, ccc,
                        testDuration, warmupDuration, operationFactory, command.Benchmark,
                        cancellationToken);
                    if (s > maxScore)
                        maxScore = s;
                }
                Write($"({cw},{ccc})->{maxScore.FormatCount()} ");
                if (maxScore > bestScore) {
                    bestScore = maxScore;
                    bestW = cw;
                    bestCC = ccc;
                }
            }
            workers = bestW;
            concurrency = bestCC;
            WriteLine();
        }

        return (workers, concurrency);
    }

    // Searches for the best value using exponentially decreasing steps in both directions.
    // Starts at 'initial', probes ±step where step halves each round.
    private static async Task<(int BestValue, double BestScore)> ExponentialSearch(
        string label, int initial, int min, int max, Func<int, Task<double>> evaluate)
    {
        initial = Math.Clamp(initial, min, max);
        var best = initial;
        var bestScore = await evaluate(initial);
        if (bestScore < 0) {
            Write($"{label}={initial}->n/a");
            return (best, bestScore);
        }
        Write($"{label}={initial}->{bestScore.FormatCount()}");

        var step = Math.Max(initial / 2, 1);
        var minStep = Math.Max(initial / 32, 1);

        while (step >= minStep) {
            var up = Math.Min(best + step, max);
            if (up != best) {
                var score = await evaluate(up);
                Write($", {up}->{score.FormatCount()}");
                if (score > bestScore) {
                    bestScore = score;
                    best = up;
                }
            }

            var down = Math.Max(best - step, min);
            if (down != best) {
                var score = await evaluate(down);
                Write($", {down}->{score.FormatCount()}");
                if (score > bestScore) {
                    bestScore = score;
                    best = down;
                }
            }

            step /= 2;
        }

        Write($" => {label}={best}");
        return (best, bestScore);
    }

    private static async Task<double> RunQuickTest(
        Func<ITestService> clientFactory,
        bool isStreaming,
        int workerCount,
        int clientConcurrency,
        double testDuration,
        double warmupDuration,
        Func<BenchmarkWorker, Func<CancellationToken, Task>> operationFactory,
        BenchmarkKind benchmarkKind,
        CancellationToken cancellationToken)
    {
        // Create workers
        var client = clientFactory.Invoke();
        var isGrpc = client is GrpcTestClient;
        var workers = new BenchmarkWorker[workerCount];
        for (var i = 0; i < workers.Length; i++) {
            if (i % clientConcurrency == 0 && i != 0)
                client = clientFactory.Invoke();
            workers[i] = isGrpc ? new GrpcBenchmarkWorker(client) : new BenchmarkWorker(client);
        }

        try {
            // Warmup
            await Benchmarks.CallFrequency(
                workers, warmupDuration, cancellationToken, operationFactory, w => w.WhenReady());

            // Reset
            GC.Collect();
            await Task.Delay(50, cancellationToken);

            // Measure
            var throughput = await Benchmarks.CallFrequency(
                workers, testDuration, cancellationToken, operationFactory);
            if (benchmarkKind == BenchmarkKind.Streams)
                throughput *= BenchmarkWorker.StreamLength;

            return throughput;
        }
        catch (Exception ex) when (ex is NotSupportedException or NotImplementedException) {
            return -1; // Signal that this framework doesn't support the operation
        }
        finally {
            // Dispose clients
            var clients = workers.Select(w => w.Client).ToHashSet();
            foreach (var c in clients)
                if (c is IDisposable d)
                    d.Dispose();
            await Task.Delay(200);
        }
    }
}
