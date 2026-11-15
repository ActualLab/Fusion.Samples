# Benchmark Results

**Updated:** 2026-11-14<br/>
**ActualLab.Fusion Version:** 15.1.38

This page summarizes benchmark results from ActualLab.Fusion.Samples repository.

> 📊 A rendered version of these results — with charts and framework comparisons — is at
> [**fusion.actuallab.net/Performance**](https://fusion.actuallab.net/Performance).

## Test Environment

| Component | Specification |
|-----------|---------------|
| **CPU** | AMD Ryzen 9 9950X3D 16-Core Processor |
| **RAM** | 96 GB DDR5 |
| **OS** | Windows 11 |
| **.NET Version** | 11.0.0 |

Note that Ryzen 9 9950X3D has 32 logical cores due to SMT.

> **Windows loopback setup (required for representative numbers).** These benchmarks run over
> localhost, so Windows loopback settings gate their throughput. Enabling **loopback large MTU**
> (`Set-LoopbackMode.ps1 enable`, or `netsh int ipv4/ipv6 set gl loopbacklargemtu=enable`) roughly
> doubles localhost stream throughput; on recent Windows 11 builds the effective default throttles
> it. **Run RpcBenchmark with large MTU ENABLED** (streams need it). **Run Run-Benchmark.cmd with it
> DISABLED** — large MTU hangs its many-connection HTTP/DB loopback path; that benchmark is
> call/HTTP-based and doesn't need it. gRPC/SignalR results are more variable on Windows loopback.

## Reference Redis Benchmark

Redis is one of the fastest network servers you can put next to ActualLab.Rpc, so we use it as a
reference point. We ran `redis-benchmark` (Redis 8.10.3, `GET` only) on the same machine and in the same
setup as the Docker RPC benchmarks below: the server container is limited to 4 CPUs, and the clients run in
separate containers with their own cores.

The numbers below are the peak throughput we could get from Redis under that limit, with the server's
CPU saturated.

| Redis configuration | Pipelining | GET throughput | Per core | Server CPU |
|---------------------|------------|----------------|----------|------------|
| Single thread (default) | none | 77.33K req/s | 77K | 100% (1 core) |
| Single thread (default) | 16 commands | 1.30M req/s | 1.30M | 98% (1 core) |
| 6 I/O threads | none | 212.91K req/s | 53K | 405% (4 cores) |
| 6 I/O threads | 16 commands | **2.48M req/s** | 621K | 416% (4 cores) |

For comparison, ActualLab.Rpc under the same 4-core limit (see Docker Calls below):

| Test | Throughput | Per core |
|------|------------|----------|
| GetUser | 4.13M calls/s | 1.03M |
| Sum | 5.25M calls/s | 1.31M |

**This is not an apples-to-apples comparison, and it favors Redis.** With pipelining,
`redis-benchmark` builds batches of 16 commands by hand and sends them back-to-back on every
connection, so the server reads 16 commands and writes 16 replies per network operation. In our RPC
benchmarks every caller makes a single call and awaits its result; batching happens automatically in
the transport, and only when calls happen to queue up. The fair counterpart of our numbers is the
"none" rows; the pipelined rows are a best case that a real application rarely reaches.

Redis also scales less than linearly here: one core handles 1.30M pipelined requests per second, but
four saturated cores handle 2.48M, or 621K per core. Its I/O threads take over reading, parsing and
writing, while command execution stays on a single thread.

Each figure is the best of two 10-second runs. Totals for multi-client runs are the sum of per-client
rates, so they are accurate to within a few percent.

To run it: start the server with
`docker run -d --name redis_srv --network <net> --cpus 4 redis redis-server --save "" --appendonly no --io-threads 6`
(omit `--io-threads` for the single-thread rows), then start the clients in the same Docker network, each as
`docker run --rm --network <net> --cpus 1 redis timeout 10 redis-benchmark -h redis_srv -t get -n 2000000000 -c 50 -P 16 -q`
(`-P 1` for the rows without pipelining) and add up the "overall" rates they report after 10 seconds.
Single-thread rows use 1 client (pipelined) or 8 clients; 6-I/O-thread rows use 24 clients, with
`-c 200` for the row without pipelining.

## Run-Benchmark.cmd

The benchmark measures throughput of a simple repository-style user lookup service that retrieves and updates user records from a database: `UserService.Get(userId)` and `Update(userId, ...)`.

To run the benchmark:
```powershell
dotnet run -c Release --project src/Benchmark/Benchmark.csproj --no-launch-profile
```

### Local Services

| Test | Result | Speedup |
|------|--------|---------|
| Regular Service | 161.23K calls/s | |
| Fusion Service | 330.53M calls/s | **~2,050x** |

### Remote Services

| Test | Result | Speedup |
|------|--------|---------|
| HTTP Client → Regular Service | 103.88K calls/s | |
| HTTP Client → Fusion Service | 365.18K calls/s | **~3.5x** |
| ActualLab.Rpc Client → Fusion Service | 7.81M calls/s | **~75x** |
| Fusion Client → Fusion Service | 226.05M calls/s | **~2,176x** |

The Regular Service rows depend on PostgreSQL: these figures are from a dedicated `postgres` container
(PostgreSQL 17.5, `docker-compose.yml` settings). The HTTP rows vary a lot between tries on Windows
loopback (one test can range from 10K to 100K+ calls/s), so each cell is the best try.

### What each scenario means

All scenarios call the *same* `UserService.Get(userId)`. What differs is **who calls it and over what
transport** — which is exactly what the numbers above are measuring.

**Local (in-process, no network):**
- **Regular Service** — a plain method; every call runs the EF Core query against the database. This is
  the DB-bound baseline.
- **Fusion Service** — the same method marked `[ComputeMethod]`. Results are cached in memory, so repeat
  calls are served from Fusion's cache without touching the database.

**Remote (a client calls the server over a network transport):**
- **HTTP Client → Regular Service** — a conventional REST/HTTP call to the plain service; each call
  round-trips to the server and hits the DB. The typical web-API baseline.
- **HTTP Client → Fusion Service** — a REST/HTTP call to the Fusion-cached service. The server answers
  from cache, but every call still pays the HTTP round-trip plus JSON (de)serialization — so the ceiling
  is the HTTP transport, not Fusion.
- **ActualLab.Rpc Client → Fusion Service** — the same remote call, but over **ActualLab.Rpc** (Fusion's
  own RPC protocol on a persistent WebSocket) instead of HTTP. It still makes a real network round-trip
  per call, but the protocol is far leaner than HTTP/REST — hence ~7.8M vs ~365K calls/s.
- **Fusion Client → Fusion Service** — a **Fusion client** (a *Compute Service Client*). It doesn't proxy
  every call over the wire: it keeps a client-side `Computed<T>` cache mirroring the server's and the
  server pushes invalidations to it. So after the first call, repeat calls are served from the client's
  local cache with **no** network round-trip at all (until the server invalidates that value) — which is
  why it approaches local Fusion-Service speed (~226M calls/s).

In short: `ActualLab.Rpc Client` = the fastest way to make a *real* remote call every time;
`Fusion Client` = eliminating most of those remote calls entirely via client-side caching + invalidation.

## Run-RpcBenchmark.cmd

This benchmark compares **ActualLab.Rpc** with **gRPC**, **SignalR**, and other RPC frameworks.
The tables below include only **ActualLab.Rpc**, **gRPC**, and **SignalR**.
Other options, such as **StreamJsonRpc** and **RESTful API**, are way slower, so we omit them.

There are two benchmarks in `RpcBenchmark` project:

RPC calls:
```powershell
dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b calls -l rpc,grpc,signalr -f msgpack6c -s -n 6
```

RPC streaming:
```powershell
dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b streams -l rpc,grpc,signalr -f msgpack6c -s -n 6
```

### Calls

| Test | ActualLab.Rpc | gRPC | SignalR |
|------|---------------|------|---------|
| Sum | 9.97M calls/s | 842.95K calls/s | 4.84M calls/s |
| GetUser | 8.91M calls/s | 848.30K calls/s | 4.28M calls/s |
| SayHello | 5.95M calls/s | 808.67K calls/s | 2.23M calls/s |

_gRPC call throughput is lower on .NET 11.0.0 than it was on .NET 10.0.8 (1.28M / 1.24M / 1.16M calls/s
with the same benchmark), even with the pipe scheduler fix described below._

### Call Latency

| Test | ActualLab.Rpc (p50 / p95 / p99) | gRPC (p50 / p95 / p99) | SignalR (p50 / p95 / p99) |
|------|----------------------------------|------------------------|---------------------------|
| Sum | 1.7ms / 2.7ms / 10.4ms | 25.0ms / 62.5ms / 63.6ms | 4.4ms / 22.1ms / 56.0ms |
| GetUser | 2.0ms / 2.6ms / 7.3ms | 24.2ms / 26.2ms / 59.0ms | 5.4ms / 6.6ms / 11.3ms |
| SayHello | 2.9ms / 3.4ms / 15.7ms | 25.4ms / 34.3ms / 60.9ms | 10.3ms / 14.2ms / 18.3ms |

Latency depends on the worker count the parameter search picks (it maximizes throughput), so it
isn't comparable between runs that ended up with different worker counts: gRPC ran with 23,200
workers here.

### Streams

Test names indicate item size: Stream1 = 1-byte items, Stream100 = 100-byte items, Stream10K = 10KB items.

| Test | ActualLab.Rpc | gRPC | SignalR |
|------|---------------|------|---------|
| Stream1 | 104.98M items/s | 39.51M items/s | 22.36M items/s |
| Stream100 | 45.89M items/s | 19.94M items/s | 14.78M items/s |
| Stream10K | 808.92K items/s | 648.00K items/s | 402.48K items/s |

### Throughput

Throughput is computed as `items/s × item_size`. Stream10K uses 10KB = 10,240 bytes.

| Test | ActualLab.Rpc | gRPC | SignalR |
|------|---------------|------|---------|
| Stream1 | 104.98 MB/s | 39.51 MB/s | 22.36 MB/s |
| Stream100 | 4.59 GB/s | 1.99 GB/s | 1.48 GB/s |
| Stream10K | 8.28 GB/s | 6.64 GB/s | 4.12 GB/s |

### .NET 11 Pipe Scheduler Fix

.NET 11 changed `PipeScheduler.ThreadPool` (System.IO.Pipelines) to queue work items with
`preferLocal: true` instead of `preferLocal: false`. Under load, pipe readers such as the SignalR
client's send loop now run almost immediately and flush tiny batches, so the number of socket sends
per call grows several times.

`RpcBenchmark` works around this on .NET 11+: `PipeSchedulerFix` (`src/ActualLab.Benchmarking`)
replaces the scheduler behind `PipeScheduler.ThreadPool` with one that uses the global thread pool
queue, as .NET 10 did. It is on by default; set the `ActualLab_PipeSchedulerFix` environment variable
to `0` or `false` to turn it off.

The table below is only an illustration that the fix works. It is a separate, shorter run
(`test -b calls -l <library> -f msgpack6c -n 3`, Sum test, .NET 11.0.0, loopback large MTU disabled),
so its numbers are not comparable with the tables above.

| Library | Fix on | Fix off | Change |
|---------|--------|---------|--------|
| SignalR | 4.86M calls/s | 0.81M calls/s | 6.0x |
| gRPC | 808K - 910K calls/s | 754K - 782K calls/s | +3% to +21% |
| MagicOnion | 833K calls/s | 716K calls/s | +16% |
| StreamJsonRpc | 477K calls/s | 449K calls/s | +6% |
| ActualLab.Rpc | 8.29M calls/s | 8.34M calls/s | none |

The fix changes SignalR numbers dramatically: without it, SignalR call throughput on .NET 11 is about
6x lower than on .NET 10. gRPC and MagicOnion seem to gain too, but gRPC results over Windows loopback
vary by 20-30% between runs, so two on/off pairs are not enough to state the size of that gain.
ActualLab.Rpc is not affected.

## Docker-Based RPC Benchmarks

These benchmarks run in Docker containers with CPU limits to measure **4-core server performance**.
The server container is limited to 4 CPUs while client containers have 24 CPUs available,
ensuring the server is the bottleneck. This setup matches [grpc_bench](https://github.com/LesnyRumcajs/grpc_bench), `SayHello` w/ `gRPC` is identical to what `grpc_bench` measures.
The Docker images run on the .NET 11 SDK base image (11.0.0 at the time of this run).

> **External cross-check.** grpc_bench's own
> [.NET gRPC result on an i9-13900KF @ 5.5 GHz](https://github.com/LesnyRumcajs/grpc_bench/discussions/441)
> (4-CPU server — a desktop CPU with per-core throughput close to the 9950X3D) is **~402K req/s**,
> close to our `SayHello` gRPC figure of **441.37K** below. Same library (grpc-dotnet),
> same 4-CPU setup — which confirms our gRPC numbers are measured fairly. gRPC-dotnet is the *fastest*
> framework in that grpc_bench run, and ActualLab.Rpc's `SayHello` (**2.46M**) is still **~6.1× faster**
> than it.

All figures are from a single run on .NET 11.0.0. HTTP was run with fixed parameters
(`-w 5400 -cc 150`), because its parameter search crashes the client. gRPC and SignalR stream rows
are from a second pass: in the first one their Stream10K test stalled (0 items/s on most tries).

### Docker Calls

| Framework | Sum | GetUser | SayHello |
|-----------|-----|---------|----------|
| ActualLab.Rpc | 5.25M calls/s | 4.13M calls/s | 2.46M calls/s |
| SignalR | 2.42M calls/s | 2.02M calls/s | 931.18K calls/s |
| gRPC | 483.52K calls/s | 467.10K calls/s | 441.37K calls/s |
| MagicOnion | 351.54K calls/s | 342.24K calls/s | 329.68K calls/s |
| StreamJsonRpc | 332.66K calls/s | 263.40K calls/s | 114.41K calls/s |
| HTTP | 93.08K calls/s | 93.87K calls/s | 92.56K calls/s |

### Docker Call Latency

| Framework | Sum (p50 / p95 / p99) | GetUser (p50 / p95 / p99) | SayHello (p50 / p95 / p99) |
|-----------|-----------------------|---------------------------|----------------------------|
| ActualLab.Rpc | 7.1ms / 12.2ms / 22.3ms | 8.9ms / 12.9ms / 17.5ms | 15.1ms / 18.9ms / 20.6ms |
| SignalR | 7.8ms / 11.6ms / 16.0ms | 9.2ms / 17.4ms / 28.8ms | 20.4ms / 25.9ms / 31.6ms |
| gRPC | 4.2ms / 5.6ms / 8.8ms | 4.4ms / 5.9ms / 25.0ms | 4.5ms / 7.9ms / 21.5ms |
| MagicOnion | 38.4ms / 47.5ms / 49.0ms | 37.1ms / 46.8ms / 48.4ms | 41.9ms / 54.9ms / 55.7ms |
| StreamJsonRpc | 53.6ms / 85.9ms / 87.6ms | 69.7ms / 98.9ms / 105.3ms | 165.1ms / 247.4ms / 250.7ms |
| HTTP | 40.2ms / 65.6ms / 67.6ms | 33.6ms / 43.4ms / 44.8ms | 37.5ms / 61.2ms / 62.9ms |

### Docker Streams

Test names indicate item size: Stream1 = 1-byte items, Stream100 = 100-byte items, Stream10K = 10KB items.

| Framework | Stream1 | Stream100 | Stream10K |
|-----------|---------|-----------|-----------|
| ActualLab.Rpc | 37.77M items/s | 13.33M items/s | 285.12K items/s |
| gRPC | 13.42M items/s | 7.27M items/s | 149.04K items/s |
| SignalR | 9.71M items/s | 5.50M items/s | 155.52K items/s |
| StreamJsonRpc | 127.44K items/s | 127.44K items/s | 63.72K items/s |

## Fusion Repository Benchmarks

These benchmarks come from the [ActualLab.Fusion](https://github.com/ActualLab/Fusion) repository and
measure Fusion's own overhead — compute-method caching, invalidation, and interception — rather than any
user logic. All numbers below are on .NET 11.0.0, AMD Ryzen 9 9950X3D (16 physical / 32 logical cores).

### Micro Benchmarks

These [BenchmarkDotNet](https://benchmarkdotnet.org) micro-benchmarks
(`tests/ActualLab.Fusion.Tests.BenchmarkRunner`) measure the **single-threaded, per-operation cost** of
Fusion's core compute-method primitives — cache hit, recompute + cache, and invalidation — with an
otherwise empty method body. The `Calls/s per core` column is `1 / Mean`, i.e. the single-thread operation
rate (not aggregate throughput).

The measured service exposes plain compute methods that just return a completed task:

```csharp
public interface IBenchmarkComputeService : IComputeService
{
    [ComputeMethod] Task<Unit> Get(long key, CancellationToken ct);
    [ComputeMethod] Task<Unit> Get(string key, CancellationToken ct);
    [ComputeMethod] Task<Unit> Get(Session session, string key, CancellationToken ct);
}
```

To run (from the ActualLab.Fusion repo):
```powershell
dotnet run -c Release --project tests/ActualLab.Fusion.Tests.BenchmarkRunner -- --filter '*CachedComputeMethodBenchmarks*' '*RecomputeComputeMethodBenchmarks*' '*RawInvalidationBenchmarks*'
```

Results below use an increased-precision job (10 warmup / 30 measured iterations, in-process toolchain)
on .NET 11.0.0.

| Operation | Calls/s per core | Mean | StdDev | Allocated |
|-----------|------------------|------|--------|-----------|
| Cached hit, `long` key<br/>`Service.Get(0L, default)` | 47.08M | 21.24 ns | 0.20 ns | 32 B |
| Cached hit, `string` key<br/>`Service.Get("key", default)` | 34.35M | 29.11 ns | 0.22 ns | 32 B |
| Cached hit, `Session` + `string` key<br/>`Service.Get(session, "key", default)` | 31.52M | 31.73 ns | 0.39 ns | 40 B |
| Invalidation (activate + 1 call)<br/>`using (Invalidation.Begin())`<br/>`    Service.Get(key, default)` | 19.74M | 50.65 ns | 5.73 ns | 112 B |
| Recompute + cache (fresh key each call)<br/>`Service.Get(i++, default)` | 2.02M | 495.3 ns | 28.51 ns | 1021 B |

A cache hit costs ~21-32 ns and one small allocation (the `ArgumentList` holding the call's arguments,
which is why the extra-argument `Session` + `string` variant allocates more); the `long` key is cheaper
than `string`/`Session`-keyed variants (no string hashing, smaller key). Invalidating a single
compute-method instance is ~51 ns / 112 B, and a full recompute + cache-fill is ~495 ns / ~1 KB (rows
ordered to mirror a value's real lifecycle: hit → invalidation → recompute).

### Multithreaded Test

A higher-level load test (`tests/ActualLab.Fusion.Tests.PerformanceTestRunner`, run via
`Run-PerformanceTest.cmd net11.0 npgsql|sqlite`) drives a real `UserService.Get(userId)` compute method.
Concurrent readers hit the method while a single mutator continuously invalidates and rewrites entries
(1000 users); reads are served from Fusion's cache, so the backing database barely matters. Best of
several runs:

| Scenario | PostgreSQL | SQLite |
|----------|---------------------|--------|
| Multithreaded — 640 readers + 1 mutator | 535.40M calls/s | 487.46M calls/s |
| Single-threaded — 1 reader, no mutators | 26.97M calls/s | 23.74M calls/s |

Because cached reads never touch the database, PostgreSQL and SQLite land within a few percent of each
other. The single-threaded figure (~24-27M calls/s per core) is a full `UserService.Get` cache hit;
multithreaded, Fusion serves ~half a billion cached compute calls/s across all cores. SQLite's
multithreaded number is noisier run-to-run — its single writer lock contends with the readers — so its
peak takes more runs to reach.

### Proxy and Interception Benchmarks

Fusion builds its compute methods, RPC clients, etc. on **method interception** via generated proxies.
This benchmark measures the per-call cost of ActualLab's proxies against
[Castle DynamicProxy](https://github.com/castleproject/Core) — the most common .NET interception library —
for three interceptor kinds:

- **Simple interceptor** — the interceptor produces the result itself (returns the method's default
  value) and never touches an underlying implementation, so it measures pure interception dispatch:
  ```csharp
  // ActualLab: CreateTypedHandler returns a handler that yields the result directly
  protected override Func<Invocation, object?>? CreateTypedHandler<T>(Invocation _, MethodDef methodDef)
      => _ => methodDef.DefaultResult;
  ```
- **Pass-through interceptor** — interception is turned off, so no handler runs and the call "falls
  through" to the wrapped target implementation; it measures dispatch **plus** delegation to the real
  method:
  ```csharp
  // ActualLab: with both flags off, the proxy forwards every call straight to the target
  MustInterceptSyncCalls = false;
  MustInterceptAsyncCalls = false;
  ```
- **No-handler interceptor** — interception stays **on** (a `MethodDef` is built and the interceptor is
  engaged for every call), but `CreateHandler` returns `null`; the binding then resolves the call to
  `NoHandler`, which forwards it to the target. So the call pays the full dispatch and *then* falls
  through — the cost of an "opted-in but unhandled" method:
  ```csharp
  // ActualLab: interception is on, but no handler is produced -> NoHandler -> target
  protected override Func<Invocation, object?>? CreateTypedHandler<T>(Invocation _, MethodDef methodDef)
      => null;
  ```

`No proxy` is a direct virtual call to the same target (its methods are marked `[MethodImpl(NoInlining)]`
so the baseline is a real, non-inlined call). Numbers are single-threaded (`Calls/s per core` = `1 / Mean`),
in-process, .NET 11.0.0.

Sync call — `int Int(int a)`:

| Proxy variant | Calls/s per core | Mean | StdDev | Allocated |
|---------------|------------------|------|--------|-----------|
| No proxy (direct virtual call) | 869.1M | 1.15 ns | 0.01 ns | – |
| **ActualLab, simple interceptor** | **404.0M** | **2.48 ns** | **0.03 ns** | **24 B** |
| ActualLab, pass-through | 265.3M | 3.77 ns | 0.04 ns | 24 B |
| ActualLab, no-handler (`CreateHandler` → null) | 263.8M | 3.79 ns | 0.05 ns | 24 B |
| **Castle DynamicProxy, simple** | **63.30M** | **15.80 ns** | **0.21 ns** | **128 B** |
| Castle DynamicProxy, pass-through | 48.95M | 20.43 ns | 0.27 ns | 160 B |

Async call — `Task<int> IntTask(int a, CancellationToken ct)`:

| Proxy variant | Calls/s per core | Mean | StdDev | Allocated |
|---------------|------------------|------|--------|-----------|
| No proxy (direct virtual call) | 871.5M | 1.15 ns | 0.05 ns | – |
| **ActualLab, simple interceptor** | **241.0M** | **4.15 ns** | **0.04 ns** | **32 B** |
| ActualLab, pass-through | 175.7M | 5.69 ns | 0.04 ns | 32 B |
| ActualLab, no-handler (`CreateHandler` → null) | 175.2M | 5.71 ns | 0.07 ns | 32 B |
| **Castle DynamicProxy, simple** | **51.92M** | **19.26 ns** | **0.17 ns** | **160 B** |
| Castle DynamicProxy, pass-through | 43.74M | 22.86 ns | 0.33 ns | 168 B |

The no-handler variant costs almost the same as the pass-through one (dispatch is the same; only the
handler differs), confirming the interception dispatch itself is the bulk of the cost. Overall,
ActualLab's interception is **~4-6x faster than Castle DynamicProxy** — and only **~2-4x slower than a
plain (non-intercepted) virtual call** — while allocating far less (0-32 B/call vs 96-168 B/call). That's
why Fusion can afford to wrap every compute method and RPC call in a proxy.
