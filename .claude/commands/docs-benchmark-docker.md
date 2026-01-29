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

**Important:** Docker benchmarks are run only once (not 3 times like native benchmarks) since the Docker environment provides more consistent results.

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

4. **Run the calls benchmark:**
   ```
   docker-compose run --build sample_rpc_benchmark_calls
   ```
   Wait for completion and collect results. Tests 6 frameworks: ActualLab.Rpc, SignalR, gRPC, MagicOnion, StreamJsonRpc, HTTP.

5. **Run the streams benchmark:**
   ```
   docker-compose run --build sample_rpc_benchmark_streams
   ```
   Wait for completion and collect results. Tests 4 frameworks: ActualLab.Rpc, gRPC, SignalR, StreamJsonRpc.

6. **Update @Benchmarks.md** with Docker benchmark results:
   - Add or update section `## Docker-Based RPC Benchmarks` after the native RPC benchmark section
   - Explain this measures **4-core server performance**: the server container is constrained to 4 CPUs while client containers have 24 CPUs available (to ensure the server is the bottleneck)
   - This matches the setup used in [grpc_bench](https://github.com/LesnyRumcajs/grpc_bench) for fair comparison
   - Add two tables:
     - **Docker Calls** table: tests in columns (Sum, GetUser, SayHello), frameworks in rows
     - **Docker Streams** table: tests in columns (Stream1, Stream100, Stream10K), frameworks in rows

## Output Format

The benchmark outputs results in this format (take the final value after `->` arrow):
```
ActualLab.Rpc:
  Sum      :   1.49M   1.49M   1.48M   1.49M ->   1.49M calls/s
  GetUser  :   1.40M   1.39M   1.39M   1.40M ->   1.40M calls/s
```

## Related

See also: `/docs-benchmark` - runs native (non-Docker) benchmarks on the host machine with 3 iterations taking best results.
