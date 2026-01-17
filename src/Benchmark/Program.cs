using System.IO;
using System.Runtime.InteropServices;
using ActualLab.Benchmarking;
using Microsoft.AspNetCore.Builder;
using Samples.Benchmark;
using Samples.Benchmark.Client;
using Samples.Benchmark.Server;
using ActualLab.Fusion.Server;
using ActualLab.OS;
using ActualLab.Rpc;
using ActualLab.Rpc.Server;

#pragma warning disable ASP0000

// var minThreadCount = WorkerCount * 2;
// ThreadPool.SetMinThreads(minThreadCount, minThreadCount);
ThreadPool.SetMaxThreads(16_384, 16_384);
RpcSerializationFormatResolver.Default = new("mempack5c");

var stopTokenSource = new CancellationTokenSource();
var stopToken = stopTokenSource.Token;
try {
    TreatControlCAsInput = false;
    CancelKeyPress += (_, ea) => {
        stopTokenSource.Cancel();
        ea.Cancel = true;
    };
}
catch (IOException) { } // Non-interactive mode

await (args switch {
    [ "server" ] => RunServer(),
    [ "client" ] => RunClient(),
    _ => Task.WhenAll(RunServer(), RunClient()),
});

async Task RunServer()
{
    try {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders()
            .AddDebug()
            .SetMinimumLevel(LogLevel.Warning);

        // Core services
        var services = builder.Services;
        services.AddAppDbContext();
        var fusion = services.AddFusion();
        fusion.AddWebServer();

        // Benchmark services
        fusion.AddServer<IFusionTestService, FusionTestService>();
        fusion.Rpc.Configure<IFusionAsRpcTestService>().IsServer(typeof(FusionTestService));

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

        await app.StartAsync(stopToken);
        WriteLine($"Server started @ {BaseUrl}");
        await TaskExt.NewNeverEndingUnreferenced().WaitAsync(stopToken);
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
    await TcpProbe.WhenReady(BaseUrl, stopToken);
    await dbServices.GetRequiredService<DbInitializer>().Initialize(true, stopToken);
    WriteLine($".NET version:       {RuntimeInfo.DotNet.VersionString ?? RuntimeInformation.FrameworkDescription}");
    WriteLine($"Item count:         {ItemCount}");
    WriteLine($"Client concurrency: {TestServiceConcurrency} workers per client or test service");
    WriteLine($"Writer count:       {WriterCount}");
    var benchmarkRunner = new BenchmarkRunner("Initialize", ClientServices.LocalDbServiceFactory);
    await benchmarkRunner.Initialize(stopToken);

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

    try { ReadKey(); }
    catch (InvalidOperationException) { } // Non-interactive mode
    // ReSharper disable once AccessToDisposedClosure
    stopTokenSource.Cancel();
}
