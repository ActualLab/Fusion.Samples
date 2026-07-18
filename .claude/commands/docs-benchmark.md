---
allowed-tools: Read, Edit, Bash
description: Run any/all benchmarks in Benchmarks.md and update it with results
---

# Run Benchmarks and Update Documentation

Run the benchmarks documented in @Benchmarks.md and update that file with the latest results.
This command covers **every** benchmark group in Benchmarks.md — native, Docker, and the ones that
live in the sibling **ActualLab.Fusion** repo.

## STEP 0 — Confirm scope FIRST (required)

The full set is large and slow (tens of minutes end-to-end), and several groups have prerequisites
(Windows loopback setting, Docker, a running PostgreSQL). **Do not start running anything until you have
asked the user which groups to run.** List the groups below, ask them to pick, and only run the chosen
ones. If the user says "all", run them all in the order below.

| # | Group | Where | Rough time | Prereqs |
|---|-------|-------|-----------|---------|
| 1 | Run-Benchmark.cmd (local/remote service throughput) | Samples repo | ~2 min ×2 | loopback large-MTU **off** (Windows) |
| 2 | Native RpcBenchmark (calls, latency, streams) | Samples repo | ~10 min | loopback large-MTU **on** (Windows) |
| 3 | Docker RPC benchmarks | Samples repo, Docker | ~30 min | Docker; NOT running inside Docker |
| 4 | Fusion Micro Benchmarks (BDN) | ActualLab.Fusion repo | ~2 min | — |
| 5 | Fusion Multithreaded Test (npgsql + sqlite) | ActualLab.Fusion repo | ~5 min | PostgreSQL running |
| 6 | Fusion Proxy & Interception (BDN) | ActualLab.Fusion repo | ~8 min | — |
| 7 | Reference Redis benchmark | host `redis-benchmark` | ~1 min | Redis running; usually **skipped** (stable) |

The **ActualLab.Fusion** repo is a sibling of this one (e.g. `D:\Projects\ActualLab.Fusion` on Windows,
`/proj/ActualLab.Fusion` in Docker). Groups 4–6 are run from there, not from the Samples repo.

## Global rules

- **Never run benchmarks in parallel** — always sequential. Parallel runs cause file-lock issues and
  skew results via resource contention.
- Minimize background load (browsers, other app servers) for representative numbers.
- When updating @Benchmarks.md, take the **best** result per cell; if a fresh run is clearly worse than
  the documented one for some rows (environmental noise — common for gRPC/SignalR over loopback and for
  SQLite's multithreaded number), keep the higher prior figure and note it.
- After running the chosen groups, update @Benchmarks.md: new "Updated" date, best results, recalculated
  speedups for Run-Benchmark.cmd, latency tables (p50/p95/p99) from the best throughput run, keeping the
  existing structure/formatting.

## Windows loopback large MTU (groups 1–2, Windows only)

Native benchmarks run over localhost, so Windows **loopback large MTU** gates their throughput. Toggle it
elevated with the repo helper: `Set-LoopbackMode.ps1 enable|disable` (or
`netsh int ipv4/ipv6 set gl loopbacklargemtu=enable|disable`). The two native groups want **opposite**
settings:

- **Run-Benchmark (group 1)** → large MTU **DISABLED** — with it enabled, its many-short-connection
  HTTP/DB loopback path **hangs**.
- **RpcBenchmark (group 2)** → large MTU **ENABLED** — streams need it (~2× throughput); gRPC/SignalR are
  fine either way.

Keep `loopbackexecutionmode` at its default (`inline`) — worker mode is not helpful.

---

## Group 1 — Run-Benchmark.cmd (Benchmark.csproj)

Measures a repository-style user lookup service (local + remote, Regular vs Fusion). **Disable** loopback
large MTU first, then run **2×** (take best):

```
dotnet run -c Release --project src/Benchmark/Benchmark.csproj --no-launch-profile
```

Collect: Local Services (Regular Service, Fusion Service); Remote Services (HTTP → Regular, HTTP → Fusion,
ActualLab.Rpc → Fusion, Fusion Client → Fusion). Recalculate the speedup columns relative to the baseline
services.

## Group 2 — Native RpcBenchmark (Run-RpcBenchmark.cmd)

Compares ActualLab.Rpc vs gRPC vs SignalR (other libraries are far slower and omitted). **Enable** loopback
large MTU first. Options: `-b calls|streams`, `-l <libs>`, `-f msgpack6c` (**always**, for ActualLab.Rpc),
`-s` (**always** — per-framework parameter search), `-n 6` (tries per test, best selected).

**Run each framework separately** (avoids interference / socket exhaustion). Order: calls for rpc, grpc,
signalr; then streams for rpc, grpc, signalr.

```
dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b calls   -l rpc     -f msgpack6c -s -n 6
dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b calls   -l grpc    -f msgpack6c -s -n 6
dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b calls   -l signalr -f msgpack6c -s -n 6
dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b streams -l rpc     -f msgpack6c -s -n 6
dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b streams -l grpc    -f msgpack6c -s -n 6
dotnet run -c Release --project src/RpcBenchmark/RpcBenchmark.csproj --no-launch-profile -- test -b streams -l signalr -f msgpack6c -s -n 6
```

Update the Calls, Call Latency, Streams, and Throughput tables (throughput = items/s × item size).

## Group 3 — Docker RPC benchmarks

Measures 4-core server performance (server container capped at 4 CPUs, clients at 24) to match
[grpc_bench](https://github.com/LesnyRumcajs/grpc_bench). Runs container-to-container, so the Windows
loopback setting is **irrelevant** here.

**Preconditions & notes:**
- This group can only run when **not** inside Docker. If `AC_OS` == `Linux in Docker`, stop and tell the
  user to run it from the host OS.
- The `sample_rpc_benchmark` image has its own lean build stage (`rpc_benchmark_build`) that compiles only
  `RpcBenchmark.csproj` — the shared `build` stage compiles the whole solution and pulls in TodoApp's
  npm/TypeScript build. Just build the three benchmark services.
- **Tested and NOT helpful (do not add):** cpuset pinning of the 4-CPU server *hurt* results; a jumbo
  Docker MTU had no effect (the server is CPU-bound). Keep grpc_bench-standard `cpus:'4'`, default MTU.
- Docker benchmarks are run **once** (not 2×) — the environment is more consistent.

Build, then start the server (keep it running in its own terminal), then run clients one framework at a
time:

```
docker-compose build sample_rpc_benchmark_server sample_rpc_benchmark_calls sample_rpc_benchmark_streams
docker-compose run --rm --remove-orphans --name rpc_bench_server sample_rpc_benchmark_server
# calls (one framework at a time):
docker-compose run --rm sample_rpc_benchmark_calls   client -b calls   -s -f msgpack6c -n 4 -l rpc        https://sample_rpc_benchmark_server
docker-compose run --rm sample_rpc_benchmark_calls   client -b calls   -s            -n 6 -l signalr    https://sample_rpc_benchmark_server
docker-compose run --rm sample_rpc_benchmark_calls   client -b calls   -s            -n 6 -l grpc       https://sample_rpc_benchmark_server
docker-compose run --rm sample_rpc_benchmark_calls   client -b calls   -s            -n 6 -l magiconion https://sample_rpc_benchmark_server
docker-compose run --rm sample_rpc_benchmark_calls   client -b calls   -s            -n 6 -l jsonrpc    https://sample_rpc_benchmark_server
docker-compose run --rm sample_rpc_benchmark_calls   client -b calls   -s            -n 6 -l http       https://sample_rpc_benchmark_server
# streams (one framework at a time):
docker-compose run --rm sample_rpc_benchmark_streams client -b streams -s -f msgpack6c -n 4 -l rpc        https://sample_rpc_benchmark_server
docker-compose run --rm sample_rpc_benchmark_streams client -b streams -s            -n 6 -l grpc       https://sample_rpc_benchmark_server
docker-compose run --rm sample_rpc_benchmark_streams client -b streams -s            -n 6 -l signalr    https://sample_rpc_benchmark_server
docker-compose run --rm sample_rpc_benchmark_streams client -b streams -s            -n 6 -l jsonrpc    https://sample_rpc_benchmark_server
```

**HTTP/SignalR may crash during `-s` parameter search (socket exhaustion).** If so, retry without `-s`
using fixed params, e.g. `-n 6 -w 5400 -cc 150`. Update the Docker Calls, Docker Call Latency, and Docker
Streams tables.

---

## Groups 4–6 — Fusion Repository Benchmarks (sibling ActualLab.Fusion repo)

Run these from the **ActualLab.Fusion** repo. They feed the "Fusion Repository Benchmarks" section of
@Benchmarks.md (Micro Benchmarks, Multithreaded Test, Proxy and Interception Benchmarks).

### BenchmarkDotNet gotcha (groups 4 & 6)

BDN builds a dedicated runner per benchmark by searching the filesystem for the project by name. If the
repo's `tmp/worktrees/` contains checkouts of the repo, BDN finds **duplicate** `*.csproj` files and fails
with *"Found more than one matching project file"*. Two fixes:

- **Preferred:** remove the leftover worktree dirs (`wsl rm -rf tmp/worktrees` once no process holds them),
  then BDN runs out-of-process normally.
- **Workaround (no cleanup):** temporarily switch the job to the in-process toolchain and revert after. In
  `tests/ActualLab.Fusion.Tests.BenchmarkRunner/Program.cs`, add
  `using BenchmarkDotNet.Toolchains.InProcess.Emit;` and replace the `Job.ShortRun` job with an
  increased-precision in-process job (also gives tighter CIs):
  ```csharp
  var job = Job.Default
      .WithId("Precise-InProcess")
      .WithToolchain(InProcessEmitToolchain.Instance)
      .WithLaunchCount(1)
      .WithWarmupCount(10)
      .WithIterationCount(30);
  ```
  Revert `Program.cs` when done (don't commit the in-process hack).

`Calls/s per core` in the tables = `1 / Mean`.

### Group 4 — Micro Benchmarks

Compute-method primitives: cache hit, recompute + cache, invalidation.

```
dotnet run -c Release --project tests/ActualLab.Fusion.Tests.BenchmarkRunner -- --filter '*CachedComputeMethodBenchmarks*' '*RecomputeComputeMethodBenchmarks*' '*RawInvalidationBenchmarks*'
```

Update the Micro Benchmarks table (Operation | Calls/s per core | Mean | StdDev | Allocated).

### Group 5 — Multithreaded Test (PerformanceTestRunner)

Drives a real `UserService.Get(userId)` compute method under many concurrent readers + 1 mutator, and also
single-threaded. Reports "Multiple readers, 1 mutator" (multithreaded peak) and "Single reader, no
mutators" (single-threaded peak), each internally best-of-3.

- **PostgreSQL must be running** for `npgsql` (`docker compose up -d postgres`, or `Docker-Start-DBs.cmd`).
  `sqlite` needs no server.
- The app ends with `Console.ReadKey()` ("Press any key to exit…"), which **throws under redirected
  stdin** — but only *after* printing results, so capture still works. Run the exe with stdin from
  `/dev/null` (or accept the trailing exception).
- Run **3× per DB** and take the best. **SQLite's multithreaded number is noisy** (single writer lock
  contends with readers) — run a few extra times to reach its peak; at peak it lands within a few percent
  of PostgreSQL because cached reads never touch the DB.

```
# Build once, then call the exe directly 3× per DB:
dotnet build -p:UseMultitargeting=true -c:Release -f:net10.0 tests/ActualLab.Fusion.Tests.PerformanceTestRunner/ActualLab.Fusion.Tests.PerformanceTestRunner.csproj
exe=./artifacts/tests/bin/ActualLab.Fusion.Tests.PerformanceTestRunner/release_net10.0/ActualLab.Fusion.Tests.PerformanceTestRunner.exe
for i in 1 2 3; do "$exe" npgsql < /dev/null; done
for i in 1 2 3; do "$exe" sqlite < /dev/null; done
```

(Equivalently `Run-PerformanceTest.cmd net10.0 npgsql` / `sqlite`, which builds then runs once.) Update the
Multithreaded Test table (PostgreSQL + SQLite, multithreaded + single-threaded, best of runs).

### Group 6 — Proxy & Interception Benchmarks

ActualLab proxies vs Castle DynamicProxy for simple / pass-through / no-handler interceptors, across a sync
(`Int`) and async (`IntTask`) call.

```
dotnet run -c Release --project tests/ActualLab.Fusion.Tests.BenchmarkRunner -- --filter '*ProxyInterceptionBenchmarks*'
```

(Uses the same BDN gotcha/in-process workaround as group 4.) Update the two Proxy and Interception tables;
keep the "simple" interceptor rows bolded (the primary comparison).

---

## Group 7 — Reference Redis benchmark (usually skipped)

Baseline using the `redis-benchmark` tool on the same machine (500K requests, best of 5). Results are
stable, so **skip unless explicitly asked**. Optimal client count (~12) was found via binary search over
1–1000. Run PING_INLINE, GET, SET and update the Reference Redis table.

```
redis-benchmark -n 500000 -c 12 -t PING_INLINE,GET,SET
```

## Output format (RPC benchmarks)

Take the final value after the `->` arrow, e.g.:
```
Sum      :   1.49M   1.49M   1.48M   1.49M ->   1.49M calls/s, p50=12μs, p95=45μs, p99=123μs
```
With `-s`, the output also includes the auto-tuned worker count / concurrency before each framework.
