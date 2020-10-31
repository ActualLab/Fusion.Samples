using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi.Models;
using Samples.Blazor.Common.Services;
using Samples.Blazor.Server.Services;
using Stl.DependencyInjection;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Fusion.Server;
using Stl.IO;

namespace Samples.Blazor.Server
{
    public class Startup
    {
        private IConfiguration Cfg { get; }
        private IWebHostEnvironment Env { get; }
        private ILogger Log { get; set; } = NullLogger<Startup>.Instance;

        public Startup(IConfiguration cfg, IWebHostEnvironment environment)
        {
            Cfg = cfg;
            Env = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddLogging(logging => {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
            });

            // DbContext & related services
            var appTempDir = PathEx.GetApplicationTempDirectory("", true);
            var dbPath = appTempDir & "App.db";
            services.AddDbContextPool<AppDbContext>(builder => {
                builder.UseSqlite($"Data Source={dbPath}", sqlite => { });
            });

            // Fusion services
            services.AddSingleton(new Publisher.Options() { Id = Settings.PublisherId });
            services.AddSingleton(new PresenceService.Options() { UpdatePeriod = TimeSpan.FromMinutes(1) });
            var fusion = services.AddFusion();
            var fusionServer = fusion.AddWebSocketServer();
            var fusionClient = fusion.AddRestEaseClient();
            var fusionAuth = fusion.AddAuthentication().AddServer();
            // This method registers services marked with any of ServiceAttributeBase descendants, including:
            // [Service], [ComputeService], [RestEaseReplicaService], [LiveStateUpdater]
            services.AttributeBased().AddServicesFrom(Assembly.GetExecutingAssembly());
            // Registering shared services from the client
            Client.Program.ConfigureSharedServices(services);

            // Note that you should never store these secrets in your code.
            // We put them here solely to simplify running this sample.
            var gitHubClientId = "7a38bc415f7e1200fee2";
            var gitHubClientSecret = Encoding.UTF8.GetString(
                Convert.FromBase64String("OGNkMTAzM2JmZjljOTk3ODc5MjhjNTNmMmE3Y2Q1NWU0ZmNlNjU0OA=="));
            services.AddAuthentication(options => {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options => {
                    options.LoginPath = "/signin";
                    options.LogoutPath = "/signout";
                })
                .AddGitHub(options => {
                    options.ClientId = gitHubClientId;
                    options.ClientSecret = gitHubClientSecret;
                    options.Scope.Add("read:user");
                    // options.Scope.Add("user:email");
                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                });

            // Web
            services.AddRouting();
            services.AddMvc().AddApplicationPart(Assembly.GetExecutingAssembly());
            services.AddServerSideBlazor(o => o.DetailedErrors = true);
            fusionAuth.AddBlazor();

            // Swagger & debug tools
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo {
                    Title = "Samples.Blazor.Server API", Version = "v1"
                });
            });
        }

        public void Configure(IApplicationBuilder app, ILogger<Startup> log)
        {
            Log = log;

            // This server serves static content from Blazor Client,
            // and since we don't copy it to local wwwroot,
            // we need to find Client's wwwroot in bin/(Debug/Release) folder
            // and set it as this server's content root.
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var binCfgPart = Regex.Match(baseDir, @"[\\/]bin[\\/]\w+[\\/]").Value;
            Env.WebRootPath = Path.GetFullPath(Path.Combine(baseDir,
                $"../../../../Client/{binCfgPart}/netstandard2.1/")) + "wwwroot";
            Env.WebRootFileProvider = new PhysicalFileProvider(Env.WebRootPath);
            StaticWebAssetsLoader.UseStaticWebAssets(Env, Cfg);

            if (Env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseWebSockets(new WebSocketOptions() {
                ReceiveBufferSize = 16_384,
                KeepAliveInterval = TimeSpan.FromSeconds(30),
            });
            app.UseFusionSession();

            // Static + Swagger
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
            });

            // API controllers
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => {
                endpoints.MapBlazorHub();
                endpoints.MapFusionWebSocketServer();
                endpoints.MapControllers();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
