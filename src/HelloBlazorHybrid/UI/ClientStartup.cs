using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Extensions;
using ActualLab.Fusion.UI;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Samples.HelloBlazorHybrid.Abstractions;

namespace Samples.HelloBlazorHybrid.UI;

[UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCodeAttribute", Justification = "Fine here")]
public static class ClientStartup
{
    public static void ConfigureServices(IServiceCollection services, WebAssemblyHostBuilder builder)
    {
        // Logging
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        // Fusion
        var fusion = services.AddFusion();
        var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
        fusion.Rpc.AddWebSocketClient(baseUri);

        // Fusion service clients
        fusion.AddClient<ICounterService>();
        fusion.AddClient<IWeatherForecastService>();
        fusion.AddClient<IChatService>();

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

        // Other UI-related services
        var fusion = services.AddFusion();
        fusion.AddBlazor();
        fusion.AddFusionTime();

        // Default update delay is set to 0.1s
        services.AddTransient<IUpdateDelayer>(c => new UpdateDelayer(c.UIActionTracker(), 0.1));
    }
}
