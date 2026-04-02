---
allowed-tools: Read, Edit, Bash(docker-compose:*), Bash(docker:*)
description: Run Docker-based RPC benchmarks and update Benchmarks.md
---

# Run Docker-Based RPC Benchmarks

Run RPC benchmarks in Docker containers and update Benchmarks.md with results.

## Prerequisites

**Environment Check:** This command can only run when Claude is NOT running in Docker.
Check the `AC_OS` environment variable - if it equals `Linux in Docker`, abort with an error message instructing the user to run this command from the host OS (use `c os` mode).

## Instructions

**Important:** Docker benchmarks are run only once (not 2 times like native benchmarks) since the Docker environment provides more consistent results.

**Critical:** Run all benchmarks sequentially, one framework at a time. NEVER run benchmarks in parallel — they share the same server and concurrent runs produce invalid results.

1. **Check environment:** Verify `AC_OS` is NOT `Linux in Docker`. If it is, stop and tell the user to run from host OS.

2. **Read @Benchmarks.md** to understand the current structure and where to add Docker benchmark tables.

3. **Check if the benchmark server is already running:**
   ```
   docker ps --filter "name=sample_rpc_benchmark_server" --format "{{.Names}} {{.Status}}"
   ```
   If not running, ask the user to start it in a separate terminal window:
   ```
   docker-compose run --build sample_rpc_benchmark_server
   ```
   The server must stay running throughout the benchmark process.

4. **Run call benchmarks — one framework at a time** using `sample_rpc_benchmark_calls` service with command override.
   Use `-s` for parameter search and `-f msgpack6c` for ActualLab.Rpc serialization format.
   Run each framework separately to avoid socket exhaustion during parameter search:
   ```
   docker-compose run --rm sample_rpc_benchmark_calls client -b calls -s -f msgpack6c -n 4 -l rpc https://sample_rpc_benchmark_server
   docker-compose run --rm sample_rpc_benchmark_calls client -b calls -s -n 6 -l signalr https://sample_rpc_benchmark_server
   docker-compose run --rm sample_rpc_benchmark_calls client -b calls -s -n 6 -l grpc https://sample_rpc_benchmark_server
   docker-compose run --rm sample_rpc_benchmark_calls client -b calls -s -n 6 -l magiconion https://sample_rpc_benchmark_server
   docker-compose run --rm sample_rpc_benchmark_calls client -b calls -s -n 6 -l jsonrpc https://sample_rpc_benchmark_server
   docker-compose run --rm sample_rpc_benchmark_calls client -b calls -s -n 6 -l http https://sample_rpc_benchmark_server
   ```
   **Note:** HTTP and SignalR may crash during parameter search due to socket exhaustion. If this happens, retry without `-s` using fixed parameters (e.g., `-w 5400 -cc 150`).

   Latency percentiles (p50/p95/p99) are reported for each call test.

5. **Run stream benchmarks — one framework at a time** using `sample_rpc_benchmark_streams` service:
   ```
   docker-compose run --rm sample_rpc_benchmark_streams client -b streams -s -f msgpack6c -n 4 -l rpc https://sample_rpc_benchmark_server
   docker-compose run --rm sample_rpc_benchmark_streams client -b streams -s -n 6 -l grpc https://sample_rpc_benchmark_server
   docker-compose run --rm sample_rpc_benchmark_streams client -b streams -s -n 6 -l signalr https://sample_rpc_benchmark_server
   docker-compose run --rm sample_rpc_benchmark_streams client -b streams -s -n 6 -l jsonrpc https://sample_rpc_benchmark_server
   ```

6. **Update @Benchmarks.md** with Docker benchmark results:
   - Add or update section `## Docker-Based RPC Benchmarks` after the native RPC benchmark section
   - Explain this measures **4-core server performance**: the server container is constrained to 4 CPUs while client containers have 24 CPUs available (to ensure the server is the bottleneck)
   - This matches the setup used in [grpc_bench](https://github.com/LesnyRumcajs/grpc_bench) for fair comparison
   - Add tables:
     - **Docker Calls** table: tests in columns (Sum, GetUser, SayHello), frameworks in rows
     - **Docker Call Latency** table: latency percentiles (p50/p95/p99) per framework per test
     - **Docker Streams** table: tests in columns (Stream1, Stream100, Stream10K), frameworks in rows

## Output Format

The benchmark outputs results in this format (take the final value after `->` arrow):
```
ActualLab.Rpc @ 1000 workers, 20 concurrency (50 clients):
  Sum      :   1.49M   1.49M   1.48M   1.49M ->   1.49M calls/s, p50=12μs, p95=45μs, p99=123μs
  GetUser  :   1.40M   1.39M   1.39M   1.40M ->   1.40M calls/s, p50=15μs, p95=55μs, p99=150μs
```
When `-s` is used, the output also includes a parameter search section before each framework showing the auto-tuned worker count and concurrency.

## Related

See also: `/docs-benchmark` - runs native (non-Docker) benchmarks on the host machine with 2 iterations taking best results.
