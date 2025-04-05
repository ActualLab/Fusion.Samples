using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Blazor.Authentication;
using ActualLab.Fusion.EntityFramework;
using ActualLab.Fusion.Server;
using ActualLab.IO;
using ActualLab.RestEase;
using ActualLab.Rpc;
using ActualLab.Rpc.Server;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;
using Samples.Blazor.Abstractions;
using Samples.Blazor.Server;
using Samples.Blazor.Server.Components.Pages;
using Samples.Blazor.Server.Services;
using Samples.Blazor.UI;

var builder = WebApplication.CreateBuilder();
var env = builder.Environment;
var cfg = builder.Configuration;
var serverSettings = cfg.GetSettings<ServerSettings>();

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


// Ensure the DB is created
var dbContextFactory = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
await using var dbContext = dbContextFactory.CreateDbContext();
// await dbContext.Database.EnsureDeletedAsync();
await dbContext.Database.EnsureCreatedAsync();

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
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
            logging.AddFilter("ActualLab.Fusion.Operations", LogLevel.Information);
        }
    });
}

void ConfigureServices()
{
    services.AddSingleton(serverSettings);

    // DbContext & related services
    var appTempDir = FilePath.GetApplicationTempDirectory("", true);
    var dbPath = appTempDir & "BlazorApp_v1.db";
    services.AddDbContextFactory<AppDbContext>(db => {
        db.UseSqlite($"Data Source={dbPath}");
        if (env.IsDevelopment())
            db.EnableSensitiveDataLogging();
    });
    services.AddDbContextServices<AppDbContext>(db => {
        db.AddEntityResolver<long, ChatMessage>();
        db.AddOperations(operations => {
            operations.ConfigureOperationLogReader(_ => new() {
                // We use FileBasedDbOperationLogChangeTracking, so unconditional wake-up period
                // can be arbitrary long - all depends on the reliability of Notifier-Monitor chain.
                // See what .ToRandom does - most timeouts in Fusion settings are RandomTimeSpan-s,
                // but you can provide a normal one too - there is an implicit conversion from it.
                CheckPeriod = TimeSpan.FromSeconds(env.IsDevelopment() ? 60 : 5).ToRandom(0.05),
            });
            operations.AddFileSystemOperationLogWatcher();
        });
    });

    // Fusion
    var fusion = services.AddFusion(RpcServiceMode.Server, true);
    var fusionServer = fusion.AddWebServer();

    // Enable this to test how the client behaves w/ a delay
    // fusion.Rpc.AddInboundMiddleware(c => new RpcRandomDelayMiddleware(c) {
    //     Delay = new(1, 0.1),
    // });

    fusion.AddDbAuthService<AppDbContext, long>();
    fusionServer.ConfigureAuthEndpoint(_ => new() {
        DefaultSignInScheme = MicrosoftAccountDefaults.AuthenticationScheme,
        SignInPropertiesBuilder = (_, properties) => {
            properties.IsPersistent = true;
        }
    });
    fusionServer.ConfigureServerAuthHelper(_ => new() {
        NameClaimKeys = [],
    });
    services.AddSingleton(new PresenceReporter.Options() {
        UpdatePeriod = TimeSpan.FromMinutes(1)
    });

    // Fusion services
    fusion.AddService<ITimeService, TimeService>();
    fusion.AddService<ISumService, SumService>();
    fusion.AddService<IComposerService, ComposerService>();
    fusion.AddService<IScreenshotService, ScreenshotService>();
    fusion.AddService<IChatService, ChatService>();

    // RestEase clients
    var restEase = services.AddRestEase();
    restEase.AddClient<IForismaticClient>();

    // Data protection
    services.AddScoped(c => c.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
    services.AddDataProtection().PersistKeysToDbContext<AppDbContext>();

    // ASP.NET Core authentication providers
    services.AddAuthentication(options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    }).AddCookie(options => {
        options.LoginPath = "/signIn";
        options.LogoutPath = "/signOut";
        if (env.IsDevelopment())
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        // This controls the expiration time stored in the cookie itself
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        // And this controls when the browser forgets the cookie
        options.Events.OnSigningIn = ctx => {
            ctx.CookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(28);
            return Task.CompletedTask;
        };
    }).AddMicrosoftAccount(options => {
        options.ClientId = serverSettings.MicrosoftAccountClientId;
        options.ClientSecret = serverSettings.MicrosoftAccountClientSecret;
        // That's for personal account authentication flow
        options.AuthorizationEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";
        options.TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    }).AddGitHub(options => {
        options.Scope.Add("read:user");
        options.Scope.Add("user:email");
        options.ClientId = serverSettings.GitHubClientId;
        options.ClientSecret = serverSettings.GitHubClientSecret;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    });

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
    fusion.AddBlazor().AddAuthentication().AddPresenceReporter(); // Must follow services.AddServerSideBlazor()!
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
    app.UseAuthentication();
    app.UseAntiforgery();

    // Razor components
    app.MapStaticAssets();
    app.MapRazorComponents<_HostPage>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(App).Assembly);

    // Fusion endpoints
    app.MapRpcWebSocketServer();
    app.MapFusionAuthEndpoints();
    app.MapFusionRenderModeEndpoints();
}
