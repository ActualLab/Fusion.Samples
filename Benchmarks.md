# Benchmark Results

**Updated:** 2026-01-17<br/>
**ActualLab.Fusion Version:** 11.4.7

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

## Run-Benchmark.cmd

```powershell
dotnet run -c Release --project src/Benchmark/Benchmark.csproj --no-launch-profile
```

### Local Services

| Test | Result | Speedup |
|------|--------|---------|
| Regular Service | 136.91K calls/s | |
| Fusion Service | 263.62M calls/s | **~1,926x** |

### Remote Services

| Test | Result | Speedup |
|------|--------|---------|
| HTTP Client → Regular Service | 99.66K calls/s | |
| HTTP Client → Fusion Service | 420.67K calls/s | **~4.2x** |
| ActualLab.Rpc Client → Fusion Service | 6.10M calls/s | **~61x** |
| Fusion Client → Fusion Service | 223.15M calls/s | **~2,239x** |
