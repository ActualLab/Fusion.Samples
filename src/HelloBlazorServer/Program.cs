using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Extensions;
using ActualLab.Fusion.Server;
using ActualLab.Fusion.Server.Middlewares;
using ActualLab.Fusion.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;
using Samples.HelloBlazorServer.Components.Pages;
using Samples.HelloBlazorServer.Services;

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
    var fusion = services.AddFusion();

    // Fusion services
    fusion.AddFusionTime(); // IFusionTime is one of the built-in compute services you can use
    fusion.AddService<CounterService>();
    fusion.AddService<WeatherForecastService>();
    fusion.AddService<ChatService>();
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
    builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    fusion.AddBlazor();
    services.AddScoped<IUpdateDelayer>(_ => FixedDelayer.MinDelay); // Default delay for ComputedStateComponents updates

    // We don't want to add all Fusion auth services, but this one is required to make _HostPage.razor work
    services.AddSingleton(_ => SessionMiddleware.Options.Default);
    services.AddScoped(c => new SessionMiddleware(c.GetRequiredService<SessionMiddleware.Options>(), c));
}

void ConfigureApp()
{
    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment()) {
        StaticWebAssetsLoader.UseStaticWebAssets(env, cfg);
    }
    else {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    app.UseHttpsRedirection();
    app.UseFusionSession();
    app.UseRouting();
    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<_HostPage>()
        .AddInteractiveServerRenderMode();
}
