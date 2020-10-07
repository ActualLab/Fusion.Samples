﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Samples.Caching.Common;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;

namespace Samples.Caching.Server.Controllers
{
    [Route("api/tenants")]
    [ApiController]
    public class TenantController : FusionController, ITenantService
    {
        private ITenantService Tenants { get; }

        public TenantController(ITenantService tenants, IPublisher publisher)
            : base(publisher)
            => Tenants = tenants;

        [HttpPost("addOrUpdate")]
        public Task AddOrUpdateAsync([FromBody] Tenant tenant, long? version, CancellationToken cancellationToken = default)
            => Tenants.AddOrUpdateAsync(tenant, version, cancellationToken);

        [HttpPost("remove")]
        public Task RemoveAsync(string tenantId, long version, CancellationToken cancellationToken = default)
            => Tenants.RemoveAsync(tenantId, version, cancellationToken);

        // Compute methods

        [HttpGet("getAll")]
        public Task<Tenant[]> GetAllAsync(CancellationToken cancellationToken = default)
            => PublishAsync(ct => Tenants.GetAllAsync(ct), cancellationToken);

        [HttpGet("get")]
        public Task<Tenant?> TryGetAsync(string tenantId, CancellationToken cancellationToken = default)
            => PublishAsync(ct => Tenants.TryGetAsync(tenantId ?? "", ct), cancellationToken);
    }
}