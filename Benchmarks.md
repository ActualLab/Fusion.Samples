# Benchmark Results

**Updated:** 2026-04-25<br/>
**ActualLab.Fusion Version:** 12.3.76

This page summarizes benchmark results from ActualLab.Fusion.Samples repository.

## Test Environment

| Component | Specification |
|-----------|---------------|
| **CPU** | AMD Ryzen 9 9950X3D 16-Core Processor |
| **RAM** | 96 GB DDR5 |
| **OS** | Windows 11 |
| **.NET Version** | 10.0.7 |

Note that Ryzen 9 9950X3D has 32 logical cores due to SMT.

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
| Regular Service | 118.15K calls/s | |
| Fusion Service | 261.32M calls/s | **~2,212x** |

### Remote Services

| Test | Result | Speedup |
|------|--------|---------|
| HTTP Client → Regular Service | 80.43K calls/s | |
| HTTP Client → Fusion Service | 393.65K calls/s | **~4.9x** |
| ActualLab.Rpc Client → Fusion Service | 7.92M calls/s | **~98x** |
| Fusion Client → Fusion Service | 215.45M calls/s | **~2,679x** |

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
| Sum | 10.16M calls/s | 1.29M calls/s | 5.31M calls/s |
| GetUser | 9.03M calls/s | 1.26M calls/s | 4.43M calls/s |
| SayHello | 6.16M calls/s | 1.18M calls/s | 2.24M calls/s |

### Call Latency

| Test | ActualLab.Rpc (p50 / p95 / p99) | gRPC (p50 / p95 / p99) | SignalR (p50 / p95 / p99) |
|------|----------------------------------|------------------------|---------------------------|
| Sum | 2.0ms / 2.7ms / 6.4ms | 3.3ms / 5.1ms / 13.7ms | 3.9ms / 8.1ms / 11.1ms |
| GetUser | 2.2ms / 3.1ms / 8.7ms | 3.3ms / 5.9ms / 16.7ms | 4.6ms / 5.8ms / 11.1ms |
| SayHello | 3.2ms / 4.3ms / 10.4ms | 3.5ms / 4.7ms / 14.2ms | 9.0ms / 12.8ms / 15.0ms |

### Streams

Test names indicate item size: Stream1 = 1-byte items, Stream100 = 100-byte items, Stream10K = 10KB items.

| Test | ActualLab.Rpc | gRPC | SignalR |
|------|---------------|------|---------|
| Stream1 | 96.96M items/s | 43.78M items/s | 18.30M items/s |
| Stream100 | 43.01M items/s | 25.87M items/s | 14.25M items/s |
| Stream10K | 820.08K items/s | 572.76K items/s | 414.72K items/s |

### Throughput

Throughput is computed as `items/s × item_size`. Stream10K uses 10KB = 10,240 bytes.

| Test | ActualLab.Rpc | gRPC | SignalR |
|------|---------------|------|---------|
| Stream1 | 96.96 MB/s | 43.78 MB/s | 18.30 MB/s |
| Stream100 | 4.30 GB/s | 2.59 GB/s | 1.43 GB/s |
| Stream10K | 8.40 GB/s | 5.86 GB/s | 4.25 GB/s |

## Docker-Based RPC Benchmarks

These benchmarks run in Docker containers with CPU limits to measure **4-core server performance**.
The server container is limited to 4 CPUs while client containers have 24 CPUs available,
ensuring the server is the bottleneck. This setup matches [grpc_bench](https://github.com/LesnyRumcajs/grpc_bench), `SayHello` w/ `gRPC` is identical to what `grpc_bench` measures.

### Docker Calls

| Framework | Sum | GetUser | SayHello |
|-----------|-----|---------|----------|
| ActualLab.Rpc | 4.75M calls/s | 4.38M calls/s | 2.52M calls/s |
| SignalR | 2.23M calls/s | 1.82M calls/s | 842.45K calls/s |
| gRPC | 437.85K calls/s | 441.44K calls/s | 399.32K calls/s |
| MagicOnion | 392.59K calls/s | 402.85K calls/s | 362.84K calls/s |
| StreamJsonRpc | 265.72K calls/s | 226.77K calls/s | 99.09K calls/s |
| HTTP | 105.25K calls/s | 103.12K calls/s | 88.18K calls/s |

### Docker Call Latency

| Framework | Sum (p50 / p95 / p99) | GetUser (p50 / p95 / p99) | SayHello (p50 / p95 / p99) |
|-----------|-----------------------|---------------------------|----------------------------|
| ActualLab.Rpc | 3.8ms / 8.1ms / 23.7ms | 4.3ms / 7.6ms / 16.2ms | 6.5ms / 35.5ms / 39.9ms |
| SignalR | 5.2ms / 12.2ms / 50.7ms | 7.0ms / 12.0ms / 37.3ms | 19.9ms / 51.0ms / 58.1ms |
| gRPC | 3.3ms / 32.3ms / 45.4ms | 3.5ms / 6.8ms / 32.1ms | 4.2ms / 9.6ms / 36.2ms |
| MagicOnion | 4.5ms / 8.3ms / 24.1ms | 5.0ms / 10.8ms / 23.1ms | 5.5ms / 8.8ms / 12.3ms |
| StreamJsonRpc | 43.4ms / 56.4ms / 59.6ms | 58.9ms / 70.1ms / 72.5ms | 107.3ms / 212.3ms / 222.0ms |
| HTTP | 33.9ms / 51.4ms / 54.9ms | 34.2ms / 44.3ms / 45.9ms | 30.4ms / 44.0ms / 45.5ms |

### Docker Streams

Test names indicate item size: Stream1 = 1-byte items, Stream100 = 100-byte items, Stream10K = 10KB items.

| Framework | Stream1 | Stream100 | Stream10K |
|-----------|---------|-----------|-----------|
| ActualLab.Rpc | 31.80M items/s | 12.66M items/s | 279.72K items/s |
| gRPC | 11.27M items/s | 6.04M items/s | 125.64K items/s |
| SignalR | 5.42M items/s | 3.62M items/s | 106.20K items/s |
| StreamJsonRpc | 115.20K items/s | 115.20K items/s | 0 items/s |
