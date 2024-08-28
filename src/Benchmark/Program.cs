﻿using Microsoft.AspNetCore.Builder;
using Samples.Benchmark;
using Samples.Benchmark.Client;
using Samples.Benchmark.Server;
using ActualLab.Fusion.Server;
using ActualLab.Rpc;
using ActualLab.Rpc.Infrastructure;
using ActualLab.Rpc.Serialization;
using ActualLab.Rpc.Server;
using ActualLab.Rpc.WebSockets;

#pragma warning disable ASP0000

// var minThreadCount = WorkerCount * 2;
// ThreadPool.SetMinThreads(minThreadCount, minThreadCount);
ThreadPool.SetMaxThreads(16_384, 16_384);
ByteSerializer.Default = MessagePackByteSerializer.Default; // Remove to switch back to MemoryPack
RpcDefaultDelegates.WebSocketChannelOptionsProvider =
    (_, _) => WebSocketChannel<RpcMessage>.Options.Default with {
        Serializer = new FastRpcMessageByteSerializer(ByteSerializer.Default),
        FrameDelayerFactory = null,
    };

var stopCts = new CancellationTokenSource();
var cancellationToken = StopToken = stopCts.Token;
TreatControlCAsInput = false;
CancelKeyPress += (_, ea) => {
    stopCts.Cancel();
    ea.Cancel = true;
};

await (args switch {
    [ "server" ] => RunServer(),
    [ "client" ] => RunClient(),
    _ => Task.WhenAll(RunServer(), RunClient()),
});

async Task RunServer()
{
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders()
        .AddDebug()
        .SetMinimumLevel(LogLevel.Warning);

    // Core services
    var services = builder.Services;
    services.AddAppDbContext();
    var fusion = services.AddFusion(RpcServiceMode.Server);
    fusion.AddWebServer();

    // Benchmark services
    fusion.AddService<IFusionTestService, FusionTestService>();
    fusion.Rpc.Service<IRpcTestService>().HasServer<IFusionTestService>();

    // Build app & initialize DB
    var app = builder.Build();
    var dbInitializer = app.Services.GetRequiredService<DbInitializer>();
    await dbInitializer.Initialize(true);

    // Start Kestrel
    app.Urls.Add(BaseUrl);
    app.UseWebSockets();
    app.MapRpcWebSocketServer();
    app.MapTestService<DbTestService>("/api/dbTestService");
    app.MapTestService<IFusionTestService>("/api/fusionTestService");
    try {
        await app.StartAsync(cancellationToken);
        await TaskExt.NewNeverEndingUnreferenced().WaitAsync(cancellationToken);
    }
    catch (OperationCanceledException) { }
    catch (Exception error) {
        Error.WriteLine($"Server failed: {error.Message}");
    }
}

async Task RunClient()
{
    // Initialize
    var dbServices = ClientServices.DbServices;
    await ServerChecker.WhenReady(BaseUrl, cancellationToken);
    await dbServices.GetRequiredService<DbInitializer>().Initialize(true, cancellationToken);
    WriteLine($"Item count:         {ItemCount}");
    WriteLine($"Client concurrency: {TestServiceConcurrency} workers per client or test service");
    WriteLine($"Writer count:       {WriterCount}");
    var benchmarkRunner = new BenchmarkRunner("Initialize", ClientServices.LocalDbServiceFactory);
    await benchmarkRunner.Initialize(cancellationToken);

    // Run
    WriteLine();
    WriteLine("Local services:");
    await new BenchmarkRunner("Fusion Service", ClientServices.LocalFusionServiceFactory).Run();
    await new BenchmarkRunner("Regular Service", ClientServices.LocalDbServiceFactory, 2).Run();

    WriteLine();
    WriteLine("Remote services:");
    await new BenchmarkRunner("Fusion Client -> Fusion Service", ClientServices.RemoteFusionServiceFactory).Run();
    await new BenchmarkRunner("ActualLab.Rpc Client -> Fusion Service", ClientServices.RemoteFusionServiceViaRpcFactory, 10).Run();
    await new BenchmarkRunner("HTTP Client -> Fusion Service", ClientServices.RemoteFusionServiceViaHttpFactory, 5).Run();
    await new BenchmarkRunner("HTTP Client -> Regular Service", ClientServices.RemoteDbServiceViaHttpFactory, 5).Run();

    ReadKey();
    // ReSharper disable once AccessToDisposedClosure
    stopCts.Cancel();
}
