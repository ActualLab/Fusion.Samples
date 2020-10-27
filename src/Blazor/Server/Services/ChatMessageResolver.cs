using System;
using Samples.Blazor.Common.Services;
using Samples.Helpers;
using Stl.DependencyInjection;

namespace Samples.Blazor.Server.Services
{
    [Service]
    public class ChatMessageResolver : DbEntityResolver<AppDbContext, long, ChatMessage>
    {
        public ChatMessageResolver(IServiceProvider services) : base(services)
        {
            BatchProcessor.ConcurrencyLevel = 1; // Just to show how it works
        }
    }
}
