using System.Linq;
using Microsoft.EntityFrameworkCore;
using Samples.Caching.Common;
using Stl.Fusion.EntityFramework;

namespace Samples.Caching.Server.Services;

public class TenantService : DbServiceBase<AppDbContext>, ISqlTenantService
{
    private bool IsComputeService { get; }

    public TenantService(IServiceProvider services) : base(services)
        => IsComputeService = GetType() != typeof(TenantService);

    public async Task AddOrUpdate(Tenant tenant, long? version, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext().ReadWrite();
        if (version.HasValue) {
            var entry = dbContext.Tenants.Update(tenant);
            entry.Property(nameof(Tenant.Version)).OriginalValue = version.GetValueOrDefault();
        }
        else {
            dbContext.Tenants.Add(tenant);
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (IsComputeService)
            using (Computed.Invalidate()) {
                _ = Get(tenant.Id, default);
                _ = GetAll(default);
            }
    }

    public async Task Remove(string tenantId, long version, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext().ReadWrite();
        var entry = dbContext.Tenants.Remove(new Tenant() { Id = tenantId });
        entry.Property(nameof(Tenant.Version)).OriginalValue = version;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (IsComputeService)
            using (Computed.Invalidate()) {
                _ = Get(tenantId, default);
                _ = GetAll(default);
            }
    }

    // Compute methods

    public virtual async Task<Tenant[]> GetAll(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        var tenants = await dbContext.Tenants.AsQueryable()
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return tenants;
    }

    public virtual async Task<Tenant?> Get(string tenantId, CancellationToken cancellationToken = default)
    {
        // var c = Computed.GetExisting(() => GetAll(default));
        await using var dbContext = CreateDbContext();
        var tenant = await dbContext.Tenants.AsQueryable()
            .SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken).ConfigureAwait(false);
        return tenant;
    }
}
