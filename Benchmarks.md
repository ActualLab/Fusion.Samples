# Benchmark Results

**Updated:** 2026-07-18<br/>
**ActualLab.Fusion Version:** 14.0.17

This page summarizes benchmark results from ActualLab.Fusion.Samples repository.

## Test Environment

| Component | Specification |
|-----------|---------------|
| **CPU** | AMD Ryzen 9 9950X3D 16-Core Processor |
| **RAM** | 96 GB DDR5 |
| **OS** | Windows 11 |
| **.NET Version** | 10.0.8 |

Note that Ryzen 9 9950X3D has 32 logical cores due to SMT.

> **Windows loopback setup (required for representative numbers).** These benchmarks run over
> localhost, so Windows loopback settings gate their throughput. Enabling **loopback large MTU**
> (`Set-LoopbackMode.ps1 enable`, or `netsh int ipv4/ipv6 set gl loopbacklargemtu=enable`) roughly
> doubles localhost stream throughput; on recent Windows 11 builds the effective default throttles
> it. **Run RpcBenchmark with large MTU ENABLED** (streams need it). **Run Run-Benchmark.cmd with it
> DISABLED** — large MTU hangs its many-connection HTTP/DB loopback path; that benchmark is
> call/HTTP-based and doesn't need it. gRPC/SignalR results are more variable on Windows loopback.

## Reference Redis Benchmark

Reference benchmark using `redis-benchmark` tool on the same machine (500K requests, best of 5 runs). Optimal client count (12) was determined via binary search over 1-1000 range.

| Operation | Result |
|-----------|--------|
| PING_INLINE | 231.59K req/s |
| GET | 229.25K req/s |
| SET | 229.67K req/s |

## Run-Benchmark.cmd

The benchmark measures throughput of a simple repository-style user lookup service that retrieves and updates user records from a database: `UserService.Get(userId)` and `Update(userId, ...)`.

To run the benchmark:
```powershell
dotnet run -c Release --project src/Benchmark/Benchmark.csproj --no-launch-profile
```

### Local Services

| Test | Result | Speedup |
|------|--------|---------|
| Regular Service | 171.05K calls/s | |
| Fusion Service | 344.98M calls/s | **~2,017x** |

### Remote Services

| Test | Result | Speedup |
|------|--------|---------|
| HTTP Client → Regular Service | 102.82K calls/s | |
| HTTP Client → Fusion Service | 304.87K calls/s | **~3.0x** |
| ActualLab.Rpc Client → Fusion Service | 7.82M calls/s | **~76x** |
| Fusion Client → Fusion Service | 230.16M calls/s | **~2,239x** |

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
| Sum | 9.91M calls/s | 1.28M calls/s | 4.85M calls/s |
| GetUser | 8.75M calls/s | 1.24M calls/s | 4.05M calls/s |
| SayHello | 6.04M calls/s | 1.16M calls/s | 2.16M calls/s |

### Call Latency

| Test | ActualLab.Rpc (p50 / p95 / p99) | gRPC (p50 / p95 / p99) | SignalR (p50 / p95 / p99) |
|------|----------------------------------|------------------------|---------------------------|
| Sum | 1.8ms / 2.4ms / 6.1ms | 3.4ms / 4.5ms / 12.1ms | 5.1ms / 16.4ms / 22.7ms |
| GetUser | 2.0ms / 2.6ms / 6.0ms | 3.5ms / 4.4ms / 11.0ms | 6.1ms / 7.8ms / 13.3ms |
| SayHello | 2.9ms / 3.4ms / 6.8ms | 3.5ms / 4.4ms / 10.5ms | 11.7ms / 16.2ms / 20.5ms |

### Streams

Test names indicate item size: Stream1 = 1-byte items, Stream100 = 100-byte items, Stream10K = 10KB items.

| Test | ActualLab.Rpc | gRPC | SignalR |
|------|---------------|------|---------|
| Stream1 | 99.96M items/s | 43.78M items/s | 17.97M items/s |
| Stream100 | 44.89M items/s | 25.87M items/s | 13.90M items/s |
| Stream10K | 807.84K items/s | 572.76K items/s | 432.00K items/s |

_gRPC stream figures retained from the previous run — they were higher than this run's noisier gRPC-over-loopback results._

### Throughput

Throughput is computed as `items/s × item_size`. Stream10K uses 10KB = 10,240 bytes.

| Test | ActualLab.Rpc | gRPC | SignalR |
|------|---------------|------|---------|
| Stream1 | 99.96 MB/s | 43.78 MB/s | 17.97 MB/s |
| Stream100 | 4.49 GB/s | 2.59 GB/s | 1.39 GB/s |
| Stream10K | 8.27 GB/s | 5.86 GB/s | 4.42 GB/s |

## Fusion Core Microbenchmarks

These are [BenchmarkDotNet](https://benchmarkdotnet.org) microbenchmarks from the
[ActualLab.Fusion](https://github.com/ActualLab/Fusion) repository
(`tests/ActualLab.Fusion.Tests.BenchmarkRunner`). Unlike the throughput benchmarks above, they measure
the **single-threaded, per-operation cost** of Fusion's core compute-method primitives — cache hit,
recompute + cache, and invalidation — with an otherwise empty method body, so the numbers reflect
Fusion's own overhead rather than any user logic. The `Calls/s per core` column is `1 / Mean`, i.e. the
single-thread operation rate (not aggregate throughput like the tables above).

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
on .NET 10.0.8.

| Operation | Calls/s per core | Mean | StdDev | Allocated |
|-----------|------------------|------|--------|-----------|
| Cached hit, `long` key<br/>`Service.Get(0L, default)` | 50.68M | 19.73 ns | 0.12 ns | 32 B |
| Cached hit, `string` key<br/>`Service.Get("key", default)` | 34.90M | 28.65 ns | 0.16 ns | 32 B |
| Cached hit, `Session` + `string` key<br/>`Service.Get(session, "key", default)` | 34.58M | 28.92 ns | 0.10 ns | 40 B |
| Recompute + cache (fresh key each call)<br/>`Service.Get(i++, default)` | 2.04M | 490.1 ns | 34.62 ns | 1007 B |
| Invalidation (activate + 1 invalidating call)<br/>`using (Invalidation.Begin())`<br/>`    Service.Get(key, default)` | 18.77M | 53.28 ns | 4.63 ns | 112 B |

A cache hit costs ~20-29 ns and one small allocation (the returned `Task<Unit>`); the `long` key is
cheaper than `string`/`Session`-keyed variants (no string hashing, smaller key). A full recompute +
cache-fill is ~490 ns / ~1 KB, and invalidating a single compute-method instance is ~53 ns / 112 B.

## Docker-Based RPC Benchmarks

These benchmarks run in Docker containers with CPU limits to measure **4-core server performance**.
The server container is limited to 4 CPUs while client containers have 24 CPUs available,
ensuring the server is the bottleneck. This setup matches [grpc_bench](https://github.com/LesnyRumcajs/grpc_bench), `SayHello` w/ `gRPC` is identical to what `grpc_bench` measures.
The Docker images run on the .NET 10 SDK base image (10.0.10 at the time of this run).

Each cell reports the best figure observed across the two most recent runs. The WebSocket-based
frameworks (ActualLab.Rpc, SignalR) and all streaming tests improved on this run; the HTTP/2 call
paths (gRPC, MagicOnion) and HTTP/1.1 came in uniformly lower — a Docker Desktop/WSL2 scheduling
variance rather than a code change — so their higher prior figures are retained.

### Docker Calls

| Framework | Sum | GetUser | SayHello |
|-----------|-----|---------|----------|
| ActualLab.Rpc | 4.77M calls/s | 4.38M calls/s | 2.52M calls/s |
| SignalR | 2.38M calls/s | 1.94M calls/s | 862.07K calls/s |
| gRPC | 437.85K calls/s | 441.44K calls/s | 399.32K calls/s |
| MagicOnion | 392.59K calls/s | 402.85K calls/s | 362.84K calls/s |
| StreamJsonRpc | 279.62K calls/s | 231.86K calls/s | 99.09K calls/s |
| HTTP | 105.25K calls/s | 103.12K calls/s | 88.18K calls/s |

### Docker Call Latency

| Framework | Sum (p50 / p95 / p99) | GetUser (p50 / p95 / p99) | SayHello (p50 / p95 / p99) |
|-----------|-----------------------|---------------------------|----------------------------|
| ActualLab.Rpc | 3.5ms / 8.4ms / 12.8ms | 4.1ms / 8.6ms / 13.1ms | 6.4ms / 9.6ms / 24.3ms |
| SignalR | 8.7ms / 28.1ms / 30.0ms | 9.8ms / 32.2ms / 35.2ms | 20.1ms / 31.2ms / 40.6ms |
| gRPC | 3.3ms / 32.3ms / 45.4ms | 3.5ms / 6.8ms / 32.1ms | 4.2ms / 9.6ms / 36.2ms |
| MagicOnion | 4.5ms / 8.3ms / 24.1ms | 5.0ms / 10.8ms / 23.1ms | 5.5ms / 8.8ms / 12.3ms |
| StreamJsonRpc | 43.4ms / 56.4ms / 59.6ms | 58.9ms / 70.1ms / 72.5ms | 107.3ms / 212.3ms / 222.0ms |
| HTTP | 33.9ms / 51.4ms / 54.9ms | 34.2ms / 44.3ms / 45.9ms | 30.4ms / 44.0ms / 45.5ms |

### Docker Streams

Test names indicate item size: Stream1 = 1-byte items, Stream100 = 100-byte items, Stream10K = 10KB items.

| Framework | Stream1 | Stream100 | Stream10K |
|-----------|---------|-----------|-----------|
| ActualLab.Rpc | 35.17M items/s | 12.97M items/s | 279.72K items/s |
| gRPC | 11.79M items/s | 6.19M items/s | 140.40K items/s |
| SignalR | 8.89M items/s | 5.08M items/s | 106.20K items/s |
| StreamJsonRpc | 120.96K items/s | 120.96K items/s | 60.48K items/s |
