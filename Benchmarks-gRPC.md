# gRPC Server Benchmarks (grpc_bench)

**Measured:** 2026-11-15 to 2026-11-17<br/>
**grpc_bench:** the `dev` branch of [ActualLab/grpc_bench](https://github.com/ActualLab/grpc_bench/tree/dev),
which is upstream commit `ee8f9a7` (2026-11-06) with every tested server moved to current versions

This page has the results of running gRPC servers from
[grpc_bench](https://github.com/LesnyRumcajs/grpc_bench) on the machine and in the setup used for the
Docker-based RPC benchmarks in [Benchmarks.md](Benchmarks.md), so that they can be compared with
ActualLab.Rpc and with the other .NET RPC libraries measured there.

The short version:

- With grpc_bench's own client shape (one `ghz` process, 50 connections, 1,000 concurrent calls) the
  fastest server reaches 384K calls/s on 4 CPUs. With a load generator that saturates the server and
  with tuned connection and concurrency counts the same server reaches 1.06M calls/s.
- Tuning the load shape roughly doubles the result of almost every server (1.5x to 2.8x).
- `ghz` can't saturate the fastest servers on this machine: it needs about 35 us of CPU per call,
  several times more than these servers do. The tuned results come from `tonic_client`, a client
  written for this test and added to the grpc_bench branch.
- Many grpc_bench servers were pinned to libraries 2 to 4 years old. The branch moves all of them to
  current versions; that changed some results by 20-25% in either direction.
- grpc-dotnet reaches 670K calls/s on .NET 11 with DATAS turned off. The gRPC test of `RpcBenchmark`,
  which uses the same library and its own .NET client, measures 567K.
- ActualLab.Rpc's `SayHello` (2.54M calls/s on the same 4 CPUs) is 2.4x the fastest gRPC server here.

## Test Environment

| Component | Specification |
|-----------|---------------|
| **CPU** | AMD Ryzen 9 9950X3D, 16 cores / 32 logical CPUs |
| **RAM** | 96 GB DDR5 |
| **OS** | Windows 11, Docker Desktop (WSL2 backend, 32 CPUs, 25 GB) |

## Setup

- **Scenario:** grpc_bench's `complex_proto`: a unary `SayHello` call whose request and reply carry
  the same message (a string, a double, a float, a bool, two integers, a `oneof` and two nested
  items). It is the message `SayHello` of `RpcBenchmark` uses.
- **Servers:** 26 servers built from the Dockerfiles of the branch above: 22 existing grpc_bench
  servers upgraded to current runtimes and libraries, and 4 new ones (`dotnet_grpc_aot`,
  `rust_volo_grpc`, `go_kitex_grpc`, `swift_grpc2`). [Versions](#versions) lists what each runs on.
- **Server container:** `--cpus 4 --memory 8g`, `GRPC_SERVER_CPUS=4`, plain HTTP/2 (no TLS, as in
  grpc_bench). grpc_bench's default memory limit (512 MB) starves the JVM servers under this load,
  and 8 GB removes memory as a factor.
- **Client containers:** on the same user-defined Docker network as the server (no published ports),
  using the 28 logical CPUs the server doesn't use.
- **Run:** a warmup, then a 15 s measured run. Every reply is decoded.
- **Server CPU:** read from the server container's cgroup (`cpu.stat`, `usage_usec`) over a window
  strictly inside the measured run. 400% means all 4 CPUs are busy.
- **One run at a time**, nothing else under load on the machine.

All 32 logical CPUs are busy in the tuned runs (4 for the server, 28 for the clients), and they are
16 physical cores with SMT. So the server's 4 CPUs are weaker here than 4 dedicated cores would be:
a server gains about 1.5x, not 2x, when its limit goes from 2 to 4 CPUs.

### Load Generators

| Client | What it is | CPU per call |
|--------|------------|--------------|
| `ghz` 0.114.0 | The client grpc_bench uses: Go, grpc-go, dynamic (reflection-based) messages, per-call statistics. One process doesn't scale with CPUs, so the tuned runs start 28 single-CPU processes. | about 35 us |
| `tonic_client` | A client written for this test on `tonic` 0.14 and added to the grpc_bench branch (`GRPC_CLIENT=tonic`). Closed-loop workers: a worker sends a call, awaits and decodes the reply, sends the next one. One single-threaded runtime per client CPU, each with its own connections. It reads the same payload file as `ghz` and prints a report in the same format. | about 10 us |

Both clients are measured by successful calls only. `ghz` includes failed calls in the
"Requests/sec" it prints, so the `ghz` figures here are its count of `OK` responses divided by the
run time. That matters for the servers that fail under load (see
[Servers with Limits](#servers-with-limits)).

## Results: Default Shape vs Tuned

`SayHello`, 4-CPU server, no TLS, calls/s.

- **Default** is the shape grpc_bench uses: one `ghz` process, 50 connections, 1,000 concurrent calls
  (20 per connection). `ghz` had 28 CPUs here instead of grpc_bench's default of 1, with which it
  would measure only itself.
- **Tuned** is the best error-free result over 28 single-CPU `ghz` processes (one connection and 150
  calls each) and `tonic_client` with the best shape found for that server.

| Server | Default | Server CPU | Tuned | Client and shape (connections x calls per connection) | Server CPU | Tuned / default | Slower than the fastest |
|--------|---------|------------|-------|------------------------------------------------------|------------|-----------------|-------------------------|
| rust_ntex | 384.0K | 316% | 1,057.1K | tonic, 20 x 600 | 400% | 2.8x | 1.0x |
| java_vertx_grpc | 349.7K | 368% | 671.8K | tonic, 28 x 338 | 395% | 1.9x | 1.6x |
| dotnet_grpc | 280.8K | 391% | 669.7K | tonic, 20 x 900 | 399% | 2.4x | 1.6x |
| java_quarkus | 319.9K | 368% | 632.1K | tonic, 28 x 75 | 396% | 2.0x | 1.7x |
| dotnet_grpc_aot (new) | 271.1K | 396% | 613.4K | tonic, 20 x 600 | 400% | 2.3x | 1.7x |
| scala_akka | 235.8K | 399% | 531.6K | ghz, 28 x 150 | 394% | 2.3x | 2.0x |
| rust_tonic_mt | 267.4K | 386% | 490.9K | tonic, 28 x 300 | 391% | 1.8x | 2.2x |
| java_armeria | 242.2K | 400% | 484.9K | tonic, 28 x 112 | 404% | 2.0x | 2.2x |
| rust_volo_grpc (new) | 243.1K | 392% | 427.6K | tonic, 28 x 225 | 389% | 1.8x | 2.5x |
| java_hotspot_grpc_pgc (grpc-java) | 206.3K | 399% | 398.7K | tonic, 28 x 338 | 404% | 1.9x | 2.7x |
| java_micronaut_workstealing | 212.7K | 398% | 387.1K | tonic, 28 x 150 | 398% | 1.8x | 2.7x |
| go_kitex_grpc (new) | 213.2K | 379% | 373.9K | ghz, 28 x 150 | 386% | 1.8x | 2.8x |
| go_vtgrpc | 201.3K | 372% | 357.3K | tonic, 28 x 450 | 390% | 1.8x | 3.0x |
| go_grpc | 196.6K | 375% | 340.3K | tonic, 28 x 300 | 382% | 1.7x | 3.1x |
| rust_grpcio | 144.2K | 398% | 269.4K | ghz, 28 x 150 | 399% | 1.9x | 3.9x |
| kotlin_grpc | 158.1K | 399% | 267.0K | tonic, 28 x 450 | 400% | 1.7x | 4.0x |
| swift_grpc | 148.7K | 400% | 243.5K | ghz, 28 x 150 | 397% | 1.6x | 4.3x |
| cpp_grpc_mt | 101.9K | 401% | 226.7K | tonic, 4 x 240 | 399% | 2.2x | 4.7x |
| cpp_asio_grpc | 88.8K | 398% | 212.3K | tonic, 4 x 240 | 399% | 2.4x | 5.0x |
| scala_pekko | 196.7K | 398% | 196.7K | the default shape | 398% | 1.0x | 5.4x |
| go_vtconnect | 56.7K | 393% | 108.7K | ghz, 28 x 150 | 394% | 1.9x | 9.7x |
| go_connect | 55.5K | 393% | 106.9K | ghz, 28 x 150 | 394% | 1.9x | 9.9x |
| java_micronaut | 45.0K | 399% | 68.0K | ghz, 28 x 150 | 400% | 1.5x | 16x |
| node_grpcjs_st | 34.5K | 126% | 59.5K | ghz, 28 x 150 | 135% | 1.7x | 18x |
| swift_grpc2 (new) | 16.0K | 400% | 16.0K | the default shape | 400% | 1.0x | 66x |
| python_async_grpc | 4.1K | 204% | 4.1K | the default shape | 204% | 1.0x | 257x |

`node_grpcjs_st` and `python_async_grpc` are single-process servers, so they can't use 4 CPUs.

What the table shows:

- **Most servers are at 400% CPU even with the default shape**, but handle half as many calls. With
  20 calls per connection a server spends most of its time on small reads and writes; with a few
  hundred calls per connection it processes them in batches.
- **Every multi-threaded server is saturated at its best shape** (382-404%).
- **The best shape is server-specific.** `rust_ntex` and grpc-dotnet peak with 20 connections and
  600-900 calls each, Quarkus and Armeria at about 75-110 calls per connection, the C++ servers with
  as few as 4 connections.
- **The client matters too.** Akka reaches 532K with `ghz` and 282K with `tonic_client`, although it
  is at 400% CPU in both cases. Kitex, grpcio, Swift, ConnectRPC, Micronaut and Node.js are also
  faster with `ghz`.
- **Native AOT doesn't help grpc-dotnet here:** the AOT build is 3-8% slower than the JIT build with
  the same GC settings.

## Details per Client

### ghz, 28 Processes

28 single-CPU `ghz` processes with one connection and 150 concurrent calls each (28 connections,
4,200 calls), one run. Calls/s counts `OK` responses only.

| Server | Calls/s | p50 / p99 | Server CPU | Failed calls |
|--------|---------|-----------|------------|--------------|
| rust_ntex | 808.4K | 3.9 / 13.0 ms | 315% (client-limited) | 2.6K |
| scala_akka | 531.6K | 4.3 / 28.7 ms | 394% | 2.6K |
| java_vertx_grpc | 529.7K | 4.8 / 61.9 ms | 359% | 3.9K |
| dotnet_grpc | 519.0K | 4.8 / 23.7 ms | 392% | 3.4K |
| java_quarkus | 491.4K | 5.0 / 18.2 ms | 392% | 3.1K |
| dotnet_grpc_aot | 474.8K | 5.2 / 29.3 ms | 401% | 3.1K |
| rust_tonic_mt | 463.1K | 4.0 / 39.4 ms | 390% | 3.0K |
| java_armeria | 445.6K | 3.7 / 52.9 ms | 402% | 2.8K |
| rust_volo_grpc | 417.5K | 4.4 / 43.8 ms | 391% | 2.5K |
| go_kitex_grpc | 373.9K | 5.7 / 27.3 ms | 386% | 3.7K |
| java_hotspot_grpc_pgc | 356.2K | 5.1 / 48.0 ms | 399% | 3.3K |
| go_vtgrpc | 348.9K | 6.3 / 32.4 ms | 381% | 3.2K |
| go_grpc | 339.7K | 7.2 / 33.5 ms | 383% | 3.1K |
| java_micronaut_workstealing | 327.6K | 5.3 / 52.5 ms | 400% | 3.6K |
| rust_grpcio | 269.4K | 9.4 / 26.2 ms | 399% | 4.0K |
| kotlin_grpc | 253.1K | 5.0 / 65.8 ms | 400% | 3.7K |
| swift_grpc | 243.5K | 6.7 / 33.3 ms | 397% | 30.5K |
| scala_pekko | 163.5K | - | 345% | 24.4M |
| cpp_grpc_mt | 142.5K | - | 398% | 667K |
| cpp_asio_grpc | 133.2K | - | 401% | 560K |
| go_vtconnect | 108.7K | 13.8 / 146.6 ms | 394% | 4.1K |
| go_connect | 106.9K | 13.4 / 122.0 ms | 394% | 4.0K |
| java_micronaut | 68.0K | 32.8 / 98.0 ms | 400% | 4.1K |
| node_grpcjs_st | 59.5K | 19.7 / 131.5 ms | 135% | 4.0K |
| python_async_grpc | 4.0K | - | 371% | 1.96M |
| swift_grpc2 | 0.2K | - | 399% | 682K |

A few thousand failed calls per run are the calls `ghz` cancels when its time is up. The rows with
hundreds of thousands or millions are servers failing under this load; their latency isn't listed.

### tonic_client

One client container with 28 CPUs unless stated otherwise. Each cell is calls/s and the server CPU.
The first six columns are the shapes every server ran with; "Best" is the best result over these and
two more shapes around the best one, with the result of a confirmation run of that shape.

| Server | 28 x 150 | 28 x 300 | 28 x 450 | 28 x 600 | 20 x 600 (20 CPUs) | 56 x 450 | Best: shape, calls/s, confirmation run |
|--------|----------|----------|----------|----------|--------------------|----------|-----------------------------------------|
| rust_ntex | 985.9K / 400% | 1,049.8K / 400% | 1,046.0K / 399% | 1,021.6K / 399% | 1,057.1K / 400% | 1,049.2K / 397% | 20 x 600, 1,057.1K, 1,032.4K |
| java_vertx_grpc | 648.9K / 395% | 657.9K / 395% | 667.6K / 395% | 664.5K / 395% | 664.7K / 395% | 662.5K / 393% | 28 x 338, 671.8K, 663.4K |
| dotnet_grpc | 637.2K / 400% | 607.9K / 400% | 593.4K / 399% | 608.0K / 403% | 664.4K / 400% | 507.4K / 401% | 20 x 900, 669.7K, 664.5K |
| java_quarkus | 577.9K / 396% | 525.5K / 400% | 511.7K / 397% | 471.5K / 401% | 480.2K / 399% | 427.2K / 399% | 28 x 75, 632.1K, 617.3K |
| dotnet_grpc_aot | 580.9K / 400% | 574.2K / 403% | 547.9K / 400% | 544.7K / 401% | 613.4K / 400% | 470.8K / 399% | 20 x 600, 613.4K, 600.6K |
| rust_tonic_mt | 490.0K / 390% | 490.8K / 388% | 479.5K / 389% | 471.5K / 388% | 480.6K / 384% | 442.5K / 392% | 28 x 300, 490.8K, 490.9K |
| java_armeria | 477.1K / 400% | 387.9K / 399% | 332.5K / 399% | 310.8K / 401% | 414.5K / 400% | 402.6K / 400% | 28 x 112, 484.9K, 482.6K |
| rust_volo_grpc | 425.8K / 393% | 425.9K / 391% | 417.3K / 390% | 402.5K / 388% | 418.5K / 383% | 382.9K / 393% | 28 x 225, 427.4K, 427.6K |
| java_hotspot_grpc_pgc | 387.9K / 402% | 377.1K / 399% | 390.8K / 401% | 385.5K / 400% | 384.1K / 400% | 378.3K / 400% | 28 x 338, 398.7K, 383.6K |
| java_micronaut_workstealing | 387.1K / 398% | 383.1K / 401% | 384.1K / 400% | 381.1K / 400% | 383.8K / 399% | 376.6K / 400% | 28 x 150, 387.1K, 375.9K |
| go_vtgrpc | 344.4K / 379% | 353.5K / 383% | 357.3K / 390% | 350.3K / 390% | 342.7K / 383% | 352.7K / 390% | 28 x 450, 357.3K, 355.3K |
| go_grpc | 327.3K / 380% | 340.3K / 382% | 336.7K / 386% | 334.6K / 386% | 333.0K / 385% | 324.5K / 390% | 28 x 300, 340.3K, 336.5K |
| go_kitex_grpc | 320.5K / 386% | 317.6K / 388% | 307.3K / 388% | 297.5K / 388% | 302.5K / 388% | 299.7K / 388% | 28 x 150, 320.5K, 320.6K |
| scala_akka | 247.9K / 400% | 281.7K / 403% | 273.8K / 397% | 272.9K / 400% | 274.8K / 400% | 263.2K / 400% | 28 x 225, 282.5K, 278.2K |
| kotlin_grpc | 259.3K / 400% | 263.3K / 402% | 267.0K / 400% | 260.9K / 399% | 259.9K / 399% | 259.5K / 399% | 28 x 450, 267.0K, 257.2K |
| rust_grpcio | 235.9K / 402% | 209.9K / 399% | 196.4K / 397% | 184.9K / 399% | 189.5K / 399% | 196.1K / 397% | 28 x 150, 235.9K, 227.9K |
| swift_grpc | 191.4K / 400% | 193.0K / 400% | 193.6K / 400% | 194.4K / 400% | 194.3K / 400% | 191.3K / 400% | 28 x 600, 194.4K, 192.9K |
| go_vtconnect | 60.3K / 396% | 67.5K / 396% | 66.4K / 399% | 66.1K / 396% | 69.2K / 396% | 62.3K / 396% | 20 x 450, 69.2K, 69.4K |
| go_connect | 58.7K / 396% | 66.2K / 394% | 65.8K / 396% | 65.0K / 396% | 68.0K / 396% | 63.9K / 397% | 20 x 450, 69.2K, 68.9K |
| java_micronaut | 52.8K / 400% | 51.9K / 402% | 52.9K / 399% | 52.3K / 401% | 51.9K / 399% | 52.5K / 397% | 28 x 338, 53.2K, 52.2K |
| node_grpcjs_st | 47.0K / 135% | 47.9K / 137% | - | - | - | - | 28 x 300, 47.9K |

The other five servers fail at these shapes and are covered in the next section.

### Servers with Limits

Five servers return errors or stop working at the shapes above. Each was measured again inside its
limit, without a search for its best shape, since none of them is near the top of the table.

| Server | What happens | Result inside the limit (`tonic_client`) |
|--------|--------------|-------------------------------------------|
| cpp_grpc_mt | Returns `CANCELLED` for most calls once much more than 1,000 calls are pending: 28 x 150 completes 29K calls/s and cancels 1.7M calls in 10 s. | 4 x 240: 226.7K; 8 x 120: 145.0K; 14 x 70: 81.0K; 28 x 35: 60.5K |
| cpp_asio_grpc | The same. | 4 x 240: 212.3K; 8 x 120: 114.1K; 14 x 70: 74.9K; 28 x 35: 55.9K |
| python_async_grpc | The same; one process. | 4 x 100: 4.0K; 8 x 120: 4.0K |
| swift_grpc2 | Allows 100 concurrent calls per connection: 100 works, 101 breaks the connection ("transport error"). | 28 x 50: 14.9K; 28 x 100: 13.7K; 56 x 100: 13.1K; 112 x 50: 13.3K |
| scala_pekko | The JVM exits with `StackOverflowError` under a few thousand concurrent calls and stops accepting connections; unstable above about 35 calls per connection. | 28 x 35: 148.7K |

- The two C++ servers and the Python one share gRPC's C core. Before the upgrade, on gRPC 1.51,
  `cpp_grpc_mt` handled 28 x 150 without errors (219K calls/s); on 1.85 it sheds load instead.
  Unlike every other server here, these get faster with fewer connections.
- `swift_grpc2` is at 400% CPU with 13-16K calls/s, so the limit isn't what makes it slow.

## Effect of the Upgrade

The best tuned result of each server before the upgrade (the versions grpc_bench pins at commit
`ee8f9a7`) and after it. The old figures were measured the same way, with a 28-process version of the
same Rust client.

| Server | Before | After | Change | Main version change |
|--------|--------|-------|--------|---------------------|
| rust_ntex | 1,008.8K | 1,057.1K | +5% | ntex 4.6.0 to 4.9.1 |
| java_vertx_grpc | 785.6K | 671.8K | -14% | Vert.x 4.4.4 to 5.2.3, grpc-java 1.58 to 1.85, JDK 23 to 25 |
| dotnet_grpc | 611.5K (593.1K with DATAS on) | 669.7K | +10% | .NET 10 to 11, Grpc.AspNetCore 2.76 to 2.85, DATAS off |
| java_quarkus | 593.4K | 632.1K | +7% | Quarkus 3.1.2 to 3.40.1 |
| scala_akka | 489.9K | 531.6K | +9% | Akka 2.8.2 to 2.8.8, JDK 23 to 25 |
| rust_tonic_mt | 488.8K | 490.9K | 0% | tonic 0.14.5 to 0.14.9 |
| go_vtgrpc | 416.9K | 357.3K | -14% | grpc-go 1.63 to 1.85, Go 1.23 to 1.27 |
| go_grpc | 395.0K | 340.3K | -14% | grpc-go 1.79 to 1.85, Go 1.26 to 1.27 |
| java_armeria | 383.7K | 484.9K | +26% | Armeria 1.20.3 to 1.43.0 |
| java_hotspot_grpc_pgc | 319.2K | 398.7K | +25% | grpc-java 1.58 to 1.85, JDK 23 to 25 |
| rust_grpcio | 291.9K | 269.4K | -8% | Rust 1.99 to 1.100 only |
| swift_grpc | 251.3K | 243.5K | -3% | Swift 6.0.3 to 6.4.2, grpc-swift 1.27.6 |
| cpp_grpc_mt | 219.5K | 226.7K | +3% | gRPC C++ 1.51 to 1.85 (and a different best shape) |
| node_grpcjs_st | 47.5K | 59.5K | +25% | Node.js 20 to 26 |

The "before" and "after" figures are single best runs measured on different days, so differences of
a few percent are within run-to-run noise.

## Measurements Before the Upgrade

The sections below were measured on 2026-11-15 and 2026-11-16 on the versions grpc_bench pinned
before the upgrade, with a 28-process predecessor of `tonic_client` (called "Rust client" below).
They explain the method, and their conclusions hold for the upgraded servers.

### Why ghz Doesn't Saturate the Fastest Servers

With 28 `ghz` processes, `rust_ntex`, Vert.x and `rust_tonic_mt` stay at 305-357% server CPU. The
reason is the cost of `ghz` itself:

- Every `ghz` process is at about 98% of its CPU during the run.
- Throughput against `rust_ntex` stops scaling once the physical cores are used up:

| ghz processes (1 CPU, 1 connection, 150 calls each) | Calls/s | Server CPU |
|-----------------------------------------------------|---------|------------|
| 8 | 397.7K | 182% |
| 16 | 663.5K | 306% |
| 20 | 697.9K | 330% |
| 24 | 790.9K | 325% |
| 28 | 714K-790K | 305-327% |

- One `ghz` process handles about 50K calls/s on a full core and about 27K when all 32 logical CPUs
  are busy, which is 20-35 us of CPU per call. `rust_ntex` spends about 4 us per call.

None of these changed the result: pinning the server and the clients to separate CPUs, more
connections per process (56, 112, 224 connections: 717K, 704K, 673K), more calls per connection,
`GOGC=400`, `--async`, a shared network namespace instead of the Docker bridge.

Lowering the server's CPU limit is the only way to saturate these servers with `ghz` on this machine
(28 `ghz` processes, 28 x 150):

| Server | 2 CPUs | 3 CPUs | 4 CPUs |
|--------|--------|--------|--------|
| rust_ntex | 558.6K (194%) | 669.5K (262%) | 789.0K (305%) |
| java_vertx_grpc | 443.6K (199%) | 524.9K (280%) | 580.1K (338%) |
| rust_tonic_mt | 246.8K (188%) | 347.8K (276%) | 409.3K (357%) |
| dotnet_grpc | 351.3K (200%) | 447.7K (297%) | 527.4K (381%) |

grpc-dotnet is saturated in all three columns and gains 1.5x from 2 to 4 CPUs: the server's extra
CPUs are SMT siblings of cores the clients are using.

### Connections and Calls per Connection: rust_ntex and dotnet_grpc

Rust client, one connection per client process unless stated otherwise. The server is at 399-402%
CPU in every row marked with no CPU figure.

| Connections x calls per connection | rust_ntex | dotnet_grpc |
|------------------------------------|-----------|-------------|
| 4 x 2,000 (7 CPUs per process) | 162.7K (191%) | - |
| 8 x 1,000 (3 CPUs per process) | 596.6K (352%) | 377.4K |
| 12 x 1,000 / 12 x 600 (2 CPUs per process) | 810.8K | 516.9K |
| 14 x 600 (2 CPUs per process) | 813.6K | 506.9K |
| 20 x 600 | 936.7K | 492.7K |
| 28 x 40 | 744.0K | 423.3K |
| 28 x 75 | 834.4K | 469.8K |
| 28 x 150 | 899.8K | 527.0K |
| 28 x 300 | 967.8K | 526.1K |
| 28 x 450 | 1,008.8K | 593.1K |
| 28 x 600 | 1,000.6K | 556.2K |
| 28 x 900 | 985.9K | - |
| 28 x 1,200 | 987.4K | 580.1K |
| 56 x 450 (2 connections per process) | 990.2K | 558.5K |
| 112 x 300 / 112 x 150 (4 connections per process) | 946.6K | 428.3K |

- Throughput grows with the number of calls per connection up to about 450 and then levels off,
  while latency keeps growing (for `rust_ntex`, p50 is 3.4 ms at 150 calls per connection, 11.6 ms at
  450 and 34.6 ms at 1,200).
- Fewer connections with more calls each are slower even when the server is at 400% CPU. One
  connection of this client carries about 75K calls/s at most, so 8 connections can't saturate
  `rust_ntex`.
- One multi-threaded client process is slower than many single-CPU ones: one Rust client process
  with 28 CPUs and 28 connections x 150 calls gives 667K against `rust_ntex`, and 28 single-CPU
  processes give 900K. `ghz` shows the same.

### .NET Runtime Settings of dotnet_grpc

grpc_bench's .NET server sets no performance-related options. Its `runtimeconfig.json` has only
`System.GC.Server: true` (the default of the Web SDK), so on .NET 10 it runs with DATAS on.
`RpcBenchmark` turns DATAS off (`GarbageCollectionAdaptationMode=0`) and RetainVM on. Tiered
compilation and PGO are at their defaults in both.

Rust client, 28 connections, two runs per cell:

| Server setting | 28 x 450 | 28 x 150 | Server memory |
|----------------|----------|----------|---------------|
| As shipped (DATAS on) | 588.2K, 582.1K | 513.4K, 515.5K | about 160 MB |
| `DOTNET_GCDynamicAdaptationMode=0` | 601.4K, 611.5K | 540.9K, 505.4K | about 430 MB |
| + `DOTNET_GCRetainVM=1` | 600.7K, 606.1K | 538.2K, 538.3K | about 420 MB |

Turning DATAS off is worth 3-5% at 450 calls per connection and costs about 270 MB of memory. At 150
calls per connection the difference is within the run-to-run noise. RetainVM adds nothing on top.

### .NET Client

One process, 24 CPUs, the worker model of `RpcBenchmark`; the best of three shapes (2,152 workers
over 24 channels, 4,096 over 16, 4,800 over 48) and of 3 tries of 5 s.

| Server | Calls/s | Best shape (workers / channels) |
|--------|---------|---------------------------------|
| rust_ntex | 557.0K | 4,096 / 16 |
| dotnet_grpc | 554.4K | 2,152 / 24 |
| java_vertx_grpc | 481.4K | 4,096 / 16 |
| java_quarkus | 392.2K | 2,152 / 24 |
| rust_tonic_mt | 313.9K | 4,096 / 16 |
| go_vtgrpc | 296.8K | 4,096 / 16 |
| go_grpc | 278.6K | 4,800 / 48 |
| java_armeria | 261.4K | 2,152 / 24 |
| java_hotspot_grpc_pgc | 243.9K | 4,096 / 16 |
| scala_akka | 218.7K | 4,096 / 16 |

This client is the most expensive of the three per call, and it is limited by its own CPU for every
server faster than grpc-dotnet. It is the client `RpcBenchmark` uses for its gRPC test, so the 567K
that test reports for grpc-dotnet is closer to a lower bound than to a tuned figure.

## Comparison with RpcBenchmark

`SayHello`, 4-CPU server, no TLS, the same machine. The `RpcBenchmark` rows are from
[Benchmarks.md](Benchmarks.md#docker-calls).

| Server | Client and shape (connections x calls per connection) | Calls/s |
|--------|------------------------------------------------------|---------|
| ActualLab.Rpc (RpcBenchmark) | RpcBenchmark, 54 x 451 | 2.54M |
| rust_ntex | tonic_client, 20 x 600 | 1.06M |
| SignalR (RpcBenchmark) | RpcBenchmark, 93 x 213 | 999K |
| java_vertx_grpc | tonic_client, 28 x 338 | 672K |
| dotnet_grpc | tonic_client, 20 x 900 | 670K |
| java_quarkus | tonic_client, 28 x 75 | 632K |
| dotnet_grpc_aot | tonic_client, 20 x 600 | 613K |
| gRPC (RpcBenchmark) | RpcBenchmark, 30 x 82 | 567K |
| scala_akka | ghz, 28 x 150 | 532K |
| MagicOnion (RpcBenchmark) | RpcBenchmark, 23 x 78 | 518K |
| rust_tonic_mt | tonic_client, 28 x 300 | 491K |
| java_armeria | tonic_client, 28 x 112 | 485K |
| go_grpc | tonic_client, 28 x 300 | 340K |

The two sides are not measured identically, and the differences favor the grpc_bench servers:

- The grpc_bench servers got a per-server search over clients and load shapes. The `RpcBenchmark`
  rows use the best known parameters of its own client, with one nearby set as a check.
- The `RpcBenchmark` client is one .NET process with 24 CPUs, in which workers share connections.
  For gRPC that is the least favorable client measured here (see [.NET Client](#net-client)).

## Versions

Every server runs on the newest stable release of its runtime and gRPC library as of 2026-11-16,
with the exceptions listed below the table.

| Server | Runtime | gRPC library and related | Before the upgrade |
|--------|---------|--------------------------|--------------------|
| rust_ntex | Rust 1.100 | ntex 4.9.1, ntex-grpc 4.1.1 | ntex 4.6.0, ntex-grpc 4.1.0 (2 weeks old) |
| rust_tonic_mt | Rust 1.100 | tonic 0.14.9, prost 0.14.6, tokio 1.54.1 | tonic 0.14.5 (8 months old) |
| rust_volo_grpc (new) | Rust 1.100 | volo-grpc 0.12.2, volo 0.12.5 | - |
| rust_grpcio | Rust 1.100 | grpcio 0.13.0 (gRPC C core 1.56) | the same; the crate's last release is from 2023-08 |
| java_vertx_grpc | JDK 25 | Vert.x 5.2.3 (`vertx-grpcio-server`), grpc-java 1.85.1 | Vert.x 4.4.4, grpc-java 1.58.0 (3.4 years old) |
| java_quarkus | JDK 25 | Quarkus 3.40.1.2 | Quarkus 3.1.2 (3.4 years old) |
| java_armeria | JDK 25 | Armeria 1.43.0, grpc-java 1.85.1 | Armeria 1.20.3 (4 years old) |
| java_hotspot_grpc_pgc | JDK 25 | grpc-java 1.85.1 | grpc-java 1.58.0 (3.1 years old) |
| java_micronaut, java_micronaut_workstealing | JDK 25 | Micronaut 5.2.6, grpc-java 1.85.1 | Micronaut 4.0.0 and 3.5.3, grpc-java 1.56.1 and 1.49.0 |
| kotlin_grpc | JDK 25 | grpc-kotlin 1.5.0, Kotlin 2.4.21, grpc-java 1.85.1 | grpc-kotlin 1.3.0, Kotlin 1.7.10, grpc-java 1.58.0 |
| scala_akka | JDK 25 | Akka 2.8.8, Akka gRPC 2.3.4, Akka HTTP 10.5.3 | Akka 2.8.2, Akka gRPC 2.3.2 (3.5 years old) |
| scala_pekko | JDK 25 | Pekko 1.7.1, Pekko gRPC 1.2.0, Pekko HTTP 1.4.2 | Pekko 2.0.0-M3, Pekko gRPC 2.0.0-M2 (milestones) |
| dotnet_grpc | .NET 11.0.0 | Grpc.AspNetCore 2.85.0, DATAS off | .NET 10.0.10, Grpc.AspNetCore 2.76.0 (10 months old) |
| dotnet_grpc_aot (new) | .NET 11.0.0, Native AOT | Grpc.AspNetCore 2.85.0, DATAS off | - |
| go_grpc | Go 1.27.4 | grpc-go 1.85.0 | Go 1.26.2, grpc-go 1.79.3 (7 months old) |
| go_vtgrpc | Go 1.27.4 | grpc-go 1.85.0, vtprotobuf 0.6.0 | Go 1.23.2, grpc-go 1.63.2 (2.6 years old) |
| go_connect, go_vtconnect | Go 1.27.4 | connect-go 1.21.1 | Go 1.23.2, connect-go 1.16.1 |
| go_kitex_grpc (new) | Go 1.27.4 | Kitex 0.16.4 | - |
| cpp_grpc_mt | GCC 16.2 | gRPC C++ 1.85.2, built from source | gRPC C++ 1.51.1 from Debian (4 years old) |
| cpp_asio_grpc | GCC 16.2 | gRPC C++ 1.85.2, asio-grpc 3.7.0, Boost 1.92.0 | asio-grpc 1.0.0, gRPC from Debian |
| swift_grpc | Swift 6.4.2 | grpc-swift 1.27.6 | Swift 6.0.3 (1.9 years old) |
| swift_grpc2 (new) | Swift 6.4.2 | grpc-swift-2 2.4.4, NIO transport 2.13.0 | - |
| node_grpcjs_st | Node.js 26.13.1 | @grpc/grpc-js 1.14.7 | Node.js 20.3.1 (3.4 years old) |
| python_async_grpc | Python 3.15 | grpcio 1.85.1 | Python 3.13 |

Not on the very latest version:

- **Akka** is at 2.8.8, the newest release on Maven Central. Later releases (2.11.0 as of this
  writing) are published only to Akka's own repository, which needs a license token.
- **Pekko** is on the stable 1.x line. Pekko 2.0 is out, but Pekko gRPC has no stable 2.x release.
- **JDK 25** is the current LTS release; newer feature releases exist.
- **Protobuf** for the Java servers is the version grpc-java 1.85.1 pins (3.25.9).

Load generators and the `RpcBenchmark` side:

| Component | Version |
|-----------|---------|
| ghz | 0.114.0, the version grpc_bench uses (2023-02; the latest is 0.121.0) |
| tonic_client | tonic 0.14.9, prost 0.14.6, tokio 1.54.1, Rust 1.100 |
| .NET (RpcBenchmark) | 11.0.0 |
| ActualLab.Fusion / ActualLab.Rpc | 15.1.38 |
| Grpc.AspNetCore (RpcBenchmark) | 2.85.0 |
| SignalR | 11.0.0 |
| MagicOnion | 7.11.0 |
| StreamJsonRpc | 2.26.150 |

## Notes

- `java_micronaut` and `java_micronaut_workstealing` have the same server code and differ in the
  executor. Upstream's `java_micronaut_workstealing_bench/Dockerfile` copied the `java_micronaut_bench`
  directory, so it built the wrong server; the branch fixes that.
- `go_kitex_grpc` needs a small hand-written file for the nested `Pet.Color` enum, which Kitex's
  code generator omits.
- `rust_thruster_mt` returns status OK without a message to some clients, and `rust_wtx` returned
  empty replies, so both were left out. The GraalVM native-image servers (`java_aot`,
  `java_quarkus_native`) were not run.
- `rust_grpcio` exits at once when it has no TTY; it needs `docker run --tty`, which grpc_bench's
  own script passes.
- Results with `h2load` (nghttp2's load generator) were measured too and are not listed: it doesn't
  decode replies and sends perfectly regular batches, so it measures something no gRPC client does.
