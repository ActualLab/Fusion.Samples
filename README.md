Welcome to the collection of [Fusion] samples!

> All project updates are published on its [Fusion Place]; it's also the best place for Q/A.\
> [![Build](https://github.com/ActualLab/Fusion.Samples/workflows/Build/badge.svg)](https://github.com/ActualLab/Fusion.Samples/actions?query=workflow%3A%22Build%22)
> [![Fusion Place](https://img.shields.io/badge/Fusion%20%40%20Voxt-BE145B)](https://voxt.ai/chat/s-1KCdcYy9z2-uJVPKZsbEo)  

Don't know what Fusion is? [You should!](https://github.com/ActualLab/Fusion) 
Fusion is your #1 partner in crime if you're 
building a real-time app (an online app delivering
some or all updates in real-time) or a high-load app.
Moreover, it plays really well with Blazor and works on MAUI.

> Curious to see Fusion in action? Explore [Voxt] –
> a very new chat app built by the minds behind Fusion.
>
> Voxt fuses **real-time audio, live transcription, and AI assistance**
> to let you communicate with utmost efficiency.
> With clients for **WebAssembly, iOS, Android, and Windows**, it boasts nearly
> 100% code sharing across these platforms.
> Beyond real-time updates, several of its features, like offline mode,
> are powered by Fusion.
>
> We're posting some code examples from Voxt codebase [here](https://voxt.ai/chat/san4Cohzym),
> so join this chat to learn how we use it in a real app.

Fusion allows you to build real-time UIs like this one —
and **it's nearly as easy as if there were no logic related to real-time
updates at all**:
![](docs/img/Samples-Blazor.gif)

<img src="https://img.shields.io/badge/-Live!-red" valign="middle"> Play with
[live version of this sample](https://fusion-samples.servicetitan.com) right now!

## What's Inside?

### Console Samples

#### [HelloWorld]

Demonstrates Fusion's core compute service pattern with automatic dependency tracking.
The sample simulates a project build system that dynamically recomputes projects
when their dependencies are invalidated, showcasing automatic caching and invalidation.

![](docs/img/Samples-HelloWorld.gif)

#### [HelloCart]

Showcases five progressive implementations of a shopping cart system—from in-memory
to multi-host distributed—using Fusion's operations framework and command handlers.
Demonstrates how to track product prices and cart totals with real-time updates.

![](docs/img/Samples-HelloCart.gif)

### Blazor Samples

#### [HelloBlazorServer]

A simple Blazor Server application demonstrating real-time UI updates using
Fusion compute services. Includes a counter, weather forecast, and chat service
with server-side rendering and WebSocket communication.

![](docs/img/Samples-HelloBlazorServer.gif)

#### [HelloBlazorHybrid]

Extends HelloBlazorServer with hybrid rendering, supporting both server-side
and WebAssembly interactive components with Fusion's compute services
for real-time data binding.

#### [Blazor Sample]

A full-featured Blazor application with GitHub OAuth authentication,
database persistence (SQLite), and Entity Framework Core integration.
Demonstrates multiple compute services including chat, composition,
image streaming, and real-time sum calculations.

![](docs/img/Samples-Blazor-Auth.gif)

#### [TodoApp Sample]

A comprehensive todo application supporting both single-server and multi-tenant
distributed architectures with PostgreSQL, SQL Server, or SQLite backends.
Showcases Fusion's database entity resolvers, sharding, backend/API
server separation patterns, and RPC streaming.

### RPC Samples

#### [MiniRpc]

A minimal client-server chat application using Fusion's RPC framework
with WebSocket transport. Demonstrates compute methods, command handlers,
and real-time state synchronization between client and server.

#### [MultiServerRpc]

Extends MiniRpc with multiple servers using consistent hashing-based routing
to distribute chat conversations across servers based on chat ID,
demonstrating horizontal scalability patterns.

#### [MeshRpc]

Demonstrates a dynamic peer-to-peer mesh network where hosts are added
and removed at runtime. Fusion's shard mapping automatically routes
requests to appropriate hosts for distributed counter services.

### Benchmarks

See [Benchmarks.md](Benchmarks.md) for the most recent results.

#### [Benchmark]

Compares Fusion services against an identical non-Fusion DAL, measuring both local and remote access throughput. 

#### [RpcBenchmark]

Benchmarks ActualLab.Rpc against gRPC, SignalR, StreamJsonRpc, MagicOnion, and HTTP (REST), 
measuring both call and streaming throughput on different payload sizes.

## Running Samples

Build & run locally with [.NET 10 SDK](https://dotnet.microsoft.com/download):

```bash
# Run this command first
dotnet build
```

| Sample              | Command                                                                                                                   |
|---------------------|---------------------------------------------------------------------------------------------------------------------------|
| [HelloCart]         | `dotnet run -p src/HelloCart/HelloCart.csproj`                                                                            |
| [HelloWorld]        | `dotnet run -p src/HelloWorld/HelloWorld.csproj`                                                                          |
| [HelloBlazorServer] | `dotnet run -p src/HelloBlazorServer/HelloBlazorServer.csproj` + open http://localhost:5005/                              |
| [HelloBlazorHybrid] | `dotnet run -p src/HelloBlazorHybrid/Server/Server.csproj` + open http://localhost:5005/                                  |
| [Blazor Sample]    | `dotnet run -p src/Blazor/Server/Server.csproj` + open http://localhost:5005/                                             |
| [TodoApp Sample]    | `dotnet run -p src/TodoApp/Host/Host.csproj` + open http://localhost:5005/                                                |
| [MiniRpc]           | `dotnet run -p src/MiniRpc/MiniRpc.csproj`                                                                                |
| [MultiServerRpc]    | `dotnet run -p src/MultiServerRpc/MultiServerRpc.csproj`                                                                  |
| [MeshRpc]           | `dotnet run -p src/MeshRpc/MeshRpc.csproj`                                                                                |
| [Benchmark]         | `dotnet run -c:Release -p src/Benchmark/Benchmark.csproj`                                                                 |
| [RpcBenchmark]      | `dotnet run -c:Release -p src/RpcBenchmark/RpcBenchmark.csproj`                                                           |

Build & run with [Docker](https://docs.docker.com/get-docker/) + 
[Docker Compose](https://docs.docker.com/compose/install/):

```bash
# Run this command first
docker-compose build
```

| Sample              | Command                                                                                               |
|---------------------|-------------------------------------------------------------------------------------------------------|
| [HelloCart]         | `docker-compose run --build sample_hello_cart`                                                        |
| [HelloWorld]        | `docker-compose run --build sample_hello_world`                                                       |
| [HelloBlazorServer] | `docker-compose run --build --service-ports sample_hello_blazor_server` + open http://localhost:5005/ |
| [HelloBlazorHybrid] | `docker-compose run --build --service-ports sample_hello_blazor_hybrid` + open http://localhost:5005/ |
| [Blazor Sample]    | `docker-compose run --build --service-ports sample_blazor` + open http://localhost:5005/              |
| [TodoApp Sample]    | `docker-compose run --build --service-ports sample_todoapp` + open http://localhost:5005/             |
| [MiniRpc]           | `docker-compose run --build sample_mini_rpc`                                                          |
| [MultiServerRpc]    | `docker-compose run --build sample_multi_server_rpc`                                                  |
| [MeshRpc]           | `docker-compose run --build sample_mesh_rpc`                                                          |
| [Benchmark]         | `docker-compose run --build sample_benchmark`                                                         |
| [RpcBenchmark]      | `docker-compose run --build sample_rpc_benchmark`                                                     |

## Useful Links

* Check out [Fusion repository on GitHub]
* Join [Fusion Place] to ask questions and track project updates.
* Go to [Documentation].

**P.S.** If you've already spent some time learning about Fusion, 
please help us to make it better by completing [Fusion Feedback Form] 
(1&hellip;3 min).


[Fusion]: https://github.com/ActualLab/Fusion
[Fusion repository on GitHub]: https://github.com/ActualLab/Fusion

[HelloCart]: src/HelloCart
[HelloWorld]: src/HelloWorld
[HelloBlazorServer]: src/HelloBlazorServer
[HelloBlazorHybrid]: src/HelloBlazorHybrid
[Blazor Sample]: src/Blazor
[TodoApp Sample]: src/TodoApp
[MiniRpc]: src/MiniRpc
[MultiServerRpc]: src/MultiServerRpc
[MeshRpc]: src/MeshRpc
[Benchmark]: src/Benchmark
[RpcBenchmark]: src/RpcBenchmark
[Documentation]: https://fusion.actuallab.net/
[Voxt]: https://voxt.ai

[Compute Services]: https://fusion.actuallab.net/Part01
[Compute Service]: https://fusion.actuallab.net/Part01
[`IComputed<T>`]: https://fusion.actuallab.net/Part01
[Computed Value]: https://fusion.actuallab.net/Part01
[Compute Service Clients]: https://fusion.actuallab.net/Part03

[Fusion Place]: https://voxt.ai/chat/s-1KCdcYy9z2-uJVPKZsbEo
[Fusion Feedback Form]: https://forms.gle/TpGkmTZttukhDMRB6
