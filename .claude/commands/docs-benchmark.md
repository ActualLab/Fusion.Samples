---
allowed-tools: Read, Edit, Bash(dotnet:*)
description: Run all benchmarks and update Benchmarks.md with results
---

# Run Benchmarks and Update Documentation

Run all benchmarks as specified in @Benchmarks.md and update the file with the latest results.

## Windows loopback large MTU (REQUIRED on Windows)

These benchmarks run over localhost, so Windows **loopback large MTU** gates their throughput.
Enabling it roughly **doubles** localhost stream throughput and is required to reproduce the
documented RpcBenchmark stream numbers; on recent Windows 11 builds the effective default throttles
loopback ~2x. Toggle it with the repo's helper (elevated): `Set-LoopbackMode.ps1 enable|disable`
(or `netsh int ipv4/ipv6 set gl loopbacklargemtu=enable|disable`).

**Trade-off — the two benchmarks want opposite settings:**
- **RpcBenchmark** → large MTU **ENABLED** (streams need it; gRPC/SignalR are fine).
- **Run-Benchmark (Benchmark.csproj)** → large MTU **DISABLED** — with it enabled, its
  many-short-connection HTTP/DB loopback path **hangs**.

So: disable large MTU before Run-Benchmark, enable it before RpcBenchmark. Keep `loopbackexecutionmode`
at its default (`inline`) — worker mode is not helpful. Also minimize background load (browsers, other
app servers) for representative numbers.

## Instructions

**Important:** Run all benchmarks sequentially, not in parallel. Running benchmarks in parallel causes file locking issues and skews results due to resource contention.

1. Read @Benchmarks.md to understand the benchmark commands and current structure

2. **Disable loopback large MTU** (`Set-LoopbackMode.ps1 disable`), then run the **Run-Benchmark.cmd**
   benchmark (**2 times** sequentially, take best results) using a command from the corresponding section of @Benchmarks.md, which is similar to:
   ```
   dotnet run -c Release --project src/Benchmark/Benchmark.csproj --no-launch-profile
   ```
   Collect results for:
   - Local Services: Regular Service, Fusion Service
   - Remote Services: HTTP Client → Regular Service, HTTP Client → Fusion Service, ActualLab.Rpc Client → Fusion Service, Fusion Client → Fusion Service

3. **Enable loopback large MTU** (`Set-LoopbackMode.ps1 enable`), then run the **Run-RpcBenchmark.cmd** benchmarks (**once** each, using `-n 6` for 6 attempts per test):

   Key options:
   - `-b calls|streams` - benchmark kind
   - `-l <libs>` - comma-separated list of libraries to test
   - `-f <format>` - serialization format. **Always use `msgpack6c`** for ActualLab.Rpc tests.
   - `-s` - **always use this flag** to enable per-framework parameter search (auto-tunes worker count and concurrency per framework)
   - `-n <count>` - number of tries per test (best result selected)

   **Run each framework separately** to avoid interference and socket exhaustion issues.
   The order is: rpc, grpc, signalr. Run calls first for all frameworks, then streams.

   Calls benchmark (run for each framework separately):
   ```
   dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b calls -l rpc -f msgpack6c -s -n 6
   dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b calls -l grpc -f msgpack6c -s -n 6
   dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b calls -l signalr -f msgpack6c -s -n 6
   ```

   Streams benchmark (run for each framework separately):
   ```
   dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b streams -l rpc -f msgpack6c -s -n 6
   dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b streams -l grpc -f msgpack6c -s -n 6
   dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b streams -l signalr -f msgpack6c -s -n 6
   ```

4. Don't benchmark Redis (unless explicitly asked), its results are quite stable.

5. Update @Benchmarks.md with:
   - New "Updated" date (today's date)
   - Best results from all benchmark runs
   - Recalculate speedup values for Run-Benchmark.cmd results (relative to baseline services)
   - Keep the existing table structure and formatting
   - Update latency tables (p50/p95/p99) with results from the best throughput run
