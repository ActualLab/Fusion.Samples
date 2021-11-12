﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Samples.Caching.Common;
using Samples.Caching.Server;
using Samples.Caching.Server.Services;
using Stl.DependencyInjection;
using Stl.Fusion.Client;
using Stl.OS;
using static System.Console;

namespace Samples.Caching.Client;

class Program
{
    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        CancelKeyPress += (s, ea) => cts.Cancel();
        var cancellationToken = cts.Token;

        var localServices = await CreateLocalServiceProvider(cancellationToken);
        await localServices.GetRequiredService<ServiceChecker>().WaitForServices(cancellationToken);
        await localServices.GetRequiredService<DbInitializer>().Initialize(true, cancellationToken);

        var benchmark = new TenantBenchmark(localServices) {
            TimeCheckOperationIndexMask = 7
        };
        await benchmark.Initialize(cancellationToken);

        benchmark.Services = localServices;
        benchmark.ConcurrencyLevel = HardwareInfo.ProcessorCount - 2;
        benchmark.DumpParameters();
        benchmark.TenantServiceResolver = c => c.GetRequiredService<ITenantService>();
        WriteLine();
        WriteLine("Local services:");
        await benchmark.Run("Fusion's Compute Service [-> EF Core -> SQL Server]", cancellationToken);
        benchmark.TenantServiceResolver = c => c.GetRequiredService<ISqlTenantService>();
        await benchmark.Run("Regular Service [-> EF Core -> SQL Server]", cancellationToken);

        WriteLine();
        WriteLine("Remote services:");
        var remoteServices = await CreateRemoteServiceProvider(cancellationToken);
        benchmark.Services = remoteServices;
        benchmark.TenantServiceResolver = c => c.GetRequiredService<ITenantService>();
        await benchmark.Run("Fusion's Replica Client [-> HTTP+WebSocket -> ASP.NET Core -> Compute Service -> EF Core -> SQL Server]", cancellationToken);
        benchmark.TenantServiceResolver = c => c.GetRequiredService<ITenantServiceClient>();
        await benchmark.Run("RestEase Client [-> HTTP -> ASP.NET Core -> Compute Service -> EF Core -> SQL Server]", cancellationToken);
        benchmark.TenantServiceResolver = c => c.GetRequiredService<ISqlTenantService>();
        await benchmark.Run("RestEase Client [-> HTTP -> ASP.NET Core -> Regular Service -> EF Core -> SQL Server]", cancellationToken);
    }

    public static Task<IServiceProvider> CreateRemoteServiceProvider(CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        var cfg = Host.CreateDefaultBuilder().Build().Services.GetRequiredService<IConfiguration>();
        services.AddSingleton(cfg);
        services.AddSettings<ClientSettings>("Client");
        var clientSettings = services.BuildServiceProvider().GetRequiredService<ClientSettings>();

        var fusion = services.AddFusion();
        var fusionClient = fusion.AddRestEaseClient((_, options) => {
            options.BaseUri = clientSettings.BaseUri;
            options.IsLoggingEnabled = false;
        }).ConfigureHttpClientFactory((_, name, options) => {
            options.HttpClientActions.Add(c => c.BaseAddress = clientSettings.ApiBaseUri);
        });
        fusionClient.AddReplicaService<ITenantService, ITenantClientDef>();
        fusionClient.AddClientService<ITenantServiceClient, ITenantClientDef>();
        fusionClient.AddClientService<ISqlTenantService, ISqlTenantClientDef>();
        return Task.FromResult((IServiceProvider) services.BuildServiceProvider());
    }

    public static Task<IServiceProvider> CreateLocalServiceProvider(CancellationToken cancellationToken)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((ctx, cfg) => {
                // Looks like there is no better way to set _default_ URL
                cfg.Sources.Insert(0, new MemoryConfigurationSource() {
                    InitialData = new Dictionary<string, string>() {
                        {WebHostDefaults.EnvironmentKey, "Production"},
                        {WebHostDefaults.ServerUrlsKey, "http://localhost:0"},
                    }
                });
            })
            .ConfigureWebHostDefaults(webHost => webHost
                .UseDefaultServiceProvider((ctx, options) => {
                    options.ValidateScopes = ctx.HostingEnvironment.IsDevelopment();
                    options.ValidateOnBuild = true;
                })
                .ConfigureServices((ctx, services) => {
                    services.AddSettings<ClientSettings>("Client");
                    services.AddSingleton<ServiceChecker>();
                })
                .UseStartup<Startup>())
            .Build();
        return Task.FromResult(host.Services);
    }
}
