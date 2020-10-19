using System;
using Stl.DependencyInjection;

namespace Samples.Caching.Client
{
    [Settings("Client")]
    public class ClientSettings
    {
        public string ServerHost { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 5010;

        public Uri BaseUri => new Uri($"http://{ServerHost}:{ServerPort}/");
        public Uri ApiBaseUri => new Uri($"{BaseUri}api/");
    }
}
