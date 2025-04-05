using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Extensions;
using ActualLab.Fusion.Server;
using ActualLab.Rpc;
using ActualLab.Rpc.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration.Memory;
using Samples.HelloBlazorHybrid.Abstractions;
using Samples.HelloBlazorHybrid.Server.Components.Pages;
using Samples.HelloBlazorHybrid.Services;
using Samples.HelloBlazorHybrid.UI;

var builder = WebApplication.CreateBuilder();
var env = builder.Environment;
var cfg = builder.Configuration;

cfg.Sources.Insert(0, new MemoryConfigurationSource() {
    InitialData = new Dictionary<string, string>(StringComparer.Ordinal) {
        { WebHostDefaults.ServerUrlsKey, "http://localhost:5005" }, // Override default server URLs
    }!
});

// Configure services
var services = builder.Services;
ConfigureLogging();
ConfigureServices();
builder.WebHost.UseDefaultServiceProvider((ctx, options) => {
    if (ctx.HostingEnvironment.IsDevelopment()) {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    }
});

// Build & configure app
var app = builder.Build();
StaticLog.Factory = app.Services.LoggerFactory();
var log = StaticLog.For<Program>();
ConfigureApp();

await app.RunAsync();
return;

void ConfigureLogging()
{
    // Logging
    services.AddLogging(logging => {
        // Use appsettings.*.json to change log filters
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
        if (env.IsDevelopment()) {
            logging.AddFilter("Microsoft", LogLevel.Warning);
            logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Information);
            logging.AddFilter("ActualLab.Fusion.Operations", LogLevel.Information);
        }
    });
}

void ConfigureServices()
{
    // Fusion
    var fusion = services.AddFusion(RpcServiceMode.Server);
    fusion.AddWebServer();

    // Fusion services
    fusion.AddFusionTime(); // IFusionTime is one of built-in compute services you can use
    fusion.AddService<ICounterService, CounterService>();
    fusion.AddService<IWeatherForecastService, WeatherForecastService>();
    fusion.AddService<IChatService, ChatService>();
    fusion.AddService<ChatBotService>();
    // This is just to make sure ChatBotService.StartAsync is called on startup
    services.AddHostedService(c => c.GetRequiredService<ChatBotService>());

    // ASP.NET Core / Blazor services
    // Web
    // services.AddMvc().AddApplicationPart(Assembly.GetExecutingAssembly());
    services.AddServerSideBlazor(o => {
        o.DetailedErrors = true;
        // Just to test TodoUI disposal handling - you shouldn't use settings like this in production:
        o.DisconnectedCircuitMaxRetained = 1;
        o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddInteractiveWebAssemblyComponents();
    fusion.AddBlazor();
    services.AddBlazorCircuitActivitySuppressor();

    // Shared UI services
    ClientStartup.ConfigureSharedServices(services);
}

void ConfigureApp()
{
    // Configure the HTTP request pipeline
    StaticWebAssetsLoader.UseStaticWebAssets(env, cfg);
    if (app.Environment.IsDevelopment()) {
        app.UseWebAssemblyDebugging();
    }
    else {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    app.UseHttpsRedirection();
    app.UseWebSockets(new WebSocketOptions() {
        KeepAliveInterval = TimeSpan.FromSeconds(30),
    });
    app.UseFusionSession();
    app.UseRouting();
    app.UseAntiforgery();

    // Razor components
    app.MapStaticAssets();
    app.MapRazorComponents<_HostPage>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(App).Assembly);

    // Fusion endpoints
    app.MapRpcWebSocketServer();
    app.MapFusionRenderModeEndpoints();
}
