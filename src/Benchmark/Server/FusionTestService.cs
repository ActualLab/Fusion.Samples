namespace Samples.Benchmark.Server;

public class FusionTestService(IServiceProvider services)
    : DbTestService(services), IFusionTestService, IFusionAsRpcTestService
{
    public override async Task AddOrUpdate(TestItem item, long? version, CancellationToken cancellationToken = default)
    {
        await base.AddOrUpdate(item, version, cancellationToken).ConfigureAwait(false);
        using (Invalidation.Begin()) {
            _ = TryGet(item.Id, default);
            _ = GetAll(default);
        }
    }

    public override async Task Remove(long itemId, long version, CancellationToken cancellationToken = default)
    {
        await base.Remove(itemId, version, cancellationToken).ConfigureAwait(false);
        using (Invalidation.Begin()) {
            _ = TryGet(itemId, default);
            _ = GetAll(default);
        }
    }
}
