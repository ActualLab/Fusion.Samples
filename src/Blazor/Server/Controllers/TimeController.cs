using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server;
using Samples.Blazor.Common.Services;

namespace Samples.Blazor.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeController : FusionController, ITimeService
    {
        private readonly ITimeService _time;

        public TimeController(ITimeService time, IPublisher publisher)
            : base(publisher)
            => _time = time;

        [HttpGet("get")]
        public Task<DateTime> GetTimeAsync(CancellationToken cancellationToken)
            => PublishAsync(ct => _time.GetTimeAsync(ct));

        [HttpGet("getUptime")]
        public Task<TimeSpan> GetUptimeAsync(TimeSpan updatePeriod, CancellationToken cancellationToken = default)
            => PublishAsync(ct => _time.GetUptimeAsync(updatePeriod, ct));
    }
}
