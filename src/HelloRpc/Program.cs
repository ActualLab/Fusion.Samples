using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Samples.HelloRpc;
using ActualLab.Rpc;
using ActualLab.Rpc.Server;
using static System.Console;

#pragma warning disable ASP0000

RpcServiceRegistry.ConstructionDumpLogLevel = LogLevel.Information;
var stopTokenSource = new CancellationTokenSource();
var cancellationToken = stopTokenSource.Token;
CancelKeyPress += (_, _) => stopTokenSource.Cancel();

var baseUrl = "http://localhost:22222/";
await (args switch {
    [ "server" ] => RunServer(),
    [ "client" ] => RunClient(),
    _ => Task.WhenAll(RunServer(), RunClient()),
});

async Task RunServer()
{
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders().AddConsole();
    var rpc = builder.Services.AddRpc();
    rpc.AddWebSocketServer();
    rpc.AddServer<IGreeter, Greeter>();
    rpc.AddClient<IClientNotifier>();
    builder.Services.AddSingleton<RpcOutboundCallOptions>(c => RpcOutboundCallOptions.Default with {
        RouterFactory = methodDef => {
            if (methodDef.Service.Type == typeof(IClientNotifier)) // Special router for IClientNotifier
                return args => RpcPeerRef.FromAddress(args.Get<string>(0));
            return args => RpcPeerRef.Default; // The default one otherwise
        }
    });
    var app = builder.Build();
    app.Urls.Add(baseUrl);
    app.UseWebSockets();
    app.MapRpcWebSocketServer();

    await app.StartAsync(cancellationToken);
    WriteLine("Server started, press Ctrl-C to stop it.");
    await Task.Delay(TimeSpan.FromDays(1), cancellationToken).SilentAwait();
    WriteLine("Server is shutting down.");
    await app.StopAsync();
}

async Task RunClient()
{
    var services = new ServiceCollection();
    services.AddLogging(logging => logging.AddConsole());
    var rpc = services.AddRpc();
    rpc.AddWebSocketClient(baseUrl);
    rpc.AddClient<IGreeter>();
    rpc.AddServer<IClientNotifier, ClientNotifier>();

    using var serviceProvider = services.BuildServiceProvider();
    var greeter = serviceProvider.GetRequiredService<IGreeter>();

    Write("Your name: ");
    var name = ReadLine() ?? "";
    var message = await greeter.SayHello(name, cancellationToken);
    WriteLine(message);
    WriteLine("Press Ctrl-C to exit.");
    await Task.Delay(TimeSpan.FromDays(1), cancellationToken).SilentAwait();
}
