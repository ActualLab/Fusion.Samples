# Benchmark Results

**Updated:** 2026-01-29<br/>
**ActualLab.Fusion Version:** 12.0.45

This page summarizes benchmark results from ActualLab.Fusion.Samples repository.

We ran each benchmark 3 times and took the best result for each test.

## Test Environment

| Component | Specification |
|-----------|---------------|
| **CPU** | AMD Ryzen 9 9950X3D 16-Core Processor |
| **RAM** | 96 GB DDR5 |
| **OS** | Windows 11 |
| **.NET Version** | 10.0.1 |

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
| Regular Service | 135.44K calls/s | |
| Fusion Service | 266.58M calls/s | **~1,968x** |

### Remote Services

| Test | Result | Speedup |
|------|--------|---------|
| HTTP Client → Regular Service | 100.72K calls/s | |
| HTTP Client → Fusion Service | 431.35K calls/s | **~4.3x** |
| ActualLab.Rpc Client → Fusion Service | 6.92M calls/s | **~69x** |
| Fusion Client → Fusion Service | 226.73M calls/s | **~2,251x** |

## Run-RpcBenchmark.cmd

This benchmark compares **ActualLab.Rpc** with **gRPC**, **SignalR**, and other RPC frameworks.
The tables below include only **ActualLab.Rpc**, **gRPC**, and **SignalR**.
Other options, such as **StreamJsonRpc** and **RESTful API**, are way slower, so we omit them.

There are two benchmarks in `RpcBenchmark` project:

RPC calls:
```powershell
dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b calls -l rpc,grpc,signalr -f msgpack6c -n 4
```

RPC streaming:
```powershell
dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b streams -l rpc,grpc,signalr -f msgpack6c -n 4
```

### Calls

| Test | ActualLab.Rpc | gRPC | SignalR |
|------|---------------|------|---------|
| Sum | 9.33M calls/s | 1.11M calls/s | 5.30M calls/s |
| GetUser | 8.37M calls/s | 1.10M calls/s | 4.43M calls/s |
| SayHello | 5.99M calls/s | 1.04M calls/s | 2.25M calls/s |

### Streams

Test names indicate item size: Stream1 = 1-byte items, Stream100 = 100-byte items, Stream10K = 10KB items.

| Test | ActualLab.Rpc | gRPC | SignalR |
|------|---------------|------|---------|
| Stream1 | 101.17M items/s | 39.59M items/s | 17.17M items/s |
| Stream100 | 47.53M items/s | 21.19M items/s | 14.00M items/s |
| Stream10K | 955.44K items/s | 691.20K items/s | 460.80K items/s |

### Throughput

Throughput is computed as `items/s × item_size`. Stream10K uses 10KB = 10,240 bytes.

| Test | ActualLab.Rpc | gRPC | SignalR |
|------|---------------|------|---------|
| Stream1 | 101.17 MB/s | 39.59 MB/s | 17.17 MB/s |
| Stream100 | 4.75 GB/s | 2.12 GB/s | 1.40 GB/s |
| Stream10K | 9.78 GB/s | 7.08 GB/s | 4.72 GB/s |

## Docker-Based RPC Benchmarks

These benchmarks run in Docker containers with CPU limits to measure **4-core server performance**.
The server container is limited to 4 CPUs while client containers have 24 CPUs available,
ensuring the server is the bottleneck. This setup matches [grpc_bench](https://github.com/LesnyRumcajs/grpc_bench), `SayHello` w/ `gRPC` is identical to what `grpc_bench` measures.

### Docker Calls

| Framework | Sum | GetUser | SayHello |
|-----------|-----|---------|----------|
| ActualLab.Rpc | 1.49M calls/s | 1.40M calls/s | 1.13M calls/s |
| SignalR | 1.31M calls/s | 1.14M calls/s | 667.69K calls/s |
| gRPC | 480.48K calls/s | 476.97K calls/s | 447.06K calls/s |
| MagicOnion | 453.41K calls/s | 448.39K calls/s | 417.47K calls/s |
| StreamJsonRpc | 279.14K calls/s | 236.43K calls/s | 107.29K calls/s |
| HTTP | 164.10K calls/s | 156.26K calls/s | 129.30K calls/s |

### Docker Streams

Test names indicate item size: Stream1 = 1-byte items, Stream100 = 100-byte items, Stream10K = 10KB items.

| Framework | Stream1 | Stream100 | Stream10K |
|-----------|---------|-----------|-----------|
| ActualLab.Rpc | 34.24M items/s | 15.56M items/s | 432.72K items/s |
| gRPC | 12.60M items/s | 6.15M items/s | 259.20K items/s |
| SignalR | 5.28M items/s | 3.93M items/s | 202.32K items/s |
| StreamJsonRpc | 144.00K items/s | 144.00K items/s | 86.40K items/s |
