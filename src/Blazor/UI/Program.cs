using Blazorise;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Samples.Blazor.Abstractions;
using Samples.Blazor.UI.Services;
using ActualLab.OS;
using ActualLab.DependencyInjection;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Blazor.Authentication;
using ActualLab.Fusion.Extensions;
using ActualLab.Fusion.UI;
using ActualLab.Rpc;
using ActualLab.Rpc.Infrastructure;
using ActualLab.Rpc.Serialization;
using ActualLab.Rpc.WebSockets;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;

namespace Samples.Blazor.UI;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (!OSInfo.IsWebAssembly)
            throw new ApplicationException("This app runs only in browser.");

        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        ConfigureServices(builder.Services, builder);
        var host = builder.Build();
        await host.Services.HostedServices().Start();
        await host.RunAsync();
    }

    public static void ConfigureServices(IServiceCollection services, WebAssemblyHostBuilder builder)
    {
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        var baseUri = new Uri(builder.HostEnvironment.BaseAddress);

        // Fusion
        var fusion = services.AddFusion();
        fusion.Rpc.AddWebSocketClient(baseUri);
        // You may comment this out - the call below just enables RPC call logging
        /*
        services.AddSingleton<RpcPeerFactory>(_ =>
            static (hub, peerRef) => peerRef.IsServer
                ? throw new NotSupportedException("No server peers on the client.")
                : new RpcClientPeer(hub, peerRef) { CallLogLevel = LogLevel.Information }
        );
        */
        fusion.AddAuthClient();

        // Fusion services
        fusion.AddClient<ITimeService>();
        fusion.AddClient<IScreenshotService>();
        fusion.AddClient<IChatService>();
        fusion.AddClient<IComposerService>();
        fusion.AddClient<ISumService>();

        ConfigureSharedServices(services);
    }

    public static void ConfigureSharedServices(IServiceCollection services)
    {
        // Configure RPC
        RpcDefaultDelegates.WebSocketChannelOptionsProvider =
            (_, _) => WebSocketChannel<RpcMessage>.Options.Default with {
                // The fastest serializer
                Serializer = new FastRpcMessageByteSerializer(MemoryPackByteSerializer.Default),
                // Default frame delayer increases invalidate-to-update delays, and since this sample
                // contains nearly real-time part (streaming & RPC streaming), we remove any delays
                // to show peak performance here.
                FrameDelayerFactory = null,
            };

        // Blazorise
        services.AddBlazorise(options => {
                options.Immediate = true;
                options.Debounce = true;
            })
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();

        // Fusion services
        var fusion = services.AddFusion();
        fusion.AddBlazor().AddAuthentication();
        fusion.AddFusionTime();
        fusion.AddService<ILocalComposerService, LocalComposerService>();

        // Default update delay is 0.1s
        services.AddTransient<IUpdateDelayer>(c => new UpdateDelayer(c.UIActionTracker(), 0.1));
    }
}
