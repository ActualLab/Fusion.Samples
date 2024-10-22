using Microsoft.EntityFrameworkCore;
using ActualLab.Fusion.EntityFramework;

namespace Samples.Benchmark;

public static class ServiceCollectionExt
{
    public static IServiceCollection AddAppDbContext(this IServiceCollection services)
    {
        var dbHost = Environment.GetEnvironmentVariable("DbHost").NullIfEmpty() ?? "localhost";
        var dbConnectionString = string.Format(DbConnectionString, dbHost);
        services.AddPooledDbContextFactory<AppDbContext>((_, dbContext) => {
            dbContext.UseNpgsql(dbConnectionString, _ => { });
        }, 512);
        services.AddDbContextServices<AppDbContext>();
        services.AddSingleton<DbInitializer>();
        services.AddSingleton<DbTestService>();
        services.AddFusion(); // DbHub now caches StateFactory, so we have to add Fusion
        return services;
    }
}
