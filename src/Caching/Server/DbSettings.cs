using Stl.DependencyInjection;

namespace Samples.Caching.Server
{
    [Settings("DB")]
    public class DbSettings
    {
        public string ServerHost { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 5020;
        public string DatabaseName { get; set; } = "Samples_Caching";
    }
}
