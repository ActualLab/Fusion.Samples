---
allowed-tools: Read, Edit, Bash(dotnet:*)
description: Run all benchmarks and update Benchmarks.md with results
---

# Run Benchmarks and Update Documentation

Run all benchmarks as specified in @Benchmarks.md and update the file with the latest results.

## Instructions

1. Read @Benchmarks.md to understand the benchmark commands and current structure

2. Run the **Run-Benchmark.cmd** benchmark (3 times, take best results):
   ```
   dotnet run -c Release --project src/Benchmark/Benchmark.csproj --no-launch-profile
   ```
   Collect results for:
   - Local Services: Regular Service, Fusion Service
   - Remote Services: HTTP Client → Regular Service, HTTP Client → Fusion Service, ActualLab.Rpc Client → Fusion Service, Fusion Client → Fusion Service

3. Run the **Run-RpcBenchmark.cmd** benchmarks (3 times each, take best results):

   Calls benchmark:
   ```
   dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b calls -l rpc,grpc,signalr -f msgpack5c -n 4
   ```

   Streams benchmark:
   ```
   dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b streams -l rpc,grpc,signalr -f msgpack5c -n 4
   ```

4. Update @Benchmarks.md with:
   - New "Updated" date (today's date)
   - Best results from all benchmark runs
   - Recalculate speedup values for Run-Benchmark.cmd results (relative to baseline services)
   - Keep the existing table structure and formatting
