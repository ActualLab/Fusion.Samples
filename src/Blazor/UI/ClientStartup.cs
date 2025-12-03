using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Blazor.Authentication;
using ActualLab.Fusion.Client.Caching;
using ActualLab.Fusion.Extensions;
using ActualLab.Fusion.UI;
using ActualLab.Rpc;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Samples.Blazor.Abstractions;
using Samples.Blazor.UI.Services;

namespace Samples.Blazor.UI;

[UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCodeAttribute", Justification = "Fine here")]
public static class ClientStartup
{
    public static void ConfigureServices(IServiceCollection services, WebAssemblyHostBuilder builder)
    {
        var logging = builder.Logging;
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        logging.AddFilter(typeof(App).Namespace, LogLevel.Information);
        logging.AddFilter(typeof(Computed).Namespace, LogLevel.Information);
        logging.AddFilter(typeof(InMemoryRemoteComputedCache).Namespace, LogLevel.Information);
        logging.AddFilter(typeof(RpcHub).Namespace, LogLevel.Debug);
        logging.AddFilter(typeof(CommandHandlerResolver).Namespace, LogLevel.Debug);
        logging.AddFilter(typeof(IRemoteComputedCache).Namespace, LogLevel.Debug);
        logging.AddFilter(typeof(ComponentInfo).Namespace, LogLevel.Debug);

        // Default RPC client serialization format
        RpcSerializationFormatResolver.Default = new("msgpack5"); // msgpack5c, mempack5, json5, etc.

        // Fusion
        var fusion = services.AddFusion();
        var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
        fusion.Rpc.AddWebSocketClient(baseUri);
        // You may comment this out - the call below just enables RPC call logging
        /*
        services.AddSingleton<RpcPeerOptions>(_ => RpcPeerOptions.Default with {
            PeerFactory = (hub, peerRef) => peerRef.IsServer
                ? throw new NotSupportedException("No server peers on the client.")
                : new RpcClientPeer(hub, peerRef) { CallLogLevel = LogLevel.Information },
        });
        */

        // Fusion services
        fusion.AddAuthClient();
        fusion.AddClient<ITimeService>();
        fusion.AddClient<IScreenshotService>();
        fusion.AddClient<IChatService>();
        fusion.AddClient<IComposerService>();
        fusion.AddClient<ISumService>();

        ConfigureSharedServices(services);
    }

    public static void ConfigureSharedServices(IServiceCollection services)
    {
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
